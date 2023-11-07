// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Redis Benchmark Client Executor.
    /// </summary>
    public class RedisBenchmarkClientExecutor : RedisExecutor
    {
        private readonly object lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisBenchmarkClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public RedisBenchmarkClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
                .WaitAndRetryAsync(3, (retries) => TimeSpan.FromSeconds(retries * 2));

            this.ClientRetryPolicy = Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
                .WaitAndRetryAsync(3, (retries) => TimeSpan.FromSeconds(retries));

            this.PollingTimeout = TimeSpan.FromMinutes(40);
        }

        /// <summary>
        /// Parameter defines the number of Memtier benchmark instances to execute against
        /// each server instance. Default = # of logical cores/vCPUs on system.
        /// </summary>
        public int ClientInstances
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ClientInstances), 1);
            }
        }

        /// <summary>
        /// Parameter defines the Memtier benchmark toolset command line to execute.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CommandLine));
            }
        }

        /// <summary>
        /// Parameter defines the length of time the Memtier benchmark should be executed.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.Duration), TimeSpan.FromSeconds(60));
            }
        }

        /// <summary>
        /// Parameter defines true/false whether the action is meant to warm up the server.
        /// We do not capture metrics on warm up operations.
        /// </summary>
        public bool WarmUp
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.WarmUp), false);
            }
        }

        /// <summary>
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        protected IAsyncPolicy ClientFlowRetryPolicy { get; set; }

        /// <summary>
        /// The retry policy to apply to each Memtier workload instance when trying to startup
        /// against a target server.
        /// </summary>
        protected IAsyncPolicy ClientRetryPolicy { get; set; }

        /// <summary>
        /// True/false whether the Redis server instance has been warmed up.
        /// </summary>
        protected bool IsServerWarmedUp { get; set; }

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
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!this.WarmUp || !this.IsServerWarmedUp)
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
                                ServerState serverState = await this.GetServerStateAsync(serverApiClient, cancellationToken);

                                // 4) Execute the client workload.
                                // ===========================================================================
                                ipAddress = IPAddress.Parse(server.IPAddress);
                                await this.ExecuteWorkloadsAsync(ipAddress, serverState, telemetryContext, cancellationToken);
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
                            ServerState serverState = await this.GetServerStateAsync(this.ServerApiClient, cancellationToken);
                            await this.ExecuteWorkloadsAsync(ipAddress, serverState, telemetryContext, cancellationToken);
                        }
                    }));
                }

                await Task.WhenAll(clientWorkloadTasks);

                if (this.WarmUp)
                {
                    this.IsServerWarmedUp = true;
                }
            }
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
            DependencyPath redisPackage = await this.GetPackageAsync(this.PackageName, CancellationToken.None);

            this.RedisPackagePath = redisPackage.Path;
            this.RedisExecutablePath = this.PlatformSpecifics.Combine(this.RedisPackagePath, "src", "redis-benchmark");

            await this.SystemManagement.MakeFileExecutableAsync(this.RedisExecutablePath, this.Platform, cancellationToken);
            this.InitializeApiClients();
        }

        private void CaptureMetrics(string output, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    this.MetadataContract.AddForScenario(
                        "Redis-Benchmark",
                        commandArguments,
                        toolVersion: null);

                    this.MetadataContract.Apply(telemetryContext);

                    // The Redis workloads run multi-threaded. The lock is meant to ensure we do not have
                    // race conditions that affect the parsing of the results.
                    lock (this.lockObject)
                    {
                        RedisBenchmarkMetricsParser redisBenchmarkMetricsParser = new RedisBenchmarkMetricsParser(output);
                        IList<Metric> workloadMetrics = redisBenchmarkMetricsParser.Parse();

                        this.Logger.LogMetrics(
                            "Redis-Benchmark",
                            scenarioName: this.Scenario,
                            startTime,
                            endTime,
                            workloadMetrics,
                            null,
                            commandArguments,
                            this.Tags,
                            telemetryContext);
                    }
                }
                catch (SchemaException exc)
                {
                    throw new WorkloadResultsException($"Failed to parse workload results.", exc, ErrorReason.WorkloadResultsParsingFailed);
                }
            }
        }

        private Task ExecuteWorkloadsAsync(IPAddress serverIPAddress, ServerState serverState, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("serverIPAddress", serverIPAddress.ToString())
                .AddContext("serverPorts", serverState.Ports);

            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkloads", relatedContext.Clone(), async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    string command = "bash";
                    string workingDirectory = this.PlatformSpecifics.Combine(this.RedisPackagePath, "src");

                    List<string> commands = new List<string>();
                    relatedContext.AddContext("command", "bash");
                    relatedContext.AddContext("commandArguments", commands);
                    relatedContext.AddContext("workingDirectory", workingDirectory);

                    List<Task> workloadProcesses = new List<Task>();
                    foreach (int serverPort in serverState.Ports)
                    {
                        for (int i = 0; i < this.ClientInstances; i++)
                        {
                            // e.g.
                            // sudo bash -c "/home/user/virtualclient/linux-x64/src/redis-benchmark -h 1.2.3.5 -p 6379 -c 2 -n 10000 -P 32 -q --csv"
                            string commandArguments = $"-c \"{this.RedisExecutablePath} -h {serverIPAddress} -p {serverPort} {this.CommandLine}\"";
                            commands.Add($"{command} {commandArguments}");

                            workloadProcesses.Add(this.ExecuteWorkloadAsync(serverPort, command, commandArguments, workingDirectory, relatedContext.Clone(), cancellationToken));

                            if (this.WarmUp)
                            {
                                // We run ONLY 1 client workload per server endpoint/port when warming up.
                                break;
                            }
                        }
                    }

                    await Task.WhenAll(workloadProcesses);
                }
            });
        }

        private async Task ExecuteWorkloadAsync(int serverPort, string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                await (this.ClientRetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
                {
                    try
                    {
                        DateTime startTime = DateTime.UtcNow;
                        using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, telemetryContext, cancellationToken, runElevated: true))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                ConsoleLogger.Default.LogMessage($"Redis benchmark process exited (server port = {serverPort})...", telemetryContext);

                                await this.LogProcessDetailsAsync(process, telemetryContext, "Redis-Benchmark", logToFile: true);
                                process.ThrowIfWorkloadFailed();

                                if (!this.WarmUp)
                                {
                                    string output = process.StandardOutput.ToString();
                                    this.CaptureMetrics(output, process.FullCommand(), startTime, DateTime.UtcNow, telemetryContext, cancellationToken);
                                }
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        this.Logger.LogMessage(
                            $"{this.TypeName}.WorkloadStartError",
                            LogLevel.Warning,
                            telemetryContext.Clone().AddError(exc));

                        throw;
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
            }
            catch (Exception exc)
            {
                this.Logger.LogMessage(
                    $"{this.TypeName}.ExecuteWorkloadError",
                    LogLevel.Error,
                    telemetryContext.Clone().AddError(exc));

                throw;
            }
        }

        private async Task<ServerState> GetServerStateAsync(IApiClient serverApiClient, CancellationToken cancellationToken)
        {
            Item<ServerState> state = await serverApiClient.GetStateAsync<ServerState>(
                nameof(ServerState),
                cancellationToken);

            if (state == null)
            {
                throw new WorkloadException(
                    $"Expected server state information missing. The server did not return state indicating the details for the Memcached server(s) running.",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            return state.Definition;
        }
    }
}
