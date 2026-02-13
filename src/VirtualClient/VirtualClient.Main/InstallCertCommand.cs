// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command executes certificate installation operations from Azure Key Vault.
    /// </summary>
    internal class InstallCertCommand : ExecuteProfileCommand
    {
        /// <summary>
        /// When true, Key Vault will be initialized. This is only needed when using default Azure authentication
        /// (no access token provided).
        /// </summary>
        protected override bool ShouldInitializeKeyVault => string.IsNullOrWhiteSpace(this.AccessToken);

        /// <summary>
        /// The name of the certificate to install from Key Vault.
        /// </summary>
        public string CertificateName { get; set; }

        /// <summary>
        /// Optional access token for Key Vault authentication.
        /// When not provided, uses default Azure credential authentication (Azure CLI, Managed Identity, etc.).
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Executes the certificate installation command.
        /// </summary>
        public override Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            // Validate required parameters
            this.ValidateParameters();

            this.Timeout = ProfileTiming.OneIteration();
            this.Profiles = new List<DependencyProfileReference>
            {
                new DependencyProfileReference("INSTALL-CERT.json")
            };

            if (this.Parameters == null)
            {
                this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            }

            this.Parameters["KeyVaultUri"] = this.KeyVault;
            this.Parameters["CertificateName"] = this.CertificateName;
            
            if (!string.IsNullOrWhiteSpace(this.AccessToken))
            {
                // Token-based authentication - no Key Vault initialization needed
                this.Parameters["AccessToken"] = this.AccessToken;
            }
   
            return base.ExecuteAsync(args, cancellationTokenSource);
        }

        private void ValidateParameters()
        {
            if (string.IsNullOrWhiteSpace(this.KeyVault))
            {
                throw new ArgumentException("The Key Vault URI must be provided (--key-vault).");
            }

            if (string.IsNullOrWhiteSpace(this.CertificateName))
            {
                throw new ArgumentException("The certificate name must be provided (--cert-name).");
            }
        }
    }
}