// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command that executes a profile to acquire an access token for an Azure Key Vault.
    /// </summary>
    internal class GetAccessTokenCommand : ExecuteProfileCommand
    {
        /// <summary>
        /// Key vault initialization is not required for getting an access token.
        /// </summary>
        protected override bool ShouldInitializeKeyVault => false;

        /// <summary>
        /// Executes the access token acquisition operations using the configured profile.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public override Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
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

            this.Parameters["KeyVaultUri"] = this.KeyVault;    
            
            return base.ExecuteAsync(args, cancellationTokenSource);
        }
    }
}