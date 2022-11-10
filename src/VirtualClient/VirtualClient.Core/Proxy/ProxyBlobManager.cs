// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Proxy
{
    using System.Collections.Generic;
    using System.IO;
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
        public ProxyBlobManager(DependencyProxyStore storeDescription, IProxyApiClient apiClient)
        {
            storeDescription.ThrowIfNull(nameof(storeDescription));
            apiClient.ThrowIfNull(nameof(apiClient));

            this.ApiClient = apiClient;
            this.StoreDescription = storeDescription;
        }

        /// <summary>
        /// Represents the store description/details.
        /// </summary>
        public DependencyStore StoreDescription { get; }

        /// <summary>
        /// The API client for interacting with the proxy endpoint.
        /// </summary>
        protected IProxyApiClient ApiClient { get; }

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
                "VirtualClient",
                this.StoreDescription.StoreName,
                blobName,
                blobDescriptor.ContainerName,
                blobDescriptor.ContentType ?? "application/octet-stream",
                blobDescriptor.ContentEncoding.WebName,
                blobPath);

            HttpResponseMessage response = await this.ApiClient.DownloadBlobAsync(info, downloadStream, cancellationToken)
                .ConfigureAwait(true);

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
                "VirtualClient",
                this.StoreDescription.StoreName,
                blobName,
                blobDescriptor.ContainerName,
                blobDescriptor.ContentType ?? "application/octet-stream",
                blobDescriptor.ContentEncoding.WebName,
                blobPath);

            HttpResponseMessage response = await this.ApiClient.UploadBlobAsync(info, uploadStream, cancellationToken)
                .ConfigureAwait(true);

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
