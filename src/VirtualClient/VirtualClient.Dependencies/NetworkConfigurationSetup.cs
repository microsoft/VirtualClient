// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Networking Configuration Setup for workloads which tests network between 2 VMs (client and server).
    /// </summary>
    public class NetworkConfigurationSetup : VirtualClientComponent
    {
        private const string SettingsBeginComment = "# VC Settings Begin";
        private const string SettingsEndComment = "# VC Settings End";
        private const string LimitsConfigPath = "/etc/security/limits.conf";
        private const string RcLocalPath = "/etc/rc.local";
        private const string SystemdConfigPath = "/etc/systemd/system.conf";
        private const string SystemdUserConfigPath = "/etc/systemd/user.conf";
        private const int NoFileLimit = 1048575;

        private ISystemManagement systemManagement;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkConfigurationSetup"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NetworkConfigurationSetup(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManagement.StateManager;
        }

        /// <summary>
        /// Parameter defines whether the network configurations should be applied.
        /// </summary>
        public bool ConfigureNetwork
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(NetworkConfigurationSetup.ConfigureNetwork), true);
            }
        }

        /// <summary>
        /// Parameter defines whether the BusyPoll should be enabled.
        /// </summary>
        public bool EnableBusyPoll
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(NetworkConfigurationSetup.EnableBusyPoll), false);
            }
        }

        /// <summary>
        /// Parameter defines whether the Firewall should be disabled.
        /// </summary>
        public bool DisableFirewall
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(NetworkConfigurationSetup.DisableFirewall), true);
            }
        }

        /// <summary>
        /// Parameter defines the name of the Visual Studio C Runtime package.
        /// </summary>
        public string VisualStudioCRuntimePackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(NetworkConfigurationSetup.VisualStudioCRuntimePackageName));
            }
        }

        /// <summary>
        /// Initializes the environment and dependencies
        /// </summary>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return base.InitializeAsync(telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Executes the setup operations for Networking Workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.ConfigureNetwork)
            {
                switch (this.Platform)
                {
                    case PlatformID.Win32NT:
                        await this.NetworkConfigurationSetupOnWindowsAsync(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;

                    case PlatformID.Unix:
                        await this.NetworkConfigurationSetupOnUnixAsync(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;
                }
            }
        }

        /// <summary>
        /// Removes any previously applied Virtual Client settings from the content.
        /// </summary>
        /// <param name="content">Applied settings that need to be removed</param>
        /// <returns>The settings that are removed.</returns>
        protected IList<string> RemovePreviouslyAppliedSettings(IEnumerable<string> content)
        {
            List<string> cleanedContent = new List<string>();
            if (content?.Any() == true)
            {
                int sectionBeginLine = -1;
                int sectionEndLine = -1;
                int contentLength = content.Count();
                for (int lineNum = 0; lineNum < contentLength; lineNum++)
                {
                    if (string.Equals(content.ElementAt(lineNum), NetworkConfigurationSetup.SettingsBeginComment, StringComparison.OrdinalIgnoreCase))
                    {
                        sectionBeginLine = lineNum;
                    }
                    else if (string.Equals(content.ElementAt(lineNum), NetworkConfigurationSetup.SettingsEndComment, StringComparison.OrdinalIgnoreCase))
                    {
                        sectionEndLine = lineNum;
                    }
                }

                if (sectionBeginLine >= 0 && sectionEndLine >= 0)
                {
                    // Keep only the content that is not VC-specific settings.
                    cleanedContent.AddRange(content.Take(sectionBeginLine));
                    if (sectionEndLine < contentLength - 1)
                    {
                        cleanedContent.AddRange(content.Skip(sectionEndLine + 1));
                    }
                }
                else
                {
                    // There are no VC settings within the content. Leave it as is.
                    cleanedContent.AddRange(content);
                }
            }

            return cleanedContent;
        }

        /// <summary>
        /// Networking Configuration setup steps for Windows.
        /// </summary>
        private async Task NetworkConfigurationSetupOnWindowsAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            State state = await this.stateManager.GetStateAsync<State>(nameof(NetworkConfigurationSetup), cancellationToken)
                .ConfigureAwait(false);

            if (state == null)
            {
                // Setup for CPS tool.
                string powerShellExe = "powershell";
                string command = "Set-NetTCPSetting -AutoReusePortRangeStartPort 10000 -AutoReusePortRangeNumberOfPorts 50000";

                IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
                DependencyPath visualStudioCRuntimePackage = await packageManager.GetPackageAsync(this.VisualStudioCRuntimePackageName, CancellationToken.None)
                    .ConfigureAwait(false);

                string visualStudioCRuntimeDllPath = this.PlatformSpecifics.ToPlatformSpecificPath(visualStudioCRuntimePackage, this.Platform, this.CpuArchitecture).Path;
                this.SetEnvironmentVariable(EnvironmentVariable.PATH, visualStudioCRuntimeDllPath, EnvironmentVariableTarget.Machine, append: true);

                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, powerShellExe, command))
                {
                    await process.StartAndWaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "NetworkConfiguration")
                            .ConfigureAwait(false);

                        process.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.DependencyInstallationFailed);
                    }
                }

                // The existence of the state object indicates the network setup/configuration was already performed.
                await this.stateManager.SaveStateAsync(nameof(NetworkConfigurationSetup), new State(), cancellationToken)
                    .ConfigureAwait(false);

                // Reboots happen out of context of the executors because they must be synchronized in relation to
                // all other components that might be running as part of a profile. Thus the profile execution
                // handler itself is responsible for handling reboots.
                this.Logger.LogMessage($"{nameof(NetworkConfigurationSetup)}.RequestReboot", LogLevel.Information, EventContext.Persisted());
                this.RequestReboot();
            }
        }

        /// <summary>
        /// Network Configuration setup steps for Linux.
        /// </summary>
        private async Task NetworkConfigurationSetupOnUnixAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            State state = await this.stateManager.GetStateAsync<State>(nameof(NetworkConfigurationSetup), cancellationToken)
                .ConfigureAwait(false);

            if (state == null)
            {
                await this.ApplySecurityLimitsConfigurationOnUnixAsync(cancellationToken)
                    .ConfigureAwait(false);

                await this.ApplySystemdConfigurationOnUnixAsync(cancellationToken)
                    .ConfigureAwait(false);

                await this.ApplyRcLocalFileCommandsAsync(cancellationToken)
                    .ConfigureAwait(false);

                // The existence of the state object indicates the network setup/configuration was already performed.
                await this.stateManager.SaveStateAsync(nameof(NetworkConfigurationSetup), new State(), cancellationToken)
                    .ConfigureAwait(false);

                // Reboots happen out of context of the executors because they must be synchronized in relation to
                // all other components that might be running as part of a profile. Thus the profile execution
                // handler itself is responsible for handling reboots.
                this.Logger.LogMessage($"{nameof(NetworkConfigurationSetup)}.RequestReboot", LogLevel.Information, telemetryContext);
                this.RequestReboot();
            }
            else
            {
                await this.systemManagement.MakeFileExecutableAsync(NetworkConfigurationSetup.RcLocalPath, this.Platform, cancellationToken)
                    .ConfigureAwait(false);

                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, "sh", NetworkConfigurationSetup.RcLocalPath))
                {
                    await process.StartAndWaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "NetworkConfiguration")
                            .ConfigureAwait(false);

                        process.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.DependencyInstallationFailed);
                    }
                }
            }
        }

        private async Task ApplyRcLocalFileCommandsAsync(CancellationToken cancellationToken)
        {
            string[] rcLocalFileContent = null;
            if (!this.systemManagement.FileSystem.File.Exists(NetworkConfigurationSetup.RcLocalPath))
            {
                rcLocalFileContent = Array.Empty<string>();
            }
            else
            {
                rcLocalFileContent = await this.systemManagement.FileSystem.File.ReadAllLinesAsync(NetworkConfigurationSetup.RcLocalPath, cancellationToken)
                    .ConfigureAwait(false);
            }

            IList<string> rcLocalContents = this.RemovePreviouslyAppliedSettings(rcLocalFileContent);

            rcLocalContents.Add(NetworkConfigurationSetup.SettingsBeginComment);
            rcLocalContents.Add("#!/bin/sh");
            rcLocalContents.Add($"sysctl -w fs.file-max={NetworkConfigurationSetup.NoFileLimit}");
            rcLocalContents.Add("sysctl -w net.ipv4.tcp_tw_reuse=1 # TIME_WAIT work-around");
            rcLocalContents.Add("sysctl -w net.ipv4.ip_local_port_range=\"10000 60000\"  # ephemeral ports increased");

            if (this.EnableBusyPoll)
            {
                rcLocalContents.Add("sysctl -w net.core.busy_poll=50");
                rcLocalContents.Add("sysctl -w net.core.busy_read=50");
            }

            if (this.DisableFirewall)
            {
                rcLocalContents.Add("iptables -I OUTPUT -j NOTRACK  # disable connection tracking");
                rcLocalContents.Add("iptables -I PREROUTING -j NOTRACK  # disable connection tracking");
                rcLocalContents.Add("iptables -P INPUT ACCEPT  # accept all inbound traffic");
                rcLocalContents.Add("iptables -P OUTPUT ACCEPT  # accept all outbound traffic");
                rcLocalContents.Add("iptables -P FORWARD ACCEPT  # accept all forward traffic");
                rcLocalContents.Add("iptables --flush  # flush the current firewall settings");
            }

            rcLocalContents.Add(NetworkConfigurationSetup.SettingsEndComment);

            await this.systemManagement.FileSystem.File.WriteAllLinesAsync(NetworkConfigurationSetup.RcLocalPath, rcLocalContents)
                .ConfigureAwait(false);
        }

        private async Task ApplySecurityLimitsConfigurationOnUnixAsync(CancellationToken cancellationToken)
        {
            // Configure: /etc/security/limits.conf
            // ================================================================================================================================
            string[] limitsFileContent = await this.systemManagement.FileSystem.File.ReadAllLinesAsync(NetworkConfigurationSetup.LimitsConfigPath, cancellationToken)
                .ConfigureAwait(false);

            IList<string> limitConfigContents = this.RemovePreviouslyAppliedSettings(limitsFileContent);

            // The default 'soft' and 'hard' limits for the maximum number of open file handles/descriptors.
            // https://linux.die.net/man/5/limits.conf
            limitConfigContents.Add(NetworkConfigurationSetup.SettingsBeginComment);
            limitConfigContents.Add($"*   soft    nofile  {NetworkConfigurationSetup.NoFileLimit}");
            limitConfigContents.Add($"*   hard    nofile  {NetworkConfigurationSetup.NoFileLimit}");
            limitConfigContents.Add(NetworkConfigurationSetup.SettingsEndComment);

            await this.systemManagement.FileSystem.File.WriteAllLinesAsync(NetworkConfigurationSetup.LimitsConfigPath, limitConfigContents)
                .ConfigureAwait(false);
        }

        private async Task ApplySystemdConfigurationOnUnixAsync(CancellationToken cancellationToken)
        {
            IFileSystem fileSystem = this.systemManagement.FileSystem;

            string settingName = "DefaultLimitNOFILE";
            Regex defaultLimitNoFileExpression = new Regex($@"\s*#*\s*{settingName}=[0-9,]*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            List<string> configFiles = new List<string>
            {
                NetworkConfigurationSetup.SystemdConfigPath,
                NetworkConfigurationSetup.SystemdUserConfigPath
            };

            foreach (string configFile in configFiles)
            {
                if (fileSystem.File.Exists(configFile))
                {
                    // Configure: /etc/systemd/system.conf
                    // https://superuser.com/questions/1200539/cannot-increase-open-file-limit-past-4096-ubuntu/1200818#_=_
                    // ================================================================================================================================
                    string configFileContent = await this.systemManagement.FileSystem.File.ReadAllTextAsync(configFile, cancellationToken)
                        .ConfigureAwait(false);

                    string setting = $"{Environment.NewLine}{settingName}={NetworkConfigurationSetup.NoFileLimit}";
                    if (defaultLimitNoFileExpression.IsMatch(configFileContent))
                    {
                        configFileContent = defaultLimitNoFileExpression.Replace(configFileContent, setting);
                    }
                    else
                    {
                        configFileContent += setting;
                    }

                    await this.systemManagement.FileSystem.File.WriteAllTextAsync(configFile, configFileContent)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
