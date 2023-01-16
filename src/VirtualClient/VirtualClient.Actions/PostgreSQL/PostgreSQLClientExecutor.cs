// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
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
        private string scriptName = "runTransactionsScript.sh";

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSQLClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public PostgreSQLClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Path to client script
        /// </summary>
        public string ClientScriptPath { get; set; }

        /// <summary>
        /// Server IpAddress(Ipaddress of the machine where postgreSQL server is hosted).
        /// </summary>
        protected string ServerIpAddress { get; set; }

        /// <summary>
        /// Initializes the workload executor paths and dependencies.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);

            await this.SetUpConfigurationAsync(cancellationToken)
                .ConfigureAwait(false); 
             
            if (this.Platform == PlatformID.Unix)
            {
                this.ClientScriptPath = this.PlatformSpecifics.Combine(this.WorkloadPackagePath, this.scriptName);

                if (this.FileSystem.File.Exists(this.ClientScriptPath))
                {
                    this.FileSystem.File.Copy(
                        this.ClientScriptPath,
                        this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "runTransactionsScript.sh"),
                        true);
                }

                if (this.FileSystem.File.Exists(this.PlatformSpecifics.Combine(this.WorkloadPackagePath, "runTransactions.tcl")))
                {
                    this.FileSystem.File.Copy(
                        this.PlatformSpecifics.Combine(this.WorkloadPackagePath, "runTransactions.tcl"),
                        this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "runTransactions.tcl"),
                        true);
                }
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                if (this.FileSystem.File.Exists(this.PlatformSpecifics.Combine(this.WorkloadPackagePath, "runTransactions.tcl")))
                {
                    this.FileSystem.File.Copy(
                        this.PlatformSpecifics.Combine(this.WorkloadPackagePath, "runTransactions.tcl"),
                        this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "runTransactions.tcl"),
                        true);
                }
            }
        }

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
            State expectedState = new State(new Dictionary<string, IConvertible>
            {
                [nameof(PostgreSQLState)] = PostgreSQLState.DBCreated
            });

            var pollingTimeout = TimeSpan.FromHours(1);
            await this.ServerApiClient.PollForExpectedStateAsync(
                nameof(PostgreSQLState), JObject.FromObject(expectedState), pollingTimeout, DefaultStateComparer.Instance, cancellationToken)
                .ConfigureAwait(false);

            this.StartTime = DateTime.Now;

            await this.SetScriptParameters(cancellationToken)
                .ConfigureAwait(false);

            string results = string.Empty;
            if (this.Platform == PlatformID.Unix)
            {
                results += await this.ExecuteCommandAsync<PostgreSQLClientExecutor>(@"bash runTransactionsScript.sh", null, this.HammerDBPackagePath, cancellationToken)
                .ConfigureAwait(false);
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                results += await this.ExecuteCommandAsync<PostgreSQLClientExecutor>($"{this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "hammerdbcli.bat")}", "auto runTransactions.tcl", this.HammerDBPackagePath, cancellationToken)
                .ConfigureAwait(false);
            }

            this.CaptureWorkloadResultsAsync(results, this.StartTime, DateTime.Now, telemetryContext, cancellationToken);
        }

        /// <summary>
        /// return parameters for client PostgreSQL.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected async Task SetScriptParameters(CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await this.ServerApiClient.GetStateAsync(nameof(PostgreSQLParameters), cancellationToken)
              .ConfigureAwait(false);

            response.ThrowOnError<WorkloadException>();

            string responseContent = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            var parameters = responseContent.FromJson<Item<PostgreSQLParameters>>();

            if (this.IsMultiRoleLayout())
            {
                ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.PrivateIPAddress, out IPAddress serverIPAddress);
                this.ServerIpAddress = serverIPAddress.ToString();
            }
            else
            {
                this.ServerIpAddress = "localhost";
            }
           
            await this.FileSystem.File.ReplaceInFileAsync(
                    this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "runTransactions.tcl"),
                    @"<VIRTUALUSERS>",
                    $"{parameters.Definition.NumOfVirtualUsers}",
                    cancellationToken).ConfigureAwait(false);
            await this.FileSystem.File.ReplaceInFileAsync(
                    this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "runTransactions.tcl"),
                    @"<HOSTNAME>",
                    $"{this.ServerIpAddress}",
                    cancellationToken).ConfigureAwait(false);
            await this.FileSystem.File.ReplaceInFileAsync(
                    this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "runTransactions.tcl"),
                    @"<USERNAME>",
                    $"{parameters.Definition.UserName}",
                    cancellationToken).ConfigureAwait(false);
            await this.FileSystem.File.ReplaceInFileAsync(
                    this.PlatformSpecifics.Combine(this.HammerDBPackagePath, "runTransactions.tcl"),
                    @"<PASSWORD>",
                    $"{parameters.Definition.Password}",
                    cancellationToken).ConfigureAwait(false);
        }

        private void CaptureWorkloadResultsAsync(string results, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    PostgreSQLMetricsParser postgreSQLParser = new PostgreSQLMetricsParser(results);
                    IList<Metric> metrics = postgreSQLParser.Parse();
                    this.LogTestResults(metrics, startTime, DateTime.UtcNow, telemetryContext);
                }
                catch (SchemaException exc)
                {
                    throw new WorkloadResultsException($"Failed to parse workload results.", exc, ErrorReason.WorkloadResultsParsingFailed);
                }
            }
        }
    }
}