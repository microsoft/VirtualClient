// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// DeathStarBench Executor
    /// </summary>
    [UnixCompatible]
    public class DeathStarBenchExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Name of socialNetwork service.
        /// </summary>
        protected const string SocialNetwork = "socialnetwork";

        /// <summary>
        /// Name of MediaMicroservices service.
        /// </summary>
        protected const string MediaMicroservices = "mediamicroservices";

        /// <summary>
        /// Name of hotel reservation service.
        /// </summary>
        protected const string HotelReservation = "hotelreservation";

        private IFileSystem fileSystem;
        private static readonly object LockObject = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="DeathStarBenchExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public DeathStarBenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
        }

        /// <summary>
        /// Client used to communicate with the locally self-hosted instance of the
        /// Virtual Client API.
        /// </summary>
        public IApiClient LocalApiClient { get; set; }

        /// <summary>
        /// Client used to communicate with the hosted instance of the
        /// Virtual Client API at server side.
        /// </summary>
        public IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// Service Name. ex: socialNetwork, mediaMicroservices, hotelReservation.
        /// </summary>
        public string ServiceName
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DeathStarBenchExecutor.Scenario), out IConvertible serviceName);
                return serviceName?.ToString();
            }

            set
            {
                this.Parameters[nameof(this.ServiceName)] = value;
            }
        }

        /// <summary>
        /// Command required for joining swarm network as worker/client.
        /// </summary>
        public State SwarmCommand { get; set; }

        /// <summary>
        /// Parameter defines the timeout to use when polling the server-side API for state changes.
        /// </summary>
        public static TimeSpan StateConfirmationPollingTimeout { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Time taken by server to have services up and running.
        /// </summary>
        public static TimeSpan ServerWarmUpTime { get; set; } = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Cancellation Token Source for Server.
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// Path to DeathStarBench Benchmark Package.
        /// </summary>
        protected string PackageDirectory { get; set; }

        /// <summary>
        /// Path to Service in DeathStarBench Benchmark Package.
        /// </summary>
        protected string ServiceDirectory { get; set; }

        /// <summary>
        /// Path to scripts.
        /// </summary>
        protected string ScriptsDirectory { get; set; }

        /// <summary>
        /// An interface that can be used to communicate with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManager => this.Dependencies.GetService<ISystemManagement>();

        /// <summary>
        /// Initializes the environment for execution of the DeathStarBench workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.ThrowIfPlatformIsNotSupported();

            await this.CheckDistroSupportAsync(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            this.InitializeApiClients();

            if (this.IsMultiRoleLayout())
            {
                ClientInstance clientInstance = this.GetLayoutClientInstance();
                string layoutIPAddress = clientInstance.IPAddress;

                this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);
                this.ThrowIfRoleNotSupported(clientInstance.Role);
            }

            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath workloadPackage = await packageManager.GetPlatformSpecificPackageAsync(
                                                    this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                                                    .ConfigureAwait(false);

            this.PackageDirectory = workloadPackage.Path;
            this.ScriptsDirectory = this.PlatformSpecifics.Combine(this.PackageDirectory, "Scripts");

            await this.InstallDependenciesAsync(cancellationToken)
                .ConfigureAwait(false);

            this.ServiceDirectory = this.PlatformSpecifics.Combine(this.PackageDirectory, this.ServiceName);
        }

        /// <summary>
        /// Execute steps for DeathStarBench.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    if (!this.IsMultiRoleLayout() || this.IsInRole(ClientRole.Server))
                    {
                        if (this.IsMultiRoleLayout())
                        {
                            try
                            {
                                // Subscribe to notifications from the Events API. The client passes instructions
                                // to the server via this API.
                                this.Logger.LogTraceMessage("subscribing to the listeners");
                                VirtualClientEventing.ReceiveInstructions += this.OnInstructionsReceived;
                                VirtualClientEventing.SetEventingApiOnline(true);

                                await this.WaitAsync(this.ServerCancellationSource.Token)
                                    .ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                            {
                                // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                            }
                            finally
                            {
                                // Cleanup the event subscription to avoid any issues with memory leaks.
                                this.Logger.LogTraceMessage("unsubscribing to the listeners");
                                VirtualClientEventing.ReceiveInstructions -= this.OnInstructionsReceived;
                                VirtualClientEventing.SetEventingApiOnline(false);
                            }
                        }
                        else
                        {
                            using (var serverExecutor = this.CreateWorkloadServer())
                            {
                                await serverExecutor.ExecuteAsync(this.ServerCancellationSource.Token)
                                    .ConfigureAwait(false);

                                this.Logger.LogMessage($"{nameof(DeathStarBenchExecutor)}.ServerExecutionCompleted", telemetryContext);
                            }
                        }
                    }

                    if (!this.IsMultiRoleLayout() || this.IsInRole(ClientRole.Client))
                    {
                        using (var clientExecutor = this.CreateWorkloadClient())
                        {
                            await clientExecutor.ExecuteAsync(this.ServerCancellationSource.Token)
                                .ConfigureAwait(false);

                            this.Logger.LogMessage($"{nameof(DeathStarBenchExecutor)}.ClientExecutionCompleted", telemetryContext);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                    this.Logger.LogMessage($"{nameof(DeathStarBenchExecutor)}.Canceled", telemetryContext);
                }
                catch (Exception)
                {
                    this.Logger.LogMessage($"{nameof(DeathStarBenchExecutor)} error occured when executing {this.Scenario} scenario", telemetryContext);
                }
            }
        }

        /// <summary>
        ///  On executes receiving instructions from client.
        /// </summary>
        /// <param name="sender">Object that sends is responsible for event to happen.</param>
        /// <param name="instructions">Instructions that are passed from sender when an event occurs.</param>
        protected void OnInstructionsReceived(object sender, JObject instructions)
        {
            lock (DeathStarBenchExecutor.LockObject)
            {
                try
                {
                    EventContext telemetryContext = EventContext.Persisted()
                        .AddContext("instructions", instructions);

                    if (VirtualClientEventing.IsApiOnline)
                    {
                        this.Logger.LogMessageAsync($"{nameof(DeathStarBenchExecutor)}.InstructionsReceived", telemetryContext, async () =>
                        {
                            CancellationToken cancellationToken = this.ServerCancellationSource.Token;

                            Item<Instructions> notification = instructions.ToObject<Item<Instructions>>();
                            Instructions workloadInstructions = notification.Definition;
                            EventContext relatedContext = EventContext.Persisted();

                            if (workloadInstructions.Type == InstructionsType.ClientServerReset)
                            {
                                this.Logger.LogTraceMessage($"Synchronization: Stopping all workloads...");
                                await this.StopDockerAsync(CancellationToken.None).ConfigureAwait(false);

                                this.DeleteWorkloadStateAsync(relatedContext, cancellationToken).GetAwaiter().GetResult();
                            }
                            else if (workloadInstructions.Type == InstructionsType.ClientServerStartExecution)
                            {
                                await this.StopDockerAsync(CancellationToken.None).ConfigureAwait(false);

                                this.DeleteWorkloadStateAsync(relatedContext, cancellationToken).GetAwaiter().GetResult();

                                this.Parameters["Scenario"] = workloadInstructions.Properties["ServiceName"];

                                var serverExecutor = this.CreateWorkloadServer();
                                this.Logger.LogTraceMessage($"Synchronization: Starting {this.ServiceName} workload...");

                                await serverExecutor.ExecuteAsync(cancellationToken);

                                // create the state here.
                                DeathStarBenchState expectedServerState = new DeathStarBenchState(this.ServiceName, true);
                                await this.LocalApiClient.GetOrCreateStateAsync(nameof(DeathStarBenchState), expectedServerState, cancellationToken)
                                        .ConfigureAwait(false);
                            }
                        });
                    }
                }
                catch
                {
                    // We should not surface exceptions that cause the eventing system
                    // issues.
                }
            }
        }

        /// <summary>
        /// Poll the server until the state is deleted/not found.
        /// </summary>
        /// <param name="client">The API client instance.</param>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="timeout">The period of time for which the client tries to get the state before timing out. </param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the state object/definition at last poll call.
        /// </returns>
        protected static async Task<HttpResponseMessage> PollUntilStateDeletedAsync(IApiClient client, string stateId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            DateTime pollingTimeout = DateTime.UtcNow.Add(timeout);
            HttpResponseMessage response = null;
            bool stateStillExists = true;

            do
            {
                try
                {
                    response = await client.GetStateAsync(stateId, cancellationToken).ConfigureAwait(false);
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        stateStillExists = false;
                    }
                }
                catch
                {
                    // State not available on server yet.
                }
                finally
                {
                    if (stateStillExists)
                    {
                        if (DateTime.UtcNow >= pollingTimeout)
                        {
                            throw new WorkloadException(
                                $"Polling for deletion of state '{stateId}' timed out (timeout={timeout}).",
                                ErrorReason.ApiStatePollingTimeout);
                        }

                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            while (stateStillExists && !cancellationToken.IsCancellationRequested);

            return response;
        }

        /// <summary>
        /// Deletes the existing states.
        /// </summary>
        /// <param name="telemetryContext">Provides context information to include with telemetry events emitted.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected Task DeleteWorkloadStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(DeathStarBenchExecutor)}.ResetState", telemetryContext, async () =>
            {
                HttpResponseMessage response = await this.LocalApiClient.DeleteStateAsync(
                                                            nameof(DeathStarBenchState),
                                                            cancellationToken).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    response.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
                }

                HttpResponseMessage swarmCommandResponse = await this.LocalApiClient.DeleteStateAsync(
                                                                       nameof(DeathStarBenchExecutor.SwarmCommand),
                                                                       cancellationToken).ConfigureAwait(false);

                if (swarmCommandResponse.StatusCode != HttpStatusCode.NoContent)
                {
                    swarmCommandResponse.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
                }
            });
        }

        /// <summary>
        /// Create the DeathStarBench workload client executor.
        /// </summary>
        protected virtual VirtualClientComponent CreateWorkloadClient()
        {
            return new DeathStarBenchClientExecutor(this.Dependencies, this.Parameters);
        }

        /// <summary>
        /// Create the DeathStarBench workload server executor.
        /// </summary>
        protected virtual VirtualClientComponent CreateWorkloadServer()
        {
            return new DeathStarBenchServerExecutor(this.Dependencies, this.Parameters);
        }

        /// <summary>
        /// Returns true/false whether the component should execute on the system/platform.
        /// </summary>
        /// <returns>Returns True or false</returns>
        protected override bool IsSupported()
        {
            bool isSupported = this.Platform == PlatformID.Unix;

            return isSupported;
        }

        /// <summary>
        /// Executes the commands.
        /// </summary>
        /// <param name="command">Command that needs to be executed</param>
        /// <param name="workingDirectory">The directory where we want to execute the command</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>Output of the workload command.</returns>
        protected async Task<string> ExecuteCommandAsync<TExecutor>(string command, string workingDirectory, CancellationToken cancellationToken)
            where TExecutor : VirtualClientComponent
        {
            string output = string.Empty;
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{command}'  at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", command);

                await this.Logger.LogMessageAsync($"{typeof(TExecutor).Name}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.SystemManager.ProcessManager.CreateElevatedProcess(this.Platform, command, null, workingDirectory))
                    {
                        SystemManagement.CleanupTasks.Add(() => process.SafeKill());
                        process.RedirectStandardOutput = true;
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogProcessDetails<TExecutor>(process, telemetryContext);
                            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                        }

                        output = process.StandardOutput.ToString();
                    }

                    return output;
                }).ConfigureAwait(false);
            }

            return output;
        }

        /// <summary>
        /// Resets the file to empty file.
        /// </summary>
        /// <param name="filePath">Path to the file to reset</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events emitted.</param>
        protected void ResetFile(string filePath, EventContext telemetryContext)
        {
            try
            {
                this.fileSystem.File.WriteAllText(filePath, string.Empty);
            }
            catch (IOException exc)
            {
                this.Logger.LogErrorMessage(exc, telemetryContext);
            }
        }

        /// <summary>
        /// Stopping docker services after a service is executed for freeing up the ports for next service to be executed.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected async Task StopDockerAsync(CancellationToken cancellationToken)
        {
            if (this.IsMultiRoleLayout())
            {
                string isSwarmNodeScriptPath = this.PlatformSpecifics.Combine(this.ScriptsDirectory, "isSwarmNode.sh");
                string isSwarmNodeCommand = "bash " + isSwarmNodeScriptPath;
                string isSwarmNode = await this.ExecuteCommandAsync<DeathStarBenchExecutor>(isSwarmNodeCommand, this.PackageDirectory, cancellationToken)
                    .ConfigureAwait(false);
                if (isSwarmNode.Trim('\n') == "true")
                {
                    this.Logger.LogTraceMessage($"{isSwarmNode.Trim('\n')} is equal to true");
                    if (this.IsInRole(ClientRole.Client))
                    {
                        await this.ExecuteCommandAsync<DeathStarBenchExecutor>("docker swarm leave", this.ServiceDirectory, cancellationToken)
                            .ConfigureAwait(false);
                    }

                    if (this.IsInRole(ClientRole.Server))
                    {
                        await this.ExecuteCommandAsync<DeathStarBenchExecutor>("docker swarm leave --force", this.ServiceDirectory, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }

                // Docker services takes some time to get released.
                do
                {
                    await this.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
                    isSwarmNode = await this.ExecuteCommandAsync<DeathStarBenchExecutor>(isSwarmNodeCommand, this.PackageDirectory, cancellationToken)
                    .ConfigureAwait(false);
                }
                while (isSwarmNode.Trim('\n') == "true");
            }
            else
            {
                string dockerProcessCountCommand = @"bash -c ""docker ps | wc -l""";
                string numberOfDockerProcess = await this.ExecuteCommandAsync<DeathStarBenchExecutor>(dockerProcessCountCommand, this.PackageDirectory, cancellationToken)
                    .ConfigureAwait(false);

                if (int.Parse(numberOfDockerProcess) > 1)
                {
                    string stopContainersScriptPath = this.PlatformSpecifics.Combine(this.ScriptsDirectory, "stopContainers.sh");
                    string stopContainersCommand = "bash " + stopContainersScriptPath;
                    await this.ExecuteCommandAsync<DeathStarBenchExecutor>(stopContainersCommand, this.ServiceDirectory, cancellationToken)
                    .ConfigureAwait(false);
                }
            }
        }

        private void InitializeApiClients()
        {
            IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();
            this.LocalApiClient = clientManager.GetOrCreateApiClient(IPAddress.Loopback.ToString(), IPAddress.Loopback);
            bool isSingleVM = !this.IsMultiRoleLayout();

            if (isSingleVM)
            {
                this.ServerApiClient = this.LocalApiClient;
            }
            else
            {
                ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                this.ServerApiClient = clientManager.GetOrCreateApiClient(serverIPAddress.ToString(), serverIPAddress);
            }
        }

        /// <summary>
        /// Install required dependencies like dockerCompose, pip installations.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        private async Task InstallDependenciesAsync(CancellationToken cancellationToken)
        {
            string dockerComposeScriptPath = this.PlatformSpecifics.Combine(this.ScriptsDirectory, "dockerComposeScript.sh");
            string dockerComposeCommand = "bash " + dockerComposeScriptPath;
            await this.ExecuteCommandAsync<DeathStarBenchExecutor>(dockerComposeCommand, this.PackageDirectory, cancellationToken)
                .ConfigureAwait(false);

            string dockerComposeFilePath = "/usr/local/bin/docker-compose";
            await this.SystemManager.MakeFileExecutableAsync(dockerComposeFilePath, this.Platform, cancellationToken)
                    .ConfigureAwait(false);

            // python3-pip installs pip3 and not pip, better to leave it on python which version of pip to use
            string pipInstallPackagesCommand = "python3 -m pip install aiohttp asyncio";
            await this.ExecuteCommandAsync<DeathStarBenchExecutor>(pipInstallPackagesCommand, this.PackageDirectory, cancellationToken)
                .ConfigureAwait(false);

            string luarocksCommand = "luarocks install luasocket";
            await this.ExecuteCommandAsync<DeathStarBenchExecutor>(luarocksCommand, this.PackageDirectory, cancellationToken)
                .ConfigureAwait(false);

        }

        private void ThrowIfPlatformIsNotSupported()
        {
            if (this.Platform != PlatformID.Unix)
            {
                throw new WorkloadException(
                    $"'{this.Platform.ToString()}' is not currently supported, only {PlatformID.Unix} is currently supported",
                    ErrorReason.PlatformNotSupported);
            }
        }

        private async Task CheckDistroSupportAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                var linuxDistributionInfo = await this.SystemManager.GetLinuxDistributionAsync(cancellationToken)
                .ConfigureAwait(false);

                switch (linuxDistributionInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                        break;
                    default:
                        throw new WorkloadException(
                            $"The DeathStarBench benchmark workload is not supported on the current Linux distro - " +
                            $"{linuxDistributionInfo.LinuxDistribution.ToString()} through Virtual Client.  Supported distros include:" +
                            $" Ubuntu ",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
        }
    }

    /// <summary>
    /// DeathStarBench Possible States.
    /// </summary>
    public class DeathStarBenchState : State
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeathStarBenchState"/> class.
        /// </summary>
        public DeathStarBenchState(
            string scenario,
            bool serviceState)
        {
            scenario.ThrowIfNull(nameof(scenario));
            serviceState.ThrowIfNull(nameof(serviceState));

            this.Properties[nameof(this.ServiceName)] = scenario;
            this.Properties[nameof(this.ServiceState)] = serviceState;
        }

        /// <summary>
        /// Workload/action scenario/Service Name
        /// </summary>
        public string ServiceName
        {
            get
            {
                return this.Properties.GetValue<string>(nameof(this.ServiceName));
            }

            set
            {
                this.Properties[nameof(this.ServiceName)] = value;
            }
        }

        /// <summary>
        /// Workload/action scenario/Service State(True or False representing Start or stop respectively).
        /// </summary>
        public bool ServiceState
        {
            get
            {
                return this.Properties.GetValue<bool>(nameof(this.ServiceState));
            }

            set
            {
                this.Properties[nameof(this.ServiceState)] = value;
            }
        }
    }

}
