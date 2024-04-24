// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Actions.Memtier;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;
    using LogLevel = Microsoft.Extensions.Logging.LogLevel;

    /// <summary>
    /// Redis/Memcached Memtier Client Executor.
    /// </summary>
    public class MemtierBenchmarkClientExecutor : MemcachedExecutor
    {
        private readonly object lockObject = new object();
        private static readonly Regex ProtocolExpression = new Regex(@"--protocol[=\s]*([a-z0-9_]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly List<Regex> StandardErrorExpressions = new List<Regex>
        {
            new Regex(@"connection\s+dropped", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"connection\s+refused", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };

        private List<ProcessOutputDescription> processOutputDescriptions;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemtierBenchmarkClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public MemtierBenchmarkClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
                .WaitAndRetryAsync(3, (retries) => TimeSpan.FromSeconds(retries * 2));

            this.ClientRetryPolicy = Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
                .WaitAndRetryAsync(3, (retries) => TimeSpan.FromSeconds(retries));

            this.PollingTimeout = TimeSpan.FromMinutes(40);

            // Ensure the duration is in integer (seconds) form.
            int duration = this.Duration;
            this.Parameters[nameof(this.Duration)] = duration;
            this.processOutputDescriptions = new List<ProcessOutputDescription>();
        }

        /// <summary>
        /// Parameter defines the number of Memtier benchmark instances to execute against
        /// each server instance. Default = 1.
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
        public int Duration
        {
            get
            {
                // The command line expects the value in Seconds. We allow the user to use either
                // an integer or timespan format in the profile.
                TimeSpan duration = this.Parameters.GetTimeSpanValue(nameof(this.Duration), TimeSpan.FromSeconds(60));
                return (int)duration.TotalSeconds;
            }
        }

        /// <summary>
        /// True/false whether the Memtier benchmark should emit metric aggregations (e.g. min, max, avg)
        /// for the metrics captured. Default = false.
        /// </summary>
        public bool EmitAggregateMetrics
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.EmitAggregateMetrics), false);
            }
        }

        /// <summary>
        /// True/false whether the Memtier benchmark should emit raw metric values parsed
        /// from the workload output. Default = true.
        /// </summary>
        public bool EmitRawMetrics
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.EmitRawMetrics), true);
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
        /// True/false whether TLS should be enabled. Default = false.
        /// </summary>
        public bool IsTLSEnabled
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.IsTLSEnabled), false);
            }
        }

        /// <summary>
        /// Parameter defines the number of server instances/copies to run.
        /// </summary>
        public string RedisResourcesPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.RedisResourcesPackageName));
            }
        }

        /// <summary>
        /// The benchmark target server (e.g. Redis, Memcached).
        /// </summary>
        protected string Benchmark { get; private set; }

        /// <summary>
        /// The retry policy to apply to each Memtier workload instance when trying to startup
        /// against a target server.
        /// </summary>
        protected IAsyncPolicy ClientRetryPolicy { get; set; }

        /// <summary>
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        protected IAsyncPolicy ClientFlowRetryPolicy { get; set; }

        /// <summary>
        /// True/false whether the Memcached/Redis server instance has been warmed up.
        /// </summary>
        protected bool IsServerWarmedUp { get; set; }

        /// <summary>
        /// Path to memtier benchmark executable (e.g. memtier_benchmark).
        /// </summary>
        protected string MemtierExecutablePath { get; set; }

        /// <summary>
        /// Path to Memtier Package.
        /// </summary>
        protected string MemtierPackagePath { get; set; }

        /// <summary>
        /// Path to Redis resources.
        /// </summary>
        protected string RedisResourcesPath { get; set; }

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

                                // 3) Get server-side state/details
                                // ===========================================================================
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
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken);
            DependencyPath memtierPackage = await this.GetPackageAsync(this.PackageName, CancellationToken.None);

            this.MemtierPackagePath = memtierPackage.Path;
            this.MemtierExecutablePath = this.PlatformSpecifics.Combine(this.MemtierPackagePath, "memtier_benchmark");

            Match protocol = MemtierBenchmarkClientExecutor.ProtocolExpression.Match(this.CommandLine);
            if (protocol.Groups[1].Value.Trim().ToLowerInvariant().StartsWith("mem"))
            {
                this.Benchmark = "Memcached";
            }
            else
            {
                this.Benchmark = "Redis";
            }

            if (this.IsTLSEnabled)
            {
                DependencyPath redisResourcesPath = await this.GetPackageAsync(this.RedisResourcesPackageName, cancellationToken);
                this.RedisResourcesPath = redisResourcesPath.Path;
            }

            await this.SystemManagement.MakeFileExecutableAsync(this.MemtierExecutablePath, this.Platform, cancellationToken);
            this.InitializeApiClients();
        }

        /// <summary>
        /// Validates the component parameters and dependencies for requirements.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            Match protocol = MemtierBenchmarkClientExecutor.ProtocolExpression.Match(this.CommandLine);

            if (!protocol.Success)
            {
                throw new WorkloadException(
                    $"The target server protocol must be defined in the '{nameof(this.CommandLine)}' parameter for the component (e.g. --protocol memcache_text). " +
                    $"See the documentation on Memtier for supported protocols.",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            if (base.IsSupported())
            {
                bool isSupported = (this.Platform == PlatformID.Unix)
                && (this.CpuArchitecture == Architecture.X64 || this.CpuArchitecture == Architecture.Arm64);

                if (!isSupported)
                {
                    this.Logger.LogNotSupported("MemtierBenchmark", this.Platform, this.CpuArchitecture, EventContext.Persisted());
                }

                return isSupported;
            }
            else
            {
                return false;
            }
            
        }

        private void CaptureMetrics(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested && this.processOutputDescriptions?.Any() == true)
            {
                try
                {
                    this.MetadataContract.AddForScenario(
                        "Memtier",
                        null,
                        toolVersion: null);

                    this.MetadataContract.Apply(telemetryContext);
                    List<Metric> allMetrics = new List<Metric>();

                    foreach (ProcessOutputDescription processInfo in this.processOutputDescriptions)
                    {
                        MemtierMetricsParser memtierMetricsAggregateParser = new MemtierMetricsParser(processInfo.Output);
                        IList<Metric> metrics = memtierMetricsAggregateParser.Parse();
                        allMetrics.AddRange(metrics);

                        if (this.EmitRawMetrics)
                        {
                            IDictionary<string, IConvertible> metadata = MemtierMetricsParser.ParseMetadata(processInfo.Command, processInfo.CpuAffinity);

                            foreach (Metric metric in metrics)
                            {
                                this.Logger.LogMetrics(
                                    $"Memtier-{this.Benchmark}",
                                    this.MetricScenario ?? this.Scenario,
                                    processInfo.StartTime,
                                    processInfo.EndTime,
                                    metric.Name,
                                    metric.Value,
                                    metric.Unit,
                                    null,
                                    processInfo.Command,
                                    this.Tags,
                                    telemetryContext,
                                    metric.Relativity,
                                    metricMetadata: metadata);
                            }
                        }
                    }

                    if (this.EmitAggregateMetrics)
                    {
                        ProcessOutputDescription processReference = this.processOutputDescriptions.First();
                        IList<Metric> aggregateMetrics = MemtierMetricsParser.Aggregate(allMetrics);
                        IDictionary<string, IConvertible> metadata = MemtierMetricsParser.ParseMetadata(processReference.Command);

                        foreach (Metric metric in aggregateMetrics)
                        {
                            this.Logger.LogMetrics(
                                $"Memtier-{this.Benchmark}",
                                this.MetricScenario ?? this.Scenario,
                                processReference.StartTime,
                                processReference.EndTime,
                                metric.Name,
                                metric.Value,
                                metric.Unit,
                                null,
                                processReference.Command,
                                this.Tags,
                                telemetryContext,
                                metric.Relativity,
                                metricMetadata: metadata);
                        }
                    }
                }
                catch (SchemaException exc)
                {
                    throw new WorkloadResultsException($"Failed to aggregate workload results.", exc, ErrorReason.WorkloadResultsParsingFailed);
                }
            }
        }

        private Task ExecuteWorkloadsAsync(IPAddress serverIPAddress, ServerState serverState, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone()
                .AddContext(nameof(serverIPAddress), serverIPAddress.ToString())
                .AddContext("serverPorts", serverState.Ports);

            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkloads", relatedContext.Clone(), async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    string command = this.MemtierExecutablePath;
                    string workingDirectory = this.MemtierPackagePath;
                    string commandArguments = string.Empty;
                    List<string> commands = new List<string>();

                    relatedContext.AddContext("command", command);
                    relatedContext.AddContext("commandArguments", commands);
                    relatedContext.AddContext("workingDirectory", workingDirectory);

                    List<Task> workloadProcesses = new List<Task>();
                    DateTime startTime = DateTime.UtcNow;

                    for (int i = 0; i < serverState.Ports.Count(); i++)
                    {
                        PortDescription portDescription = serverState.Ports.ElementAt(i);
                        int serverPort = portDescription.Port;

                        for (int instances = 0; instances < this.ClientInstances; instances++)
                        {
                            // memtier_benchmark Documentation:
                            // https://github.com/RedisLabs/memtier_benchmark

                            if (this.IsTLSEnabled)
                            {
                                commandArguments = $"--server {serverIPAddress} --port {serverPort} --tls --cert {this.PlatformSpecifics.Combine(this.RedisResourcesPath, "redis.crt")}  --key {this.PlatformSpecifics.Combine(this.RedisResourcesPath, "redis.key")} --cacert {this.PlatformSpecifics.Combine(this.RedisResourcesPath, "ca.crt")} {this.CommandLine}";
                            }
                            else
                            {
                                commandArguments = $"--server {serverIPAddress} --port {serverPort} {this.CommandLine}";
                            }

                            commands.Add(commandArguments);
                            workloadProcesses.Add(this.ExecuteWorkloadAsync(portDescription, command, commandArguments, workingDirectory, relatedContext.Clone(), cancellationToken));

                            if (this.WarmUp)
                            {
                                // We run ONLY 1 client workload per server endpoint/port when warming up.
                                break;
                            }
                        }
                    }

                    await Task.WhenAll(workloadProcesses);

                    if (!this.WarmUp)
                    {
                        this.CaptureMetrics(telemetryContext, cancellationToken);
                    }
                }
            });
        }

        private async Task ExecuteWorkloadAsync(PortDescription serverPort, string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
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
                                await this.LogProcessDetailsAsync(process, telemetryContext, "Memtier", logToFile: true);
                                process.ThrowIfWorkloadFailed(MemcachedExecutor.SuccessExitCodes);

                                // The Memtier workload for whatever reason emits the following statement in standard error:
                                // 'Writing results to stdout'. We will throw if there is any other information in standard error. Certain
                                // other issues are reported in standard out even though the exit code of 0 (success) is returned. Not the best
                                // choices for a benchmark, but handling these here.
                                if (process.StandardError.Length > 0)
                                {
                                    process.ThrowOnStandardError<WorkloadException>(
                                        errorReason: ErrorReason.WorkloadFailed,
                                        expressions: MemtierBenchmarkClientExecutor.StandardErrorExpressions.ToArray());
                                }

                                // We don't capture metrics on warm up operations.
                                if (!this.WarmUp)
                                {
                                    string output = process.StandardOutput.ToString();
                                    this.processOutputDescriptions.Add(new ProcessOutputDescription
                                    {
                                        Command = this.ParseCommand(process.FullCommand()),
                                        CpuAffinity = serverPort.CpuAffinity,
                                        EndTime = DateTime.UtcNow,
                                        Output = output,
                                        StartTime = startTime
                                    });
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

        private string ParseCommand(string command)
        {
            List<string> excludingRegexList = new List<string>
                {
                    @".*\/memtier_benchmark",
                    @"--port\s+\d+",
                    @"--key-prefix\s+\w+",
                    @"--key-prefix\s+\w+", 
                    @"--print-percentiles\s+(?:\d{1,2}(?:\.\d+)?(?:,\d{1,2}(?:\.\d+)?)*)+", 
                    @"--cert\s+.*\.crt",
                    @"--key\s+.*\.key",
                    @"--cacert\s+.*\.crt",
                    @"--server\s+[\d.]+",
                };
            foreach (string regexPattern in excludingRegexList)
            {
                command = Regex.Replace(command, regexPattern, string.Empty);
            }

            command = Regex.Replace(command, @"\s+", " "); // Removes extra spaces

            return command.Trim();
        }

        private class ProcessOutputDescription
        {
            /// <summary>
            /// The Memtier command line used to start the process.
            /// </summary>
            public string Command { get; set; }

            /// <summary>
            /// The CPU affinity provided to the client by the server. Redis servers
            /// for example will be often bound to a single logical processor instance and Memcached
            /// to all logical processors.
            /// </summary>
            public string CpuAffinity { get; set; }

            /// <summary>
            /// The time at which the Memtier workload completed execution.
            /// </summary>
            public DateTime EndTime { get; set; }

            /// <summary>
            /// The standard output of the Memtier workload from which metrics will
            /// be parsed.
            /// </summary>
            public string Output { get; set; }

            /// <summary>
            /// The time at which the Memtier workload began execution.
            /// </summary>
            public DateTime StartTime { get; set; }
        }
    }
}
