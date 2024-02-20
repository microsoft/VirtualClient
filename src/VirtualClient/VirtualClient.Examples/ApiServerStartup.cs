// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Diagnostics.CodeAnalysis;
    using System.IO.Abstractions;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Api;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Defines startup settings and requirements for the VC
    /// API using ASP.NET Core Kestrel middleware components.
    /// </summary>
    public class ApiServerStartup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiServerStartup"/> class.
        /// </summary>
        /// <param name="configuration">The configuration for the API service(s).</param>
        public ApiServerStartup(IConfiguration configuration)
        {
            configuration.ThrowIfNull(nameof(configuration));
            this.Configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration settings supplied to the REST API on startup.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// ASP.NET Core startup method for configuring dependency services used by the
        /// VC API service.
        /// </summary>
        /// <param name="services">The services provider to which the dependencies will be added.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
            services.AddSingleton<IFileSystem>(new FileSystem());

            // Add ASP.NET Core MVC Dependency Injection Middleware
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1
            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
            })
            .AddNewtonsoftJson();
        }

        /// <summary>
        /// ASP.NET Core startup method for configuring the application hosting environment for the
        /// VC API service.
        /// </summary>
        /// <param name="applicationBuilder">Provides context required to configure the application.</param>
        /// <param name="hostEnvironment">IWebHostEnvironment required by ASP.NET.</param>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Required signature for ASP.NET Core startup class definitions.")]
        public void Configure(IApplicationBuilder applicationBuilder, IWebHostEnvironment hostEnvironment)
        {
            applicationBuilder.UseMiddleware<ApiExceptionMiddleware>();
            applicationBuilder.UseMvc();
        }
    }
}
