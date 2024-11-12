// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides functionality for installing Nvidia container toolkit on linux.
    /// </summary>
    public class NvidiaContainerToolkitInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManager;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="NvidiaContainerToolkitInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public NvidiaContainerToolkitInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManager.StateManager;
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Executes Nvidia container toolkit installation steps.
        /// </summary>
        /// <returns></returns>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger.LogTraceMessage($"{this.TypeName}.ExecutionStarted", telemetryContext);

            State installationState = await this.stateManager.GetStateAsync<State>(nameof(NvidiaContainerToolkitInstallation), cancellationToken)
                .ConfigureAwait(false);

            if (installationState == null)
            {
                if (this.Platform == PlatformID.Unix)
                {
                    LinuxDistributionInfo distroInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken)
                        .ConfigureAwait(false);

                    telemetryContext.AddContext("linuxDistribution", distroInfo.LinuxDistribution);

                    switch (distroInfo.LinuxDistribution)
                    {
                        case LinuxDistribution.Ubuntu:
                        case LinuxDistribution.Debian:
                        case LinuxDistribution.CentOS8:
                        case LinuxDistribution.CentOS7:
                        case LinuxDistribution.RHEL7:
                        case LinuxDistribution.SUSE:
                            break;

                        default:
                            // different distro installation to be addded.
                            throw new WorkloadException(
                                $"Nvidia Container Toolkit Installation is not supported by Virtual Client on the current Linux distro '{distroInfo.LinuxDistribution}'",
                                ErrorReason.LinuxDistributionNotSupported);
                    }

                    await this.NvidiaContainerToolkitInstallationAsync(distroInfo.LinuxDistribution, telemetryContext, cancellationToken)
                        .ConfigureAwait(false);

                    await this.stateManager.SaveStateAsync(nameof(NvidiaContainerToolkitInstallation), new State(), cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    // CUDA and Nvidia driver installation for other platforms to be added.
                    throw new WorkloadException(
                        $"Nvidia Container Toolkit is not supported by Virtual Client on the current platform '{this.Platform}'.",
                        ErrorReason.PlatformNotSupported);
                }
            }
        }

        private async Task NvidiaContainerToolkitInstallationAsync(LinuxDistribution linuxDistribution, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            List<string> installationCommands = this.NvidiaContainerToolkitInstallationCommands(linuxDistribution);

            foreach (string command in installationCommands)
            {
                await this.ExecuteCommandAsync(command, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private List<string> NvidiaContainerToolkitInstallationCommands(LinuxDistribution linuxDistribution)
        {
            List<string> commands = new List<string>();

            switch (linuxDistribution)
            {
                case LinuxDistribution.Ubuntu:
                case LinuxDistribution.Debian:

                    string setupCommand = "curl -fsSL https://nvidia.github.io/libnvidia-container/gpgkey " +
                        "| sudo gpg --dearmor -o /usr/share/keyrings/nvidia-container-toolkit-keyring.gpg \\\n  " +
                        "&& curl -s -L https://nvidia.github.io/libnvidia-container/stable/deb/nvidia-container-toolkit.list | \\\n " +
                        "   sed 's#deb https://#deb [signed-by=/usr/share/keyrings/nvidia-container-toolkit-keyring.gpg] https://#g' | \\\n  " +
                        "  sudo tee /etc/apt/sources.list.d/nvidia-container-toolkit.list";

                    commands.Add($"bash -c \"{setupCommand}\"");
                    commands.Add("apt-get update");
                    commands.Add("apt-get install -y nvidia-container-toolkit");
                    commands.Add("nvidia-ctk runtime configure --runtime=docker");
                    commands.Add("systemctl restart docker");
                    
                    break;

                case LinuxDistribution.CentOS7:

                    setupCommand = "curl -s -L https://nvidia.github.io/libnvidia-container/stable/rpm/nvidia-container-toolkit.repo | \\" +
                        "  sudo tee /etc/yum.repos.d/nvidia-container-toolkit.repo";

                    commands.Add($"bash -c \"{setupCommand}\"");
                    commands.Add("yum clean expire-cache");
                    commands.Add("yum install -y nvidia-container-toolkit");
                    commands.Add("systemctl restart docker");

                    break;

                case LinuxDistribution.CentOS8:

                    setupCommand = "curl -s -L https://nvidia.github.io/libnvidia-container/stable/rpm/nvidia-container-toolkit.repo | \\" +
                       "  sudo tee /etc/yum.repos.d/nvidia-container-toolkit.repo";

                    commands.Add($"bash -c \"{setupCommand}\"");
                    commands.Add("yum clean expire-cache");
                    commands.Add("yum install -y nvidia-container-toolkit");
                    commands.Add("systemctl restart docker");

                    break;

                case LinuxDistribution.RHEL7:

                    setupCommand = "curl -s -L https://nvidia.github.io/libnvidia-container/stable/rpm/nvidia-container-toolkit.repo | \\" +
                       "  sudo tee /etc/yum.repos.d/nvidia-container-toolkit.repo";

                    commands.Add($"bash -c \"{setupCommand}\"");
                    commands.Add("yum clean expire-cache");
                    commands.Add("yum install -y nvidia-container-toolkit");
                    commands.Add("systemctl restart docker");

                    break;

                case LinuxDistribution.SUSE:

                    setupCommand = "zypper ar https://nvidia.github.io/libnvidia-container/stable/rpm/nvidia-container-toolkit.repo";

                    commands.Add($"bash -c \"{setupCommand}\"");
                    commands.Add("zypper refresh");
                    commands.Add("zypper --gpg-auto-import-keys install -y nvidia-container-toolkit");
                    commands.Add("systemctl restart docker");

                    break;
            }

            return commands;
        }

        private Task ExecuteCommandAsync(string commandLine, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();

            return this.RetryPolicy.ExecuteAsync(async () =>
            {
                using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, commandLine, null, workingDirectory))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());
                    this.LogProcessTrace(process);

                    await process.StartAndWaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, relatedContext, "NvidiaToolkitInstallation")
                            .ConfigureAwait(false);

                        process.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.DependencyInstallationFailed);
                    }
                }
            });
        }
    }
}
