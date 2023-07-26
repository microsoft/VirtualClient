// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// PostgreSQL Client executor.
    /// Executes the transactions to the server.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class PostgreSQLClientExecutor : PostgreSQLExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSQLClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public PostgreSQLClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.PollingInterval = TimeSpan.FromSeconds(20);
            this.PollingTimeout = TimeSpan.FromHours(1);
        }

        /// <summary>
        /// The interval at which polling requests should be made against the target server-side
        /// API for state changes.
        /// </summary>
        protected TimeSpan PollingInterval { get; set; }

        /// <summary>
        /// A time range at which the client will poll for expected state before timing out.
        /// </summary>
        protected TimeSpan PollingTimeout { get; set; }

        /// <summary>
        /// Executes the client threads.
        /// 1. Polls and waits for the DB creation to be completed.
        /// 2. Get client parameters from server.
        /// 3. Excutes the client threads.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // The VC server-side instance/API must be confirmed online.
            await this.ServerApiClient.PollForHeartbeatAsync(
                this.PollingTimeout,
                cancellationToken,
                this.PollingInterval);

            // The VC server-side instance/role must be confirmed ready for the client to operate (e.g. the database is initialized).
            await this.ServerApiClient.PollForServerOnlineAsync(
                this.PollingTimeout,
                cancellationToken,
                this.PollingInterval);

            // The PostgreSQL database must be confirmed to be initialized and ready.
            await this.ServerApiClient.PollForExpectedStateAsync<PostgreSQLServerState>(
                nameof(PostgreSQLServerState),
                (state => state.DatabaseInitialized == true),
                this.PollingTimeout,
                cancellationToken,
                this.PollingInterval);

            // Configure the HammerDB transactions execution workload.
            await this.ConfigureWorkloadAsync(cancellationToken);

            await this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", telemetryContext, async () =>
            {
                DateTime startTime = DateTime.UtcNow;

                string results = string.Empty;
                if (this.Platform == PlatformID.Unix)
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        "bash",
                        $"-c \"{this.Combine(this.HammerDBPackagePath, "hammerdbcli")} auto {PostgreSQLServerExecutor.RunTransactionsTclName}\"",
                        this.HammerDBPackagePath,
                        telemetryContext,
                        cancellationToken))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "PostgreSQL", logToFile: true);
                            process.ThrowIfWorkloadFailed();

                            results = process.StandardOutput.ToString();
                        }
                    }
                }
                else if (this.Platform == PlatformID.Win32NT)
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        this.Combine(this.HammerDBPackagePath, "hammerdbcli.bat"),
                        $"auto {PostgreSQLServerExecutor.RunTransactionsTclName}",
                        this.HammerDBPackagePath,
                        telemetryContext,
                        cancellationToken))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "PostgreSQL", logToFile: true);
                            process.ThrowIfWorkloadFailed();

                            results = process.StandardOutput.ToString();
                        }
                    }
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    this.CaptureMetricsAsync(results, startTime, DateTime.UtcNow, telemetryContext, cancellationToken);
                }
            });
        }

        private void CaptureMetricsAsync(string results, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    PostgreSQLMetricsParser postgreSQLParser = new PostgreSQLMetricsParser(results);
                    IList<Metric> metrics = postgreSQLParser.Parse();

                    this.CaptureMetrics(metrics, startTime, DateTime.UtcNow, telemetryContext);
                }
                catch (SchemaException exc)
                {
                    throw new WorkloadResultsException($"Failed to parse workload results.", exc, ErrorReason.WorkloadResultsParsingFailed);
                }
            }
        }

        private Task ConfigureWorkloadAsync(CancellationToken cancellationToken)
        {
            // Each time that we run we copy the script file and the TCL file into the root HammerDB directory
            // alongside the benchmarking toolsets (e.g. hammerdbcli).
            string tclPath = this.Combine(this.HammerDBPackagePath, "benchmarks", this.Benchmark.ToLowerInvariant(), "postgresql", PostgreSQLServerExecutor.RunTransactionsTclName);
            string tclCopyPath = this.Combine(this.HammerDBPackagePath, PostgreSQLClientExecutor.RunTransactionsTclName);

            if (!this.FileSystem.File.Exists(tclPath))
            {
                throw new DependencyException(
                    $"Required script file missing. The script file required in order to run transactions against the database '{PostgreSQLClientExecutor.RunTransactionsTclName}' " +
                    $"does not exist in the HammerDB package.",
                    ErrorReason.DependencyNotFound);
            }

            this.FileSystem.File.Copy(tclPath, tclCopyPath, true);
            return this.SetTclScriptParametersAsync(tclCopyPath, cancellationToken);
        }

        private async Task SetTclScriptParametersAsync(string tclFilePath, CancellationToken cancellationToken)
        {
            Item<PostgreSQLServerState> state = await this.ServerApiClient.GetStateAsync<PostgreSQLServerState>(
                nameof(PostgreSQLServerState),
                cancellationToken);

            if (state == null)
            {
                throw new WorkloadException(
                    $"Expected server state information missing. The server did not return state indicating the settings to use for running transactions against the database.",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            PostgreSQLServerState parameters = state.Definition;
            string targetIPAddress = "localhost";

            if (this.IsMultiRoleLayout())
            {
                ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                targetIPAddress = serverInstance.IPAddress;
            }

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                "<PORT>",
                this.Port.ToString(),
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                "<DATABASENAME>",
                this.DatabaseName,
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                "<VIRTUALUSERS>",
                parameters.UserCount.ToString(),
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                @"<HOSTNAME>",
                targetIPAddress,
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                @"<USERNAME>",
                parameters.UserName,
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                @"<PASSWORD>",
                parameters.Password,
                cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                tclFilePath,
                @"<SUPERUSERPWD>",
                parameters.Password,
                cancellationToken);
        }
    }
}