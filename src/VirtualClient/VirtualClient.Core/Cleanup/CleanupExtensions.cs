// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Cleanup
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods that support runtime cleanup operations.
    /// </summary>
    public static class CleanupExtensions
    {
        /// <summary>
        /// Cleans the default "logs" directory provided deleting any files and folders that are beyond the
        /// defined retention period.
        /// </summary>
        /// <param name="systemManagement">The system management instance.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retentionDate">A retention date to apply to files within the directory. Any files created within this retention date are preserved.</param>
        public static async Task CleanLogsDirectoryAsync(this ISystemManagement systemManagement, CancellationToken cancellationToken, DateTime? retentionDate = null)
        {
            IFileSystem fileSystem = systemManagement.FileSystem;
            string logsDirectory = systemManagement.PlatformSpecifics.GetLogsPath();

            if (fileSystem.Directory.Exists(logsDirectory))
            {
                IEnumerable<string> logFiles = fileSystem.Directory.EnumerateFiles(logsDirectory, "*.*", SearchOption.AllDirectories);
                await CleanupExtensions.DeleteFilesAsync(fileSystem, logFiles, retentionDate, cancellationToken);

                IEnumerable<string> logDirectories = fileSystem.Directory.EnumerateDirectories(logsDirectory, "*.*", SearchOption.AllDirectories);
                await CleanupExtensions.DeleteDirectoriesAsync(fileSystem, logDirectories, retentionDate, cancellationToken);
            }
        }

        /// <summary>
        /// Cleans the default "packages" directory provided deleting any files and folders with the exception
        /// of packages defined as "built-in" that are part of the Virtual Client package itself.
        /// </summary>
        /// <param name="systemManagement">The system management instance.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public static async Task CleanPackagesDirectoryAsync(this ISystemManagement systemManagement, CancellationToken cancellationToken)
        {
            IFileSystem fileSystem = systemManagement.FileSystem;
            string packagesDirectory = systemManagement.PlatformSpecifics.GetPackagePath();

            if (fileSystem.Directory.Exists(packagesDirectory))
            {
                IEnumerable<string> directories = fileSystem.Directory.EnumerateDirectories(packagesDirectory, "*.*", System.IO.SearchOption.TopDirectoryOnly);
                if (directories?.Any() == true)
                {
                    foreach (string directory in directories)
                    {
                        await fileSystem.Directory.DeleteAsync(directory, true);
                    }
                }

                IEnumerable<string> files = fileSystem.Directory.EnumerateFiles(packagesDirectory, "*.*", System.IO.SearchOption.TopDirectoryOnly);
                if (files?.Any() == true)
                {
                    foreach (string file in files)
                    {
                        await fileSystem.File.DeleteAsync(file);
                    }
                }
            }
        }

        /// <summary>
        /// Cleans the default "state" directory provided deleting any files and folders.
        /// </summary>
        /// <param name="systemManagement">The system management instance.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public static async Task CleanStateDirectoryAsync(this ISystemManagement systemManagement, CancellationToken cancellationToken)
        {
            IFileSystem fileSystem = systemManagement.FileSystem;
            string stateDirectory = systemManagement.PlatformSpecifics.GetStatePath();

            if (fileSystem.Directory.Exists(stateDirectory))
            {
                IEnumerable<string> stateFiles = fileSystem.Directory.EnumerateFiles(stateDirectory, "*.*", SearchOption.AllDirectories);
                await CleanupExtensions.DeleteFilesAsync(fileSystem, stateFiles, null, cancellationToken);

                IEnumerable<string> stateDirectories = fileSystem.Directory.EnumerateDirectories(stateDirectory, "*.*", SearchOption.AllDirectories);
                await CleanupExtensions.DeleteDirectoriesAsync(fileSystem, stateDirectories, null, cancellationToken);
            }
        }

        private static async Task DeleteDirectoriesAsync(IFileSystem fileSystem, IEnumerable<string> directories, DateTime? retentionDate, CancellationToken cancellationToken)
        {
            if (directories?.Any() == true)
            {
                foreach (string directory in directories.OrderByDescending(path => path.Length))
                {
                    if (retentionDate != null)
                    {
                        // Leave any files that are within the retention period/date.
                        DateTime directoryCreationDate = fileSystem.Directory.GetCreationTimeUtc(directory);
                        if (directoryCreationDate >= retentionDate.Value)
                        {
                            continue;
                        }
                    }

                    await fileSystem.Directory.DeleteAsync(directory, true);
                }
            }
        }

        private static async Task DeleteFilesAsync(IFileSystem fileSystem, IEnumerable<string> files, DateTime? retentionDate, CancellationToken cancellationToken)
        {
            if (files?.Any() == true)
            {
                foreach (string file in files)
                {
                    if (retentionDate != null)
                    {
                        // Leave any files that are within the retention period/date.
                        DateTime fileCreationDate = fileSystem.File.GetCreationTimeUtc(file);
                        if (fileCreationDate >= retentionDate.Value)
                        {
                            continue;
                        }
                    }

                    await fileSystem.File.DeleteAsync(file);
                }
            }
        }
    }
}
