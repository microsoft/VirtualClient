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
        /// A connection string or SAS token used to authenticate/authorize with the blob store.
        /// </summary>
        public string ConnectionToken { get; }
    }
}
