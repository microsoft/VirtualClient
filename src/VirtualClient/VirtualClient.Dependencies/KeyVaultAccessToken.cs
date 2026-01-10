// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Virtual Client component that acquires an Azure access token for the specified Key Vault
    /// using interactive browser authentication with a device-code fallback.
    /// </summary>
    public class KeyVaultAccessToken : VirtualClientComponent
    {
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultAccessToken"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">Parameters to the Virtual Client component.</param>
        public KeyVaultAccessToken(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.fileSystem.ThrowIfNull(nameof(this.fileSystem));
        }

        /// <summary>
        /// Gets the Azure Key Vault URI for which the access token will be requested.
        /// Example: https://anyvault.vault.azure.net/
        /// </summary>
        protected Uri KeyVaultUri { get; set; }

        /// <summary>
        /// Gets the Azure tenant ID used to acquire an access token.
        /// </summary>
        protected string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the full file path where the acquired access token will be written when file logging is enabled.
        /// This is resolved during <see cref="InitializeAsync(EventContext, CancellationToken)"/> when
        /// <see cref="VirtualClientComponent.LogFileName"/> is provided.
        /// </summary>
        protected string AccessTokenPath { get; set; }

        /// <summary>
        /// Resolves the access token output file path
        /// and removes any existing token file so the current run produces a fresh token output.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(this.LogFileName))
            {
                string directory = !string.IsNullOrWhiteSpace(this.LogFolderName)
                    ? this.LogFolderName
                    : this.fileSystem.Directory.GetCurrentDirectory();

                this.AccessTokenPath = this.Combine(directory, this.LogFileName);

                if (this.fileSystem.File.Exists(this.AccessTokenPath))
                {
                    await this.fileSystem.File.DeleteAsync(this.AccessTokenPath);
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
            this.KeyVaultUri = new Uri(this.Parameters.GetValue<string>(nameof(this.KeyVaultUri)));
            this.KeyVaultUri.ThrowIfNull(nameof(this.KeyVaultUri));

            this.TenantId = this.Parameters.GetValue<string>(nameof(this.TenantId));
            if (string.IsNullOrWhiteSpace(this.TenantId))
            {
                EndpointUtility.TryParseMicrosoftEntraTenantIdReference(this.KeyVaultUri, out string tenant);
                this.TenantId = tenant;
            }

            this.TenantId.ThrowIfNullOrWhiteSpace(nameof(this.TenantId));

            string accessToken = null;
            if (!cancellationToken.IsCancellationRequested)
            {
                TokenRequestContext requestContext = this.GetTokenRequestContext();
                try
                {
                    // Attempt an interactive (browser-based) authentication first. On most Windows environments
                    // this will work and is the most convenient for the user. On many Linux systems, there may
                    // not be a GUI and thus no browser. In that case, we fall back to the device code credential
                    // option in the catch block below.
                    InteractiveBrowserCredential credential = new InteractiveBrowserCredential(
                        new InteractiveBrowserCredentialOptions
                        {
                            TenantId = this.TenantId
                        });

                    accessToken = await this.AcquireInteractiveTokenAsync(credential, requestContext, cancellationToken);
                }
                catch (AuthenticationFailedException exc) when (exc.Message.Contains("Unable to open a web page"))
                {
                    // Browser-based authentication is unavailable; switch to device code flow and present
                    // the user with a code and URL to complete authentication from another device.
                    DeviceCodeCredential credential = new DeviceCodeCredential(new DeviceCodeCredentialOptions
                    {
                        TenantId = this.TenantId,
                        DeviceCodeCallback = (codeInfo, token) =>
                        {
                            Console.WriteLine(string.Empty);
                            Console.WriteLine("Browser-based authentication unavailable (e.g. no GUI). Using device/code option.");
                            Console.WriteLine(string.Empty);
                            Console.WriteLine("********************** Azure Key Vault Authorization **********************");
                            Console.WriteLine(string.Empty);
                            Console.WriteLine(codeInfo.Message);
                            Console.WriteLine(string.Empty);
                            Console.WriteLine("***************************************************************************");
                            Console.WriteLine(string.Empty);

                            return Task.CompletedTask;
                        }
                    });

                    accessToken = await this.AcquireDeviceCodeTokenAsync(credential, requestContext, cancellationToken);
                }

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    throw new AuthenticationFailedException("Authentication failed. No access token could be obtained.");
                }

                if (!string.IsNullOrEmpty(this.AccessTokenPath))
                {
                    using (FileSystemStream fileStream = this.fileSystem.FileStream.New(
                        this.AccessTokenPath,
                        FileMode.Create,
                        FileAccess.ReadWrite,
                        FileShare.ReadWrite))
                    {
                        byte[] bytedata = Encoding.Default.GetBytes(accessToken);
                        fileStream.Write(bytedata, 0, bytedata.Length);
                        await fileStream.FlushAsync().ConfigureAwait(false);
                        this.Logger.LogTraceMessage($"Access token saved to file: {this.AccessTokenPath}");
                    }
                }

                Console.WriteLine("[Access Token]:");
                Console.WriteLine(accessToken);
            }
        }

        /// <summary>
        /// Acquires an access token using interactive browser authentication.
        /// </summary>
        /// <param name="credential">The interactive browser credential to use.</param>
        /// <param name="requestContext">The request context containing the required scopes.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The access token string.</returns>
        protected virtual async Task<string> AcquireInteractiveTokenAsync(
            TokenCredential credential,
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            AccessToken response = await credential.GetTokenAsync(requestContext, cancellationToken);
            return response.Token;
        }

        /// <summary>
        /// Acquires an access token using device-code authentication.
        /// This is used as a fallback when interactive browser authentication is unavailable.
        /// </summary>
        /// <param name="credential">The device code credential to use.</param>
        /// <param name="requestContext">The request context containing the required scopes.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The access token string.</returns>
        protected virtual async Task<string> AcquireDeviceCodeTokenAsync(
            TokenCredential credential,
            TokenRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            AccessToken response = await credential.GetTokenAsync(requestContext, cancellationToken);
            return response.Token;
        }

        /// <summary>
        /// Creates the <see cref="TokenRequestContext"/> used to request an access token for the target Key Vault resource.
        /// Uses the Key Vault resource scope: "{KeyVaultUri}/.default".
        /// </summary>
        /// <returns>The token request context containing the required scopes.</returns>
        protected virtual TokenRequestContext GetTokenRequestContext()
        {
            string[] installerTenantResourceScopes = new string[]
            {
                new Uri(baseUri: this.KeyVaultUri, relativeUri: ".default").ToString(),
                // Example of a specific scope:
                // "api://56e7ee83-1cf6-4048-a664-c2a08955f825/user_impersonation"
            };

            return new TokenRequestContext(scopes: installerTenantResourceScopes);
        }
    }
}