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
        /// Returns the total memory (in kilobytes) installed/available on the system.
        /// </summary>
        /// <returns>Total system memory in KiloBytes</returns>
        public long GetTotalSystemMemoryKiloBytes();

        /// <summary>
        /// Returns the core counts on the system.
        /// </summary>
        /// <returns>System core count.</returns>
        public int GetSystemCoreCount();

        /// <summary>
        /// Read local Linux Distribution.
        /// </summary>
        /// <returns>Linux Distribution information.</returns>
        public Task<LinuxDistributionInfo> GetLinuxDistributionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Add directory to $PATH in Linux and environment variable PATH for Windows.
        /// </summary>
        public void AddToPathEnvironmentVariable(string directory, EnvironmentVariableTarget environmentVariableTarget = EnvironmentVariableTarget.Process);

        /// <summary>
        /// Refresh Environment variables.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        public Task RefreshEnvironmentVariableAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns the environment variable associated with given target.
        /// </summary>
        /// <param name="environmentVariableName">Name of the environment variable</param>
        /// <param name="environmentVariableTarget">EnvironmentVariable target (User/Machine/Process)</param>
        /// <returns>String of environment variable.</returns>
        public string GetEnvironmentVariable(string environmentVariableName, EnvironmentVariableTarget environmentVariableTarget);
    }
}