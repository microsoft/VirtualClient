// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Core;
    using Azure.Security.KeyVault.Certificates;
    using Azure.Security.KeyVault.Keys;
    using Azure.Security.KeyVault.Secrets;
    using Polly;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides methods for retrieving secrets, keys, and certificates from an Azure Key Vault.
    /// </summary>
    public class KeyVaultManager : IKeyVaultManager
    {
        private static readonly IAsyncPolicy DefaultRetryPolicy = Policy
            .Handle<RequestFailedException>(error =>
                error.Status < 400 ||
                error.Status == (int)HttpStatusCode.RequestTimeout ||
                error.Status == (int)HttpStatusCode.ServiceUnavailable)
            .WaitAndRetryAsync(5, retries => TimeSpan.FromSeconds(retries + 1));

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultManager"/> class.
        /// </summary>
        public KeyVaultManager()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultManager"/> class.
        /// </summary>
        /// <param name="storeDescription">Provides the store details and requirements for the Key Vault manager.</param>
        public KeyVaultManager(DependencyKeyVaultStore storeDescription)
        {
            storeDescription.ThrowIfNull(nameof(storeDescription));
            this.StoreDescription = storeDescription;
        }

        /// <summary>
        /// Represents the store description/details.
        /// </summary>
        public DependencyStore StoreDescription { get; }

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
        public async Task<string> GetSecretAsync(
            string secretName,
            CancellationToken cancellationToken,
            string keyVaultUri = null,
            IAsyncPolicy retryPolicy = null)
        {
            this.ValidateKeyVaultStore();
            this.StoreDescription.ThrowIfNull(nameof(this.StoreDescription));
            secretName.ThrowIfNullOrWhiteSpace(nameof(secretName), "The secret name cannot be null or empty.");

            // Use the keyVaultUri if provided as a parameter, otherwise use the store's EndpointUri
            Uri vaultUri = !string.IsNullOrWhiteSpace(keyVaultUri)
                ? new Uri(keyVaultUri)
                : ((DependencyKeyVaultStore)this.StoreDescription).EndpointUri;

            SecretClient client = this.CreateSecretClient(vaultUri, ((DependencyKeyVaultStore)this.StoreDescription).Credentials);

            try
            {
                return await (retryPolicy ?? KeyVaultManager.DefaultRetryPolicy).ExecuteAsync(async () =>
                {
                    KeyVaultSecret secret = await client.GetSecretAsync(secretName, cancellationToken: cancellationToken);
                    return secret.Value;
                }).ConfigureAwait(false);
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Forbidden)
            {
                throw new DependencyException(
                    $"Access denied to secret '{secretName}' in vault '{vaultUri}'.",
                    ex,
                    ErrorReason.Http403ForbiddenResponse);
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                throw new DependencyException(
                    $"Secret '{secretName}' not found in vault '{vaultUri}'.",
                    ex,
                    ErrorReason.Http404NotFoundResponse);
            }
            catch (RequestFailedException ex)
            {
                throw new DependencyException(
                    $"Failed to get secret '{secretName}' from vault '{vaultUri}': {ex.Message}",
                    ex,
                    ErrorReason.HttpNonSuccessResponse);
            }
            catch (Exception ex)
            {
                throw new DependencyException(
                    $"Failed to get secret '{secretName}' from vault '{vaultUri}'.",
                    ex,
                    ErrorReason.HttpNonSuccessResponse);
            }
        }

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
        public async Task<KeyVaultKey> GetKeyAsync(
            string keyName,
            CancellationToken cancellationToken,
            string keyVaultUri = null,
            IAsyncPolicy retryPolicy = null)
        {
            this.ValidateKeyVaultStore();
            this.StoreDescription.ThrowIfNull(nameof(this.StoreDescription));
            keyName.ThrowIfNullOrWhiteSpace(nameof(keyName), "The key name cannot be null or empty.");

            // Use the keyVaultUri if provided as a parameter, otherwise use the store's EndpointUri
            Uri vaultUri = !string.IsNullOrWhiteSpace(keyVaultUri)
                ? new Uri(keyVaultUri)
                : ((DependencyKeyVaultStore)this.StoreDescription).EndpointUri;

            KeyClient client = this.CreateKeyClient(vaultUri, ((DependencyKeyVaultStore)this.StoreDescription).Credentials);

            try
            {
                return await (retryPolicy ?? KeyVaultManager.DefaultRetryPolicy).ExecuteAsync(async () =>
                {
                    KeyVaultKey key = await client.GetKeyAsync(keyName, cancellationToken: cancellationToken);
                    return key;
                }).ConfigureAwait(false);
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Forbidden)
            {
                throw new DependencyException(
                    $"Access denied to key '{keyName}' in vault '{vaultUri}'.",
                    ex,
                    ErrorReason.Http403ForbiddenResponse);
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                throw new DependencyException(
                    $"Key '{keyName}' not found in vault '{vaultUri}'.",
                    ex,
                    ErrorReason.Http404NotFoundResponse);
            }
            catch (RequestFailedException ex)
            {
                throw new DependencyException(
                    $"Failed to get key '{keyName}' from vault '{vaultUri}': {ex.Message}",
                    ex,
                    ErrorReason.HttpNonSuccessResponse);
            }
            catch (Exception ex)
            {
                throw new DependencyException(
                    $"Failed to get key '{keyName}' from vault '{vaultUri}'.",
                    ex,
                    ErrorReason.HttpNonSuccessResponse);
            }
        }

        /// <summary>
        /// Retrieves a certificate from the Azure Key Vault.
        /// </summary>
        /// <param name="platform">The operating system platform.</param>
        /// <param name="certName">The name of the certificate to be retrieved</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="keyVaultUri">The URI of the Azure Key Vault.</param>
        /// <param name="retryPolicy">A policy to use for handling retries when transient errors/failures happen.</param>
        /// <returns>
        /// A <see cref="X509Certificate2"/> containing the certificate 
        /// </returns>
        /// <exception cref="DependencyException">
        /// Thrown if the certificate is not found, access is denied, or another error occurs.
        /// </exception>
        public async Task<X509Certificate2> GetCertificateAsync(
            PlatformID platform,
            string certName,
            CancellationToken cancellationToken,
            string keyVaultUri = null,
            IAsyncPolicy retryPolicy = null)
        {
            this.StoreDescription.ThrowIfNull(nameof(this.StoreDescription));
            certName.ThrowIfNullOrWhiteSpace(nameof(certName), "The certificate name cannot be null or empty.");

            // Use the keyVaultUri if provided as a parameter, otherwise use the store's EndpointUri
            Uri vaultUri = !string.IsNullOrWhiteSpace(keyVaultUri)
                ? new Uri(keyVaultUri)
                : ((DependencyKeyVaultStore)this.StoreDescription).EndpointUri;

            try
            {
                KeyVaultSecret keyVaultSecret = await (retryPolicy ?? KeyVaultManager.DefaultRetryPolicy).ExecuteAsync(async () =>
                {
                    SecretClient secretsClient = new SecretClient(vaultUri, ((DependencyKeyVaultStore)this.StoreDescription).Credentials);
                    Response<KeyVaultSecret> response = await secretsClient.GetSecretAsync(certName, version: null, cancellationToken);

                    return response.Value;
                }).ConfigureAwait(false);

                byte[] privateKeyBytes = Convert.FromBase64String(keyVaultSecret.Value);
                X509Certificate2 certificate = null;

                var keyStorageFlags = X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet;

#if NET9_0_OR_GREATER
                if (platform == PlatformID.Unix)
                {
                    certificate = X509CertificateLoader.LoadPkcs12(privateKeyBytes, null, X509KeyStorageFlags.PersistKeySet);
                }
                else if (platform == PlatformID.Win32NT)
                {
                    certificate = X509CertificateLoader.LoadPkcs12(privateKeyBytes, null, keyStorageFlags);
                }
#elif NET8_0_OR_GREATER
                if (platform == PlatformID.Unix)
                {
                    certificate = new X509Certificate2(privateKeyBytes, (string)null, X509KeyStorageFlags.PersistKeySet);
                }
                else if (platform == PlatformID.Win32NT)
                {
                    certificate = new X509Certificate2(privateKeyBytes, (string)null, keyStorageFlags);
                }
#endif

                if (certificate is null || !certificate.HasPrivateKey)
                {
                    throw new DependencyException("Failed to retrieve certificate content with private key.");
                }

                return certificate;
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.Forbidden)
            {
                throw new DependencyException(
                    $"Access denied to certificate '{certName}' in vault '{vaultUri}'.",
                    ex,
                    ErrorReason.Http403ForbiddenResponse);
            }
            catch (RequestFailedException ex) when (ex.Status == (int)HttpStatusCode.NotFound)
            {
                throw new DependencyException(
                    $"Certificate '{certName}' not found in vault '{vaultUri}'.",
                    ex,
                    ErrorReason.Http404NotFoundResponse);
            }
            catch (RequestFailedException ex)
            {
                throw new DependencyException(
                    $"Failed to get certificate '{certName}' from vault '{vaultUri}': {ex.Message}",
                    ex,
                    ErrorReason.HttpNonSuccessResponse);
            }
            catch (Exception ex)
            {
                throw new DependencyException(
                    $"Failed to get certificate '{certName}' from vault '{vaultUri}'.",
                    ex,
                    ErrorReason.HttpNonSuccessResponse);
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SecretClient"/> class.
        /// </summary>
        /// <param name="vaultUri">The URI of the Azure Key Vault.</param>
        /// <param name="credential">The credentials used to authenticate with the Azure Key Vault.</param>
        /// <returns>A <see cref="SecretClient"/> instance.</returns>
        protected virtual SecretClient CreateSecretClient(Uri vaultUri, TokenCredential credential)
        {
            return new SecretClient(vaultUri, credential);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="KeyClient"/> class.
        /// </summary>
        /// <param name="vaultUri">The URI of the Azure Key Vault.</param>
        /// <param name="credential">The credentials used to authenticate with the Azure Key Vault.</param>
        /// <returns>A <see cref="KeyClient"/> instance.</returns>
        protected virtual KeyClient CreateKeyClient(Uri vaultUri, TokenCredential credential)
        {
            return new KeyClient(vaultUri, credential);
        }

        /// <summary>
        /// Creates a new instance of the <see cref="CertificateClient"/> class.
        /// </summary>
        /// <param name="vaultUri">The URI of the Azure Key Vault.</param>
        /// <param name="credential">The credentials used to authenticate with the Azure Key Vault.</param>
        /// <returns>A <see cref="CertificateClient"/> instance.</returns>
        protected virtual CertificateClient CreateCertificateClient(Uri vaultUri, TokenCredential credential)
        {
            return new CertificateClient(vaultUri, credential);
        }

        /// <summary>
        /// Validates that the required properties are present in the dependency descriptor.
        /// </summary>
        /// <exception cref="DependencyException">
        /// Thrown if any required property is missing or empty.
        /// </exception>
        private void ValidateKeyVaultStore()
        {
            if (this.StoreDescription == null)
            {
                throw new DependencyException(
                        $"Cannot Resolve Keyvault Objects as could not find any KeyVault references. " +
                        $"Please provide the keyVault details using --keyVault parameter of Virtual Client",
                        ErrorReason.DependencyDescriptionInvalid);
            }
        }
    }
}
