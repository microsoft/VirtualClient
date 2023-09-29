// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for generic rest client
    /// </summary>
    public class RestClient : IRestClient
    {
        private bool disposed = false;

        internal RestClient(HttpClient httpClient = null)
        {
            this.Client = httpClient ?? new HttpClient();
        }

        /// <summary>
        /// Gets or sets the authentication provider that adds auth header to the rest client.
        /// </summary>
        public IHttpAuthentication AuthenticationProvider { get; set; }

        /// <summary>
        /// The underlying HTTP client.
        /// </summary>
        public HttpClient Client { get; }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            await this.ApplyAuthenticationHeaderAsync(cancellationToken);
            HttpResponseMessage responseMessage = await this.Client.DeleteAsync(requestUri, cancellationToken);
            this.ResetAuthenticationOnUnauthorized(responseMessage);

            return responseMessage;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> DeleteAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            await this.ApplyAuthenticationHeaderAsync(cancellationToken);
            using (HttpRequestMessage request = new HttpRequestMessage
            {
                Content = content,
                Method = HttpMethod.Delete,
                RequestUri = requestUri
            })
            {
                HttpResponseMessage responseMessage = await this.Client.SendAsync(request, cancellationToken);
                this.ResetAuthenticationOnUnauthorized(responseMessage);

                return responseMessage;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
        {
            await this.ApplyAuthenticationHeaderAsync(cancellationToken);
            HttpResponseMessage responseMessage = await this.Client.GetAsync(requestUri, completionOption, cancellationToken);
            this.ResetAuthenticationOnUnauthorized(responseMessage);

            return responseMessage;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
        {
            await this.ApplyAuthenticationHeaderAsync(cancellationToken);
            using (HttpRequestMessage request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = requestUri,
                Content = content
            })
            {
                HttpResponseMessage responseMessage = await this.Client.SendAsync(request, completionOption, cancellationToken);
                this.ResetAuthenticationOnUnauthorized(responseMessage);

                return responseMessage;
            }
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> HeadAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            await this.ApplyAuthenticationHeaderAsync(cancellationToken);

            // HttpClient class DOES NOT have HEAD convenience method.
            // https://github.com/mono/mono/blob/master/mcs/class/System.Net.Http/System.Net.Http/HttpClient.cs#L144
            // https://stackoverflow.com/questions/16416699/http-head-request-with-httpclient-in-net-4-5-and-c-sharp

            HttpResponseMessage responseMessage = await this.Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, requestUri), cancellationToken);
            this.ResetAuthenticationOnUnauthorized(responseMessage);

            return responseMessage;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> PatchAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            await this.ApplyAuthenticationHeaderAsync(cancellationToken);
            HttpResponseMessage responseMessage = await this.Client.PatchAsync(requestUri, content, cancellationToken);
            this.ResetAuthenticationOnUnauthorized(responseMessage);

            return responseMessage;
        }

        /// <inheritdoc/>
        /// <summary>
        /// Post REST call without any content.
        /// </summary>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="cancellationToken">Calcellation token.</param>
        /// <returns>Http response message.</returns>
        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            await this.ApplyAuthenticationHeaderAsync(cancellationToken);
            HttpResponseMessage responseMessage = await this.Client.PostAsync(requestUri, null, cancellationToken);
            this.ResetAuthenticationOnUnauthorized(responseMessage);

            return responseMessage;
        }

        /// <summary>
        /// Post REST call.
        /// </summary>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="content">Http content to send.</param>
        /// <param name="cancellationToken">Calcellation token.</param>
        /// <returns>Http response message.</returns>
        public async Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            await this.ApplyAuthenticationHeaderAsync(cancellationToken);
            HttpResponseMessage responseMessage = await this.Client.PostAsync(requestUri, content, cancellationToken);
            this.ResetAuthenticationOnUnauthorized(responseMessage);

            return responseMessage;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            await this.ApplyAuthenticationHeaderAsync(cancellationToken);
            HttpResponseMessage responseMessage = await this.Client.PutAsync(requestUri, content, cancellationToken);
            this.ResetAuthenticationOnUnauthorized(responseMessage);

            return responseMessage;
        }

        /// <inheritdoc/>
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await this.ApplyAuthenticationHeaderAsync(cancellationToken);
            HttpResponseMessage responseMessage = await this.Client.SendAsync(request, cancellationToken);
            this.ResetAuthenticationOnUnauthorized(responseMessage);

            return responseMessage;
        }

        internal void AddAcceptedMediaTypeHeader(MediaTypeWithQualityHeaderValue mediaTypeHeader)
        {
            this.Client.DefaultRequestHeaders.Accept.Add(mediaTypeHeader);
        }

        internal void AddHeader(string name, IList<string> values)
        {
            this.Client.DefaultRequestHeaders.Add(name, values);
        }

        internal void SetAuthorizationHeader(AuthenticationHeaderValue authHeader)
        {
            this.Client.DefaultRequestHeaders.Authorization = authHeader;
        }

        /// <inheritdoc/>
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    this.Client.Dispose();
                }

                this.disposed = true;
            }
        }

        private void ResetAuthenticationOnUnauthorized(HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                this.AuthenticationProvider?.Reset();
                throw new UnauthorizedAccessException($"HttpResponse indicates unauthorized for given access token. ReasonPhrase: '{response.ReasonPhrase}'.");
            }
        }

        private async Task ApplyAuthenticationHeaderAsync(CancellationToken cancellationToken)
        {
            if (this.AuthenticationProvider != null)
            {
                await this.AuthenticationProvider.AuthenticateAsync(this.Client, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}