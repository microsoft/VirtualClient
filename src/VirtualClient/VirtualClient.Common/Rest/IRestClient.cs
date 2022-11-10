// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Rest
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for generic rest client
    /// </summary>
    public interface IRestClient : IDisposable
    {
        /// <summary>
        /// Authentication provider to modify header on httpclient.
        /// </summary>
        IHttpAuthentication AuthenticationProvider { get; set; }

        /// <summary>
        /// Delete REST call.
        /// </summary>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>Http response message.</returns>
        Task<HttpResponseMessage> DeleteAsync(Uri requestUri, CancellationToken cancellationToken);

        /// <summary>
        /// Delete REST call.
        /// </summary>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="content">Http content to send.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>Http response message.</returns>
        Task<HttpResponseMessage> DeleteAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken);

        /// <summary>
        /// Get REST call.
        /// </summary>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <param name="completionOption">Http completion option</param>
        /// <returns>Http response message.</returns>
        Task<HttpResponseMessage> GetAsync(Uri requestUri, CancellationToken cancellationToken, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead);

        /// <summary>
        /// Get REST call.
        /// </summary>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="content">Http content to send.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <param name="completionOption">Http completion option</param>
        /// <returns>Http response message.</returns>
        Task<HttpResponseMessage> GetAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead);

        /// <summary>
        /// HEAD REST call.
        /// </summary>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>Http response message.</returns>
        Task<HttpResponseMessage> HeadAsync(Uri requestUri, CancellationToken cancellationToken);

        /// <summary>
        /// PATCH REST call.
        /// </summary>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="content">Http content to send.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>Http response message.</returns>
        Task<HttpResponseMessage> PatchAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken);

        /// <summary>
        /// Post REST call without content.
        /// </summary>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>Http response message.</returns>
        Task<HttpResponseMessage> PostAsync(Uri requestUri, CancellationToken cancellationToken);

        /// <summary>
        /// POST REST call.
        /// </summary>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="content">Http content to send.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>Http response message.</returns>
        Task<HttpResponseMessage> PostAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken);

        /// <summary>
        /// Put REST call.
        /// </summary>
        /// <param name="requestUri">The request uri.</param>
        /// <param name="content">Http content to send.</param>
        /// <param name="cancellationToken">Request cancellation token.</param>
        /// <returns>Http response message.</returns>
        Task<HttpResponseMessage> PutAsync(Uri requestUri, HttpContent content, CancellationToken cancellationToken);
    }
}