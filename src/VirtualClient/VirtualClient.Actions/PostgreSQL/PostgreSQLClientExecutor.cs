// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient;
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
        // Maintained in the HammerDB package that we use.
        private const string RunTransactionsScriptName = "runTransactionsScript.sh";
        private const string RunTransactionsTclName = "runTransactions.tcl";

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSQLClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public PostgreSQLClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.PollingTimeout = TimeSpan.FromHours(1);
        }

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
                cancellationToken);

            // The VC server-side instance/role must be confirmed ready for the client to operate (e.g. the database is initialized).
            await this.ServerApiClient.PollForServerOnlineAsync(
                this.PollingTimeout,
                cancellationToken);

            // The PostgreSQL database must be confirmed to be initialized and ready.
            await this.ServerApiClient.PollForExpectedStateAsync<PostgreSQLServerState>(
                nameof(PostgreSQLServerState),
                (state => state.DatabaseInitialized == true),
                this.PollingTimeout,
                cancellationToken);

            // Configure the HammerDB transactions execution workload.
            await this.ConfigureWorkloadAsync(cancellationToken);

            DateTime startTime = DateTime.UtcNow;

            string results = string.Empty;
            if (this.Platform == PlatformID.Unix)
            {
                results = await this.ExecuteCommandAsync<PostgreSQLClientExecutor>(
                    $@"bash {PostgreSQLClientExecutor.RunTransactionsScriptName}",
                    null,
                    this.HammerDBPackagePath,
                    cancellationToken);
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                results = await this.ExecuteCommandAsync<PostgreSQLClientExecutor>(
                    $"{this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "hammerdbcli.bat")}",
                    $"auto {PostgreSQLClientExecutor.RunTransactionsTclName}",
                    this.HammerDBPackagePath,
                    cancellationToken);
            }

            this.CaptureMetricsAsync(results, startTime, DateTime.UtcNow, telemetryContext, cancellationToken);
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
            string scriptPath = this.Combine(this.HammerDBPackagePath, "postgresql", PostgreSQLClientExecutor.RunTransactionsScriptName);
            string tclPath = this.Combine(this.HammerDBPackagePath, "postgresql", PostgreSQLClientExecutor.RunTransactionsTclName);
            string scriptCopyPath = null;
            string tclCopyPath = null;

            if (!this.FileSystem.File.Exists(tclPath))
            {
                throw new DependencyException(
                    $"Required script file missing. The script file required in order to run transactions against the database '{PostgreSQLClientExecutor.RunTransactionsTclName}' " +
                    $"does not exist in the HammerDB package.",
                    ErrorReason.DependencyNotFound);
            }

            if (this.Platform == PlatformID.Unix)
            {
                if (!this.FileSystem.File.Exists(scriptPath))
                {
                    throw new DependencyException(
                        $"Required script file missing. The script file required in order to run transactions against the database '{PostgreSQLClientExecutor.RunTransactionsScriptName}' " +
                        $"does not exist in the HammerDB package.",
                        ErrorReason.DependencyNotFound);
                }

                scriptCopyPath = this.Combine(this.HammerDBPackagePath, PostgreSQLClientExecutor.RunTransactionsScriptName);
                tclCopyPath = this.Combine(this.HammerDBPackagePath, PostgreSQLClientExecutor.RunTransactionsTclName);

                this.FileSystem.File.Copy(scriptPath, scriptCopyPath, true);
                this.FileSystem.File.Copy(tclPath, tclCopyPath, true);
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                tclCopyPath = this.Combine(this.HammerDBPackagePath, PostgreSQLClientExecutor.RunTransactionsTclName);
                this.FileSystem.File.Copy(tclPath, tclCopyPath, true);
            }

            return this.SetTclScriptParametersAsync(cancellationToken);
        }

        private async Task SetTclScriptParametersAsync(CancellationToken cancellationToken)
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
                    this.PlatformSpecifics.Combine(this.HammerDBPackagePath, PostgreSQLClientExecutor.RunTransactionsTclName),
                    @"<VIRTUALUSERS>",
                    $"{parameters.UserCount}",
                    cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                    this.PlatformSpecifics.Combine(this.HammerDBPackagePath, PostgreSQLClientExecutor.RunTransactionsTclName),
                    @"<HOSTNAME>",
                    $"{targetIPAddress}",
                    cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                    this.PlatformSpecifics.Combine(this.HammerDBPackagePath, PostgreSQLClientExecutor.RunTransactionsTclName),
                    @"<USERNAME>",
                    $"{parameters.UserName}",
                    cancellationToken);

            await this.FileSystem.File.ReplaceInFileAsync(
                    this.PlatformSpecifics.Combine(this.HammerDBPackagePath, PostgreSQLClientExecutor.RunTransactionsTclName),
                    @"<PASSWORD>",
                    $"{parameters.Password}",
                    cancellationToken);
        }
    }
}