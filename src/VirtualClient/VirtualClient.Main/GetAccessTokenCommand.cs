// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using Azure.Storage.Blobs;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Identity.Client.Platforms.Features.DesktopOs.Kerberos;
    using Newtonsoft.Json;
    using Polly;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;
    using VirtualClient.Contracts.Validation;
    using VirtualClient.Logging;
    using VirtualClient.Metadata;

    /// <summary>
    /// Command that executes a profile to acquire an access token for an Azure Key Vault.
    /// </summary>
    internal class GetAccessTokenCommand : ExecuteProfileCommand
    {
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

            IServiceCollection dependencies = this.InitializeDependencies(args);
            this.Parameters = this.ResolveParameters();

            return base.ExecuteAsync(args, cancellationTokenSource);
        }

        private Dictionary<string, IConvertible> ResolveParameters()
        {
            ////IKeyVaultManager keyVaultManager = dependencies.GetService<IKeyVaultManager>();
            ////var a = keyVaultManager.StoreDescription;

            var parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);

            if(string.IsNullOrWhiteSpace(this.KeyVault))
            {
                Uri baseUri = new Uri(this.KeyVault);
                var store = new DependencyKeyVaultStore("KeyVault", baseUri);
            }

            return parameters;
        }
    }
}
