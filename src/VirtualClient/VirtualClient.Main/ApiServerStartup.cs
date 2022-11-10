// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO.Abstractions;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using VirtualClient.Api;
    using VirtualClient.Contracts;
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
        /// <param name="dependencies">Provides dependencies required by the API service(s).</param>
        public ApiServerStartup(IServiceCollection dependencies)
        {
            dependencies.ThrowIfNull(nameof(dependencies));
            this.Dependencies = dependencies;
        }

        /// <summary>
        /// Provides dependencies required by the API service(s).
        /// </summary>
        public IServiceCollection Dependencies { get; }

        /// <summary>
        /// ASP.NET Core startup method for configuring dependency services used by the
        /// VC API service.
        /// </summary>
        /// <param name="services">The services provider to which the dependencies will be added.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IConfiguration>(this.Dependencies.GetService<IConfiguration>());
            services.AddSingleton<ILogger>(this.Dependencies.GetService<ILogger>());
            services.AddSingleton<IStateManager>(this.Dependencies.GetService<IStateManager>());
            services.AddSingleton<IFileSystem>(this.Dependencies.GetService<IFileSystem>());
            services.AddSingleton<PlatformSpecifics>(this.Dependencies.GetService<PlatformSpecifics>());

            // Add ASP.NET Core MVC Dependency Injection Middleware
            // https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1
            services.AddMvc(options =>
            {
                options.EnableEndpointRouting = false;
            })
            .AddNewtonsoftJson();

            // Add support for controller versioning (e.g. controllers that support different versions of API methods).
            services.AddApiVersioning(options =>
            {
                options.ReportApiVersions = true;
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.DefaultApiVersion = new ApiVersion(1, 0);
            });
        }

        /// <summary>
        /// ASP.NET Core startup method for configuring the application hosting environment for the
        /// VC API service.
        /// </summary>
        /// <param name="applicationBuilder">Provides context required to configure the application.</param>
        /// <param name="hostEnvironment"></param>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Required signature for ASP.NET Core startup class definitions.")]
        public void Configure(IApplicationBuilder applicationBuilder, IWebHostEnvironment hostEnvironment)
        {
            applicationBuilder.UseMiddleware<ApiExceptionMiddleware>(this.Dependencies.GetService<ILogger>());
            applicationBuilder.UseMvc();
        }
    }
}
