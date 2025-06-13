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
        /// <param name="secretName">The name of the secret to be retrieved</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="keyVaultUri">The URI of the Azure Key Vault.</param>
        /// <param name="retryPolicy">A policy to use for handling retries when transient errors/failures happen.</param>
        /// <returns>
        /// A <see cref="string"/> containing the secret value.
        /// </returns>
        /// <exception cref="DependencyException">
        /// Thrown if the secret is not found, access is denied, or another error occurs.
        /// </exception>
        Task<string> GetSecretAsync(
            string secretName,
            CancellationToken cancellationToken,
            string keyVaultUri = null,
            IAsyncPolicy retryPolicy = null);

        /// <summary>
        /// Retrieves a key from the Azure Key Vault.
        /// </summary>
        /// <param name="keyName">The name of the key to be retrieved</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="keyVaultUri">The URI of the Azure Key Vault.</param>
        /// <param name="retryPolicy">A policy to use for handling retries when transient errors/failures happen.</param>
        /// <returns>
        /// A <see cref="KeyVaultKey"/> containing the key.
        /// </returns>
        /// <exception cref="DependencyException">
        /// Thrown if the key is not found, access is denied, or another error occurs.
        /// </exception>
        Task<KeyVaultKey> GetKeyAsync(
            string keyName,
            CancellationToken cancellationToken,
            string keyVaultUri = null,
            IAsyncPolicy retryPolicy = null);

        /// <summary>
        /// Retrieves a certificate from the Azure Key Vault.
        /// </summary>
        /// <param name="certName">The name of the certificate to be retrieved</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="keyVaultUri">The URI of the Azure Key Vault.</param>
        /// <param name="retrieveWithPrivateKey">flag to decode whether to retrieve certificate with private key</param>
        /// <param name="retryPolicy">A policy to use for handling retries when transient errors/failures happen.</param>
        /// <returns>
        /// A <see cref="X509Certificate2"/> containing the certificate.
        /// </returns>
        /// <exception cref="DependencyException">
        /// Thrown if the certificate is not found, access is denied, or another error occurs.
        /// </exception>
        Task<X509Certificate2> GetCertificateAsync(
            string certName,
            CancellationToken cancellationToken,
            string keyVaultUri = null,
            bool retrieveWithPrivateKey = false,
            IAsyncPolicy retryPolicy = null);
    }
}
