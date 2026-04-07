// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Proxy
{
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides information for a blob to download from/upload to a store via a 
    /// proxy endpoint.
    /// </summary>
    public class ProxyBlobDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyBlobDescriptor"/> class.
        /// </summary>
        /// <param name="storeType">The type of blob/content store (e.g. Content, Packages).</param>
        /// <param name="blobName">The name of the blob including its path (e.g. anyblob.zip, /any/path/to/the/blob/anyblob.zip).</param>
        /// <param name="containerName">The name of the blob container.</param>
        /// <param name="contentType">The MIME type of content (e.g. application/json).</param>
        /// <param name="contentEncoding">The web encoding for the content (e.g. UTF-8 -> Encoding.UTF8.WebName).</param>
        /// <param name="blobPath">An optional path/virtual path to where the blob should be stored. This is equivalent to a directory path.</param>
        /// <param name="source">The source of the blob upload/download request (e.g. VirtualClient).</param>
        public ProxyBlobDescriptor(string storeType, string blobName, string containerName, string contentType, string contentEncoding, string blobPath = null, string source = null)
        {
            storeType.ThrowIfNullOrWhiteSpace(nameof(storeType));
            blobName.ThrowIfNullOrWhiteSpace(nameof(blobName));
            containerName.ThrowIfNullOrWhiteSpace(nameof(containerName));
            contentType.ThrowIfNullOrWhiteSpace(nameof(contentType));
            contentEncoding.ThrowIfNullOrWhiteSpace(nameof(contentEncoding));

            this.Source = source?.Trim();
            this.StoreType = storeType.Trim();
            this.BlobName = blobName.Trim();
            this.BlobPath = blobPath?.Trim()?.TrimEnd('/');
            this.ContainerName = containerName.Trim();
            this.ContentType = contentType.Trim();
            this.ContentEncoding = contentEncoding.Trim();
        }

        /// <summary>
        /// The name of the blob including its path (e.g. anyblob.zip).
        /// </summary>
        public string BlobName { get; }

        /// <summary>
        /// An optional path/virtual path to where the blob should be stored. This is equivalent to a directory path
        /// (e.g. /any/path/to/the/blob).
        /// </summary>
        public string BlobPath { get; }

        /// <summary>
        /// The name of the blob container.
        /// </summary>
        public string ContainerName { get; }

        /// <summary>
        /// The encoding for the content (e.g. UTF-8 -> Encoding.UTF8.WebName).
        /// </summary>
        public string ContentEncoding { get; }

        /// <summary>
        /// The MIME type of content (e.g. application/json, application/octet-stream).
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// The type of blob/content store (e.g. Content, Packages).
        /// </summary>
        public string StoreType { get; }

        /// <summary>
        /// The source of the blob upload/download request (e.g. VirtualClient).
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// Returns true if the blob name is a full path to the blob and outputs both the
        /// blob name and path separately.
        /// </summary>
        /// <param name="name">The name of the blob that might also contain a path/virtual path.</param>
        /// <param name="blobName">The blob name parsed from the path.</param>
        /// <param name="blobPath">The path portion of the full blob name.</param>
        /// <returns></returns>
        public static bool TryGetBlobPath(string name, out string blobName, out string blobPath)
        {
            blobName = null;
            blobPath = null;

            string sanitizedBlobName = name.Trim('/');
            int indexOfPath = sanitizedBlobName.LastIndexOf('/');

            if (indexOfPath >= 0)
            {
                // e.g.
                // /any/path/to/the/blob/blobname.zip
                //
                // /any/path/to/the/blob
                blobPath = $"/{sanitizedBlobName.Substring(0, indexOfPath).Trim()}";

                // blobname.zip
                blobName = sanitizedBlobName.Substring(indexOfPath + 1).Trim();
            }

            return blobPath != null;
        }
    }
}
