// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for common operations associated with blob managers.
    /// </summary>
    public static class BlobManagerExtensions
    {
        /// <summary>
        /// Returns true/false whether the content blob store is defined and exists in the dependencies.
        /// </summary>
        /// <param name="dependencies">The dependencies to verify.</param>
        /// <param name="store">The content blob store information if it exists.</param>
        /// <returns>True if the content blob store is defined. False if not.</returns>
        public static bool TryGetContentStoreManager(this IServiceCollection dependencies, out IBlobManager store)
        {
            dependencies.ThrowIfNull(nameof(dependencies));
            return BlobManagerExtensions.TryGetBlobStore(dependencies, DependencyStore.Content, out store);
        }

        /// <summary>
        /// Returns true/false whether the content blob store is defined and exists in the dependencies
        /// for the component.
        /// </summary>
        /// <param name="component">The component with dependencies to verify.</param>
        /// <param name="store">The packages blob store information if it exists.</param>
        /// <returns>True if the content blob store is defined. False if not.</returns>
        public static bool TryGetContentStoreManager(this VirtualClientComponent component, out IBlobManager store)
        {
            component.ThrowIfNull(nameof(component));
            return BlobManagerExtensions.TryGetBlobStore(component.Dependencies, DependencyStore.Content, out store);
        }

        /// <summary>
        /// Returns true/false whether the packages blob store is defined and exists in the dependencies.
        /// </summary>
        /// <param name="dependencies">The dependencies to verify.</param>
        /// <param name="store">The packages blob store information if it exists.</param>
        /// <returns>True if the packages blob store is defined. False if not.</returns>
        public static bool TryGetPackageStoreManager(this IServiceCollection dependencies, out IBlobManager store)
        {
            dependencies.ThrowIfNull(nameof(dependencies));
            return BlobManagerExtensions.TryGetBlobStore(dependencies, DependencyStore.Packages, out store);
        }

        /// <summary>
        /// Returns true/false whether the packages blob store is defined and exists in the dependencies
        /// for the component.
        /// </summary>
        /// <param name="component">The component with dependencies to verify.</param>
        /// <param name="store">The packages blob store information if it exists.</param>
        /// <returns>True if the packages blob store is defined. False if not.</returns>
        public static bool TryGetPackageStoreManager(this VirtualClientComponent component, out IBlobManager store)
        {
            component.ThrowIfNull(nameof(component));
            return BlobManagerExtensions.TryGetBlobStore(component.Dependencies, DependencyStore.Packages, out store);
        }

        private static bool TryGetBlobStore(IServiceCollection dependencies, string storeName, out IBlobManager store)
        {
            store = null;
            if (dependencies.TryGetService<IEnumerable<IBlobManager>>(out IEnumerable<IBlobManager> stores))
            {
                store = stores.FirstOrDefault(s => string.Equals(s.StoreDescription.StoreName, storeName, StringComparison.OrdinalIgnoreCase));
            }

            return store != null;
        }
    }
}
