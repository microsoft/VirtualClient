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
                    string wgetExe = this.Platform == PlatformID.Unix ? "wget2" : "wget";

                    await (this.RetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
                    {
                        // e.g.
                        // anypackage-1.0.0.tar.gz -> anypackage-1.0.0
                        string downloadedPackageName = PackageManager.GetArchiveFileNameWithoutExtension(this.PackageUri.Segments.Last());

                        // e.g.
                        // /packages/anypackage-1.0.0.tar.gz
                        string downloadedPackagePath = this.GetPackagePath(Path.GetFileName(this.PackageUri.ToString()));

                        // e.g.
                        // /packages/anypackage-1.0.0
                        string installationPath = this.GetPackagePath(downloadedPackageName);
                        string workingDirectory = this.GetPackagePath();

                        // Cleanup the file if it exists.
                        if (this.fileSystem.File.Exists(downloadedPackagePath))
                        {
                            await this.fileSystem.File.DeleteAsync(downloadedPackagePath);
                        }

                        using (IProcessProxy process = await this.ExecuteCommandAsync(wgetExe, this.PackageUri.ToString(), workingDirectory, telemetryContext, cancellationToken))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "Wget", logToFile: true);
                                process.ThrowIfDependencyInstallationFailed();

                                if (PackageManager.TryGetArchiveFileType(downloadedPackagePath, out ArchiveType archiveType))
                                {
                                    await this.packageManager.ExtractPackageAsync(downloadedPackagePath, installationPath, cancellationToken, archiveType);
                                }

                                telemetryContext.AddContext("installationPath", installationPath);

                                DependencyPath packageInstalled = new DependencyPath(this.PackageName, installationPath, metadata: new Dictionary<string, IConvertible>
                                {
                                    ["packageUri"] = this.PackageUri.ToString()
                                });

                                await this.packageManager.RegisterPackageAsync(packageInstalled, cancellationToken);
                            }
                        }
                    });
                }
            }
        }
    }
}
