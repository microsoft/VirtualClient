// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Controller
{
    using System;
    using System.IO;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides information specific to SDK agent scenarios.
    /// </summary>
    public static class AgentSpecifics
    {
        internal static readonly string AgentName = Path.GetFileNameWithoutExtension(VirtualClientRuntime.ExecutableName);

        /// <summary>
        /// Returns the default path for logs/log files on the local system.<br/><br/><i>(Note that the logs
        /// folder is outside of the SDK agent binaries folder to enable the agent to be replaced without
        /// log file loss)</i>
        /// <br/>
        /// <list type="bullet">
        /// <item>
        /// Default on Linux = $HOME/user/{agent_folder}/logs/{agent_id}/{experiment_id}<br/>
        /// (e.g. /home/user/SdkAgent/logs/10.1.15.1/6ab6fbb1-ab4f-472b-878a-5efb295cb4bc).
        /// </item>
        /// <item>
        /// Default on Windows = %USERPROFILE%\{agent_folder}\logs\{agent_id}\{experiment_id}<br/>
        /// (e.g. C:\Users\SdkAgent\logs\10.1.15.1\6ab6fbb1-ab4f-472b-878a-5efb295cb4bc).
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="platformSpecifics">Provides the platform and CPU architecture for the local system (e.g. Linux, Windows, X64, ARM64).</param>
        /// <param name="agentId">An identifier for the local system/agent (e.g. an IP address).</param>
        /// <param name="experimentId">The ID of the experiment.</param>
        /// <param name="experimentName">An identifier used to group experiments on a set of target systems together.</param>
        /// <returns>A path on the local system to which log files are written.</returns>
        public static string GetLocalLogsPath(PlatformSpecifics platformSpecifics, string agentId, string experimentId, string experimentName = null)
        {
            platformSpecifics.ThrowIfNull(nameof(platformSpecifics));
            agentId.ThrowIfNull(nameof(agentId));
            experimentId.ThrowIfNull(nameof(experimentId));

            if (!string.IsNullOrEmpty(experimentName))
            {
                // e.g. 
                // /home/user/sdkagent/logs/10.1.15.1/dc_cycle
                return Path.GetFullPath(platformSpecifics.Combine(
                    platformSpecifics.CurrentDirectory, 
                    "/../logs", 
                    agentId.ToLowerInvariant(), 
                    experimentName.ToLowerInvariant()));
            }
            else
            {
                // e.g. 
                // /home/user/sdkagent/logs/10.1.15.1/6ab6fbb1-ab4f-472b-878a-5efb295cb4bc
                return Path.GetFullPath(platformSpecifics.Combine(
                    platformSpecifics.CurrentDirectory, 
                    "/../logs", 
                    agentId.ToLowerInvariant(), 
                    experimentId.ToLowerInvariant()));
            }
        }

        /// <summary>
        /// Returns the default path for packages on the local system.<br/><br/><i>(Note that the packages
        /// folder is outside of the SDK agent binaries folder to enable the agent to be replaced without
        /// package file loss)</i>
        /// <br/>
        /// <list type="bullet">
        /// <item>
        /// Default on Linux = $HOME/user/{agent_folder}/packages<br/>
        /// (e.g. /home/user/SdkAgent/packages).
        /// </item>
        /// <item>
        /// Default on Windows = %USERPROFILE%\{agent_folder}\packages<br/>
        /// (e.g. C:\Users\SdkAgent\packages).
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="platformSpecifics">Provides the platform and CPU architecture for the local system (e.g. Linux, Windows, X64, ARM64).</param>
        /// <returns>A path on the local system to which packages exist or can be installed.</returns>
        public static string GetLocalPackagesPath(PlatformSpecifics platformSpecifics)
        {
            return Path.GetFullPath(platformSpecifics.Combine(platformSpecifics.CurrentDirectory, "/../packages"));
        }

        /// <summary>
        /// Returns the default path for state management on the local system.
        /// <br/>
        /// <list type="bullet">
        /// <item>
        /// Default on Linux = $HOME/user/{agent_folder}/{platform_architecture}/state/{experiment_id}<br/>
        /// (e.g. /home/user/SdkAgent/linux-x64/state/6ab6fbb1-ab4f-472b-878a-5efb295cb4bc).
        /// </item>
        /// <item>
        /// Default on Windows = %USERPROFILE%\{agent_folder}\{platform_architecture}\state\{experiment_id}<br/>
        /// (e.g. C:\Users\SdkAgent\win-x64\state\6ab6fbb1-ab4f-472b-878a-5efb295cb4bc).
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="platformSpecifics">Provides the platform and CPU architecture for the local system (e.g. Linux, Windows, X64, ARM64).</param>
        /// <param name="experimentId">The ID of the experiment.</param>
        /// <returns>A path on the local system to which state files are maintained.</returns>
        public static string GetLocalStatePath(PlatformSpecifics platformSpecifics, string experimentId)
        {
            return platformSpecifics.Combine(platformSpecifics.CurrentDirectory, "state", experimentId.ToLowerInvariant());
        }

        /// <summary>
        /// Returns the default path for temp files on the local system.
        /// <br/>
        /// <list type="bullet">
        /// <item>
        /// Default on Linux = $HOME/user/{agent_folder}/{platform_architecture}/temp/{experiment_id}<br/>
        /// (e.g. /home/user/SdkAgent/linux-x64/temp/6ab6fbb1-ab4f-472b-878a-5efb295cb4bc).
        /// </item>
        /// <item>
        /// Default on Windows = %USERPROFILE%\{agent_folder}\{platform_architecture}\temp\{experiment_id}<br/>
        /// (e.g. C:\Users\SdkAgent\win-x64\temp\6ab6fbb1-ab4f-472b-878a-5efb295cb4bc).
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="platformSpecifics">Provides the platform and CPU architecture for the local system (e.g. Linux, Windows, X64, ARM64).</param>
        /// <param name="experimentId">The ID of the experiment.</param>
        /// <returns>A path on the local system to which temp files are written.</returns>
        public static string GetLocalTempPath(PlatformSpecifics platformSpecifics, string experimentId)
        {
            return platformSpecifics.Combine(platformSpecifics.CurrentDirectory, "temp", experimentId.ToLowerInvariant());
        }

        /// <summary>
        /// Returns the default installation path for the remote agent.
        /// <br/>
        /// <list type="bullet">
        /// <item>
        /// Default on Linux = $HOME/{agent_folder}/{platform_architecture}<br/>
        /// (e.g. /home/user/SdkAgent/linux-arm64).
        /// </item>
        /// <item>
        /// Default on Windows = %USERPROFILE%\{agent_folder}\{platform_architecture}<br/>
        /// (e.g. C:\Users\SdkAgent\win-x64).
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="targetPlatformSpecifics">Provides the platform and CPU architecture for the target/remote system (e.g. Linux, Windows, X64, ARM64).</param>
        /// <returns>A path on the target system to which the remote agent exists or can be installed.</returns>
        public static string GetRemoteAgentInstallationPath(PlatformSpecifics targetPlatformSpecifics)
        {
            string agentDirectory = AgentSpecifics.GetRemoteAgentDirectoryPath(targetPlatformSpecifics.Platform);
            return targetPlatformSpecifics.Combine(agentDirectory, targetPlatformSpecifics.PlatformArchitectureName);
        }

        /// <summary>
        /// Returns the default path for logs/log files on the remote system.<br/><br/><i>(Note that the logs
        /// folder is outside of the SDK agent binaries folder to enable the agent to be replaced without
        /// log file loss)</i>
        /// <br/>
        /// <list type="bullet">
        /// <item>
        /// Default on Linux = $HOME/{agent_folder}/logs/{agent_id}/{experiment_id}<br/>
        /// (e.g. /home/user/SdkAgent/logs/10.1.15.1/6ab6fbb1-ab4f-472b-878a-5efb295cb4bc).
        /// </item>
        /// <item>
        /// Default on Windows = %USERPROFILE%\{agent_folder}\logs\{agent_id}\{experiment_id}<br/>
        /// (e.g. C:\Users\SdkAgent\logs\6ab6fbb1-ab4f-472b-878a-5efb295cb4bc).
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="targetPlatformSpecifics">Provides the platform and CPU architecture for the target/remote system (e.g. Linux, Windows, X64, ARM64).</param>
        /// <param name="agentId">An identifier for the local system/agent (e.g. an IP address).</param>
        /// <param name="experimentId">The ID of the experiment.</param>
        /// <param name="experimentName">An identifier used to group experiments on a set of target systems together.</param>
        /// <returns>A path on the target system to which the remote packages exists or can be installed.</returns>
        public static string GetRemoteLogsPath(PlatformSpecifics targetPlatformSpecifics, string agentId, string experimentId, string experimentName = null)
        {
            string defaultPath = null;
            string agentDirectory = AgentSpecifics.GetRemoteAgentDirectoryPath(targetPlatformSpecifics.Platform);

            if (!string.IsNullOrWhiteSpace(experimentName))
            {
                defaultPath = targetPlatformSpecifics.Combine(agentDirectory, "logs", agentId.ToLowerInvariant(), experimentName.ToLowerInvariant());
            }
            else
            {
                defaultPath = targetPlatformSpecifics.Combine(agentDirectory, "logs", agentId.ToLowerInvariant(), experimentId.ToLowerInvariant());
            }

            return defaultPath;
        }

        /// <summary>
        /// Returns the default installation path for packages on the remote system.<br/><br/><i>(Note that the packages
        /// folder is outside of the SDK agent binaries folder to enable the agent to be replaced without
        /// package file loss)</i>
        /// <br/>
        /// <list type="bullet">
        /// <item>
        /// Default on Linux = $HOME/{agent_folder}/packages<br/>
        /// (e.g. /home/user/SdkAgent/packages).
        /// </item>
        /// <item>
        /// Default on Windows = %USERPROFILE%\{agent_folder}\packages<br/>
        /// (e.g. C:\Users\SdkAgent\packages).
        /// </item>
        /// </list>
        /// </summary>
        /// <param name="targetPlatformSpecifics">Provides the platform and CPU architecture for the target/remote system (e.g. Linux, Windows, X64, ARM64).</param>
        /// <param name="packagesFolder">The folder name for the packages allowing for different packages.</param>
        /// <returns>A path on the target system to which the remote packages exists or can be installed.</returns>
        public static string GetRemotePackagesPath(PlatformSpecifics targetPlatformSpecifics, string packagesFolder = "packages")
        {
            string agentDirectory = AgentSpecifics.GetRemoteAgentDirectoryPath(targetPlatformSpecifics.Platform);
            return targetPlatformSpecifics.Combine(agentDirectory, packagesFolder);
        }

        private static string GetRemoteAgentDirectoryPath(PlatformID platform)
        {
            string defaultPath = null;
            string agentName = AgentSpecifics.AgentName;

            if (platform == PlatformID.Unix)
            {
                defaultPath = $"$HOME/{agentName.ToLowerInvariant()}";
            }
            else if (platform == PlatformID.Win32NT)
            {
                defaultPath = $"%USERPROFILE%\\{agentName.ToLowerInvariant()}";
            }
            else
            {
                throw new NotSupportedException($"Unsupported platform '{platform}'. Supported platforms are Unix and Windows.");
            }

            return defaultPath;
        }
    }
}
