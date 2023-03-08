// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides components and services necessary for interacting with the local
    /// system and environment.
    /// </summary>
    public interface ISystemManagement : ISystemInfo
    {
        /// <summary>
        /// Provides features for interacting with the system disks.
        /// </summary>
        IDiskManager DiskManager { get; }

        /// <summary>
        /// Provides features for interacting with file system.
        /// </summary>
        IFileSystem FileSystem { get; }

        /// <summary>
        /// Provides features for interacting with the system firewall.
        /// </summary>
        IFirewallManager FirewallManager { get; }

        /// <summary>
        /// Provides features for managing packages in the system.
        /// </summary>
        IPackageManager PackageManager { get; }

        /// <summary>
        /// Provides features for creating and managing processes on the system.
        /// </summary>
        ProcessManager ProcessManager { get; }

        /// <summary>
        /// Provides features for managing/preserving state on the system.
        /// </summary>
        IStateManager StateManager { get; }

        /// <summary>
        /// Read local Linux Distribution.
        /// </summary>
        /// <returns>Linux Distribution information.</returns>
        Task<LinuxDistributionInfo> GetLinuxDistributionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns the total memory (in kilobytes) installed/available on the system.
        /// </summary>
        /// <returns>Total system memory in KiloBytes</returns>
        long GetTotalSystemMemoryKiloBytes();

        /// <summary>
        /// Returns the core counts on the system.
        /// </summary>
        /// <returns>System core count.</returns>
        int GetSystemCoreCount();
    }
}