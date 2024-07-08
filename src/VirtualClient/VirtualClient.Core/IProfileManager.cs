// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;
    using Polly;

    /// <summary>
    /// Provides features for managing profiles and related operations.
    /// </summary>
    public interface IProfileManager
    {
        /// <summary>
        /// Downloads a profile from the target URI endpoint location into the stream provided (e.g. in-memory stream, file stream).
        /// </summary>
        /// <param name="profileUri">The URI to the profile to download.</param>
        /// <param name="downloadStream">The stream into which the blob will be downloaded.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="credentials">Identity credentials to use for authentication against the store.</param>
        /// <param name="retryPolicy">A policy to use for handling retries when transient errors/failures happen.</param>
        /// <returns>Full details for the blob as it exists in the store (e.g. name, content encoding, content type).</returns>
        Task DownloadProfileAsync(Uri profileUri, Stream downloadStream, CancellationToken cancellationToken, TokenCredential credentials = null, IAsyncPolicy retryPolicy = null);
    }
}
