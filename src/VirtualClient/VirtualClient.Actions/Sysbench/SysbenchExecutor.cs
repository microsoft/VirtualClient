// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The Sysbench workload executor.
    /// </summary>
    public class SysbenchExecutor : VirtualClientComponent
    {
        private readonly IStateManager stateManager;

        /// <summary>
        /// Constructor for <see cref="SysbenchExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SysbenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.stateManager = this.SystemManager.StateManager;
            // Supported roles for this client/server workload.
            this.SupportedRoles = new List<string>
            {
                ClientRole.Client,
                ClientRole.Server
            };
        }

        /// <summary>
        /// Parameter defines the scenario to use for the MySQL user accounts used
        /// to create the DB and run transactions against it.
        /// </summary>
        public string DatabaseScenario
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DatabaseScenario), SysbenchScenario.Default);
            }
        }

        /// <summary>
        /// Number of records per table.
        /// </summary>
        public int RecordCount
        {
            get
            {
                int numRecords = 1;

                // default formulaic setup of the database
                // records & threads depend on the core count
                if (this.Parameters.TryGetValue(nameof(SysbenchExecutor.RecordCount), out IConvertible recordCount)
                    && this.DatabaseScenario == SysbenchScenario.Default)
                {
                    numRecords = recordCount.ToInt32(CultureInfo.InvariantCulture);
                }
                else
                {
                    CpuInfo cpuInfo = this.SystemManager.GetCpuInfoAsync(CancellationToken.None).GetAwaiter().GetResult();
                    int coreCount = cpuInfo.LogicalProcessorCount;

                    int recordCountExponent = this.DatabaseScenario == SysbenchScenario.Balanced
                        ? (int)Math.Log2(coreCount)
                        : (int)Math.Log2(coreCount) + 2;

                    numRecords = (int)Math.Pow(10, recordCountExponent);
                }

                return numRecords;
            }
        }

        /// <summary>
        /// The database name option passed to Sysbench.
        /// </summary>
        public string DatabaseName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchClientExecutor.DatabaseName), "sbtest");
            }
        }

        /// <summary>
        /// Client used to communicate with the hosted instance of the
        /// Virtual Client API at server side.
        /// </summary>
        public IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// Cancellation Token Source for Server.
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// Server IpAddress on which MySQL Server runs.
        /// </summary>
        protected string ServerIpAddress { get; set; }

        /// <summary>
        /// Server IpAddress on which the client runs.
        /// </summary>
        protected string ClientIpAddress { get; set; }

        /// <summary>
        /// Sysbench package location
        /// </summary>
        protected string SysbenchPackagePath { get; set; }

        /// <summary>
        /// An interface that can be used to communicate with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManager => this.Dependencies.GetService<ISystemManagement>();

        /// <summary>
        /// Executes the Sysbench workload.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes the environment for execution of the Sysbench workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.CheckDistroSupportAsync(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.InitializeExecutablesAsync(telemetryContext, cancellationToken);

            this.InitializeApiClients(telemetryContext, cancellationToken);

            if (this.IsMultiRoleLayout())
            {
                ClientInstance clientInstance = this.GetLayoutClientInstance();
                string layoutIPAddress = clientInstance.IPAddress;

                this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);
                this.ThrowIfRoleNotSupported(clientInstance.Role);
            }
        }

        /// <summary>
        /// Initializes API client.
        /// </summary>
        protected void InitializeApiClients(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();

                if (!this.IsMultiRoleLayout())
                {
                    this.ServerIpAddress = IPAddress.Loopback.ToString();
                    this.ServerApiClient = clientManager.GetOrCreateApiClient(this.ServerIpAddress, IPAddress.Loopback);
                }
                else
                {
                    ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                    IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                    this.ServerIpAddress = serverIPAddress.ToString();
                    this.ServerApiClient = clientManager.GetOrCreateApiClient(this.ServerIpAddress, serverIPAddress);
                    this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", this.ServerApiClient);

                    ClientInstance clientInstance = this.GetLayoutClientInstances(ClientRole.Client).First();
                    IPAddress.TryParse(clientInstance.IPAddress, out IPAddress clientIPAddress);

                    this.ClientIpAddress = clientIPAddress.ToString();
                }
            }
        }

        /// <summary>
        /// Initializes the workload executables on the system (e.g. attributes them as executable).
        /// </summary>
        protected async Task InitializeExecutablesAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // store state with initialization status & record/table counts, if does not exist already

            SysbenchState state = await this.stateManager.GetStateAsync<SysbenchState>(nameof(SysbenchState), cancellationToken)
                ?? new SysbenchState();

            if (!state.SysbenchInitialized)
            {
                string sysbenchDirectory = this.GetPackagePath(this.PackageName);

                List<string> sysbenchInitCommands = new List<string>()
                {
                    $"sed -i \"s/CREATE TABLE/CREATE TABLE IF NOT EXISTS/g\" {sysbenchDirectory}/src/lua/oltp_common.lua",
                    "./autogen.sh",
                    "./configure",
                    "make -j",
                    "make install"
                };

                foreach (string command in sysbenchInitCommands)
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        command,
                        null,
                        sysbenchDirectory,
                        telemetryContext,
                        cancellationToken,
                        runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "SysbenchExecutor", logToFile: true);
                            process.ThrowIfDependencyInstallationFailed();
                        }
                    }
                }

                state.SysbenchInitialized = true;
            }
           
            await this.stateManager.SaveStateAsync<SysbenchState>(nameof(SysbenchState), state, cancellationToken);
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = base.IsSupported()
                && (this.Platform == PlatformID.Unix)
                && (this.CpuArchitecture == Architecture.X64 || this.CpuArchitecture == Architecture.Arm64);

            if (!isSupported)
            {
                this.Logger.LogNotSupported("Sysbench", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
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
                                $"The Sysbench OLTP workload is not supported on the current Linux distro - " +
                                $"{linuxDistributionInfo.LinuxDistribution}.  Supported distros include:" +
                                $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Ubuntu)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Debian)}",
                                ErrorReason.LinuxDistributionNotSupported);
                    }
                }
            }
        }

        internal class SysbenchState : State
        {
            public SysbenchState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool SysbenchInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(SysbenchState.SysbenchInitialized), false);
                }

                set
                {
                    this.Properties[nameof(SysbenchState.SysbenchInitialized)] = value;
                }
            }

            public bool DatabasePopulated
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(SysbenchState.DatabasePopulated), false);
                }

                set
                {
                    this.Properties[nameof(SysbenchState.DatabasePopulated)] = value;
                }
            }
        }

        /// <summary>
        /// Defines the Sysbench OLTP benchmark scenario.
        /// </summary>
        internal class SysbenchScenario
        {
            public const string Balanced = nameof(Balanced);

            public const string InMemory = nameof(InMemory);

            public const string Default = nameof(Default);
        }
    }
}