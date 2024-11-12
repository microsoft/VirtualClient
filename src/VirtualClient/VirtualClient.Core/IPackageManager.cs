// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Polly;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides support for installing and managing packages on the system.
    /// </summary>
    public interface IPackageManager
    {
        /// <summary>
        /// Provides platform-specific information.
        /// </summary>
        public PlatformSpecifics PlatformSpecifics { get; }

        /// <summary>
        /// Performs extensions package discovery on the system.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        Task<PlatformExtensions> DiscoverExtensionsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Performs package discovery on the system.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        Task<IEnumerable<DependencyPath>> DiscoverPackagesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Extracts/unzips the package at the file path provided. This supports standard .zip and .nupkg
        /// file formats.
        /// </summary>
        /// <param name="packageFilePath">The path to the package zip file.</param>
        /// <param name="destinationPath">The path to the directory where the files should be extracted.</param>
        /// <param name="archiveType">The type of archive format the file is in.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the extract operation.</param>
        Task ExtractPackageAsync(string packageFilePath, string destinationPath, CancellationToken cancellationToken, ArchiveType archiveType = ArchiveType.Zip);

        /// <summary>
        /// Returns the package/dependency path information if it is registered.
        /// </summary>
        /// <param name="packageName">The name of the package dependency.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        Task<DependencyPath> GetPackageAsync(string packageName, CancellationToken cancellationToken);

        /// <summary>
        /// Performs package initialization on the system including extraction of package archives.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        Task InitializePackagesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Downloads and installs a dependency/package from the package store defined.
        /// </summary>
        /// <param name="packageStoreManager">The blob manager to use for downloading the package to the file system.</param>
        /// <param name="description">Provides information about the target package.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="installationPath">Optional installation path to be used to override the default installation path.</param>
        /// <param name="retryPolicy">A retry policy to apply to the blob download and installation to allow for transient error handling.</param>
        /// <returns>The path where the Blob package was installed.</returns>
        Task<string> InstallPackageAsync(IBlobManager packageStoreManager, DependencyDescriptor description, CancellationToken cancellationToken, string installationPath = null, IAsyncPolicy retryPolicy = null);

        /// <summary>
        /// Registers/saves the path so that it can be used by dependencies, workloads and monitors. Paths registered
        /// follow a strict format
        /// </summary>
        /// <param name="package">Describes a package dependency to register with the system.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        Task RegisterPackageAsync(DependencyPath package, CancellationToken cancellationToken);
    }
}
