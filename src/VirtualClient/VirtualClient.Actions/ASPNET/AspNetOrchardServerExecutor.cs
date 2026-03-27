// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.ProcessAffinity;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// AspNet Orchard Server Executor.
    /// </summary>
    public class AspNetOrchardServerExecutor : VirtualClientMultiRoleComponent
    {
        private Task serverProcess;
        private bool disposed;
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManagement;

        private string dotnetExePath;
        private string aspnetOrchardDirectory;
        private string aspnetOrchardPublishPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetOrchardServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public AspNetOrchardServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
            this.stateManager = this.systemManagement.StateManager;
            this.fileSystem = this.systemManagement.FileSystem;
            this.ServerRetryPolicy = Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
                 .WaitAndRetryAsync(3, (retries) => TimeSpan.FromSeconds(retries));
        }

        /// <summary>
        /// The name of the package where the AspNetBench package is downloaded.
        /// </summary>
        public string TargetFramework
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(AspNetOrchardServerExecutor.TargetFramework)).ToLower();
            }
        }

        /// <summary>
        /// The port to run for Orchard Server.
        /// </summary>
        public string ServerPort
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(AspNetOrchardServerExecutor.ServerPort), "5014");
            }
        }

        /// <summary>
        /// API Client that is used to communicate with server-hosted instance of the Virtual Client Server.
        /// </summary>
        public IApiClient ServerApi { get; set; }

        /// <summary>
        /// The name of the package where the wrk package is downloaded.
        /// </summary>
        public string WrkPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(AspNetOrchardServerExecutor.WrkPackageName), "wrk");
            }
        }

        /// <summary>
        /// The name of the package where the DotNetSDK package is downloaded.
        /// </summary>
        public string DotNetSdkPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(AspNetOrchardServerExecutor.DotNetSdkPackageName), "dotnetsdk");
            }
        }

        /// <summary>
        /// Gets or sets whether to bind the workload to specific CPU cores.
        /// </summary>
        public bool BindToCores
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.BindToCores), defaultValue: false);
            }
        }

        /// <summary>
        /// Gets the CPU core affinity specification (e.g., "0-3", "0,2,4,6").
        /// </summary>
        public string CoreAffinity
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.CoreAffinity), out IConvertible value);
                return value?.ToString();
            }
        }

        /// <summary>
        /// A retry policy to apply to the server when starting to handle transient issues that
        /// would otherwise prevent it from starting successfully.
        /// </summary>
        protected IAsyncPolicy ServerRetryPolicy { get; set; }

        /// <summary>
        /// Disposes of resources used by the executor including shutting down any
        /// instances of Redis server running.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.disposed)
                {
                    this.KillServerInstancesAsync(null, CancellationToken.None)
                        .GetAwaiter().GetResult();
                    this.disposed = true;
                }
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the AspNetBench workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.packageManager.GetPackageAsync(this.PackageName, cancellationToken)
                .ConfigureAwait(false);
            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The expected workload package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }
            else
            {
                this.aspnetOrchardDirectory = this.Combine(workloadPackage.Path, "src", "OrchardCore.Cms.Web");
            }

            DependencyPath dotnetSdkPackage = await this.packageManager.GetPackageAsync(this.DotNetSdkPackageName, cancellationToken)
                .ConfigureAwait(false);
            if (dotnetSdkPackage == null)
            {
                throw new DependencyException(
                    $"The expected DotNet SDK package does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.dotnetExePath = this.Combine(dotnetSdkPackage.Path, this.Platform == PlatformID.Unix ? "dotnet" : "dotnet.exe");

            this.InitializeApiClients();
        }

        /// <summary>
        /// Validates the component parameters.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();

            if (this.BindToCores)
            {
                this.ThrowIfParameterNotDefined(nameof(this.CoreAffinity));
            }
        }

        /// <summary>
        /// Initializes API client.
        /// </summary>
        protected void InitializeApiClients()
        {
            IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();
            ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();

            this.ServerApi = clientManager.GetOrCreateApiClient(serverInstance.Name, serverInstance);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        /// <exception cref="DependencyException"></exception>
        protected async Task BuildAspNetOrchardAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string buildArgument = $"publish -c Release --sc -f {this.TargetFramework} {this.Combine(this.aspnetOrchardDirectory, "OrchardCore.Cms.Web.csproj")}";
            await this.ExecuteCommandAsync(this.dotnetExePath, buildArgument, this.aspnetOrchardDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            // bin/Release/net9.0/linux-x64/publish
            this.aspnetOrchardPublishPath = this.Combine(
                this.aspnetOrchardDirectory,
                "bin",
                "Release",
                this.TargetFramework,
                this.PlatformArchitectureName,
                "publish");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteServer", telemetryContext, async () =>
            {
                try
                {
                    this.SetServerOnline(false);

                    await this.ServerApi.PollForHeartbeatAsync(TimeSpan.FromMinutes(5), cancellationToken);

                    await this.DeleteStateAsync(telemetryContext, cancellationToken);
                    await this.KillServerInstancesAsync(telemetryContext, cancellationToken);
                    await this.BuildAspNetOrchardAsync(telemetryContext, cancellationToken);
                    this.StartServerInstances(telemetryContext, cancellationToken);

                    await this.SaveStateAsync(telemetryContext, cancellationToken);
                    this.SetServerOnline(true);

                    using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                    {
                        await Task.WhenAny(this.serverProcess);
                        if (cancellationToken.IsCancellationRequested)
                        {
                            await Task.WhenAll(this.serverProcess);
                        }
                    }
                }
                catch
                {
                    this.SetServerOnline(false);
                    await this.KillServerInstancesAsync(telemetryContext, cancellationToken);
                    throw;
                }
            });
        }

        private Task DeleteStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();
            return this.Logger.LogMessageAsync($"{this.TypeName}.DeleteState", relatedContext, async () =>
            {
                using (HttpResponseMessage response = await this.ServerApi.DeleteStateAsync(nameof(State), cancellationToken))
                {
                    relatedContext.AddResponseContext(response);
                    if (response.StatusCode != HttpStatusCode.NoContent)
                    {
                        response.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
                    }
                }
            });
        }

        private Task KillServerInstancesAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger.LogTraceMessage($"{this.TypeName}.KillServerInstances");
            this.ExecuteCommandAsync("pkill", "OrchardCore", this.aspnetOrchardDirectory, telemetryContext, cancellationToken);
            this.ExecuteCommandAsync("fuser", $"-n tcp -k {this.ServerPort}", this.aspnetOrchardDirectory, telemetryContext, cancellationToken);

            return this.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
        }

        private void StartServerInstances(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();

            this.Logger.LogMessage($"{this.TypeName}.StartServerInstances", relatedContext, () =>
            {
                try
                {
                    string command = "nohup";
                    string workingDirectory = this.aspnetOrchardDirectory;
                    string commandArguments = $"{this.Combine(this.aspnetOrchardPublishPath, "OrchardCore.Cms.Web")} --urls http://*:{this.ServerPort}";

                    relatedContext.AddContext("command", command);
                    relatedContext.AddContext("commandArguments", commandArguments);
                    relatedContext.AddContext("workingDir", workingDirectory);
                    relatedContext.AddContext("bindToCores", this.BindToCores);
                    relatedContext.AddContext("coreAffinity", this.CoreAffinity);

                    this.serverProcess = this.StartServerInstanceAsync(command, commandArguments, workingDirectory, relatedContext, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                }
            });
        }

        private Task StartServerInstanceAsync(string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return (this.ServerRetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
            {
                try
                {
                    IProcessProxy process = null;
                    ProcessAffinityConfiguration affinityConfig = null;

                    if (this.BindToCores && !string.IsNullOrWhiteSpace(this.CoreAffinity))
                    {
                        affinityConfig = ProcessAffinityConfiguration.Create(
                            this.Platform,
                            this.CoreAffinity);

                        telemetryContext.AddContext("affinityMask", affinityConfig.ToString());

                        if (this.Platform == PlatformID.Win32NT)
                        {
                            process = this.systemManagement.ProcessManager.CreateProcess(
                                command,
                                commandArguments,
                                workingDirectory);
                        }
                        else
                        {
                            string fullCommandLine = $"{command} {commandArguments}";
                            
                            LinuxProcessAffinityConfiguration linuxConfig = (LinuxProcessAffinityConfiguration)affinityConfig;
                            string wrappedCommand = linuxConfig.GetCommandWithAffinity(null, fullCommandLine);

                            process = this.systemManagement.ProcessManager.CreateProcess(
                                "/bin/bash",
                                $"-c {wrappedCommand}",
                                workingDirectory);
                        }
                    }
                    else
                    {
                        process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, telemetryContext, cancellationToken);
                    }

                    using (process)
                    {
                        if (affinityConfig != null)
                        {
                            if (this.Platform == PlatformID.Win32NT)
                            {
                                process.Start();
                                process.ApplyAffinity((WindowsProcessAffinityConfiguration)affinityConfig);
                                await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                            }
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "Orchard");
                            process.ThrowIfWorkloadFailed(successCodes: new int[] { 0 });
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                }
                catch (Exception exc)
                {
                    this.Logger.LogMessage(
                        $"{this.TypeName}.StartServerInstanceError",
                        LogLevel.Error,
                        telemetryContext.Clone().AddError(exc));

                    throw;
                }
            });
        }

        private Task SaveStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();
            return this.Logger.LogMessageAsync($"{this.TypeName}.SaveState", relatedContext, async () =>
            {
                Item<State> serverState = new Item<State>(nameof(State), new State());
                serverState.Definition.Online(true);
                using (HttpResponseMessage response = await this.ServerApi.UpdateStateAsync<State>(serverState.Id, serverState, cancellationToken))
                {
                    relatedContext.AddResponseContext(response);
                    response.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
                }
            });
        }
    }
}
