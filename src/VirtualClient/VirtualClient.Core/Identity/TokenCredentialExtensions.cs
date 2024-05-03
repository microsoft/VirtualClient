namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;
    using global::Azure.Core;
    using global::Azure.Identity;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods to get token credentials
    /// </summary>
    public static class TokenCredentialExtensions
    {
        /// <summary>
        /// Generate TokenCredential from the AadPrincipalSettings in configuration.
        /// </summary>
        /// <param name="tenantId">Tenant Id.</param>
        /// <param name="principalClientId">Principle client id, ignored if managed identity is provided..</param>
        /// <param name="managedIdentityClientId">The client id of the managed Identity.</param>
        /// <param name="certificateThumbprint">Certificate thumbprint, not used if managed identity is provided.</param>
        /// <param name="certificateIssuer">Certificate issuer, not used if managed identity is provided.</param>
        /// <param name="certificateSubject">Certificate subjectName, not used if managed identity is provided.</param>
        /// <param name="certManager">Certificate manager.</param>
        /// <returns></returns>
        public static async Task<TokenCredential> GetTokenCredentialAsync(string tenantId, string principalClientId, string managedIdentityClientId, string certificateThumbprint, string certificateIssuer = null, string certificateSubject = null, ICertificateManager certManager = null)
        {
            tenantId.ThrowIfNull(nameof(tenantId));
            TokenCredential credential;
            certManager = certManager ?? new CertificateManager();
            X509Certificate2 cert;

            // Default to Managed Identity
            if (!string.IsNullOrWhiteSpace(managedIdentityClientId))
            {
                credential = new ManagedIdentityCredential(managedIdentityClientId);
            }

            // Otherwise fetch credentials by subject and issuer
            else if (!string.IsNullOrEmpty(certificateIssuer) && !string.IsNullOrEmpty(certificateSubject))
            {
                cert = await certManager.GetCertificateFromStoreAsync(certificateIssuer, certificateSubject).ConfigureAwait(false);
                if (!cert.HasPrivateKey)
                {
                    string errorMessage = $"Certificate '{cert.Subject}' with issuer '{cert.Issuer}' and subject name '{cert.SubjectName.Name}' doesn't have private key to get authentication token.";
                    throw new AuthenticationException(errorMessage);
                }

                credential = new ClientCertificateCredential(
                    tenantId,
                    principalClientId,
                    cert);
            }

            // If that fails, fetch the token by certificate thumbprint.
            else
            {
                cert = await certManager.GetCertificateFromStoreAsync(certificateThumbprint).ConfigureAwait(false);
                if (!cert.HasPrivateKey)
                {
                    string errorMessage = $"Certificate '{cert.Subject}' with thumbprint '{cert.Thumbprint}' doesn't have private key to get authentication token.";
                    throw new AuthenticationException(errorMessage);
                }

                credential = new ClientCertificateCredential(
                    tenantId,
                    principalClientId,
                    cert);
            }

            return credential;
        }
    }
}
