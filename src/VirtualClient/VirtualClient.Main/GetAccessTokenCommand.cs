// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command that executes a profile to acquire an access token for an Azure Key Vault.
    /// </summary>
    internal class GetAccessTokenCommand : ExecuteProfileCommand
    {
        /// <summary>
        /// A path to a file to which the access token should be written.
        /// </summary>
        public string OutputFilePath { get; set; }

        /// <summary>
        /// The tenant ID associated with your Microsoft Entra ID (formerly Azure Active Directory).
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Initializes the command state before execution.
        /// </summary>
        protected override void Initialize(string[] args, PlatformSpecifics platformSpecifics)
        {
            this.Timeout = ProfileTiming.OneIteration();
            this.Profiles = new List<DependencyProfileReference>
            {
                new DependencyProfileReference("GET-ACCESS-TOKEN.json")
            };

            if (this.Parameters == null)
            {
                this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            }

            this.Parameters["FilePath"] = this.OutputFilePath;
            this.Parameters["TenantId"] = this.TenantId;
        }
    }
}