// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for certificate manager
    /// </summary>
    public interface IAuthenticationProvider<TResult>
    {
        /// <summary>
        /// Get authentication result for client on a resource.
        /// </summary>
        /// <returns>Authentication result.</returns>
        Task<TResult> AuthenticateAsync(CancellationToken cancellationToken);
    }
}