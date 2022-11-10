// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using VirtualClient.Common;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Example command to run the API service.
    /// </summary>
    internal class RunApiCommand
    {
        /// <summary>
        /// The set of target servers to which to proxy requests to.
        /// </summary>
        public IEnumerable<string> ApiServers { get; set; }

        /// <summary>
        /// The port on which the API service should run.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Executes the default monitor execution command.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public async Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {

            int exitCode = 0;

            try
            {
                await this.StartApiHostAsync(cancellationTokenSource.Token)
                    .ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.Message);
                Console.WriteLine(exc.StackTrace);
                exitCode = 1;
            }

            return exitCode;
        }

        private async Task StartApiHostAsync(CancellationToken cancellationToken)
        {
            IWebHostBuilder webHostBuilder = new WebHostBuilder();

            // When hosting locally, we use the Kestrel HTTP server.
            webHostBuilder.UseKestrel(options =>
            {
                // IMPORTANT:
                // You have to configure the HTTP server to listen for TCP requests on both the local/loopback
                // address as well as any other IP address. Additionally, a firewall entry must be created for
                // the port for inbound connections.
                //
                // Firewall Entry Details
                // -----------------------------------------------
                // Name: Example Workload: Allow API Requests
                // Action: Allow the connection
                // Protocol Type: TCP
                // Local Port: 4501
                // Remote Port: All
                // Profiles: Domain, Private, Public

                // 1) Listen for TCP requests for any IP address.
                options.ListenAnyIP(this.Port);
            });

            IServiceCollection dependencies = new ServiceCollection();
            if (this.ApiServers?.Any() == true)
            {
                dependencies.AddSingleton<IEnumerable<Uri>>(this.ApiServers.Select(srv => new Uri($"http://{srv}:{this.Port}")));
            }

            webHostBuilder.UseStartup<ApiServerStartup>(context => new ApiServerStartup(dependencies));

            await this.OpenFirewallPortAsync(this.Port).ConfigureAwait(false);

            using (IWebHost webHost = webHostBuilder.Build())
            {
                await webHost.RunAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private Task OpenFirewallPortAsync(int apiPort)
        {
            IFirewallManager firewallManager = null;
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                    firewallManager = new WindowsFirewallManager(new WindowsProcessManager());
                    break;

                case PlatformID.Unix:
                    firewallManager = new UnixFirewallManager(new UnixProcessManager());
                    break;
            }

            return firewallManager.EnableInboundConnectionsAsync(new List<FirewallEntry>
            {
                new FirewallEntry(
                    "Example Workload: Allow API Support",
                    "Allows individual instances to communicate with each other via the self-hosted REST API",
                    "tcp",
                    new List<int> { apiPort })
            },
            CancellationToken.None);
        }
    }
}
