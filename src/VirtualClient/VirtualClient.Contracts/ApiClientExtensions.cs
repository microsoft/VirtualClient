// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Net;
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

    /// <summary>
    /// Extension methods for <see cref="IApiClient"/> instances.
    /// </summary>
    public static class ApiClientExtensions
    {
        private static ILogger defaultLogger = NullLogger.Instance;

        /// <summary>
        /// Extension reads the contents of the response body and deserializes it to the type provided.
        /// </summary>
        /// <typeparam name="T">The data type of the object to which the response content will be deserialized.</typeparam>
        /// <param name="response">Http response message.</param>
        /// <returns>Json serailized object from content.</returns>
        public static async Task<T> FromContentAsync<T>(this HttpResponseMessage response)
        {
            string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return responseContent.FromJson<T>();
        }

        /// <summary>
        /// Returns State Content of the given stateId.
        /// </summary>
        /// <typeparam name="TState">The data type of the state object.</typeparam>
        /// <param name="client">The API client instance.</param>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <param name="logger">A logger to use for capturing API telemetry.</param>
        /// <returns>
        /// Content of the state object.
        /// </returns>
        public static Task<Item<TState>> GetStateAsync<TState>(this IApiClient client, string stateId, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null, ILogger logger = null)
        {
            client.ThrowIfNull(nameof(client));
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("uri", client.BaseUri.ToString())
                .AddContext(nameof(stateId), stateId);

            return (logger ?? defaultLogger).LogMessageAsync("ClientServer.GetState", telemetryContext, async () =>
            {
                HttpResponseMessage response = await client.GetStateAsync(stateId, cancellationToken, retryPolicy)
                    .ConfigureAwait(false);

                telemetryContext.AddResponseContext(response);
                response.ThrowOnError<ApiException>(ErrorReason.HttpNonSuccessResponse);

                return await response.FromContentAsync<Item<TState>>();
            });
        }

        /// <summary>
        /// Makes a request to get/create a state object/definition.
        /// </summary>
        /// <param name="client">The API client instance.</param>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="state">The state object/definition.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <param name="logger">A logger to use for capturing API telemetry.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the state object/definition.
        /// </returns>
        public static Task<HttpResponseMessage> GetOrCreateStateAsync(this IApiClient client, string stateId, JObject state, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null, ILogger logger = null)
        {
            client.ThrowIfNull(nameof(client));
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));
            state.ThrowIfNull(nameof(state));

            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("uri", client.BaseUri.ToString())
                .AddContext(nameof(stateId), stateId);

            return (logger ?? defaultLogger).LogMessageAsync("ClientServer.GetOrCreateState", telemetryContext, async () =>
            {
                HttpResponseMessage response = await client.GetStateAsync(stateId, cancellationToken, retryPolicy)
                    .ConfigureAwait(false);

                telemetryContext.AddResponseContext(response);

                if (!response.IsSuccessStatusCode)
                {
                    response = await client.CreateStateAsync(stateId, state, cancellationToken, retryPolicy)
                        .ConfigureAwait(false);

                    telemetryContext.AddResponseContext(response);
                }

                return response;
            });
        }

        /// <summary>
        /// Makes a request to get/create a state object/definition.
        /// </summary>
        /// <param name="client">The API client instance.</param>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="state">The state object/definition.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <param name="logger">A logger to use for capturing API telemetry.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the state object/definition.
        /// </returns>
        public static Task<HttpResponseMessage> GetOrCreateStateAsync<TState>(this IApiClient client, string stateId, TState state, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null, ILogger logger = null)
        {
            client.ThrowIfNull(nameof(client));
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));
            state.ThrowIfNull(nameof(state));

            return client.GetOrCreateStateAsync(stateId, JObject.FromObject(state), cancellationToken, retryPolicy, logger);
        }

        /// <summary>
        /// Polls the API targeted by the client to determine when server-side has signaled it is online and ready.
        /// </summary>
        /// <param name="client">The API client instance.</param>
        /// <param name="timeout">The period of time for which the client tries to get the state before timing out. </param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="logger">A logger to capture the responses from the server-side.</param>
        public static Task PollForServerOnlineAsync(this IApiClient client, TimeSpan timeout, CancellationToken cancellationToken, ILogger logger = null)
        {
            client.ThrowIfNull(nameof(client));

            DateTime pollingTimeout = DateTime.UtcNow.Add(timeout);
            bool apiOnline = false;

            List<object> responses = new List<object>();
            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("uri", client.BaseUri.ToString())
                .AddContext(nameof(timeout), pollingTimeout);

            return (logger ?? defaultLogger).LogMessageAsync("ClientServer.PollForServerOnline", telemetryContext, async () =>
            {
                telemetryContext.AddContext(nameof(responses), responses);

                do
                {
                    try
                    {
                        ConsoleLogger.Default.LogTraceMessage("...............................Polling for server online.");
                        using (HttpResponseMessage response = await client.GetEventingOnlineStatusAsync(cancellationToken).ConfigureAwait(false))
                        {
                            responses.Add(new
                            {
                                httpStatusCode = (int)response.StatusCode,
                                httpStatus = response.StatusCode.ToString()
                            });

                            apiOnline = response.IsSuccessStatusCode;
                        }
                    }
                    catch
                    {
                        // API service may not be online yet.
                    }
                    finally
                    {
                        if (!apiOnline)
                        {
                            if (DateTime.UtcNow >= pollingTimeout)
                            {
                                throw new WorkloadException(
                                    $"Polling for server online signal timed out (timeout={timeout}).",
                                    ErrorReason.ApiStatePollingTimeout);
                            }

                            await Task.Delay(VirtualClientApiClient.DefaultPollingWaitTime, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                }
                while (!apiOnline && !cancellationToken.IsCancellationRequested);
            });
        }

        /// <summary>
        /// Polls the API targeted by the client for a heartbeat.
        /// </summary>
        /// <param name="client">The API client instance.</param>
        /// <param name="timeout">The period of time for which the client tries to get the state before timing out. </param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="logger">A logger to capture the responses from the server-side.</param>
        public static Task PollForHeartbeatAsync(this IApiClient client, TimeSpan timeout, CancellationToken cancellationToken, ILogger logger = null)
        {
            client.ThrowIfNull(nameof(client));

            DateTime pollingTimeout = DateTime.UtcNow.Add(timeout);
            bool heartbeatConfirmed = false;

            List<object> responses = new List<object>();
            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("uri", client.BaseUri.ToString())
                .AddContext(nameof(timeout), pollingTimeout);

            return (logger ?? defaultLogger).LogMessageAsync("ClientServer.PollForHeartbeat", telemetryContext, async () =>
            {
                telemetryContext.AddContext(nameof(responses), responses);

                do
                {
                    try
                    {
                        ConsoleLogger.Default.LogTraceMessage("...............................Polling for heartbeat.");
                        using (HttpResponseMessage response = await client.GetHeartbeatAsync(cancellationToken).ConfigureAwait(false))
                        {
                            responses.Add(new
                            {
                                httpStatusCode = (int)response.StatusCode,
                                httpStatus = response.StatusCode.ToString()
                            });

                            heartbeatConfirmed = response.IsSuccessStatusCode;
                        }
                    }
                    catch
                    {
                        // State not available on server yet.
                    }
                    finally
                    {
                        if (!heartbeatConfirmed)
                        {
                            if (DateTime.UtcNow >= pollingTimeout)
                            {
                                throw new WorkloadException($"Polling for an API heartbeat timed out (timeout={timeout}).", ErrorReason.ApiStatePollingTimeout);
                            }

                            await Task.Delay(VirtualClientApiClient.DefaultPollingWaitTime, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                }
                while (!heartbeatConfirmed && !cancellationToken.IsCancellationRequested);
            });
        }

        /// <summary>
        /// Poll the server till there exists the expected state.
        /// </summary>
        /// <param name="client">The API client instance.</param>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="state">The state object/definition.</param>
        /// <param name="timeout">The period of time for which the client tries to get the state before timing out. </param>
        /// <param name="comparer">A comparer to use to determine equality between the client-side state and the server-side state.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="logger">A logger to capture the responses from the server-side.</param>
        public static Task PollForExpectedStateAsync(this IApiClient client, string stateId, JObject state, TimeSpan timeout, IEqualityComparer<JObject> comparer, CancellationToken cancellationToken, ILogger logger = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));
            state.ThrowIfNull(nameof(state));

            DateTime pollingTimeout = DateTime.UtcNow.Add(timeout);
            bool stateFound = false;

            List<object> responses = new List<object>();
            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("uri", client.BaseUri.ToString())
                .AddContext(nameof(stateId), stateId)
                .AddContext(nameof(timeout), pollingTimeout);

            return (logger ?? defaultLogger).LogMessageAsync("ClientServer.PollForExpectedState", telemetryContext, async () =>
            {
                telemetryContext.AddContext(nameof(responses), responses);

                do
                {
                    JObject currentState = null;

                    try
                    {
                        using (HttpResponseMessage response = await client.GetStateAsync(stateId, cancellationToken).ConfigureAwait(false))
                        {
                            responses.Add(new
                            {
                                httpStatusCode = (int)response.StatusCode,
                                httpStatus = response.StatusCode.ToString()
                            });

                            if (response.IsSuccessStatusCode)
                            {
                                string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                                Item<JObject> responseStateItem = responseContent.FromJson<Item<JObject>>();
                                currentState = responseStateItem.Definition;
                                telemetryContext.Properties["currentState"] = currentState;
                                stateFound = comparer.Equals(currentState, state);
                            }
                        }
                    }
                    catch
                    {
                        // State not available on server yet.
                    }
                    finally
                    {
                        if (!stateFound)
                        {
                            if (DateTime.UtcNow >= pollingTimeout)
                            {
                                throw new WorkloadException(
                                    $"Polling for expected state '{stateId}' timed out (timeout={timeout}).",
                                    ErrorReason.ApiStatePollingTimeout);
                            }

                            await Task.Delay(VirtualClientApiClient.DefaultPollingWaitTime, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                }
                while (!stateFound && !cancellationToken.IsCancellationRequested);
            });
        }

        /// <summary>
        /// Poll the server till there exists the expected state.
        /// </summary>
        /// <param name="client">The API client instance.</param>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="comparer">A comparer function to use to determine if the server-side state matches expected.</param>
        /// <param name="timeout">The period of time for which the client tries to get the state before timing out. </param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="logger">A logger to capture the responses from the server-side.</param>
        public static Task PollForExpectedStateAsync<TState>(this IApiClient client, string stateId, Func<TState, bool> comparer, TimeSpan timeout, CancellationToken cancellationToken, ILogger logger = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));
            comparer.ThrowIfNull(nameof(comparer));

            DateTime pollingTimeout = DateTime.UtcNow.Add(timeout);
            bool stateFound = false;

            List<object> responses = new List<object>();
            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("uri", client.BaseUri.ToString())
                .AddContext(nameof(stateId), stateId)
                .AddContext(nameof(timeout), pollingTimeout);

            return (logger ?? defaultLogger).LogMessageAsync("ClientServer.PollForExpectedState", telemetryContext, async () =>
            {
                telemetryContext.AddContext(nameof(responses), responses);

                do
                {
                    TState currentState = default(TState);

                    try
                    {
                        using (HttpResponseMessage response = await client.GetStateAsync(stateId, cancellationToken).ConfigureAwait(false))
                        {
                            responses.Add(new
                            {
                                httpStatusCode = (int)response.StatusCode,
                                httpStatus = response.StatusCode.ToString()
                            });

                            if (response.IsSuccessStatusCode)
                            {
                                string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                                Item<TState> responseStateItem = responseContent.FromJson<Item<TState>>();
                                currentState = responseStateItem.Definition;
                                telemetryContext.Properties["currentState"] = currentState;
                                stateFound = comparer.Invoke(currentState);
                            }
                        }
                    }
                    catch
                    {
                        // State not available on server yet.
                    }
                    finally
                    {
                        if (!stateFound)
                        {
                            if (DateTime.UtcNow >= pollingTimeout)
                            {
                                throw new WorkloadException(
                                    $"Polling for expected state '{stateId}' timed out (timeout={timeout}). " +
                                    $"Latest response state: {currentState?.ToString()}",
                                    ErrorReason.ApiStatePollingTimeout);
                            }

                            await Task.Delay(VirtualClientApiClient.DefaultPollingWaitTime, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                }
                while (!stateFound && !cancellationToken.IsCancellationRequested);
            });
        }

        /// <summary>
        /// Poll the server until the state is deleted/not found.
        /// </summary>
        /// <param name="client">The API client instance.</param>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="timeout">The period of time for which the client tries to get the state before timing out. </param>
        /// <param name="logger">A logger to use for capturing API telemetry.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the state object/definition at last poll call.
        /// </returns>
        public static Task PollUntilStateIsDeletedAsync(this IApiClient client, string stateId, CancellationToken cancellationToken, TimeSpan timeout, ILogger logger = null)
        {
            client.ThrowIfNull(nameof(client));
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            DateTime pollingTimeout = DateTime.UtcNow.Add(timeout);
            bool stateExists = true;

            List<object> responses = new List<object>();
            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("uri", client.BaseUri.ToString())
                .AddContext(nameof(stateId), stateId)
                .AddContext(nameof(timeout), pollingTimeout);

            return (logger ?? ConsoleLogger.Default).LogMessageAsync("ClientServer.PollUntilStateIsDeleted", telemetryContext, async () =>
            {
                telemetryContext.AddContext(nameof(responses), responses);

                do
                {
                    try
                    {
                        using (HttpResponseMessage response = await client.GetStateAsync(stateId, cancellationToken).ConfigureAwait(false))
                        {
                            responses.Add(new
                            {
                                httpStatusCode = (int)response.StatusCode,
                                httpStatus = response.StatusCode.ToString()
                            });

                            stateExists = response.StatusCode == HttpStatusCode.OK;
                        }
                    }
                    catch
                    {
                        // State not available on server yet.
                    }
                    finally
                    {
                        if (stateExists)
                        {
                            if (DateTime.UtcNow >= pollingTimeout)
                            {
                                throw new WorkloadException(
                                    $"Polling for deletion of state '{stateId}' timed out (timeout={timeout}).",
                                    ErrorReason.ApiStatePollingTimeout);
                            }

                            await Task.Delay(VirtualClientApiClient.DefaultPollingWaitTime, cancellationToken).ConfigureAwait(false);
                        }
                    }
                }
                while (stateExists && !cancellationToken.IsCancellationRequested);
            });
        }

        /// <summary>
        /// Sends instructions to the target Virtual Client instance.
        /// </summary>
        /// <param name="client">The API client instance.</param>
        /// <param name="instructions">Instructions to send to target Virtual Client Instance.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <param name="logger">A logger to use for capturing API telemetry.</param>
        /// <returns></returns>
        public static Task SendInstructionsAsync(this IApiClient client, Item<ClientServerRequest> instructions, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null, ILogger logger = null)
        {
            client.ThrowIfNull(nameof(client));
            instructions.ThrowIfNull(nameof(instructions));

            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("uri", client.BaseUri.ToString())
                .AddContext("instructions", instructions.Definition);

            return (logger ?? defaultLogger).LogMessageAsync("ClientServer.SendInstructions", telemetryContext, async () =>
            {
                HttpResponseMessage response = await client.SendInstructionsAsync(JObject.FromObject(instructions), cancellationToken, retryPolicy)
                    .ConfigureAwait(false);

                telemetryContext.AddResponseContext(response);
                response.ThrowOnError<ApiException>(ErrorReason.HttpNonSuccessResponse);
            });
        }

        /// <summary>
        /// Verify if given state exists.
        /// </summary>
        /// <param name="client">The API client instance.</param>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <param name="logger">A logger to use for capturing API telemetry.</param>
        /// <returns>
        /// A <see cref="bool"/> containing the boolean value representing whether state exits.
        /// </returns>
        public static Task<bool> VerifyStateExistsAsync(this IApiClient client, string stateId, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null, ILogger logger = null)
        {
            client.ThrowIfNull(nameof(client));
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("uri", client.BaseUri.ToString())
                .AddContext(nameof(stateId), stateId);

            return (logger ?? defaultLogger).LogMessageAsync("ClientServer.VerifyStateExists", telemetryContext, async () =>
            {
                HttpResponseMessage response = await client.GetStateAsync(stateId, cancellationToken, retryPolicy)
                    .ConfigureAwait(false);

                telemetryContext.AddResponseContext(response);
                return response.IsSuccessStatusCode;
            });
        }
    }
}
