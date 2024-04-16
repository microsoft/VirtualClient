// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Polly;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Implements a test/mock package manager that tracks dependency packages in-memory and can be
    /// used to setup and confirm dependency-related behaviors in test scenarios.
    /// </summary>
    public class InMemoryPackageManager : List<DependencyPath>, IPackageManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryPackageManager"/> class.
        /// </summary>
        /// <param name="platformSpecifics">PlatformSpecifics that provides methods for platform-specific actions.</param>
        public InMemoryPackageManager(TestPlatformSpecifics platformSpecifics)
        {
            platformSpecifics.ThrowIfNull(nameof(platformSpecifics));
            this.PlatformSpecifics = platformSpecifics;
        }

        /// <summary>
        /// Provides platform-specific information.
        /// </summary>
        public PlatformSpecifics PlatformSpecifics { get; }

        /// <summary>
        /// Delegate allows the user to define custom logic to execute whenever extension packages
        /// are being discovered.
        /// </summary>
        public Func<PlatformExtensions> OnDiscoverExtensions { get; set; }

        /// <summary>
        /// Delegate allows the user to define custom logic to execute whenever packages
        /// are being discovered.
        /// </summary>
        public Func<IEnumerable<DependencyPath>> OnDiscoverPackages { get; set; }

        /// <summary>
        /// Delegate allows the user to define custom logic to execute whenever a 
        /// package is extracted.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> packageFilePath - The path to the package archive/zip file.</item>
        /// <item><see cref="string"/> destinationPath - The path where the package archive/zip file should be extracted (i.e. the exact directory name).</item>
        /// </list>
        /// </list>
        /// </summary>
        public Action<string, string> OnExtractPackage { get; set; }

        /// <summary>
        /// Delegate allows the user to define custom logic when a package is retrieved.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> packageName - The name of the package/dependency.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Func<string, DependencyPath> OnGetPackageLocation { get; set; }

        /// <summary>
        /// Delegate allows the user to define custom logic to execute whenever packages
        /// are being initialized.
        /// </summary>
        public Action OnInitializePackages { get; set; }

        /// <summary>
        /// Delegate allows the user to define custom logic to execute whenever a 
        /// package is installed from a store.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="IBlobManager"/> blob manager - Enables downloading the blob package to the system.</item>
        /// <item><see cref="DependencyDescriptor"/> description - A description of the Blob package to install.</item>
        /// <item><see cref="string"/> installationPath - The path where the package should be installed. Overrides the default installation path.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Func<IBlobManager, DependencyDescriptor, string, string> OnInstallPackage { get; set; }

        /// <summary>
        /// Delegate allows the user to define custom logic when a package is registered.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="DependencyPath"/> package - The package/dependency to register.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Action<DependencyPath> OnRegisterPackageLocation { get; set; }

        /// <summary>
        /// Mimics the behavior of discovering packages on the system.
        /// </summary>
        public Task<PlatformExtensions> DiscoverExtensionsAsync(CancellationToken cancellationToken)
        {
            return this.OnDiscoverExtensions != null
                ? Task.FromResult(this.OnDiscoverExtensions.Invoke())
                : Task.FromResult(new PlatformExtensions());
        }

        /// <summary>
        /// Mimics the behavior of discovering packages on the system.
        /// </summary>
        public Task<IEnumerable<DependencyPath>> DiscoverPackagesAsync(CancellationToken cancellationToken)
        {
            return this.OnDiscoverPackages != null
                ? Task.FromResult(this.OnDiscoverPackages.Invoke())
                : Task.FromResult(this as IEnumerable<DependencyPath>);
        }

        /// <summary>
        /// Mimics the behavior of extracting an archive/zip package file.
        /// </summary>
        public Task ExtractPackageAsync(string packageFilePath, string destinationPath, CancellationToken cancellationToken, ArchiveType archiveType = ArchiveType.Zip)
        {
            this.OnExtractPackage?.Invoke(packageFilePath, destinationPath);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Reads the package from the local in-memory list/store.
        /// </summary>
        public Task<DependencyPath> GetPackageAsync(string packageName, CancellationToken cancellationToken)
        {
            DependencyPath package = (this.OnGetPackageLocation != null)
                ? this.OnGetPackageLocation.Invoke(packageName)
                : this.FirstOrDefault(pkg => pkg.Name == packageName);

            return Task.FromResult(package);
        }

        /// <summary>
        /// Mimics the behavior of initializing packages on the system.
        /// </summary>
        public Task InitializePackagesAsync(CancellationToken cancellationToken)
        {
            this.OnInitializePackages?.Invoke();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Mimics the installation of a dependency/package from the package store defined.
        /// </summary>
        public Task<string> InstallPackageAsync(IBlobManager blobManager, DependencyDescriptor description, CancellationToken cancellationToken, string installationPath = null, IAsyncPolicy retryPolicy = null)
        {
            string archiveFileName = PackageManager.GetArchiveFileNameWithoutExtension(description.Name);

            string packageInstallationLocation = this.OnInstallPackage != null
                ? this.OnInstallPackage.Invoke(blobManager, description, installationPath)
                : this.PlatformSpecifics.GetPackagePath(archiveFileName.ToLowerInvariant());

            this.Add(new DependencyPath(description.PackageName.ToLowerInvariant(), packageInstallationLocation, $"Description of '{description.Name}' package"));

            return Task.FromResult(packageInstallationLocation);
        }

        /// <summary>
        /// Mimics the registration of a package with the package manager. This saves the package to the local
        /// in-memory list/store.
        /// </summary>
        public Task RegisterPackageAsync(DependencyPath package, CancellationToken cancellationToken)
        {
            this.OnRegisterPackageLocation?.Invoke(package);

            DependencyPath existingPackage = this.FirstOrDefault(pkg => pkg.Name == package.Name);
            if (existingPackage != null)
            {
                this.Remove(existingPackage);
            }

            this.Add(package);

            return Task.CompletedTask;
        }
    }
}
