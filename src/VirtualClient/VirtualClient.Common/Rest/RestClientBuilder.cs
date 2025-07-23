// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Security.Cryptography.X509Certificates;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Builder for generic rest client
    /// </summary>
    public class RestClientBuilder : IRestClientBuilder
    {
        // disable this for now.
        private RestClient restClient;
        private TimeSpan? httpTimeout;
        private bool disposed = false;
        private HttpClientHandler handler;

        /// <summary>
        /// Constructor for the rest client builder
        /// </summary>
        /// <param name="timeout">The HTTP timeout to apply.</param>
        public RestClientBuilder(TimeSpan? timeout = null)
        {
            this.restClient = new RestClient();
            this.httpTimeout = timeout;
            this.handler = new HttpClientHandler();
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
            this.handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) =>
            {
                return true;
            };

            return this;
        }

        /// <inheritdoc/>
        public IRestClientBuilder AddCertificate(X509Certificate2 certificate)
        {
            this.handler.ClientCertificates.Add(certificate);
            return this;
        }

        /// <summary>
        /// Build the rest client.
        /// </summary>
        /// <returns>The built rest client.</returns>
        public IRestClient Build()
        {
            HttpClient client = new HttpClient(this.handler);
            this.restClient = new RestClient(client);
            if (this.httpTimeout != null)
            {
                this.restClient.Client.Timeout = this.httpTimeout.Value;
            }

            return this.restClient;
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
                }

                this.disposed = true;
            }
        }
    }
}