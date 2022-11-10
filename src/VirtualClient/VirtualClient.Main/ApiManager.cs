// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using System;
    using System.Diagnostics.Metrics;
    using System.Net;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Rest;

    /// <summary>
    /// Provides methods for managing the Virtual Client API service and hosting
    /// requirements.
    /// </summary>
    public class ApiManager : IApiManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiManager"/> class.
        /// </summary>
        /// <param name="firewallManager">
        /// Provides features for opening firewall ports required to host the API service.
        /// </param>
        public ApiManager(IFirewallManager firewallManager)
        {
            firewallManager.ThrowIfNull(nameof(firewallManager));
            this.FirewallManager = firewallManager;
        }

        /// <inheritdoc />
        public IFirewallManager FirewallManager { get; }

        /// <inheritdoc />
        public async Task StartApiHostAsync(IServiceCollection dependencies, int port, CancellationToken cancellationToken)
        {
            X509Certificate2 cert = RsaCrypto.CreateSelfSignedCertificate("cn=virtualclient.com", 2048);

            IWebHostBuilder webHostBuilder = new WebHostBuilder();

            // When hosting locally, we use the Kestrel HTTP server.
            webHostBuilder.UseKestrel(options =>
            {
                // IMPORTANT:
                // You have to configure the HTTPS server to listen for TCP requests on both the local/loopback
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

                // 1) Listen for TCP requests for any IP address.

                // ************ HTTPS change doesn't work on Windows, pending research ****************
                ////options.Listen(IPAddress.Any, port, listenOptions =>
                ////{
                ////    listenOptions.UseHttps(cert, configureOptions: option =>
                ////    {
                ////        option.SslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12;
                ////    });
                ////});
                // ************ HTTPS change doesn't work on Windows, pending research ****************

                options.ListenAnyIP(port);
            });

            webHostBuilder.UseStartup<ApiServerStartup>(context => new ApiServerStartup(dependencies));

            await this.OpenFirewallPortAsync(port).ConfigureAwait(false);

            using (IWebHost webHost = webHostBuilder.Build())
            {
                await webHost.RunAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private Task OpenFirewallPortAsync(int apiPort)
        {
            return this.FirewallManager.EnableInboundConnectionsAsync(new List<FirewallEntry>
            {
                new FirewallEntry(
                    "Virtual Client: Allow API Support",
                    "Allows individual Virtual Client instances to communicate with each other via the self-hosted REST API",
                    "tcp",
                    new List<int> { apiPort })
            },
            CancellationToken.None);
        }
    }
}
