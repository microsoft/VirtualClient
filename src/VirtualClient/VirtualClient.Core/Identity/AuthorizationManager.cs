// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Identity
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Core;
    using Azure.Identity;

    /// <summary>
    /// Provides functions for handling authorization requirements on 
    /// Azure subscription resources.
    /// </summary>
    public class AuthorizationManager : IAuthorizationManager
    {
        /// <summary>
        /// Acquires an access token from the target Azure tenant.
        /// </summary>
        /// <param name="resourceUri">The target subscription resource URI (e.g. https://any.vault.azure.net/).</param>
        /// <param name="tenantId">The ID of the Azure tenant in which the target subscription resource exists.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the request operation.</param>
        public async Task<string> GetAccessTokenAsync(Uri resourceUri, string tenantId, CancellationToken cancellationToken)
        {
            string accessToken = null;
            if (!cancellationToken.IsCancellationRequested)
            {
                TokenRequestContext context = this.CreateRequestContext(resourceUri, tenantId);

                try
                {
                    // Attempt an interactive (browser-based) authentication first. On most Windows environments
                    // this will work and is the most convenient for the user. On many Linux systems, there may
                    // not be a GUI and thus no browser. In that case, we fall back to the device code credential
                    // option in the catch block below.
                    accessToken = await this.AttemptTokenInteractiveFlowAsync(context, cancellationToken);
                }
                catch (AuthenticationFailedException exc) when (exc.Message.Contains("Unable to open a web page"))
                {
                    // Browser-based authentication is unavailable; switch to device code flow and present
                    // the user with a code and URL to complete authentication from another device.
                    accessToken = await this.AttemptTokenDeviceCodeFlowAsync(context, cancellationToken);
                }

                if (string.IsNullOrWhiteSpace(accessToken))
                {
                    throw new AuthenticationFailedException("Authentication failed. No access token could be obtained.");
                }
            }

            return accessToken;
        }

        /// <summary>
        /// Attempts to get an access token using an interactive browser session.
        /// </summary>
        /// <param name="context">Provides context to the target resource and tenant for which to get the access token.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>An access token for authorization on the target resource.</returns>
        protected virtual async Task<string> AttemptTokenDeviceCodeFlowAsync(TokenRequestContext context, CancellationToken cancellationToken)
        {
            DeviceCodeCredential credential = new DeviceCodeCredential(new DeviceCodeCredentialOptions
            {
                TenantId = context.TenantId,
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

            AccessToken response = await credential.GetTokenAsync(context, cancellationToken);

            return response.Token;
        }

        /// <summary>
        /// Attempts to get an access token using an interactive browser session.
        /// </summary>
        /// <param name="context">Provides context to the target resource and tenant for which to get the access token.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>An access token for authorization on the target resource.</returns>
        protected virtual async Task<string> AttemptTokenInteractiveFlowAsync(TokenRequestContext context, CancellationToken cancellationToken)
        {
            InteractiveBrowserCredential credential = new InteractiveBrowserCredential(
                new InteractiveBrowserCredentialOptions
                {
                    TenantId = context.TenantId
                });

            AccessToken response = await credential.GetTokenAsync(context, cancellationToken);

            return response.Token;
        }

        /// <summary>
        /// Creates a request context to for access token requests.
        /// </summary>
        /// <param name="resourceUri">The target subscription resource URI (e.g. https://any.vault.azure.net/).</param>
        /// <param name="tenantId">The ID of the Azure tenant in which the target subscription resource exists.</param>
        protected TokenRequestContext CreateRequestContext(Uri resourceUri, string tenantId)
        {
            var scopes = new string[]
            {
                new Uri(resourceUri, ".default").ToString()
            };

            return new TokenRequestContext(scopes, tenantId: tenantId);
        }
    }
}
