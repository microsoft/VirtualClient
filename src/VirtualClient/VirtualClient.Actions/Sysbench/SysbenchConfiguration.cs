// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Configures the MySQL database for Sysbench use.
    /// </summary>
    public class SysbenchConfiguration : SysbenchExecutor
    {
        private readonly IStateManager stateManager;
        private string sysbenchPopulationArguments;

        /// <summary>
        /// Initializes a new instance of the <see cref="SysbenchConfiguration"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public SysbenchConfiguration(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.stateManager = this.Dependencies.GetService<IStateManager>();
        }

        /// <summary>
        /// The specifed action that controls the execution of the dependency.
        /// </summary>
        public string Action
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.Action), out IConvertible action);
                return action?.ToString();
            }
        }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                switch (this.Action)
                {
                    case ConfigurationAction.Cleanup:
                        await this.CleanUpDatabase(telemetryContext, cancellationToken);
                        break;
                    case ConfigurationAction.CreateTables:
                        if (this.Benchmark.Equals("oltp", StringComparison.OrdinalIgnoreCase))
                        {
                            await this.PrepareOLTPDatabase(telemetryContext, cancellationToken);
                        }
                        else if (this.Benchmark.Equals("tpcc", StringComparison.OrdinalIgnoreCase))
                        {
                            await this.PrepareTPCCDatabase(telemetryContext, cancellationToken);
                        }

                        break;
                    case ConfigurationAction.PopulateTables:
                        if (this.Benchmark.Equals("oltp", StringComparison.OrdinalIgnoreCase))
                        {
                            await this.PopulateOLTPDatabase(telemetryContext, cancellationToken);
                        }
                        else if (this.Benchmark.Equals("tpcc", StringComparison.OrdinalIgnoreCase))
                        {
                            await this.PopulateTPCCDatabase(telemetryContext, cancellationToken);
                        }

                        break;
                    default:
                        throw new DependencyException(
                            $"The specified Sysbench action '{this.Action}' is not supported. Supported actions include: \"{ConfigurationAction.PopulateTables}, {ConfigurationAction.Cleanup}, {ConfigurationAction.CreateTables}\".",
                            ErrorReason.NotSupported);
                }
            }
        }

        private async Task CleanUpDatabase(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            SysbenchState state = await this.stateManager.GetStateAsync<SysbenchState>(nameof(SysbenchState), cancellationToken)
               ?? new SysbenchState();

            if (state.DatabasePopulated)
            {
                int tableCount = GetTableCount(this.DatabaseScenario, this.TableCount, this.Workload);

                string serverIp = (this.IsMultiRoleLayout() && this.IsInRole(ClientRole.Client)) ? this.ServerIpAddress : "localhost";

                string sysbenchCleanupArguments = $"--dbName {this.DatabaseName} --databaseSystem {this.DatabaseSystem} --benchmark {this.Benchmark} --tableCount {tableCount}  --hostIpAddress \"{serverIp}\"";

                string script = $"{this.SysbenchPackagePath}/cleanup-database.py";

                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    SysbenchExecutor.PythonCommand,
                    $"{script} {sysbenchCleanupArguments}",
                    this.SysbenchPackagePath,
                    telemetryContext,
                    cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "Sysbench", logToFile: true);
                        process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadFailed);
                    }
                }
            }

            state.DatabasePopulated = false;
            await this.stateManager.SaveStateAsync<SysbenchState>(nameof(SysbenchState), state, cancellationToken);
        }

        private async Task PrepareOLTPDatabase(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            SysbenchState state = await this.stateManager.GetStateAsync<SysbenchState>(nameof(SysbenchState), cancellationToken)
               ?? new SysbenchState();

            if (!state.DatabasePopulated)
            {
                int tableCount = GetTableCount(this.DatabaseScenario, this.TableCount, this.Workload);
                int threadCount = GetThreadCount(this.SystemManager, this.DatabaseScenario, this.Threads);

                string sysbenchPrepareArguments = $"--dbName {this.DatabaseName} --databaseSystem {this.DatabaseSystem} --benchmark {this.Benchmark} --threadCount {threadCount} --tableCount {tableCount} --recordCount 1 --password {this.SuperUserPassword}";
                string serverIp = (this.IsMultiRoleLayout() && this.IsInRole(ClientRole.Client)) ? this.ServerIpAddress : "localhost";
                sysbenchPrepareArguments += $" --host \"{serverIp}\"";

                string command = $"{this.SysbenchPackagePath}/populate-database.py";

                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    SysbenchExecutor.PythonCommand,
                    $"{command} {sysbenchPrepareArguments}",
                    this.SysbenchPackagePath,
                    telemetryContext,
                    cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "Sysbench", logToFile: true);
                        process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                    }
                }
            }
            else
            {
                throw new DependencyException(
                            $"Database preparation failed. A database has already been populated on the system. Please drop the tables, or run \"{ConfigurationAction.Cleanup}\" Action" +
                            $"before attempting to create new tables on this database.",
                            ErrorReason.NotSupported);
            }
        }

        private async Task PrepareTPCCDatabase(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            SysbenchState state = await this.stateManager.GetStateAsync<SysbenchState>(nameof(SysbenchState), cancellationToken)
               ?? new SysbenchState();

            if (!state.DatabasePopulated)
            {
                int tableCount = GetTableCount(this.DatabaseScenario, this.TableCount, this.Workload);
                int threadCount = GetThreadCount(this.SystemManager, this.DatabaseScenario, this.Threads);
                int warehouseCount = GetWarehouseCount(this.DatabaseScenario, this.WarehouseCount);

                string sysbenchPrepareArguments = $"--dbName {this.DatabaseName} --databaseSystem {this.DatabaseSystem} --benchmark {this.Benchmark} --tableCount {tableCount} --warehouses 1 --threadCount {threadCount} --password {this.SuperUserPassword}";
                string serverIp = (this.IsMultiRoleLayout() && this.IsInRole(ClientRole.Client)) ? this.ServerIpAddress : "localhost";
                sysbenchPrepareArguments += $" --host \"{serverIp}\"";

                string script = $"{this.SysbenchPackagePath}/populate-database.py";

                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    SysbenchExecutor.PythonCommand,
                    $"{script} {sysbenchPrepareArguments}",
                    this.SysbenchPackagePath,
                    telemetryContext,
                    cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "Sysbench", logToFile: true);
                        process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                    }
                }
            }
            else
            {
                throw new DependencyException(
                            $"Database preparation failed. A database has already been populated on the system. Please drop the tables, or run \"{ConfigurationAction.Cleanup}\" Action" +
                            $"before attempting to create new tables on this database.",
                            ErrorReason.NotSupported);
            }
        }

        private async Task PopulateOLTPDatabase(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            SysbenchState state = await this.stateManager.GetStateAsync<SysbenchState>(nameof(SysbenchState), cancellationToken)
               ?? new SysbenchState();

            if (!state.DatabasePopulated)
            {
                await this.Logger.LogMessageAsync($"{this.TypeName}.PopulateDatabase", telemetryContext.Clone(), async () =>
                {
                    int tableCount = GetTableCount(this.DatabaseScenario, this.TableCount, this.Workload);
                    int threadCount = GetThreadCount(this.SystemManager, this.DatabaseScenario, this.Threads);
                    int recordCount = GetRecordCount(this.SystemManager, this.DatabaseScenario, this.RecordCount);

                    string sysbenchLoggingArguments = $"--dbName {this.DatabaseName} --databaseSystem {this.DatabaseSystem} --benchmark {this.Benchmark} --threadCount {threadCount} --tableCount {tableCount} --recordCount {recordCount}";
                    this.sysbenchPopulationArguments = $"{sysbenchLoggingArguments} --password {this.SuperUserPassword}";

                    string serverIp = (this.IsMultiRoleLayout() && this.IsInRole(ClientRole.Client)) ? this.ServerIpAddress : "localhost";
                    this.sysbenchPopulationArguments += $" --host \"{serverIp}\"";

                    string script = $"{this.SysbenchPackagePath}/populate-database.py";

                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        SysbenchExecutor.PythonCommand,
                        $"{script} {this.sysbenchPopulationArguments}",
                        this.SysbenchPackagePath,
                        telemetryContext,
                        cancellationToken))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "Sysbench", logToFile: true);
                            process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);

                            this.AddPopulationDurationMetric(sysbenchLoggingArguments, process, telemetryContext, cancellationToken);
                        }
                    }

                    state.DatabasePopulated = true;
                    await this.stateManager.SaveStateAsync<SysbenchState>(nameof(SysbenchState), state, cancellationToken);
                });
            }
            else
            {
                throw new DependencyException(
                            $"Database preparation failed. A database has already been populated on the system. Please drop the tables, or run \"{ConfigurationAction.Cleanup}\" Action" +
                            $"before attempting to add new records to the populated tables.",
                            ErrorReason.NotSupported);
            }
        }

        private async Task PopulateTPCCDatabase(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            SysbenchState state = await this.stateManager.GetStateAsync<SysbenchState>(nameof(SysbenchState), cancellationToken)
               ?? new SysbenchState();

            if (!state.DatabasePopulated)
            {
                await this.Logger.LogMessageAsync($"{this.TypeName}.PopulateDatabase", telemetryContext.Clone(), async () =>
                {
                    int tableCount = GetTableCount(this.DatabaseScenario, this.TableCount, this.Workload);
                    int threadCount = GetThreadCount(this.SystemManager, this.DatabaseScenario, this.Threads);
                    int warehouseCount = GetWarehouseCount(this.DatabaseScenario, this.WarehouseCount);

                    string sysbenchLoggingArguments = $"--dbName {this.DatabaseName} --databaseSystem {this.DatabaseSystem} --benchmark {this.Benchmark} --threadCount {threadCount} --tableCount {tableCount} --warehouses {warehouseCount}";
                    this.sysbenchPopulationArguments = $"{sysbenchLoggingArguments} --password {this.SuperUserPassword}";

                    string serverIp = (this.IsMultiRoleLayout() && this.IsInRole(ClientRole.Client)) ? this.ServerIpAddress : "localhost";
                    this.sysbenchPopulationArguments += $" --host \"{serverIp}\"";

                    string script = $"{this.SysbenchPackagePath}/populate-database.py";

                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        SysbenchExecutor.PythonCommand,
                        $"{script} {this.sysbenchPopulationArguments}",
                        this.SysbenchPackagePath,
                        telemetryContext,
                        cancellationToken))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "Sysbench", logToFile: true);
                            process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);

                            this.AddPopulationDurationMetric(sysbenchLoggingArguments, process, telemetryContext, cancellationToken);
                        }
                    }

                    state.DatabasePopulated = true;
                    await this.stateManager.SaveStateAsync<SysbenchState>(nameof(SysbenchState), state, cancellationToken);
                });
            }
            else
            {
                throw new DependencyException(
                            $"Database preparation failed. A database has already been populated on the system. Please drop the tables, or run \"{ConfigurationAction.Cleanup}\" Action" +
                            $"before attempting to add new records to the populated tables.",
                            ErrorReason.NotSupported);
            }
        }

        /// <summary>
        /// Add metrics to telemtry.
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="process"></param>
        /// <param name="telemetryContext"></param>
        /// <param name="cancellationToken"></param>
        private void AddPopulationDurationMetric(string arguments, IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    "Sysbench",
                    process.FullCommand());

                this.MetadataContract.Apply(telemetryContext);

                string text = process.StandardOutput.ToString();

                List<Metric> metrics = new List<Metric>();
                double duration = (process.ExitTime - process.StartTime).TotalMinutes;
                metrics.Add(new Metric("PopulateDatabaseDuration", duration, "minutes", MetricRelativity.LowerIsBetter));

                this.Logger.LogMetrics(
                    toolName: "Sysbench",
                    scenarioName: this.MetricScenario ?? this.Scenario,
                    process.StartTime,
                    process.ExitTime,
                    metrics,
                    null,
                    scenarioArguments: arguments,
                    this.Tags,
                    telemetryContext);
            }
        }

        /// <summary>
        /// Supported Sysbench Client actions.
        /// </summary>
        internal class ConfigurationAction
        {
            /// <summary>
            /// Initializes the tables on the database.
            /// </summary>
            public const string CreateTables = nameof(CreateTables);

            /// <summary>
            /// Creates Database on MySQL server and Users on Server and any Clients.
            /// </summary>
            public const string PopulateTables = nameof(PopulateTables);

            /// <summary>
            /// Runs sysbench cleanup action.
            /// </summary>
            public const string Cleanup = nameof(Cleanup);
        }
    }
}
