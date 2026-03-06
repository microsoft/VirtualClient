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
        /// Defines the name of the certificate to install from a Key Vault.
        /// </summary>
        public string CertificateName { get; set; }

        /// <summary>
        /// Defines an access token for Key Vault authentication when installing certificates.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// The name (logical name) to use when registering the package.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// A path to a file to which the certificate should be written.
        /// </summary>
        public string OutputFilePath { get; set; }

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
            this.Validate();
            this.Initialize();

            return base.ExecuteAsync(args, cancellationTokenSource);
            
        }

        protected void Initialize()
        {
            if (this.Parameters == null)
            {
                this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            }

            this.Timeout = ProfileTiming.OneIteration();
            List<DependencyProfileReference> dependencyProfiles = new List<DependencyProfileReference>();

            // Scenario 1: Certificate installation only OR Certificate + Package installation
            if (!string.IsNullOrWhiteSpace(this.CertificateName))
            {
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
        }

        protected void SetupCertificateInstallation()
        {
            // If Access Token is not provided and Tenant ID is provided, bootstrap will get token using browser-based
            // (or device code flow) authentication to retrieve token.
            this.Parameters["CertificateName"] = this.CertificateName;
            this.Parameters["FilePath"] = this.OutputFilePath;
            this.Parameters["AccessToken"] = this.AccessToken;
            this.Parameters["TenantId"] = this.TenantId;
        }

        protected void SetupPackageInstallation()
        {
            string registerAsName = this.Name;
            if (string.IsNullOrWhiteSpace(registerAsName))
            {
                registerAsName = Path.GetFileNameWithoutExtension(this.PackageName);
            }

            this.Parameters["Package"] = this.PackageName;
            this.Parameters["RegisterAsName"] = registerAsName;
        }

        protected void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.PackageName) && string.IsNullOrWhiteSpace(this.CertificateName))
            {
                throw new ArgumentException(
                    "At least one operation must be specified. Use --package for package installation from remote store " +
                    "or --cert-name for certificate installation.");
            }

            if (!string.IsNullOrWhiteSpace(this.CertificateName))
            {
                if (this.KeyVaultStore == null)
                {
                    throw new ArgumentException(
                        "A Key Vault URI must be provided on the command line (--key-vault) to install a certificate.");
                }

                // The user may have defined the authentication information in the Key Vault connection definition. For
                // these cases, we are using a pre-existing certificate (on the system) in order to download a certificate.
                // The tenant ID is not required for this case.
                //
                // e.g.
                // --key-vault="https://any.vault.azure.net?cid=8cdebecc...&tid=42005d4d...&crti=ANY&crts=any.corp.azure.com"
                if (string.IsNullOrWhiteSpace(this.AccessToken) 
                    && string.IsNullOrWhiteSpace(this.TenantId) 
                    && (this.KeyVaultStore as DependencyKeyVaultStore)?.Credentials == null)
                {
                    throw new ArgumentException(
                        "The Azure tenant ID must be provided on the command line (--tenant-id) to install a certificate.");
                }
            }

            if (!string.IsNullOrWhiteSpace(this.PackageName) && this.PackageStore == null)
            {
                throw new ArgumentException(
                    "A package store must be provided on the command line (--package-store) when installing packages.");
            }
        }
    }
}
