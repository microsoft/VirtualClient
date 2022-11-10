// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Common
{
    using System;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods for <see cref="IServiceProvider"/> instances.
    /// </summary>
    public static class ServiceProviderExtensions
    {
        /// <summary>
        /// Returns the service/dependency object registered in the environment context.
        /// </summary>
        /// <typeparam name="TService">The data type of the service to locate.</typeparam>
        /// <returns>
        /// The service matching the data type defined.
        /// </returns>
        public static TService GetService<TService>(this IServiceProvider serviceProvider)
        {
            serviceProvider.ThrowIfNull(nameof(serviceProvider));

            object service = serviceProvider.GetService(typeof(TService));
            return (TService)service;
        }

        /// <summary>
        /// Try to get the service/dependency object registered in the environment context.
        /// </summary>
        /// <typeparam name="TService">The data type of the service to locate.</typeparam>
        /// <param name="serviceProvider"><see cref="IServiceProvider"></see></param>
        /// <param name="service"> Out parameter need to be update if service matching the data type is found</param>
        /// <returns>True if service matching the data type is found</returns>
        public static bool TryGetService<TService>(this IServiceProvider serviceProvider, out TService service)
        {
            serviceProvider.ThrowIfNull(nameof(serviceProvider));
            var result = false;
            service = default(TService);

            try
            {
                object tService = serviceProvider.GetService(typeof(TService));
                if (tService != null && tService is TService)
                {
                    service = (TService)tService;
                    result = true;
                }
            }
            catch
            {
                // Do nothing
            }

            return result;
        }
    }
}
