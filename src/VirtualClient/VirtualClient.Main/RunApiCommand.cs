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
    using VirtualClient.Common;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Api;
    using VirtualClient.Contracts;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Command runs the Virtual Client API and allows for monitoring of a local or
    /// remote API endpoint. This is primarily for manual debugging of connectivity issues.
    /// </summary>
    public class RunApiCommand : CommandBase
    {
        /// <summary>
        /// The IP address of a remote/target system running a Virtual Client API in which
        /// to monitor.
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// True if an instance of the Virtual Client API service should be monitored. An IP address
        /// can be provided. When an IP address is provided, the remote API service will be monitored
        /// for heartbeats. Otherwise, the local API service will be monitored.
        /// </summary>
        public bool Monitor { get; set; }

        /// <summary>
        /// Executes the operations to run the Virtual Client API on the local system and to
        /// monitor either a local or remote instance.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public override async Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            CancellationToken cancellationToken = cancellationTokenSource.Token;
            IServiceCollection dependencies = this.InitializeDependencies(args);
            ILogger logger = dependencies.GetService<ILogger>();
            ISystemManagement systemManagement = dependencies.GetService<ISystemManagement>();
            IApiManager apiManager = dependencies.GetService<IApiManager>();
            IApiClientManager apiClientManager = dependencies.GetService<IApiClientManager>();

            if (this.IsCleanRequested)
            {
                await this.CleanAsync(systemManagement, cancellationToken, logger);
            }

            int localPort = apiClientManager.GetApiPort(new ClientInstance(
                Environment.MachineName,
                System.Net.IPAddress.Loopback.ToString(),
                ClientRole.Client));

            int serverPort = localPort;

            if (this.ApiPorts?.Any() == true)
            {
                // Examples:
                // --api-port=4500
                //               -> both client and server will use the same port. This is OK so long as they are
                //                  on different systems.
                //
                // --port=4500/Client,4501/Server
                //               -> client will use 4500, server will use 4501. This allows both the client and
                //                  the server to run on the same system.
                serverPort = apiClientManager.GetApiPort(new ClientInstance(
                    Environment.MachineName,
                    System.Net.IPAddress.Loopback.ToString(),
                    ClientRole.Server));
            }

            // 1) Host the API server locally.
            Task apiServerHostingTask = apiServerHostingTask = RunApiCommand.CreateApiServerHostingTask(apiManager, dependencies, localPort, cancellationToken);

            // 2) Monitor either the local API or a remote API for heartbeats.
            Task apiHeartbeatTestTask = Task.CompletedTask;

            if (this.Monitor)
            {
                apiHeartbeatTestTask = RunApiCommand.CreateApiMonitoringTask(this.IPAddress, serverPort, cancellationToken);
            }

            await apiServerHostingTask.ConfigureAwait(false);

            cancellationTokenSource.Cancel();

            // Allow all background tasks to exit gracefully where possible.
            Task gracefulExitTimeoutTask = Task.Delay(TimeSpan.FromMinutes(1));
            
            await Task.WhenAny(Task.WhenAll(apiServerHostingTask, apiHeartbeatTestTask), gracefulExitTimeoutTask)
                .ConfigureAwait(false);

            return 0;
        }

        /// <summary>
        /// Initializes dependencies required by Virtual Client application operations.
        /// </summary>
        protected override IServiceCollection InitializeDependencies(string[] args)
        {
            IServiceCollection dependencies = base.InitializeDependencies(args);
            PlatformSpecifics platformSpecifics = dependencies.GetService<PlatformSpecifics>();
            ILogger logger = dependencies.GetService<ILogger>();

            ISystemManagement systemManagement = DependencyFactory.CreateSystemManager(
                this.AgentId,
                Guid.NewGuid().ToString(),
                platformSpecifics,
                logger);

            IApiManager apiManager = new ApiManager(systemManagement.FirewallManager);

            dependencies.AddSingleton<ISystemInfo>(systemManagement);
            dependencies.AddSingleton<ISystemManagement>(systemManagement);
            dependencies.AddSingleton<IApiManager>(apiManager);
            dependencies.AddSingleton<ProcessManager>(systemManagement.ProcessManager);
            dependencies.AddSingleton<IDiskManager>(systemManagement.DiskManager);
            dependencies.AddSingleton<IFileSystem>(systemManagement.FileSystem);
            dependencies.AddSingleton<IFirewallManager>(systemManagement.FirewallManager);
            dependencies.AddSingleton<IPackageManager>(systemManagement.PackageManager);
            dependencies.AddSingleton<IStateManager>(systemManagement.StateManager);

            return dependencies;
        }

        private static Task CreateApiServerHostingTask(IApiManager apiManager, IServiceCollection dependencies, int port, CancellationToken cancellationToken)
        {
            return apiManager.StartApiHostAsync(dependencies, port, cancellationToken);
        }

        private static Task CreateApiMonitoringTask(string apiHostIpAddress, int port, CancellationToken cancellationToken, bool stateTest = false)
        {
            VirtualClientApiClient apiClient = null;
            if (!string.IsNullOrWhiteSpace(apiHostIpAddress))
            {
                apiClient = DependencyFactory.CreateVirtualClientApiClient(System.Net.IPAddress.Parse(apiHostIpAddress), port);
            }
            else
            {
                apiClient = DependencyFactory.CreateVirtualClientApiClient(System.Net.IPAddress.Loopback, port);
            }

            if (stateTest)
            {
                return apiClient.RunStateTest(cancellationToken);
            }

            return apiClient.RunHeartbeatTest(cancellationToken);
        }
    }
}
