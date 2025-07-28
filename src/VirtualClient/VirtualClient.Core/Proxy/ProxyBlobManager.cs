// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Polly;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Proxy;

    /// <summary>
    /// A blob manager for uploading and downloading blobs via a proxy API endpoint.
    /// </summary>
    internal class ProxyBlobManager : IBlobManager
    {
        internal static readonly List<int> RetryableCodes = new List<int>
        {
            (int)HttpStatusCode.BadGateway,
            (int)HttpStatusCode.GatewayTimeout,
            (int)HttpStatusCode.ServiceUnavailable,
            (int)HttpStatusCode.GatewayTimeout,
            (int)HttpStatusCode.InternalServerError
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobManager"/> class.
        /// </summary>
        /// <param name="storeDescription">Provides the store details and requirement for the blob manager.</param>
        /// <param name="apiClient">The API client for interacting with the proxy endpoint.</param>
        /// <param name="source">Defines an explicit source to use for blob downloads/uploads.</param>
        public ProxyBlobManager(DependencyProxyStore storeDescription, IProxyApiClient apiClient, string source = null)
        {
            storeDescription.ThrowIfNull(nameof(storeDescription));
            apiClient.ThrowIfNull(nameof(apiClient));

            this.ApiClient = apiClient;
            this.StoreDescription = storeDescription;
            this.Source = source;
        }

        /// <summary>
        /// Event is invoked whenever a blob is uploaded.
        /// </summary>
        public event EventHandler<ProxyBlobEventArgs> BlobUpload;

        /// <summary>
        ///  Event is invoked whenever a blob upload fails.
        /// </summary>
        public event EventHandler<ProxyBlobEventArgs> BlobUploadError;

        /// <summary>
        /// Event is invoked whenever a blob is downloaded.
        /// </summary>
        public event EventHandler<ProxyBlobEventArgs> BlobDownload;

        /// <summary>
        ///  Event is invoked whenever a blob download fails.
        /// </summary>
        public event EventHandler<ProxyBlobEventArgs> BlobDownloadError;

        /// <summary>
        /// Represents the store description/details.
        /// </summary>
        public DependencyStore StoreDescription { get; }

        /// <summary>
        /// The API client for interacting with the proxy endpoint.
        /// </summary>
        protected IProxyApiClient ApiClient { get; }

        /// <summary>
        /// Defines an explicit source to use for blob downloads/uploads.
        /// </summary>
        protected string Source { get; }

        public async Task<DependencyDescriptor> DownloadBlobAsync(DependencyDescriptor descriptor, Stream downloadStream, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null)
        {
            descriptor.ThrowIfNull(nameof(descriptor));
            downloadStream.ThrowIfNull(nameof(downloadStream));

            ProxyBlobManager.ValidateDescriptor(descriptor);
            BlobDescriptor blobDescriptor = new BlobDescriptor(descriptor);

            string blobName;
            string blobPath;
            if (!ProxyBlobDescriptor.TryGetBlobPath(blobDescriptor.Name, out blobName, out blobPath))
            {
                blobName = descriptor.Name;
            }

            ProxyBlobDescriptor info = new ProxyBlobDescriptor(
                this.StoreDescription.StoreName,
                blobName,
                blobDescriptor.ContainerName,
                blobDescriptor.ContentType ?? "application/octet-stream",
                blobDescriptor.ContentEncoding.WebName,
                blobPath: blobPath,
                source: this.Source);

            this.BlobDownload?.Invoke(this, new ProxyBlobEventArgs(info));
            HttpResponseMessage response = await this.ApiClient.DownloadBlobAsync(info, downloadStream, cancellationToken)
                .ConfigureAwait(true);

            if (!response.IsSuccessStatusCode)
            {
                this.BlobDownloadError?.Invoke(this, new ProxyBlobEventArgs(info, new
                {
                    httpStatus = response.StatusCode.ToString(),
                    httpStatusCode = response.StatusCode
                }));
            }

            response.ThrowOnError<DependencyException>(ErrorReason.DependencyInstallationFailed);

            return descriptor;
        }

        public async Task<DependencyDescriptor> UploadBlobAsync(DependencyDescriptor descriptor, Stream uploadStream, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null)
        {
            descriptor.ThrowIfNull(nameof(descriptor));
            uploadStream.ThrowIfNull(nameof(uploadStream));

            ProxyBlobManager.ValidateDescriptor(descriptor);
            BlobDescriptor blobDescriptor = new BlobDescriptor(descriptor);

            string blobName;
            string blobPath;
            if (!ProxyBlobDescriptor.TryGetBlobPath(blobDescriptor.Name, out blobName, out blobPath))
            {
                blobName = descriptor.Name;
            }

            ProxyBlobDescriptor info = new ProxyBlobDescriptor(
                this.StoreDescription.StoreName,
                blobName,
                blobDescriptor.ContainerName,
                blobDescriptor.ContentType ?? "application/octet-stream",
                blobDescriptor.ContentEncoding.WebName,
                blobPath: blobPath,
                source: this.Source);

            this.BlobUpload?.Invoke(this, new ProxyBlobEventArgs(info));
            HttpResponseMessage response = await this.ApiClient.UploadBlobAsync(info, uploadStream, cancellationToken)
                .ConfigureAwait(true);

            if (!response.IsSuccessStatusCode)
            {
                this.BlobUploadError?.Invoke(this, new ProxyBlobEventArgs(info, new
                {
                    httpStatus = response.StatusCode.ToString(),
                    httpStatusCode = response.StatusCode
                }));
            }

            response.ThrowOnError<DependencyException>(ErrorReason.ApiRequestFailed);

            return descriptor;
        }

        private static void ValidateDescriptor(DependencyDescriptor descriptor)
        {
            string requiredProperty = "Name";
            if (!descriptor.ContainsKey(requiredProperty))
            {
                throw new DependencyException(
                    $"Required property missing. The descriptor supplied does not contain the required '{requiredProperty}' property. " +
                    $"This describes the name of the blob to upload or download.");
            }

            requiredProperty = "ContainerName";
            if (!descriptor.ContainsKey(requiredProperty))
            {
                throw new DependencyException(
                    $"Required property missing. The descriptor supplied does not contain the required '{requiredProperty}' property. " +
                    $"This describes the name of the container in which the blob to be uploaded or downloaded exists.");
            }
        }
    }
}
