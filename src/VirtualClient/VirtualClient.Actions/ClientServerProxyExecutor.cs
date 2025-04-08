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
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Rest;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Core;

    /// <summary>
    /// Allows an action defined in a profile to be sent to one or more remote instances of the
    /// Virtual Client for execution.
    /// </summary>
    internal class ClientServerProxyExecutor : VirtualClientComponentCollection
    {
        private static readonly List<string> CompletedStatuses = new List<string>
        {
            ClientServerStatus.ExecutionCompleted.ToString(),
            ClientServerStatus.Failed.ToString()
        };

        private IApiClientManager apiClientManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ClientServerProxyExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>(exc =>
            {
                VirtualClientException vcError = exc as VirtualClientException;
                if (vcError != null)
                {
                    return (int)vcError.Reason < 500;
                }

                return true;
            }).WaitAndRetryAsync(3, retries => TimeSpan.FromSeconds(1));

            this.PollingTimeout = TimeSpan.FromMinutes(30);
            this.StateConfirmationTimeout = TimeSpan.FromMinutes(10);

            this.apiClientManager = dependencies.GetService<IApiClientManager>();
        }

        /// <summary>
        /// The Virtual Client instance role to which the child actions will be sent/forwarded.
        /// </summary>
        public string TargetRole
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.TargetRole), "localhost");
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
        /// The API client for communications with the server.
        /// </summary>
        protected IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// The timespan at which the client will poll the server for responses before
        /// timing out.
        /// </summary>
        protected TimeSpan StateConfirmationTimeout { get; set; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // Overview:
            // ----------------------------------------------------------------------------------------------------------------------
            // The ExecuteAsync method is where the core workload/test logic is executed. By the time we are here, we should
            // be confident that all required dependencies have been verified in the InitializeAsync() method below and
            // that we are ready to rock!!

            IEnumerable<ClientInstance> targetServers = this.GetTargetInstances();

            foreach (VirtualClientComponent component in this)
            {
                List<Task> clientWorkloadTasks = new List<Task>();

                foreach (ClientInstance server in targetServers)
                {
                    // Reliability/Recovery:
                    // The pattern here is to allow for any steps within the workflow to fail and to simply start the entire workflow
                    // over again.
                    clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            IApiClient serverApiClient = this.apiClientManager.GetOrCreateApiClient(server.Name, server);

                            // 1) Confirm server is online.
                            await this.PollForTargetInstanceOnlineAsync(serverApiClient, cancellationToken)
                                .ConfigureAwait(false);

                            // 2) Ask the server to reset and stop any workloads running.
                            await this.SendAndPollForResetAsync(serverApiClient, cancellationToken)
                                .ConfigureAwait(false);

                            // 3) Send instructions to execute a workload/component and poll for completion.
                            await this.SendInstructionsAndPollForCompletionAsync(serverApiClient, component, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }));
                }

                await Task.WhenAll(clientWorkloadTasks).ConfigureAwait(false);
            }
        }

        private async Task PollForTargetInstanceOnlineAsync(IApiClient serverApiClient, CancellationToken cancellationToken)
        {
            this.Logger.LogTraceMessage("Sync: Poll server-side API for heartbeat...");

            await serverApiClient.PollForHeartbeatAsync(this.PollingTimeout, cancellationToken)
                .ConfigureAwait(false);

            this.Logger.LogTraceMessage("Sync: Poll server-side for online signal...");

            await serverApiClient.PollForServerOnlineAsync(this.PollingTimeout, cancellationToken)
                .ConfigureAwait(false);

            this.Logger.LogTraceMessage("Sync: Server-side online signal confirmed...");
        }

        private async Task SendInstructionsAndPollForCompletionAsync(IApiClient serverApiClient, VirtualClientComponent component, CancellationToken cancellationToken)
        {
            // Send instructions to execute a workload/component on the server-side
            Instructions instructions = new Instructions(InstructionsType.ClientServerStartExecution);
            instructions.AddComponent(component.TypeName, component.Parameters);

            this.Logger.LogTraceMessage("Synchronization: Send client workload request...");

            HttpResponseMessage response = await serverApiClient.SendInstructionsAsync<Instructions>(instructions, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new WorkloadException(
                    $"Workload instructions request failed with response '{(int)response.StatusCode}/{response.StatusCode.ToString()}'",
                    ErrorReason.ApiRequestFailed);
            }

            // Poll until the server-side indicates the workload is either completed or failed.
            Item<Instructions> instructionsInstance = await response.Content.ReadAsJsonAsync<Item<Instructions>>()
                .ConfigureAwait(false);

            await serverApiClient.PollForExpectedStateAsync<State>(
                instructionsInstance.Id,
                (state => ClientServerProxyExecutor.CompletedStatuses.Contains(state.Status(), StringComparer.OrdinalIgnoreCase)),
                TimeSpan.FromDays(90),
                cancellationToken).ConfigureAwait(false);

            Item<State> state = await serverApiClient.GetStateAsync<State>(instructionsInstance.Id, cancellationToken)
                .ConfigureAwait(false);

            if (string.Equals(state.Definition.Status(), ClientServerStatus.Failed.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                int? errorReasonCode = state.Definition.ErrorReason();
                string errorMessage = $"Server-side workload failed for component '{component.TypeName}' on target instance '{serverApiClient.BaseUri.Host}'. {state.Definition.ErrorMessage()}";
                ErrorReason errorReason = errorReasonCode != null ? (ErrorReason)errorReasonCode : ErrorReason.WorkloadFailed;

                throw new WorkloadException(errorMessage, errorReason);
            }
        }

        private async Task SendAndPollForResetAsync(IApiClient serverApiClient, CancellationToken cancellationToken)
        {
            Instructions instructions = new Instructions(InstructionsType.ClientServerReset);

            this.Logger.LogTraceMessage("Sync: Send reset request...");

            HttpResponseMessage response = await serverApiClient.SendInstructionsAsync<Instructions>(instructions, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                throw new WorkloadException(
                    $"Reset request failed with response '{(int)response.StatusCode}/{response.StatusCode.ToString()}'",
                    ErrorReason.ApiRequestFailed);
            }

            Item<Instructions> instructionsInstance = await response.Content.ReadAsJsonAsync<Item<Instructions>>()
                .ConfigureAwait(false);

            await serverApiClient.PollForExpectedStateAsync<State>(
                instructionsInstance.Id,
                (state => string.Equals(state.Status(), ClientServerStatus.ResetCompleted)),
                this.StateConfirmationTimeout,
                cancellationToken).ConfigureAwait(false);

            this.Logger.LogTraceMessage("Sync: Reset confirmed...");
        }

        private IEnumerable<ClientInstance> GetTargetInstances()
        {
            IEnumerable<ClientInstance> targetServers = null;

            if (this.TargetRole == "localhost")
            {
                targetServers = new List<ClientInstance> { new ClientInstance(Environment.MachineName, IPAddress.Loopback.ToString()) };
            }
            else
            { 
                targetServers = this.GetLayoutClientInstances(this.TargetRole, throwIfNotExists: false);

                if (targetServers?.Any() != true)
                {
                    targetServers = this.GetLayoutClientInstances(this.TargetRole);
                }
            }

            if (targetServers?.Any() != true)
            {
                throw new DependencyException(
                    $"The set of target servers cannot be determined from one or more of the '{this.TypeName}' definitions in the profile.",
                    ErrorReason.DependencyNotFound);
            }

            return targetServers;
        }
    }
}