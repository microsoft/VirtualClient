// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies.Packaging
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Provides functionality for downloading and installing dependency packages from
    /// a cloud blob store onto the system.
    /// </summary>
    public class DependencyPackageInstallation : VirtualClientComponent
    {
        private const string FirstDisk = "{FirstDisk}";
        private const string LastDisk = "{LastDisk}";

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyPackageInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public DependencyPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// The type of archival file format the blob is in.
        /// </summary>
        public ArchiveType ArchiveType
        {
            get
            { 
                return this.Parameters.GetEnumValue<ArchiveType>(nameof(DependencyPackageInstallation.ArchiveType), ArchiveType.Zip);
            }
        }

        /// <summary>
        /// The name of the blob package in the storage account.
        /// </summary>
        public string BlobName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DependencyPackageInstallation.BlobName));
            }

            set
            {
                this.Parameters[nameof(DependencyPackageInstallation.BlobName)] = value;
            }
        }

        /// <summary>
        /// The name of the container in which the blob package exists in 
        /// the storage account.
        /// </summary>
        public string BlobContainer
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DependencyPackageInstallation.BlobContainer));
            }

            set
            {
                this.Parameters[nameof(DependencyPackageInstallation.BlobContainer)] = value;
            }
        }

        /// <summary>
        /// The path in which to install the package.
        /// </summary>
        public string InstallationPath
        {
            get 
            {
                return this.Parameters.GetValue<string>(nameof(DependencyPackageInstallation.InstallationPath), string.Empty);
            }

            set
            {
                this.Parameters[nameof(DependencyPackageInstallation.InstallationPath)] = value;
            }
        }

        /// <summary>
        /// Whether the blob should be extracted.
        /// </summary>
        public bool Extract
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(DependencyPackageInstallation.Extract), true);
            }

            set
            {
                this.Parameters[nameof(DependencyPackageInstallation.Extract)] = value;
            }
        }

        /// <summary>
        /// Executes the blob package download/installation operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            MetadataContract.Persist(
                $"package_{this.PackageName}",
                this.BlobName,
                MetadataContractCategory.Dependencies,
                true);

            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            IFileSystem fileSystem = this.Dependencies.GetService<IFileSystem>();

            DependencyPath existingPackage = await packageManager.GetPackageAsync(this.PackageName, cancellationToken)
                .ConfigureAwait(false);

            telemetryContext.AddContext("packageDirectory", this.PlatformSpecifics.GetPackagePath());
            telemetryContext.AddContext("packageExists", existingPackage != null);
            telemetryContext.AddContext("package", existingPackage);

            // If a built-in package exists, we do not currently override it with a Blob package
            // downloaded to the system.
            if (existingPackage == null || !fileSystem.Directory.Exists(existingPackage.Path))
            {
                BlobDescriptor packageDescription = new BlobDescriptor
                {
                    Name = this.BlobName,
                    ContainerName = this.BlobContainer,
                    PackageName = this.PackageName,
                    ArchiveType = this.ArchiveType,
                    Extract = this.Extract
                };

                telemetryContext.AddContext("package", packageDescription);

                string installationPath = null;
                if (!string.IsNullOrWhiteSpace(this.InstallationPath))
                {
                    IDiskManager diskManager = this.Dependencies.GetService<IDiskManager>();
                    IEnumerable<Disk> disks = await diskManager.GetDisksAsync(cancellationToken).ConfigureAwait(false);
                    if (!DependencyPackageInstallation.TryResolveRelativeDiskLocation(disks, this.InstallationPath, this.Platform, out installationPath))
                    {
                        throw new WorkloadException(
                            $"The installation path provided '{this.InstallationPath}' cannot be resolved",
                            ErrorReason.DependencyInstallationFailed);
                    }
                }

                if (!this.TryGetPackageStoreManager(out IBlobManager blobManager))
                {
                    throw new DependencyException(
                        $"Package store not defined. The package '{packageDescription.Name}' cannot be installed because the package store information " +
                        $"was not provided to the application on the command line (e.g. --packages).",
                        ErrorReason.PackageStoreNotDefined);
                }

                string packageLocation = await packageManager.InstallPackageAsync(blobManager, packageDescription, cancellationToken, installationPath)
                    .ConfigureAwait(false);

                if (string.IsNullOrWhiteSpace(packageLocation))
                {
                    throw new DependencyException(
                        $"Blob package installation failed for package '{this.BlobName}'.",
                        ErrorReason.DependencyInstallationFailed);
                }

                telemetryContext.AddContext("packageLocation", packageLocation);
            }
        }

        /// <summary>
        /// Downloads the dependency from the container specified to the path defined.
        /// </summary>
        /// <param name="blobManager">The blob manager to use for downloading the package dependency.</param>
        /// <param name="description">The dependency description.</param>
        /// <param name="downloadPath">Provides the location where the package should be downloaded.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected virtual async Task<Stream> DownloadDependencyPackageAsync(IBlobManager blobManager, DependencyDescriptor description, string downloadPath, CancellationToken cancellationToken)
        {
            FileStream stream = new FileStream(downloadPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            await blobManager.DownloadBlobAsync(description, stream, cancellationToken)
                .ConfigureAwait(false);

            return stream;
        }

        private static bool TryResolveRelativeDiskLocation(IEnumerable<Disk> disks, string relativeLocationReference, PlatformID platform, out string diskLocation)
        {
            diskLocation = relativeLocationReference;
            if (DependencyPackageInstallation.IsRelativeDiskLocation(relativeLocationReference))
            {
                if (relativeLocationReference.StartsWith(DependencyPackageInstallation.FirstDisk))
                {
                    string accessPath = disks.OrderBy(d => d.Index).First().GetPreferredAccessPath(platform);
                    diskLocation = Path.Combine(relativeLocationReference.Replace(DependencyPackageInstallation.FirstDisk, accessPath));
                    return true;
                }

                if (relativeLocationReference.StartsWith(DependencyPackageInstallation.LastDisk))
                {
                    string accessPath = disks.OrderBy(d => d.Index).Last().GetPreferredAccessPath(platform);
                    diskLocation = Path.Combine(relativeLocationReference.Replace(DependencyPackageInstallation.LastDisk, accessPath));
                    return true;
                }

                return false;
            }
            
            return true;
        }

        private static bool IsRelativeDiskLocation(string relativeLocationReference)
        {
            string pattern = "^{.+}";
            Match match = Regex.Match(relativeLocationReference, pattern);
            return match.Success;
        }
    }
}
