// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for <see cref="MetadataExtensions"/> instances.
    /// </summary>
    public static class MetadataExtensions
    {
        /// <summary>
        /// Returns metadata contract information for CPU hardware parts on the system.
        /// </summary>
        /// <param name="systemManagement">Provides features for interaction with the system on which the application is running.</param>
        /// <param name="logger">A logger that can be used to capture error information.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public static async Task<IEnumerable<IDictionary<string, object>>> GetCpuPartsMetadataAsync(this ISystemManagement systemManagement, ILogger logger = null, CancellationToken cancellationToken = default)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));
            List<IDictionary<string, object>> parts = new List<IDictionary<string, object>>();

            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    CpuInfo cpuInfo = await systemManagement.GetCpuInfoAsync(CancellationToken.None);
                    Match cpuVendor = Regex.Match(cpuInfo.Description, "(Intel|AMD|ARM)", RegexOptions.IgnoreCase);
                    Match cpuFamily = Regex.Match(cpuInfo.Description, @"Family\s+([a-z0-9]+)", RegexOptions.IgnoreCase);
                    Match cpuStepping = Regex.Match(cpuInfo.Description, @"Stepping\s+([a-z0-9]+)", RegexOptions.IgnoreCase);

                    parts.Add(new Dictionary<string, object>
                    {
                        { "type", "CPU" },
                        { "vendor",  cpuVendor.Success ? cpuVendor.Groups[1].Value?.Trim() : null },
                        { "description", cpuInfo.Description },
                        { "family", cpuFamily.Success ? cpuFamily.Groups[1].Value?.Trim() : null },
                        { "stepping", cpuStepping.Success ? cpuStepping.Groups[1].Value?.Trim() : null },
                        { "model", cpuInfo.Name },
                    });
                }
            }
            catch (Exception exc)
            {
                // Best effort. VC should not crash.
                logger?.LogMessage("SystemManagement.GetCpuPartsMetadataError", LogLevel.Warning, EventContext.Persisted().AddError(exc));
            }

            return parts;
        }

        /// <summary>
        /// Returns metadata contract information for hardware parts on the system.
        /// </summary>
        /// <param name="systemManagement">Provides features for interaction with the system on which the application is running.</param>
        /// <param name="logger">A logger that can be used to capture error information.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public static async Task<IDictionary<string, object>> GetHardwarePartsMetadataAsync(this ISystemManagement systemManagement, ILogger logger = null, CancellationToken cancellationToken = default)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));
            IDictionary<string, object> metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            List<IDictionary<string, object>> parts = new List<IDictionary<string, object>>();

            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    IEnumerable<IDictionary<string, object>> cpuParts = await systemManagement.GetCpuPartsMetadataAsync(logger, cancellationToken);
                    if (cpuParts?.Any() == true)
                    {
                        parts.AddRange(cpuParts);
                    }

                    IEnumerable<IDictionary<string, object>> memoryParts = await systemManagement.GetMemoryPartsMetadataAsync(logger, cancellationToken);
                    if (memoryParts?.Any() == true)
                    {
                        parts.AddRange(memoryParts);
                    }

                    IEnumerable<IDictionary<string, object>> networkParts = await systemManagement.GetNetworkPartsMetadataAsync(logger, cancellationToken);
                    if (networkParts?.Any() == true)
                    {
                        parts.AddRange(networkParts);
                    }

                    if (parts.Any())
                    {
                        metadata["parts"] = parts;
                    }
                }
            }
            catch (Exception exc)
            {
                // Best effort. VC should not crash.
                logger?.LogMessage("SystemManagement.GetHardwarePartsMetadataError", LogLevel.Warning, EventContext.Persisted().AddError(exc));
            }

            return metadata;
        }

        /// <summary>
        /// Returns metadata contract information for memory/chip hardware parts on the system.
        /// </summary>
        /// <param name="systemManagement">Provides features for interaction with the system on which the application is running.</param>
        /// <param name="logger">A logger that can be used to capture error information.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public static async Task<IEnumerable<IDictionary<string, object>>> GetMemoryPartsMetadataAsync(this ISystemManagement systemManagement, ILogger logger = null, CancellationToken cancellationToken = default)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));
            List<IDictionary<string, object>> parts = new List<IDictionary<string, object>>();

            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    MemoryInfo memoryInfo = await systemManagement.GetMemoryInfoAsync(CancellationToken.None);

                    if (memoryInfo.Chips?.Any() == true)
                    {
                        foreach (MemoryChipInfo chipInfo in memoryInfo.Chips)
                        {
                            parts.Add(new Dictionary<string, object>
                            {
                                { "type", "Memory" },
                                { "vendor", chipInfo.Manufacturer },
                                { "description", $"{chipInfo.Manufacturer} {chipInfo.PartNumber}" },
                                { "bytes", chipInfo.Capacity },
                                { "speed", chipInfo.Speed },
                                { "partNumber", chipInfo.PartNumber }
                            });
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                // Best effort. VC should not crash.
                logger?.LogMessage("SystemManagement.GetMemoryPartsMetadataError", LogLevel.Warning, EventContext.Persisted().AddError(exc));
            }

            return parts;
        }

        /// <summary>
        /// Returns metadata contract information for network/adapter hardware parts on the system.
        /// </summary>
        /// <param name="systemManagement">Provides features for interaction with the system on which the application is running.</param>
        /// <param name="logger">A logger that can be used to capture error information.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public static async Task<IEnumerable<IDictionary<string, object>>> GetNetworkPartsMetadataAsync(this ISystemManagement systemManagement, ILogger logger = null, CancellationToken cancellationToken = default)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));
            List<IDictionary<string, object>> parts = new List<IDictionary<string, object>>();

            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    NetworkInfo networkInfo = await systemManagement.GetNetworkInfoAsync(cancellationToken);

                    if (networkInfo?.Interfaces?.Any() == true)
                    {
                        foreach (NetworkInterfaceInfo networkInterface in networkInfo.Interfaces)
                        {
                            parts.Add(new Dictionary<string, object>
                            {
                                { "type", "Network" },
                                { "vendor", Regex.Match(networkInterface.Description, "([a-z0-9]+)", RegexOptions.IgnoreCase)?.Groups[1].Value.Trim() },
                                { "description", networkInterface.Description }
                            });
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                // Best effort. VC should not crash.
                logger?.LogMessage("SystemManagement.GetNetworkPartsMetadataError", LogLevel.Warning, EventContext.Persisted().AddError(exc));
            }

            return parts;
        }

        /// <summary>
        /// Returns metadata contract information for the host itself (e.g. physical node, VM).
        /// </summary>
        /// <param name="systemManagement">Provides features for interaction with the system on which the application is running.</param>
        /// <param name="logger">A logger that can be used to capture error information.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public static async Task<IDictionary<string, object>> GetHostMetadataAsync(this ISystemManagement systemManagement, ILogger logger = null, CancellationToken cancellationToken = default)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));
            IDictionary<string, object> metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    string osFamily = "Other";
                    switch (systemManagement.Platform)
                    {
                        case PlatformID.Unix:
                            osFamily = "Unix";
                            break;

                        case PlatformID.Win32NT:
                            osFamily = "Windows";
                            break;
                    }

                    if (systemManagement.Platform == PlatformID.Win32NT)
                    {
                        metadata.Add("osName", "Windows");
                    }
                    else if (systemManagement.Platform == PlatformID.Unix)
                    {
                        LinuxDistributionInfo distro = await systemManagement.GetLinuxDistributionAsync(CancellationToken.None);
                        metadata.Add("osName", distro.OperationSystemFullName);
                    }

                    // Operating System Metadata
                    // -------------------------------------------------
                    metadata.Add("computerName", Environment.MachineName);
                    metadata.Add("osFamily", osFamily);
                    
                    metadata.Add("osDescription", Environment.OSVersion.VersionString);
                    metadata.Add("osVersion", Environment.OSVersion.Version.ToString());
                    metadata.Add("osPlatformArchitecture", systemManagement.PlatformArchitectureName);

                    // CPU/Processor System Metadata
                    // -------------------------------------------------
                    CpuInfo cpuInfo = await systemManagement.GetCpuInfoAsync(CancellationToken.None);
                    Match cpuVendor = Regex.Match(cpuInfo.Description, "(Intel|AMD|ARM)", RegexOptions.IgnoreCase);
                    Match cpuFamily = Regex.Match(cpuInfo.Description, @"Family\s+([a-z0-9]+)", RegexOptions.IgnoreCase);
                    Match cpuStepping = Regex.Match(cpuInfo.Description, @"Stepping\s+([a-z0-9]+)", RegexOptions.IgnoreCase);

                    metadata.Add("cpuArchitecture", systemManagement.CpuArchitecture.ToString().ToUpperInvariant()); // X64, X84, ARM, ARM64
                    metadata.Add("cpuSockets", cpuInfo.SocketCount);
                    metadata.Add("cpuPhysicalCores", cpuInfo.PhysicalCoreCount);
                    metadata.Add("cpuPhysicalCoresPerSocket", cpuInfo.PhysicalCoreCount / cpuInfo.SocketCount);
                    metadata.Add("cpuLogicalProcessors", cpuInfo.LogicalProcessorCount);
                    metadata.Add("cpuLogicalProcessorsPerCore", cpuInfo.LogicalProcessorCountPerPhysicalCore);

                    IEnumerable<CpuCacheInfo> cpuCaches = cpuInfo.Caches;
                    if (cpuCaches?.Any() == true)
                    {
                        foreach (var cache in cpuCaches.OrderBy(cache => cache.SizeInBytes))
                        {
                            // e.g.
                            // cpuCacheBytes_L1
                            // cpuCacheBytes_L1d
                            // cpuCacheBytes_L1i
                            // cpuCacheBytes_L2
                            // cpuCacheBytes_L3
                            metadata.Add($"cpuCacheBytes_{cache.Name}", cache.SizeInBytes);
                        }

                        metadata.Add("cpuLastCacheBytes", cpuCaches.OrderByDescending(cache => cache.Name).First().SizeInBytes);
                    }

                    metadata.Add("numaNodes", cpuInfo.NumaNodeCount);

                    // System Memory Metadata
                    // -------------------------------------------------
                    MemoryInfo memoryInfo = await systemManagement.GetMemoryInfoAsync(CancellationToken.None);
                    metadata.Add("memoryBytes", memoryInfo.TotalMemory * 1024);
                }
            }
            catch (Exception exc)
            {
                // Best effort. VC should not crash.
                logger?.LogMessage("SystemManagement.GetHostMetadataError", LogLevel.Warning, EventContext.Persisted().AddError(exc));
            }

            return metadata;
        }

        /// <summary>
        /// Returns metadata contract information for installed code compilers (e.g. gcc, cc, gfortran) on the system.
        /// </summary>
        /// <param name="systemManagement">Provides features for interaction with the system on which the application is running.</param>
        /// <param name="logger">A logger that can be used to capture error information.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public static async Task<IDictionary<string, object>> GetInstalledCompilerMetadataAsync(this ISystemManagement systemManagement, ILogger logger = null, CancellationToken cancellationToken = default)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));
            IDictionary<string, object> metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            try
            {
                string ccCompilerVersion = await systemManagement.GetInstalledCompilerVersionAsync("cc", cancellationToken);
                string gccCompilerVersion = await systemManagement.GetInstalledCompilerVersionAsync("gcc", cancellationToken);
                string gfortranCompilerVersion = await systemManagement.GetInstalledCompilerVersionAsync("gfortran", cancellationToken);

                metadata.Add("compilerVersion_cc", ccCompilerVersion);
                metadata.Add("compilerVersion_gcc", gccCompilerVersion);
                metadata.Add("compilerVersion_gfortran", gfortranCompilerVersion);
            }
            catch (Exception exc)
            {
                // Best effort. VC should not crash.
                logger?.LogMessage("SystemManagement.GetInstalledCompilerMetadataError", LogLevel.Warning, EventContext.Persisted().AddError(exc));
            }

            return metadata;
        }

        private static async Task<string> GetInstalledCompilerVersionAsync(this ISystemManagement systemManagement, string compilerName, CancellationToken cancellationToken)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));

            string installedVersion = null;
            string compiler = compilerName.ToLowerInvariant();

            using (IProcessProxy process = systemManagement.ProcessManager.CreateProcess(compiler, "--version"))
            {
                await process.StartAndWaitAsync(cancellationToken);

                // The compiler toolset may NOT be installed on the system. Unless we get a success response, we do
                // not attempt to parse the compiler version.
                if (!cancellationToken.IsCancellationRequested && !process.IsErrored())
                {
                    Match versionMatch = Regex.Match(process.StandardOutput.ToString(), @"[a-z0-9\s]+\([\x20-\x7F]+\)\s*([0-9a-z\.-]+)", RegexOptions.IgnoreCase);
                    if (versionMatch.Success)
                    {
                        installedVersion = versionMatch.Groups[1].Value.Trim();
                    }
                }
            }

            return installedVersion;
        }
    }
}
