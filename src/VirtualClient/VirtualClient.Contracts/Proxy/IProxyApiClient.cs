// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Polly;

    /// <summary>
    /// API client that allows the Virtual Client to upload/download files and packages and to emit telemetry
    /// to/from a proxy API endpoint.
    /// </summary>
    public interface IProxyApiClient
    {
        /// <summary>
        /// Gets the base URI to the server hosting the API (e.g. http://localhost:4500).
        /// </summary>
        Uri BaseUri { get; }

        /// <summary>
        /// Downloads a blob from the store into the stream provided (e.g. in-memory stream, file stream).
        /// </summary>
        /// <param name="descriptor">Provides the storage location details for the blob to download.</param>
        /// <param name="downloadStream">The stream into which the blob will be downloaded.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A policy to use for handling retries when transient errors/failures happen.</param>
        /// <returns>Full details for the blob as it exists in the store (e.g. name, content encoding, content type).</returns>
        Task<HttpResponseMessage> DownloadBlobAsync(ProxyBlobDescriptor descriptor, Stream downloadStream, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null);

        /// <summary>
        /// Uploads a blob from the stream provided into the store.
        /// </summary>
        /// <param name="descriptor">Provides the storage location and content details for the blob being uploaded.</param>
        /// <param name="uploadStream">The stream that contains the blob binary content to upload.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A policy to use for handling retries when transient errors/failures happen.</param>
        /// <returns>Full details for the blob as it exists in the store (e.g. name, content encoding, content type).</returns>
        Task<HttpResponseMessage> UploadBlobAsync(ProxyBlobDescriptor descriptor, Stream uploadStream, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null);

        /// <summary>
        /// Uploads a batch of telemetry to the proxy endpoint.
        /// </summary>
        /// <param name="messages">The batch of telemetry messages/events to upload.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A policy to use for handling retries when transient errors/failures happen.</param>
        /// <returns>Full details for the blob as it exists in the store (e.g. name, content encoding, content type).</returns>
        Task<HttpResponseMessage> UploadTelemetryAsync(IEnumerable<ProxyTelemetryMessage> messages, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null);
    }
}
