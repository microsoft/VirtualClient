// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Extensions
{
    using System;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;

    /// <summary>
    /// Extension methods for <see cref="IServiceCollection"/> instances.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        private static DefaultServiceProviderFactory providerFactory = new DefaultServiceProviderFactory();

        /// <summary>
        /// Extension method returns true/false whether the collection contains a service whose type matches
        /// the type specified.
        /// </summary>
        /// <typeparam name="T">The data type of the service/dependency.</typeparam>
        /// <param name="services">The services collection containing the target service/dependency.</param>
        /// <returns>
        /// True if the collection contains the service, false if not.
        /// </returns>
        public static bool HasService<T>(this IServiceCollection services)
            where T : class
        {
            services.ThrowIfNull(nameof(services));
            return services.Any(desc => desc.ServiceType == typeof(T));
        }

        /// <summary>
        /// Extension method returns the service from the collection whose type matches
        /// the type specified.
        /// </summary>
        /// <typeparam name="T">The data type of the service/dependency.</typeparam>
        /// <param name="services">The services collection containing the target service/dependency.</param>
        /// <returns>
        /// A service/dependency whose type matches the type provided.
        /// </returns>
        public static T GetService<T>(this IServiceCollection services)
            where T : class
        {
            services.ThrowIfNull(nameof(services));

            T service = default(T);
            Type serviceType = typeof(T);
            ServiceDescriptor descriptor = services.FirstOrDefault(desc => desc.ServiceType == serviceType);

            if (descriptor == null)
            {
                throw new InvalidOperationException($"Unable to resolve service for type '{serviceType.FullName}'.");
            }

            if (descriptor != null)
            {
                IServiceProvider serviceProvider = ServiceCollectionExtensions.providerFactory.CreateServiceProvider(services);
                service = serviceProvider.GetService<T>();
            }

            return service;
        }

        /// <summary>
        /// Extension method returns true if a service is defined in the services collection whose type matches
        /// the type specified and outputs that service.
        /// </summary>
        /// <typeparam name="T">The data type of the service/dependency.</typeparam>
        /// <param name="services">The services collection containing the target service/dependency.</param>
        /// <param name="service">Output parameter set to the service is it exists in the services collection.</param>
        /// <returns>
        /// True if the service/dependency whose type matches the type provided exists, false if not.
        /// </returns>
        public static bool TryGetService<T>(this IServiceCollection services, out T service)
            where T : class
        {
            services.ThrowIfNull(nameof(services));

            service = default(T);
            bool hasService = services.HasService<T>();

            if (hasService)
            {
                service = services.GetService<T>();
            }

            return hasService;
        }
    }
}