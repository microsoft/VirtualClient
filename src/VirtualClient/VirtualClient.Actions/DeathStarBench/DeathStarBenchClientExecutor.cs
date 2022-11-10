// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Executes DeathStarBench Client and generates HTTP load.
    /// </summary>
    public class DeathStarBenchClientExecutor : DeathStarBenchExecutor
    {
        private string workPath;
        private IAsyncPolicy clientExecutionRetryPolicy;
        private TimeSpan serverOnlinePollingTimeout;

        private Dictionary<string, Dictionary<string, string>> actionScript = new Dictionary<string, Dictionary<string, string>>()
        {
            {
                DeathStarBenchExecutor.SocialNetwork, new Dictionary<string, string>()
                {
                    { "ComposePost", "./scripts/social-network/compose-post.lua http://localhost:8080/wrk2-api/post/compose" },
                    { "ReadHomeTimeline", "./scripts/social-network/read-home-timeline.lua http://localhost:8080/wrk2-api/home-timeline/read " },
                    { "ReadUserTimeline", "./scripts/social-network/read-user-timeline.lua http://localhost:8080/wrk2-api/user-timeline/read" },
                    { "MixedWorkload", "./scripts/social-network/mixed-workload.lua http://localhost:8080" }
                }
            },
            {
                DeathStarBenchExecutor.MediaMicroservices, new Dictionary<string, string>()
                {
                    { "ComposeReviews", "./scripts/media-microservices/compose-review.lua http://localhost:8080/wrk2-api/review/compose" }
                }
            },
            {
                DeathStarBenchExecutor.HotelReservation, new Dictionary<string, string>()
                {
                    { "MixedWorkload", "./scripts/hotel-reservation/mixed-workload_type_1.lua http://0.0.0.0:5000" }
                }
            }
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="DeathStarBenchClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public DeathStarBenchClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
        {
            this.serverOnlinePollingTimeout = TimeSpan.FromHours(1);
            this.clientExecutionRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);
        }

        /// <summary>
        /// The number of Threads for running DeathStarBench workload.
        /// </summary>
        public string NumberOfThreads
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DeathStarBenchClientExecutor.NumberOfThreads));
            }
        }

        /// <summary>
        /// The number of Connections for running DeathStarBench workload. NumberOfConnections should be greater than NumberOfThreads.
        /// </summary>
        public string NumberOfConnections
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DeathStarBenchClientExecutor.NumberOfConnections));
            }
        }

        /// <summary>
        /// The duration for running DeathStarBench workload.
        /// </summary>
        public string Duration
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DeathStarBenchClientExecutor.Duration));
            }
        }

        /// <summary>
        /// The number of request per sec for running DeathStarBench workload.
        /// </summary>
        public string RequestPerSec
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DeathStarBenchClientExecutor.RequestPerSec));
            }
        }

        /// <summary>
        /// Executes the client threads.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(DeathStarBenchClientExecutor)}.Execute", telemetryContext, async () =>
            {
                if (this.IsMultiRoleLayout())
                {
                    await this.clientExecutionRetryPolicy.ExecuteAsync(async () =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.ExecuteClientWorkloadAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                        }
                    }).ConfigureAwait(false);
                }
                else
                {
                    DeathStarBenchState expectedServerState = new DeathStarBenchState(this.ServiceName, true);

                    this.Logger.LogTraceMessage("Client waiting for server to be up");
                    await this.ServerApiClient.PollForExpectedStateAsync(
                        nameof(DeathStarBenchState),
                        JObject.FromObject(expectedServerState),
                        DeathStarBenchExecutor.StateConfirmationPollingTimeout,
                        DefaultStateComparer.Instance,
                        cancellationToken).ConfigureAwait(false);
                    try
                    {
                        this.Logger.LogTraceMessage("Client Execution start");
                        await this.ExecuteClientAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        this.Logger.LogTraceMessage("Client Stopping docker services");
                        await this.StopDockerAsync(CancellationToken.None).ConfigureAwait(false);

                        this.Logger.LogTraceMessage("Deleting states");
                        this.DeleteWorkloadStateAsync(telemetryContext, cancellationToken).GetAwaiter().GetResult();
                    }
                }

            });
        }

        private Task ExecuteClientWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(DeathStarBenchClientExecutor)}.ExecuteClientWorkload", telemetryContext, async () =>
            {
                this.Logger.LogTraceMessage("Synchronization: Wait for server online...");

                // 1) Confirm server is online.
                // ===========================================================================
                await this.ServerApiClient.PollForHeartbeatAsync(this.serverOnlinePollingTimeout, cancellationToken)
                    .ConfigureAwait(false);

                // 2) Wait for the server to signal the eventing API is online.
                // ===========================================================================
                await this.ServerApiClient.PollForServerOnlineAsync(this.serverOnlinePollingTimeout, cancellationToken)
                    .ConfigureAwait(false);

                this.Logger.LogTraceMessage($"{nameof(DeathStarBenchClientExecutor)}.ServerOnline");

                // 3) Request the server to stop ALL workload processes (Reset)
                // ===========================================================================
                this.Logger.LogTraceMessage("Synchronization: Request server to stop all workloads...");

                await this.ResetServerAsync(telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                // stop docker services at client side.
                await this.StopDockerAsync(cancellationToken).ConfigureAwait(false);

                // 4) Request the server to start the next workload.
                // ===========================================================================
                Item<Instructions> instructions = new Item<Instructions>(
                nameof(Instructions),
                new Instructions(InstructionsType.ClientServerStartExecution, new Dictionary<string, IConvertible>
                {
                    ["ServiceName"] = this.ServiceName
                }));

                this.Logger.LogTraceMessage($"Synchronization: Request server to start DeathStarBench workload...");

                await this.ServerApiClient.SendInstructionsAsync(JObject.FromObject(instructions), cancellationToken)
                    .ConfigureAwait(false);

                // 5) Confirm the server has started the requested workload.
                // ===========================================================================
                this.Logger.LogTraceMessage("Synchronization: Wait for start of server workload...");

                // We can directly poll for the expected state. Do this.Most probably will work because we don't have different tools.
                DeathStarBenchState expectedServerState = new DeathStarBenchState(this.ServiceName, true);

                await this.ServerApiClient.PollForExpectedStateAsync(
                    nameof(DeathStarBenchState),
                    JObject.FromObject(expectedServerState),
                    DeathStarBenchExecutor.StateConfirmationPollingTimeout,
                    DefaultStateComparer.Instance,
                    cancellationToken).ConfigureAwait(false);

                this.Logger.LogTraceMessage("Synchronization: Server workload startup confirmed...");
                this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                // 6) Execute the client workload.
                // ===========================================================================
                try
                {
                    await this.ExecuteClientAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    // Stop docker services at client side(If multiVM get out of swarm network and single VM stop the services).
                    await this.StopDockerAsync(CancellationToken.None).ConfigureAwait(false);

                    this.Logger.LogTraceMessage("Synchronization: Wait for server to stop workload after being requested to stop...");
                    // send Instructions to server for ClientserverReset. 
                    await this.ResetServerAsync(telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                }

            });
        }

        private async Task ResetServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            Item<Instructions> instructions = new Item<Instructions>(
                nameof(Instructions),
                new Instructions(InstructionsType.ClientServerReset, new Dictionary<string, IConvertible>
                {
                    ["ServiceName"] = this.ServiceName
                }));

            HttpResponseMessage response = await this.ServerApiClient.SendInstructionsAsync(JObject.FromObject(instructions), cancellationToken)
                .ConfigureAwait(false);

            response.ThrowOnError<WorkloadException>();

            this.Logger.LogTraceMessage("sent instructions to server to reset the server.");

            this.Logger.LogTraceMessage("polling for state delete");

            // Confirm the server has stopped all workloads
            await DeathStarBenchClientExecutor.PollUntilStateDeletedAsync(
                this.ServerApiClient,
                nameof(DeathStarBenchState),
                DeathStarBenchExecutor.StateConfirmationPollingTimeout,
                cancellationToken).ConfigureAwait(false);

        }

        private async Task ExecuteClientAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.workPath = this.PlatformSpecifics.Combine(this.ServiceDirectory, "wrk2");

            this.StartTime = DateTime.Now;

            if (this.IsMultiRoleLayout())
            {
                string joinSwarmCommand = await this.GetJoinSwarmCommand(cancellationToken)
                    .ConfigureAwait(false);
                await this.ExecuteCommandAsync<DeathStarBenchClientExecutor>(joinSwarmCommand, this.ServiceDirectory, cancellationToken)
                .ConfigureAwait(false);

                await this.WaitAsync(DeathStarBenchExecutor.ServerWarmUpTime, cancellationToken)
                .ConfigureAwait(false);
            }

            if (string.Equals(this.ServiceName, DeathStarBenchExecutor.MediaMicroservices, StringComparison.OrdinalIgnoreCase))
            {
                string wrkPath = this.PlatformSpecifics.Combine(this.workPath, "wrk");
                await this.SystemManager.MakeFileExecutableAsync(wrkPath, this.Platform, cancellationToken)
                        .ConfigureAwait(false);
            }

            await this.ExecuteCommandAsync<DeathStarBenchClientExecutor>("make", this.workPath, cancellationToken)
                .ConfigureAwait(false);

            foreach (var action in this.actionScript[this.ServiceName.ToLower()].Keys)
            {
                string resultsPath = this.PlatformSpecifics.Combine(this.workPath, $"results.txt");
                this.ResetFile(resultsPath, telemetryContext);

                await this.ExecuteCommandAsync<DeathStarBenchClientExecutor>(
                    @$"bash -c ""./wrk -D exp -t {this.NumberOfThreads} -c {this.NumberOfConnections} -d {this.Duration} -L -s {this.actionScript[this.ServiceName.ToLower()][action]} -R {this.RequestPerSec} >> results.txt""",
                    this.workPath,
                    cancellationToken)
                .ConfigureAwait(false);

                await this.CaptureWorkloadResultsAsync(resultsPath, $"{this.ServiceName}_{action}", this.StartTime, DateTime.Now, telemetryContext)
                    .ConfigureAwait(false);
            }
        }

        private async Task CaptureWorkloadResultsAsync(string resultsFilePath, string scenarioName, DateTime startTime, DateTime endTime, EventContext telemetryContext)
        {
            IFileSystem fileSystem = this.Dependencies.GetService<IFileSystem>();
            if (!fileSystem.File.Exists(resultsFilePath))
            {
                throw new WorkloadException(
                    $"The DeathStarBench results file was not found at path '{resultsFilePath}'.",
                    ErrorReason.WorkloadFailed);
            }

            string resultsContent = await fileSystem.File.ReadAllTextAsync(resultsFilePath)
                .ConfigureAwait(false);

            DeathStarBenchMetricsParser deathStarBenchMetricsParser = new DeathStarBenchMetricsParser(resultsContent);
            IList<Metric> metrics = deathStarBenchMetricsParser.Parse();
            this.Logger.LogMetrics(
                        "DeathStarBench",
                        scenarioName,
                        startTime,
                        endTime,
                        metrics,
                        string.Empty,
                        this.Parameters.ToString(),
                        this.Tags,
                        telemetryContext);
        }

        /// <summary>
        /// Gets the parameters that define the scale configuration for the DeathStarBench.
        /// </summary>
        private async Task<string> GetJoinSwarmCommand(CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await this.ServerApiClient.GetStateAsync(nameof(this.SwarmCommand), cancellationToken)
               .ConfigureAwait(false);

            response.ThrowOnError<WorkloadException>();

            string responseContent = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            State state = responseContent.FromJson<Item<State>>().Definition;

            return state.Properties[nameof(this.SwarmCommand)].ToString();
        }
    }
}
