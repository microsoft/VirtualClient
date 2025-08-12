// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Polly;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// NTttcp Executor
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class NTttcpExecutor2 : VirtualClientComponent
    {
        /// <summary>
        /// Warmup Time
        /// </summary>
        protected static readonly TimeSpan DefaultWarmupTime = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Cooldown Time
        /// </summary>
        protected static readonly TimeSpan DefaultCooldownTime = TimeSpan.FromSeconds(10);

        private const string OutputFileName = "ntttcp-results.xml";
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="NTttcpExecutor2"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NTttcpExecutor2(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.ProcessStartRetryPolicy = Policy.Handle<Exception>(exc => exc.Message.Contains("sockwiz_tcp_listener_open bind"))
               .WaitAndRetryAsync(5, retries => TimeSpan.FromSeconds(retries * 3));
        }

        /// <summary>
        /// The type of the protocol that should be used for the workload.(e.g. TCP,UDP)
        /// </summary>
        public ProtocolType Protocol
        {
            get
            {
                return (ProtocolType)Enum.Parse(typeof(ProtocolType), this.Parameters.GetValue<string>(nameof(this.Protocol)), true);
            }
        }

        /// <summary>
        /// Client used to communicate with the locally self-hosted instance of the
        /// Virtual Client API.
        /// </summary>
        protected IApiClient LocalApiClient { get; set; }

        /// <summary>
        /// Client used to communicate with the target self-hosted instance of the
        /// Virtual Client API (i.e. the server-side instance).
        /// </summary>
        protected IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// Provides features for management of the system/environment.
        /// </summary>
        protected ISystemManagement SystemManager
        {
            get
            {
                return this.Dependencies.GetService<ISystemManagement>();
            }
        }

        /// <summary>
        ///  Name of the tool (CPS,SockPerf,Latte,NTttcp).
        /// </summary>
        protected string Tool { get; set; }

        /// <summary>
        /// Process name of the tool.
        /// </summary>
        protected string ProcessName { get; set; }

        /// <summary>
        /// Name of the scenario.
        /// </summary>
        protected string Name { get; set; }

        /// <summary>
        /// Tool executable path.
        /// </summary>
        protected string ExecutablePath { get; set; }

        /// <summary>
        /// Path to the metrics/results.
        /// </summary>
        protected string ResultsPath { get; set; }

        /// <summary>
        /// The retry policy to apply to the startup of the NTttcp workload to handle
        /// transient issues.
        /// </summary>
        protected IAsyncPolicy ProcessStartRetryPolicy { get; set; }

        /// <summary>
        /// Cancellation Token Source for Server.
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// The role of the current Virtual Client instance. Supported roles = Client or Server
        /// </summary>
        protected string Role { get; set; }

        /// <summary>
        /// Intialize NTttcp.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string protocol = this.Protocol.ToString().ToLowerInvariant();
            if (protocol != "tcp" && protocol != "udp")
            {
                throw new NotSupportedException($"The network protocol '{this.Protocol}' is not supported for the NTttcp workload.");
            }

            if (string.IsNullOrWhiteSpace(this.Scenario))
            {
                throw new WorkloadException(
                    $"Scenario parameter missing. The profile supplied is missing the required '{nameof(this.Scenario)}' parameter " +
                    $"for one or more of the '{this.TypeName}' steps.",
                    ErrorReason.InvalidProfileDefinition);
            }

            ClientInstance clientInstance = this.GetLayoutClientInstance(this.AgentId);
            string layoutIPAddress = clientInstance.IPAddress;

            this.Logger.LogTraceMessage($"Layout-Defined IP Address: {layoutIPAddress}");
            this.Logger.LogTraceMessage($"Layout-Defined Role: {clientInstance.Role}");

            this.ThrowIfLayoutNotDefined();
            this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);

            DependencyPath workloadPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            telemetryContext.AddContext("package", workloadPackage);

            this.Role = clientInstance.Role;
            this.InitializeApiClients();

            this.Name = $"{this.Scenario} {this.Role}";
            this.ProcessName = "ntttcp";
            this.Tool = "NTttcp";

            string resultsDir = this.Combine(workloadPackage.Path, this.Scenario);
            this.fileSystem.Directory.CreateDirectory(resultsDir);

            this.ResultsPath = this.Combine(resultsDir, NTttcpClientExecutor2.OutputFileName);

            if (this.Platform == PlatformID.Win32NT)
            {
                this.ExecutablePath = this.Combine(workloadPackage.Path, "NTttcp.exe");
            }
            else if (this.Platform == PlatformID.Unix)
            {
                this.ExecutablePath = this.Combine(workloadPackage.Path, "ntttcp");
            }
            else
            {
                throw new NotSupportedException($"{this.Platform} is not supported");
            }

            await this.SystemManager.MakeFileExecutableAsync(this.ExecutablePath, this.Platform, cancellationToken);
        }

        /// <summary>
        /// Executes the NTttcp workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Role != ClientRole.Client && this.Role != ClientRole.Server)
            {
                throw new NotSupportedException($"The role: {this.Role} is not supported for {this.TypeName}." +
                    $" Environment layout should contain only {ClientRole.Client} or {ClientRole.Server} as roles");
            }

            using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    CancellationToken serverCancellationToken = this.ServerCancellationSource.Token;

                    if (this.Role == ClientRole.Server)
                    {
                        using (var serverExecutor = this.CreateWorkloadServer())
                        {
                            await serverExecutor.ExecuteAsync(serverCancellationToken)
                                .ConfigureAwait(false);

                            this.Logger.LogMessage($"{this.TypeName}.ServerExecutionCompleted", telemetryContext);
                        }
                    }
                    else if (this.Role == ClientRole.Client)
                    {
                        using (var clientExecutor = this.CreateWorkloadClient())
                        {
                            await clientExecutor.ExecuteAsync(serverCancellationToken)
                                .ConfigureAwait(false);

                            this.Logger.LogMessage($"{this.TypeName}.ClientExecutionCompleted", telemetryContext);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                    this.Logger.LogMessage($"{this.TypeName}.Canceled", telemetryContext);
                }
            }
        }

        /// <summary>
        /// Get new CPS client instance.
        /// </summary>
        protected virtual VirtualClientComponent CreateWorkloadClient()
        {
            return new CPSClientExecutor2(this.Dependencies, this.Parameters);
        }

        /// <summary>
        /// Get new Networking workload server instance.
        /// </summary>
        protected virtual VirtualClientComponent CreateWorkloadServer()
        {
            return new NetworkingWorkloadProxy(this.Dependencies, this.Parameters);
        }

        /// <summary>
        /// Delete Results File
        /// </summary>
        /// <returns></returns>
        protected async Task DeleteResultsFileAsync()
        {
            if (this.SystemManager.FileSystem.File.Exists(this.ResultsPath))
            {
                await this.SystemManager.FileSystem.File.DeleteAsync(this.ResultsPath)
                    .ConfigureAwait(false);
            }

        }

        /// <summary>
        /// Enable the firewall rule for the tool executable.
        /// </summary>
        protected async Task EnableInboundFirewallAccessAsync(string exePath, ISystemManagement systemManagement, CancellationToken cancellationToken)
        {
            if (exePath != null)
            {
                FirewallEntry firewallEntry = new FirewallEntry(
                    $"Virtual Client: Allow {exePath}",
                    "Allows client and server instances of the Virtual Client to communicate via the self-hosted API service.",
                    exePath);

                await systemManagement.FirewallManager.EnableInboundAppAsync(firewallEntry, cancellationToken)
                    .ConfigureAwait(false);
            }

        }

        /// <summary>
        /// Returns true if results are found in the results file within the polling/timeout
        /// period specified.
        /// </summary>
        protected async Task WaitForResultsAsync(TimeSpan timeout, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IFile fileAccess = this.SystemManager.FileSystem.File;
            string resultsContent = null;
            DateTime pollingTimeout = DateTime.UtcNow.Add(timeout);

            while (DateTime.UtcNow < pollingTimeout && !cancellationToken.IsCancellationRequested)
            {
                if (fileAccess.Exists(this.ResultsPath))
                {
                    try
                    {
                        resultsContent = await this.SystemManager.FileSystem.File.ReadAllTextAsync(this.ResultsPath)
                            .ConfigureAwait(false);

                        if (!string.IsNullOrWhiteSpace(resultsContent))
                        {
                            this.Logger.LogMessage($"{this.TypeName}.WorkloadOutputFileContents", telemetryContext
                                .AddContext("results", resultsContent));

                            break;
                        }
                    }
                    catch (IOException)
                    {
                        // This can be hit if the application is exiting/cancelling while attempting to read
                        // the results file.
                    }
                }

                await this.WaitAsync(TimeSpan.FromSeconds(2), cancellationToken).ConfigureAwait(false);
            }

            if (string.IsNullOrWhiteSpace(resultsContent))
            {
                throw new WorkloadResultsException(
                    $"Results not found. The workload '{this.ExecutablePath}' did not produce any valid results.",
                    ErrorReason.WorkloadFailed);
            }
        }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="commandArguments">The command arguments to use to run the workload toolset.</param>
        /// <param name="timeout">The absolute timeout for the workload.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events emitted.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected Task<IProcessProxy> ExecuteWorkloadAsync(string commandArguments, TimeSpan timeout, EventContext telemetryContext, CancellationToken cancellationToken)
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
                        using (process = this.SystemManager.ProcessManager.CreateProcess(this.ExecutablePath, commandArguments))
                        {
                            try
                            {
                                this.CleanupTasks.Add(() => process.SafeKill());
                                await process.StartAndWaitAsync(cancellationToken, timeout);

                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    if (process.IsErrored())
                                    {
                                        await this.LogProcessDetailsAsync(process, relatedContext, "NTttcp", logToFile: true);
                                        process.ThrowIfWorkloadFailed();
                                    }

                                    await this.WaitForResultsAsync(TimeSpan.FromMinutes(2), relatedContext, cancellationToken);

                                    string results = await this.LoadResultsAsync(this.ResultsPath, cancellationToken);
                                    await this.LogProcessDetailsAsync(process, relatedContext, "NTttcp", results: results.AsArray(), logToFile: true);
                                }
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
                    }).ConfigureAwait(false);
                }

                return process;
            });
        }

        /// <summary>
        /// Logs the workload metrics to the telemetry.
        /// </summary>
        protected async Task CaptureMetricsAsync(string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            this.MetadataContract.AddForScenario(
               "NTttcp",
               commandArguments,
               toolVersion: null);

            IFile fileAccess = this.SystemManager.FileSystem.File;
            EventContext relatedContext = telemetryContext.Clone();

            if (fileAccess.Exists(this.ResultsPath))
            {
                string resultsContent = await this.LoadResultsAsync(this.ResultsPath, CancellationToken.None);

                if (!string.IsNullOrWhiteSpace(resultsContent))
                {
                    bool isRoleClient = (this.Role == ClientRole.Client) ? true : false;
                    MetricsParser parser = new NTttcpMetricsParser2(resultsContent, isRoleClient);
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

                    this.MetadataContract.Apply(telemetryContext);

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

                    if (this.Platform == PlatformID.Unix)
                    {
                        string sysctlResults = await this.GetSysctlOutputAsync(CancellationToken.None);

                        if (!string.IsNullOrWhiteSpace(sysctlResults))
                        {
                            SysctlParser sysctlParser = new SysctlParser(sysctlResults);
                            string parsedSysctlResults = sysctlParser.Parse();

                            relatedContext.AddContext("sysctlResults", parsedSysctlResults);
                        }
                    }
                }
            }
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
                using (IProcessProxy process = this.SystemManager.ProcessManager.CreateProcess(sysctlCommand, sysctlArguments))
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

        private void InitializeApiClients()
        {
            IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();
            this.LocalApiClient = clientManager.GetOrCreateApiClient(IPAddress.Loopback.ToString(), IPAddress.Loopback);

            if (this.Role == ClientRole.Client)
            {
                ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                // It is important that we reuse the API client. The HttpClient created underneath will need to use a
                // new connection from the connection pool typically for each instance created. Especially for the case with
                // this workload that is testing network resources, we need to be very cognizant of our usage of TCP connections.
                this.ServerApiClient = clientManager.GetOrCreateApiClient(serverInstance.IPAddress, serverIPAddress);
                this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", this.ServerApiClient);
            }
        }

        /// <summary>
        /// NTttcp State object.
        /// </summary>
        internal class NTttcpWorkloadState : State
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="NTttcpWorkloadState"/> class.
            /// </summary>
            public NTttcpWorkloadState(ClientServerStatus status, IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
                this.Status = status;
            }

            /// <summary>
            /// An identifier for the status of state (e.g. ClientServerReset).
            /// </summary>
            [JsonProperty(PropertyName = "status", Required = Required.Always)]
            [System.Text.Json.Serialization.JsonConverter(typeof(StringEnumConverter))]
            public ClientServerStatus Status { get; set; }

        }
    }
}
