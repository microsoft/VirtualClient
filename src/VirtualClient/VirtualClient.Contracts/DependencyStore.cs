// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents a store that can host dependencies and extensions for download to the
    /// platform runtime.
    /// </summary>
    public class DependencyStore
    {
        /// <summary>
        /// Content store name.
        /// </summary>
        public const string Content = nameof(DependencyStore.Content);

        /// <summary>
        /// KeyVault store name.
        /// </summary>
        public const string KeyVault = nameof(DependencyStore.KeyVault);

        /// <summary>
        /// Packages store name.
        /// </summary>
        public const string Packages = nameof(DependencyStore.Packages);

        /// <summary>
        /// Profiles store name.
        /// </summary>
        public const string Profiles = nameof(DependencyStore.Profiles);

        /// <summary>
        /// Store Type = AzureStorageBlob
        /// </summary>
        public const string StoreTypeAzureStorageBlob = "AzureStorageBlob";

        /// <summary>
        /// Store Type = AzureStorageBlob
        /// </summary>
        public const string StoreTypeAzureEventHubNamespace = "AzureEventHubNamespace";

        /// <summary>
        /// Store Type = FileSystem
        /// </summary>
        public const string StoreTypeFileSystem = "FileSystem";

        /// <summary>
        /// Store Type = AzureKeyVault
        /// </summary>
        public const string StoreTypeAzureKeyVault = "AzureKeyVault";

        /// <summary>
        /// Store Type = ProxyApi
        /// </summary>
        public const string StoreTypeProxyApi = "ProxyApi";

        /// <summary>
        /// Store Type = AzureCDN
        /// </summary>
        public const string StoreTypeAzureCDN = "AzureCDN";

        /// <summary>
        /// Telemetry store name.
        /// </summary>
        public const string Telemetry = nameof(DependencyStore.Telemetry);

        /// <summary>
        /// Initializes an instance of the <see cref="DependencyBlobStore"/> class.
        /// </summary>
        /// <param name="storeName">The name of the content store (e.g. Content, Packages).</param>
        /// <param name="storeType">The type of store (e.g. FileSystem, AzureStorageBlob).</param>
        protected DependencyStore(string storeName, string storeType)
        {
            storeName.ThrowIfNullOrWhiteSpace(nameof(storeName));
            storeType.ThrowIfNullOrWhiteSpace(nameof(storeType));

            this.StoreName = storeName;
            this.StoreType = storeType;
        }

        /// <summary>
        /// A friendly name for the content store that can be referenced by
        /// other parts of the application (e.g. Monitoring).
        /// </summary>
        public string StoreName { get; }

        /// <summary>
        /// A friendly name for the content store that can be referenced by
        /// other parts of the application (e.g. Monitoring).
        /// </summary>
        public string StoreType { get; }
    }
}
