// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// DeathStarBench Server Executor.
    /// </summary>
    public class DeathStarBenchServerExecutor : DeathStarBenchExecutor
    {
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeathStarBenchServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public DeathStarBenchServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// The type of graph for running social Network Scenario of DeathStarBench workload.ex: "socfb-Reed98, ego-twitter".
        /// </summary>
        public string GraphType
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DeathStarBenchServerExecutor.GraphType));
            }
        }

        /// <summary>
        /// Server IpAddress(Ipaddress of the machine that acts as Manager in docker swarm network).
        /// </summary>
        protected string ServerIpAddress { get; set; }

        private string TokenFilePath { get; set; }

        /// <summary>
        /// Executes server side of workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(DeathStarBenchExecutor)}.ExecuteServer", telemetryContext, async () =>
            {
                // The current model uses an event handler to subscribe to events that are processed by the 
                // Events API. Event handlers have a signature that is may be too strict to 
                using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    if (!this.IsMultiRoleLayout())
                    {
                        try
                        {
                            this.Logger.LogTraceMessage("Server Polling until state deleted or not present.");
                            await DeathStarBenchClientExecutor.PollUntilStateDeletedAsync(
                                this.ServerApiClient,
                                nameof(DeathStarBenchState),
                                DeathStarBenchExecutor.StateConfirmationPollingTimeout,
                                cancellationToken).ConfigureAwait(false);

                            await this.StopDockerAsync(this.ServerCancellationSource.Token).ConfigureAwait(false);

                            await this.ExecuteServerAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

                            DeathStarBenchState serverState = new DeathStarBenchState(this.ServiceName, true);
                            HttpResponseMessage response = await this.ServerApiClient.GetOrCreateStateAsync(nameof(DeathStarBenchState), serverState, cancellationToken)
                                                                        .ConfigureAwait(false);

                            response.ThrowOnError<WorkloadException>();

                        }
                        catch (Exception)
                        {
                            throw new Exception($"Some error at server side in {this.ServiceName} scenario");
                        }
                    }
                    else
                    {
                        await this.ExecuteServerAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                    }
                }
            });
        }

        /// <summary>
        /// Executes the server workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task ExecuteServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                if (this.IsMultiRoleLayout())
                {
                    Console.WriteLine($"{this.ServiceDirectory}");
                    this.TokenFilePath = this.PlatformSpecifics.Combine(this.ServiceDirectory, "token.txt");
                    this.ResetFile(this.TokenFilePath, telemetryContext);

                    ClientInstance clientInstance = this.GetLayoutClientInstance();
                    this.ServerIpAddress = clientInstance.IPAddress;

                    string swarmHostCommand = $@"bash -c ""docker swarm init --advertise-addr {this.ServerIpAddress} | grep ' docker swarm join' >> token.txt""";
                    await this.ExecuteCommandAsync<DeathStarBenchServerExecutor>(swarmHostCommand, this.ServiceDirectory, cancellationToken)
                           .ConfigureAwait(false);

                    await this.SetOrUpdateClientCommandLine(cancellationToken)
                           .ConfigureAwait(false);

                    if (!string.Equals(this.ServiceName, DeathStarBenchExecutor.MediaMicroservices, StringComparison.OrdinalIgnoreCase))
                    {
                        await this.ExecuteCommandAsync<DeathStarBenchServerExecutor>(@$"bash -c ""docker stack deploy --compose-file=docker-compose-swarm.yml {this.ServiceName}""", this.ServiceDirectory, cancellationToken)
                                .ConfigureAwait(false);
                    }
                    else
                    {
                        await this.ExecuteCommandAsync<DeathStarBenchServerExecutor>(@$"bash -c ""docker stack deploy --compose-file=docker-compose.yml {this.ServiceName}""", this.ServiceDirectory, cancellationToken)
                                .ConfigureAwait(false);
                    }
                }
                else
                {
                    await this.ExecuteCommandAsync<DeathStarBenchServerExecutor>($"docker-compose -f docker-compose.yml up -d", this.ServiceDirectory, cancellationToken)
                              .ConfigureAwait(false);
                }

                // wait for docker services to be up and running.
                await this.WaitAsync(DeathStarBenchExecutor.ServerWarmUpTime, cancellationToken)
                          .ConfigureAwait(false);

                if (string.Equals(this.ServiceName, DeathStarBenchExecutor.MediaMicroservices, StringComparison.OrdinalIgnoreCase))
                {
                    await this.ExecuteCommandAsync<DeathStarBenchServerExecutor>("python3 scripts/write_movie_info.py -c datasets/tmdb/casts.json -m datasets/tmdb/movies.json", this.ServiceDirectory, cancellationToken)
                              .ConfigureAwait(false);
                }
                else if (string.Equals(this.ServiceName, DeathStarBenchExecutor.SocialNetwork, StringComparison.OrdinalIgnoreCase))
                {
                    await this.ExecuteCommandAsync<DeathStarBenchServerExecutor>(@$"bash -c ""python3 scripts/init_social_graph.py --graph={this.GraphType} --limit=1000""", this.ServiceDirectory, cancellationToken)
                              .ConfigureAwait(false);
                }
            }
            catch
            {
                throw new Exception($"Error occured at starting server in {this.ServiceName}");
            }
        }

        /// <summary>
        /// Disposes of resources used by the class instance.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (!this.disposed)
                {
                    VirtualClientEventing.ReceiveInstructions -= this.OnInstructionsReceived;
                    this.ServerCancellationSource?.Dispose();
                }

                this.disposed = true;
            }
        }

        private async Task SetOrUpdateClientCommandLine(CancellationToken cancellationToken)
        {
            ISystemManagement systemManager = this.Dependencies.GetService<ISystemManagement>();

            string clientCommand = await systemManager.FileSystem.File.ReadAllTextAsync(this.TokenFilePath, cancellationToken)
                                .ConfigureDefaults();

            this.SwarmCommand = new State(new Dictionary<string, IConvertible>
            {
                [nameof(this.SwarmCommand)] = clientCommand
            });

            HttpResponseMessage response = await this.LocalApiClient.GetStateAsync(nameof(this.SwarmCommand), cancellationToken)
                   .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                response = await this.LocalApiClient.CreateStateAsync(nameof(this.SwarmCommand), JObject.FromObject(this.SwarmCommand), cancellationToken)
                    .ConfigureAwait(false);

                response.ThrowOnError<WorkloadException>();
            }
            else
            {
                this.Logger.LogTraceMessage($"Updating Deathstarcommand state ");

                string responseContent = await response.Content.ReadAsStringAsync()
                                                    .ConfigureAwait(false);

                Item<State> stateItem = responseContent.FromJson<Item<State>>();

                stateItem.Definition.Properties[nameof(this.SwarmCommand)] = clientCommand;

                response = await this.LocalApiClient.UpdateStateAsync(stateItem.Id, JObject.FromObject(stateItem), cancellationToken)
                                    .ConfigureAwait(false);

                response.ThrowOnError<WorkloadException>();
            }
        }
    }
}
