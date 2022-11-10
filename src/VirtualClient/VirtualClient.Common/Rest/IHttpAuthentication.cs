// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Rest
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for automatically refresh tokens for httpclient.
    /// </summary>
    public interface IHttpAuthentication
    {
        /// <summary>
        /// Auto refresh authentication header on httpclient if it's expired.
        /// </summary>
        /// <param name="httpClient">Http client.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <param name="autoRefresh">Auto refresh token, default is true.</param>
        /// <returns>Completed task.</returns>
        Task AuthenticateAsync(HttpClient httpClient, CancellationToken cancellationToken, bool autoRefresh = true);

        /// <summary>
        /// Reset the token cache so that the next token would be fresh.
        /// </summary>
        void Reset();
    }
}