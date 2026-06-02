// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using Azure.Core;
    using Azure.Identity;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;
    using VirtualClient.Identity;

    /// <summary>
    /// Provides methos for creating credentials/tokens for authentication with network/internet endpoints
    /// (e.g. Microsoft Entra).
    /// </summary>
    public static class CredentialFactory
    {
        /// <summary>
        /// Creates a token credential for authentication with Azure services using a user-assigned managed identity.
        /// </summary>
        /// <param name="clientId">The client ID of the user-assigned managed identity.</param>
        /// <returns>A <see cref="TokenCredential"/> instance for authenticating with the specified managed identity.</returns>
        public static TokenCredential CreateManagedIdentityTokenCredential(string clientId)
        {
            return new ManagedIdentityCredential(clientId);
        }

        /// <summary>
        /// Creates a token credential using a certificate specified by issuer and subject name for authentication against a
        /// Microsoft Entra App/ID.
        /// </summary>
        /// <param name="certificateManager">Reads certificates from the system.</param>
        /// <param name="clientId">The Microsoft Entra application client ID.</param>
        /// <param name="tenantId">The ID of the Microsoft Entra tenant.</param>
        /// <param name="certificateThumbprint">The certificate thumbprint.</param>
        /// <returns>A <see cref="TokenCredential"/> instance for authenticating with the specified certificate.</returns>
        public static TokenCredential CreateCertificateTokenCredential(ICertificateManager certificateManager, string clientId, string tenantId, string certificateThumbprint)
        {
            certificateManager.ThrowIfNull(nameof(certificateManager));
            clientId.ThrowIfNullOrWhiteSpace(nameof(clientId));
            tenantId.ThrowIfNullOrWhiteSpace(nameof(tenantId));
            certificateThumbprint.ThrowIfNullOrWhiteSpace(nameof(certificateThumbprint));

            // Always search CurrentUser/My store first.
            PlatformID platform = Environment.OSVersion.Platform;
            StoreName storeName = StoreName.My;
            List<StoreLocation> storeLocations = new List<StoreLocation>
            {
                StoreLocation.CurrentUser
            };

            if (platform == PlatformID.Win32NT)
            {
                // There is no local machine store on Unix/Linux systems. This store is available on
                // Windows only.
                storeLocations.Add(StoreLocation.LocalMachine);
            }

            ClientCertificateCredentialOptions credentialOptions = new ClientCertificateCredentialOptions
            {
                // Required to support integration with Microsoft Entra ID/Apps for certificate subject names defined
                // in the "trustedCertificateSubjects" section of the Microsoft Entra ID/App manifest.
                SendCertificateChain = true
            };

            X509Certificate2 certificate = null;

            if (platform == PlatformID.Unix)
            {
                string currentUser = Environment.UserName;

                try
                {
                    certificate = certificateManager.GetCertificateFromStoreAsync(
                        certificateThumbprint,
                        storeLocations,
                        storeName).GetAwaiter().GetResult();
                }
                catch (CryptographicException) when (currentUser?.ToLowerInvariant() == "root")
                {
                    // Backup:
                    // We are likely running as sudo/root. The .NET SDK will
                    // look for the certificate in the location specific to 'root'
                    // by default. We want to try the current user location as well.
                    PlatformSpecifics platformSpecifics = new PlatformSpecifics(
                        Environment.OSVersion.Platform,
                        RuntimeInformation.ProcessArchitecture);

                    currentUser = platformSpecifics.GetLoggedInUser();

                    certificate = certificateManager.GetCertificateFromPathAsync(
                        certificateThumbprint,
                        string.Format(CertificateManager.DefaultUnixCertificateDirectory, currentUser)).GetAwaiter().GetResult();
                }
            }
            else
            {
                certificate = certificateManager.GetCertificateFromStoreAsync(certificateThumbprint, storeLocations, storeName)
                    .GetAwaiter().GetResult();
            }

            return new ClientCertificateCredential(tenantId, clientId, certificate, credentialOptions);
        }

        /// <summary>
        /// Creates a token credential using a certificate specified by issuer and subject name for authentication against a
        /// Microsoft Entra App/ID.
        /// </summary>
        /// <param name="certificateManager">Reads certificates from the system.</param>
        /// <param name="clientId">The Microsoft Entra application client ID.</param>
        /// <param name="tenantId">The ID of the Microsoft Entra tenant.</param>
        /// <param name="certificateIssuer">The certificate issuer. This can be a full issuer string or a portion of it.</param>
        /// <param name="certificateSubject">The certificate subject name. This can be a full subject name or a portion of it.</param>
        /// <returns>A <see cref="TokenCredential"/> instance for authenticating with the specified certificate.</returns>
        public static TokenCredential CreateCertificateTokenCredential(ICertificateManager certificateManager, string clientId, string tenantId, string certificateIssuer, string certificateSubject)
        {
            certificateManager.ThrowIfNull(nameof(certificateManager));
            clientId.ThrowIfNullOrWhiteSpace(nameof(clientId));
            tenantId.ThrowIfNullOrWhiteSpace(nameof(tenantId));
            certificateIssuer.ThrowIfNullOrWhiteSpace(nameof(certificateIssuer));
            certificateSubject.ThrowIfNullOrWhiteSpace(nameof(certificateSubject));

            // Always search CurrentUser/My store first.
            PlatformID platform = Environment.OSVersion.Platform;
            StoreName storeName = StoreName.My;
            List<StoreLocation> storeLocations = new List<StoreLocation>
            {
                StoreLocation.CurrentUser
            };

            if (platform == PlatformID.Win32NT)
            {
                // There is no local machine store on Unix/Linux systems. This store is available on
                // Windows only.
                storeLocations.Add(StoreLocation.LocalMachine);
            }

            ClientCertificateCredentialOptions credentialOptions = new ClientCertificateCredentialOptions
            {
                // Required to support integration with Microsoft Entra ID/Apps for certificate subject names defined
                // in the "trustedCertificateSubjects" section of the Microsoft Entra ID/App manifest.
                SendCertificateChain = true
            };

            X509Certificate2 certificate = null;

            if (platform == PlatformID.Unix)
            {
                string currentUser = Environment.UserName;

                try
                {
                    certificate = certificateManager.GetCertificateFromStoreAsync(
                        certificateIssuer,
                        certificateSubject,
                        storeLocations,
                        storeName).GetAwaiter().GetResult();
                }
                catch (CryptographicException) when (currentUser?.ToLowerInvariant() == "root")
                {
                    // Backup:
                    // We are likely running as sudo/root. The .NET SDK will
                    // look for the certificate in the location specific to 'root'
                    // by default. We want to try the current user location as well.
                    PlatformSpecifics platformSpecifics = new PlatformSpecifics(
                        Environment.OSVersion.Platform,
                        RuntimeInformation.ProcessArchitecture);

                    currentUser = platformSpecifics.GetLoggedInUser();

                    certificate = certificateManager.GetCertificateFromPathAsync(
                        certificateIssuer,
                        certificateSubject,
                        string.Format(CertificateManager.DefaultUnixCertificateDirectory, currentUser)).GetAwaiter().GetResult();
                }
            }
            else
            {
                certificate = certificateManager.GetCertificateFromStoreAsync(
                    certificateIssuer,
                    certificateSubject,
                    storeLocations,
                    storeName).GetAwaiter().GetResult();
            }

            return new ClientCertificateCredential(tenantId, clientId, certificate, credentialOptions);
        }
    }
}
