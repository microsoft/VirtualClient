// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Identity
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides functions for handling authorization requirements.
    /// </summary>
    public interface IAuthorizationManager
    {
        /// <summary>
        /// Acquires an access token from the target Azure tenant.
        /// </summary>
        /// <param name="resourceUri">The target subscription resource URI (e.g. https://any.vault.azure.net/).</param>
        /// <param name="tenantId">The ID of the Azure tenant in which the target subscription resource exists.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the request operation.</param>
        Task<string> GetAccessTokenAsync(Uri resourceUri, string tenantId, CancellationToken cancellationToken);
    }
}
