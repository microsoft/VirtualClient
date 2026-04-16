// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO.Abstractions;
    using System.Linq;
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
    /// NTttcp(Test Bandwith and Throughput) Tool Client Executor. 
    /// </summary>
    public class NTttcpExecutor : NetworkingWorkloadToolExecutor
    {
        private const string OutputFileName = "ntttcp-results.xml";
        private const string SendOutputFileName = "ntttcp-results-send.xml";
        private const string ReceiveOutputFileName = "ntttcp-results-recv.xml";
        private const int ReversePortOffset = 100;
        private static readonly TimeSpan DefaultWarmupTime = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan DefaultCooldownTime = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Initializes a new instance of the <see cref="NTttcpExecutor"/> class.
        /// </summary>
        /// <param name="component">Component to copy.</param>
        public NTttcpExecutor(VirtualClientComponent component)
           : base(component)
        {
            this.ProcessStartRetryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retries => TimeSpan.FromSeconds(retries * 3));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NTttcpExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NTttcpExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
            this.ProcessStartRetryPolicy = Policy
                .Handle<Exception>()
                .WaitAndRetryAsync(3, retries => TimeSpan.FromSeconds(retries * 3));
        }

        /// <summary>
        /// Get buffer size value in bytes for Client.(e.g. 4K,64K,1400)
        ///  Where 4K = 4*1024 = 4096 bytes.
        /// </summary>
        public string BufferSizeClient
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.BufferSizeClient)).ToString();
            }
        }

        /// <summary>
        /// Get buffer size value in bytes for Server.(e.g. 4K,64K,1400)
        ///  Where 4K = 4*1024 = 4096 bytes.
        /// </summary>
        public string BufferSizeServer
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.BufferSizeServer)).ToString();
            }
        }

        /// <summary>
        /// get number of concurrent threads to use.
        /// </summary>
        public int ThreadCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ThreadCount), 1);
            }
        }

        /// <summary>
        /// Parameter defines the duration for running the NTttcp workload.
        /// </summary>
        public TimeSpan TestDuration
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.TestDuration), TimeSpan.FromSeconds(60));
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
                return this.Parameters.GetValue<int>(nameof(this.Port), 5001);
            }
        }

        /// <summary>
        /// ReceiverMultiClientMode is only for server/receiver role.
        /// The ReceiverMultiClientMode tells server to work in multi-client mode.
        /// </summary>
        public bool? ReceiverMultiClientMode
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.ReceiverMultiClientMode), out IConvertible receiverMultiClientMode);
                return receiverMultiClientMode?.ToBoolean(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// SenderLastClient is only for client/sender role.
        /// The SenderLastClient indicates that this is the last client when test with multi-client mode.
        /// </summary>
        public bool? SenderLastClient
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.SenderLastClient), out IConvertible senderLastClient);
                return senderLastClient?.ToBoolean(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// ThreadsPerServerPort is only for client/sender role.
        /// The ThreadsPerServerPort gets the number of threads per each server port.
        /// </summary>
        public int? ThreadsPerServerPort
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.ThreadsPerServerPort), out IConvertible threadsPerServerPort);
                return threadsPerServerPort?.ToInt32(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// ConnectionsPerThread is only for client/sender role.
        /// The ConnectionsPerThread gets the number of connections in each sender thread.
        /// </summary>
        public int? ConnectionsPerThread
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.ConnectionsPerThread), out IConvertible connectionsPerThread);
                return connectionsPerThread?.ToInt32(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// DevInterruptsDifferentiator gets the differentiator.
        /// Used for getting number of interrupts for the devices specified by the differentiator.
        /// Examples for differentiator: Hyper-V PCIe MSI, mlx4, Hypervisor callback interrupts,etc.
        /// </summary>
        public string DevInterruptsDifferentiator
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.DevInterruptsDifferentiator), out IConvertible differentiator);
                return differentiator?.ToString();
            }
        }

        /// <summary>
        /// The type of the protocol that should be used for the workload.(e.g. TCP,UDP)
        /// </summary>
        public string Protocol
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Protocol));
            }
        }

        /// <summary>
        /// NoSyncEnabled is only for client/sender role.
        /// The NoSyncEnabled indicates that synchronization is disabled for the client.
        /// </summary>
        public bool? NoSyncEnabled
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.NoSyncEnabled), out IConvertible noSyncEnabled);
                return noSyncEnabled?.ToBoolean(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Duplex mode for the NTttcp workload. Valid values are "Half" (default) and "Full".
        /// In full-duplex mode, each node runs both a sender and receiver process simultaneously.
        /// </summary>
        public string DuplexMode
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.DuplexMode), out IConvertible duplexMode);
                return duplexMode?.ToString();
            }
        }

        /// <summary>
        /// Returns true if the workload is configured for full-duplex mode.
        /// </summary>
        protected bool IsFullDuplex
        {
            get
            {
                return string.Equals(this.DuplexMode, "Full", StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Path to the send-direction results file (used in full-duplex mode).
        /// </summary>
        protected string SendResultsPath { get; set; }

        /// <summary>
        /// Path to the receive-direction results file (used in full-duplex mode).
        /// </summary>
        protected string ReceiveResultsPath { get; set; }

        /// <summary>
        /// The port used for the reverse direction in full-duplex mode.
        /// </summary>
        protected int ReversePort
        {
            get
            {
                return this.Port + NTttcpExecutor.ReversePortOffset;
            }
        }

        /// <summary>
        /// The retry policy to apply to the startup of the NTttcp workload to handle
        /// transient issues.
        /// </summary>
        protected IAsyncPolicy ProcessStartRetryPolicy { get; set; }

        /// <summary>
        /// Initializes the environment and dependencies for running the tool.
        /// </summary>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string protocol = this.Protocol.ToLowerInvariant();
            if (protocol != "tcp" && protocol != "udp")
            {
                throw new NotSupportedException($"The network protocol '{this.Protocol}' is not supported for the NTttcp workload.");
            }

            if (string.IsNullOrWhiteSpace(this.Scenario))
            {
                throw new WorkloadException(
                    $"Scenario parameter missing. The profile supplied is missing the required '{nameof(this.Scenario)}' parameter " +
                    $"for one or more of the '{nameof(NTttcpExecutor)}' steps.",
                    ErrorReason.InvalidProfileDefinition);
            }

            DependencyPath workloadPackage = this.GetDependencyPath(this.PackageName, cancellationToken);

            this.IsInClientRole = this.IsInRole(ClientRole.Client);
            this.IsInServerRole = !this.IsInClientRole;

            this.Role = this.IsInClientRole ? ClientRole.Client : ClientRole.Server;
            this.Name = $"{this.Scenario} {this.Role}";
            this.ProcessName = "ntttcp";
            this.Tool = NetworkingWorkloadTool.NTttcp;
            this.ResultsPath = this.PlatformSpecifics.Combine(workloadPackage.Path, NTttcpExecutor.OutputFileName);
            this.SendResultsPath = this.PlatformSpecifics.Combine(workloadPackage.Path, NTttcpExecutor.SendOutputFileName);
            this.ReceiveResultsPath = this.PlatformSpecifics.Combine(workloadPackage.Path, NTttcpExecutor.ReceiveOutputFileName);

            if (this.Platform == PlatformID.Win32NT)
            {
                this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "NTttcp.exe");
            }
            else if (this.Platform == PlatformID.Unix)
            {
                this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "ntttcp");
            }
            else
            {
                throw new NotSupportedException($"{this.Platform} is not supported");
            }

            return this.SystemManagement.MakeFileExecutableAsync(this.ExecutablePath, this.Platform, cancellationToken);
        }

        /// <summary>
        /// Returns the NTttcp command line arguments.
        /// </summary>
        protected override string GetCommandLineArguments()
        {
            string command = null;
            if (this.Platform == PlatformID.Win32NT)
            {
                command = this.GetWindowsSpecificCommandLine();
            }
            else if (this.Platform == PlatformID.Unix)
            {
                command = this.GetLinuxSpecificCommandLine();
            }

            return command;
        }

        /// <summary>
        /// Gets the Sysctl command output.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        protected Task<string> GetSysctlOutputAsync(CancellationToken cancellationToken)
        {
            string sysctlCommand = "sysctl";
            string sysctlArguments = "net.ipv4.tcp_rmem net.ipv4.tcp_wmem";

            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("command", sysctlCommand)
                .AddContext("commandArguments", sysctlArguments);

            return this.Logger.LogMessageAsync($"{nameof(NTttcpExecutor)}.GetSysctlOutput", telemetryContext, async () =>
            {
                string results = null;
                using (IProcessProxy process = this.SystemManagement.ProcessManager.CreateProcess(sysctlCommand, sysctlArguments))
                {
                    try
                    {
                        await process.StartAndWaitAsync(CancellationToken.None);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "Sysctl", logToFile: true);
                            process.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.DependencyInstallationFailed);

                            results = process.StandardOutput.ToString();
                        }
                    }
                    finally
                    {
                        process.SafeKill(this.Logger);
                    }
                }

                return results;
            });
        }

        /// <inheritdoc/>
        protected override Task ExecuteWorkloadAsync(string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            if (this.IsFullDuplex)
            {
                return this.ExecuteFullDuplexWorkloadAsync(telemetryContext, cancellationToken, timeout);
            }

            return this.ExecuteHalfDuplexWorkloadAsync(commandArguments, telemetryContext, cancellationToken, timeout);
        }

        /// <summary>
        /// Executes the half-duplex (original single-direction) workload.
        /// </summary>
        protected Task ExecuteHalfDuplexWorkloadAsync(string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            EventContext relatedContext = telemetryContext.Clone()
               .AddContext("command", this.ExecutablePath)
               .AddContext("commandArguments", commandArguments);

            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", relatedContext, async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    await this.ProcessStartRetryPolicy.ExecuteAsync(async () =>
                    {
                        await this.DeleteResultsFileAsync();

                        using (IProcessProxy process = this.SystemManagement.ProcessManager.CreateProcess(this.ExecutablePath, commandArguments))
                        {
                            try
                            {
                                await process.StartAndWaitAsync(cancellationToken, timeout);

                                if (process.IsErrored())
                                {
                                    await this.LogProcessDetailsAsync(process, relatedContext, "NTttcp");
                                    process.ThrowIfWorkloadFailed();
                                }
                                else
                                {
                                    string results = await this.WaitForResultsAsync(TimeSpan.FromMinutes(1), relatedContext);
                                    await this.LogProcessDetailsAsync(process, relatedContext, "NTttcp", results: new KeyValuePair<string, string>(this.ResultsPath, results));

                                    this.CaptureMetrics(
                                        results,
                                        process.FullCommand(),
                                        process.StartTime,
                                        process.ExitTime,
                                        relatedContext);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                // Expected when the client signals a cancellation.
                            }
                            catch (TimeoutException exc)
                            {
                                // We give this a best effort but do not want it to prevent the next workload
                                // from executing.
                                this.Logger.LogMessage($"{this.TypeName}.WorkloadTimeout", LogLevel.Warning, relatedContext.AddError(exc));
                            }
                            catch (Exception exc)
                            {
                                this.Logger.LogMessage($"{this.TypeName}.WorkloadStartupError", LogLevel.Warning, relatedContext.AddError(exc));
                                throw;
                            }
                            finally
                            {
                                process.SafeKill(this.Logger);
                            }
                        }
                    });
                }
            });
        }

        /// <summary>
        /// Executes the full-duplex workload — runs both sender and receiver processes concurrently.
        /// The receiver is started first, then after a brief delay the sender is launched.
        /// Both processes run in parallel for the test duration.
        /// </summary>
        protected Task ExecuteFullDuplexWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            string sendCommandArguments = this.GetFullDuplexSendCommandLineArguments();
            string receiveCommandArguments = this.GetFullDuplexReceiveCommandLineArguments();

            EventContext relatedContext = telemetryContext.Clone()
               .AddContext("command", this.ExecutablePath)
               .AddContext("sendCommandArguments", sendCommandArguments)
               .AddContext("receiveCommandArguments", receiveCommandArguments)
               .AddContext("duplexMode", "Full");

            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteFullDuplexWorkload", relatedContext, async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    await this.ProcessStartRetryPolicy.ExecuteAsync(async () =>
                    {
                        await this.DeleteFullDuplexResultsFilesAsync();

                        using (IProcessProxy receiveProcess = this.SystemManagement.ProcessManager.CreateProcess(this.ExecutablePath, receiveCommandArguments))
                        using (IProcessProxy sendProcess = this.SystemManagement.ProcessManager.CreateProcess(this.ExecutablePath, sendCommandArguments))
                        {
                            try
                            {
                                // Start receiver first to ensure it is listening before sender connects.
                                Task receiveTask = receiveProcess.StartAndWaitAsync(cancellationToken, timeout);

                                // Brief delay to allow receiver to bind.
                                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);

                                Task sendTask = sendProcess.StartAndWaitAsync(cancellationToken, timeout);

                                await Task.WhenAll(receiveTask, sendTask).ConfigureAwait(false);

                                // Capture send metrics.
                                if (!sendProcess.IsErrored())
                                {
                                    string sendResults = await this.WaitForResultsAsync(
                                        TimeSpan.FromMinutes(1), relatedContext, this.SendResultsPath);

                                    await this.LogProcessDetailsAsync(
                                        sendProcess,
                                        relatedContext,
                                        "NTttcp",
                                        results: new KeyValuePair<string, string>(this.SendResultsPath, sendResults));

                                    this.CaptureDirectionalMetrics(
                                        sendResults,
                                        sendProcess.FullCommand(),
                                        sendProcess.StartTime,
                                        sendProcess.ExitTime,
                                        "Send",
                                        true,
                                        relatedContext);
                                }
                                else
                                {
                                    await this.LogProcessDetailsAsync(sendProcess, relatedContext, "NTttcp");
                                    this.Logger.LogMessage($"{this.TypeName}.FullDuplexSendFailed", LogLevel.Warning, relatedContext);
                                }

                                // Capture receive metrics.
                                if (!receiveProcess.IsErrored())
                                {
                                    string receiveResults = await this.WaitForResultsAsync(
                                        TimeSpan.FromMinutes(1), relatedContext, this.ReceiveResultsPath);

                                    await this.LogProcessDetailsAsync(
                                        receiveProcess,
                                        relatedContext,
                                        "NTttcp",
                                        results: new KeyValuePair<string, string>(this.ReceiveResultsPath, receiveResults));

                                    this.CaptureDirectionalMetrics(
                                        receiveResults,
                                        receiveProcess.FullCommand(),
                                        receiveProcess.StartTime,
                                        receiveProcess.ExitTime,
                                        "Receive",
                                        false,
                                        relatedContext);
                                }
                                else
                                {
                                    await this.LogProcessDetailsAsync(receiveProcess, relatedContext, "NTttcp");
                                    this.Logger.LogMessage($"{this.TypeName}.FullDuplexReceiveFailed", LogLevel.Warning, relatedContext);
                                }

                                // If both failed, throw.
                                if (sendProcess.IsErrored() && receiveProcess.IsErrored())
                                {
                                    throw new WorkloadException(
                                        $"Both sender and receiver processes failed in full-duplex mode.",
                                        ErrorReason.WorkloadFailed);
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                // Expected when the client signals a cancellation.
                            }
                            catch (TimeoutException exc)
                            {
                                this.Logger.LogMessage($"{this.TypeName}.FullDuplexWorkloadTimeout", LogLevel.Warning, relatedContext.AddError(exc));
                            }
                            catch (WorkloadException)
                            {
                                throw;
                            }
                            catch (Exception exc)
                            {
                                this.Logger.LogMessage($"{this.TypeName}.FullDuplexWorkloadStartupError", LogLevel.Warning, relatedContext.AddError(exc));
                                throw;
                            }
                            finally
                            {
                                sendProcess.SafeKill(this.Logger);
                                receiveProcess.SafeKill(this.Logger);
                            }
                        }
                    });
                }
            });
        }

        /// <summary>
        /// Logs the workload metrics to the telemetry.
        /// </summary>
        protected override void CaptureMetrics(string results, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            this.CaptureDirectionalMetrics(results, commandArguments, startTime, endTime, direction: null, isClientParser: this.IsInClientRole, telemetryContext: telemetryContext);
        }

        /// <summary>
        /// Logs direction-tagged workload metrics to telemetry.
        /// </summary>
        /// <param name="results">The raw XML results from the NTttcp process.</param>
        /// <param name="commandArguments">The command line arguments used.</param>
        /// <param name="startTime">The start time of the process.</param>
        /// <param name="endTime">The end time of the process.</param>
        /// <param name="direction">The direction label (Send, Receive) or null for half-duplex.</param>
        /// <param name="isClientParser">True if parsing sender XML (ntttcps root), false for receiver XML (ntttcpr root).</param>
        /// <param name="telemetryContext">The telemetry context.</param>
        protected void CaptureDirectionalMetrics(string results, string commandArguments, DateTime startTime, DateTime endTime, string direction, bool isClientParser, EventContext telemetryContext)
        {
            if (!string.IsNullOrWhiteSpace(results))
            {
                this.MetadataContract.AddForScenario(
                    this.Tool.ToString(),
                    commandArguments,
                    toolVersion: null);

                EventContext relatedContext = telemetryContext.Clone();
                MetricsParser parser = new NTttcpMetricsParser(results, isClientParser);
                IList<Metric> metrics = parser.Parse();

                if (parser.Metadata.Any())
                {
                    this.MetadataContract.Add(
                        parser.Metadata.ToDictionary(entry => entry.Key, entry => entry.Value as object),
                        MetadataContract.ScenarioCategory,
                        true);

                    foreach (var entry in parser.Metadata)
                    {
                        relatedContext.Properties[entry.Key] = entry.Value;
                    }
                }

                if (direction != null)
                {
                    relatedContext.Properties["direction"] = direction;
                    relatedContext.Properties["duplexMode"] = "Full";
                }

                this.MetadataContract.Apply(relatedContext);

                string scenarioName = direction != null
                    ? $"{this.Scenario} {this.Role} {direction}"
                    : this.Name;

                this.Logger.LogMetrics(
                    this.Tool.ToString(),
                    scenarioName,
                    startTime,
                    endTime,
                    metrics,
                    string.Empty,
                    commandArguments,
                    this.Tags,
                    relatedContext);

                if (this.Platform == PlatformID.Unix)
                {
                    string sysctlResults = this.GetSysctlOutputAsync(CancellationToken.None).GetAwaiter().GetResult();

                    if (!string.IsNullOrWhiteSpace(sysctlResults))
                    {
                        SysctlParser sysctlParser = new SysctlParser(sysctlResults);
                        string parsedSysctlResults = sysctlParser.Parse();

                        relatedContext.AddContext("sysctlResults", parsedSysctlResults);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the command line arguments for the send direction in full-duplex mode.
        /// Client sends on the forward port (to server's receiver).
        /// Server sends on the reverse port (to client's receiver).
        /// </summary>
        protected string GetFullDuplexSendCommandLineArguments()
        {
            // Client sends on forward port, Server sends on reverse port
            int sendPort = this.IsInClientRole ? this.Port : this.ReversePort;

            if (this.Platform == PlatformID.Win32NT)
            {
                return this.GetWindowsSpecificCommandLine(isSender: true, port: sendPort, resultsPath: this.SendResultsPath);
            }

            return this.GetLinuxSpecificCommandLine(isSender: true, port: sendPort, resultsPath: this.SendResultsPath);
        }

        /// <summary>
        /// Returns the command line arguments for the receive direction in full-duplex mode.
        /// Client receives on the reverse port (from server's sender).
        /// Server receives on the forward port (from client's sender).
        /// </summary>
        protected string GetFullDuplexReceiveCommandLineArguments()
        {
            // Client receives on reverse port, Server receives on forward port
            int recvPort = this.IsInClientRole ? this.ReversePort : this.Port;

            if (this.Platform == PlatformID.Win32NT)
            {
                return this.GetWindowsSpecificCommandLine(isSender: false, port: recvPort, resultsPath: this.ReceiveResultsPath);
            }

            return this.GetLinuxSpecificCommandLine(isSender: false, port: recvPort, resultsPath: this.ReceiveResultsPath);
        }

        /// <summary>
        /// Waits for results at a specific file path.
        /// </summary>
        protected async Task<string> WaitForResultsAsync(TimeSpan timeout, EventContext telemetryContext, string resultsPath)
        {
            string results = null;
            IFile fileAccess = this.SystemManagement.FileSystem.File;
            DateTime pollingTimeout = DateTime.UtcNow.Add(timeout);

            while (DateTime.UtcNow < pollingTimeout)
            {
                if (fileAccess.Exists(resultsPath))
                {
                    try
                    {
                        results = await this.SystemManagement.FileSystem.File.ReadAllTextAsync(resultsPath);

                        if (!string.IsNullOrWhiteSpace(results))
                        {
                            break;
                        }
                    }
                    catch
                    {
                        // File may still be written to.
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            }

            return results;
        }

        private async Task DeleteResultsFileAsync()
        {
            if (this.SystemManagement.FileSystem.File.Exists(this.ResultsPath))
            {
                await this.SystemManagement.FileSystem.File.DeleteAsync(this.ResultsPath)
                    .ConfigureAwait(false);
            }
        }

        private async Task DeleteFullDuplexResultsFilesAsync()
        {
            if (this.SystemManagement.FileSystem.File.Exists(this.SendResultsPath))
            {
                await this.SystemManagement.FileSystem.File.DeleteAsync(this.SendResultsPath)
                    .ConfigureAwait(false);
            }

            if (this.SystemManagement.FileSystem.File.Exists(this.ReceiveResultsPath))
            {
                await this.SystemManagement.FileSystem.File.DeleteAsync(this.ReceiveResultsPath)
                    .ConfigureAwait(false);
            }
        }

        private string GetWindowsSpecificCommandLine()
        {
            return this.GetWindowsSpecificCommandLine(isSender: this.IsInClientRole, port: this.Port, resultsPath: this.ResultsPath);
        }

        private string GetWindowsSpecificCommandLine(bool isSender, int port, string resultsPath)
        {
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;

            // For NTttcp, -m always specifies the receiver's IP address.
            // Forward direction (port = this.Port): receiver is the server
            // Reverse direction (port = this.ReversePort): receiver is the client
            bool isReverseDirection = port != this.Port;
            string receiverIPAddress = isReverseDirection ? clientIPAddress : serverIPAddress;

            return $"{(isSender ? "-s" : "-r")} " +
                $"-m {this.ThreadCount},*,{receiverIPAddress} " +
                $"-wu {NTttcpExecutor.DefaultWarmupTime.TotalSeconds} " +
                $"-cd {NTttcpExecutor.DefaultCooldownTime.TotalSeconds} " +
                $"-t {this.TestDuration.TotalSeconds} " +
                $"-l {(isSender ? $"{this.BufferSizeClient}" : $"{this.BufferSizeServer}")} " +
                $"-p {port} " +
                $"-xml {resultsPath} " +
                $"{(this.Protocol.ToLowerInvariant() == "udp" ? "-u" : string.Empty)} " +
                $"{(this.NoSyncEnabled == true ? "-ns" : string.Empty)} " +
                $"{(isSender && this.IsInClientRole ? $"-nic {clientIPAddress}" : string.Empty)}".Trim();
        }

        private string GetLinuxSpecificCommandLine()
        {
            return this.GetLinuxSpecificCommandLine(isSender: this.IsInClientRole, port: this.Port, resultsPath: this.ResultsPath);
        }

        private string GetLinuxSpecificCommandLine(bool isSender, int port, string resultsPath)
        {
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            // For NTttcp, -m always specifies the receiver's IP address.
            // Forward direction (port = this.Port): receiver is the server
            // Reverse direction (port = this.ReversePort): receiver is the client
            bool isReverseDirection = port != this.Port;
            string receiverIPAddress = isReverseDirection ? clientIPAddress : serverIPAddress;

            return $"{(isSender ? "-s" : "-r")} " +
                $"-V " +
                $"-m {this.ThreadCount},*,{receiverIPAddress} " +
                $"-W {NTttcpExecutor.DefaultWarmupTime.TotalSeconds} " +
                $"-C {NTttcpExecutor.DefaultCooldownTime.TotalSeconds} " +
                $"-t {this.TestDuration.TotalSeconds} " +
                $"-b {(isSender ? $"{this.BufferSizeClient}" : $"{this.BufferSizeServer}")} " +
                $"-x {resultsPath} " +
                $"-p {port} " +
                $"{(this.Protocol.ToLowerInvariant() == "udp" ? "-u" : string.Empty)} " +
                $"{((isSender && this.SenderLastClient == true) ? "-L" : string.Empty)} " +
                $"{((!isSender && this.ReceiverMultiClientMode == true) ? "-M" : string.Empty)} " +
                $"{((isSender && this.ThreadsPerServerPort != null) ? $"-n {this.ThreadsPerServerPort}" : string.Empty)} " +
                $"{((isSender && this.ConnectionsPerThread != null) ? $"-l {this.ConnectionsPerThread}" : string.Empty)} " +
                $"{(this.NoSyncEnabled == true ? "-N" : string.Empty)} " +
                $"{((this.DevInterruptsDifferentiator != null) ? $"--show-dev-interrupts {this.DevInterruptsDifferentiator}" : string.Empty)} " +
                $"{(isSender && this.Protocol.ToLowerInvariant() == "tcp" ? "--show-tcp-retrans" : string.Empty)}".Trim();
        }
    }
}
