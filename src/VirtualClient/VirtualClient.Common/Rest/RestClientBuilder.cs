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
        private TimeSpan? httpTimeout;
        private bool disposed = false;

        private List<MediaTypeWithQualityHeaderValue> acceptedMediaTypes;
        private AuthenticationHeaderValue authenticationHeader;

#pragma warning disable CA2213 // We will reuse these single objects for the lifetime of virtual client execution
        private HttpClientHandler handler;
#pragma warning restore CA2213 // Disposable fields should be disposed

        /// <summary>
        /// Constructor for the rest client builder
        /// </summary>
        /// <param name="timeout">The HTTP timeout to apply.</param>
        public RestClientBuilder(TimeSpan? timeout = null)
        {
            this.httpTimeout = timeout;
            this.handler = new HttpClientHandler();
            this.acceptedMediaTypes = new List<MediaTypeWithQualityHeaderValue>();
        }

        /// <inheritdoc/>
        public IRestClientBuilder AddAuthorizationHeader(string authToken, string headerName = "Bearer")
        {
            this.authenticationHeader = new AuthenticationHeaderValue(authToken, headerName);
            return this;
        }

        /// <inheritdoc/>
        public IRestClientBuilder AddAcceptedMediaType(MediaType mediaType)
        {
            mediaType.ThrowIfNull(nameof(mediaType));
            this.acceptedMediaTypes.Add(new MediaTypeWithQualityHeaderValue(mediaType.FieldName));
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
            RestClient restClient = new RestClient(client);

            if (this.authenticationHeader != null)
            {
                restClient.SetAuthorizationHeader(this.authenticationHeader);
            }

            if (this.acceptedMediaTypes.Count > 0)
            {
                foreach (var mediaType in this.acceptedMediaTypes)
                {
                    restClient.AddAcceptedMediaTypeHeader(mediaType);
                }
            }

            if (this.httpTimeout != null)
            {
                restClient.Client.Timeout = this.httpTimeout.Value;
            }

            return restClient;
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