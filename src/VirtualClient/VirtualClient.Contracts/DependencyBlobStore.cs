// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using Azure.Core;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents an Azure Storage Account blob store.
    /// </summary>
    public class DependencyBlobStore : DependencyStore
    {
        /// <summary>
        /// Initializes an instance of the <see cref="DependencyBlobStore"/> class.
        /// </summary>
        /// <param name="storeName">The name of the content store (e.g. Content, Packages).</param>
        /// <param name="connectionString">A connection string to the target Storage Account.</param>
        public DependencyBlobStore(string storeName, string connectionString)
            : base(storeName, DependencyStore.StoreTypeAzureStorageBlob)
        {
            connectionString.ThrowIfNullOrWhiteSpace(nameof(connectionString));
            this.ConnectionString = connectionString;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="DependencyBlobStore"/> class.
        /// </summary>
        /// <param name="storeName">The name of the content store (e.g. Content, Packages).</param>
        /// <param name="endpointUri">The endpoint URI to the target Storage Account (e.g. SAS URI).</param>
        public DependencyBlobStore(string storeName, Uri endpointUri)
            : base(storeName, DependencyStore.StoreTypeAzureStorageBlob)
        {
            endpointUri.ThrowIfNull(nameof(endpointUri));
            this.EndpointUri = endpointUri;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="DependencyBlobStore"/> class.
        /// </summary>
        /// <param name="storeName">The name of the content store (e.g. Content, Packages).</param>
        /// <param name="endpointUri">The endpoint URI to the target Storage Account (e.g. SAS URI).</param>
        /// <param name="credentials">An identity token credential to use for authentication against the Storage Account.</param>
        public DependencyBlobStore(string storeName, Uri endpointUri, TokenCredential credentials)
            : this(storeName, endpointUri)
        {
            credentials.ThrowIfNull(nameof(credentials));
            this.Credentials = credentials;
        }

        /// <summary>
        /// The endpoint connection string used to access the target Storage Account.
        /// </summary>
        public string ConnectionString { get; }

        /// <summary>
        /// The endpoint URI or SAS URI used to access the target Storage Account.
        /// </summary>
        public Uri EndpointUri { get; }

        /// <summary>
        /// An identity token credential to use for authentication against the Storage Account. 
        /// </summary>
        public TokenCredential Credentials { get; }
    }
}
