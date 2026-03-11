// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Installation component for AMD GPU Drivers
    /// </summary>
    public class AMDGPUDriverInstallation : VirtualClientComponent
    {
        private const string Mi25ExeName = "AMD-mi25.exe";
        private const string V620ExeName = "Setup.exe";

        // Known-good ROCm installation URLs for each supported Ubuntu codename.
        // These are the default URLs used when the profile does not specify a LinuxInstallationFile.
        private static readonly Dictionary<string, string> SupportedInstallationFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "focal", "https://repo.radeon.com/amdgpu-install/5.5/ubuntu/focal/amdgpu-install_5.5.50500-1_all.deb" },
            { "jammy", "https://repo.radeon.com/amdgpu-install/6.3.3/ubuntu/jammy/amdgpu-install_6.3.60303-1_all.deb" }
        };

        private IPackageManager packageManager;
        private IFileSystem fileSystem;
        private ISystemManagement systemManager;
        private IStateManager stateManager;
        private LinuxDistributionInfo linuxDistributionInfo;
        private string osVersionCodename;

        /// <summary>
        /// Initializes a new instance of the <see cref="AMDGPUDriverInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public AMDGPUDriverInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            dependencies.ThrowIfNull(nameof(dependencies));
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManager.StateManager;
            this.fileSystem = this.systemManager.FileSystem;
            this.packageManager = this.systemManager.PackageManager;
        }

        /// <summary>
        /// Determines whether Reboot is required or not after Driver installation
        /// </summary>
        public bool RebootRequired
        {
            get
            {
                switch (this.Platform)
                {
                    case PlatformID.Win32NT:
                        return this.Parameters.GetValue<bool>(nameof(AMDGPUDriverInstallation.RebootRequired), false);
                    default:
                        return this.Parameters.GetValue<bool>(nameof(AMDGPUDriverInstallation.RebootRequired), true);
                }
            }
        }

        /// <summary>
        /// Gpu Type on which driver is installed. (e.g. mi25, v620)
        /// </summary>
        public string GpuModel
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(AMDGPUDriverInstallation.GpuModel), "mi25");
            }
        }

        /// <summary>
        /// The user who is running.
        /// </summary>
        public string Username
        {
            get
            {
                string username = this.Parameters.GetValue<string>(nameof(AMDGPUDriverInstallation.Username), string.Empty);
                if (string.IsNullOrWhiteSpace(username))
                {
                    username = Environment.UserName;
                }

                return username;
            }
        }

        /// <summary>
        /// The AMD GPU driver installtion file for Linux
        /// </summary>
        public string LinuxInstallationFile
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(AMDGPUDriverInstallation.LinuxInstallationFile), string.Empty);
            }

            set
            {
                this.Parameters[nameof(AMDGPUDriverInstallation.LinuxInstallationFile)] = value;
            }
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Executes  GPU driver installation steps.
        /// </summary>
        /// <returns></returns>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.Execute", telemetryContext, async () =>
            {
                State installationState = await this.stateManager.GetStateAsync<State>(nameof(AMDGPUDriverInstallation), cancellationToken)
                    .ConfigureAwait(false);

                if (installationState == null)
                {
                    if (this.Platform == PlatformID.Win32NT)
                    {
                        await this.InstallAMDGPUDriverWindows(telemetryContext, cancellationToken)
                                   .ConfigureAwait(false);

                        await this.stateManager.SaveStateAsync(nameof(AMDGPUDriverInstallation), new State(), cancellationToken)
                            .ConfigureAwait(false);

                    }
                    else if (this.Platform == PlatformID.Unix)
                    {
                        telemetryContext.AddContext("LinuxDistribution", this.linuxDistributionInfo.LinuxDistribution);

                        switch (this.linuxDistributionInfo.LinuxDistribution)
                        {
                            case LinuxDistribution.Ubuntu:
                                break;

                            default:
                                // different distro installation to be addded.
                                this.Logger.LogMessage(
                                    $"AMD GPU driver installation is not supported by Virtual Client on the current Linux distro '{this.linuxDistributionInfo.LinuxDistribution}'.",
                                    telemetryContext);

                                break;
                        }

                        await this.InstallAMDGPUDriverLinux(telemetryContext, cancellationToken)
                                   .ConfigureAwait(false);

                        await this.stateManager.SaveStateAsync(nameof(AMDGPUDriverInstallation), new State(), cancellationToken)
                            .ConfigureAwait(false);
                    }

                    VirtualClientRuntime.IsRebootRequested = this.RebootRequired;
                }

                if (this.Platform == PlatformID.Unix)
                {
                    await this.ExecutePostRebootCommands(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                }
            });
        }

        /// <summary>
        /// Initializes docker installation requirements.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                this.linuxDistributionInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken)
                    .ConfigureAwait(false);

                this.osVersionCodename = await this.DetectOsVersionCodenameAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Readability")]
        private async Task InstallAMDGPUDriverLinux(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.ResolveLinuxInstallationFile();

            if (string.IsNullOrWhiteSpace(this.LinuxInstallationFile))
            {
                throw new DependencyException($"The linux installation file can not be null or empty and it is: {this.LinuxInstallationFile}", ErrorReason.DependencyNotFound);
            }

            // The .bashrc file is used to define commands that should be run whenever the system
            // is booted. For the purpose of the AMD GPU driver installation, we want to include extra
            // paths in the $PATH environment variable post installation.
            string userHome = this.GetUserHomePath();
            string bashRcPath = $"{userHome}/.bashrc";

            this.fileSystem.Directory.CreateDirectory(Path.GetDirectoryName(bashRcPath) !);

            // We hit a bug where the .bashrc file does not exist on the system. To prevent issues later
            // we are creating the file if it is missing.
            if (!this.fileSystem.File.Exists(bashRcPath))
            {
                await this.fileSystem.File.WriteAllLinesAsync(
                    bashRcPath,
                    new string[]
                    {
                        "# ~/.bashrc: executed by bash(1) for non-login shells.",
                        "# see /usr/share/doc/bash/examples/startup-files (in the package bash-doc)",
                        "# for examples"
                    },
                    cancellationToken);
            }

            List<string> prerequisiteCommands = this.PrerequisiteCommands();
            List<string> installationCommands = this.VersionSpecificInstallationCommands();
            List<string> postInstallationCommands = this.PostInstallationCommands();

            List<List<string>> commandsLists = new List<List<string>>
            {
                prerequisiteCommands,
                installationCommands,
                postInstallationCommands
            };

            foreach (var commandsList in commandsLists)
            {
                foreach (string command in commandsList)
                {
                    IProcessProxy process = await this.ExecuteCommandAsync(command, null, Environment.CurrentDirectory, telemetryContext, cancellationToken, true)
                        .ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "AMDGPUDriverInstallation", logToFile: true)
                            .ConfigureAwait(false);

                        process.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.DependencyInstallationFailed);
                    }
                }
            }

        }

        private List<string> PrerequisiteCommands()
        {
            List<string> commands = new List<string>();

            switch (this.linuxDistributionInfo.LinuxDistribution)
            {
                case LinuxDistribution.Ubuntu:
                    // Clean up any broken package state from previous failed installation attempts.
                    // The amdgpu-dkms package can be left in a half-configured state if the DKMS module
                    // build fails, which causes all subsequent apt-get commands to fail.
                    commands.Add("bash -c 'dpkg --remove --force-remove-reinstreq amdgpu-dkms 2>/dev/null || true'");
                    commands.Add("bash -c 'dpkg --configure -a 2>/dev/null || true'");
                    commands.Add("apt-get -yq update");
                    commands.Add("apt-get install -yq libpci3 libpci-dev doxygen unzip cmake git");
                    commands.Add("apt-get install -yq libnuma-dev libncurses5");
                    commands.Add("apt-get install -yq libyaml-cpp-dev");
                    commands.Add("apt-get -yq update");

                    break;

                default:
                    break;
            }

            return commands;
        }

        private List<string> VersionSpecificInstallationCommands()
        {
            string installationFileName = this.LinuxInstallationFile.Split('/').Last();
            List<string> commands = new List<string>()
            {
            };

            switch (this.linuxDistributionInfo.LinuxDistribution)
            {
                case LinuxDistribution.Ubuntu:
                    commands.Add($"wget {this.LinuxInstallationFile}");
                    commands.Add($"DEBIAN_FRONTEND=noninteractive apt-get install -yq ./{installationFileName}");
                    break;

                default:
                    break;
            }

            return commands;
        }

        private List<string> PostInstallationCommands()
        {
            string userHome = this.GetUserHomePath();

            // last 2 command are to make sure that we are blacklisting AMD GPU drivers before rebooting
            return new List<string>
            {
                "amdgpu-install -y --usecase=hiplibsdk,rocm,dkms",
                $"bash -c \"echo 'export PATH=/opt/rocm/bin${{PATH:+:${{PATH}}}}' | " +
                $"sudo tee -a {userHome}/.bashrc\"",
                $"bash -c \"echo 'blacklist amdgpu' | sudo tee -a /etc/modprobe.d/amdgpu.conf \"",
                "update-initramfs -u -k all"
            };
        }

        private async Task ExecutePostRebootCommands(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            List<string> commands = new List<string>();

            switch (this.linuxDistributionInfo.LinuxDistribution)
            {
                case LinuxDistribution.Ubuntu:
                    // first command is to enable the AMD GPU drivers after reboot is completed.
                    commands.Add("modprobe amdgpu");
                    commands.Add("apt-get install -yq rocblas rocm-smi-lib ");
                    commands.Add("apt-get install -yq rocm-validation-suite");
                    break;

                default:
                    break;
            }

            foreach (string command in commands)
            {
                IProcessProxy process = await this.ExecuteCommandAsync(command, null, Environment.CurrentDirectory, telemetryContext, cancellationToken, true)
                    .ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, "AMDGPUDriverInstallation", logToFile: true)
                        .ConfigureAwait(false);

                    process.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.DependencyInstallationFailed);
                }
            }
        }

        /// <summary>
        /// Reads /etc/os-release to detect the OS version codename (e.g., "focal", "jammy").
        /// Follows the same pattern as MongoDBServerInstallation's codename detection.
        /// </summary>
        private async Task<string> DetectOsVersionCodenameAsync(CancellationToken cancellationToken)
        {
            try
            {
                string osReleaseContent = await this.fileSystem.File.ReadAllTextAsync("/etc/os-release", cancellationToken)
                    .ConfigureAwait(false);

                Match match = Regex.Match(osReleaseContent, @"VERSION_CODENAME=(\w+)", RegexOptions.Multiline);
                return match.Success ? match.Groups[1].Value : null;
            }
            catch
            {
                // If /etc/os-release cannot be read, return null and let ResolveLinuxInstallationFile
                // fall back to the profile-provided LinuxInstallationFile parameter.
                return null;
            }
        }

        /// <summary>
        /// Resolves the correct Linux installation file URL based on the detected OS codename.
        /// If the profile provides an explicit LinuxInstallationFile, that takes precedence.
        /// Otherwise, the built-in mapping of codename to ROCm URL is used.
        /// </summary>
        private void ResolveLinuxInstallationFile()
        {
            // If the profile explicitly provided a LinuxInstallationFile, use it as-is.
            if (!string.IsNullOrWhiteSpace(this.LinuxInstallationFile))
            {
                return;
            }

            // Look up the detected codename in the built-in mapping.
            if (!string.IsNullOrWhiteSpace(this.osVersionCodename)
                && AMDGPUDriverInstallation.SupportedInstallationFiles.TryGetValue(this.osVersionCodename, out string resolvedUrl))
            {
                this.LinuxInstallationFile = resolvedUrl;
                return;
            }

            throw new DependencyException(
                $"No AMD GPU driver installation file is available for the detected OS codename '{this.osVersionCodename}'. " +
                $"Supported codenames: {string.Join(", ", AMDGPUDriverInstallation.SupportedInstallationFiles.Keys)}. " +
                $"You can provide an explicit URL via the 'LinuxInstallationFile' profile parameter.",
                ErrorReason.DependencyNotFound);
        }

        private string GetUserHomePath()
        {
            string username = this.Username;
            if (string.Equals(username, "root", StringComparison.OrdinalIgnoreCase))
            {
                return "/root";
            }

            return $"/home/{username}";
        }

        private async Task InstallAMDGPUDriverWindows(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string installerPath = string.Empty;

            DependencyPath amdDriverInstallerPackage = await this.packageManager.GetPackageAsync(
                this.PackageName, cancellationToken)
                    .ConfigureAwait(false);

            if (amdDriverInstallerPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            switch (this.GpuModel)
            {
                case "mi25":
                    {
                        await this.InstallAMDGPUDriverOnMi25(amdDriverInstallerPackage.Path, telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;
                    }

                case "v620":
                    {
                        await this.InstallAMDGPUDriverOnV620(amdDriverInstallerPackage.Path, telemetryContext, cancellationToken)
                            .ConfigureAwait(false);
                        break;
                    }

                default:
                    {
                        throw new NotSupportedException($"GpuModel '{this.GpuModel}' is not supported.");
                    }
            }
        }

        private async Task InstallAMDGPUDriverOnMi25(string driverPackagePath, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string installationFile = this.Combine(driverPackagePath, this.GpuModel, AMDGPUDriverInstallation.Mi25ExeName);

            if (!this.fileSystem.File.Exists(installationFile))
            {
                throw new DependencyException($"The installer file was not found in the directory {driverPackagePath}", ErrorReason.DependencyNotFound);
            }

            IProcessProxy process = await this.ExecuteCommandAsync(installationFile, "/S /v/qn", Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            if (!cancellationToken.IsCancellationRequested)
            {
                await this.LogProcessDetailsAsync(process, telemetryContext).ConfigureAwait(false);
                process.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.DependencyInstallationFailed);
            }
        }

        private async Task InstallAMDGPUDriverOnV620(string driverPackagePath, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string installationFile = this.Combine(driverPackagePath, this.GpuModel, AMDGPUDriverInstallation.V620ExeName);

            if (!this.fileSystem.File.Exists(installationFile))
            {
                throw new DependencyException($"The installer file was not found in the directory {driverPackagePath}", ErrorReason.DependencyNotFound);
            }

            IProcessProxy process = await this.ExecuteCommandAsync(installationFile, "-INSTALL -OUTPUT screen", Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            if (!cancellationToken.IsCancellationRequested)
            {
                await this.LogProcessDetailsAsync(process, telemetryContext).ConfigureAwait(false);
                process.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.DependencyInstallationFailed);
            }
        }
    }
}