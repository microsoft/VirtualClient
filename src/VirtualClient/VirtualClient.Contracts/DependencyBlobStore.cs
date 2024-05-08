using Azure.Core;
using VirtualClient.Common.Extensions;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    /// <summary>
    /// Represents an Azure storage account blob store.
    /// </summary>
    public class DependencyBlobStore : DependencyStore
    {
        /// <summary>
        /// Initializes an instance of the <see cref="DependencyBlobStore"/> class. Uses Azure managed identity as default auth.
        /// </summary>
        /// <param name="storeName">The name of the content store (e.g. Content, Packages).</param>
        public DependencyBlobStore(string storeName)
            : base(storeName, DependencyStore.StoreTypeAzureStorageBlob)
        {
            this.UseManagedIdentity = true;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="DependencyBlobStore"/> class.
        /// </summary>
        /// <param name="storeName">The name of the content store (e.g. Content, Packages).</param>
        /// <param name="connectionToken">A connection string or SAS token used to authenticate/authorize with the blob store.</param>
        public DependencyBlobStore(string storeName, string connectionToken)
            : base(storeName, DependencyStore.StoreTypeAzureStorageBlob)
        {
            connectionToken.ThrowIfNullOrWhiteSpace(nameof(connectionToken));
            this.ConnectionToken = connectionToken;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="DependencyBlobStore"/> class.
        /// </summary>
        /// <param name="storeName">The name of the content store (e.g. Content, Packages).</param>
        /// <param name="endpointUrl"></param>
        /// <param name="tokenCredential"></param>
        public DependencyBlobStore(string storeName, string endpointUrl, TokenCredential tokenCredential)
            : base(storeName, DependencyStore.StoreTypeAzureStorageBlob)
        {
            this.EndpointUrl = endpointUrl;
            this.TokenCredential = tokenCredential;
        }

        /// <summary>
        /// A connection string or SAS token used to authenticate/authorize with the blob store.
        /// </summary>
        public string ConnectionToken { get; }

        /// <summary>
        /// Endpoint for Azure Storage url
        /// </summary>
        public string EndpointUrl { get; }

        /// <summary>
        /// TokenCredential for Azure Storage
        /// </summary>
        public TokenCredential TokenCredential { get; }
    }
}
