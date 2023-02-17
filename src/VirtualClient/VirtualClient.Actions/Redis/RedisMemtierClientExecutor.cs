// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// RedisMemtier Client Executor.
    /// </summary>
    public class RedisMemtierClientExecutor : RedisExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisMemtierClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public RedisMemtierClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(3, (retries) => TimeSpan.FromSeconds(retries * 2));
            this.PollingTimeout = TimeSpan.FromMinutes(40);
        }

        /// <summary>
        /// Number of clients per thread.
        /// </summary>
        public int ClientsPerThread
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ClientsPerThread));
            }
        }

        /// <summary>
        /// Number of threads.
        /// </summary>
        public int ThreadCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ThreadCount));
            }
        }

        /// <summary>
        /// Number of concurrent requests from client.
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
        public string RunCount
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.RunCount), out IConvertible runCount);
                return runCount?.ToString();
            }
        }

        /// <summary>
        /// Path to memtier benchmark executable (e.g. memtier_benchmark).
        /// </summary>
        protected string MemtierExecutablePath { get; set; }

        /// <summary>
        /// Path to Redis Package.
        /// </summary>
        protected string MemtierPackagePath { get; set; }

        /// <summary>
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        protected IAsyncPolicy ClientFlowRetryPolicy { get; set; }

        /// <summary>
        /// The timespan at which the client will poll the server for responses before
        /// timing out.
        /// </summary>
        protected TimeSpan PollingTimeout { get; set; }

        /// <summary>
        /// Executes  client side.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IPAddress ipAddress;
            List<Task> clientWorkloadTasks = new List<Task>();

            if (this.IsMultiRoleLayout())
            {
                IEnumerable<ClientInstance> targetServers = this.GetLayoutClientInstances(ClientRole.Server);
                foreach (ClientInstance server in targetServers)
                {
                    // Reliability/Recovery:
                    // The pattern here is to allow for any steps within the workflow to fail and to simply start the entire workflow
                    // over again.
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

                            await serverApiClient.PollForServerOnlineAsync(TimeSpan.FromSeconds(30), cancellationToken);

                            this.Logger.LogTraceMessage("Synchronization: Server online signal confirmed...");
                            this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                            // 3) Get Parameters required.
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
        /// Initializes the environment and dependencies for client of redis Memtier workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
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
                try
                {
                    MemtierMetricsParser memtierMetricsParser = new MemtierMetricsParser(results);
                    IList<Metric> metrics = memtierMetricsParser.Parse();

                    this.Logger.LogMetrics(
                        "Redis-Memtier",
                        scenarioName: this.Scenario,
                        startTime,
                        endTime,
                        metrics,
                        string.Empty,
                        null,
                        this.Tags,
                        telemetryContext);
                }
                catch (SchemaException exc)
                {
                    throw new WorkloadResultsException($"Failed to parse workload results.", exc, ErrorReason.WorkloadResultsParsingFailed);
                }
            }
        }

        private Task ExecuteWorkloadAsync(IPAddress ipAddress, int serverInstances, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", telemetryContext.Clone(), async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    DateTime startTime = DateTime.UtcNow;
                    StringBuilder results = new StringBuilder();

                    for (int i = 1; i <= serverInstances; i++)
                    {
                        int port = this.Port + i - 1;

                        string commandArguments = 
                            $"--server {ipAddress} --port {port} --protocol {this.Protocol} --clients {this.ClientsPerThread} --threads {this.ThreadCount} --ratio 1:9 " +
                            $"--data-size 32 --pipeline {this.PipelineDepth} --key-minimum 1 --key-maximum 10000000 --key-pattern R:R --run-count {this.RunCount} --test-time {this.Duration.TotalSeconds} " +
                            $"--print-percentiles 50,90,95,99,99.9 --random-data";

                        using (IProcessProxy process = await this.ExecuteCommandAsync(this.MemtierExecutablePath, commandArguments, this.MemtierPackagePath, telemetryContext, cancellationToken, runElevated: true))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "Redis-Memtier", results: process.StandardOutput.ToString().AsArray(), logToFile: true);
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
