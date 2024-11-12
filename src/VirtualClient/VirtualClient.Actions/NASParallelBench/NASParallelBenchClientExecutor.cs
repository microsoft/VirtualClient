// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Class encapsulating logic to execute and collect metrics for
    /// NASA Advanced Supercomputing parallel benchmarks. (NAS parallel benchmarks)
    /// For Client side.
    /// </summary>
    public class NASParallelBenchClientExecutor : NASParallelBenchExecutor
    {
        private List<IApiClient> apiClients;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="NASParallelBenchClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public NASParallelBenchClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ClientHeartbeatPollingTimeout = TimeSpan.FromMinutes(20);
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.apiClients = new List<IApiClient>();
        }

        /// <summary>
        /// Name of the benchmark to be executed.
        /// </summary>
        public string Benchmark => this.Parameters.GetValue<string>(nameof(NASParallelBenchClientExecutor.Benchmark));

        /// <summary>
        /// The user who has the ssh identity registered for.
        /// </summary>
        public string Username
        {
            get
            {
                string username = this.Parameters.GetValue<string>(nameof(NASParallelBenchClientExecutor.Username), string.Empty);
                if (string.IsNullOrWhiteSpace(username))
                {
                    username = this.PlatformSpecifics.GetLoggedInUser();
                }

                return username;
            }
        }

        /// <summary>
        /// The timeout to apply to polling for individual/target client instances
        /// of the Virtual Client to come online.
        /// </summary>
        protected TimeSpan ClientHeartbeatPollingTimeout { get; set; }

        /// <summary>
        /// Executes NAS parallel benchmark client side.
        /// </summary>
        protected async override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.IsBenchmarkSupported())
            {
                await this.WaitForClientsOnlineAsync(cancellationToken)
                    .ConfigureAwait(false);

                await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private Task ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", telemetryContext.Clone(), async () =>
            {
                IFileSystem fileSystem = this.SystemManager.FileSystem;

                string benchmarkPath = this.PlatformSpecifics.Combine(this.GetBenchmarkDirectory(), "bin", this.Benchmark);

                if (!this.SystemManager.FileSystem.File.Exists(benchmarkPath))
                {
                    throw new WorkloadException(
                    $"Could not find benchmark: '{this.Benchmark}' on path: '{benchmarkPath}'",
                    ErrorReason.InvalidProfileDefinition);
                }

                // Number of threads used by the process.
                // Open Multi Threading number of threads.
                string ompNumThreads = $"export OMP_NUM_THREADS={Environment.ProcessorCount}";
                string command = string.Empty;
                string args = string.Empty;
                string scenarioArguments = string.Empty;

                if (this.IsMultiRoleLayout())
                {
                    IEnumerable<string> allInstances = this.Layout.Clients.Select(cl => cl.IPAddress.ToString()).ToList();
                    string hosts = string.Join(",", allInstances);
                    scenarioArguments = $"-np {this.Layout.Clients.Count()} --host {hosts} {benchmarkPath}";

                    // It turns of the fixed number of processes required by the benchmark.
                    // For example BT requires number of processes to be square number like 1,4,9,etc.
                    // On turning off the condition the workload will run with the maximum possible processes from the given number of processes.
                    // For example "-np 6", It will run with 4 processes.
                    string npbNprocsStrict = $"export NPB_NPROCS_STRICT=off";
                    command = $"runuser -l {this.Username} -c \'{ompNumThreads} && {npbNprocsStrict} && mpiexec {scenarioArguments}\'";
                }
                else
                {
                    command = $"{ompNumThreads} && {benchmarkPath}";
                }

                telemetryContext.AddContext("command", "bash");
                telemetryContext.AddContext("commandArguments", $"-c \"{command}\"");

                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    using (IProcessProxy process = this.SystemManager.ProcessManager.CreateElevatedProcess(this.Platform, "bash", $"-c \"{command}\""))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "NASParallelBench", logToFile: true);

                            process.ThrowIfWorkloadFailed();
                            this.CaptureMetrics(process, scenarioArguments, telemetryContext);
                        }
                    }
                }
            });
        }

        private void CaptureMetrics(IProcessProxy process, string commandArguments, EventContext telemetryContext)
        {
            this.MetadataContract.AddForScenario(
                "NASParallelBench",
                process.FullCommand(),
                toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            NASParallelBenchMetricsParser parser = new NASParallelBenchMetricsParser(process.StandardOutput.ToString());
            IList<Metric> metrics = parser.Parse().ToList();

            string computingMethod = this.IsMultiRoleLayout() ? "MPI" : "OMP";

            this.Logger.LogMetrics(
                "NASParallelBench",
                $"{computingMethod}_{this.Benchmark}",
                process.StartTime,
                process.ExitTime,
                metrics,
                null,
                commandArguments,
                this.Tags,
                telemetryContext);
        }

        /// <summary>
        /// All Benchmarks do not have multiple machine supported. 
        /// The given method checks whether we can run the workload in the given environment layout or not. 
        /// </summary>
        /// <returns>Boolean if defined sub-benchmark is supported.</returns>
        private bool IsBenchmarkSupported()
        {
            bool isSupported = true;

            if ((this.Benchmark.StartsWith("dc") && this.IsMultiRoleLayout()) ||
                (this.Benchmark.StartsWith("ua") && this.IsMultiRoleLayout()) ||
                (this.Benchmark.StartsWith("dt") && !this.IsMultiRoleLayout()) ||
                (this.Benchmark.StartsWith("dt") && this.Benchmark.Contains("WH") && this.Layout.Clients.Count() < 5) ||
                (this.Benchmark.StartsWith("dt") && this.Benchmark.Contains("BH") && this.Layout.Clients.Count() < 5) ||
                (this.Benchmark.StartsWith("dt") && this.Benchmark.Contains("SH") && this.Layout.Clients.Count() <= 12))
            {
                isSupported = false;
            }

            return isSupported;
        }

        private async Task WaitForClientsOnlineAsync(CancellationToken cancellationToken)
        {
            if (this.IsMultiRoleLayout())
            {
                IEnumerable<ClientInstance> instances = this.GetLayoutClientInstances(ClientRole.Server);
                IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();

                try
                {
                    foreach (ClientInstance client in instances)
                    {
                        IApiClient apiClient = clientManager.GetOrCreateApiClient(client.IPAddress, IPAddress.Parse(client.IPAddress));
                        this.apiClients.Add(apiClient);

                        await apiClient.PollForExpectedStateAsync(nameof(this.NpbBuildState), JObject.FromObject(this.NpbBuildState), this.ClientHeartbeatPollingTimeout, DefaultStateComparer.Instance, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
                finally
                {
                    this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", this.apiClients.ToArray());
                }
            }
        }
    }
}
