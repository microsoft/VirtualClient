// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;
    using System.Threading;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;

    /// <summary>
    /// Class for producing EventHub channels
    /// </summary>
    public static class ApiHosting
    { 
        /// <summary>
        /// Creates a <see cref="IWebHost"/> instance to host the VC API services referenced
        /// by the startup instance on the local machine. The ASP.NET Kestrel server is used.
        /// </summary>
        /// <param name="configuration">Provides configuration settings to the host.</param>
        /// <param name="firewallManager">Provides methods for opening ports in the firewall required to host the API HTTP server.</param>
        /// <param name="port">The port for which HTTP listeners will be setup.</param>
        public static IWebHost StartApiServer<TStartup>(IConfiguration configuration, IFirewallManager firewallManager, int port)
            where TStartup : class
        {
            ApiHosting.OpenFirewallPorts(firewallManager, port, CancellationToken.None);

            IWebHostBuilder webHostBuilder = new WebHostBuilder();
            webHostBuilder.UseConfiguration(configuration);

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
                // Name: Virtual Client: Allow API Requests
                // Action: Allow the connection
                // Protocol Type: TCP
                // Local Port: 4500
                // Remote Port: All
                // Profiles: Domain, Private, Public

                // Listen for TCP requests for any IP address.
                options.ListenAnyIP(port);
            });

            webHostBuilder.UseStartup<ApiServerStartup>(context => new ApiServerStartup(configuration));

            return webHostBuilder.Build();
        }

        private static void OpenFirewallPorts(IFirewallManager firewallManager, int apiPort, CancellationToken cancellationToken)
        {
            firewallManager.EnableInboundConnectionsAsync(new List<FirewallEntry>
            {
                new FirewallEntry(
                    "Virtual Client: Allow API Support",
                    "Allows individual Virtual Client instances to communicate with each other via the self-hosted REST API",
                    "tcp",
                    new List<int> { ApiClientManager.DefaultApiPort })
            },
            cancellationToken).GetAwaiter().GetResult();
        }
    }
}
