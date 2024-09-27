// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Installs DCGMI tool for capturing metrics on GPU
    /// </summary>
    public class DCGMIInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="DCGMIInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public DCGMIInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Executes DCGMI installation steps.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.ThrowIfPlatformArchitectureIsNotSupported();
            var linuxDistributionInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken)
                                                .ConfigureAwait(false);
            switch (linuxDistributionInfo.LinuxDistribution)
            {
                case LinuxDistribution.Ubuntu:
                case LinuxDistribution.Debian:
                    await this.InstallDCGMIUbuntuOrDebianAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                    break;
                case LinuxDistribution.RHEL8:
                case LinuxDistribution.CentOS8:
                    await this.InstallDCGMIRHELOrCentOSAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                    break;
                case LinuxDistribution.SUSE:
                    await this.InstallDCGMISUSEAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new WorkloadException(
                                $"The DCGMI monitor is not supported on the current Linux distro - " +
                                $"{linuxDistributionInfo.LinuxDistribution.ToString()}.  Supported distros include:" +
                                $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Ubuntu)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Debian)}" +
                                $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.CentOS8)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.RHEL8)}" +
                                $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.SUSE)}",
                                ErrorReason.LinuxDistributionNotSupported);
            }
        }

        private void ThrowIfPlatformArchitectureIsNotSupported()
        {
            if (this.Platform == PlatformID.Unix)
            {
                Console.WriteLine($"Architecture is {this.PlatformSpecifics.CpuArchitecture}, Architecture.X64 is {Architecture.X64}");
                if (this.PlatformSpecifics.CpuArchitecture != Architecture.X64 && this.PlatformSpecifics.CpuArchitecture != Architecture.Arm64)
                {
                    throw new WorkloadException(
                            $"DCGMI Installtion is not supported on the current platform {this.Platform} through VC." +
                            $"Supported Platforms-Architecture include:" +
                            $"{PlatformID.Unix}-{Architecture.X64}," + $"{PlatformID.Unix}-{Architecture.Arm64}",
                            ErrorReason.PlatformNotSupported);
                }
            }
            else
            {
                throw new WorkloadException(
                            $"DCGMI Installtion is not supported on the current platform {this.Platform} through VC." +
                            $"Supported Platforms-Architecture include:" +
                            $"{PlatformID.Unix}-{Architecture.X64}," + $"{PlatformID.Unix}-{Architecture.Arm64}",
                            ErrorReason.PlatformNotSupported);
            }
        }

        private async Task InstallDCGMIUbuntuOrDebianAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // DCGMI installation guide https://docs.nvidia.com/datacenter/dcgm/latest/user-guide/getting-started.html
            List<string> commands = new List<string>();

            string delKeyCommand = "apt-key del 7fa2af80";
            string metaPackageDownloadCommand = string.Empty;
            if (this.PlatformSpecifics.CpuArchitecture == Architecture.X64)
            {
                metaPackageDownloadCommand = $@"bash -c ""wget https://developer.download.nvidia.com/compute/cuda/repos/$(echo $(. /etc/os-release; echo $ID$VERSION_ID) | sed -e 's/\.//g')/x86_64/cuda-keyring_1.0-1_all.deb""";
            }
            else if (this.PlatformSpecifics.CpuArchitecture == Architecture.Arm64) 
            {
                metaPackageDownloadCommand = $@"bash -c ""wget https://developer.download.nvidia.com/compute/cuda/repos/$(echo $(. /etc/os-release; echo $ID$VERSION_ID) | sed -e 's/\.//g')/sbsa/cuda-keyring_1.0-1_all.deb""";
            }
            else if (this.PlatformSpecifics.CpuArchitecture == Architecture.Ppc64le)
            {
                metaPackageDownloadCommand = $@"bash -c ""wget https://developer.download.nvidia.com/compute/cuda/repos/$(echo $(. /etc/os-release; echo $ID$VERSION_ID) | sed -e 's/\.//g')/ppc64le/cuda-keyring_1.0-1_all.deb""";
            }

            string cudaGPGKeyInstallCommand = @"dpkg -i cuda-keyring_1.0-1_all.deb";
            string updateCommand = @"apt-get update";
            string dcGPUManagerInstallCommand = @"apt-get install -y datacenter-gpu-manager";
            string enableDCGMCommand = @"systemctl --now enable nvidia-dcgm";
            commands.Add(delKeyCommand);
            commands.Add(metaPackageDownloadCommand);
            commands.Add(cudaGPGKeyInstallCommand);
            commands.Add(updateCommand);
            commands.Add(dcGPUManagerInstallCommand);
            commands.Add(enableDCGMCommand);
            foreach (string command in commands)
            {
                await this.ExecuteCommandAsync(command, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task InstallDCGMIRHELOrCentOSAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            List<string> commands = new List<string>();

            string delKeyCommand = "rpm --erase gpg-pubkey-7fa2af80*";
            string metaPackageDownloadCommand;

            if (this.PlatformSpecifics.CpuArchitecture == Architecture.X64)
            {
                metaPackageDownloadCommand = @"bash -c ""dnf config-manager --add-repo http://developer.download.nvidia.com/compute/cuda/repos/$(echo $(. /etc/os-release;echo $ID`rpm -E ""%{?rhel}%{?fedora}""`))/x86_64/cuda-rhel8.repo""";
            }
            else
            {
                metaPackageDownloadCommand = @"bash -c ""dnf config-manager --add-repo http://developer.download.nvidia.com/compute/cuda/repos/$(echo $(. /etc/os-release;echo $ID`rpm -E ""%{?rhel}%{?fedora}""`))/sbsa/cuda-rhel8.repo""";
            }

            string metaDataUpdateComand = @"dnf clean expire-cache";
            string dCGMInstallCommand = @"dnf install -y datacenter-gpu-manager";
            string enableDCGMCommand = @"systemctl --now enable nvidia-dcgm";
            commands.Add(delKeyCommand);
            commands.Add(metaPackageDownloadCommand);
            commands.Add(metaDataUpdateComand);
            commands.Add(dCGMInstallCommand);
            commands.Add(enableDCGMCommand);
            foreach (string command in commands)
            {
                await this.ExecuteCommandAsync(command, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task InstallDCGMISUSEAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            List<string> commands = new List<string>();

            string delKeyCommand = "rpm --erase gpg-pubkey-7fa2af80*";
            string metaPackageDownloadCommand;

            if (this.PlatformSpecifics.CpuArchitecture == Architecture.X64)
            {
                metaPackageDownloadCommand = @"bash -c ""zypper ar  http://developer.download.nvidia.com/compute/cuda/repos/$(echo $(. /etc/os-release; echo $ID$VERSION_ID) | sed -e 's/\.[0-9]//')/x86_64/cuda-$(. /etc/os-release;echo $ID$VERSION_ID | sed -e 's/\.[0-9]//').repo""";
            }
            else
            {
                metaPackageDownloadCommand = @"bash -c ""zypper ar http://developer.download.nvidia.com/compute/cuda/repos/$(echo $(. /etc/os-release; echo $ID$VERSION_ID) | sed -e 's/\.[0-9]//')/sbsa/cuda-$(. /etc/os-release;echo $ID$VERSION_ID | sed -e 's/\.[0-9]//').repo""";
            }

            string metaDataUpdateCommand = @"zypper refresh";
            string updateCommand = @"apt-get update";
            string dCGMInstallCommand = @"zypper install datacenter-gpu-manager";
            string enableDCGMCommand = @" systemctl --now enable nvidia-dcgm";
            commands.Add(delKeyCommand);
            commands.Add(metaPackageDownloadCommand);
            commands.Add(metaDataUpdateCommand);
            commands.Add(updateCommand);
            commands.Add(dCGMInstallCommand);
            commands.Add(enableDCGMCommand);
            foreach (string command in commands)
            {
                await this.ExecuteCommandAsync(command, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private Task ExecuteCommandAsync(string commandLine, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();

            return this.RetryPolicy.ExecuteAsync(async () =>
            {
                string output = string.Empty;
                using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, commandLine, null, workingDirectory))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());
                    this.Logger.LogTraceMessage($"Executing process '{commandLine}' at directory '{workingDirectory}'.", EventContext.Persisted());

                    await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, relatedContext, logToFile: true);
                        process.ThrowIfDependencyInstallationFailed();
                    }
                }
            });
        }
    }
}
