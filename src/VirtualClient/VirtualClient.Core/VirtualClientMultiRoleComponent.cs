// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Core
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Rest;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;
    using VirtualClient.Core;

    /// <summary>
    /// The base class for all Virtual Client profile actions and monitors.
    /// </summary>
    public abstract class VirtualClientMultiRoleComponent : VirtualClientComponent
    {
        private static readonly List<string> CompletedStatuses = new List<string>
        {
            ClientServerStatus.ExecutionCompleted.ToString(),
            ClientServerStatus.Failed.ToString()
        };

        private ISystemManagement systemManagement;
        private IApiClientManager apiClientManager;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="parameters"></param>
        protected VirtualClientMultiRoleComponent(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.apiClientManager = dependencies.GetService<IApiClientManager>();
        }

        /// <summary>
        /// General retry policy when used in talking with other clients.
        /// </summary>
        protected IAsyncPolicy RetryPolicy { get; set; } =
            Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
                .WaitAndRetryAsync(3, (retries) => TimeSpan.FromSeconds(retries));

        /// <summary>
        /// The timespan at which the client will poll the server for responses before
        /// timing out.
        /// </summary>
        protected TimeSpan PollingTimeout { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="role"></param>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected Task WaitForRoleAsync(string role, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            List<Task> tasks = new List<Task>();

            if (this.IsMultiRoleLayout())
            {
                IEnumerable<ClientInstance> targetServers = this.GetLayoutClientInstances(role);
                foreach (ClientInstance server in targetServers)
                {
                    // Reliability/Recovery:
                    // The pattern here is to allow for any steps within the workflow to fail and to simply start the entire workflow
                    // over again.
                    tasks.Add(this.RetryPolicy.ExecuteAsync(async () =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            IApiClient apiClient = this.apiClientManager.GetOrCreateApiClient(server.Name, server);

                            // 1) Confirm server is online.
                            // ===========================================================================
                            this.Logger.LogTraceMessage("Synchronization: Poll server API for heartbeat...");
                            await apiClient.PollForHeartbeatAsync(this.PollingTimeout, cancellationToken);
                        }
                    }));
                }
            }

            return Task.WhenAll(tasks);
        }

        /// <summary>
        /// Not implemented yet. Designed to terminate all corresponding roles in a layout.
        /// </summary>
        /// <param name="role">Name of the role to terminate</param>
        protected void RegisterToTerminateRole(string role)
        {
            IEnumerable<ClientInstance> targetServers = this.GetLayoutClientInstances(role);
            IList<IApiClient> clients = new List<IApiClient>();
            foreach (ClientInstance server in targetServers)
            {
                clients.Add(this.apiClientManager.GetOrCreateApiClient(server.Name, server));
            }

            this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", clients.ToArray());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        /// <exception cref="WorkloadException"></exception>
        protected Task SendInstructionsAndPollForCompletionAsync(string role, CancellationToken cancellationToken)
        {
            // Send instructions to execute a workload/component on the server-side
            Instructions instructions = new Instructions(InstructionsType.ClientServerStartExecution);
            instructions.AddComponent(this.TypeName, this.Parameters);

            List<Task> tasks = new List<Task>();

            if (this.IsMultiRoleLayout())
            {
                IEnumerable<ClientInstance> targetServers = this.GetLayoutClientInstances(role);
                foreach (ClientInstance server in targetServers)
                {
                    // Reliability/Recovery:
                    // The pattern here is to allow for any steps within the workflow to fail and to simply start the entire workflow
                    // over again.
                    tasks.Add(this.RetryPolicy.ExecuteAsync(async () =>
                    {
                        IApiClient apiClient = this.apiClientManager.GetOrCreateApiClient(server.Name, server);

                        this.Logger.LogTraceMessage("Synchronization: Send client workload request...");

                        HttpResponseMessage response = await apiClient.SendInstructionsAsync<Instructions>(instructions, cancellationToken)
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

                        await apiClient.PollForExpectedStateAsync<State>(
                            instructionsInstance.Id,
                            (state => VirtualClientMultiRoleComponent.CompletedStatuses.Contains(state.Status(), StringComparer.OrdinalIgnoreCase)),
                            TimeSpan.FromDays(90),
                            cancellationToken).ConfigureAwait(false);

                        Item<State> state = await apiClient.GetStateAsync<State>(instructionsInstance.Id, cancellationToken)
                            .ConfigureAwait(false);

                        if (string.Equals(state.Definition.Status(), ClientServerStatus.Failed.ToString(), StringComparison.OrdinalIgnoreCase))
                        {
                            int? errorReasonCode = state.Definition.ErrorReason();
                            string errorMessage = $"Server-side workload failed for component '{this.TypeName}' on target instance '{apiClient.BaseUri.Host}'. {state.Definition.ErrorMessage()}";
                            ErrorReason errorReason = errorReasonCode != null ? (ErrorReason)errorReasonCode : ErrorReason.WorkloadFailed;

                            throw new WorkloadException(errorMessage, errorReason);
                        }
                    }));
                }
            }

            return Task.WhenAll(tasks);
        }
    }
}