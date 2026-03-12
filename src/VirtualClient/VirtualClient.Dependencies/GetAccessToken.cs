// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Identity;

    /// <summary>
    /// Component that acquires an Azure access token for the specified Key Vault
    /// using interactive browser authentication with a device-code fallback.
    /// </summary>
    public class GetAccessToken : VirtualClientComponent
    {
        private IAuthorizationManager authorizationManager;
        private IKeyVaultManager keyVaultManager;
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetAccessToken"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">Parameters to the Virtual Client component.</param>
        public GetAccessToken(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.authorizationManager = dependencies.GetService<IAuthorizationManager>();
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.keyVaultManager = dependencies.GetService<IKeyVaultManager>();
        }

        /// <summary>
        /// Gets or sets the full file path where the acquired access token will be written when file logging is enabled.
        /// This is resolved during <see cref="InitializeAsync(EventContext, CancellationToken)"/> when
        /// <see cref="VirtualClientComponent.LogFileName"/> is provided.
        /// </summary>
        public string FilePath
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.FilePath), out IConvertible filePath);
                return filePath?.ToString();
            }
        }

        /// <summary>
        /// Gets the Azure tenant ID used to acquire an access token.
        /// </summary>
        public string TenantId
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.TenantId));
            }
        }

        /// <summary>
        /// Resolves the access token output file path
        /// and removes any existing token file so the current run produces a fresh token output.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(this.FilePath))
            {
                if (this.fileSystem.File.Exists(this.FilePath))
                {
                    await this.fileSystem.File.DeleteAsync(this.FilePath);
                }
            }
        }

        /// <summary>
        /// Acquires an access token for the configured Key Vault URI using Azure Identity.
        /// The component attempts interactive browser authentication first and falls back to
        /// device-code authentication when a browser is not available (e.g. headless Linux).
        /// The token is always written to standard output. Token is also written to a file if AccessTokenPath is resolved.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string accessToken = null;
            if (!cancellationToken.IsCancellationRequested)
            {
                accessToken = await this.authorizationManager.GetAccessTokenAsync(
                    new Uri(this.keyVaultManager.StoreDescription.ToString()), 
                    this.TenantId, 
                    cancellationToken);

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    throw new AuthenticationFailedException("Authentication failed. No access token could be obtained.");
                }

                if (!string.IsNullOrEmpty(this.FilePath))
                {
                    string directory = Path.GetDirectoryName(this.FilePath);
                    if (!this.fileSystem.Directory.Exists(directory))
                    {
                        this.fileSystem.Directory.CreateDirectory(directory);
                    }

                    await this.fileSystem.File.WriteAllTextAsync(this.FilePath, accessToken, cancellationToken);
                }
                else
                {
                    // Output to standard output if a target file path is not provided.
                    Console.WriteLine();
                    Console.WriteLine("[Access Token]:");
                    Console.WriteLine(accessToken);
                    Console.WriteLine();
                }
            }
        }
    }
}