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
    using Asp.Versioning;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
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
        public async Task StartApiHostAsync(IServiceCollection dependencies, int apiPort, CancellationToken cancellationToken)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder();

            // Configure the ASP.NET MVC self-hosted REST API application
            // ---------------------------------------------------------------------
            builder.Services.AddSingleton<IConfiguration>(dependencies.GetService<IConfiguration>());
            builder.Services.AddSingleton<ILogger>(dependencies.GetService<ILogger>());
            builder.Services.AddSingleton<IStateManager>(dependencies.GetService<IStateManager>());
            builder.Services.AddSingleton<IFileSystem>(dependencies.GetService<IFileSystem>());
            builder.Services.AddSingleton<PlatformSpecifics>(dependencies.GetService<PlatformSpecifics>());

            builder.Services.AddControllers();
            builder.Services.AddProblemDetails();
            builder.Services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
            })
            .AddNewtonsoftJson()
            .AddApplicationPart(Assembly.GetAssembly(typeof(HeartbeatController)))
            .AddControllersAsServices();

            builder.Services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1.0);
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;

                options.ApiVersionReader = ApiVersionReader.Combine(
                    new QueryStringApiVersionReader("api-version"),
                    new HeaderApiVersionReader("x-api-version"));
            })
            .AddMvc();

            // Open a port in the firewall for the REST API (e.g. 4500 by default)
            // ---------------------------------------------------------------------
            await this.OpenFirewallPortAsync(apiPort);

            using (WebApplication apiHost = builder.Build())
            {
                apiHost.UseMiddleware<ApiExceptionMiddleware>(dependencies.GetService<ILogger>());
                apiHost.MapControllers();
                apiHost.Urls.Add($"http://*:{apiPort}");
                
                await apiHost.RunAsync();
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
