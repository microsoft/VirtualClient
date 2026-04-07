// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Polly;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides methods for uploading and downloading blobs from a store.
    /// </summary>
    public interface IBlobManager
    {
        /// <summary>
        /// Represents the store description/details.
        /// </summary>
        DependencyStore StoreDescription { get; }

        /// <summary>
        /// Downloads a blob from the store into the stream provided (e.g. in-memory stream, file stream).
        /// </summary>
        /// <param name="descriptor">Provides the storage location details for the blob to download.</param>
        /// <param name="downloadStream">The stream into which the blob will be downloaded.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A policy to use for handling retries when transient errors/failures happen.</param>
        /// <returns>Full details for the blob as it exists in the store (e.g. name, content encoding, content type).</returns>
        Task<DependencyDescriptor> DownloadBlobAsync(DependencyDescriptor descriptor, Stream downloadStream, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null);

        /// <summary>
        /// Uploads a blob from the stream provided into the store.
        /// </summary>
        /// <param name="descriptor">Provides the storage location and content details for the blob being uploaded.</param>
        /// <param name="uploadStream">The stream that contains the blob binary content to upload.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A policy to use for handling retries when transient errors/failures happen.</param>
        /// <param name="metadata">Metadata in which to tag the blob.</param>
        /// <returns>Full details for the blob as it exists in the store (e.g. name, content encoding, content type).</returns>
        Task<DependencyDescriptor> UploadBlobAsync(DependencyDescriptor descriptor, Stream uploadStream, CancellationToken cancellationToken, IDictionary<string, IConvertible> metadata = null, IAsyncPolicy retryPolicy = null);
    }
}
