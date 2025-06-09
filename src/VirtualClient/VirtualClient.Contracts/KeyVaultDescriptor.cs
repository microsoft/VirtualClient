// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Diagnostics;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents the address location and metadata of a secret, key, or certificate in Azure Key Vault.
    /// </summary>
    /// <remarks>
    /// Semantics:
    /// The address of a Key Vault object is the combination of the following properties:
    /// - The vault URI in which it is stored.
    /// - The name of the secret, key, or certificate.
    /// - The type of the object (Secret, Key, Certificate).
    /// 
    /// Naming Conventions:
    /// https://docs.microsoft.com/en-us/azure/key-vault/general/about-keys-secrets-certificates
    /// </remarks>
    [DebuggerDisplay("{VaultUri}/{ObjectType}/{ObjectName}")]
    public class KeyVaultDescriptor : DependencyDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultDescriptor"/> class.
        /// </summary>
        public KeyVaultDescriptor()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultDescriptor"/> class.
        /// </summary>
        /// <param name="descriptor">The base dependency descriptor to copy from.</param>
        public KeyVaultDescriptor(DependencyDescriptor descriptor)
            : base(descriptor)
        {
        }

        /// <summary>
        /// Gets or sets the type of the Key Vault object (e.g. "Secret", "Key", "Certificate").
        /// </summary>
        public KeyVaultObjectType ObjectType
        {
            get => this.GetValue<KeyVaultObjectType>(nameof(this.ObjectType));
            set => this[nameof(this.ObjectType)] = value;
        }

        /// <summary>
        /// Gets or sets the URI of the Azure Key Vault.
        /// </summary>
        public string VaultUri
        {
            get
        {
                this.TryGetValue(nameof(this.VaultUri), out IConvertible vaultUri);
                return vaultUri?.ToString();
            }
            set => this[nameof(this.VaultUri)] = value;
        }

        /// <summary>
        /// Gets or sets the version of the Key Vault object, if applicable.
        /// </summary>
        public string Version
        {
            get
            {
                this.TryGetValue(nameof(this.Version), out IConvertible version);
                return version?.ToString();
            }
            set => this[nameof(this.Version)] = value;
        }

        /// <summary>
        /// Gets or sets the value of the secret, if applicable.
        /// </summary>
        public string Value
        {
            get
            {
                this.TryGetValue(nameof(this.Value), out IConvertible value);
                return value?.ToString();
            }
            set => this[nameof(this.Value)] = value;
        }

        /// <summary>
        /// Gets or sets the KeyId or CertificateId, if applicable.
        /// </summary>
        public string ObjectId
        {
            get
            {
                this.TryGetValue(nameof(this.ObjectId), out IConvertible id);
                return id?.ToString();
            }
            set => this[nameof(this.ObjectId)] = value;
        }

        /// <summary>
        /// Gets or sets the policy or metadata for the Key Vault object, if applicable.
        /// </summary>
        public string Policy
        {
            get
            {
                this.TryGetValue(nameof(this.Policy), out IConvertible policy);
                return policy?.ToString();
            }
            set => this[nameof(this.Policy)] = value;
        }
    }
}
