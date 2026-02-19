// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
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

        /// <summary>
        /// Sets the directory (and any subdirectories or files) to allow full permissions on the system to any
        /// user or group (e.g. chmod -R 777 on Linux).
        /// <list type="bullet">
        /// <item>https://linuxhandbook.com/linux-file-permissions/</item>
        /// <item>https://chmodcommand.com/chmod-777/</item>
        /// </list>
        /// </summary>
        /// <param name="systemManagement">The system management instance.</param>
        /// <param name="directoryPath">The path to the directory.</param>
        /// <param name="platform">The OS platform on which the binary should be executable.</param>
        /// <param name="telemetryContext">Provides context information for telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="owner">Defines a user to apply to the directory structure as owner.</param>
        /// <param name="logger">A logger to use for capturing telemetry.</param>
        public static async Task SetFullPermissionsAsync(
            this ISystemManagement systemManagement, string directoryPath, PlatformID platform, EventContext telemetryContext, CancellationToken cancellationToken, string owner = null, ILogger logger = null)
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
                    // https://chmodcommand.com/chmod-777/
                    // https://linuxhandbook.com/linux-file-permissions/

                    EventContext relatedContext = telemetryContext.Clone();
                    relatedContext.AddContext("directory", directoryPath);
                    relatedContext.AddContext("owner", owner);
                    relatedContext.AddContext("permissions", "777");

                    logger?.LogMessage($"SetFullPermissions (directory = '{directoryPath}', owner = '{owner}')", relatedContext);
                    
                    using (IProcessProxy chmod = systemManagement.ProcessManager.CreateProcess("sudo", $"chmod -R 777 \"{directoryPath}\""))
                    {
                        await chmod.StartAndWaitAsync(cancellationToken, TimeSpan.FromSeconds(30));

                        chmod.ThrowIfErrored<WorkloadException>(
                            ProcessProxy.DefaultSuccessCodes,
                            $"Failed to attribute the directory '{directoryPath}' with full permissions.");
                    }

                    if (!string.IsNullOrWhiteSpace(owner))
                    {
                        using (IProcessProxy chown = systemManagement.ProcessManager.CreateProcess("sudo", $"chown {owner}:{owner} \"{directoryPath}\""))
                        {
                            await chown.StartAndWaitAsync(cancellationToken, TimeSpan.FromSeconds(30));

                            chown.ThrowIfErrored<WorkloadException>(
                                ProcessProxy.DefaultSuccessCodes,
                                $"Failed to set owner for the directory '{directoryPath}' to '{owner}'.");
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Sets the logger for the <see cref="ISystemManagement"/> instance and underlying dependencies.
        /// </summary>
        /// <param name="systemManagement">The system management instance.</param>
        /// <param name="logger">The logger to set.</param>
        public static void SetLogger(this ISystemManagement systemManagement, ILogger logger)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));
            logger.ThrowIfNull(nameof(logger));

            DiskManager diskManager = systemManagement.DiskManager as DiskManager;
            if (diskManager != null)
            {
                diskManager.Logger = logger;
            }

            PackageManager packageManager = systemManagement.PackageManager as PackageManager;
            if (packageManager != null)
            {
                packageManager.Logger = logger;
            }
        }
    }
}
