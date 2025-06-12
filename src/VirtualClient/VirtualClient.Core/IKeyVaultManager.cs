// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Security.KeyVault.Keys;
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
        /// <returns> A <see cref="string"/> containing the secret value. </returns>
        Task<string> GetSecretAsync(
            KeyVaultDescriptor descriptor,
            CancellationToken cancellationToken,
            IAsyncPolicy retryPolicy = null);

        /// <summary>
        /// Retrieves a key from the Azure Key Vault.
        /// </summary>
        /// <param name="descriptor"> Provides the details for the key to retrieve (requires "KeyName" and "VaultUri"). </param>
        /// <param name="cancellationToken"> A token that can be used to cancel the operation. </param>
        /// <param name="retryPolicy"> A policy to use for handling retries when transient errors/failures happen. </param>
        /// <returns> A <see cref="KeyVaultKey"/> containing the key properties. </returns>
        Task<KeyVaultKey> GetKeyAsync(
            KeyVaultDescriptor descriptor,
            CancellationToken cancellationToken,
            IAsyncPolicy retryPolicy = null);

        /// <summary>
        /// Retrieves a certificate from the Azure Key Vault.
        /// </summary>
        /// <param name="descriptor"> Provides the details for the certificate to retrieve (requires "CertificateName" and "VaultUri"). </param>
        /// <param name="cancellationToken"> A token that can be used to cancel the operation. </param>
        /// <param name="retrieveWithPrivateKey"> Indicates whether the private key should be retrieved along with the certificate. Default is false </param>
        /// <param name="retryPolicy"> A policy to use for handling retries when transient errors/failures happen. </param>
        /// <returns> A <see cref="X509Certificate2"/> containing the requested certificate. </returns>
        Task<X509Certificate2> GetCertificateAsync(
            KeyVaultDescriptor descriptor,
            CancellationToken cancellationToken,
            bool retrieveWithPrivateKey = false,
            IAsyncPolicy retryPolicy = null);
    }
}
