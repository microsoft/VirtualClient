using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using VirtualClient.Common;
using VirtualClient.Common.Extensions;
using VirtualClient.Common.Telemetry;
using VirtualClient.Contracts;
using VirtualClient.Contracts.Metadata;

namespace VirtualClient.Actions.Kafka
{
    internal enum KafkaCommandType
    {
        Setup,
        ProducerTest,
        ConsumerTest
    }

    /// <summary>
    /// Kafka client executor.
    /// </summary>
    public class KafkaClientExecutor : KafkaExecutor
    {
        private readonly object lockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public KafkaClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
                .WaitAndRetryAsync(3, (retries) => TimeSpan.FromSeconds(retries * 2));

            this.ClientRetryPolicy = Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
                .WaitAndRetryAsync(3, (retries) => TimeSpan.FromSeconds(retries));

            this.PollingTimeout = TimeSpan.FromMinutes(40);
        }

        /// <summary>
        /// Parameter defines the kafka command line to execute.
        /// </summary>
        public string CommandLine { get; set; }

        /// <summary>
        /// Port on which server runs.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port));
            }
        }

        /// <summary>
        /// Parameter defines true/false whether the action is meant to warm up the server.
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
        /// The timespan at which the client will poll the server for responses before
        /// timing out.
        /// </summary>
        protected TimeSpan PollingTimeout { get; set; }

        /// <summary>
        /// Path to Kafka topic executable.
        /// </summary>
        protected string KafkaTopicScriptPath { get; set; }

        /// <summary>
        /// Path to Kafka producer performance test executable.
        /// </summary>
        protected string KafkProducerPerfScriptPath { get; set; }

        /// <summary>
        /// Path to Kafka consumer performance test executable.
        /// </summary>
        protected string KafkaConsumerPerfScriptPath { get; set; }

        /// <summary>
        /// Path to Kafka command executable.
        /// </summary>
        protected string KafkaCommandScriptPath { get; set; }

        /// <summary>
        /// True/false whether the Producer test has ran once
        /// </summary>
        protected bool IsServerWarmedUp { get; set; }

        /// <summary>
        /// Parameter defines the kafka command type to decide which exe to run.
        /// </summary>
        private string CommandType
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CommandType));
            }
        }

        /// <summary>
        /// Validates the component definition for requirements.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            if (!Enum.IsDefined(typeof(KafkaCommandType), this.CommandType))
            {
                throw new ArgumentException($"Parameter CommandType should be one of ${KafkaCommandType.Setup}, ${KafkaCommandType.ProducerTest} or ${KafkaCommandType.ConsumerTest}");
            }
        }

        /// <summary>
        /// Initializes the environment and dependencies for client of kafka Benchmark workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    this.KafkaTopicScriptPath = this.Combine(this.KafkaPackagePath, "bin", "windows", "kafka-topics.bat");
                    this.KafkProducerPerfScriptPath = this.Combine(this.KafkaPackagePath, "bin", "windows", "kafka-producer-perf-test.bat");
                    this.KafkaConsumerPerfScriptPath = this.Combine(this.KafkaPackagePath, "bin", "windows", "kafka-consumer-perf-test.bat");
                    break;

                case PlatformID.Unix:
                    this.KafkaTopicScriptPath = this.Combine(this.KafkaPackagePath, "bin", "kafka-topics.sh");
                    this.KafkProducerPerfScriptPath = this.Combine(this.KafkaPackagePath, "bin", "kafka-producer-perf-test.sh");
                    this.KafkaConsumerPerfScriptPath = this.Combine(this.KafkaPackagePath, "bin", "windows", "kafka-consumer-perf-test.bat");
                    break;
            }

            this.CommandLine = this.Parameters.GetValue<string>(nameof(this.CommandLine));
            Enum.TryParse(this.CommandType, out KafkaCommandType kafkaCommandType);

            switch (kafkaCommandType)
            {
                case KafkaCommandType.Setup:
                    this.KafkaCommandScriptPath = this.KafkaTopicScriptPath;
                    break;
                case KafkaCommandType.ProducerTest:
                    this.KafkaCommandScriptPath = this.KafkProducerPerfScriptPath;
                    break;
                case KafkaCommandType.ConsumerTest:
                    this.KafkaCommandScriptPath = this.KafkaConsumerPerfScriptPath;
                    break;
            }

            await this.SystemManagement.MakeFileExecutableAsync(this.KafkaTopicScriptPath, this.Platform, cancellationToken);
            await this.SystemManagement.MakeFileExecutableAsync(this.KafkProducerPerfScriptPath, this.Platform, cancellationToken);
            await this.SystemManagement.MakeFileExecutableAsync(this.KafkaConsumerPerfScriptPath, this.Platform, cancellationToken);
        }

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
                        this.CommandLine = string.Format(this.CommandLine, server.IPAddress);
                        clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                IApiClient serverApiClient = this.ApiClientManager.GetOrCreateApiClient(server.Name, server);
                                // 1) Confirm server is online.
                                // ===========================================================================
                                this.Logger.LogTraceMessage("Synchronization: Poll server API for heartbeat...");

                                await serverApiClient.PollForHeartbeatAsync(this.PollingTimeout, cancellationToken)
                                    .ConfigureAwait(false);

                                // 2) Confirm the server-side application (e.g. web server) is online.
                                // ===========================================================================
                                this.Logger.LogTraceMessage("Synchronization: Poll server for online signal...");

                                await serverApiClient.PollForServerOnlineAsync(TimeSpan.FromMinutes(10), cancellationToken)
                                    .ConfigureAwait(false);

                                this.Logger.LogTraceMessage("Synchronization: Server online signal confirmed...");
                                this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                                // 3) Execute the client workload.
                                // ===========================================================================
                                ipAddress = IPAddress.Parse(server.IPAddress);
                                await this.ExecuteWorkloadsAsync(ipAddress, telemetryContext, cancellationToken)
                                    .ConfigureAwait(false);
                            }
                        }));
                    }
                }
                else
                {
                    ipAddress = IPAddress.Loopback;
                    this.CommandLine = string.Format(this.CommandLine, "localhost");
                    clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.ExecuteWorkloadsAsync(ipAddress, telemetryContext, cancellationToken).ConfigureAwait(false);
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

        private Task ExecuteWorkloadsAsync(IPAddress serverIPAddress, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("serverIPAddress", serverIPAddress.ToString());

            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkloads", relatedContext.Clone(), async () =>
            {
                List<string> commands = new List<string>();
                relatedContext.AddContext("command", this.PlatformSpecificCommandType);
                relatedContext.AddContext("commandArguments", commands);

                List<Task> workloadProcesses = new List<Task>();
                string commandArguments = this.GetPlatformFormattedCommandArguement(this.KafkaCommandScriptPath, this.CommandLine);
                commands.Add($"{this.PlatformSpecificCommandType} {commandArguments}");

                workloadProcesses.Add(this.ExecuteWorkloadAsync(this.PlatformSpecificCommandType, commandArguments, this.KafkaPackagePath, relatedContext, cancellationToken));

                await Task.WhenAll(workloadProcesses);
            });
        }

        private async Task ExecuteWorkloadAsync(string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                await (this.ClientRetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
                {
                    try
                    {
                        DateTime startTime = DateTime.UtcNow;
                        using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, telemetryContext, cancellationToken, runElevated: true, timeout: TimeSpan.FromMinutes(5)))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                ConsoleLogger.Default.LogMessage($"Kafka benchmark process exited (server port = {this.Port})...", telemetryContext);

                                await this.LogProcessDetailsAsync(process, telemetryContext, "Kafka", logToFile: true);
                                process.ThrowIfWorkloadFailed();

                                if (this.CommandType != KafkaCommandType.Setup.ToString())
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

        private void CaptureMetrics(string output, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    this.MetadataContract.AddForScenario(
                        "Kafka-Benchmark",
                        commandArguments,
                        toolVersion: null);

                    this.MetadataContract.Apply(telemetryContext);

                    // The Kafka workloads run multi-threaded. The lock is meant to ensure we do not have
                    // race conditions that affect the parsing of the results.
                    lock (this.lockObject)
                    {
                        MetricsParser kafkaMetricsParser = new KafkaProducerMetricsParser(output);
                        if (this.CommandType == KafkaCommandType.ConsumerTest.ToString())
                        {
                            kafkaMetricsParser = new KafkaConsumerMetricsParser(output);
                        }

                        IList<Metric> workloadMetrics = kafkaMetricsParser.Parse();

                        this.Logger.LogMetrics(
                            "Kafka-Benchmark",
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
    }
}
