// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Implements a test/mock api manager that starts api host
    /// </summary>
    public class InMemoryApiManager : IApiManager
    {
        /// <summary>
        /// Delegate enables custom logic to be executed on a call to start api host.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="IServiceCollection"/> dependencies - Provides dependencies required by the API service(s).</item>
        /// <item><see cref="CancellationToken"/> cancellationToken - A token that can be used to cancel the operations of the API host.</item>
        /// /// <item><see cref="int"/> port - The port on which the API will listen to HTTP requests.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Func<IServiceCollection, int, CancellationToken, Task> OnStartApiHost { get; set; }

        /// <summary>
        /// Starts the Virtual Client API service running in the background.
        /// </summary>
        /// <param name="dependencies">Provides dependencies required by the API service(s).</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations of the API host.</param>
        /// <param name="port">The port on which the API will listen to HTTP requests.</param>
        public Task StartApiHostAsync(IServiceCollection dependencies, int port, CancellationToken cancellationToken)
        {
            return this.OnStartApiHost != null 
                ? this.OnStartApiHost.Invoke(dependencies, port, cancellationToken)
                : Task.CompletedTask;
        }
    }
}
