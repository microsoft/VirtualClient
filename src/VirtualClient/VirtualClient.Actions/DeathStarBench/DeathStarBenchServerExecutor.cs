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
    using VirtualClient.Common;
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
        private string tokenFilePath;

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
                    await this.DeleteStateAsync(telemetryContext, cancellationToken);
                    await this.StopDockerAsync(telemetryContext, this.ServerCancellationSource.Token);
                    await this.ExecuteServerAsync(telemetryContext, cancellationToken);
                    await this.SaveStateAsync(this.ServiceName, telemetryContext, cancellationToken);

                    if (this.IsMultiRoleLayout())
                    {
                        await this.WaitAsync(cancellationToken);
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
                    Console.WriteLine($"Service Directory: {this.ServiceDirectory}");
                    this.tokenFilePath = this.PlatformSpecifics.Combine(this.ServiceDirectory, "token.txt");
                    this.ResetFile(this.tokenFilePath, telemetryContext);

                    ClientInstance clientInstance = this.GetLayoutClientInstance();
                    this.ServerIpAddress = clientInstance.IPAddress;

                    string swarmHostCommand = $@"bash -c ""docker swarm init --advertise-addr {this.ServerIpAddress} | grep ' docker swarm join' >> token.txt""";

                    using (IProcessProxy process = await this.ExecuteCommandAsync(swarmHostCommand, this.ServiceDirectory, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                        }
                    }

                    await this.SetOrUpdateClientCommandLine(cancellationToken)
                        .ConfigureAwait();

                    if (!string.Equals(this.ServiceName, DeathStarBenchExecutor.MediaMicroservices, StringComparison.OrdinalIgnoreCase))
                    {
                        using (IProcessProxy process = await this.ExecuteCommandAsync(@$"bash -c ""docker stack deploy --compose-file=docker-compose-swarm.yml {this.ServiceName}""", this.ServiceDirectory, telemetryContext, cancellationToken, runElevated: true))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                            }
                        }
                    }
                    else
                    {
                        using (IProcessProxy process = await this.ExecuteCommandAsync(@$"bash -c ""docker stack deploy --compose-file=docker-compose.yml {this.ServiceName}""", this.ServiceDirectory, telemetryContext, cancellationToken, runElevated: true))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                            }
                        }
                    }
                }
                else
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync($"docker-compose -f docker-compose.yml up -d", this.ServiceDirectory, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                        }
                    }
                }

                // wait for docker services to be up and running.
                await this.WaitAsync(DeathStarBenchExecutor.ServerWarmUpTime, cancellationToken)
                    .ConfigureAwait();

                if (string.Equals(this.ServiceName, DeathStarBenchExecutor.MediaMicroservices, StringComparison.OrdinalIgnoreCase))
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync("python3 scripts/write_movie_info.py -c datasets/tmdb/casts.json -m datasets/tmdb/movies.json", this.ServiceDirectory, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                        }
                    }
                }
                else if (string.Equals(this.ServiceName, DeathStarBenchExecutor.SocialNetwork, StringComparison.OrdinalIgnoreCase))
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(@$"bash -c ""python3 scripts/init_social_graph.py --graph={this.GraphType} --limit=1000""", this.ServiceDirectory, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                        }
                    }
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
                    VirtualClientRuntime.ReceiveInstructions -= this.OnInstructionsReceived;
                    this.ServerCancellationSource?.Dispose();
                }

                this.disposed = true;
            }
        }

        private Task DeleteStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();
            return this.Logger.LogMessageAsync($"{this.TypeName}.DeleteState", relatedContext, async () =>
            {
                using (HttpResponseMessage response = await this.LocalApiClient.DeleteStateAsync(nameof(DeathStarBenchState), cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        relatedContext.AddResponseContext(response);
                        response.ThrowOnError<WorkloadException>();
                    }
                }
            });
        }

        private Task SaveStateAsync(string serviceName, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();
            return this.Logger.LogMessageAsync($"{this.TypeName}.SaveState", relatedContext, async () =>
            {
                using (HttpResponseMessage response = await this.LocalApiClient.UpdateStateAsync(
                    nameof(DeathStarBenchState),
                    new Item<DeathStarBenchState>(nameof(DeathStarBenchState), new DeathStarBenchState(serviceName, true)),
                    cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        relatedContext.AddResponseContext(response);
                        response.ThrowOnError<WorkloadException>();
                    }
                }
            });
        }

        private async Task SetOrUpdateClientCommandLine(CancellationToken cancellationToken)
        {
            ISystemManagement systemManager = this.Dependencies.GetService<ISystemManagement>();

            string clientCommand = await systemManager.FileSystem.File.ReadAllTextAsync(this.tokenFilePath, cancellationToken)
                .ConfigureAwait();

            this.SwarmCommand = new State(new Dictionary<string, IConvertible>
            {
                [nameof(this.SwarmCommand)] = clientCommand
            });

            HttpResponseMessage response = await this.LocalApiClient.GetStateAsync(nameof(this.SwarmCommand), cancellationToken)
                .ConfigureAwait();

            if (!response.IsSuccessStatusCode)
            {
                response = await this.LocalApiClient.CreateStateAsync(nameof(this.SwarmCommand), JObject.FromObject(this.SwarmCommand), cancellationToken)
                    .ConfigureAwait();

                response.ThrowOnError<WorkloadException>();
            }
            else
            {
                string responseContent = await response.Content.ReadAsStringAsync()
                    .ConfigureAwait();

                Item<State> stateItem = responseContent.FromJson<Item<State>>();
                stateItem.Definition.Properties[nameof(this.SwarmCommand)] = clientCommand;

                response = await this.LocalApiClient.UpdateStateAsync(stateItem.Id, JObject.FromObject(stateItem), cancellationToken)
                    .ConfigureAwait();

                response.ThrowOnError<WorkloadException>();
            }
        }
    }
}
