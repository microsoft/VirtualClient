// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using MathNet.Numerics.Distributions;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Installs packages from a remote location using the 'wget' toolset.
    /// </summary>
    public class WgetPackageInstallation : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="WgetPackageInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public WgetPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;
            this.packageManager = this.systemManagement.PackageManager;
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
        }

        /// <summary>
        /// Parameter describes the URI to the package to download and install.
        /// </summary>
        public Uri PackageUri
        {
            get
            {
                return new Uri(this.Parameters.GetValue<string>(nameof(this.PackageUri)));
            }
        }

        /// <summary>
        /// Parameter describes a subpath within the extracted package to include when
        /// registering the location of the package. Note that some packages when extracted
        /// have subdirectories within the package. This allows the author to account for these
        /// aspects.
        /// </summary>
        public string SubPath
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.SubPath), out IConvertible subPath);
                return subPath?.ToString();
            }
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Executes the operations to install the package from the remote location using the wget toolset.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext("packageName", this.PackageName);
            telemetryContext.AddContext("packageUri", this.PackageUri);

            if (!cancellationToken.IsCancellationRequested)
            {
                DependencyPath existingPackage = await this.packageManager.GetPackageAsync(this.PackageName, cancellationToken);

                if (existingPackage == null)
                {
                    DependencyPath wgetPackage = null;

                    try
                    {
                        wgetPackage = await this.GetPlatformSpecificPackageAsync(PackageManager.BuiltInWgetPackageName, cancellationToken);
                    }
                    catch (DependencyException exc)
                    {
                        throw new DependencyException(
                            $"Missing required package. The '{PackageManager.BuiltInWgetPackageName}' package does not exist. This package is expected " +
                            $"to be included with the Virtual Client. It may be necessary to use a newer version of the Virtual Client.",
                            exc,
                            ErrorReason.DependencyNotFound);
                    }

                    telemetryContext.AddContext("wgetPackagePath", wgetPackage.Path);

                    // We download the file or directory into the 'packages' folder. We compiled wget2 for Linux
                    // operations.
                    string wgetExe = this.Combine(wgetPackage.Path, this.Platform == PlatformID.Unix ? "wget2" : "wget.exe");

                    this.RetryPolicy = Policy.NoOpAsync();
                    await (this.RetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
                    {
                        // e.g.
                        // anypackage-1.0.0.tar.gz -> anypackage-1.0.0
                        string downloadedPackageName = PackageManager.GetArchiveFileNameWithoutExtension(this.PackageUri.Segments.Last());

                        // e.g.
                        // /packages/anypackage-1.0.0.tar.gz
                        string downloadedPackagePath = this.GetPackagePath(Path.GetFileName(this.PackageUri.ToString()));
                        string installationPath = downloadedPackagePath;
                        string packagesDirectory = this.GetPackagePath();

                        // Cleanup the file if it exists.
                        if (this.fileSystem.File.Exists(downloadedPackagePath))
                        {
                            await this.fileSystem.File.DeleteAsync(downloadedPackagePath);
                        }

                        if (this.Platform == PlatformID.Unix)
                        {
                            await this.systemManagement.MakeFileExecutableAsync(wgetExe, this.Platform, cancellationToken);

                            // wget will look for a set of shared libraries. The paths to these libraries can be configured in the
                            // LD_LIBRARY_PATH environment variable on Unix systems.
                            this.SetEnvironmentVariable(EnvironmentVariable.LD_LIBRARY_PATH, wgetPackage.Path, append: true);
                        }

                        using (IProcessProxy process = await this.ExecuteCommandAsync(wgetExe, this.PackageUri.ToString(), packagesDirectory, telemetryContext, cancellationToken))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "Wget", logToFile: true);
                                process.ThrowIfDependencyInstallationFailed();

                                // Technique:
                                // Sometimes when we extract .tar files, the directory name does not match the name of the .tar or .tar.gz file.
                                // To create some amount of consistency here, we extract the package into a "known" directory. This allows us to then
                                // rename the folder inside to a consistent name. We use the name 'download' for the internal folder name.
                                //
                                // e.g.
                                // Given PackageName = redis
                                // https://github.com/redis/redis/archive/refs/tags/6.2.1.tar.gz -> extracts into a directory /packages/redis/redis-6.2.1
                                //
                                // We then rename the folder inside to the package name defined in the profile -> /packages/redis/download
                                if (PackageManager.TryGetArchiveFileType(downloadedPackagePath, out ArchiveType archiveType))
                                {
                                    // e.g.
                                    // /packages/anypackage-1.0.0.tar.gz -> (installed in) -> /packages/anypackage/anypackage.1.0.0
                                    installationPath = this.GetPackagePath(this.PackageName);

                                    await this.packageManager.ExtractPackageAsync(downloadedPackagePath, installationPath, cancellationToken, archiveType);
                                }

                                // Note that installation path is the final path even though we are using the packages path above
                                // as the destination.
                                telemetryContext.AddContext("installationPath", installationPath);
                                telemetryContext.AddContext("subPath", this.SubPath);

                                DependencyPath packageInstalled = new DependencyPath(
                                    this.PackageName,
                                    this.Combine(installationPath, this.SubPath),
                                    metadata: new Dictionary<string, IConvertible>
                                    {
                                        ["packageUri"] = this.PackageUri.ToString()
                                    });

                                await this.packageManager.RegisterPackageAsync(packageInstalled, cancellationToken);
                                await this.fileSystem.File.DeleteAsync(downloadedPackagePath);
                            }
                        }
                    });
                }
            }
        }
        
        /// <summary>
        /// Validates the component parameters and platform as supported.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();

            if (string.IsNullOrWhiteSpace(this.PackageName))
            {
                throw new DependencyException(
                    $"Package name not defined. The '{nameof(this.PackageName)}' parameter must be defined for the component.",
                    ErrorReason.DependencyDescriptionInvalid);
            }
        }
    }
}
