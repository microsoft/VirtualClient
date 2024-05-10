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

    /// <summary>
    /// Configures the MySQL database for Sysbench use.
    /// </summary>
    public class SysbenchConfiguration : SysbenchExecutor
    {
        private readonly IStateManager stateManager;
        private string sysbenchPrepareArguments;

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
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            SysbenchState state = await this.stateManager.GetStateAsync<SysbenchState>(nameof(SysbenchState), cancellationToken)
               ?? new SysbenchState();

            if (!state.DatabasePopulated)
            {
                await this.Logger.LogMessageAsync($"{this.TypeName}.PopulateDatabase", telemetryContext.Clone(), async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        if (this.Benchmark == BenchmarkName.OLTP)
                        {
                            await this.PrepareOLTPMySQLDatabase(telemetryContext, cancellationToken);
                        }
                        else if (this.Benchmark == BenchmarkName.TPCC)
                        {
                            await this.PrepareTPCCMySQLDatabase(telemetryContext, cancellationToken);
                        }
                        else
                        {
                            throw new DependencyException(
                            $"The '{this.Benchmark}' benchmark is not supported with the Sysbench workload. Supported options include: \"OLTP, TPCC\".",
                            ErrorReason.NotSupported);
                        }
                    }
                });

                if (this.RecordCount > 1)
                {
                    state.DatabasePopulated = true;
                    await this.stateManager.SaveStateAsync<SysbenchState>(nameof(SysbenchState), state, cancellationToken);
                }
            }
        }

        private async Task PrepareOLTPMySQLDatabase(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int tableCount = GetTableCount(this.DatabaseScenario, this.TableCount, this.Workload);
            int threadCount = GetThreadCount(this.SystemManager, this.DatabaseScenario, this.Threads);
            int recordCount = GetRecordCount(this.SystemManager, this.DatabaseScenario, this.RecordCount);

            this.sysbenchPrepareArguments = $"--dbName {this.DatabaseName} --serverType {this.ServerType} --benchmark {this.Benchmark} --tableCount {tableCount} --recordCount {recordCount} --threadCount {threadCount} --password {this.SuperUserPassword}";

            string command = $"python3";
            string arguments = $"{this.SysbenchPackagePath}/populate-database.py ";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                command,
                arguments + this.sysbenchPrepareArguments,
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

        private async Task PrepareTPCCMySQLDatabase(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int tableCount = GetTableCount(this.DatabaseScenario, this.TableCount, this.Workload);
            int threadCount = GetThreadCount(this.SystemManager, this.DatabaseScenario, this.Threads);
            int warehouseCount = GetWarehouseCount(this.DatabaseScenario, this.WarehouseCount);

            this.sysbenchPrepareArguments = $"--dbName {this.DatabaseName} --serverType {this.ServerType} --benchmark {this.Benchmark} --tableCount {tableCount} --warehouses {warehouseCount} --threadCount {threadCount} --password {this.SuperUserPassword}";

            string command = $"python3";
            string arguments = $"{this.SysbenchPackagePath}/populate-database.py ";

            using (IProcessProxy process = await this.ExecuteCommandAsync(
                command,
                arguments + this.sysbenchPrepareArguments,
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
    }
}
