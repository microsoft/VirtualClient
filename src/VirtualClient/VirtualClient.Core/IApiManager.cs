// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Provides methods for managing the Virtual Client API service and hosting
    /// requirements.
    /// </summary>
    public interface IApiManager
    {
        /// <summary>
        /// Starts the Virtual Client API service running in the background.
        /// </summary>
        /// <param name="dependencies">Provides dependencies required by the API service(s).</param>
        /// <param name="port">The port on which the API will listen to HTTP requests.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations of the API host.</param>
        Task StartApiHostAsync(IServiceCollection dependencies, int port, CancellationToken cancellationToken);
    }
}
