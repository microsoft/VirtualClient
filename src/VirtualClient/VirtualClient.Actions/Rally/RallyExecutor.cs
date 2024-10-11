// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The Rally workload executor.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class RallyExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Command used to run the python scripts in the ES-Rally package
        /// </summary>
        protected const string PythonCommand = "python3";

        /// <summary>
        /// Constructor for <see cref="RallyExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public RallyExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.SupportedRoles = new List<string>
            {
                ClientRole.Client,
                ClientRole.Server
            };
        }

        /// <summary>
        /// Disk filter specified
        /// </summary>
        public string DiskFilter
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DiskFilter), "osdisk:false&biggestsize");
            }
        }

        /// <summary>
        /// Client used to communicate with the hosted instance of the
        /// Virtual Client API at server side.
        /// </summary>
        public IEnumerable<IApiClient> ServerApiClients { get; set; }

        /// <summary>
        /// Client IpAddress that coordinates the workload.
        /// </summary>
        protected string ClientIpAddress { get; set; }

        /// <summary>
        /// Current user
        /// </summary>
        protected string CurrentUser { get; set; }

        /// <summary>
        /// Data directory path
        /// </summary>
        protected string DataDirectory { get; set; }

        /// <summary>
        /// Rally package location
        /// </summary>
        protected string RallyPackagePath { get; set; }

        /// <summary>
        /// Cancellation Token Source for Server.
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// Manages the state of the system.
        /// </summary>
        protected IStateManager StateManager => this.Dependencies.GetService<IStateManager>();

        /// <summary>
        /// An interface that can be used to communicate with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManager => this.Dependencies.GetService<ISystemManagement>();

        /// <summary>
        /// Initializes the environment for execution of the Rally workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.CheckDistroSupportAsync(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            // Rally Package Setup

            DependencyPath package = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken)
                .ConfigureAwait(false);

            package.ThrowIfNull(this.PackageName);
            this.RallyPackagePath = package.Path;

            // Initializing Server(s) API clients.

            this.InitializeApiClients(telemetryContext, cancellationToken);

            await this.InstallElasticsearchRallyWorkload(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            // Important Configuration Variables: User must own the DataDirectory

            this.CurrentUser = this.SystemManager.GetLoggedInUserName();
            this.DataDirectory = await this.GetDataDirectoryAsync(cancellationToken)
                .ConfigureAwait(false);

            // To run Rally, $PATH must contain ~/.local/bin.

            string homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string localDirectory = this.PlatformSpecifics.Combine(homeDirectory, ".local", "bin");
            this.SetEnvironmentVariable(EnvironmentVariable.PATH, localDirectory, append: true);
        }

        /// <summary>
        /// Not Implemented.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes API client.
        /// </summary>
        protected void InitializeApiClients(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();
                this.ServerApiClients = new List<IApiClient>();

                if (!this.IsMultiRoleLayout())
                {
                    this.ClientIpAddress = IPAddress.Loopback.ToString();
                }
                else
                {
                    // Ensure that instance exists in the layout.

                    ClientInstance instance = this.GetLayoutClientInstance();
                    string layoutIPAddress = instance.IPAddress;

                    this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);
                    this.ThrowIfRoleNotSupported(instance.Role);

                    // Setup client instance.

                    ClientInstance clientInstance = this.GetLayoutClientInstances(ClientRole.Client).First();

                    IPAddress.TryParse(clientInstance.IPAddress, out IPAddress clientIpAddress);
                    this.ClientIpAddress = clientIpAddress.ToString();

                    // Setting up serverApiClients.

                    IEnumerable<ClientInstance> serverInstances = this.GetLayoutClientInstances(ClientRole.Server);

                    foreach (ClientInstance serverInstance in serverInstances)
                    {
                        IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                        IApiClient apiClient = clientManager.GetOrCreateApiClient(serverIPAddress.ToString(), serverIPAddress);
                        this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", apiClient);

                        this.ServerApiClients.Append(apiClient);
                    }
                }
            }
        }

        /// <summary>
        /// Grabs the available data directory on the system.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="WorkloadException"></exception>
        protected async Task<string> GetDataDirectoryAsync(CancellationToken cancellationToken)
        {
            string diskPath = string.Empty;

            if (!cancellationToken.IsCancellationRequested)
            {
                IEnumerable<Disk> disks = await this.SystemManager.DiskManager.GetDisksAsync(cancellationToken)
                        .ConfigureAwait(false);

                if (disks?.Any() != true)
                {
                    throw new WorkloadException(
                        "Unexpected scenario. The disks defined for the system could not be properly enumerated.",
                        ErrorReason.WorkloadUnexpectedAnomaly);
                }

                IEnumerable<Disk> disksToTest = DiskFilters.FilterDisks(disks, this.DiskFilter, this.Platform).ToList();

                if (disksToTest?.Any() != true)
                {
                    throw new WorkloadException(
                        "Expected disks to test not found. Given the parameters defined for the profile action/step or those passed " +
                        "in on the command line, the requisite disks do not exist on the system or could not be identified based on the properties " +
                        "of the existing disks.",
                        ErrorReason.DependencyNotFound);
                }

                diskPath = $"{disksToTest.First().GetPreferredAccessPath(this.Platform)}";
            }

            return diskPath;
        }

        private async Task InstallElasticsearchRallyWorkload(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.Logger.LogMessageAsync($"{this.TypeName}.InstallWorkload", telemetryContext.Clone(), async () =>
            {
                RallyState state = await this.StateManager.GetStateAsync<RallyState>(nameof(RallyState), cancellationToken)
                    ?? new RallyState();

                if (!state.RallyInitialized)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // install.py simply upgrades pip and installs Rally v2.9.0

                        string arguments = $"{this.RallyPackagePath}/install.py";

                        using (IProcessProxy process = await this.ExecuteCommandAsync(
                            RallyExecutor.PythonCommand,
                            arguments,
                            this.RallyPackagePath,
                            telemetryContext,
                            cancellationToken))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "ElasticsearchRally", logToFile: true);
                                process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                            }
                        }

                        state.RallyInitialized = true;
                        await this.StateManager.SaveStateAsync<RallyState>(nameof(RallyState), state, cancellationToken);
                    }
                }
            });

            return;
        }

        private async Task CheckDistroSupportAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                var linuxDistributionInfo = await this.SystemManager.GetLinuxDistributionAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    switch (linuxDistributionInfo.LinuxDistribution)
                    {
                        case LinuxDistribution.Ubuntu:
                        case LinuxDistribution.Debian:
                            break;
                        default:
                            throw new WorkloadException(
                                $"The ES-Rally workload is not supported on the current Linux distro - " +
                                $"{linuxDistributionInfo.LinuxDistribution}.  Supported distros include:" +
                                $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Ubuntu)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Debian)}",
                                ErrorReason.LinuxDistributionNotSupported);
                    }
                }
            }
            else
            {
                throw new WorkloadException(
                $"The ES-Rally workload is not supported on the current platform.",
                ErrorReason.PlatformNotSupported);
            }
        }

        internal class RallyState : State
        {
            public RallyState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool RallyInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(RallyState.RallyInitialized), false);
                }

                set
                {
                    this.Properties[nameof(RallyState.RallyInitialized)] = value;
                }
            }

            public bool RallyConfigured
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(RallyState.RallyConfigured), false);
                }

                set
                {
                    this.Properties[nameof(RallyState.RallyConfigured)] = value;
                }
            }
        }
    }
}
