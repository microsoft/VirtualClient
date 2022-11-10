// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Rest;

    /// <summary>
    /// Virtual Client REST API client.
    /// </summary>
    public class ExampleApiClient
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

        private const string SomethingApiRoute = "/api/something";

        private static IAsyncPolicy<HttpResponseMessage> defaultHttpGetRetryPolicy = ExampleApiClient.GetDefaultHttpGetRetryPolicy(
            (retries) => TimeSpan.FromMilliseconds(retries * 500));

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleApiClient"/> class.
        /// </summary>
        /// <param name="restClient">
        /// The REST client that handles HTTP communications with the API
        /// service.
        /// </param>
        /// <param name="baseUri">
        /// The base URI to the server hosting the API.
        /// </param>
        public ExampleApiClient(IRestClient restClient, Uri baseUri)
        {
            restClient.ThrowIfNull(nameof(restClient));
            baseUri.ThrowIfNull(nameof(baseUri));

            this.RestClient = restClient;
            this.BaseUri = baseUri;
        }

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
        public async Task<HttpResponseMessage> GetSomethingAsync(CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            // Format: /api/state/{stateId}
            string route = $"{ExampleApiClient.SomethingApiRoute}";
            Uri requestUri = new Uri(this.BaseUri, route);

            return await (retryPolicy ?? ExampleApiClient.defaultHttpGetRetryPolicy).ExecuteAsync(async () =>
            {
                return await this.RestClient.GetAsync(requestUri, cancellationToken)
                    .ConfigureAwait(false);
            }).ConfigureAwait(false);
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
    }
}
