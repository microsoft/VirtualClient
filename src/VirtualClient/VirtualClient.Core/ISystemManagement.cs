// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.IO.Abstractions;
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
        /// Provides features for creating and managing processes on the system.
        /// </summary>
        ISshClientManager SshClientManager { get; }
    }
}