// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Net;
    using System.Runtime.ConstrainedExecution;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;
    using Azure.Core;
    using Azure.Security.KeyVault.Certificates;
    using Azure.Security.KeyVault.Keys;
    using Azure.Security.KeyVault.Secrets;
    using Polly;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

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
        /// <param name="descriptor">Provides the details for the secret to retrieve (requires "SecretName" and "VaultUri").</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A policy to use for handling retries when transient errors/failures happen.</param>
        /// <returns>
        /// A <see cref="KeyVaultDescriptor"/> containing the secret value and metadata.
        /// </returns>
        /// <exception cref="DependencyException">
        /// Thrown if the secret is not found, access is denied, or another error occurs.
        /// </exception>
        public async Task<KeyVaultDescriptor> GetSecretAsync(
            KeyVaultDescriptor descriptor,
            CancellationToken cancellationToken,
            IAsyncPolicy retryPolicy = null)
        {
            this.ValidateKeyVaultStore();
            this.StoreDescription.ThrowIfNull(nameof(this.StoreDescription));
            KeyVaultManager.ValidateDescriptor(descriptor, nameof(descriptor.Name));

            // Use descriptor.VaultUri if set, otherwise use the store's EndpointUri
            Uri vaultUri = !string.IsNullOrWhiteSpace(descriptor.VaultUri)
                ? new Uri(descriptor.VaultUri)
                : ((DependencyKeyVaultStore)this.StoreDescription).EndpointUri;

            string secretName = descriptor.Name;

            SecretClient client = this.CreateSecretClient(vaultUri, ((DependencyKeyVaultStore)this.StoreDescription).Credentials);

            try
            {
                return await (retryPolicy ?? KeyVaultManager.DefaultRetryPolicy).ExecuteAsync(async () =>
                {
                    KeyVaultSecret secret = await client.GetSecretAsync(secretName, cancellationToken: cancellationToken);
                    KeyVaultDescriptor result = new KeyVaultDescriptor(descriptor)
                    {
                        Value = secret.Value,
                        Version = secret.Properties.Version,
                        Name = secretName,
                        VaultUri = vaultUri.ToString(),
                        ObjectId = secret.Id?.ToString(),
                        ObjectType = KeyVaultObjectType.Secret
                    };
                    return result;
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
        /// <param name="descriptor">Provides the details for the key to retrieve (requires "KeyName" and "VaultUri").</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A policy to use for handling retries when transient errors/failures happen.</param>
        /// <returns>
        /// A <see cref="KeyVaultDescriptor"/> containing the key metadata.
        /// </returns>
        /// <exception cref="DependencyException">
        /// Thrown if the key is not found, access is denied, or another error occurs.
        /// </exception>
        public async Task<KeyVaultDescriptor> GetKeyAsync(
            KeyVaultDescriptor descriptor,
            CancellationToken cancellationToken,
            IAsyncPolicy retryPolicy = null)
        {
            this.ValidateKeyVaultStore();
            KeyVaultManager.ValidateDescriptor(descriptor, nameof(descriptor.Name));

            // Use descriptor.VaultUri if set, otherwise use the store's EndpointUri
            Uri vaultUri = !string.IsNullOrWhiteSpace(descriptor.VaultUri)
                ? new Uri(descriptor.VaultUri)
                : ((DependencyKeyVaultStore)this.StoreDescription).EndpointUri;

            string keyName = descriptor.Name;
            KeyClient client = this.CreateKeyClient(vaultUri, ((DependencyKeyVaultStore)this.StoreDescription).Credentials);

            try
            {
                return await (retryPolicy ?? KeyVaultManager.DefaultRetryPolicy).ExecuteAsync(async () =>
                {
                    KeyVaultKey key = await client.GetKeyAsync(keyName, cancellationToken: cancellationToken);
                    KeyVaultDescriptor result = new KeyVaultDescriptor(descriptor)
                    {
                        ObjectType = KeyVaultObjectType.Key,
                        Name = keyName,
                        VaultUri = vaultUri.ToString(),
                        Version = key.Properties.Version,
                        ObjectId = key.Id.ToString()
                    };
                    return result;
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
        /// <param name="descriptor">Provides the details for the certificate to retrieve (requires "CertificateName" and "VaultUri").</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A policy to use for handling retries when transient errors/failures happen.</param>
        /// <returns>
        /// A <see cref="KeyVaultDescriptor"/> containing the certificate metadata.
        /// </returns>
        /// <exception cref="DependencyException">
        /// Thrown if the certificate is not found, access is denied, or another error occurs.
        /// </exception>
        public async Task<KeyVaultDescriptor> GetCertificateAsync(
            KeyVaultDescriptor descriptor,
            CancellationToken cancellationToken,
            IAsyncPolicy retryPolicy = null)
        {
            this.ValidateKeyVaultStore();
            KeyVaultManager.ValidateDescriptor(descriptor, nameof(descriptor.Name));

            // Use descriptor.VaultUri if set, otherwise use the store's EndpointUri
            Uri vaultUri = !string.IsNullOrWhiteSpace(descriptor.VaultUri)
                ? new Uri(descriptor.VaultUri)
                : ((DependencyKeyVaultStore)this.StoreDescription).EndpointUri;

            string certName = descriptor.Name;
            CertificateClient client = this.CreateCertificateClient(vaultUri, ((DependencyKeyVaultStore)this.StoreDescription).Credentials);

            try
            {
                return await (retryPolicy ?? KeyVaultManager.DefaultRetryPolicy).ExecuteAsync(async () =>
                {
                    KeyVaultCertificateWithPolicy cert = await client.GetCertificateAsync(certName, cancellationToken: cancellationToken);
                    KeyVaultDescriptor result = new KeyVaultDescriptor(descriptor)
                    {
                        ObjectType = KeyVaultObjectType.Certificate,
                        Name = certName,
                        VaultUri = vaultUri.ToString(),
                        Version = cert.Properties.Version,
                        ObjectId = cert.Id.ToString(),
                        Policy = cert.Policy?.ToString()
                    };
                    return result;
                }).ConfigureAwait(false);
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
        /// <param name="descriptor">The dependency descriptor to validate.</param>
        /// <param name="requiredProperties">The required property names.</param>
        /// <exception cref="DependencyException">
        /// Thrown if any required property is missing or empty.
        /// </exception>
        private static void ValidateDescriptor(DependencyDescriptor descriptor, params string[] requiredProperties)
        {
            descriptor.ThrowIfNull(nameof(descriptor));
            foreach (string property in requiredProperties)
            {
                if (!descriptor.ContainsKey(property) || string.IsNullOrWhiteSpace(descriptor[property]?.ToString()))
                {
                    throw new DependencyException(
                        $"The required property '{property}' is missing or empty in the dependency descriptor.",
                        ErrorReason.DependencyDescriptionInvalid);
                }
            }
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
