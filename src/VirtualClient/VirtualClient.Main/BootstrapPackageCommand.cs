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
    internal class BootstrapPackageCommand : ExecuteProfileCommand
    {
        /// <summary>
        /// When true, Key Vault will be initialized. This is only needed when using default Azure authentication
        /// (no access token provided).
        /// </summary>
        protected override bool ShouldInitializeKeyVault => string.IsNullOrWhiteSpace(this.AccessToken);

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
        /// Executes the bootstrap command.
        /// Supports:
        /// - Package installation from remote store (requires --packages, optionally --package for specific package name)
        /// - Certificate installation (requires --cert-name and --key-vault)
        /// - Both certificate and package installation (--cert-name --key-vault --packages)
        /// </summary>
        public override Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            // Validate that at least one operation is requested
            this.ValidateParameters();

            this.Timeout = ProfileTiming.OneIteration();
            this.Profiles = new List<DependencyProfileReference>
            {
                new DependencyProfileReference("BOOTSTRAP-DEPENDENCIES.json")
            };

            var scenariosToExecute = new List<string>();

            // Scenario 1: Certificate installation only OR Certificate + Package installation
            if (!string.IsNullOrWhiteSpace(this.CertificateName))
            {
                scenariosToExecute.Add("InstallCertificate");
                this.SetupCertificateInstallation();
            }

            // Scenario 2: Package installation (can be standalone or after certificate)
            if (!string.IsNullOrWhiteSpace(this.PackageName))
            {
                scenariosToExecute.Add("InstallDependencies");
                this.SetupPackageInstallation();
            }

            this.Scenarios = scenariosToExecute;
            return base.ExecuteAsync(args, cancellationTokenSource);
        }

        protected void ValidateParameters()
        {
            if (this.Parameters == null)
            {
                this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            }

            // At least one operation must be specified
            if (string.IsNullOrWhiteSpace(this.PackageName) && string.IsNullOrWhiteSpace(this.CertificateName))
            {
                throw new ArgumentException(
                    "At least one operation must be specified. Use --package for package installation from remote store " +
                    "or --cert-name for certificate installation.");
            }

            // If certificate installation is requested, KeyVault URI is required
            if (!string.IsNullOrWhiteSpace(this.CertificateName) && string.IsNullOrWhiteSpace(this.KeyVault))
            {
                throw new ArgumentException(
                    "The Key Vault URI must be provided (--key-vault) when installing certificates (--cert-name).");
            }
        }

        protected void SetupCertificateInstallation()
        {
            // Set certificate-related parameters
            this.Parameters["KeyVaultUri"] = this.KeyVault;
            this.Parameters["CertificateName"] = this.CertificateName;

            if (!string.IsNullOrWhiteSpace(this.AccessToken))
            {
                // Token-based authentication - no Key Vault initialization needed
                this.Parameters["AccessToken"] = this.AccessToken;
            }
        }

        protected void SetupPackageInstallation()
        {
            string registerAsName = this.Name;
            if (String.IsNullOrWhiteSpace(registerAsName))
            {
                registerAsName = Path.GetFileNameWithoutExtension(this.PackageName);
            }

            this.Parameters["Package"] = this.PackageName;
            this.Parameters["RegisterAsName"] = registerAsName;
        }
    }
}
