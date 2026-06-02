// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// MongoDB workload base executor.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public abstract class MongoDBExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        protected MongoDBExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ApiClientManager = this.Dependencies.GetService<IApiClientManager>();
        }

        /// <summary>
        /// Port on which MongoDB server runs.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port), 27017);
            }
        }

        /// <summary>
        /// Provides the ability to create API clients for interacting with local as well as remote instances
        /// of the Virtual Client API service.
        /// </summary>
        protected IApiClientManager ApiClientManager { get; }

        /// <summary>
        /// Client used to communicate with the hosted instance of the
        /// Virtual Client API at server side.
        /// </summary>
        protected IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// Server IpAddress on which MongoDB Server runs.
        /// </summary>
        protected string ServerIpAddress { get; set; }

        /// <summary>
        /// Cancellation Token Source for Server.
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// Initializes the environment and dependencies for running the MongoDB workload.
        /// </summary>
        /// <param name="telemetryContext">The telemetry context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.EvaluateParametersAsync(cancellationToken).ConfigureAwait(false);

            if (this.IsMultiRoleLayout())
            {
                ClientInstance clientInstance = this.GetLayoutClientInstance();
                string layoutIPAddress = clientInstance.IPAddress;

                this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);
                this.ThrowIfRoleNotSupported(clientInstance.Role);
            }
        }

        /// <summary>
        /// Initializes API clients for communication between client and server.
        /// </summary>
        protected void InitializeApiClients()
        {
            bool isSingleVM = !this.IsMultiRoleLayout();

            if (isSingleVM)
            {
                this.ServerIpAddress = IPAddress.Loopback.ToString();
                this.ServerApiClient = this.ApiClientManager.GetOrCreateApiClient(IPAddress.Loopback.ToString(), IPAddress.Loopback);
            }
            else
            {   
                var serverInstances = this.GetLayoutClientInstances(ClientRole.Server);
                if (!serverInstances.Any())
                {
                    throw new InvalidOperationException("No server instance found. Please check the layout configuration.");
                }

                ClientInstance serverInstance = serverInstances.First();
                if (IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress))
                {
                    this.ServerIpAddress = serverIPAddress.ToString();
                    this.ServerApiClient = this.ApiClientManager.GetOrCreateApiClient(serverIPAddress.ToString(), serverIPAddress);
                }
                else
                {
                    throw new InvalidOperationException($"Invalid IP address format: {serverInstance.IPAddress}");
                }

                this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", this.ServerApiClient);
            }
        }
    }
}
