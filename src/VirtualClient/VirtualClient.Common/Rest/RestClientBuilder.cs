// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Builder for generic rest client
    /// </summary>
    public class RestClientBuilder : IRestClientBuilder
    {
        private RestClient restClient;
        private TimeSpan? httpTimeout;
        private bool disposed = false;

        /// <summary>
        /// Constructor for the rest client builder
        /// </summary>
        /// <param name="timeout">The HTTP timeout to apply.</param>
        public RestClientBuilder(TimeSpan? timeout = null)
        {
            this.restClient = new RestClient();
            this.httpTimeout = timeout;
        }

        /// <inheritdoc/>
        public IRestClientBuilder AddAuthorizationHeader(string authToken, string headerName = "Bearer")
        {
            this.restClient.SetAuthorizationHeader(new AuthenticationHeaderValue(headerName, authToken));
            return this;
        }

        /// <inheritdoc/>
        public IRestClientBuilder AddAcceptedMediaType(MediaType mediaType)
        {
            mediaType.ThrowIfNull(nameof(mediaType));
            this.restClient.AddAcceptedMediaTypeHeader(new MediaTypeWithQualityHeaderValue(mediaType.FieldName));
            return this;
        }

        /// <inheritdoc/>
        public IRestClientBuilder AlwaysTrustServerCertificate()
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true;
            };

            HttpClient client = new HttpClient(handler);
            this.restClient = new RestClient(client);
            return this;
        }

        /// <summary>
        /// Build the rest client.
        /// </summary>
        /// <returns>The built rest client.</returns>
        public IRestClient Build()
        {
            RestClient output = this.restClient;
            this.restClient = new RestClient();

            if (this.httpTimeout != null)
            {
                output.Client.Timeout = this.httpTimeout.Value;
            }

            return output;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.restClient.Dispose();
                }

                this.disposed = true;
            }
        }
    }
}