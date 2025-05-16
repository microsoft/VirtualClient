// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Threading;
    using System.Threading.Tasks;
    using Polly;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides methods for retrieving secrets, keys, and certificates from an Azure Key Vault.
    /// </summary>
    public interface IKeyVaultManager
    {
        /// <summary>
        /// Represents the store description/details.
        /// </summary>
        DependencyStore StoreDescription { get; }

        /// <summary>
        /// Retrieves a secret from the Azure Key Vault.
        /// </summary>
        /// <param name="descriptor"> Provides the details for the secret to retrieve (requires "SecretName" and "VaultUri"). </param>
        /// <param name="cancellationToken"> A token that can be used to cancel the operation. </param>
        /// <param name="retryPolicy"> A policy to use for handling retries when transient errors/failures happen. </param>
        /// <returns> A <see cref="KeyVaultDescriptor"/> containing the secret value and metadata. </returns>
        Task<KeyVaultDescriptor> GetSecretAsync(
            KeyVaultDescriptor descriptor,
            CancellationToken cancellationToken,
            IAsyncPolicy retryPolicy = null);

        /// <summary>
        /// Retrieves a key from the Azure Key Vault.
        /// </summary>
        /// <param name="descriptor"> Provides the details for the key to retrieve (requires "KeyName" and "VaultUri"). </param>
        /// <param name="cancellationToken"> A token that can be used to cancel the operation. </param>
        /// <param name="retryPolicy"> A policy to use for handling retries when transient errors/failures happen. </param>
        /// <returns> A <see cref="KeyVaultDescriptor"/> containing the key metadata. </returns>
        Task<KeyVaultDescriptor> GetKeyAsync(
            KeyVaultDescriptor descriptor,
            CancellationToken cancellationToken,
            IAsyncPolicy retryPolicy = null);

        /// <summary>
        /// Retrieves a certificate from the Azure Key Vault.
        /// </summary>
        /// <param name="descriptor"> Provides the details for the certificate to retrieve (requires "CertificateName" and "VaultUri"). </param>
        /// <param name="cancellationToken"> A token that can be used to cancel the operation. </param>
        /// <param name="retryPolicy"> A policy to use for handling retries when transient errors/failures happen. </param>
        /// <returns> A <see cref="KeyVaultDescriptor"/> containing the certificate metadata. </returns>
        Task<KeyVaultDescriptor> GetCertificateAsync(
            KeyVaultDescriptor descriptor,
            CancellationToken cancellationToken,
            IAsyncPolicy retryPolicy = null);
    }
}
