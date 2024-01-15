// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents information about the system platform and architecture.
    /// </summary>
    public interface ISystemInfo
    {
        /// <summary>
        /// The ID of the Virtual Client/agent for the context of the larger
        /// experiment in operation.
        /// </summary>
        string AgentId { get; }

        /// <summary>
        /// The ID of the larger experiment in operation.
        /// </summary>
        string ExperimentId { get; }

        /// <summary>
        /// The CPU/processor architecture.
        /// </summary>
        Architecture CpuArchitecture { get; }

        /// <summary>
        /// The system OS/platform.
        /// </summary>
        PlatformID Platform { get; }

        /// <summary>
        /// The name of the platform/architecture for the system on which the application is
        /// running (e.g. linux-x64, linux-arm64, win-x64, win-arm64).
        /// </summary>
        public string PlatformArchitectureName { get; }

        /// <summary>
        /// The system OS/platform specific information.
        /// </summary>
        PlatformSpecifics PlatformSpecifics { get; }

        /// <summary>
        /// Whether VC is running in container.
        /// </summary>
        bool RunningInContainer { get; }

        /// <summary>
        /// Checks if the local IP Address is defined on current system.
        /// </summary>
        /// <param name="ipAddress">IP address present in the environment layout for the agent</param>
        /// <returns>True/False is an IP is defined on current system.</returns>
        bool IsLocalIPAddress(string ipAddress);

        /// <summary>
        /// Get the logged In Username i.e, username of the user who invoked a command with elevated privileges using the "sudo" command in Unix operating system.
        /// </summary>
        /// <returns>Username of the logged In user</returns>
        string GetLoggedInUserName();

        /// <summary>
        /// Returns information about the CPU on the system.
        /// </summary>
        Task<CpuInfo> GetCpuInfoAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns information about the specific Linux distribution (e.g. Ubuntu, CentOS).
        /// </summary>
        Task<LinuxDistributionInfo> GetLinuxDistributionAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns information about memory on the system.
        /// </summary>
        Task<MemoryInfo> GetMemoryInfoAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Returns information about network features on the system.
        /// </summary>
        Task<NetworkInfo> GetNetworkInfoAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Causes the process to idle until the operations are cancelled.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the wait operation.</param>
        Task WaitAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Causes the process to idle until the time defined by the timeout or until the operations
        /// are cancelled.
        /// </summary>
        /// <param name="timeout">The date/time at which the wait ends and execution should continue.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the wait operation.</param>
        Task WaitAsync(DateTime timeout, CancellationToken cancellationToken);

        /// <summary>
        /// Causes the process to idle for the period of time defined by the timeout or until the operations
        /// are cancelled.
        /// </summary>
        /// <param name="timeout">The maximum time to wait before continuing.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the wait operation.</param>
        Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);
    }
}
