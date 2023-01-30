// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerUI;
    using VirtualClient.Api;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

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
            IWebHostBuilder webHostBuilder = new WebHostBuilder();

            this.ConfigureServices(webHostBuilder, dependencies);
            this.ConfigureApplication(webHostBuilder, dependencies);
            this.ConfigureHosting(webHostBuilder, port);

            await this.OpenFirewallPortAsync(port).ConfigureAwait(false);

            using (IWebHost webHost = webHostBuilder.Build())
            {
                await webHost.RunAsync().ConfigureAwait(false);
            }
        }

        private void ConfigureApplication(IWebHostBuilder hostBuilder, IServiceCollection dependencies)
        {
            hostBuilder.Configure(apiService =>
            {
                // Enable middleware to serve generated Swagger as a JSON endpoint.
                apiService.UseSwagger();

                // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.),
                // specifying the Swagger JSON endpoint.
                apiService.UseSwaggerUI(doc =>
                {
                    doc.SwaggerEndpoint("/swagger/v1/swagger.json", "Virtual Client API (v1)");

                    // "Try it Out" only for GET methods.
                    doc.SupportedSubmitMethods(SubmitMethod.Delete, SubmitMethod.Get, SubmitMethod.Head, SubmitMethod.Post, SubmitMethod.Put);
                    doc.RoutePrefix = string.Empty;
                    doc.ConfigObject.DefaultModelRendering = ModelRendering.Example;
                });

                apiService.UseApiVersioning();
                apiService.UseMiddleware<ApiExceptionMiddleware>(dependencies.GetService<ILogger>());
                apiService.UseMvc();
            });
        }

        private void ConfigureHosting(IWebHostBuilder hostBuilder, int apiPort)
        {
            // When hosting locally, we use the Kestrel HTTP server.
            hostBuilder.UseKestrel(options =>
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
                // X509Certificate2 cert = RsaCrypto.CreateSelfSignedCertificate("cn=virtualclient.com", 2048);
                //
                // ************ HTTPS change doesn't work on Windows, pending research ****************
                ////options.Listen(IPAddress.Any, port, listenOptions =>
                ////{
                ////    listenOptions.UseHttps(cert, configureOptions: option =>
                ////    {
                ////        option.SslProtocols = System.Security.Authentication.SslProtocols.Tls13 | System.Security.Authentication.SslProtocols.Tls12;
                ////    });
                ////});
                // ************ HTTPS change doesn't work on Windows, pending research ****************

                options.ListenAnyIP(apiPort);
            });
        }

        private void ConfigureServices(IWebHostBuilder hostBuilder, IServiceCollection dependencies)
        {
            hostBuilder.ConfigureServices((services) =>
            {
                services.AddSingleton<IConfiguration>(dependencies.GetService<IConfiguration>());
                services.AddSingleton<ILogger>(dependencies.GetService<ILogger>());
                services.AddSingleton<IStateManager>(dependencies.GetService<IStateManager>());
                services.AddSingleton<IFileSystem>(dependencies.GetService<IFileSystem>());
                services.AddSingleton<PlatformSpecifics>(dependencies.GetService<PlatformSpecifics>());

                // Add ASP.NET Core MVC Dependency Injection Middleware
                // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1
                services.AddMvc(options =>
                {
                    options.EnableEndpointRouting = false;
                })
                .AddNewtonsoftJson()
                .AddApplicationPart(Assembly.GetAssembly(typeof(HeartbeatController)))
                .AddControllersAsServices();

                // Add support for controller versioning (e.g. controllers that support different versions of API methods).
                services.AddApiVersioning(options =>
                {
                    options.ReportApiVersions = true;
                    options.AssumeDefaultVersionWhenUnspecified = true;
                    options.DefaultApiVersion = new ApiVersion(1, 0);
                });

                // Add OpenAPI/Swagger definition.
                // https://docs.microsoft.com/en-us/aspnet/core/tutorials/getting-started-with-swashbuckle?view=aspnetcore-3.1&tabs=visual-studio
                // https://github.com/domaindrivendev/Swashbuckle.AspNetCore
                // https://idratherbewriting.com/learnapidoc
                services.AddSwaggerGen(doc =>
                {
                    doc.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "Virtual Client API",
                        Description = "Virtual Client REST API/service.",
                        Contact = new OpenApiContact()
                        {
                            Name = "Virtual Client Team",
                            Email = "crc_vc_fte@microsoft.com",
                            Url = new Uri("https://microsoft.github.io/VirtualClient/")
                        },
                        License = new OpenApiLicense
                        {
                            Name = "API License",
                            Url = new Uri("https://github.com/microsoft/VirtualClient/blob/main/LICENSE")
                        },
                    });

                    // All JSON input and output objects are expected to be in camel-case.
                    doc.DescribeAllParametersInCamelCase();
                });
            });
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
