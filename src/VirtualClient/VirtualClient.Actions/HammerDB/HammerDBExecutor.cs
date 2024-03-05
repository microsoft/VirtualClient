// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.SysbenchExecutor;

    /// <summary>
    /// PostgreSQL Executor
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class HammerDBExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Default table count for a Sysbench run.
        /// </summary>
        public const int DefaultTableCount = 10;

        internal const string CreateDBTclName = "createDB.tcl";
        internal const string RunTransactionsTclName = "runTransactions.tcl";
        private readonly IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="HammerDBExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public HammerDBExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.SupportedRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ClientRole.Client,
                ClientRole.Server
            };

            this.stateManager = this.SystemManager.StateManager;
        }

        /// <summary>
        /// Defines the name of the PostgreSQL database to create/use for the transactions.
        /// </summary>
        public string DatabaseName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DatabaseName));
            }
        }

        /// <summary>
        /// Parameter defines the scenario to use for the PostgreSQL user accounts used
        /// to create the DB and run transactions against it.
        /// </summary>
        public string DatabaseScenario
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DatabaseScenario), HammerDBScenario.Balanced);
            }
        }

        /// <summary>
        /// Parameter defines the port number on which the PostgreSQL server will run on.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port));
            }
        }

        /// <summary>
        /// Number of records per table.
        /// </summary>
        public int? RecordCount
        {
            get
            {
                this.Parameters.TryGetValue(nameof(SysbenchExecutor.RecordCount), out IConvertible records);
                return records?.ToInt32(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// The table count passed to HammerDB.
        /// </summary>
        public int? TableCount
        {
            get
            {
                this.Parameters.TryGetValue(nameof(SysbenchClientExecutor.TableCount), out IConvertible tableCount);
                return tableCount?.ToInt32(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Number of threads.
        /// </summary>
        public int? Threads
        {
            get
            {
                this.Parameters.TryGetValue(nameof(SysbenchClientExecutor.Threads), out IConvertible threads);
                return threads?.ToInt32(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// The workload option passed to Sysbench.
        /// </summary>
        public string Workload
        {
            get
            {
                this.Parameters.TryGetValue(nameof(SysbenchClientExecutor.Workload), out IConvertible workload);
                return workload?.ToString();
            }
        }

        /// <summary>
        /// Client used to communicate with the hosted instance of the
        /// Virtual Client API at server side.
        /// </summary>
        protected IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// Server's Cancellation Token Source.
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// Server IpAddress on which MySQL Server runs.
        /// </summary>
        protected string ServerIpAddress { get; set; }

        /// <summary>
        /// The file path where Hammer DB package is downloaded.
        /// </summary>
        protected string HammerDBPackagePath { get; set; }

        /// <summary>
        /// Provides methods for managing system requirements.
        /// </summary>
        protected ISystemManagement SystemManager { get; }

        /// <inheritdoc/>
        /// <summary>
        /// Method to determine the table count for the given run.
        /// </summary>
        /// <returns></returns>
        public static int GetTableCount(string databaseScenario, int? tables)
        {
            int tableCount = tables.GetValueOrDefault(DefaultTableCount);

            // if not using the configurable scenario, must use 10 tables
            // if using a workload that must use only 1 table, table count adjusted as such 

            tableCount = (databaseScenario == SysbenchScenario.Configure) ? tableCount : DefaultTableCount;
            return tableCount;
        }

        /// <summary>
        /// Method to determine the record count for the given run.
        /// </summary>
        /// <returns></returns>
        public static int GetRecordCount(ISystemManagement systemManagement, string databaseScenario, int? records)
        {
            CpuInfo cpuInfo = systemManagement.GetCpuInfoAsync(CancellationToken.None).GetAwaiter().GetResult();
            int coreCount = cpuInfo.LogicalProcessorCount;

            // record count calcuated for default use is 10^n where n = core count
            // for the in memory scenario, it is n+2

            int recordCountExponent = (databaseScenario != SysbenchScenario.InMemory)
                ? (int)Math.Log2(coreCount)
                : (int)Math.Log2(coreCount) + 2;

            int recordEstimate = (int)Math.Pow(10, recordCountExponent);

            int recordCount = records.GetValueOrDefault(recordEstimate);

            // record count specified in profile if it is the configurable scenario
            // if the record count specified is 1, assume it's pre-initialization, and do not use estimate

            recordCount = (databaseScenario == SysbenchScenario.Configure || recordCount == 1) ? recordCount : recordEstimate;

            return recordCount;
        }

        /// <summary>
        /// Method to determine the thread count for the given run.
        /// </summary>
        /// <returns></returns>
        public static int GetThreadCount(ISystemManagement systemManagement, string databaseScenario, int? threads)
        {
            CpuInfo cpuInfo = systemManagement.GetCpuInfoAsync(CancellationToken.None).GetAwaiter().GetResult();
            int coreCount = cpuInfo.LogicalProcessorCount;

            // number of threads defaults to core count

            int threadCount = threads.GetValueOrDefault(coreCount);
            threadCount = (databaseScenario == SysbenchScenario.Configure) ? threadCount : coreCount;

            return threadCount;
        }

        /// <inheritdoc/>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.CheckDistroSupportAsync(cancellationToken)
                .ConfigureAwait(false);

            await HammerDBExecutor.OpenFirewallPortsAsync(this.Port, this.SystemManager.FirewallManager, cancellationToken);

            DependencyPath hammerDBPackage = await this.GetPackageAsync(this.PackageName, cancellationToken).ConfigureAwait(false);
            this.HammerDBPackagePath = hammerDBPackage.Path;

            await this.InitializeExecutablesAsync(telemetryContext, cancellationToken);

            this.InitializeApiClients(cancellationToken);

            // The *.vcpkg definition is expected to contain definitions specific to each platform/architecture
            // where the PostgreSQL application is installed.
            // 
            // e.g.
            // "metadata": {
            //   "installationPath-linux-x64": "/etc/postgresql/14/main",
            //   "installationPath-linux-arm64": "/etc/postgresql/14/main",
            //   "installationPath-windows-x64": "C:\\Program Files\\PostgreSQL\\14",
            //   "installationPath-windows-arm64": "C:\\Program Files\\PostgreSQL\\14",
            // }
            string metadataKey = $"{PackageMetadata.InstallationPath}-{this.PlatformArchitectureName}";
            if (!hammerDBPackage.Metadata.TryGetValue(metadataKey, out IConvertible installationPath))
            {
                throw new WorkloadException(
                    $"Missing installation path. The '{this.PackageName}' package registration is missing the required '{metadataKey}' " +
                    $"metadata definition. This is required in order to execute PostgreSQL operations from the location where the software is installed.",
                    ErrorReason.DependencyNotFound);
            }

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
        protected void InitializeApiClients(CancellationToken cancellationToken)
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
                }
            }
        }

        /// <summary>
        /// Initializes the workload executables on the system (e.g. attributes them as executable).
        /// </summary>
        protected async Task InitializeExecutablesAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // store state with initialization status & record/table counts, if does not exist already

            HammerDBState state = await this.stateManager.GetStateAsync<HammerDBState>(nameof(HammerDBState), cancellationToken)
                ?? new HammerDBState();

            if (!state.HammerDBInitialized)
            {
                LinuxDistributionInfo distributionInfo = await this.SystemManager.GetLinuxDistributionAsync(cancellationToken)
                    .ConfigureAwait(false);
                string distribution = distributionInfo.LinuxDistribution.ToString();

                string arguments = $"{this.HammerDBPackagePath}/configure-workload-generator.py";

                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    "python3",
                    arguments,
                    this.HammerDBPackagePath,
                    telemetryContext,
                    cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "HammerDBExecutor", logToFile: true);
                        process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                    }
                }

                state.HammerDBInitialized = true;
            }

            await this.stateManager.SaveStateAsync<HammerDBState>(nameof(HammerDBState), state, cancellationToken);
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = base.IsSupported()
                && (this.Platform == PlatformID.Win32NT || this.Platform == PlatformID.Unix)
                && (this.CpuArchitecture == Architecture.X64);

            if (!isSupported)
            {
                this.Logger.LogNotSupported("HammerDB", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        private static Task OpenFirewallPortsAsync(int port, IFirewallManager firewallManager, CancellationToken cancellationToken)
        {
            return firewallManager.EnableInboundConnectionsAsync(
                new List<FirewallEntry>
                {
                    new FirewallEntry(
                        "PostgreSQL: Allow Multiple Machines communications",
                        "Allows individual machine instances to communicate with other machine in client-server scenario",
                        "tcp",
                        new List<int> { port })
                },
                cancellationToken);
        }

        private async Task CheckDistroSupportAsync(CancellationToken cancellationToken)
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
                            $"The PostgreSQL TPCC workload is not supported on the current Linux distro - " +
                            $"{linuxDistributionInfo.LinuxDistribution}. Supported distros include:" +
                            $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Ubuntu)}," +
                            $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Debian)}",
                            ErrorReason.LinuxDistributionNotSupported);
                    }
                }
            }
        }

        internal class HammerDBState : State
        {
            public HammerDBState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool HammerDBInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(HammerDBState.HammerDBInitialized), false);
                }

                set
                {
                    this.Properties[nameof(HammerDBState.HammerDBInitialized)] = value;
                }
            }

            public bool DatabasePopulated
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(HammerDBState.DatabasePopulated), false);
                }

                set
                {
                    this.Properties[nameof(HammerDBState.DatabasePopulated)] = value;
                }
            }
        }

        /// <summary>
        /// Defines the HammerDB scenario.
        /// </summary>
        internal class HammerDBScenario
        {
            public const string Balanced = nameof(Balanced);

            public const string InMemory = nameof(InMemory);

            public const string Configure = nameof(Configure);
        }
    }
}