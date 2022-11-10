// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// This manager can be used to interact with any server-side endpoint (including a localhost endpoint).
    /// </summary>
    public static class ClientServerInteractionsExtensions
    {
        private static ILogger defaultLogger = NullLogger.Instance;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static async Task<Item<ClientServerState>> GetStateAsync(this IApiClient client, string stateId, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null, ILogger logger = null)
        {
            client.ThrowIfNull(nameof(client));
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            EventContext telemetryContext = EventContext.Persisted();
            return await (logger ?? defaultLogger).LogMessageAsync("ClientServerInteractions.GetState", telemetryContext, async () =>
            {
                HttpResponseMessage response = await client.GetStateAsync(stateId, cancellationToken, retryPolicy)
                    .ConfigureAwait(false);

                telemetryContext.AddResponseContext(response);
                response.ThrowOnError<ApiException>(ErrorReason.HttpNonSuccessResponse);

                string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return responseContent.FromJson<Item<ClientServerState>>();

            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task SendInstructionsAsync(this IApiClient client, Item<ClientServerRequest> instructions, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null, ILogger logger = null)
        {
            client.ThrowIfNull(nameof(client));
            instructions.ThrowIfNull(nameof(instructions));

            EventContext telemetryContext = EventContext.Persisted();
            await (logger ?? defaultLogger).LogMessageAsync($"ClientServerInteractions.SendInstructions", telemetryContext, async () =>
            {
                HttpResponseMessage response = await client.SendInstructionsAsync(JObject.FromObject(instructions), cancellationToken)
                    .ConfigureAwait(false);

                telemetryContext.AddResponseContext(response);
                response.ThrowOnError<ApiException>(ErrorReason.HttpNonSuccessResponse);

            }).ConfigureAwait(false);
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task SynchronizeStateAsync(this IApiClient client, Item<ClientServerState> state, CancellationToken cancellationToken, TimeSpan? timeout = null, ILogger logger = null)
        {
            client.ThrowIfNull(nameof(client));
            state.ThrowIfNull(nameof(state));

            EventContext telemetryContext = EventContext.Persisted();
            await (logger ?? defaultLogger).LogMessageAsync("ClientServerInteractions.SynchronizeState", telemetryContext, async () =>
            {
                JObject stateObject = JObject.FromObject(state.Definition);
                await client.PollForExpectedStateAsync(state.Id, stateObject, timeout ?? TimeSpan.FromMinutes(2), DefaultStateComparer.Instance, cancellationToken)
                    .ConfigureAwait(false);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        public static async Task VerifyOnlineAsync(this IApiClient client, CancellationToken cancellationToken, TimeSpan? timeout = null, ILogger logger = null)
        {
            client.ThrowIfNull(nameof(client));

            EventContext telemetryContext = EventContext.Persisted();
            await (logger ?? defaultLogger).LogMessageAsync("ClientServerInteractions.VerifyOnline", telemetryContext, async () =>
            {
                HttpResponseMessage response = await client.PollForHeartbeatAsync(timeout ?? TimeSpan.FromMinutes(2), cancellationToken)
                    .ConfigureAwait(false);

                telemetryContext.AddResponseContext(response);

            }).ConfigureAwait(false);
        }
    }
}
