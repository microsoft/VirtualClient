// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// MemcachedMemtier Client Executor.
    /// </summary>
    public class MemcachedMemtierClientExecutor : MemcachedExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemcachedMemtierClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public MemcachedMemtierClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(3, (retries) => TimeSpan.FromSeconds(retries * 2));
            this.PollingTimeout = TimeSpan.FromMinutes(40);
        }

        /// <summary>
        /// Number of clients.
        /// </summary>
        public int ClientsPerThread
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ClientsPerThread));
            }
        }

        /// <summary>
        /// Number of threads to be created at client side.
        /// </summary>
        public int ThreadCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ThreadCount));
            }
        }

        /// <summary>
        /// Pipeline depth at client side.
        /// </summary>
        public int PipelineDepth 
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.PipelineDepth));
            }
        }

        /// <summary>
        /// Time for which client executes load on server.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.Duration));
            }
        }

        /// <summary>
        /// Protocol to use at client side.
        /// </summary>
        public string Protocol
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Protocol));
            }
        }

        /// <summary>
        /// Number of runs the client executes load on server.
        /// </summary>
        public int RunCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.RunCount), 1);
            }
        }

        /// <summary>
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        protected IAsyncPolicy ClientFlowRetryPolicy { get; set; }

        /// <summary>
        /// Path to memtier benchmark executable (e.g. memtier_benchmark).
        /// </summary>
        protected string MemtierExecutablePath { get; set; }

        /// <summary>
        /// Path to Redis Package.
        /// </summary>
        protected string MemtierPackagePath { get; set; }

        /// <summary>
        /// The timespan at which the client will poll the server for responses before
        /// timing out.
        /// </summary>
        protected TimeSpan PollingTimeout { get; set; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IPAddress ipAddress;
            List<Task> clientWorkloadTasks = new List<Task>();

            if (this.IsMultiRoleLayout())
            {
                IEnumerable<ClientInstance> targetServers = this.GetLayoutClientInstances(ClientRole.Server);

                foreach (ClientInstance server in targetServers)
                {
                    clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            IApiClient serverApiClient = this.ApiClientManager.GetOrCreateApiClient(server.Name, server);

                            // 1) Confirm server is online.
                            // ===========================================================================
                            this.Logger.LogTraceMessage("Synchronization: Poll server API for heartbeat...");
                            await serverApiClient.PollForHeartbeatAsync(this.PollingTimeout, cancellationToken);

                            // 2) Confirm the server-side application (e.g. web server) is online.
                            // ===========================================================================
                            this.Logger.LogTraceMessage("Synchronization: Poll server for online signal...");
                            await serverApiClient.PollForServerOnlineAsync(TimeSpan.FromMinutes(10), cancellationToken);

                            this.Logger.LogTraceMessage("Synchronization: Server online signal confirmed...");
                            this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                            // 3) Get Parameters required.
                            // ===========================================================================
                            int serverInstances = await this.GetServerCount(serverApiClient, cancellationToken);

                            // 4) Execute the client workload.
                            // ===========================================================================
                            ipAddress = IPAddress.Parse(server.IPAddress);
                            await this.ExecuteWorkloadAsync(ipAddress, serverInstances, telemetryContext, cancellationToken);
                        }
                    }));
                }
            }
            else
            {
                ipAddress = IPAddress.Loopback;
                clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        int serverInstances = await this.GetServerCount(this.ServerApiClient, cancellationToken);
                        await this.ExecuteWorkloadAsync(ipAddress, serverInstances, telemetryContext, cancellationToken);
                    }
                }));
            }

            return Task.WhenAll(clientWorkloadTasks);
        }

        /// <summary>
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken);
            DependencyPath memtierPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, CancellationToken.None);

            this.MemtierPackagePath = memtierPackage.Path;
            this.MemtierExecutablePath = this.PlatformSpecifics.Combine(this.MemtierPackagePath, "memtier_benchmark");

            await this.SystemManagement.MakeFileExecutableAsync(this.MemtierExecutablePath, this.Platform, cancellationToken);
            this.InitializeApiClients();
        }

        private void CaptureMetrics(string results, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                results.ThrowIfNullOrWhiteSpace(nameof(results));

                try
                {
                    MemcachedMemtierMetricsParser resultsParser = new MemcachedMemtierMetricsParser(results);
                    IList<Metric> workloadMetrics = resultsParser.Parse();

                    this.Logger.LogMetrics(
                        "Memcached-Memtier",
                        this.Scenario,
                        startTime,
                        endTime,
                        workloadMetrics,
                        null,
                        null,
                        this.Tags,
                        telemetryContext);
                }
                catch (SchemaException exc)
                {
                    throw new WorkloadResultsException($"Failed to parse workload results file.", exc, ErrorReason.WorkloadResultsParsingFailed);
                }
            }
        }

        private Task ExecuteWorkloadAsync(IPAddress serverIpAddress, int serverInstances, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", telemetryContext.Clone(), async () =>
            {
                DateTime startTime = DateTime.UtcNow;
                StringBuilder results = new StringBuilder();

                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    for (int i = 0; i < serverInstances; i++)
                    {
                        int port = this.Port + i;

                        string command = $"-u {this.Username} {this.MemtierExecutablePath}";
                        string commandArguments = 
                            $"--server {serverIpAddress} --port {port} --protocol {this.Protocol} --clients {this.ClientsPerThread} --threads {this.ThreadCount} --ratio 1:9 --data-size 32 " +
                            $"--pipeline {this.PipelineDepth} --key-minimum 1 --key-maximum 10000000 --key-pattern R:R --run-count {this.RunCount} --test-time {this.Duration.TotalSeconds} " +
                            $"--print-percentiles 50,90,95,99,99.9 --random-data";

                        using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, this.MemtierPackagePath, telemetryContext, cancellationToken, runElevated: true))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "Memcached-Memtier", results: process.StandardOutput.ToString().AsArray(), logToFile: true);
                                process.ThrowIfWorkloadFailed();
                                results.AppendLine(process.StandardOutput.ToString());
                            }
                        }
                    }

                    this.CaptureMetrics(results.ToString(), startTime, DateTime.UtcNow, telemetryContext, cancellationToken);
                }
            });
        }

        private async Task<int> GetServerCount(IApiClient serverApiClient, CancellationToken cancellationToken)
        {
            Item<ServerState> state = await serverApiClient.GetStateAsync<ServerState>(
                nameof(ServerState),
                cancellationToken);

            if (state == null)
            {
                throw new WorkloadException(
                    $"Expected server state information missing. The server did not return state indicating the count/instances of the Memcached server are running.",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            return state.Definition.ServerCopies;
        }
    }
}
