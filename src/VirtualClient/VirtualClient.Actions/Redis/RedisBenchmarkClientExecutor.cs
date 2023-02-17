// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Redis Benchmark Client Executor.
    /// </summary>
    public class RedisBenchmarkClientExecutor : RedisExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisBenchmarkClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public RedisBenchmarkClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(3, (retries) => TimeSpan.FromSeconds(retries * 2));
            this.PollingTimeout = TimeSpan.FromMinutes(40);
        }

        /// <summary>
        /// Number of requests from client.
        /// </summary>
        public int RequestCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.RequestCount));
            }
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
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        protected IAsyncPolicy ClientFlowRetryPolicy { get; set; }

        /// <summary>
        /// The timespan at which the client will poll the server for responses before
        /// timing out.
        /// </summary>
        protected TimeSpan PollingTimeout { get; set; }

        /// <summary>
        /// Path to memtier benchmark executable (e.g. memtier_benchmark).
        /// </summary>
        protected string RedisExecutablePath { get; set; }

        /// <summary>
        /// Path to Redis Package.
        /// </summary>
        protected string RedisPackagePath { get; set; }

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

                            // 3) Execute the client workload.
                            // ===========================================================================
                            ipAddress = IPAddress.Parse(server.IPAddress);
                            await this.ExecuteWorkloadAsync(ipAddress, telemetryContext, cancellationToken);
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
                        await this.ExecuteWorkloadAsync(ipAddress, telemetryContext, cancellationToken);
                    }
                }));
            }

            return Task.WhenAll(clientWorkloadTasks);
        }

        /// <summary>
        /// Initializes the environment and dependencies for client of redis Benchmark workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            DependencyPath redisPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, CancellationToken.None);

            this.RedisPackagePath = redisPackage.Path;
            this.RedisExecutablePath = this.PlatformSpecifics.Combine(this.RedisPackagePath, "src", "redis-benchmark");

            await this.SystemManagement.MakeFileExecutableAsync(this.RedisExecutablePath, this.Platform, cancellationToken);
            this.InitializeApiClients();
        }

        private void CaptureMetrics(IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    RedisBenchmarkMetricsParser redisBenchmarkMetricsParser = new RedisBenchmarkMetricsParser(process.StandardOutput.ToString());
                    IList<Metric> metrics = redisBenchmarkMetricsParser.Parse();

                    this.Logger.LogMetrics(
                        "Redis-Benchmark",
                        scenarioName: this.Scenario,
                        process.StartTime,
                        process.ExitTime,
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

        private Task ExecuteWorkloadAsync(IPAddress ipAddress, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", telemetryContext.Clone(), async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    // e.g.
                    // sudo bash -c "/home/user/virtualclient/linux-x64/src/redis-benchmark -h 1.2.3.5 -p 6379 -c 2 -n 10000 -P 32 -q --csv"
                    string workingDirectory = this.PlatformSpecifics.Combine(this.RedisPackagePath, "src");
                    string commandArguments = @$"-c ""{this.RedisExecutablePath} -h {ipAddress} -p {this.Port} -c {this.ClientsPerThread} -n {this.RequestCount} -P {this.PipelineDepth} -q --csv""";

                    using (IProcessProxy process = await this.ExecuteCommandAsync("bash", commandArguments, workingDirectory, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "Redis-Benchmark", logToFile: true);

                            process.ThrowIfWorkloadFailed();
                            this.CaptureMetrics(process, telemetryContext, cancellationToken);
                        }
                    }
                }
            });
        }
    }
}
