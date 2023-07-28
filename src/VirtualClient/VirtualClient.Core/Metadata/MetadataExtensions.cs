// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Win32;
    using Polly;
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
        /// Returns metadata contract information for the CPU/processor components on the system.
        /// </summary>
        /// <param name="systemManagement">Provides features for interaction with the system on which the application is running.</param>
        /// <param name="logger">A logger that can be used to capture error information.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public static async Task<IDictionary<string, object>> GetCpuMetadataAsync(this ISystemManagement systemManagement, ILogger logger = null, CancellationToken cancellationToken = default)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));

            IDictionary<string, object> metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    CpuInfo cpuInfo = await systemManagement.GetCpuInfoAsync(CancellationToken.None);
                    Match cpuVendor = Regex.Match(cpuInfo.Description, "(Intel|AMD|ARM)", RegexOptions.IgnoreCase);
                    Match cpuFamily = Regex.Match(cpuInfo.Description, @"Family\s+([a-z0-9]+)", RegexOptions.IgnoreCase);
                    Match cpuStepping = Regex.Match(cpuInfo.Description, @"Stepping\s+([a-z0-9]+)", RegexOptions.IgnoreCase);

                    metadata.Add("cpuArchitecture", systemManagement.CpuArchitecture.ToString().ToUpperInvariant()); // X64, X84, ARM, ARM64
                    metadata.Add("cpuSockets", cpuInfo.SocketCount);
                    metadata.Add("cpuPhysicalCores", cpuInfo.PhysicalCoreCount);
                    metadata.Add("cpuPhysicalCoresPerSocket", cpuInfo.PhysicalCoreCount / cpuInfo.SocketCount);
                    metadata.Add("cpuLogicalProcessors", cpuInfo.LogicalCoreCount);
                    metadata.Add("cpuLogicalProcessorsPerCore", cpuInfo.LogicalCoreCount / cpuInfo.SocketCount);
                    metadata.Add("cpuVendor", cpuVendor.Success ? cpuVendor.Groups[1].Value?.Trim() : null);
                    metadata.Add("cpuFamily", cpuFamily.Success ? cpuFamily.Groups[1].Value?.Trim() : null);
                    metadata.Add("cpuStepping", cpuStepping.Success ? cpuStepping.Groups[1].Value?.Trim() : null);
                    metadata.Add("cpuModel", cpuInfo.Name);
                    metadata.Add("cpuModelDescription", cpuInfo.Description);
                    metadata.Add("numaNodes", cpuInfo.NumaNodeCount);

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
                }
            }
            catch (Exception exc)
            {
                // Best effort. VC should not crash.
                logger?.LogMessage("SystemManagement.GetCpuMetadataError", LogLevel.Warning, EventContext.Persisted().AddError(exc));
            }

            return metadata;
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

                    string osName = null;
                    if (systemManagement.Platform == PlatformID.Win32NT)
                    {
                        osName = "Windows";
                    }
                    else if (systemManagement.Platform == PlatformID.Unix)
                    {
                        LinuxDistributionInfo distro = await systemManagement.GetLinuxDistributionAsync(CancellationToken.None);
                        osName = distro.LinuxDistribution.ToString();
                    }

                    metadata.Add("computerName", Environment.MachineName);
                    metadata.Add("osFamily", osFamily);
                    metadata.Add("osName", osName);
                    metadata.Add("osDescription", Environment.OSVersion.VersionString);
                    metadata.Add("osVersion", Environment.OSVersion.Version.ToString());
                    metadata.Add("osPlatformArchitecture", systemManagement.PlatformArchitectureName);
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
        /// Returns metadata contract information for the memory components on the system.
        /// </summary>
        /// <param name="systemManagement">Provides features for interaction with the system on which the application is running.</param>
        /// <param name="logger">A logger that can be used to capture error information.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public static async Task<IDictionary<string, object>> GetMemoryMetadataAsync(this ISystemManagement systemManagement, ILogger logger = null, CancellationToken cancellationToken = default)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));
            IDictionary<string, object> metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    MemoryInfo memoryInfo = await systemManagement.GetMemoryInfoAsync(CancellationToken.None);
                    metadata.Add("memoryBytes", memoryInfo.TotalMemory);

                    if (memoryInfo.Chips?.Any() == true)
                    {
                        // memoryManufacturerChip1
                        // memoryBytesChip1
                        // memorySpeedChip1
                        // memoryPartNumberChip1
                        // memoryManufacturerChip2
                        // memoryBytesChip2
                        // memorySpeedChip2
                        // memoryPartNumberChip2
                        int memoryIndex = 0;
                        foreach (MemoryChipInfo chipInfo in memoryInfo.Chips)
                        {
                            memoryIndex++;
                            metadata.Add($"memoryManufacturerChip{memoryIndex}", chipInfo.Manufacturer);
                            metadata.Add($"memoryBytesChip{memoryIndex}", chipInfo.Capacity);
                            metadata.Add($"memorySpeedChip{memoryIndex}", chipInfo.Speed);
                            metadata.Add($"memoryPartNumberChip{memoryIndex}", chipInfo.PartNumber);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                // Best effort. VC should not crash.
                logger?.LogMessage("SystemManagement.GetMemoryMetadataError", LogLevel.Warning, EventContext.Persisted().AddError(exc));
            }

            return metadata;
        }

        /// <summary>
        /// Returns metadata contract information for the local network interfaces/cards on the system.
        /// </summary>
        /// <param name="systemManagement">Provides features for interaction with the system on which the application is running.</param>
        /// <param name="logger">A logger that can be used to capture error information.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public static async Task<IDictionary<string, object>> GetNetworkInterfaceMetadataAsync(this ISystemManagement systemManagement, ILogger logger = null, CancellationToken cancellationToken = default)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));
            IDictionary<string, object> metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    if (systemManagement.Platform == PlatformID.Win32NT)
                    {
                        var networkCardsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkCards", false);
                        if (networkCardsKey != null)
                        {
                            string[] keyNames = networkCardsKey.GetSubKeyNames();
                            if (keyNames?.Any() == true)
                            {
                                int cardIndex = 0;
                                foreach (string key in keyNames)
                                {
                                    var specificNetworkCardKey = networkCardsKey.OpenSubKey(key);
                                    if (specificNetworkCardKey != null)
                                    {
                                        cardIndex++;
                                        object cardDescription = specificNetworkCardKey.GetValue("Description");

                                        if (cardDescription != null)
                                        {
                                            metadata[$"networkInterface{cardIndex}"] = cardDescription.ToString();
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (systemManagement.Platform == PlatformID.Unix)
                    {
                        IAsyncPolicy<int> retryPolicy = Policy.HandleResult<int>(exitCode => exitCode != 0).WaitAndRetryAsync(3, retries => TimeSpan.FromSeconds(retries));

                        using (IProcessProxy lspci = systemManagement.ProcessManager.CreateProcess("lspci"))
                        {
                            // We will retry a few times if the process returns an exit code that is a non-success/non-zero value.
                            await retryPolicy.ExecuteAsync(async () =>
                            {
                                await lspci.StartAndWaitAsync(cancellationToken);
                                return lspci.ExitCode;
                            });

                            string pciDevices = lspci.StandardOutput?.ToString();
                            if (!string.IsNullOrWhiteSpace(pciDevices))
                            {
                                Regex networkControllerExpression = new Regex(@"Network\s+controller\:\s*([\x20-\x7E]+)", RegexOptions.IgnoreCase);
                                MatchCollection matches1 = networkControllerExpression.Matches(pciDevices);

                                int interfaceIndex = 0;
                                if (matches1?.Any() == true)
                                {
                                    foreach (Match match in matches1)
                                    {
                                        interfaceIndex++;
                                        metadata.Add($"networkInterface{interfaceIndex}", match.Groups[1].Value?.ToString().Trim());
                                    }
                                }

                                // On VM systems, there will not necessarily be a Network Controller
                                // presented, but an ethernet controller may be.
                                Regex ethernetControllerExpression = new Regex(@"Ethernet\s+controller\:\s*([\x20-\x7E]+)", RegexOptions.IgnoreCase);
                                MatchCollection matches2 = ethernetControllerExpression.Matches(pciDevices);

                                if (matches2?.Any() == true)
                                {
                                    foreach (Match match in matches2)
                                    {
                                        interfaceIndex++;
                                        metadata.Add($"networkInterface{interfaceIndex}", match.Groups[1].Value?.ToString().Trim());
                                    }
                                }
                            }
                        }
                    }

                    metadata["networkAccelerationEnabled"] = false;
                    if (metadata.Values.Any(desc => desc?.ToString().Contains("Mellanox", StringComparison.OrdinalIgnoreCase) == true))
                    {
                        metadata["networkAccelerationEnabled"] = true;
                    }
                }
            }
            catch (Exception exc)
            {
                // Best effort. VC should not crash.
                logger?.LogMessage("SystemManagement.GetNetworkInterfaceMetadataError", LogLevel.Warning, EventContext.Persisted().AddError(exc));
            }

            return metadata;
        }
    }
}
