// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for <see cref="IPackageManager"/> instances.
    /// </summary>
    public static class PackageManagerExtensions
    {
        /// <summary>
        /// Returns the package/dependency path information if it is registered.
        /// </summary>
        public static async Task<DependencyPath> GetPlatformSpecificPackageAsync(this IPackageManager packageManager, string packageName, PlatformID platform, Architecture architecture, CancellationToken cancellationToken)
        {
            DependencyPath package = await packageManager.GetPackageAsync(packageName, cancellationToken)
                .ConfigureAwait(false);

            if (package == null)
            {
                throw new DependencyException(
                    $"A package with the name '{packageName}' was not found on the system.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            return packageManager.PlatformSpecifics.ToPlatformSpecificPath(package, platform, architecture);
        }

        /// <summary>
        /// Registers the set of packages on the system so that they can be referenced by other
        /// components at runtime.
        /// </summary>
        /// <param name="packageManager">A package manager instance.</param>
        /// <param name="packages">A set of packages to register on the system.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public static async Task RegisterPackagesAsync(this IPackageManager packageManager, IEnumerable<DependencyPath> packages, CancellationToken cancellationToken)
        {
            packageManager.ThrowIfNull(nameof(packageManager));
            if (packages?.Any() == true)
            {
                foreach (DependencyPath package in packages)
                {
                    await packageManager.RegisterPackageAsync(package, (CancellationToken)cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Installs the set of extensions within the packages on the system.
        /// </summary>
        /// <param name="packageManager">A package manager instance.</param>
        /// <param name="extensionPackages">A set of packages containing extensions to install on the system.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public static async Task InstallExtensionsAsync(this IPackageManager packageManager, IEnumerable<DependencyPath> extensionPackages, CancellationToken cancellationToken)
        {
            packageManager.ThrowIfNull(nameof(packageManager));
            if (extensionPackages?.Any() == true)
            {
                foreach (DependencyPath package in extensionPackages)
                {
                    await packageManager.InstallExtensionsAsync(package, cancellationToken)
                         .ConfigureAwait(false);
                }
            }
        }
    }
}
