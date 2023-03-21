// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Rest
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection.Metadata;
    using System.Threading;
    using System.Threading.Tasks;
    using Polly;
    using Polly.Extensions.Http;

    /// <summary>
    /// Interface for generic rest client
    /// </summary>
    public class RestClient : IRestClient
    {
        /// <summary>
        /// Defines the default retry policy rest client to handle transient http error.
        /// </summary>
        private static IAsyncPolicy<HttpResponseMessage> defaultRetryPolicy = HttpPolicyExtensions.HandleTransientHttpError()
            .WaitAndRetryAsync(5, (retries) => TimeSpan.FromMilliseconds(retries * 100));

        /// <summary>
        /// When unauthorized the authored code below does two things:
        /// <list type="number">
        /// <item>Resets the authentication header value.</item>
        /// <item>Throws a <see cref="UnauthorizedAccessException"/>.</item>
        /// </list>
        /// This means the client should retry once, with the intent the unauth from the Http response was due to
        /// an expired bearer token, and refreshing this token solves the issue.
        /// </summary>
        private static IAsyncPolicy unauthorizedRetryPolicy = Policy.Handle<UnauthorizedAccessException>()
            .RetryAsync();

        private bool disposed = false;

        /// <summary>
        /// Gets the retry policy to apply when experiencing transient issues.
        /// </summary>
        private IAsyncPolicy<HttpResponseMessage> retryPolicy;

        internal RestClient(HttpClient httpClient = null, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            this.Client = httpClient ?? new HttpClient();
            this.retryPolicy = retryPolicy ?? RestClient.defaultRetryPolicy
                .WrapAsync(RestClient.unauthorizedRetryPolicy);
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
        public Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            return this.retryPolicy.ExecuteAsync(async () =>
            {
                await this.ApplyAuthenticationHeaderAsync(cancellationToken).ConfigureAwait(false);
                HttpResponseMessage responseMessage = await this.Client.DeleteAsync(requestUri, cancellationToken).ConfigureAwait(false);
                this.ResetAuthenticationOnUnauthorized(responseMessage);
                return responseMessage;
            });
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> DeleteAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            return this.retryPolicy.ExecuteAsync(async () =>
            {
                await this.ApplyAuthenticationHeaderAsync(cancellationToken).ConfigureAwait(false);
                HttpRequestMessage request = new HttpRequestMessage
                {
                    Content = content,
                    Method = HttpMethod.Delete,
                    RequestUri = requestUri
                };
                HttpResponseMessage responseMessage = await this.Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                this.ResetAuthenticationOnUnauthorized(responseMessage);
                return responseMessage;
            });
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
        {
            return this.retryPolicy.ExecuteAsync(async () =>
            {
                await this.ApplyAuthenticationHeaderAsync(cancellationToken).ConfigureAwait(false);
                HttpResponseMessage responseMessage = await this.Client.GetAsync(requestUri, completionOption, cancellationToken).ConfigureAwait(false);
                this.ResetAuthenticationOnUnauthorized(responseMessage);
                return responseMessage;
            });
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
        {
            return this.retryPolicy.ExecuteAsync(async () =>
            {
                await this.ApplyAuthenticationHeaderAsync(cancellationToken).ConfigureAwait(false);
                HttpRequestMessage request = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = requestUri,
                    Content = content
                };

                HttpResponseMessage responseMessage = await this.Client.SendAsync(request, completionOption, cancellationToken)
                    .ConfigureAwait(false);

                this.ResetAuthenticationOnUnauthorized(responseMessage);
                return responseMessage;
            });
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> HeadAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            return this.retryPolicy.ExecuteAsync(async () =>
            {
                await this.ApplyAuthenticationHeaderAsync(cancellationToken).ConfigureAwait(false);
                // Http Client doesn't have HEAD wrapper.
                // https://github.com/mono/mono/blob/master/mcs/class/System.Net.Http/System.Net.Http/HttpClient.cs#L144
                // https://stackoverflow.com/questions/16416699/http-head-request-with-httpclient-in-net-4-5-and-c-sharp
                HttpResponseMessage responseMessage = await this.Client.SendAsync(new HttpRequestMessage(HttpMethod.Head, requestUri), cancellationToken).ConfigureAwait(false);
                this.ResetAuthenticationOnUnauthorized(responseMessage);
                return responseMessage;
            });
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> PatchAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            return this.retryPolicy.ExecuteAsync(async () =>
            {
                await this.ApplyAuthenticationHeaderAsync(cancellationToken).ConfigureAwait(false);
                HttpResponseMessage responseMessage = await this.Client.PatchAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
                this.ResetAuthenticationOnUnauthorized(responseMessage);
                return responseMessage;
            });
        }

        /// <inheritdoc/>
        /// <summary>
        /// Post REST call without any content.
        /// </summary>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="cancellationToken">Calcellation token.</param>
        /// <returns>Http response message.</returns>
        public Task<HttpResponseMessage> PostAsync(Uri requestUri, CancellationToken cancellationToken)
        {
            return this.retryPolicy.ExecuteAsync(async () =>
            {
                await this.ApplyAuthenticationHeaderAsync(cancellationToken).ConfigureAwait(false);
                HttpResponseMessage responseMessage = await this.Client.PostAsync(requestUri, null, cancellationToken).ConfigureAwait(false);
                this.ResetAuthenticationOnUnauthorized(responseMessage);
                return responseMessage;
            });
        }

        /// <summary>
        /// Post REST call.
        /// </summary>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="content">Http content to send.</param>
        /// <param name="cancellationToken">Calcellation token.</param>
        /// <returns>Http response message.</returns>
        public Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            return this.retryPolicy.ExecuteAsync(async () =>
            {
                await this.ApplyAuthenticationHeaderAsync(cancellationToken).ConfigureAwait(false);
                HttpResponseMessage responseMessage = await this.Client.PostAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
                this.ResetAuthenticationOnUnauthorized(responseMessage);
                return responseMessage;
            });
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken)
        {
            return this.retryPolicy.ExecuteAsync(async () =>
            {
                await this.ApplyAuthenticationHeaderAsync(cancellationToken).ConfigureAwait(false);
                HttpResponseMessage responseMessage = await this.Client.PutAsync(requestUri, content, cancellationToken).ConfigureAwait(false);
                this.ResetAuthenticationOnUnauthorized(responseMessage);
                return responseMessage;
            });
        }

        /// <inheritdoc/>
        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return this.retryPolicy.ExecuteAsync(async () =>
            {
                await this.ApplyAuthenticationHeaderAsync(cancellationToken).ConfigureAwait(false);
                HttpResponseMessage responseMessage = await this.Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                this.ResetAuthenticationOnUnauthorized(responseMessage);
                return responseMessage;
            });
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