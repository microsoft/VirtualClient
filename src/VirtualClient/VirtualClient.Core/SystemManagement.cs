// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides components and services necessary for interacting with the local
    /// system and environment.
    /// </summary>
    public class SystemManagement : ISystemManagement
    {
        internal SystemManagement()
        {
        }

        /// <summary>
        /// A set of one or more cleanup tasks registered to execute on application
        /// shutdown/exit.
        /// </summary>
        public static List<Action> CleanupTasks { get; } = new List<Action>();

        /// <summary>
        /// Set to true to request a system reboot during profile execution steps.
        /// </summary>
        public static bool IsRebootRequested { get; set; }

        /// <inheritdoc />
        public string AgentId { get; internal set; }

        /// <inheritdoc />
        public Architecture CpuArchitecture
        {
            get
            {
                return this.PlatformSpecifics.CpuArchitecture;
            }
        }

        /// <inheritdoc />
        public bool RunningInContainer { get; internal set; }

        /// <inheritdoc />
        public IDiskManager DiskManager { get; internal set; }

        /// <inheritdoc />
        public string ExperimentId { get; internal set; }

        /// <inheritdoc />
        public IFileSystem FileSystem { get; internal set; }

        /// <inheritdoc />
        public IFirewallManager FirewallManager { get; internal set; }

        /// <inheritdoc />
        public IPackageManager PackageManager { get; internal set; }

        /// <inheritdoc />
        public PlatformID Platform
        {
            get
            {
                return this.PlatformSpecifics.Platform;
            }
        }

        /// <inheritdoc />
        public PlatformSpecifics PlatformSpecifics { get; internal set; }

        /// <inheritdoc />
        public ProcessManager ProcessManager { get; internal set; }

        /// <summary>
        /// Provides features for managing/preserving state on the system.
        /// </summary>
        public IStateManager StateManager { get; internal set; }

        /// <summary>
        /// Returns true/false whether the IP address provided matches an IP address
        /// on the local system.
        /// </summary>
        /// <param name="ipAddress">The IP address to verify.</param>
        public bool IsLocalIPAddress(string ipAddress)
        {
            IPAddress address = IPAddress.Parse(ipAddress);
            IPHostEntry hostEntry = Dns.GetHostEntry(Dns.GetHostName());

            bool isLocal = false;
            if (IPAddress.IsLoopback(address))
            {
                isLocal = true;
            }
            else
            {
                isLocal = hostEntry.AddressList.Contains<IPAddress>(address);
            }

            return isLocal;
        }

        /// <summary>
        /// Cleans up any tracked resources.
        /// </summary>
        public static void Cleanup()
        {
            if (SystemManagement.CleanupTasks.Any())
            {
                SystemManagement.CleanupTasks.ForEach(cleanup =>
                {
                    try
                    {
                        cleanup.Invoke();
                    }
                    catch
                    {
                        // Best effort here.
                    }
                });
            }
        }

        /// <summary>
        /// Add directory to $PATH in Linux and environment variable PATH for Windows.
        /// </summary>
        public void AddDirectoryToPath(string directory, EnvironmentVariableTarget environmentVariableTarget = EnvironmentVariableTarget.Process)
        {
            string originalPath = Environment.GetEnvironmentVariable("PATH", environmentVariableTarget);
            string newPath = string.Empty;
            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    originalPath = originalPath?.TrimEnd(';');
                    newPath = $"{originalPath};{directory}";
                    break;

                case PlatformID.Unix:
                    originalPath = originalPath?.TrimEnd(':');
                    newPath = $"{originalPath}:{directory}";
                    break;
            }

            Environment.SetEnvironmentVariable("PATH", newPath, environmentVariableTarget);
        }
        
        /// <summary>
        /// Refresh Environment variables on command line.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel operation.</param>
        /// <returns></returns>
        public async Task RefreshEnvironmentVariableAsync(CancellationToken cancellationToken)
        {
            string scriptPath = this.PlatformSpecifics.GetScriptPath();
            if (this.Platform == PlatformID.Win32NT)
            {
                using (IProcessProxy process = this.ProcessManager.CreateElevatedProcess(
                        this.Platform, "RefreshEnv.cmd", scriptPath))
                {
                    await process.StartAndWaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        process.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.SystemOperationFailed);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the enviroment variable associated with given target.
        /// </summary>
        /// <param name="environmentVariableName">Name of the environment variable</param>
        /// <param name="environmentVariableTarget">EnvironmentVariable target (User/Machine/Process)</param>
        /// <returns>Environment variable string.</returns>
        public string GetEnvironmentVariable(string environmentVariableName, EnvironmentVariableTarget environmentVariableTarget)
        {
            return Environment.GetEnvironmentVariable(environmentVariableName, environmentVariableTarget);
        }

        /// <summary>
        /// Returns the total memory (in kilobytes) installed/available on the system.
        /// </summary>
        public long GetTotalSystemMemoryKiloBytes()
        {
            PlatformSpecifics.ThrowIfNotSupported(this.Platform);
            long systemMemoryInKb = 0;

            switch (this.Platform)
            {
                case PlatformID.Win32NT:

                    // Note on choice of interop:
                    // We found cases where the interop GetPhysicallyInstalledSystemMemory(out totalKilobytes) experiences
                    // an error on systems given certain memory usage situations. This causes the application to throw an
                    // exception and to fail. The interop GlobalMemoryStatusEx(memoryStatus) API call seems to be more stable
                    // as well as it provides more information should we need it at some point in the future.
                    // (see https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-getphysicallyinstalledsystemmemory)
                    // (see https://docs.microsoft.com/en-us/windows/win32/api/sysinfoapi/nf-sysinfoapi-globalmemorystatusex).
                    WindowsInterop.MEMORYSTATUSEX memoryStatus = new WindowsInterop.MEMORYSTATUSEX();
                    WindowsInterop.GlobalMemoryStatusEx(memoryStatus);

                    if (memoryStatus == null)
                    {
                        throw new DependencyException(
                            $"Windows interop call failed. The application could not retrieve the total system memory installed.",
                            ErrorReason.SystemMemoryReadFailed);
                    }

                    systemMemoryInKb = (long)memoryStatus.ullTotalPhys / 1024;
                    break;

                case PlatformID.Unix:
                    using (IProcessProxy free = this.ProcessManager.CreateProcess("free"))
                    {
                        free.StartAndWaitAsync(CancellationToken.None).GetAwaiter().GetResult();
                        free.ThrowIfErrored<DependencyException>(
                            ProcessProxy.DefaultSuccessCodes,
                            $"Linux 'free' command call failed. The application could not retrieve the total system memory installed.",
                            ErrorReason.SystemMemoryReadFailed);

                        Match memoryOutput = Regex.Match(free.StandardOutput.ToString(), @"Mem:\s*([0-9]+)", RegexOptions.IgnoreCase);
                        if (!memoryOutput.Success)
                        {
                            throw new DependencyException(
                            $"Linux 'free' call returned invalid results. The application could not retrieve the total system memory installed.",
                            ErrorReason.SystemMemoryReadFailed);
                        }

                        systemMemoryInKb = long.Parse(memoryOutput.Groups[1].Value.Trim());
                    }

                    break;
            }

            return systemMemoryInKb;
        }

        /// <summary>
        /// Returns the core counts on the system.
        /// </summary>
        public int GetSystemCoreCount()
        {
            return Environment.ProcessorCount;
        }

        /// <inheritdoc />
        public async Task WaitAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public async Task WaitAsync(DateTime timeout, CancellationToken cancellationToken)
        {
            DateTime effectiveTimeout = timeout;
            if (timeout.Kind != DateTimeKind.Utc)
            {
                effectiveTimeout = timeout.ToUniversalTime();
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                if (DateTime.UtcNow >= effectiveTimeout)
                {
                    break;
                }

                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <inheritdoc />
        public Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return Task.Delay(timeout, cancellationToken);
        }

        /// <inheritdoc/>
        public async Task<LinuxDistributionInfo> GetLinuxDistributionAsync(CancellationToken cancellationToken)
        {
            using (IProcessProxy process = this.ProcessManager.CreateElevatedProcess(PlatformID.Unix, "hostnamectl", string.Empty, Environment.CurrentDirectory))
            {
                await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                process.ThrowIfErrored<ProcessException>(ProcessProxy.DefaultSuccessCodes, "hostnamectl failed.", errorReason: ErrorReason.LinuxDistributionNotSupported);
                HostnamectlParser parser = new HostnamectlParser(process.StandardOutput.ToString());
                return parser.Parse();
            }
        }

        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "Interop code.")]
        internal static class WindowsInterop
        {
            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1313:Parameter names should begin with lower-case letter", Justification = "Windows Interop")]
            public static extern bool GetPhysicallyInstalledSystemMemory(out ulong TotalMemoryInKilobytes);

            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.SysUInt)]
            public static extern uint GetLastError();

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public struct IP_ADDR_STRING
            {
                public IntPtr Next;
                public IP_ADDRESS_STRING IpAddress;
                public IP_ADDRESS_STRING IpMask;
                public int Context;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            public struct IP_ADDRESS_STRING
            {
                [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
                public string Address;
            }

            /// <summary>
            /// contains information about the current state of both physical and virtual memory, including extended memory
            /// </summary>
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Interop struct")]
            [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "Interop struct")]
            public class MEMORYSTATUSEX
            {
                /// <summary>
                /// Size of the structure, in bytes. You must set this member before calling GlobalMemoryStatusEx.
                /// </summary>
                public uint dwLength;

                /// <summary>
                /// Number between 0 and 100 that specifies the approximate percentage of physical memory that is in use (0 indicates no memory use and 100 indicates full memory use).
                /// </summary>
                public uint dwMemoryLoad;

                /// <summary>
                /// Total size of physical memory, in bytes.
                /// </summary>
                public ulong ullTotalPhys;

                /// <summary>
                /// Size of physical memory available, in bytes.
                /// </summary>
                public ulong ullAvailPhys;

                /// <summary>
                /// Size of the committed memory limit, in bytes. This is physical memory plus the size of the page file, minus a small overhead.
                /// </summary>
                public ulong ullTotalPageFile;

                /// <summary>
                /// Size of available memory to commit, in bytes. The limit is ullTotalPageFile.
                /// </summary>
                public ulong ullAvailPageFile;

                /// <summary>
                /// Total size of the user mode portion of the virtual address space of the calling process, in bytes.
                /// </summary>
                public ulong ullTotalVirtual;

                /// <summary>
                /// Size of unreserved and uncommitted memory in the user mode portion of the virtual address space of the calling process, in bytes.
                /// </summary>
                public ulong ullAvailVirtual;

                /// <summary>
                /// Size of unreserved and uncommitted memory in the extended portion of the virtual address space of the calling process, in bytes.
                /// </summary>
                public ulong ullAvailExtendedVirtual;

                /// <summary>
                /// Initializes a new instance of the <see cref="T:MEMORYSTATUSEX"/> class.
                /// </summary>
                public MEMORYSTATUSEX()
                {
                    this.dwLength = (uint)Marshal.SizeOf(typeof(WindowsInterop.MEMORYSTATUSEX));
                }
            }
        }
    }
}