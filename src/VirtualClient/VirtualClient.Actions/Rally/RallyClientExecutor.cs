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
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The Rally Client workload executor.
    /// </summary>
    public class RallyClientExecutor : RallyExecutor
    {
        private string rallyExecutionArguments;
        private Guid raceId;

        /// <summary>
        /// Initializes a new instance of the <see cref="RallyClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public RallyClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);
            this.PollingTimeout = TimeSpan.FromMinutes(40);
        }

        /// <summary>
        /// The track targeted for run by Rally.
        /// </summary>
        public string TrackName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(RallyClientExecutor.TrackName));
            }
        }

        /// <summary>
        /// The Elasticsearch Distribution Version.
        /// </summary>
        public string DistributionVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(RallyClientExecutor.DistributionVersion), "8.0.0");
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

        private string ServerIpAddresses { get; set; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            List<Task> clientWorkloadTasks = new List<Task>();

            if (this.IsMultiRoleLayout())
            {
                clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // 1) Confirm server(s) is online.
                        // ===========================================================================
                        this.Logger.LogTraceMessage("Synchronization: Poll server(s) API for heartbeat...");

                        foreach (IApiClient serverApiClient in this.ServerApiClients)
                        {
                            await serverApiClient.PollForHeartbeatAsync(this.PollingTimeout, cancellationToken)
                                .ConfigureAwait(false);
                        }

                        // 2) Confirm the server-side application (e.g. web server(s)) is online.
                        // ===========================================================================
                        this.Logger.LogTraceMessage("Synchronization: Poll server(s) for online signal...");

                        foreach (IApiClient serverApiClient in this.ServerApiClients)
                        {
                            await serverApiClient.PollForServerOnlineAsync(TimeSpan.FromMinutes(10), cancellationToken)
                                .ConfigureAwait(false);
                        }

                        this.Logger.LogTraceMessage("Synchronization: Server(s) online signal confirmed...");

                        // 3) Execute the client workload.
                        // ===========================================================================
                        this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                        await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }));
            }
            else
            {
                clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                    }
                }));
            }

            await Task.WhenAll(clientWorkloadTasks);
            return;
        }

        /// <summary>
        /// Initializes the environment for execution of the Rally workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            this.raceId = Guid.NewGuid();

            // Initialize list of Elasticsearch Hosts (ie. 1+ VM cluster.)

            if (this.IsMultiRoleLayout())
            {
                IEnumerable<ClientInstance> serverInstances = this.GetLayoutClientInstances(ClientRole.Server);

                foreach (ClientInstance serverInstance in serverInstances)
                {
                    IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);
                    this.ServerIpAddresses += $"{serverIPAddress.ToString()}:39200,";
                }
            }
            else
            {
                this.ServerIpAddresses = $"{IPAddress.Loopback.ToString()}:39200";
            }

            // Configure Elasticsearch & Rally on the client.

            await this.Logger.LogMessageAsync($"{this.TypeName}.ConfigureClient", telemetryContext.Clone(), async () =>
            {
                RallyState state = await this.StateManager.GetStateAsync<RallyState>(nameof(RallyState), cancellationToken)
                    ?? new RallyState();

                if (!state.RallyConfigured)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // configure-client.py does the following:
                        //      - assigns ownership of the data directory to current user (esrally cannot run as root)
                        //      - initializes rally.ini config file
                        //      - assigns the rally directory to the chosen disk data directory (too many documents to keep in memory)
                        //      - starts the rally daemon

                        string configArguments = $"--directory {this.DataDirectory} --user {this.CurrentUser} --clientIp {this.ClientIpAddress}";
                        string arguments = $"{this.RallyPackagePath}/configure-client.py ";

                        using (IProcessProxy process = await this.ExecuteCommandAsync(
                            RallyExecutor.PythonCommand,
                            arguments + configArguments,
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

                        state.RallyConfigured = true;
                        await this.StateManager.SaveStateAsync<RallyState>(nameof(RallyState), state, cancellationToken);
                    }
                }
            });
        }

        private async Task CaptureMetrics(IProcessProxy process, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    "Elasticsearch-Rally",
                    process.FullCommand(),
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                string resultsFile = this.PlatformSpecifics.Combine(this.DataDirectory, "benchmarks", "races", this.raceId.ToString(), "race.json");
                string results = await this.LoadResultsAsync(resultsFile, cancellationToken);

                if (!string.IsNullOrEmpty(results))
                {
                    try
                    {
                        RallyMetricsParser parser = new RallyMetricsParser(results);
                        IList<Metric> metrics = parser.Parse();

                        this.Logger.LogMetrics(
                            toolName: "ElasticsearchRally",
                            scenarioName: this.MetricScenario ?? this.Scenario,
                            process.StartTime,
                            process.ExitTime,
                            metrics,
                            null,
                            scenarioArguments: this.rallyExecutionArguments,
                            this.Tags,
                            telemetryContext);
                    }
                    catch (Exception exc)
                    {
                        throw new WorkloadException($"Failed to parse ElasticsearchRally output.", exc, ErrorReason.InvalidResults);
                    }
                }
            }
        }

        private Task ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", telemetryContext.Clone(), async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    string workingDirectory = this.PlatformSpecifics.Combine(this.RallyPackagePath, ".venv", "bin");

                    // Future Feature Option:
                    //      --pipeline=benchmark-only argument if a user wants to provision their own cluster on Windows.
                    //      Rally cannot provision ES clusters on Windows OS. Could become a future VC Dependency, but user would have to
                    //          provide their own documents. If the user has a full, working Windows cluster, Rally can just be run on the
                    //          client with no server configuration required (other than setting it online for VC communication purposes).

                    this.rallyExecutionArguments = $"race --track={this.TrackName} --distribution-version={this.DistributionVersion} --target-hosts={this.ServerIpAddresses.TrimEnd(',')} --race-id={this.raceId} --runtime-jdk=bundled";

                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        "esrally",
                        this.rallyExecutionArguments,
                        workingDirectory,
                        telemetryContext,
                        cancellationToken))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "ElasticsearchRally", logToFile: true);
                            process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadFailed);

                            await this.CaptureMetrics(process, telemetryContext, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                }
            });
        }
    }
}
