// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Win32;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for <see cref="ISystemManagement"/> instances.
    /// </summary>
    public static class SystemManagementExtensions
    {
        /// <summary>
        /// Returns the package/dependency path information if it is registered.
        /// </summary>
        /// <param name="systemManagement">Provides dependencies for interfacing with the system.</param>
        /// <param name="packageName">The name of the package (e.g. openssl).</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        /// <param name="throwIfNotfound">True to throw an exception if the package does not exist on the system.</param>
        public static async Task<DependencyPath> GetPlatformSpecificPackageAsync(this ISystemManagement systemManagement, string packageName, CancellationToken cancellationToken, bool throwIfNotfound = true)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));
            packageName.ThrowIfNullOrWhiteSpace(nameof(packageName));

            IFileSystem fileSystem = systemManagement.FileSystem;
            IPackageManager packageManager = systemManagement.PackageManager;
            PlatformSpecifics platformSpecifics = systemManagement.PlatformSpecifics;

            DependencyPath platformSpecificPackage = null;
            DependencyPath package = await packageManager.GetPackageAsync(packageName, cancellationToken, throwIfNotfound);

            if (package != null)
            {
                package = platformSpecifics.ToPlatformSpecificPath(
                    package,
                    platformSpecifics.Platform,
                    platformSpecifics.CpuArchitecture);

                if (fileSystem.Directory.Exists(package.Path))
                {
                    platformSpecificPackage = package;
                }
                else if (throwIfNotfound)
                {
                    throw new DependencyException(
                        $"The package '{packageName}' exists but does not contain a folder for platform/architecture '{platformSpecifics.PlatformArchitectureName}'.",
                        ErrorReason.WorkloadDependencyMissing);
                }
            }

            return platformSpecificPackage;
        }

        /// <summary>
        /// Prepares the binary at the path specified to be executable on the OS/system platform
        /// (e.g. chmod +x on Linux).
        /// </summary>
        /// <param name="systemManagement">The system management instance.</param>
        /// <param name="filePath">The path to the binary.</param>
        /// <param name="platform">The OS platform on which the binary should be executable.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public static async Task MakeFileExecutableAsync(this ISystemManagement systemManagement, string filePath, PlatformID platform, CancellationToken cancellationToken)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));
            filePath.ThrowIfNullOrWhiteSpace(nameof(filePath));
            PlatformSpecifics.ThrowIfNotSupported(platform);

            if (!systemManagement.FileSystem.File.Exists(filePath))
            {
                throw new DependencyException($"The file at path '{filePath}' does not exist.", ErrorReason.WorkloadDependencyMissing);
            }

            switch (platform)
            {
                case PlatformID.Unix:
                    using (IProcessProxy chmod = systemManagement.ProcessManager.CreateElevatedProcess(platform, "chmod", $"+x \"{filePath}\""))
                    {
                        await chmod.StartAndWaitAsync(cancellationToken, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                        chmod.ThrowIfErrored<WorkloadException>(
                            ProcessProxy.DefaultSuccessCodes,
                            $"Failed to attribute the binary at path '{filePath}' as executable.");
                    }

                    break;
            }
        }

        /// <summary>
        /// Prepares the binaries at the path specified to be executable on the OS/system platform
        /// (e.g. chmod +x on Linux).
        /// </summary>
        /// <param name="systemManagement">The system management instance.</param>
        /// <param name="directoryPath">The path to the directory of files/binaries.</param>
        /// <param name="platform">The OS platform on which the binary should be executable.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public static async Task MakeFilesExecutableAsync(this ISystemManagement systemManagement, string directoryPath, PlatformID platform, CancellationToken cancellationToken)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));
            directoryPath.ThrowIfNullOrWhiteSpace(nameof(directoryPath));
            PlatformSpecifics.ThrowIfNotSupported(platform);

            if (!systemManagement.FileSystem.Directory.Exists(directoryPath))
            {
                throw new DependencyException($"The directory '{directoryPath}' does not exist.", ErrorReason.WorkloadDependencyMissing);
            }

            switch (platform)
            {
                case PlatformID.Unix:
                    // https://chmodcommand.com/chmod-2777/
                    // chmod 2777 sets everything to read/write/executable in the defined directory and make new file/directory inherit parent folder.
                    using (IProcessProxy chmod = systemManagement.ProcessManager.CreateElevatedProcess(platform, "chmod", $"-R 2777 \"{directoryPath}\""))
                    {
                        await chmod.StartAndWaitAsync(cancellationToken, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                        chmod.ThrowIfErrored<WorkloadException>(
                            ProcessProxy.DefaultSuccessCodes,
                            $"Failed to attribute the binaries in the directory '{directoryPath}' as executable.");
                    }

                    break;
            }
        }

        /// <summary>
        /// Reboots the operating system.
        /// </summary>
        /// <param name="systemManagement">The system management instance.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public static async Task RebootSystemAsync(this ISystemManagement systemManagement, CancellationToken cancellationToken)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));
            PlatformSpecifics.ThrowIfNotSupported(systemManagement.Platform);

            if (!cancellationToken.IsCancellationRequested)
            {
                PlatformID platform = systemManagement.Platform;
                switch (platform)
                {
                    case PlatformID.Unix:
                        using (IProcessProxy rebootSystem = systemManagement.ProcessManager.CreateElevatedProcess(platform, "shutdown -r now"))
                        {
                            await rebootSystem.StartAndWaitAsync(cancellationToken)
                                .ConfigureAwait(false);

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                rebootSystem.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.SystemOperationFailed);
                            }
                        }

                        break;

                    case PlatformID.Win32NT:
                        using (IProcessProxy rebootSystem = systemManagement.ProcessManager.CreateElevatedProcess(platform, "shutdown.exe", "-r -t 0"))
                        {
                            await rebootSystem.StartAndWaitAsync(cancellationToken)
                                .ConfigureAwait(false);

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                rebootSystem.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.SystemOperationFailed);
                            }
                        }

                        break;
                }
            }
        }
    }
}
