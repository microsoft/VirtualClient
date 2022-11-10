// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Installation component for OpenFOAM9
    /// </summary>
    public class OpenFOAMInstallation : VirtualClientComponent
    {
        private const string AddPublicKeyCommand = "sh -c \"wget -O - https://dl.openfoam.org/gpg.key | apt-key add -\"";
        private const string UpdateSoftwareRepositoriesCommand = "add-apt-repository http://dl.openfoam.org/ubuntu";
        private const string UpdateAptPackageCommand = "apt update";
        private const string InstallOpenFOAMx64Command = "apt -y install openfoam9";
        private const string InstallOpenFOAMarm64Command = "apt install openfoam --yes --quiet";
       
        /// <summary>
        /// Initializes a new instance of the <see cref="OpenFOAMInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public OpenFOAMInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            dependencies.ThrowIfNull(nameof(dependencies));

            this.SystemManager = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// Retrieves the interface to interacting with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManager { get; }

        /// <summary>
        /// Installs OpenFOAM9
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.InstallOpenFOAM(cancellationToken, telemetryContext);
        }

        private async Task InstallOpenFOAM(CancellationToken cancellationToken, EventContext telemetryContext)
        {
            LinuxDistributionInfo linuxDistroInfo = await this.SystemManager.GetLinuxDistributionAsync(cancellationToken).ConfigureAwait(false);

            if (linuxDistroInfo.LinuxDistribution == LinuxDistribution.Ubuntu)
            {
                ProcessManager manager = this.SystemManager.ProcessManager;
                List<string> installationCommands = new List<string>();

                if (this.CpuArchitecture == Architecture.X64)
                {
                    installationCommands.Add(AddPublicKeyCommand);
                    installationCommands.Add(UpdateSoftwareRepositoriesCommand);
                    installationCommands.Add(UpdateAptPackageCommand);
                    installationCommands.Add(InstallOpenFOAMx64Command);
                }
                else if (this.CpuArchitecture == Architecture.Arm64)
                {
                    installationCommands.Add(UpdateAptPackageCommand);
                    installationCommands.Add(InstallOpenFOAMarm64Command);
                }

                foreach (var command in installationCommands)
                {
                    await this.ExecuteCommandAsync(manager, command, null, telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                throw new DependencyException(
                           $"You are on Linux distrubution {linuxDistroInfo.LinuxDistribution.ToString()}, which is not supported by OpenFOAM.",
                           ErrorReason.LinuxDistributionNotSupported);
            }
        }

        private async Task ExecuteCommandAsync(ProcessManager manager, string command, string arguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (IProcessProxy process = this.SystemManager.ProcessManager.CreateElevatedProcess(this.Platform, command, arguments))
            {
                SystemManagement.CleanupTasks.Add(() => process.SafeKill());
                await process.StartAndWaitAsync(cancellationToken, null)
                    .ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    this.Logger.LogProcessDetails<OpenFOAMInstallation>(process, telemetryContext);
                    process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.DependencyInstallationFailed);
                }
            }
        }
    }
}
