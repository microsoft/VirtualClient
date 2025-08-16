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
        /// Provides features for creating and managing processes on the system.
        /// </summary>
        ISshClientFactory SshClientFactory { get; }

        /// <summary>
        /// Provides features for managing/preserving state on the system.
        /// </summary>
        IStateManager StateManager { get; }

        /// <summary>
        /// Overwrite the default of 260 char in windows file path length to 32,767.
        /// https://learn.microsoft.com/en-us/windows/win32/fileio/maximum-file-path-limitation?tabs=registry
        /// </summary>
        void EnableLongPathInWindows();
    }
}