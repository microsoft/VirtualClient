// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using Azure.Core;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents an Azure Key vault Namespace store
    /// </summary>
    public class DependencyKeyVaultStore : DependencyStore
    {
        /// <summary>
        /// Initializes an instance of the <see cref="DependencyKeyVaultStore"/> class.
        /// </summary>
        /// <param name="storeName">The name of the KeyVault store (e.g. KeyVault).</param>
        /// <param name="endpointUri">The URI/SAS for the target Key Vault.</param>
        public DependencyKeyVaultStore(string storeName, Uri endpointUri)
            : base(storeName, DependencyStore.StoreTypeAzureKeyVault)
        {
            endpointUri.ThrowIfNull(nameof(endpointUri));
            this.EndpointUri = endpointUri;
            this.KeyVaultNameSpace = endpointUri.Host;
        }

        /// <summary>
        /// Initializes an instance of the <see cref="DependencyKeyVaultStore"/> class.
        /// </summary>
        /// <param name="storeName">The name of the KeyVault store (e.g. KeyVault).</param>
        /// <param name="endpointUri">The URI/SAS for the target Key Vault.</param>
        /// <param name="credentials">An identity token credential to use for authentication against the Key Vault.</param>
        public DependencyKeyVaultStore(string storeName, Uri endpointUri, TokenCredential credentials)
            : this(storeName, endpointUri)
        {
            credentials.ThrowIfNull(nameof(credentials));
            this.Credentials = credentials;
        }

        /// <summary>
        /// The URI/SAS for the target Key Vault.
        /// </summary>
        public Uri EndpointUri { get; }

        /// <summary>
        /// The Key Vault namespace.
        /// </summary>
        public string KeyVaultNameSpace { get; }

        /// <summary>
        /// An identity token credential to use for authentication against the Key vault. 
        /// </summary>
        public TokenCredential Credentials { get; }
    }
}
