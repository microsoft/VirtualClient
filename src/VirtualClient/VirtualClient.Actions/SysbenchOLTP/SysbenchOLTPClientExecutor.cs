// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The Sysbench Client workload executor.
    /// </summary>
    public class SysbenchOLTPClientExecutor : SysbenchOLTPExecutor
    {
        private const string SysbenchFileName = "src/sysbench";
        private readonly IPackageManager packageManager;
        private readonly IStateManager stateManager;
        private string sysbenchPrepareArguments;
        private string sysbenchExecutionArguments;
        private string sysbenchDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="SysbenchOLTPClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public SysbenchOLTPClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.packageManager = this.SystemManager.PackageManager;
            this.stateManager = this.SystemManager.StateManager;
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);
            this.PollingTimeout = TimeSpan.FromMinutes(40);
        }

        /// <summary>
        /// The database name option passed to Sysbench.
        /// </summary>
        public string DatabaseName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchOLTPClientExecutor.DatabaseName));
            }
        }

        /// <summary>
        /// The total time of execution option passed to Sysbench.
        /// </summary>
        public string DurationSecs
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchOLTPClientExecutor.DurationSecs));
            }
        }

        /// <summary>
        /// Number of runs the client executes load on server.
        /// </summary>
        public string NumberOfRuns
        {
            get
            {
                this.Parameters.TryGetValue(nameof(SysbenchOLTPClientExecutor.NumberOfRuns), out IConvertible numberOfRuns);
                return numberOfRuns?.ToString();
            }
        }

        /// <summary>
        /// The number of tables option passed to Sysbench.
        /// </summary>
        public string NumTables
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchOLTPClientExecutor.NumTables));
            }
        }

        /// <summary>
        /// The table size option passed to Sysbench.
        /// </summary>
        public string RecordCount
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchOLTPClientExecutor.RecordCount));
            }
        }

        /// <summary>
        /// The number of threads option passed to Sysbench.
        /// </summary>
        public string Threads
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchOLTPClientExecutor.Threads));
            }
        }

        /// <summary>
        /// The workload option passed to Sysbench.
        /// </summary>
        public string Workload
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchOLTPClientExecutor.Workload));
            }
        }

        /// <summary>
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        protected IAsyncPolicy ClientFlowRetryPolicy { get; set; }

        /// <summary>
        /// The timespan at which the client will poll the server for responses before
        /// timing out.
        /// </summary>
        protected TimeSpan PollingTimeout { get; set; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IPAddress ipAddress;
            List<Task> clientWorkloadTasks = new List<Task>();

            if (this.IsMultiRoleLayout())
            {
                IEnumerable<ClientInstance> targetServers = this.GetLayoutClientInstances(ClientRole.Server);

                foreach (ClientInstance server in targetServers)
                {
                    clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            IApiClient serverApiClient = this.ApiClientManager.GetOrCreateApiClient(server.Name, server);

                            // 1) Confirm server is online.
                            // ===========================================================================
                            this.Logger.LogTraceMessage("Synchronization: Poll server API for heartbeat...");

                            await serverApiClient.PollForHeartbeatAsync(this.PollingTimeout, cancellationToken)
                                .ConfigureAwait(false);

                            // 2) Confirm the server-side application (e.g. web server) is online.
                            // ===========================================================================
                            this.Logger.LogTraceMessage("Synchronization: Poll server for online signal...");

                            await serverApiClient.PollForServerOnlineAsync(TimeSpan.FromMinutes(10), cancellationToken)
                                .ConfigureAwait(false);

                            this.Logger.LogTraceMessage("Synchronization: Server online signal confirmed...");
                            this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                            // 3) Execute the client workload.
                            // ===========================================================================
                            ipAddress = IPAddress.Parse(server.IPAddress);
                            await this.ExecuteWorkloadAsync(ipAddress, telemetryContext, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }));
                }
            }
            else
            {
                ipAddress = IPAddress.Loopback;
                clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.ExecuteWorkloadAsync(ipAddress, telemetryContext, cancellationToken).ConfigureAwait(false);
                    }
                }));
            }

            return Task.WhenAll(clientWorkloadTasks);
        }

        /// <summary>
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            this.InitializeApiClients();

            // get sysbench workload path
            DependencyPath workloadPackage = await this.packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, CancellationToken.None).ConfigureAwait(false);
            this.sysbenchDirectory = this.GetPackagePath(this.PackageName);

            SysbenchOLTPState state = await this.stateManager.GetStateAsync<SysbenchOLTPState>($"{nameof(SysbenchOLTPState)}", cancellationToken)
                ?? new SysbenchOLTPState();

            if (!state.SysbenchInitialized)
            {
                // install sysbench using repo scripts
                await this.InstallSysbenchOLTPPackage(cancellationToken).ConfigureAwait(false);
                state.SysbenchInitialized = true;
            }

            // prepares database based on prepare arguments in profile 
            this.sysbenchPrepareArguments = $@"oltp_common --tables={this.NumTables} --mysql-db={this.DatabaseName} --mysql-host={this.ServerIpAddress} prepare";

            string sysbenchPath = this.PlatformSpecifics.Combine(this.sysbenchDirectory, SysbenchOLTPClientExecutor.SysbenchFileName);
            await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(sysbenchPath, this.sysbenchPrepareArguments, this.sysbenchDirectory, cancellationToken)
                .ConfigureAwait(false);

            await this.stateManager.SaveStateAsync<SysbenchOLTPState>($"{nameof(SysbenchOLTPState)}", state, cancellationToken);
        }

        private void CaptureMetrics(string results, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                results.ThrowIfNullOrWhiteSpace(nameof(results));

                this.Logger.LogMessage($"{nameof(SysbenchOLTPClientExecutor)}.CaptureMetrics", telemetryContext.Clone()
                    .AddContext("results", results));

                try
                {
                    SysbenchOLTPMetricsParser parser = new SysbenchOLTPMetricsParser(results);
                    this.Logger.LogMetrics(
                        toolName: "Sysbench",
                        scenarioName: "OLTP",
                        startTime,
                        endTime,
                        parser.Parse(),
                        metricCategorization: "Sysbench",
                        scenarioArguments: this.sysbenchExecutionArguments,
                        this.Tags,
                        telemetryContext);
                }
                catch (Exception exc)
                {
                    throw new WorkloadException($"Failed to parse sysbench output: {results}.", exc, ErrorReason.InvalidResults);
                }
            }
        }

        private Task ExecuteWorkloadAsync(IPAddress serverIpAddress, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", telemetryContext.Clone(), async () =>
            {
                this.StartTime = DateTime.UtcNow;
                this.sysbenchExecutionArguments = $@"{this.Workload} --threads={this.Threads} --time={this.DurationSecs} --tables={this.NumTables} --table-size={this.RecordCount} --mysql-db={this.DatabaseName} --mysql-host={this.ServerIpAddress}";

                DateTime startTime = DateTime.UtcNow;
                // gets executor arguments, combines with path & directory to get full command; metrics are stdout
                string sysbenchPath = this.PlatformSpecifics.Combine(this.sysbenchDirectory, SysbenchOLTPClientExecutor.SysbenchFileName);
                string results = await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(sysbenchPath, this.sysbenchExecutionArguments + " run", this.sysbenchDirectory, cancellationToken)
                    .ConfigureAwait(false);

                await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(sysbenchPath, this.sysbenchExecutionArguments + " cleanup", this.sysbenchDirectory, cancellationToken)
                    .ConfigureAwait(false);

                this.CaptureMetrics(results, this.StartTime, DateTime.UtcNow, telemetryContext, cancellationToken);
            });
        }

        private async Task InstallSysbenchOLTPPackage(CancellationToken cancellationToken)
        {
            const string autogenScriptCommand = "./autogen.sh";
            const string configureScriptCommand = "./configure";
            const string makeCommand = "make -j";
            const string makeInstallCommand = "make install";

            // build sysbench
            await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(autogenScriptCommand, null, this.sysbenchDirectory, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(configureScriptCommand, null, this.sysbenchDirectory, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(makeCommand, null, this.sysbenchDirectory, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync<SysbenchOLTPClientExecutor>(makeInstallCommand, null, this.sysbenchDirectory, cancellationToken)
                .ConfigureAwait(false);
        }

        internal class SysbenchOLTPState : State
        {
            public SysbenchOLTPState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool SysbenchInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(SysbenchOLTPState.SysbenchInitialized), false);
                }

                set
                {
                    this.Properties[nameof(SysbenchOLTPState.SysbenchInitialized)] = value;
                }
            }
        }
    }
}
