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
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// NTttcp(Test Bandwith and Throughput) Tool Client Executor. 
    /// </summary>
    [WindowsCompatible]
    [UnixCompatible]
    public class NTttcpExecutor : NetworkingWorkloadToolExecutor
    {
        private const string OutputFileName = "ntttcp-results.xml";
        private static readonly TimeSpan DefaultWarmupTime = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan DefaultCooldownTime = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Initializes a new instance of the <see cref="NTttcpExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NTttcpExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
            this.ProcessStartRetryPolicy = Policy.Handle<Exception>(exc => exc.Message.Contains("sockwiz_tcp_listener_open bind"))
               .WaitAndRetryAsync(5, retries => TimeSpan.FromSeconds(retries * 3));

            this.Parameters.SetIfNotDefined(nameof(this.ThreadCount), 1);
            this.Parameters.SetIfNotDefined(nameof(this.TestDuration), 60);
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
        /// Parameter defines the duration (in seconds) for running the NTttcp workload.
        /// </summary>
        public int TestDuration
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.TestDuration), 60);
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
                        process.SafeKill();
                    }
                }

                return results;
            });
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
                        await this.DeleteResultsFileAsync();

                        using (process = this.SystemManagement.ProcessManager.CreateProcess(this.ExecutablePath, commandArguments))
                        {
                            try
                            {
                                this.CleanupTasks.Add(() => process.SafeKill());
                                await process.StartAndWaitAsync(cancellationToken, timeout);

                                if (process.IsErrored())
                                {
                                    await this.LogProcessDetailsAsync(process, relatedContext, "NTttcp");
                                    process.ThrowIfWorkloadFailed();
                                }
                                else
                                {
                                    string results = await this.WaitForResultsAsync(TimeSpan.FromMinutes(1), relatedContext);
                                    await this.LogProcessDetailsAsync(process, relatedContext, "NTttcp", results: results.AsArray());

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
                                process.SafeKill();
                            }
                            catch (Exception exc)
                            {
                                this.Logger.LogMessage($"{this.TypeName}.WorkloadStartupError", LogLevel.Warning, relatedContext.AddError(exc));
                                process.SafeKill();
                                throw;
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

                EventContext relatedContext = telemetryContext.Clone();
                MetricsParser parser = new NTttcpMetricsParser(results, this.IsInClientRole);
                IList<Metric> metrics = parser.Parse();

                if (parser.Metadata.Any())
                {
                    this.MetadataContract.Add(
                        parser.Metadata.ToDictionary(entry => entry.Key, entry => entry.Value as object),
                        MetadataContractCategory.Scenario,
                        true);

                    foreach (var entry in parser.Metadata)
                    {
                        relatedContext.Properties[entry.Key] = entry.Value;
                    }
                }

                this.MetadataContract.Apply(relatedContext);

                this.Logger.LogMetrics(
                    this.Tool.ToString(),
                    this.Name,
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

        private async Task DeleteResultsFileAsync()
        {
            if (this.SystemManagement.FileSystem.File.Exists(this.ResultsPath))
            {
                await this.SystemManagement.FileSystem.File.DeleteAsync(this.ResultsPath)
                    .ConfigureAwait(false);
            }
        }

        private string GetWindowsSpecificCommandLine()
        {
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            return $"{(this.IsInClientRole ? "-s" : "-r")} " +
                $"-m {this.ThreadCount},*,{serverIPAddress} " +
                $"-wu {NTttcpExecutor.DefaultWarmupTime.TotalSeconds} " +
                $"-cd {NTttcpExecutor.DefaultCooldownTime.TotalSeconds} " +
                $"-t {this.TestDuration} " +
                $"-l {(this.IsInClientRole ? $"{this.BufferSizeClient}" : $"{this.BufferSizeServer}")} " +
                $"-p {this.Port} " +
                $"-xml {this.ResultsPath} " +
                $"{(this.Protocol.ToLowerInvariant() == "udp" ? "-u" : string.Empty)} " +
                $"{(this.IsInClientRole ? $"-nic {clientIPAddress}" : string.Empty)}".Trim();
        }

        private string GetLinuxSpecificCommandLine()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            return $"{(this.IsInClientRole ? "-s" : "-r")} " +
                $"-V " +
                $"-m {this.ThreadCount},*,{serverIPAddress} " +
                $"-W {NTttcpExecutor.DefaultWarmupTime.TotalSeconds} " +
                $"-C {NTttcpExecutor.DefaultCooldownTime.TotalSeconds} " +
                $"-t {this.TestDuration} " +
                $"-b {(this.IsInClientRole ? $"{this.BufferSizeClient}" : $"{this.BufferSizeServer}")} " +
                $"-x {this.ResultsPath} " +
                $"-p {this.Port} " +
                $"{(this.Protocol.ToLowerInvariant() == "udp" ? "-u" : string.Empty)} " +
                $"{((this.IsInClientRole && this.SenderLastClient == true) ? "-L" : string.Empty)} " +
                $"{((this.IsInServerRole && this.ReceiverMultiClientMode == true) ? "-M" : string.Empty)} " +
                $"{((this.IsInClientRole && this.ThreadsPerServerPort != null) ? $"-n {this.ThreadsPerServerPort}" : string.Empty)} " +
                $"{((this.IsInClientRole && this.ConnectionsPerThread != null) ? $"-l {this.ConnectionsPerThread}" : string.Empty)} " +
                $"{((this.DevInterruptsDifferentiator != null) ? $"--show-dev-interrupts {this.DevInterruptsDifferentiator}" : string.Empty)}".Trim();
        }
    }
}
