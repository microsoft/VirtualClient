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
            this.UseConnectionToken = true;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="DependencyBlobStore"/> class.
        /// </summary>
        /// <param name="storeName">The name of the content store (e.g. Content, Packages).</param>
        /// <param name="certificateCommonName"></param>
        /// <param name="issuer"></param>
        /// <param name="certificateThumbprint"></param>
        /// <param name="clientId"></param>
        /// <param name="tenantId"></param>
        public DependencyBlobStore(string storeName, string certificateCommonName, string issuer, string certificateThumbprint, string clientId, string tenantId)
            : base(storeName, DependencyStore.StoreTypeAzureStorageBlob)
        {
            this.CertificateCommonName = certificateCommonName;
            this.Issuer = issuer;
            this.CertificateThumbprint = certificateThumbprint;
            this.ClientId = clientId;
            this.TenantId = tenantId;
            this.UseCertificate = true;
        }

        /// <summary>
        /// A connection string or SAS token used to authenticate/authorize with the blob store.
        /// </summary>
        public string ConnectionToken { get; }

        /// <summary>
        /// If blob store uses connection token to authenticate.
        /// </summary>
        public bool UseConnectionToken { get; }

        /// <summary>
        /// Certificate common name to search in certificate store
        /// </summary>
        public string CertificateCommonName { get; }

        /// <summary>
        /// Certificate common name to search in certificate store
        /// </summary>
        public string CertificateThumbprint { get; }

        /// <summary>
        /// Client Id for 
        /// </summary>
        public string ClientId { get; }

        /// <summary>
        /// 
        /// </summary>
        public string TenantId { get; }

        /// <summary>
        /// 
        /// </summary>
        public string Issuer { get; }

        /// <summary>
        /// If blob store uses certificate to authenticate.
        /// </summary>
        public bool UseCertificate { get; }

        /// <summary>
        /// If blob store uses managed identity to authenticate.
        /// </summary>
        public bool UseManagedIdentity { get; }
    }
}
