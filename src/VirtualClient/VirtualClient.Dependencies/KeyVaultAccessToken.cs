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
    /// Virtual Client component that acquires an access token for an Azure Key Vault
    /// using interactive browser or device-code authentication.
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
        /// The Azure tenant ID used when requesting an access token for the Key Vault.
        /// </summary>
        protected string TenantId
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.TenantId));
            }
        }

        /// <summary>
        /// The Azure Key Vault URI for which an access token will be requested.
        /// Example: https://anyvault.vault.azure.net/
        /// </summary>
        protected string KeyVaultUri
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.KeyVaultUri));
            }
        }

        /// <summary>
        /// The full file path where the acquired access token will be written,
        /// when configured via <see cref="VirtualClientComponent.LogFileName"/> / <see cref="VirtualClientComponent.LogFolderName"/>.
        /// </summary>
        protected string AccessTokenPath { get; set; }

        /// <summary>
        /// Initializes the component for execution, including resolving the access token
        /// output path and removing any existing token file if configured.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(this.LogFileName))
            {
                string directory = !string.IsNullOrWhiteSpace(this.LogFolderName)
                    ? this.LogFolderName
                    : this.fileSystem.Directory.GetCurrentDirectory();

                this.AccessTokenPath = this.fileSystem.Path.GetFullPath(
                    this.fileSystem.Path.Combine(directory, this.LogFileName));

                if (this.fileSystem.File.Exists(this.AccessTokenPath))
                {
                    await this.fileSystem.File.DeleteAsync(this.AccessTokenPath);
                }
            }
        }

        /// <summary>
        /// Acquires an access token for the configured Key Vault URI using Azure Identity.
        /// Attempts interactive browser authentication first and falls back to
        /// device-code authentication when a browser is not available.
        /// The access token can optionally be written to a file and is always
        /// written to the console output.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.KeyVaultUri.ThrowIfNullOrWhiteSpace(nameof(this.KeyVaultUri));
            this.TenantId.ThrowIfNullOrWhiteSpace(nameof(this.TenantId));

            string accessToken = null;
            if (!cancellationToken.IsCancellationRequested)
            {
                string[] installerTenantResourceScopes = new string[]
                {
                    new Uri(baseUri: new Uri(this.KeyVaultUri), relativeUri: ".default").ToString(),
                    // Example of a specific scope:
                    // "api://56e7ee83-1cf6-4048-a664-c2a08955f825/user_impersonation"
                };

                TokenRequestContext requestContext = new TokenRequestContext(scopes: installerTenantResourceScopes);

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

                    AccessToken response = await credential.GetTokenAsync(requestContext, cancellationToken);
                    accessToken = response.Token;
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

                    AccessToken token = await credential.GetTokenAsync(requestContext, cancellationToken);
                    accessToken = token.Token;
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
    }
}