// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Security.Authentication;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Azure.Core;
    using global::Azure.Identity;
    using VirtualClient;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides a cache to hold access tokens for a given Azure credential.
    /// Use the following documentation as reference:
    /// <list type="bullet">
    /// <item>
    /// <a href='https://github.com/Azure/azure-sdk-for-net/blob/Azure.Identity_1.4.1/sdk/identity/Azure.Identity/README.md'>Azure.Identity README</a>
    /// </item>
    /// <item>
    /// <a href='https://docs.microsoft.com/en-us/dotnet/api/azure.identity?view=azure-dotnet'>Azure.Identity Namespace</a>
    /// </item>
    /// <item>
    /// <a href='https://www.nuget.org/packages/Azure.Identity/'>Azure.Identity NuGet Package(s)</a>
    /// </item>
    /// <item>
    /// <a href='https://docs.microsoft.com/en-us/azure/app-service/overview-managed-identity?tabs=dotnet'>Using Managed Identity</a>
    /// </item>
    /// </list>
    /// </summary>
    public class TokenCredentialCache : TokenCredential
    {
        /// <summary>
        /// The default context to use for token requests.
        /// </summary>
        public static readonly TokenRequestContext DefaultContext = new TokenRequestContext(new string[] { TokenCredentialCache.DefaultScope });

        private const string DefaultScope = "https://graph.microsoft.com/.default";

        /// <summary>
        /// Creates a new instance of the <see cref="TokenCredentialCache"/> class.
        /// </summary>
        /// <param name="credential">
        /// The credential provider that will be used to get access tokens.
        /// </param>
        /// <param name="autoRefreshBuffer">
        /// A period of time before the AAD token provider cached token expires at which
        /// it should be automatically refreshed.
        /// </param>
        public TokenCredentialCache(TokenCredential credential, TimeSpan? autoRefreshBuffer = null)
        {
            credential.ThrowIfNull(nameof(credential));

            this.Credential = credential;
            this.AutoRefreshBuffer = autoRefreshBuffer;
        }

        /// <summary>
        /// A period of time before the AAD token provider cached token expires at which
        /// it should be automatically refreshed.
        /// </summary>
        public TimeSpan? AutoRefreshBuffer { get; }

        /// <summary>
        /// Used to cache/save a token for repeated use in authenticating with an Azure
        /// resource (e.g. repeat WebApp API calls).
        /// </summary>
        public AccessToken CachedToken { get; set; }

        /// <summary>
        /// The credential for which the access tokens are being cached.
        /// </summary>
        public TokenCredential Credential { get; }

        /// <summary>
        /// Factory method creates a <see cref="TokenCredentialCache"/> where the underlying credential uses a
        /// certificate as proof of identity that can be used to get access tokens for a given client application.
        /// </summary>
        /// <param name="tenantId">The ID of the Azure tenant.</param>
        /// <param name="clientId">The ID of the client application for which the access token will be requested.</param>
        /// <param name="certificateThumbprint">The thumbprint of the certificate to read from the local system.</param>
        /// <param name="certificateIssuer">The issuer of the certificate to read from the local system.</param>
        /// <param name="certificateSubject">The subject name of the certificate to read from the local system.</param>
        /// <param name="certificateManager">Provides features required to read certificates from the local system/store.</param>
        public static TokenCredential CreateClientCertificateCredential(string tenantId, string clientId, string certificateThumbprint, string certificateIssuer = null, string certificateSubject = null, ICertificateManager certificateManager = null)
        {
            X509Certificate2 cert;
            if (!string.IsNullOrEmpty(certificateIssuer) && !string.IsNullOrEmpty(certificateSubject))
            {
                cert = (certificateManager ?? new CertificateManager()).GetCertificateFromStoreAsync(certificateIssuer, certificateSubject)
                .GetAwaiter().GetResult();
            }
            else
            {
                cert = (certificateManager ?? new CertificateManager()).GetCertificateFromStoreAsync(certificateThumbprint)
                    .GetAwaiter().GetResult();
            }

            TokenCredentialCache.ValidateCertificate(cert);

            return new ClientCertificateCredential(tenantId, clientId, cert);
        }

        /// <summary>
        /// Factory method creates a <see cref="TokenCredential"/> using a managed identity that can be used to get 
        /// access tokens for a given client application.
        /// </summary>
        /// <param name="managedIdentityClientId">The ID of the managed identity for which the access token will be requested.</param>
        public static TokenCredential CreateManagedIdentityCredential(string managedIdentityClientId)
        {
            return new ManagedIdentityCredential(managedIdentityClientId);
        }

        /// <summary>
        /// Returns true if the access token is expired or is near expiration within 
        /// the buffer of time defined.
        /// </summary>
        /// <param name="token">The access token.</param>
        /// <param name="buffer">
        /// A period of time before which the access token is set to expire at which it should be considered expired.
        /// </param>
        /// <returns>True if the token is expired or expires within the buffer of time defined.</returns>
        public static bool IsTokenExpired(AccessToken token, TimeSpan? buffer = null)
        {
            bool isExpired = false;
            if (!string.IsNullOrWhiteSpace(token.Token))
            {
                if (buffer == null)
                {
                    isExpired = token.ExpiresOn < DateTime.UtcNow;
                }
                else
                {
                    isExpired = token.ExpiresOn < DateTime.UtcNow + buffer;
                }
            }

            return isExpired;
        }

        /// <summary>
        /// Gets an access token using the underlying credentials.
        /// </summary>
        /// <param name="requestContext">Context information used when getting the access token.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// An access token that can be used to authenticate with Azure resources.
        /// </returns>
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return this.GetTokenAsync(requestContext, cancellationToken).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Gets an access token using the underlying credentials.
        /// </summary>
        /// <param name="requestContext">Context information used when getting the access token.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// An access token that can be used to authenticate with Azure resources.
        /// </returns>
        public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(this.CachedToken.Token) || TokenCredentialCache.IsTokenExpired(this.CachedToken))
            {
                this.CachedToken = await this.Credential.GetTokenAsync(requestContext, cancellationToken)
                    .ConfigureAwait(false);
            }

            return this.CachedToken;
        }

        /// <summary>
        /// Resets the cached access token forcing a refresh on the next attempt to get the
        /// access token for the underlying credential.
        /// </summary>
        public TokenCredentialCache Reset()
        {
            this.CachedToken = new AccessToken(null, DateTimeOffset.UtcNow);
            return this;
        }

        private static void ValidateCertificate(X509Certificate2 cert)
        {
            cert.ThrowIfNull(nameof(cert));
            if (!cert.HasPrivateKey)
            {
                string errorMessage = $"Certificate '{cert.Subject}' with thumbprint '{cert.Thumbprint}' doesn't have private key to get authentication token.";
                throw new AuthenticationException(errorMessage);
            }
        }
    }
}
