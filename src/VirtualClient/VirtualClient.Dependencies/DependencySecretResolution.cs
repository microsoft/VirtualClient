// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

// REMOVE THIS FILE FROM HERE! WE DONT NEED IT.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Resolves secret values from Azure Key Vault using the injected IKeyVaultManager.
    /// </summary>
    public class DependencySecretResolution : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DependencySecretResolution"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public DependencySecretResolution(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// The list of secret names to resolve from the Key Vault.
        /// </summary>
        public IList<string> SecretNames
        {
            get
            {
                string names = this.Parameters.GetValue<string>(nameof(this.SecretNames));
                return string.IsNullOrWhiteSpace(names)
                    ? new List<string>()
                    : names.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                           .Select(n => n.Trim())
                           .ToList();
            }

            set
            {
                this.Parameters[nameof(this.SecretNames)] = string.Join(";", value);
            }
        }

        /// <summary>
        /// The resolved secrets (name-value pairs).
        /// </summary>
        public IDictionary<string, string> ResolvedSecrets { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Resolves the secrets from Key Vault and stores them in <see cref="ResolvedSecrets"/>.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.SecretNames == null || !this.SecretNames.Any())
            {
                throw new DependencyException(
                    "At least one secret name must be specified to resolve secrets.",
                    ErrorReason.DependencyDescriptionInvalid);
            }

            IKeyVaultManager keyVaultManager = this.Dependencies.GetService<IKeyVaultManager>();
            if (keyVaultManager == null)
            {
                throw new DependencyException(
                    "Key Vault manager is not available. Ensure the dependency is registered.",
                    ErrorReason.DependencyDescriptionInvalid);
            }

            var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (string secretName in this.SecretNames)
            {
                var descriptor = new KeyVaultDescriptor
                {
                    ObjectName = secretName,
                    ObjectType = KeyVaultObjectType.Secret
                    // VaultUri is not set here; the manager uses its injected store.
                };

                KeyVaultDescriptor secret = await keyVaultManager.GetSecretAsync(descriptor, cancellationToken);
                resolved[secretName] = secret.Value;
            }

            this.ResolvedSecrets = resolved;

            // Optionally, add resolved secret names to telemetry context (do not log values in production!)
            telemetryContext.AddContext("resolvedSecrets", string.Join(",", this.ResolvedSecrets.Keys));
        }
    }
}
