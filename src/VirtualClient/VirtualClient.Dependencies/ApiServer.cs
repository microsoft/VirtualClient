// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Ensures the Virtual Client API service is running.
    /// </summary>
    public class ApiServer : VirtualClientComponent
    {
        private static Task apiHostingTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiServer"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">Parameters to the VC component.</param>
        public ApiServer(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// The time to wait in-between subsequent restart attempts of the API service.
        /// </summary>
        protected TimeSpan RestartAttemptWaitTime { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Starts the Virtual Client API server.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IApiManager apiManager = this.Dependencies.GetService<IApiManager>();
            IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();
            EventContext relatedContext = telemetryContext.Clone();

            // Default port
            int apiPort = clientManager.GetApiPort();
            if (this.IsMultiRoleLayout())
            {
                ClientInstance client = this.GetLayoutClientInstance();
                apiPort = clientManager.GetApiPort(client);
                relatedContext.AddContext(nameof(client), client);
            }

            relatedContext.AddContext(nameof(apiPort), apiPort);

            ApiServer.apiHostingTask = this.Logger.LogMessageAsync($"{this.TypeName}.StartupApiServer", telemetryContext, async () =>
            {
                try
                {
                    await apiManager.StartApiHostAsync(this.Dependencies, apiPort, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (VirtualClientException exc)
                {
                    throw new DependencyException("Virtual Client API host service failed to start.", exc, ErrorReason.ApiStartupFailed);
                }
            });

            return Task.CompletedTask;
        }
    }
}