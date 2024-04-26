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
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// CPS(Connections Per Second) Tool Client Executor. 
    /// </summary>
    [WindowsCompatible]
    [UnixCompatible]
    public class CPSExecutor : NetworkingWorkloadToolExecutor
    {
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="CPSClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public CPSExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.ProcessStartRetryPolicy = Policy.Handle<Exception>(exc => exc.Message.Contains("sockwiz")).Or<VirtualClientException>()
                .WaitAndRetryAsync(5, retries => TimeSpan.FromSeconds(retries * 3));

            this.Parameters.SetIfNotDefined(nameof(this.ConnectionDuration), 0);
            this.Parameters.SetIfNotDefined(nameof(this.DataTransferMode), 1);
            this.Parameters.SetIfNotDefined(nameof(this.DisplayInterval), 10);
            this.Parameters.SetIfNotDefined(nameof(this.ConnectionsPerThread), 100);
            this.Parameters.SetIfNotDefined(nameof(this.MaxPendingRequestsPerThread), 100);
            this.Parameters.SetIfNotDefined(nameof(this.Port), 7201);
            this.Parameters.SetIfNotDefined(nameof(this.WarmupTime), 8);
            this.Parameters.SetIfNotDefined(nameof(this.DelayTime), 0);
            this.Parameters.SetIfNotDefined(nameof(this.ConfidenceLevel), 99);
        }

        /// <summary>
        /// gets the number of connections value
        /// </summary>
        public int Connections
        {
            get 
            {
                return this.Parameters.GetValue<int>(nameof(this.Connections));
            }
        }

        /// <summary>
        /// The duration (in seconds) for each connection.
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
        /// </summary>
        public int DataTransferMode
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(CPSExecutor.DataTransferMode), 1);
            }
        }

        /// <summary>
        /// The interval (in seconds) to display CPS
        /// </summary>
        public int DisplayInterval
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(CPSExecutor.DisplayInterval), 10);
            }
        }

        /// <summary>
        /// ConnectionsPerThread is only for client/sender role.
        /// The ConnectionsPerThread gets the number of connections in each sender thread.
        /// </summary>
        public int ConnectionsPerThread
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(CPSExecutor.ConnectionsPerThread), 100);
            }
        }

        /// <summary>
        /// </summary>
        public int MaxPendingRequestsPerThread
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(CPSExecutor.MaxPendingRequestsPerThread), 100);
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
                return this.Parameters.GetValue<int>(nameof(this.Port), 7201);
            }
        }

        /// <summary>
        /// Parameter defines the duration (in seconds) for running the CPS workload.
        /// </summary>
        public int TestDuration
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.TestDuration), 60);
            }
        }

        /// <summary>
        /// gets test warmup time values in seconds.
        /// </summary>
        public int WarmupTime
        {
            get 
            {
                return this.Parameters.GetValue<int>(nameof(this.WarmupTime), 8);
            }
        }

        /// <summary>
        /// gets test delay time values in seconds.
        /// </summary>
        public int DelayTime
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.DelayTime), 0);
            }
        }

        /// <summary>
        /// gets the confidence level used for calculating the confidence intervals.
        /// </summary>
        public double ConfidenceLevel
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ConfidenceLevel), 99);
            }
        }

        /// <summary>
        /// The retry policy to apply to the startup of the CPS workload to handle
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
                    $"for one or more of the '{nameof(CPSExecutor)}' steps.",
                    ErrorReason.InvalidProfileDefinition);
            }

            DependencyPath workloadPackage = this.GetDependencyPath(this.PackageName, cancellationToken);

            this.IsInClientRole = this.IsInRole(ClientRole.Client);
            this.IsInServerRole = !this.IsInClientRole;
            this.Role = this.IsInClientRole ? ClientRole.Client : ClientRole.Server;

            // e.g.
            // CPS_T16 Client, CPS_T16 Server
            this.Name = $"{this.Scenario} {this.Role}";
            this.ProcessName = "cps";
            this.Tool = NetworkingWorkloadTool.CPS;

            if (this.Platform == PlatformID.Win32NT)
            {
                this.ExecutablePath = this.Combine(workloadPackage.Path, "cps.exe");
            }
            else if (this.Platform == PlatformID.Unix)
            {
                this.ExecutablePath = this.Combine(workloadPackage.Path, "cps");
            }
            else
            {
                throw new NotSupportedException($"{this.Platform} is not supported");
            }

            // Validating CPS parameters
            if (this.TestDuration <= 0)
            {
                throw new WorkloadException("Test duration cannot be equal or less than zero for CPS workload", ErrorReason.InstructionsNotValid);
            }
            else if (this.WarmupTime >= this.TestDuration)
            {
                throw new WorkloadException("WarmpUp time must be less than test duration for CPS workload", ErrorReason.InstructionsNotValid);
            }
            else if (this.DelayTime >= this.TestDuration)
            {
                throw new WorkloadException("Delay time must be less than test duration for CPS workload", ErrorReason.InstructionsNotValid);
            }
            else if ((this.DelayTime + this.WarmupTime) >= this.TestDuration)
            {
                throw new WorkloadException("Sum of delay time and WarmUp time must be less than test duration for CPS workload", ErrorReason.InstructionsNotValid);
            }

            return this.SystemManagement.MakeFileExecutableAsync(this.ExecutablePath, this.Platform, cancellationToken);
        }

        /// <summary>
        /// Returns the CPS command line arguments.
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
                                this.CleanupTasks.Add(() => process.SafeKill());
                                await process.StartAndWaitAsync(cancellationToken, timeout);
                                await this.LogProcessDetailsAsync(process, relatedContext, "CPS");
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
                                process.SafeKill();

                                throw new WorkloadException($"CPS workload did not exit within the timeout period defined (timeout={timeout}).", exc, ErrorReason.WorkloadFailed);
                            }
                            catch (Exception exc)
                            {
                                this.Logger.LogMessage($"{this.GetType().Name}.WorkloadStartupError", LogLevel.Warning, relatedContext.AddError(exc));
                                throw new WorkloadException($"CPS workload failed to start successfully", exc, ErrorReason.WorkloadFailed);
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

                MetricsParser parser = new CPSMetricsParser(results, this.ConfidenceLevel, this.WarmupTime);
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
                    $"Workload results missing. The CPS workload did not produce valid results.",
                    ErrorReason.WorkloadResultsNotFound);
            }
        }
    }
}
