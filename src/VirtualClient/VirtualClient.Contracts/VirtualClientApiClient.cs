// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Rest;
    using VirtualClient.Contracts;

    /// <summary>
    /// Virtual Client REST API client.
    /// </summary>
    public class VirtualClientApiClient : IApiClient
    {
        /// <summary>
        /// The default state document JSON serialization settings.
        /// </summary>
        public static readonly JsonSerializerSettings StateSerializationSettings = new JsonSerializerSettings
        {
            Converters = new List<JsonConverter>
            {
                new ParameterDictionaryJsonConverter()
            },

            // Format: 2012-03-21T05:40:12.340Z
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,

            // We tried using PreserveReferenceHandling.All and Object, but ran into issues
            // when deserializing string arrays and read only dictionaries
            ReferenceLoopHandling = ReferenceLoopHandling.Error,

            // This is the default setting, but to avoid remote code execution bugs do NOT change
            // this to any other setting.
            TypeNameHandling = TypeNameHandling.None
        };

        private const string EventsApiRoute = "/api/events";
        private const string HeartbeatApiRoute = "/api/heartbeat";
        private const string StateApiRoute = "/api/state";

        private static IAsyncPolicy<HttpResponseMessage> defaultHttpDeleteRetryPolicy = VirtualClientApiClient.GetDefaultHttpDeleteRetryPolicy(
            (retries) => TimeSpan.FromMilliseconds(retries * 500));

        private static IAsyncPolicy<HttpResponseMessage> defaultHttpGetRetryPolicy = VirtualClientApiClient.GetDefaultHttpGetRetryPolicy(
            (retries) => TimeSpan.FromMilliseconds(retries * 500));

        private static IAsyncPolicy<HttpResponseMessage> defaultHttpPostRetryPolicy = VirtualClientApiClient.GetDefaultHttpPostRetryPolicy(
            (retries) => TimeSpan.FromMilliseconds(retries * 500));

        private static IAsyncPolicy<HttpResponseMessage> defaultHttpPutRetryPolicy = VirtualClientApiClient.GetDefaultHttpPutRetryPolicy(
            (retries) => TimeSpan.FromMilliseconds(retries * 500));

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientApiClient"/> class.
        /// </summary>
        /// <param name="restClient">
        /// The REST client that handles HTTP communications with the API
        /// service.
        /// </param>
        /// <param name="baseUri">
        /// The base URI to the server hosting the API (e.g. https://somevcserver:4500).
        /// </param>
        public VirtualClientApiClient(IRestClient restClient, Uri baseUri)
        {
            restClient.ThrowIfNull(nameof(restClient));
            baseUri.ThrowIfNull(nameof(baseUri));

            this.RestClient = restClient;
            this.BaseUri = baseUri;
        }

        /// <summary>
        /// The default port used by the API service for HTTP/TCP communications.
        /// </summary>
        public static int DefaultApiPort { get; set; } = 4500;

        /// <summary>
        /// Gets the base URI to the server hosting the API.
        /// </summary>
        public Uri BaseUri { get; }

        /// <summary>
        /// Gets or sets the REST client that handles HTTP communications
        /// with the API service.
        /// </summary>
        protected IRestClient RestClient { get; }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> CreateStateAsync(string stateId, JObject state, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));
            state.ThrowIfNull(nameof(state));

            using (StringContent requestBody = VirtualClientApiClient.CreateJsonContent(state.ToString()))
            {
                // Format: /api/state/{stateId}
                string route = $"{VirtualClientApiClient.StateApiRoute}/{stateId}";
                Uri requestUri = new Uri(this.BaseUri, route);

                return await (retryPolicy ?? VirtualClientApiClient.defaultHttpPostRetryPolicy).ExecuteAsync(async () =>
                {
                    return await this.RestClient.PostAsync(requestUri, requestBody, cancellationToken)
                        .ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> CreateStateAsync<TState>(string stateId, TState state, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
            where TState : State
        {
            return this.CreateStateAsync(stateId, JObject.FromObject(state), cancellationToken, retryPolicy);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> DeleteStateAsync(string stateId, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            // Format: /api/state/{stateId}
            string route = $"{VirtualClientApiClient.StateApiRoute}/{stateId}";
            Uri requestUri = new Uri(this.BaseUri, route);

            return (retryPolicy ?? VirtualClientApiClient.defaultHttpDeleteRetryPolicy).ExecuteAsync(async () =>
            {
                return await this.RestClient.DeleteAsync(requestUri, cancellationToken)
                    .ConfigureAwait(false);
            });
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetEventingOnlineStatusAsync(CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            // Format: /api/events
            string route = VirtualClientApiClient.EventsApiRoute;
            Uri requestUri = new Uri(this.BaseUri, route);

            return (retryPolicy ?? VirtualClientApiClient.defaultHttpGetRetryPolicy).ExecuteAsync(async () =>
            {
                return await this.RestClient.HeadAsync(requestUri, cancellationToken)
                    .ConfigureAwait(false);
            });
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetHeartbeatAsync(CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            // Format: /api/heartbeat
            string route = VirtualClientApiClient.HeartbeatApiRoute;
            Uri requestUri = new Uri(this.BaseUri, route);

            return (retryPolicy ?? VirtualClientApiClient.defaultHttpGetRetryPolicy).ExecuteAsync(async () =>
            {
                return await this.RestClient.GetAsync(requestUri, cancellationToken)
                    .ConfigureAwait(false);
            });
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetStateAsync(string stateId, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            // Format: /api/state/{stateId}
            string route = $"{VirtualClientApiClient.StateApiRoute}/{stateId}";
            Uri requestUri = new Uri(this.BaseUri, route);

            return (retryPolicy ?? VirtualClientApiClient.defaultHttpGetRetryPolicy).ExecuteAsync(async () =>
            {
                return await this.RestClient.GetAsync(requestUri, cancellationToken)
                    .ConfigureAwait(false);
            });
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> SendInstructionsAsync(JObject instructions, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            instructions.ThrowIfNull(nameof(instructions));

            using (StringContent requestBody = VirtualClientApiClient.CreateJsonContent(instructions.ToString()))
            {
                // Format: /api/events
                // Body  : instructions
                string route = $"{VirtualClientApiClient.EventsApiRoute}";
                Uri requestUri = new Uri(this.BaseUri, route);

                return await (retryPolicy ?? VirtualClientApiClient.defaultHttpPostRetryPolicy).ExecuteAsync(async () =>
                {
                    return await this.RestClient.PostAsync(requestUri, requestBody, cancellationToken)
                        .ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> SendInstructionsAsync<TInstructions>(TInstructions instructions, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
            where TInstructions : Instructions
        {
            instructions.ThrowIfNull(nameof(instructions));

            using (StringContent requestBody = VirtualClientApiClient.CreateJsonContent(instructions.ToString()))
            {
                // Format: /api/events?api-version=2.0
                // Body  : instructions
                string route = $"{VirtualClientApiClient.EventsApiRoute}?api-version=2.0";
                Uri requestUri = new Uri(this.BaseUri, route);

                return await (retryPolicy ?? VirtualClientApiClient.defaultHttpPostRetryPolicy).ExecuteAsync(async () =>
                {
                    return await this.RestClient.PostAsync(requestUri, requestBody, cancellationToken)
                        .ConfigureAwait(false);
                }).ConfigureAwait(false);
            } 
        }

        /// <inheritdoc />
        public async Task<HttpResponseMessage> UpdateStateAsync(string stateId, JObject state, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));
            state.ThrowIfNull(nameof(state));

            using (StringContent requestBody = VirtualClientApiClient.CreateJsonContent(state.ToString()))
            {
                // Format: /api/state/{stateId}
                string route = $"{VirtualClientApiClient.StateApiRoute}/{stateId}";
                Uri requestUri = new Uri(this.BaseUri, route);

                return await (retryPolicy ?? VirtualClientApiClient.defaultHttpPutRetryPolicy).ExecuteAsync(async () =>
                {
                    return await this.RestClient.PutAsync(requestUri, requestBody, cancellationToken)
                        .ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> UpdateStateAsync<TState>(string stateId, Item<TState> state, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
            where TState : State
        {
            return this.UpdateStateAsync(stateId, JObject.FromObject(state), cancellationToken, retryPolicy);
        }

        /// <summary>
        /// Creates the default retry policy for HTTP DELETE calls made to the API service.
        /// </summary>
        /// <param name="retryWaitInterval">
        /// Defines the individual retry wait interval given the number of retries. The integer parameter defines the retries that have occurred at that moment in time.
        /// </param>
        internal static IAsyncPolicy<HttpResponseMessage> GetDefaultHttpDeleteRetryPolicy(Func<int, TimeSpan> retryWaitInterval)
        {
            // This is not a full list of HTTP status codes that could be considered non-transient but is a
            // list of codes that would be expected from the Virtual Client API during normal operations.
            List<HttpStatusCode> nonTransientErrorCodes = new List<HttpStatusCode>
            {
                HttpStatusCode.BadRequest,
                HttpStatusCode.Forbidden,
                HttpStatusCode.NetworkAuthenticationRequired,
                HttpStatusCode.HttpVersionNotSupported,
                HttpStatusCode.Unauthorized
            };

            return Policy.HandleResult<HttpResponseMessage>(response =>
            {
                // Retry if the response status is not a success status code (i.e. 200s) but only if the status
                // code is also not in the list of non-transient status codes.
                bool shouldRetry = !response.IsSuccessStatusCode && !nonTransientErrorCodes.Contains(response.StatusCode);
                return shouldRetry;

            }).WaitAndRetryAsync(10, retryWaitInterval);
        }

        /// <summary>
        /// Creates the default retry policy for HTTP GET calls made to the API service.
        /// </summary>
        /// <param name="retryWaitInterval">
        /// Defines the individual retry wait interval given the number of retries. The integer parameter defines the retries that have occurred at that moment in time.
        /// </param>
        internal static IAsyncPolicy<HttpResponseMessage> GetDefaultHttpGetRetryPolicy(Func<int, TimeSpan> retryWaitInterval)
        {
            // This is not a full list of HTTP status codes that could be considered non-transient but is a
            // list of codes that would be expected from the Virtual Client API during normal operations.
            List<HttpStatusCode> nonTransientErrorCodes = new List<HttpStatusCode>
            {
                HttpStatusCode.BadRequest,
                HttpStatusCode.NotFound,
                HttpStatusCode.Locked,
                HttpStatusCode.Forbidden,
                HttpStatusCode.NetworkAuthenticationRequired,
                HttpStatusCode.HttpVersionNotSupported,
                HttpStatusCode.Unauthorized
            };

            return Policy.HandleResult<HttpResponseMessage>(response =>
            {
                // Retry if the response status is not a success status code (i.e. 200s) but only if the status
                // code is also not in the list of non-transient status codes.
                bool shouldRetry = !response.IsSuccessStatusCode && !nonTransientErrorCodes.Contains(response.StatusCode);
                return shouldRetry;

            }).WaitAndRetryAsync(10, retryWaitInterval);
        }

        /// <summary>
        /// Creates the default retry policy for HTTP POST calls made to the API service.
        /// </summary>
        /// <param name="retryWaitInterval">
        /// Defines the individual retry wait interval given the number of retries. The integer parameter defines the retries that have occurred at that moment in time.
        /// </param>
        internal static IAsyncPolicy<HttpResponseMessage> GetDefaultHttpPostRetryPolicy(Func<int, TimeSpan> retryWaitInterval)
        {
            // This is not a full list of HTTP status codes that could be considered non-transient but is a
            // list of codes that would be expected from the Virtual Client API during normal operations.
            List<HttpStatusCode> nonTransientErrorCodes = new List<HttpStatusCode>
            {
                HttpStatusCode.BadRequest,
                HttpStatusCode.Conflict,
                HttpStatusCode.Forbidden,
                HttpStatusCode.NetworkAuthenticationRequired,
                HttpStatusCode.HttpVersionNotSupported,
                HttpStatusCode.Unauthorized
            };

            return Policy.HandleResult<HttpResponseMessage>(response =>
            {
                // Retry if the response status is not a success status code (i.e. 200s) but only if the status
                // code is also not in the list of non-transient status codes.
                bool shouldRetry = !response.IsSuccessStatusCode && !nonTransientErrorCodes.Contains(response.StatusCode);
                return shouldRetry;

            }).WaitAndRetryAsync(10, retryWaitInterval);
        }

        /// <summary>
        /// Creates the default retry policy for HTTP PUT calls made to the API service.
        /// </summary>
        /// <param name="retryWaitInterval">
        /// Defines the individual retry wait interval given the number of retries. The integer parameter defines the retries that have occurred at that moment in time.
        /// </param>
        internal static IAsyncPolicy<HttpResponseMessage> GetDefaultHttpPutRetryPolicy(Func<int, TimeSpan> retryWaitInterval)
        {
            // This is not a full list of HTTP status codes that could be considered non-transient but is a
            // list of codes that would be expected from the Virtual Client API during normal operations.
            List<HttpStatusCode> nonTransientErrorCodes = new List<HttpStatusCode>
            {
                HttpStatusCode.BadRequest,
                HttpStatusCode.Conflict,
                HttpStatusCode.Forbidden,
                HttpStatusCode.NetworkAuthenticationRequired,
                HttpStatusCode.HttpVersionNotSupported,
                HttpStatusCode.Unauthorized
            };

            return Policy.HandleResult<HttpResponseMessage>(response =>
            {
                // Retry if the response status is not a success status code (i.e. 200s) but only if the status
                // code is also not in the list of non-transient status codes.
                bool shouldRetry = !response.IsSuccessStatusCode && !nonTransientErrorCodes.Contains(response.StatusCode);
                return shouldRetry;

            }).WaitAndRetryAsync(10, retryWaitInterval);
        }

        private static StringContent CreateJsonContent(string content)
        {
            return new StringContent(content, Encoding.UTF8, "application/json");
        }
    }
}
