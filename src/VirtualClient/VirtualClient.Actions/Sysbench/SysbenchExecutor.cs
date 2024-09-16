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
    using System.Security.Cryptography;
    using System.Text;
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
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class SysbenchExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Default table count for a Sysbench run.
        /// </summary>
        public const int DefaultTableCount = 10;

        /// <summary>
        /// Default table count for a 'select' workload type
        /// </summary>
        public const int SelectWorkloadDefaultTableCount = 1;

        private readonly IStateManager stateManager;
        private static readonly string[] SelectWorkloads =
        {
            "select_random_points",
            "select_random_ranges"
        };

        /// <summary>
        /// Constructor for <see cref="SysbenchExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SysbenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.stateManager = this.SystemManager.StateManager;
            this.SupportedRoles = new List<string>
            {
                ClientRole.Client,
                ClientRole.Server
            };
        }

        /// <summary>
        /// The database name option passed to Sysbench.
        /// </summary>
        public string Benchmark
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchClientExecutor.Benchmark));
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
        /// Parameter defines the scenario to use for the MySQL user accounts used
        /// to create the DB and run transactions against it.
        /// </summary>
        public string DatabaseScenario
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DatabaseScenario), SysbenchScenario.Balanced);
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
        /// The workload option passed to Sysbench.
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
        /// Number of records per table.
        /// </summary>
        public string DatabaseSystem
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchClientExecutor.DatabaseSystem));
            }
        }

        /// <summary>
        /// Parameter defines the SuperUser Password for PostgreSQL Server.
        /// </summary>
        public string SuperUserPassword
        {
            get
            {
                byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(this.ExperimentId));
                return Convert.ToBase64String(hashBytes);
            }
        }

        /// <summary>
        /// Number of records per table.
        /// </summary>
        public int? WarehouseCount
        {
            get
            {
                this.Parameters.TryGetValue(nameof(SysbenchExecutor.WarehouseCount), out IConvertible warehouseCount);
                return warehouseCount?.ToInt32(CultureInfo.InvariantCulture);
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
        /// Sysbench package location
        /// </summary>
        protected string SysbenchPackagePath { get; set; }

        /// <summary>
        /// An interface that can be used to communicate with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManager => this.Dependencies.GetService<ISystemManagement>();

        /// <summary>
        /// Method to determine the table count for the given run.
        /// </summary>
        /// <returns></returns>
        public static int GetTableCount(string databaseScenario, int? tables, string? workload)
        {
            int tableCount = tables.GetValueOrDefault(DefaultTableCount);

            // if not using the configurable scenario, must use 10 tables
            // if using a workload that must use only 1 table, table count adjusted as such 

            tableCount = (databaseScenario == SysbenchScenario.Configure) ? tableCount : DefaultTableCount;
            tableCount = (SelectWorkloads.Contains(workload, StringComparer.OrdinalIgnoreCase)) ? SelectWorkloadDefaultTableCount : tableCount;

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

            recordCountExponent = Math.Max(3, recordCountExponent);

            int recordEstimate = (int)Math.Pow(10, recordCountExponent);

            int recordCount = records.GetValueOrDefault(recordEstimate);

            // record count specified in profile if it is the configurable scenario
            // if the record count specified is 1, assume it's pre-initialization, and do not use estimate

            recordCount = (databaseScenario == SysbenchScenario.Configure || recordCount == 1) ? recordCount : recordEstimate;

            return recordCount;
        }

        /// <summary>
        /// Method to determine the record count for the given run.
        /// </summary>
        /// <returns></returns>
        public static int GetWarehouseCount(string databaseScenario, int? warehouses)
        {
            int warehouseCount = warehouses.GetValueOrDefault(100);
            warehouseCount = (databaseScenario == SysbenchScenario.Configure || warehouseCount == 1) ? warehouseCount : 100;

            return warehouseCount;
        }

        /// <summary>
        /// Method to determine the record count for the given run.
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

            DependencyPath workloadPackage = await this.GetPackageAsync(this.PackageName, cancellationToken).ConfigureAwait(false);
            workloadPackage.ThrowIfNull(this.PackageName);

            DependencyPath package = await this.GetPackageAsync(this.PackageName, cancellationToken);
            this.SysbenchPackagePath = package.Path;

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
                LinuxDistributionInfo distributionInfo = await this.SystemManager.GetLinuxDistributionAsync(cancellationToken)
                    .ConfigureAwait(false);
                string distribution = distributionInfo.LinuxDistribution.ToString();

                string arguments = $"{this.SysbenchPackagePath}/configure-workload-generator.py --distro {distribution} --databaseSystem {this.DatabaseSystem} --packagePath {this.SysbenchPackagePath}";

                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    "python3",
                    arguments,
                    this.SysbenchPackagePath,
                    telemetryContext,
                    cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "SysbenchExecutor", logToFile: true);
                        process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                    }
                }

                state.SysbenchInitialized = true;
            }
           
            await this.stateManager.SaveStateAsync<SysbenchState>(nameof(SysbenchState), state, cancellationToken);
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
                                $"The Sysbench workload is not supported on the current Linux distro - " +
                                $"{linuxDistributionInfo.LinuxDistribution}.  Supported distros include:" +
                                $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Ubuntu)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Debian)}",
                                ErrorReason.LinuxDistributionNotSupported);
                    }
                }
            }
            else
            {
                throw new WorkloadException(
                $"The Sysbench workload generator is not supported on the current platform.",
                ErrorReason.PlatformNotSupported);
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
            public const string Configure = nameof(Configure);
        }

        /// <summary>
        /// Defines the Sysbench OLTP benchmark scenario.
        /// </summary>
        internal class BenchmarkName
        {
            public const string OLTP = nameof(OLTP);
            public const string TPCC = nameof(TPCC);
        }
    }
}