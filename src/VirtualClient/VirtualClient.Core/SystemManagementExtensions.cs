// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net.NetworkInformation;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Win32;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for <see cref="ISystemManagement"/> instances.
    /// </summary>
    public static class SystemManagementExtensions
    {
        /// <summary>
        /// Returns a set of device drivers on the system.
        /// </summary>
        /// <param name="systemManagement">The system management instance.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>Device driver information.</returns>
        public static async Task<IEnumerable<IDictionary<string, IConvertible>>> GetSystemDetailedInfoAsync(this ISystemManagement systemManagement, CancellationToken cancellationToken)
        {
            systemManagement.ThrowIfNull(nameof(systemManagement));

            List<IDictionary<string, IConvertible>> info = new List<IDictionary<string, IConvertible>>();

            if (systemManagement.Platform == PlatformID.Win32NT)
            {
                await SystemManagementExtensions.AddWindowsSystemInfoAsync(systemManagement, info, cancellationToken)
                    .ConfigureAwait(false);

                SystemManagementExtensions.AddSystemNetworkInfo(info);
            }
            else if (systemManagement.Platform == PlatformID.Unix)
            {
                await SystemManagementExtensions.AddUnixSystemInfoAsync(systemManagement, info, cancellationToken)
                     .ConfigureAwait(false);

                SystemManagementExtensions.AddSystemNetworkInfo(info);
            }

            return info;
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

        private static void AddSystemNetworkInfo(List<IDictionary<string, IConvertible>> info)
        {
            IEnumerable<NetworkInterface> ethernetInterfaces = NetworkInterface.GetAllNetworkInterfaces()?.Where(i => i.NetworkInterfaceType == NetworkInterfaceType.Ethernet);

            if (ethernetInterfaces?.Any() == true)
            {
                List<string> interfaces = new List<string>();

                foreach (NetworkInterface networkInterface in ethernetInterfaces)
                {
                    if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        interfaces.Add($"{networkInterface.Name} (speed={networkInterface.Speed}, status={networkInterface.OperationalStatus})");
                    }
                }

                if (interfaces.Any())
                {
                    info.Add(new Dictionary<string, IConvertible>
                    {
                        ["toolset"] = ".NET SDK",
                        ["command"] = "n/a",
                        ["commandOutput"] = string.Join(Environment.NewLine, interfaces)
                    });
                }
            }
        }

        private static async Task AddUnixSystemInfoAsync(ISystemManagement systemManagement, List<IDictionary<string, IConvertible>> info, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                IAsyncPolicy<int> retryPolicy = Policy.HandleResult<int>(exitCode => exitCode != 0).WaitAndRetryAsync(3, retries => TimeSpan.FromSeconds(retries));
                using (IProcessProxy uname = systemManagement.ProcessManager.CreateProcess("uname", "--all"))
                {
                    // We will retry a few times if the process returns an exit code that is a non-success/non-zero value.
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        await uname.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        return uname.ExitCode;

                    }).ConfigureAwait(false);

                    string osInfo = uname.StandardOutput?.ToString();
                    if (!string.IsNullOrWhiteSpace(osInfo))
                    {
                        info.Add(new Dictionary<string, IConvertible>
                        {
                            ["toolset"] = "uname",
                            ["command"] = "uname --all",
                            ["commandOutput"] = osInfo.Trim()
                        });
                    }
                }

                using (IProcessProxy lspci = systemManagement.ProcessManager.CreateProcess("lspci", "-k -mm -vvv"))
                {
                    // We will retry a few times if the process returns an exit code that is a non-success/non-zero value.
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        await lspci.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        return lspci.ExitCode;

                    }).ConfigureAwait(false);

                    string pciDevices = lspci.StandardOutput?.ToString();
                    if (!string.IsNullOrWhiteSpace(pciDevices))
                    {
                        info.Add(new Dictionary<string, IConvertible>
                        {
                            ["toolset"] = "lspci",
                            ["command"] = "lspci -k -mm -vvv",
                            ["commandOutput"] = pciDevices.Trim()
                        });
                    }
                }

                using (IProcessProxy lscpu = systemManagement.ProcessManager.CreateProcess("lscpu", "-J"))
                {
                    // We will retry a few times if the process returns an exit code that is a non-success/non-zero Jvalue.
                    await retryPolicy.ExecuteAsync(async () =>
                    {
                        await lscpu.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        return lscpu.ExitCode;

                    }).ConfigureAwait(false);

                    string hardwareInfo = lscpu.StandardOutput?.ToString();
                    if (!string.IsNullOrWhiteSpace(hardwareInfo))
                    {
                        IDictionary<string, IConvertible> infoItems = new Dictionary<string, IConvertible>();
                        JObject hardwareInfoItems = JObject.Parse(hardwareInfo);
                        JToken items = hardwareInfoItems.SelectToken("lscpu");
                        foreach (JToken field in items.Children())
                        {
                            string fieldName = field["field"].Value<string>().Trim().Replace(":", string.Empty);
                            string fieldValue = field["data"].Value<string>().Trim();
                            infoItems[fieldName] = fieldValue;
                        }
                        
                        info.Add(new Dictionary<string, IConvertible>
                        {
                            ["toolset"] = "lscpu",
                            ["command"] = "lscpu -J",
                            ["commandOutput"] = infoItems.ToJson()
                        });
                    }
                }
            }
        }

        private static async Task AddWindowsSystemInfoAsync(ISystemManagement systemManagement, List<IDictionary<string, IConvertible>> info, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                IAsyncPolicy<int> retryPolicy = Policy.HandleResult<int>(exitCode => exitCode != 0).WaitAndRetryAsync(3, retries => TimeSpan.FromSeconds(retries));

                // Support for the pnputil /enum-devices command started in Windows 10. This support does not exist on
                // Windows Server 2019 but does exist on 2022+.
                bool enumDevicesSupported = false;
                using (IProcessProxy pnpUtil = systemManagement.ProcessManager.CreateProcess("pnputil", "/enum-devices /drivers"))
                {
                    await pnpUtil.StartAndWaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (pnpUtil.ExitCode == 0)
                    {
                        enumDevicesSupported = true;
                        string drivers = pnpUtil.StandardOutput?.ToString();

                        if (!string.IsNullOrWhiteSpace(drivers))
                        {
                            info.Add(new Dictionary<string, IConvertible>
                            {
                                ["toolset"] = "pnputil",
                                ["command"] = "pnputil /enum-devices /drivers",
                                ["commandOutput"] = drivers.Trim()
                            });
                        }
                    }
                }

                if (!enumDevicesSupported)
                {
                    using (IProcessProxy pnpUtil = systemManagement.ProcessManager.CreateProcess("pnputil", "/enum-drivers"))
                    {
                        // We will retry a few times if the process returns an exit code that is a non-success/non-zero value.
                        await retryPolicy.ExecuteAsync(async () =>
                        {
                            await pnpUtil.StartAndWaitAsync(cancellationToken)
                                .ConfigureAwait(false);

                            return pnpUtil.ExitCode;

                        }).ConfigureAwait(false);

                        string drivers = pnpUtil.StandardOutput?.ToString();

                        if (!string.IsNullOrWhiteSpace(drivers))
                        {
                            info.Add(new Dictionary<string, IConvertible>
                            {
                                ["toolset"] = "pnputil",
                                ["command"] = "pnputil /enum-drivers",
                                ["commandOutput"] = drivers.Trim()
                            });
                        }
                    }
                }
            }
        }

        private static async Task AddNetworkAccelerationMetadataAsync(ISystemManagement systemManagement, IDictionary<string, IConvertible> systemMetadata, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                bool networkAccelerationEnabled = false;
                if (systemManagement.Platform == PlatformID.Win32NT)
                {
                    var networkCardsKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\NetworkCards", false);
                    if (networkCardsKey != null)
                    {
                        string[] keyNames = networkCardsKey.GetSubKeyNames();
                        if (keyNames?.Any() == true)
                        {
                            foreach (string key in keyNames)
                            {
                                var specificNetworkCardKey = networkCardsKey.OpenSubKey(key);
                                if (specificNetworkCardKey != null)
                                {
                                    object cardDescription = specificNetworkCardKey.GetValue("Description");

                                    if (cardDescription != null && cardDescription.ToString().Contains("Mellanox", StringComparison.OrdinalIgnoreCase))
                                    {
                                        networkAccelerationEnabled = true;
                                        break;
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
                            await lspci.StartAndWaitAsync(cancellationToken)
                                .ConfigureAwait(false);

                            return lspci.ExitCode;

                        }).ConfigureAwait(false);

                        string pciDevices = lspci.StandardOutput?.ToString();
                        if (!string.IsNullOrWhiteSpace(pciDevices) && pciDevices.Contains("Mellanox", StringComparison.OrdinalIgnoreCase))
                        {
                            networkAccelerationEnabled = true;
                        }
                    }
                }

                systemMetadata["networkAccelerationEnabled"] = networkAccelerationEnabled;
            }
        }
    }
}
