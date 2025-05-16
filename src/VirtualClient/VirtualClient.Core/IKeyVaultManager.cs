// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Security.Cryptography.X509Certificates;
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
        /// A retry policy to apply to transient issues with accessing secrets in an Azure Key Vault.
        /// </summary>
        IAsyncPolicy KeyVaultAccessRetryPolicy { get; set; }

        /// <summary>
        /// Represents the store description/details.
        /// </summary>
        DependencyStore StoreDescription { get; }

        /// <summary>
        /// Retrieves a certificate from the Azure Key Vault.
        /// </summary>
        /// <param name="certificateName">Certificate Name to download.</param>
        /// <param name="systemManagement">Provides access to system and environment management features.</param>
        /// <param name="cancellationToken"> A token that can be used to cancel the operation. </param>
        /// <param name="retryPolicy"> A policy to use for handling retries when transient errors/failures happen. </param>
        /// <returns> A <see cref="X509Certificate2"/> containing the certificate metadata. </returns>
        Task<X509Certificate2> GetCertificateAsync(
            string certificateName,
            ISystemManagement systemManagement,
            CancellationToken cancellationToken,
            IAsyncPolicy retryPolicy = null);
    }
}