// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client;
    using Polly;
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

        private List<Metric> aggregatedMetrics = new List<Metric>();

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
        /// Parameter defines true/false whether the client action should also emit metric per each server process separately.
        /// Default value is false and collects only aggregate metrics across all the redis/memcached server processes running in the system.
        /// </summary>
        public bool PerProcessMetric
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.PerProcessMetric), false);
            }
        }

        /// <summary>
        /// yes if TLS is enabled.
        /// </summary>
        public string IsTLSEnabled
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.IsTLSEnabled), "no");
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

            if (string.Equals(this.IsTLSEnabled, "yes", StringComparison.OrdinalIgnoreCase))
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
            bool isSupported = base.IsSupported()
                && (this.Platform == PlatformID.Unix)
                && (this.CpuArchitecture == Architecture.X64 || this.CpuArchitecture == Architecture.Arm64);

            if (!isSupported)
            {
                this.Logger.LogNotSupported("MemtierBenchmark", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }
        
        private void AggregateAndCapturePerProcessMetrics(string results, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                results.ThrowIfNullOrWhiteSpace(nameof(results));

                try
                {
                    this.MetadataContract.AddForScenario(
                        "Memtier",
                        commandArguments,
                        toolVersion: null);

                    this.MetadataContract.Apply(telemetryContext);

                    // The Memtier workloads run multi-threaded. The lock is meant to ensure we do not have
                    // race conditions that affect the parsing of the results.
                    lock (this.lockObject)
                    {
                        MemtierMetricsParser resultsParser = new MemtierMetricsParser(results);
                        IList<Metric> workloadMetrics = resultsParser.Parse();
                        this.aggregatedMetrics.AddRange(workloadMetrics);

                        if (this.PerProcessMetric)
                        {
                            this.Logger.LogMetrics(
                            $"Memtier-{this.Benchmark}",
                            this.Scenario,
                            startTime,
                            endTime,
                            workloadMetrics,
                            null,
                            commandArguments,
                            this.Tags,
                            telemetryContext);
                        }

                    }
                }
                catch (SchemaException exc)
                {
                    throw new WorkloadResultsException($"Failed to parse workload results file.", exc, ErrorReason.WorkloadResultsParsingFailed);
                }
            }
        }

        private void CaptureAggregateMetrics(string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    this.MetadataContract.AddForScenario(
                        "Memtier",
                        commandArguments,
                        toolVersion: null);

                    this.MetadataContract.Apply(telemetryContext);

                    Dictionary<string, List<double>> metricNameValueListDict = new Dictionary<string, List<double>>();
                    Dictionary<string, List<Metric>> metricNameMetricsListDict = new Dictionary<string, List<Metric>>();

                    foreach (var metric in this.aggregatedMetrics)
                    {
                        if (metricNameMetricsListDict.ContainsKey(metric.Name))
                        {
                            metricNameValueListDict[metric.Name].Add(metric.Value);
                        }
                        else
                        {
                            metricNameValueListDict.Add(metric.Name, new List<double>() { metric.Value });
                        }

                        if (metricNameMetricsListDict.ContainsKey(metric.Name))
                        {
                            metricNameMetricsListDict[metric.Name].Add(metric);
                        }
                        else
                        {
                            metricNameMetricsListDict.Add(metric.Name, new List<Metric>() { metric });
                        }
                    }

                    List<Metric> newAggregateListOfMetrics = new List<Metric>();

                    foreach (var metricKeyValuePair in metricNameValueListDict)
                    {
                        double avgValue = metricKeyValuePair.Value.Average();
                        double minValue = metricKeyValuePair.Value.Min();
                        double maxValue = metricKeyValuePair.Value.Max();
                        double stdevValue = Math.Sqrt(metricKeyValuePair.Value.Select(x => Math.Pow(x - avgValue, 2)).Average());
                        List<double> sortedValues = metricKeyValuePair.Value.OrderBy(x => x).ToList();
                        double p80Value = sortedValues[(int)Math.Ceiling(sortedValues.Count * 0.8) - 1];

                        newAggregateListOfMetrics.Add(new Metric($"{metricKeyValuePair.Key}_Avg", avgValue, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Unit, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Relativity));
                        newAggregateListOfMetrics.Add(new Metric($"{metricKeyValuePair.Key}_Min", minValue, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Unit, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Relativity));
                        newAggregateListOfMetrics.Add(new Metric($"{metricKeyValuePair.Key}_Max", maxValue, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Unit, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Relativity));
                        newAggregateListOfMetrics.Add(new Metric($"{metricKeyValuePair.Key}_Stdev", stdevValue, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Unit, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Relativity));
                        newAggregateListOfMetrics.Add(new Metric($"{metricKeyValuePair.Key}_P80", p80Value, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Unit, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Relativity));
                        if (metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Unit != MetricUnit.Milliseconds)
                        {
                            // For throughput and bandwidth related metrics only.
                            double sumValue = metricKeyValuePair.Value.Sum();
                            newAggregateListOfMetrics.Add(new Metric($"{metricKeyValuePair.Key}_Sum", sumValue, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Unit, metricNameMetricsListDict[metricKeyValuePair.Key].ElementAt(0).Relativity));
                        }                        
                    }

                    this.Logger.LogMetrics(
                        $"Memtier-{this.Benchmark}",
                        this.Scenario,
                        startTime,
                        endTime,
                        newAggregateListOfMetrics,
                        null,
                        commandArguments,
                        this.Tags,
                        telemetryContext);
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

                    foreach (int serverPort in serverState.Ports)
                    {
                        for (int i = 0; i < this.ClientInstances; i++)
                        {
                            // memtier_benchmark Documentation:
                            // https://github.com/RedisLabs/memtier_benchmark

                            if (string.Equals(this.IsTLSEnabled, "yes", StringComparison.OrdinalIgnoreCase))
                            {
                                commandArguments = $"--server {serverIPAddress} --port {serverPort} --tls --cert {this.PlatformSpecifics.Combine(this.RedisResourcesPath, "redis.crt")}  --key {this.PlatformSpecifics.Combine(this.RedisResourcesPath, "redis.key")} --cacert {this.PlatformSpecifics.Combine(this.RedisResourcesPath, "ca.crt")} {this.CommandLine}";
                            }
                            else
                            {
                                commandArguments = $"--server {serverIPAddress} --port {serverPort} {this.CommandLine}";
                            }

                            commands.Add(commandArguments);
                            workloadProcesses.Add(this.ExecuteWorkloadAsync(serverPort, command, commandArguments, workingDirectory, relatedContext.Clone(), cancellationToken));

                            if (this.WarmUp)
                            {
                                // We run ONLY 1 client workload per server endpoint/port when warming up.
                                break;
                            }
                        }
                    }

                    await Task.WhenAll(workloadProcesses);
                    DateTime endTime = DateTime.UtcNow;

                    this.CaptureAggregateMetrics(commandArguments, startTime, endTime, telemetryContext, cancellationToken);
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
                                ConsoleLogger.Default.LogMessage($"Memtier benchmark process exited (server port = {serverPort})...", telemetryContext);

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
                                    string parsedCommandArguments = this.ParseCommand(process.FullCommand());
                                    this.AggregateAndCapturePerProcessMetrics(output, parsedCommandArguments, startTime, DateTime.UtcNow, telemetryContext, cancellationToken);
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
    }
}
