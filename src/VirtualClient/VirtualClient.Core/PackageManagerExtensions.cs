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
        public static async Task<DependencyPath> GetPackageAsync(this IPackageManager packageManager, string packageName, CancellationToken cancellationToken, bool throwIfNotfound = true)
        {
            packageManager.ThrowIfNull(nameof(packageManager));
            packageName.ThrowIfNullOrWhiteSpace(nameof(packageName));

            DependencyPath package = await packageManager.GetPackageAsync(packageName, cancellationToken);

            if (throwIfNotfound && package == null)
            {
                throw new DependencyException(
                    $"A package with the name '{packageName}' was not found on the system.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            return package;
        }

        /// <summary>
        /// Returns the package/dependency path information if it is registered.
        /// </summary>
        public static async Task<DependencyPath> GetPlatformSpecificPackageAsync(this IPackageManager packageManager, string packageName, PlatformID platform, Architecture architecture, CancellationToken cancellationToken, bool throwIfNotfound = true)
        {
            packageManager.ThrowIfNull(nameof(packageManager));
            packageName.ThrowIfNullOrWhiteSpace(nameof(packageName));

            DependencyPath package = await packageManager.GetPackageAsync(packageName, cancellationToken, throwIfNotfound)
                .ConfigureAwait(false);

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
                // Note:
                // We process the *.vcpkg files in chronological order from earliest to latest
                // using the file creation timestamp. This helps address scenarios where there are *.vcpkg
                // files that have the same/duplicate name. In this scenario, we will effectively take the
                // *.vcpkg file for the latest package created or downloaded and use that for registration.
                foreach (DependencyPath package in packages.OrderBy(pkg => pkg.Timestamp))
                {
                    await packageManager.RegisterPackageAsync(package, (CancellationToken)cancellationToken);
                }
            }
        }
    }
}
