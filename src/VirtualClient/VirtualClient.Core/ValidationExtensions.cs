// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for the <see cref="DependencyPath"/> and related instances.
    /// </summary>
    public static class ValidationExtensions
    {
        /// <summary>
        /// Validates that the package given is found.
        /// </summary>
        /// <param name="dependencyPath">The dependency path.</param>
        /// <param name="packageName">The name of the package.</param>
        public static void ThrowOnPackageNotFound([ValidatedNotNull] this DependencyPath dependencyPath, string packageName)
        {
            if (dependencyPath == null)
            {
                throw new WorkloadException(
                    $"The '{packageName}' workload package was not found in the packages directory.",
                    ErrorReason.WorkloadDependencyMissing);
            }
        }
    }
}
