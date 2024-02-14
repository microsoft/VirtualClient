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
    /// Provides functionality for installing specific version of apptainer on linux.
    /// </summary>
    public class ApptainerInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManager;
        private string installationFile;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApptainerInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public ApptainerInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The version of apptainer to install from the feed.
        /// </summary>
        public string Version
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ApptainerInstallation.Version), "1.1.6");
            }
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Initializes apptainer installation requirements.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                LinuxDistributionInfo distroInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken)
                    .ConfigureAwait(false);

                switch (distroInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                        this.installationFile = $"apptainer_{this.Version}_amd64.deb";

                        break;

                    default:
                        // different distro installation to be addded.
                        throw new WorkloadException(
                            $"Apptainer Installtion is not supported on the current Linux distro - {distroInfo.LinuxDistribution.ToString()}.  through VC " +
                            $" Supported distros include:" +
                            $" Ubuntu ",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
            else
            {
                // apptainer installation for windows to be added.
                throw new WorkloadException(
                    $"Apptainer Installtion is not supported on the current platform {this.Platform} through VC." +
                    $"Supported Platforms include:" +
                    $" Unix ",
                    ErrorReason.PlatformNotSupported);
            }

        }

        /// <summary>
        /// Executes apptainer installation steps.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            LinuxDistributionInfo distroInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken)
                .ConfigureAwait(false);

            switch (distroInfo.LinuxDistribution)
            {
                case LinuxDistribution.Ubuntu:
                    await this.ApptainerInstallInUbuntuAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);

                    break;
            }
        }

        private async Task ApptainerInstallInUbuntuAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string getApptainerCommand = $"wget https://github.com/apptainer/apptainer/releases/download/v{this.Version}/{this.installationFile}";
            string installApptainerCommand = $"dpkg -i {this.installationFile}";
            string removeApptainerCommand = $"rm {this.installationFile}";

            await this.ExecuteCommandAsync(getApptainerCommand, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync(installApptainerCommand, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync(removeApptainerCommand, telemetryContext, cancellationToken)
                .ConfigureAwait(false);        
        }

        private Task ExecuteCommandAsync(string commandLine, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.RetryPolicy.ExecuteAsync(async () =>
            {
                IProcessProxy process = await this.ExecuteCommandAsync(commandLine, Environment.CurrentDirectory, telemetryContext, cancellationToken, true)
                .ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, nameof(ApptainerInstallation), logToFile: true);
                    process.ThrowIfDependencyInstallationFailed();
                }
            });
        }
    }
}
