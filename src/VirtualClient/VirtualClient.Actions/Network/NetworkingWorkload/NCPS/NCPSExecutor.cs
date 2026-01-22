// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// NCPS(New Connections Per Second) Tool Executor. 
    /// </summary>
    public class NCPSExecutor : NetworkingWorkloadToolExecutor
    {
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="NCPSExecutor"/> class.
        /// </summary>
        /// <param name="component">Component to copy.</param>
        public NCPSExecutor(VirtualClientComponent component)
           : base(component)
        {
            this.ProcessStartRetryPolicy = Policy.Handle<Exception>(exc => exc.Message.Contains("sockwiz")).Or<VirtualClientException>()
                .WaitAndRetryAsync(5, retries => TimeSpan.FromSeconds(retries * 3));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NCPSExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NCPSExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.ProcessStartRetryPolicy = Policy.Handle<Exception>(exc => exc.Message.Contains("sockwiz")).Or<VirtualClientException>()
                .WaitAndRetryAsync(5, retries => TimeSpan.FromSeconds(retries * 3));
        }

        /// <summary>
        /// The thread count for the workload.
        /// </summary>
        public int ThreadCount
        {
            get 
            {
                return this.Parameters.GetValue<int>(nameof(this.ThreadCount), 16);
            }
        }

        /// <summary>
        /// The total number of connections to keep open (client-side only).
        /// </summary>
        public int TotalConnectionsToOpen
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.TotalConnectionsToOpen), this.ThreadCount * 100);
            }
        }

        /// <summary>
        /// The max number of pending connect requests at any given time (client-side only).
        /// </summary>
        public int MaxPendingRequests
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.MaxPendingRequests), this.TotalConnectionsToOpen);
            }
        }

        /// <summary>
        /// The duration (in milliseconds) for each connection.
        /// </summary>
        public int ConnectionDuration
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ConnectionDuration), 0);
            }
        }

        /// <summary>
        /// The data transfer mode for each connection.
        /// 0: no send/receive, 1: one send/receive, p: ping/pong (continuous send/receive)
        /// s: continuous send, r: continuous receive
        /// </summary>
        public string DataTransferMode
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DataTransferMode), "1");
            }
        }

        /// <summary>
        /// The interval (in seconds) to display NCPS stats.
        /// </summary>
        public int DisplayInterval
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(NCPSExecutor.DisplayInterval), 1);
            }
        }

        /// <summary>
        /// The starting port for the range of ports that will be used for client/server 
        /// network connections.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port), 9800);
            }
        }

        /// <summary>
        /// Number of TCP ports to listen on or connect to.
        /// </summary>
        public int PortCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.PortCount), this.ThreadCount);
            }
        }

        /// <summary>
        /// Parameter defines the duration for running the NCPS workload.
        /// Note: NCPS -t parameter includes warmup time specified by -wt parameter.
        /// </summary>
        public TimeSpan TestDuration
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.TestDuration), TimeSpan.FromSeconds(60));
            }
        }

        /// <summary>
        /// Gets test warmup time values.
        /// </summary>
        public TimeSpan WarmupTime
        {
            get 
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.WarmupTime), TimeSpan.FromSeconds(5));
            }
        }

        /// <summary>
        /// Gets test delay time values.
        /// </summary>
        public TimeSpan DelayTime
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.DelayTime), TimeSpan.Zero);
            }
        }

        /// <summary>
        /// Gets the confidence level used for calculating the confidence intervals.
        /// </summary>
        public double ConfidenceLevel
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ConfidenceLevel), 99);
            }
        }

        /// <summary>
        /// Gets additional optional parameters.
        /// </summary>
        public string AdditionalParams
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.AdditionalParams), string.Empty);
            }
        }

        /// <summary>
        /// The retry policy to apply to the startup of the NCPS workload to handle
        /// transient issues.
        /// </summary>
        protected IAsyncPolicy ProcessStartRetryPolicy { get; set; }

        /// <summary>
        /// Initializes the environment and dependencies for running the tool.
        /// </summary>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(this.Scenario))
            {
                throw new WorkloadException(
                    $"Scenario parameter missing. The profile supplied is missing the required '{nameof(this.Scenario)}' parameter " +
                    $"for one or more of the '{nameof(NCPSExecutor)}' steps.",
                    ErrorReason.InvalidProfileDefinition);
            }

            DependencyPath workloadPackage = this.GetDependencyPath(this.PackageName, cancellationToken);

            this.IsInClientRole = this.IsInRole(ClientRole.Client);
            this.IsInServerRole = !this.IsInClientRole;
            this.Role = this.IsInClientRole ? ClientRole.Client : ClientRole.Server;

            // e.g.
            // NCPS_T16 Client, NCPS_T16 Server
            this.Name = $"{this.Scenario} {this.Role}";
            this.ProcessName = "ncps";
            this.Tool = NetworkingWorkloadTool.NCPS;

            if (this.Platform == PlatformID.Win32NT)
            {
                this.ExecutablePath = this.Combine(workloadPackage.Path, "ncps.exe");
            }
            else if (this.Platform == PlatformID.Unix)
            {
                this.ExecutablePath = this.Combine(workloadPackage.Path, "ncps");
            }
            else
            {
                throw new NotSupportedException($"{this.Platform} is not supported");
            }

            // Validating NCPS parameters
            if (this.TestDuration <= TimeSpan.Zero)
            {
                throw new WorkloadException("Test duration cannot be equal or less than zero for NCPS workload", ErrorReason.InstructionsNotValid);
            }
            else if (this.WarmupTime >= this.TestDuration)
            {
                throw new WorkloadException("WarmUp time must be less than test duration for NCPS workload", ErrorReason.InstructionsNotValid);
            }
            else if (this.DelayTime >= this.TestDuration)
            {
                throw new WorkloadException("Delay time must be less than test duration for NCPS workload", ErrorReason.InstructionsNotValid);
            }
            else if ((this.DelayTime + this.WarmupTime) >= this.TestDuration)
            {
                throw new WorkloadException("Sum of delay time and WarmUp time must be less than test duration for NCPS workload", ErrorReason.InstructionsNotValid);
            }

            return this.SystemManagement.MakeFileExecutableAsync(this.ExecutablePath, this.Platform, cancellationToken);
        }

        /// <summary>
        /// Returns the NCPS command line arguments.
        /// </summary>
        protected override string GetCommandLineArguments()
        {
            return null;
        }

        /// <inheritdoc/>
        protected override Task<IProcessProxy> ExecuteWorkloadAsync(string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            IProcessProxy process = null;

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("command", this.ExecutablePath)
                .AddContext("commandArguments", commandArguments);

            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", relatedContext, async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    await this.ProcessStartRetryPolicy.ExecuteAsync(async () =>
                    {
                        using (process = this.SystemManagement.ProcessManager.CreateProcess(this.ExecutablePath, commandArguments))
                        {
                            try
                            {
                                this.CleanupTasks.Add(() => process.SafeKill(this.Logger));
                                await process.StartAndWaitAsync(cancellationToken, timeout, withExitConfirmation: true);
                                await this.LogProcessDetailsAsync(process, relatedContext, "NCPS");
                                process.ThrowIfWorkloadFailed();

                                this.CaptureMetrics(
                                    process.StandardOutput.ToString(),
                                    process.FullCommand(),
                                    process.StartTime,
                                    process.ExitTime,
                                    relatedContext);
                            }
                            catch (OperationCanceledException)
                            {
                                // Expected when the client signals a cancellation.
                            }
                            catch (TimeoutException exc)
                            {
                                // We give this a best effort but do not want it to prevent the next workload
                                // from executing.
                                this.Logger.LogMessage($"{this.GetType().Name}.WorkloadTimeout", LogLevel.Warning, relatedContext.AddError(exc));
                                process.SafeKill(this.Logger);

                                throw new WorkloadException($"NCPS workload did not exit within the timeout period defined (timeout={timeout}).", exc, ErrorReason.WorkloadFailed);
                            }
                            catch (Exception exc)
                            {
                                this.Logger.LogMessage($"{this.GetType().Name}.WorkloadStartupError", LogLevel.Warning, relatedContext.AddError(exc));
                                throw new WorkloadException($"NCPS workload failed to start successfully", exc, ErrorReason.WorkloadFailed);
                            }
                        }
                    });
                }

                return process;
            });
        }

        /// <summary>
        /// Logs the workload metrics to the telemetry.
        /// </summary>
        protected override void CaptureMetrics(string results, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            if (!string.IsNullOrWhiteSpace(results))
            {
                this.MetadataContract.AddForScenario(
                    this.Tool.ToString(),
                    commandArguments,
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                MetricsParser parser = new NCPSMetricsParser(results, this.ConfidenceLevel, this.WarmupTime.TotalSeconds);
                IList<Metric> metrics = parser.Parse();

                this.Logger.LogMetrics(
                    this.Tool.ToString(),
                    this.Name,
                    startTime,
                    endTime,
                    metrics,
                    string.Empty,
                    commandArguments,
                    this.Tags,
                    telemetryContext);
            }
            else
            {
                throw new WorkloadException(
                    $"Workload results missing. The NCPS workload did not produce valid results.",
                    ErrorReason.WorkloadResultsNotFound);
            }
        }
    }
}
