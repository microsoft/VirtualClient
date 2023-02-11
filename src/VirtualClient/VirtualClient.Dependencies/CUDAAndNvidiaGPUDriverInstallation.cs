// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides functionality for installing specific version of CUDA and supported Nvidia GPU driver on linux.
    /// </summary>
    public class CudaAndNvidiaGPUDriverInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManager;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="CudaAndNvidiaGPUDriverInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public CudaAndNvidiaGPUDriverInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManager.StateManager;
        }

        /// <summary>
        /// The version of CUDA to be installed.
        /// </summary>
        public string CudaVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(CudaAndNvidiaGPUDriverInstallation.CudaVersion), string.Empty);
            }

            set
            {
                this.Parameters[nameof(CudaAndNvidiaGPUDriverInstallation.CudaVersion)] = value;
            }
        }

        /// <summary>
        /// The version of Nvidia GPU driver to be installed.
        /// </summary>
        public string DriverVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(CudaAndNvidiaGPUDriverInstallation.DriverVersion), string.Empty);
            }

            set
            {
                this.Parameters[nameof(CudaAndNvidiaGPUDriverInstallation.DriverVersion)] = value;
            }
        }

        /// <summary>
        /// The local runfile to install Cuda and Nvidia GPU driver.
        /// </summary>
        public string LocalRunFile
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(CudaAndNvidiaGPUDriverInstallation.LocalRunFile), string.Empty);
            }

            set
            {
                this.Parameters[nameof(CudaAndNvidiaGPUDriverInstallation.LocalRunFile)] = value;
            }
        }

        /// <summary>
        /// The user who has the ssh identity registered for.
        /// </summary>
        public string Username => this.Parameters.GetValue<string>(nameof(CudaAndNvidiaGPUDriverInstallation.Username));

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Executes CUDA and Nvidia GPU driver installation steps.
        /// </summary>
        /// <returns></returns>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger.LogTraceMessage($"{this.TypeName}.ExecutionStarted", telemetryContext);

            State installationState = await this.stateManager.GetStateAsync<State>(nameof(CudaAndNvidiaGPUDriverInstallation), cancellationToken)
                .ConfigureAwait(false);

            if (installationState == null)
            {
                if (this.Platform == PlatformID.Unix)
                {
                    LinuxDistributionInfo linuxDistributionInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken)
                        .ConfigureAwait(false);

                    telemetryContext.AddContext("LinuxDistribution", linuxDistributionInfo.LinuxDistribution);

                    switch (linuxDistributionInfo.LinuxDistribution)
                    {
                        case LinuxDistribution.Ubuntu:
                        case LinuxDistribution.Debian:
                        case LinuxDistribution.CentOS7:
                        case LinuxDistribution.RHEL7:
                        case LinuxDistribution.RHEL8:
                        case LinuxDistribution.SUSE:
                            break;

                        default:
                            // different distro installation to be addded.
                            throw new WorkloadException(
                                $"CUDA and Nvidia GPU Driver Installtion is not supported on the current Linux distro - {linuxDistributionInfo.LinuxDistribution.ToString()}.  through VC " +
                                $" Supported distros include:" +
                                $" Ubuntu, Debian, CentOS7, RHEL7, RHEL8, SUSE ",
                                ErrorReason.LinuxDistributionNotSupported);
                    }

                    await this.CudaAndNvidiaGPUDriverInstallationAsync(linuxDistributionInfo.LinuxDistribution, telemetryContext, cancellationToken)
                        .ConfigureAwait(false);

                    await this.stateManager.SaveStateAsync(nameof(CudaAndNvidiaGPUDriverInstallation), new State(), cancellationToken)
                        .ConfigureAwait(false);

                    SystemManagement.IsRebootRequested = true;
                }
                else
                {
                    // CUDA and Nvidia driver installation for other platforms to be added.
                    throw new WorkloadException(
                        $"CUDA and Nvidia GPU Driver Installtion is not supported on the current platform {this.Platform} through VC." +
                        $"Supported Platforms include:" +
                        $" Unix ",
                        ErrorReason.PlatformNotSupported);
                }
            }

            this.Logger.LogTraceMessage($"{this.TypeName}.ExecutionCompleted", telemetryContext);
        }

        private async Task CudaAndNvidiaGPUDriverInstallationAsync(LinuxDistribution linuxDistribution, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // List<string> cleanupCommands = this.CleanupCommands(linuxDistribution);
            List<string> prerequisiteCommands = this.PrerequisiteCommands(linuxDistribution);
            List<string> installationCommands = this.VersionSpecificInstallationCommands(linuxDistribution);
            List<string> postInstallationCommands = this.PostInstallationCommands();

            List<List<string>> commandsLists = new List<List<string>>
            {
                // cleanup needs rebopot again, did not find any need of doing it as of now.
                // keeping the commands in here for reference in case we may need cleanup in future.
                // cleanupCommands,
                prerequisiteCommands,
                installationCommands,
                postInstallationCommands
            };

            foreach (var commandsList in commandsLists)
            {
                foreach (string command in commandsList)
                {
                    await this.ExecuteCommandAsync(command, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private List<string> CleanupCommands(LinuxDistribution linuxDistribution)
        {
            List<string> commands = new List<string>();

            switch (linuxDistribution)
            {
                case LinuxDistribution.Ubuntu:
                case LinuxDistribution.Debian:

                    commands.Add($"bash -c \"apt-get --purge remove \"*cuda*\" \"*cublas*\" \"*cufft*\" \"*cufile*\" \"*curand*\"" +
                        $" \"*cusolver*\" \"*cusparse*\" \"*gds-tools*\" \"*npp*\" \"*nvjpeg*\" \"nsight*\" 2>/dev/null || true\"");
                    commands.Add("bash -c \"apt-get --purge remove \"*nvidia*\" 2>/dev/null || true\"");
                    commands.Add("sudo apt-get autoremove");

                    break;

                case LinuxDistribution.RHEL7:
                case LinuxDistribution.CentOS7:

                    commands.Add($"bash -c \"yum remove \"cuda*\" \"*cublas*\" \"*cufft*\" \"*cufile*\" \"*curand*\"" +
                        $" \"*cusolver*\" \"*cusparse*\" \"*gds-tools*\" \"*npp*\" \"*nvjpeg*\" \"nsight*\" 2>/dev/null || true\"");
                    commands.Add("bash -c \"yum remove \"*nvidia*\" 2>/dev/null || true\"");

                    break;

                case LinuxDistribution.RHEL8:

                    commands.Add($"bash -c \"dnf remove \"cuda*\" \"*cublas*\" \"*cufft*\" \"*cufile*\" \"*curand*\" " +
                        $"\"*cusolver*\" \"*cusparse*\" \"*gds-tools*\" \"*npp*\" \"*nvjpeg*\" \"nsight*\" 2>/dev/null || true\"");
                    commands.Add("bash -c \"dnf module remove --all nvidia-driver 2>/dev/null || true\"");
                    commands.Add("sudo dnf module reset nvidia-driver");

                    break;

                case LinuxDistribution.SUSE:

                    commands.Add($"bash -c \"zypper remove \"cuda*\" \"*cublas*\" \"*cufft*\" \"*cufile*\" \"*curand*\"" +
                        $" \"*cusolver*\" \"*cusparse*\" \"*gds-tools*\" \"*npp*\" \"*nvjpeg*\" \"nsight*\" 2>/dev/null || true\"");
                    commands.Add("bash -c \"zypper remove \"*nvidia*\" 2>/dev/null || true\"");

                    break;
            }           

            return commands;
        }

        private List<string> PrerequisiteCommands(LinuxDistribution linuxDistribution)
        {
            List<string> commands = new List<string>();

            switch (linuxDistribution)
            {
                case LinuxDistribution.Ubuntu:
                case LinuxDistribution.Debian:

                    commands.Add("apt update");
                    commands.Add("apt install build-essential -yq");
                    break;

                case LinuxDistribution.CentOS7:
                case LinuxDistribution.CentOS8:
                case LinuxDistribution.RHEL7:
                    commands.Add("yum check-update");
                    commands.Add("dnf install make automake gcc gcc-c++ kernel-devel");
                    break;

                case LinuxDistribution.SUSE:
                    commands.Add("zypper refresh");
                    commands.Add("zypper info -t pattern devel_basis");
                    commands.Add("sudo zypper install -t pattern devel_basis");
                    break;
            }

            return commands;
        }

        private List<string> VersionSpecificInstallationCommands(LinuxDistribution linuxDistribution)
        {
            string runFileName = this.LocalRunFile.Split('/').Last();
            List<string> commands = new List<string>
            {
                $"wget {this.LocalRunFile}",
                $"sh {runFileName} --silent"
            };

            switch (linuxDistribution)
            {
                case LinuxDistribution.Debian:
                case LinuxDistribution.Ubuntu:
                    commands.Add("apt update");
                    commands.Add("apt upgrade -y");
                    commands.Add($"apt install nvidia-driver-{this.DriverVersion} nvidia-dkms-{this.DriverVersion} -y");
                    commands.Add($"apt install cuda-drivers-fabricmanager-{this.DriverVersion} -y");

                    break;

                case LinuxDistribution.CentOS7:
                case LinuxDistribution.CentOS8:
                case LinuxDistribution.RHEL7:
                    commands.Add($"dnf module install nvidia-driver:{this.DriverVersion}/fm");
                    break;

                case LinuxDistribution.SUSE:
                    commands.Add($"zypper install cuda-drivers-fabricmanager-{this.DriverVersion}");
                    break;
            }
            
            return commands;
        }

        private List<string> PostInstallationCommands()
        {
            return new List<string>
            {
                $"bash -c \"echo 'export PATH=/usr/local/cuda-{this.CudaVersion}/bin${{PATH:+:${{PATH}}}}' | " +
                $"sudo tee -a /home/{this.Username}/.bashrc\"",

                $"bash -c \"echo 'export LD_LIBRARY_PATH=/usr/local/cuda-{this.CudaVersion}/lib64${{LD_LIBRARY_PATH:+:${{LD_LIBRARY_PATH}}}}' | " +
                $"sudo tee -a /home/{this.Username}/.bashrc\""
            };
        }

        private Task ExecuteCommandAsync(string commandLine, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.RetryPolicy.ExecuteAsync(async () =>
            {
                string output = string.Empty;
                using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, commandLine, null, workingDirectory))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());
                    this.LogProcessTrace(process);

                    await process.StartAndWaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "GpuDriverInstallation")
                            .ConfigureAwait(false);

                        process.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.DependencyInstallationFailed);
                    }
                }
            });
        }
    }
}
