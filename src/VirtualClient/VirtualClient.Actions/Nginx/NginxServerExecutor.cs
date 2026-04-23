// Copyright (c) Microsoft Corporation.
namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The NGINX Server Executor
    /// </summary>
    [UnixCompatible]
    public class NginxServerExecutor : VirtualClientComponent
    {
        private TimeSpan pollingInterval = TimeSpan.FromSeconds(120);

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NginxServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.SystemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.pollingInterval = parameters.GetTimeSpanValue(nameof(this.pollingInterval), TimeSpan.FromSeconds(60));
        }

        /// <summary>
        /// Number workers used. Number cores nginx can use. Set to null or 0 to use all cores.
        /// </summary>
        public int Workers
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Workers), 0);
            }
        }

        /// <summary>
        /// Polling Timeout
        /// </summary>
        public TimeSpan Timeout
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.Timeout), TimeSpan.FromMinutes(30));
            }
        }

        /// <summary>
        /// File size to transport between Client and Server
        /// </summary>
        public int FileSizeInKB
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.FileSizeInKB), 1);
            }
        }

        /// <summary>
        /// The role of current instance
        /// </summary>
        public string Role
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Role));
            }
        }

        /// <summary>
        /// Provides components and services necessary for interacting with the local system and environment.
        /// </summary>
        protected ISystemManagement SystemManagement { get; }

        /// <summary>
        /// API Client that is used to communicate with self-hosted instance of the Virtual Client.
        /// </summary>
        protected IApiClient ServerApi { get; set; }

        /// <summary>
        /// API Client that is used to communicate with client-hosted instance of the Virtual Client Client.
        /// </summary>
        protected IApiClient ClientApi { get; set; }

        /// <summary>
        /// The path to the Nginx package.
        /// </summary>
        protected string PackageDirectory { get; set; }

        /// <summary>
        /// Initializes the API dependencies for running Nginx Server
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger.LogTraceMessage($"Role: {this.Roles}; Layout: {this.GetLayoutClientInstances}");

            PlatformSpecifics.ThrowIfNotSupported(this.CpuArchitecture);
            if (this.Platform != PlatformID.Unix)
            {
                this.Logger.LogNotSupported(this.PackageName, this.Platform, this.CpuArchitecture, EventContext.Persisted());
                throw new NotSupportedException($"The OS/system platform '{this.Platform}' is not supported.");
            }

            IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();

            ClientInstance serverInstance = this.GetLayoutClientInstances(this.Role).First();
            IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);
            this.ServerApi = clientManager.GetOrCreateApiClient(serverInstance.Name, serverInstance);

            ClientInstance clientInstance = this.GetLayoutClientInstances(ClientRole.Client).First();
            IPAddress.TryParse(clientInstance.IPAddress, out IPAddress clientIPAddress);
            this.ClientApi = clientManager.GetOrCreateApiClient(clientInstance.Name, clientInstance);

            DependencyPath workloadPackage = await this.SystemManagement.PackageManager.GetPackageAsync(this.PackageName, cancellationToken).ConfigureAwait(false);
            if (workloadPackage == null)
            {
                throw new DependencyException($"{this.TypeName} did not find package ({this.PackageName}) in the packages directory.", ErrorReason.WorkloadDependencyMissing);
            }

            workloadPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);
            this.PackageDirectory = workloadPackage.Path;

            this.SystemManagement.FileSystem.File.ThrowIfFileDoesNotExist(this.PlatformSpecifics.Combine(this.PackageDirectory, "setup-reset.sh"));
            this.SystemManagement.FileSystem.File.ThrowIfFileDoesNotExist(this.PlatformSpecifics.Combine(this.PackageDirectory, "setup-config.sh"));
            this.SystemManagement.FileSystem.File.ThrowIfFileDoesNotExist(this.PlatformSpecifics.Combine(this.PackageDirectory, "setup-content.sh"));

            string resetFilePath = this.PlatformSpecifics.Combine(this.PackageDirectory, "reset.sh");

            if (!this.SystemManagement.FileSystem.File.Exists(resetFilePath))
            {
                IProcessProxy process1 = await this.ExecuteCommandAsync(command: "bash", commandArguments: "setup-reset.sh", workingDirectory: this.PackageDirectory, telemetryContext, cancellationToken, runElevated: true);
                await this.LogProcessDetailsAsync(process1, telemetryContext, this.PackageDirectory, logToFile: true);
                process1.ThrowIfWorkloadFailed();
                telemetryContext.AddContext("resetContent", process1.StandardOutput);

                using (FileSystemStream fileStream = this.SystemManagement.FileSystem.FileStream.New(resetFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    byte[] bytedata = Encoding.Default.GetBytes(process1.StandardOutput.ToString());
                    fileStream.Write(bytedata, 0, bytedata.Length);
                    await fileStream.FlushAsync().ConfigureAwait(false);
                    fileStream.Close();
                    fileStream.Dispose();
                    this.Logger.LogTraceMessage($"File Created...{resetFilePath}");
                }
            }

            IProcessProxy process2 = await this.ExecuteCommandAsync(command: "bash", commandArguments: $"setup-content.sh {this.FileSizeInKB}", workingDirectory: this.PackageDirectory, telemetryContext, cancellationToken, runElevated: true).ConfigureAwait(false);
            await this.LogProcessDetailsAsync(process2, telemetryContext, this.PackageDirectory, logToFile: true);
            process2.ThrowIfWorkloadFailed();

            ClientInstance backendInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
            IPAddress.TryParse(backendInstance.IPAddress, out IPAddress backendIPAddress);
            IProcessProxy process3 = await this.ExecuteCommandAsync(command: "bash", commandArguments: $"setup-config.sh {((this.Workers != 0) ? this.Workers : "auto")} {this.Role} {backendIPAddress}", workingDirectory: this.PackageDirectory, telemetryContext, cancellationToken, runElevated: true).ConfigureAwait(false);
            await this.LogProcessDetailsAsync(process3, telemetryContext, this.PackageDirectory, logToFile: true);
            process3.ThrowIfWorkloadFailed();

            await this.ServerApi.DeleteStateAsync("version", cancellationToken).ConfigureAwait(false);
            await this.ServerApi.DeleteStateAsync(nameof(State), cancellationToken).ConfigureAwait(false);
            this.SetServerOnline(true);
            this.Logger.LogTraceMessage($"{this.TypeName} Initialize Complete.");
        }

        /// <summary>
        /// Executes component logic
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext
                 .AddContext("currentDirectory", Environment.CurrentDirectory)
                 .AddContext("toolName", "nginx")
                 .AddContext("timeout", this.Timeout);

            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Dictionary<string, string> nginxVersion = await this.GetNginxVersionAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                    State nginxState = new State();
                    nginxVersion.Any(x => nginxState.Properties.TryAdd(x.Key, x.Value));
                    await this.ServerApi.CreateStateAsync<State>("version", nginxState, cancellationToken).ConfigureAwait(false);
                    telemetryContext.AddContext(nameof(nginxVersion), nginxVersion);

                    using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                    {
                        await this.ExecuteNginxCommandAsync(NginxCommand.Start, workingDirectory: null, telemetryContext, cancellationToken).ConfigureAwait(false);

                        Item<State> serverState = new Item<State>(nameof(State), new State());
                        serverState.Definition.Timeout(DateTime.UtcNow.Add(this.Timeout));
                        serverState.Definition.Online(true);
                        await this.ServerApi.CreateStateAsync<State>(nameof(State), serverState.Definition, cancellationToken).ConfigureAwait(false);

                        while (!(serverState.Definition.Timeout() < DateTime.UtcNow))
                        {
                            EventContext relatedContext = telemetryContext
                                .Clone()
                                .AddContext(nameof(serverState), serverState);

                            await this.ClientApi.PollForExpectedStateAsync<State>(nameof(State), (state => state.Online() == true), this.Timeout, cancellationToken, this.pollingInterval).ConfigureAwait(false);
                            Item<State> clientState = await this.ClientApi.GetStateAsync<State>(nameof(State), cancellationToken).ConfigureAwait(false);
                            relatedContext.AddContext(nameof(clientState), clientState);

                            await Task.Delay(this.pollingInterval, cancellationToken);
                            DateTime timeout = clientState.Definition.Properties.GetValue<DateTime>("Timeout", serverState.Definition.Timeout());

                            serverState.Definition.Timeout(timeout);
                            await this.ServerApi.UpdateStateAsync<State>(nameof(State), serverState, cancellationToken).ConfigureAwait(false);
                            relatedContext.AddContext(nameof(serverState), serverState);
                        }
                    }
                }
                catch (Exception exc)
                {
                    this.Logger.LogErrorMessage(exc, telemetryContext);
                    throw;
                }
                finally
                {
                    await this.ResetNginxAsync(telemetryContext, cancellationToken).ConfigureAwait(false);                    
                }
            }
        }

        /// <summary>
        /// Disposes of resources used by the executor including resetting server.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Task.Run(async () =>
                    {
                        await this.ResetNginxAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
                    }).Wait(TimeSpan.FromSeconds(30));
                }
                catch (AggregateException)
                {
                    // Best-effort cleanup during dispose; exceptions are intentionally swallowed.
                }
            }
        }

        /// <summary>
        /// Reset Server for Nginx
        /// </summary>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task ResetNginxAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                IProcessProxy process1 = await this.ExecuteCommandAsync(command: "bash", commandArguments: "reset.sh", workingDirectory: this.PackageDirectory, telemetryContext, cancellationToken, runElevated: true).ConfigureAwait(false);
                await this.LogProcessDetailsAsync(process1, telemetryContext, this.PackageName, logToFile: true);

                await this.ExecuteNginxCommandAsync(NginxCommand.Stop, workingDirectory: null, telemetryContext, cancellationToken).ConfigureAwait(false);

                State serverState = new State();
                serverState.Online(false);
                serverState.Properties["ResetTime"] = DateTime.UtcNow.ToString();
                await this.ServerApi.UpdateStateAsync<State>(nameof(State), new Item<State>(nameof(State), serverState), cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                this.Logger.LogTraceMessage("Failed to reset server.");
            }
        }

        /// <summary>
        ///  Executes a command to get Nginx Version installed from the system.
        /// </summary>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the process execution.</param>
        /// <returns>NginxVersion</returns>
        protected async Task<Dictionary<string, string>> GetNginxVersionAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IProcessProxy process = await this.ExecuteNginxCommandAsync(NginxCommand.GetVersion, null, telemetryContext, cancellationToken).ConfigureAwait(false);
            string standardErr = process.StandardError.ToString();

            return this.TransformNginxVersionToDictionary(standardErr);
        }

        private Dictionary<string, string> TransformNginxVersionToDictionary(string processOutput)
        {
            processOutput.ThrowIfNullOrEmpty(nameof(processOutput));
            string[] standardOutputSections = processOutput.Split(Environment.NewLine, StringSplitOptions.TrimEntries);

            string nginxVersion =
                standardOutputSections
                .Where(x => x.Contains("nginx version", StringComparison.OrdinalIgnoreCase)).FirstOrDefault()
                .Split(":").Last().Trim();

            nginxVersion.ThrowIfNullOrEmpty(nameof(nginxVersion), $"{nameof(processOutput)} does not contain nginx version. {nameof(processOutput)}: {processOutput}");

            string sslVersion =
                standardOutputSections
                .Where(x => x.Contains("OpenSSL", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            string serverNameIndicationSupport =
                standardOutputSections
                .Where(x => x.Contains("TLS", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            string arguments =
                standardOutputSections
                .Where(x => x.Contains("configure arguments", StringComparison.OrdinalIgnoreCase)).FirstOrDefault()
                ?.Split(":").Last().Trim();

            nginxVersion.ThrowIfNullOrEmpty(nameof(nginxVersion), $"{nameof(processOutput)} does not contain nginx version. {nameof(processOutput)}: {processOutput}");
            return new Dictionary<string, string>
            {
                { nameof(nginxVersion), nginxVersion },
                { nameof(sslVersion), sslVersion },
                { nameof(serverNameIndicationSupport), serverNameIndicationSupport },
                { nameof(arguments), arguments }
            };
        }

        private Task<IProcessProxy> ExecuteNginxCommandAsync(NginxCommand nginxCommand, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            nginxCommand.ThrowIfNull("nginxCommand");
            telemetryContext.ThrowIfNull("telemetryContext");

            telemetryContext.AddContext(nameof(nginxCommand), nginxCommand);
            string commandArgs = nginxCommand.ConvertToCommandArgs();
            telemetryContext.AddContext(nameof(commandArgs), $"{commandArgs}");

            if (this.Platform != PlatformID.Unix)
            {
                throw new NotSupportedException($"Nginx command is not supported on '{this.Platform}' platform/architecture systems.");
            }

            return this.Logger.LogMessageAsync($"{nameof(this.TypeName)}.ExecuteNginxCommand", telemetryContext, async () =>
            {
                IProcessProxy process = await this.ExecuteCommandAsync(command: "sudo", commandArguments: commandArgs, workingDirectory: workingDirectory, telemetryContext, cancellationToken, runElevated: true).ConfigureAwait(false);
                await this.LogProcessDetailsAsync(process, telemetryContext, logToFile: true);
                process.ThrowIfWorkloadFailed();
                return process;
            });
        }
    }
}
