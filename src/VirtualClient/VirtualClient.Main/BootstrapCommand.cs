// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command executes bootstrap operations including package installation and certificate installation.
    /// </summary>
    internal class BootstrapCommand : ExecuteProfileCommand
    {
        /// <summary>
        /// When true, Key Vault will be initialized using default Azure authentication.
        /// Returns true when both --tenant-id and --access-token are NOT provided (use default authentication).
        /// Returns false when either is provided (use token authentication).
        /// </summary>
        protected override bool ShouldInitializeKeyVault => 
            string.IsNullOrWhiteSpace(this.AccessToken) && string.IsNullOrWhiteSpace(this.TenantId);

        /// <summary>
        /// The name of the certificate to install from Key Vault.
        /// Optional - if not provided, only package installation will occur.
        /// </summary>
        public string CertificateName { get; set; }

        /// <summary>
        /// Optional access token for Key Vault authentication when installing certificates.
        /// When not provided, uses default Azure credential authentication (Azure CLI, Managed Identity, etc.).
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// The name (logical name) to use when registering the package.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The name of the package (in storage) to bootstrap/install.
        /// </summary>
        public string PackageName { get; set; }

        /// <summary>
        /// The tenant ID for the Azure subscription.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Executes the bootstrap command.
        /// Supports:
        /// - Package installation from remote store (requires --packages, optionally --package for specific package name)
        /// - Certificate installation (requires --cert-name and --key-vault)
        /// - Both certificate and package installation (--cert-name --key-vault --packages)
        /// </summary>
        public override Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            if (this.Parameters == null)
            {
                this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            }

            this.Timeout = ProfileTiming.OneIteration();
            List<DependencyProfileReference> dependencyProfiles = new List<DependencyProfileReference>();

            if (string.IsNullOrWhiteSpace(this.PackageName) && string.IsNullOrWhiteSpace(this.CertificateName))
            {
                throw new ArgumentException(
                    "At least one operation must be specified. Use --package for package installation from remote store " +
                    "or --cert-name for certificate installation.");
            }

            // Scenario 1: Certificate installation only OR Certificate + Package installation
            if (!string.IsNullOrWhiteSpace(this.CertificateName))
            {
                // If Access Token is not provided and Tenant ID is provided, bootstrap will get token using browser-based (or device code flow) authentication to retrieve token.
                if (!string.IsNullOrWhiteSpace(this.AccessToken))
                {
                    this.Parameters["AccessToken"] = this.AccessToken;
                }
                else if (!string.IsNullOrWhiteSpace(this.TenantId))
                {
                    this.SetupAccessToken();
                    dependencyProfiles.Add(new DependencyProfileReference("GET-ACCESS-TOKEN.json"));
                }

                this.SetupCertificateInstallation();
                dependencyProfiles.Add(new DependencyProfileReference("BOOTSTRAP-CERTIFICATE.json"));
            }

            // Scenario 2: Package installation (can be standalone or after certificate)
            if (!string.IsNullOrWhiteSpace(this.PackageName))
            {
                this.SetupPackageInstallation();
                dependencyProfiles.Add(new DependencyProfileReference("BOOTSTRAP-PACKAGE.json"));
            }

            this.Profiles = dependencyProfiles;
            return base.ExecuteAsync(args, cancellationTokenSource);
        }

        protected void SetupAccessToken()
        {
            if (string.IsNullOrWhiteSpace(this.KeyVault))
            {
                throw new ArgumentException(
                    "The Key Vault URI must be provided (--key-vault) when getting access token for certificate installation.");
            }

            if(string.IsNullOrWhiteSpace(this.TenantId))
            {
                throw new ArgumentException(
                    "Tenant ID must be provided (--tenant-id) when getting access token for certificate installation.");
            }

            this.Parameters["KeyVaultUri"] = this.KeyVault;
            this.Parameters["TenantId"] = this.TenantId;
            this.Parameters["LogFileName"] = "AccessToken.txt";
        }

        protected void SetupCertificateInstallation()
        {
            if (string.IsNullOrWhiteSpace(this.CertificateName))
            {
                throw new ArgumentException(
                    "The certificate name must be provided (--cert-name) when installing certificates.");
            }

            if (string.IsNullOrWhiteSpace(this.KeyVault))
            {
                throw new ArgumentException(
                    "The Key Vault URI must be provided (--key-vault) when installing certificates (--cert-name).");
            }

            // Set certificate-related parameters
            this.Parameters["KeyVaultUri"] = this.KeyVault;
            this.Parameters["CertificateName"] = this.CertificateName;
        }

        protected void SetupPackageInstallation()
        {
            if (string.IsNullOrWhiteSpace(this.PackageName))
            {
                throw new ArgumentException(
                    "The package name must be provided (--package) when installing packages.");
            }

            string registerAsName = this.Name;
            if (string.IsNullOrWhiteSpace(registerAsName))
            {
                registerAsName = Path.GetFileNameWithoutExtension(this.PackageName);
            }

            this.Parameters["Package"] = this.PackageName;
            this.Parameters["RegisterAsName"] = registerAsName;
        }
    }
}
