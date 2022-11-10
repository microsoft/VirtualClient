// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
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
            if (this.Dependencies.HasService<IEnumerable<Uri>>())
            {
                services.AddSingleton<IEnumerable<Uri>>(this.Dependencies.GetService<IEnumerable<Uri>>());
            }

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
        /// <param name="hostEnvironment">IWebHostEnvironment required by ASP.NET</param>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Required signature for ASP.NET Core startup class definitions.")]
        public void Configure(IApplicationBuilder applicationBuilder, IWebHostEnvironment hostEnvironment)
        {
            applicationBuilder.UseMvc();
        }
    }
}
