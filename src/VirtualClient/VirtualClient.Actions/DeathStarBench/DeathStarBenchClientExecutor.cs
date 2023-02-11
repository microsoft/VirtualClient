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
    using VirtualClient.Common;
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
        public string ThreadCount
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DeathStarBenchClientExecutor.ThreadCount));
            }
        }

        /// <summary>
        /// The number of Connections for running DeathStarBench workload. ConnectionCount should be greater than ThreadCount.
        /// </summary>
        public string ConnectionCount
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DeathStarBenchClientExecutor.ConnectionCount));
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
                            await this.ExecuteClientWorkloadAsync(telemetryContext, cancellationToken).ConfigureAwait();
                        }
                    }).ConfigureAwait();
                }
                else
                {
                    DeathStarBenchState expectedServerState = new DeathStarBenchState(this.ServiceName, true);

                    await this.ServerApiClient.PollForExpectedStateAsync(
                        nameof(DeathStarBenchState),
                        JObject.FromObject(expectedServerState),
                        DeathStarBenchExecutor.StateConfirmationPollingTimeout,
                        DefaultStateComparer.Instance,
                        cancellationToken).ConfigureAwait();

                    try
                    {
                        this.Logger.LogTraceMessage("Client Execution start");
                        await this.ExecuteClientAsync(telemetryContext, cancellationToken)
                            .ConfigureAwait();
                    }
                    finally
                    {
                        this.Logger.LogTraceMessage("Client Stopping docker services");
                        await this.StopDockerAsync(CancellationToken.None)
                            .ConfigureAwait();

                        this.Logger.LogTraceMessage("Deleting states");
                        await this.DeleteWorkloadStateAsync(telemetryContext, cancellationToken)
                            .ConfigureAwait();
                    }
                }

            });
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, string resultsFilePath, string scenarioName, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                IFileSystem fileSystem = this.Dependencies.GetService<IFileSystem>();
                if (!fileSystem.File.Exists(resultsFilePath))
                {
                    throw new WorkloadException(
                        $"The DeathStarBench results file was not found at path '{resultsFilePath}'.",
                        ErrorReason.WorkloadFailed);
                }

                string content = await fileSystem.File.ReadAllTextAsync(resultsFilePath)
                    .ConfigureAwait();

                await this.LogProcessDetailsAsync(process, telemetryContext, "DeathStarBench", results: content ?? string.Empty, logToFile: true)
                    .ConfigureAwait();

                if (content != null)
                {
                    DeathStarBenchMetricsParser deathStarBenchMetricsParser = new DeathStarBenchMetricsParser(content);
                    IList<Metric> metrics = deathStarBenchMetricsParser.Parse();

                    this.Logger.LogMetrics(
                        "DeathStarBench",
                        scenarioName,
                        process.StartTime,
                        process.ExitTime,
                        metrics,
                        null,
                        this.Parameters.ToString(),
                        this.Tags,
                        telemetryContext);
                }
            }
        }

        private Task ExecuteClientWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(DeathStarBenchClientExecutor)}.ExecuteClientWorkload", telemetryContext, async () =>
            {
                this.Logger.LogTraceMessage("Synchronization: Wait for server online...");

                // 1) Confirm server is online.
                // ===========================================================================
                await this.ServerApiClient.PollForHeartbeatAsync(this.serverOnlinePollingTimeout, cancellationToken)
                    .ConfigureAwait();

                // 2) Wait for the server to signal the eventing API is online.
                // ===========================================================================
                await this.ServerApiClient.PollForServerOnlineAsync(this.serverOnlinePollingTimeout, cancellationToken)
                    .ConfigureAwait();

                this.Logger.LogTraceMessage($"{nameof(DeathStarBenchClientExecutor)}.ServerOnline");

                // 3) Request the server to stop ALL workload processes (Reset)
                // ===========================================================================
                this.Logger.LogTraceMessage("Synchronization: Request server to stop all workloads...");

                await this.ResetServerAsync(telemetryContext, cancellationToken)
                    .ConfigureAwait();

                // stop docker services at client side.
                await this.StopDockerAsync(cancellationToken)
                    .ConfigureAwait();

                // 4) Request the server to start the next workload.
                // ===========================================================================
                Item<Instructions> instructions = new Item<Instructions>(
                nameof(Instructions),
                new Instructions(InstructionsType.ClientServerStartExecution, new Dictionary<string, IConvertible>
                {
                    ["ServiceName"] = this.ServiceName
                }));

                this.Logger.LogTraceMessage($"Synchronization: Request server to start workload...");

                await this.ServerApiClient.SendInstructionsAsync(JObject.FromObject(instructions), cancellationToken)
                    .ConfigureAwait();

                // 5) Confirm the server has started the requested workload.
                // ===========================================================================
                this.Logger.LogTraceMessage("Synchronization: Wait for start of server workload...");

                await this.ServerApiClient.PollForExpectedStateAsync<DeathStarBenchState>(
                    nameof(DeathStarBenchState),
                    (state) => state.ServiceName == this.ServiceName && state.ServiceState == true,
                    DeathStarBenchExecutor.StateConfirmationPollingTimeout,
                    cancellationToken,
                    this.Logger).ConfigureAwait();

                this.Logger.LogTraceMessage("Synchronization: Server workload startup confirmed...");
                this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                // 6) Execute the client workload.
                // ===========================================================================
                try
                {
                    await this.ExecuteClientAsync(telemetryContext, cancellationToken).ConfigureAwait();
                }
                finally
                {
                    // Stop docker services at client side(If multiVM get out of swarm network and single VM stop the services).
                    await this.StopDockerAsync(CancellationToken.None)
                        .ConfigureAwait();

                    this.Logger.LogTraceMessage("Synchronization: Wait for server to stop workload...");

                    // send Instructions to server for ClientserverReset. 
                    await this.ResetServerAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait();
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
                .ConfigureAwait();

            response.ThrowOnError<WorkloadException>();

            this.Logger.LogTraceMessage("Synchronization: Request server to stop workload...");

            // Confirm the server has stopped all workloads
            await DeathStarBenchClientExecutor.PollUntilStateDeletedAsync(
                this.ServerApiClient,
                nameof(DeathStarBenchState),
                DeathStarBenchExecutor.StateConfirmationPollingTimeout,
                cancellationToken).ConfigureAwait();

        }

        private async Task ExecuteClientAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.workPath = this.PlatformSpecifics.Combine(this.ServiceDirectory, "wrk2");

            if (this.IsMultiRoleLayout())
            {
                string joinSwarmCommand = await this.GetJoinSwarmCommand(cancellationToken)
                    .ConfigureAwait();

                await this.ExecuteCommandAsync(joinSwarmCommand, this.ServiceDirectory, cancellationToken)
                    .ConfigureAwait();

                await this.WaitAsync(DeathStarBenchExecutor.ServerWarmUpTime, cancellationToken)
                    .ConfigureAwait();
            }

            if (string.Equals(this.ServiceName, DeathStarBenchExecutor.MediaMicroservices, StringComparison.OrdinalIgnoreCase))
            {
                string wrkPath = this.PlatformSpecifics.Combine(this.workPath, "wrk");
                await this.SystemManager.MakeFileExecutableAsync(wrkPath, this.Platform, cancellationToken)
                        .ConfigureAwait();
            }

            await this.ExecuteCommandAsync("make", this.workPath, cancellationToken)
                .ConfigureAwait();

            foreach (var action in this.actionScript[this.ServiceName.ToLower()].Keys)
            {
                string resultsPath = this.PlatformSpecifics.Combine(this.workPath, $"results.txt");
                this.ResetFile(resultsPath, telemetryContext);

                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        @$"bash -c ""./wrk -D exp -t {this.ThreadCount} -c {this.ConnectionCount} -d {this.Duration} -L -s {this.actionScript[this.ServiceName.ToLower()][action]} -R {this.RequestPerSec} >> results.txt""",
                        this.workPath,
                        telemetryContext,
                        cancellationToken,
                        runElevated: true).ConfigureAwait())
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "DeathStarBench")
                                .ConfigureAwait();

                            process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);

                            await this.CaptureMetricsAsync(process, resultsPath, $"{this.ServiceName}_{action}", telemetryContext, cancellationToken)
                                .ConfigureAwait();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets the parameters that define the scale configuration for the DeathStarBench.
        /// </summary>
        private async Task<string> GetJoinSwarmCommand(CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await this.ServerApiClient.GetStateAsync(nameof(this.SwarmCommand), cancellationToken)
               .ConfigureAwait();

            response.ThrowOnError<WorkloadException>();

            string responseContent = await response.Content.ReadAsStringAsync()
                .ConfigureAwait();

            State state = responseContent.FromJson<Item<State>>().Definition;

            return state.Properties[nameof(this.SwarmCommand)].ToString();
        }
    }
}
