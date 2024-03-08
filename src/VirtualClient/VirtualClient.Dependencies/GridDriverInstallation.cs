// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    internal class GridDriverInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManager;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="GridDriverInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public GridDriverInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManager.StateManager;
        }

        /// <summary>
        /// The version of GRID driver to be installed.
        /// </summary>
        public string DriverVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(GridDriverInstallation.DriverVersion), string.Empty);
            }

            set
            {
                this.Parameters[nameof(GridDriverInstallation.DriverVersion)] = value;
            }
        }

        /// <summary>
        /// JSON resource file with forward links to local runfiles to install GRID drivers.
        /// </summary>
        public string AzureRepository
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(GridDriverInstallation.AzureRepository), string.Empty);
            }

            set
            {
                this.Parameters[nameof(GridDriverInstallation.AzureRepository)] = value;
            }
        }

        /// <summary>
        /// Determines whether Reboot is required or not after Driver installation.
        /// </summary>
        public bool RebootRequired
        {
            get
            {
                switch (this.Platform)
                {
                    case PlatformID.Unix:
                    case PlatformID.Win32NT:
                        return this.Parameters.GetValue<bool>(nameof(GridDriverInstallation.RebootRequired), false);

                    default:
                        return this.Parameters.GetValue<bool>(nameof(GridDriverInstallation.RebootRequired), true);
                }
            }
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Executes GRID driver installation steps.
        /// </summary>
        /// <returns></returns>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger.LogTraceMessage($"{this.TypeName}.ExecutionStarted", telemetryContext);

            bool isDriverInstalled = await this.CheckIfDriverInstalledAsync(telemetryContext, cancellationToken);

            if (isDriverInstalled == false)
            {
                if (this.Platform == PlatformID.Unix)
                {
                    LinuxDistributionInfo linuxDistributionInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken)
                        .ConfigureAwait(false);

                    telemetryContext.AddContext("LinuxDistribution", linuxDistributionInfo.LinuxDistribution);

                    switch (linuxDistributionInfo.LinuxDistribution)
                    {
                        case LinuxDistribution.Ubuntu:
                        case LinuxDistribution.CentOS7:
                        case LinuxDistribution.CentOS8:
                        case LinuxDistribution.RHEL7:
                        case LinuxDistribution.RHEL8:
                            break;

                        default:
                            // different distro installation to be addded.
                            throw new WorkloadException(
                                $"GRID driver installation is not supported by Virtual Client on the current Linux distro '{linuxDistributionInfo.LinuxDistribution}'.",
                                ErrorReason.LinuxDistributionNotSupported);
                    }

                    await this.InstallGridDriverAsync(linuxDistributionInfo, telemetryContext, cancellationToken)
                        .ConfigureAwait(false);

                    isDriverInstalled = await this.CheckIfDriverInstalledAsync(telemetryContext, cancellationToken);

                    if (isDriverInstalled == false)
                    {
                        throw new DependencyException("Failed to install GRID driver");
                    }
                }
                else if (this.Platform == PlatformID.Win32NT)
                {
                    throw new DependencyException("GRID driver installation is not supported on Windows");
                }

                VirtualClientRuntime.IsRebootRequested = this.RebootRequired;
            }

            this.Logger.LogTraceMessage($"{this.TypeName}.ExecutionCompleted", telemetryContext);
        }

        private async Task<bool> CheckIfDriverInstalledAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string command = "nvidia-smi -q | grep \"\"Driver Version\"\"";
            string bashCommand = $"bash -c \"{command}\"";

            try
            {
                string output = await this.ExecuteCommandAsync(bashCommand, null, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

                return output.Contains(this.DriverVersion);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task InstallGridDriverAsync(LinuxDistributionInfo linuxDistributionInfo, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            List<string> prerequisiteCommands = this.PrerequisiteCommands(linuxDistributionInfo.LinuxDistribution);
            List<string> installationCommands = this.VersionSpecificInstallationCommands(linuxDistributionInfo);

            List<List<string>> commandsLists = new List<List<string>>
            {
                prerequisiteCommands,
                installationCommands
            };

            foreach (var commandsList in commandsLists)
            {
                foreach (string command in commandsList)
                {
                    await this.ExecuteCommandAsync(command, null, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private List<string> PrerequisiteCommands(LinuxDistribution linuxDistribution)
        {
            List<string> commands = new List<string>();

            switch (linuxDistribution)
            {
                case LinuxDistribution.Ubuntu:
                    commands.Add("apt update");
                    commands.Add("apt install build-essential -yq");
                    commands.Add("apt-get update");
                    commands.Add("apt-get install jq");
                    break;

                case LinuxDistribution.CentOS7:
                case LinuxDistribution.CentOS8:
                case LinuxDistribution.RHEL7:
                case LinuxDistribution.RHEL8:
                    commands.Add("yum check-update");
                    commands.Add("dnf install make automake gcc gcc-c++ kernel-devel");
                    commands.Add("yum install epel-release");
                    commands.Add("yum install jq");
                    break;
            }

            return commands;
        }

        private List<string> VersionSpecificInstallationCommands(LinuxDistributionInfo linuxDistributionInfo)
        {
            List<string> commands = new List<string>();

            string osDistribution = string.Empty;
            string osVersion = string.Empty;

            switch (linuxDistributionInfo.LinuxDistribution)
            {
                case LinuxDistribution.Ubuntu:
                    string[] parts = Regex.Split(linuxDistributionInfo.OperationSystemFullName, "[ .]");
                    osDistribution = "Ubuntu";
                    osVersion = parts[1] + ".x";
                    break;
                case LinuxDistribution.CentOS7:
                case LinuxDistribution.RHEL7:
                    osDistribution = "RHEL/CentOS";
                    osVersion = "7.x";
                    break;
                case LinuxDistribution.CentOS8:
                case LinuxDistribution.RHEL8:
                    osDistribution = "RHEL/CentOS";
                    osVersion = "8.x";
                    break;
            }

            string curlCommand = $"curl -s {this.AzureRepository} | jq -r '.OS[] | select(.Name == \"\"Linux\"\") | .Version[] | select(.Name == \"\"{osDistribution}\"\" and .Version == \"\"{osVersion}\"\") | .Driver[] | select(.Type == \"\"GRID\"\") | .Version[] | select(.Num == \"\"{this.DriverVersion}\"\") | .FwLink'";

            commands.Add($"bash -c \"wget -O NVIDIA-Linux-x86_64-grid.run $({curlCommand})\"");
            commands.Add("chmod +x NVIDIA-Linux-x86_64-grid.run");
            commands.Add("sudo ./NVIDIA-Linux-x86_64-grid.run");

            return commands;
        }

        private Task<string> ExecuteCommandAsync(string commandLine, string commandLineArgs, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return (Task<string>)this.RetryPolicy.ExecuteAsync(async () =>
            {
                using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, commandLine, commandLineArgs, workingDirectory))
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

                    return process.StandardOutput.ToString();
                }
            });
        }
    }
}
