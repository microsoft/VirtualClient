// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command resets the Virtual Client environment for "first time" run scenarios.
    /// </summary>
    public class CleanArtifactsCommand : CommandBase
    {
        /// <summary>
        /// Executes the operations to reset the environment.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="dependencies">Dependencies/services created for the application.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        protected override async Task<int> ExecuteAsync(string[] args, IServiceCollection dependencies, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
            ILogger logger = dependencies.GetService<ILogger>();

            if (this.CleanTargets == null)
            {
                // The default is to clean everything.
                this.CleanTargets = new List<string>
                {
                    VirtualClient.Contracts.CleanTargets.All
                };
            }

            await this.CleanAsync(systemManagement, cancellationToken, logger);

            return 0;
        }
    }
}