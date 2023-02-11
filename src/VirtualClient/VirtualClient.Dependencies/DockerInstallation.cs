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
    /// Provides functionality for installing specific version of docker on linux.
    /// </summary>
    public class DockerInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManager;
        private string installDockerCommand;

        /// <summary>
        /// Initializes a new instance of the <see cref="DockerInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public DockerInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The version of docker to install from the feed.
        /// </summary>
        public string Version
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DockerInstallation.Version), string.Empty);
            }

            set
            {
                this.Parameters[nameof(DockerInstallation.Version)] = value;
            }
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

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
                LinuxDistributionInfo distroInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken)
                    .ConfigureAwait(false);

                switch (distroInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                        if (this.Version != string.Empty)
                        {
                            this.installDockerCommand = 
                                @$"bash -c ""apt-get install docker-ce=$(apt-cache  madison docker-ce | grep {this.Version} | awk '{{print $3}}') " +
                                @$"docker-ce-cli=$(apt-cache madison docker-ce | grep {this.Version} | awk '{{print $3}}') containerd.io docker-compose-plugin --yes --quiet""";
                        }

                        // installs latest version if no version is provided.
                        else
                        {
                            this.installDockerCommand = @"bash -c ""apt-get install docker-ce docker-ce-cli containerd.io docker-compose-plugin --yes --quiet""";
                        }

                        break;

                    default:
                        // different distro installation to be addded.
                        throw new WorkloadException(
                            $"Docker Installtion is not supported on the current Linux distro - {distroInfo.LinuxDistribution.ToString()}.  through VC " +
                            $" Supported distros include:" +
                            $" Ubuntu ",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
            else
            {
                // docker installation for windows to be added.
                throw new WorkloadException(
                    $"Docker Installtion is not supported on the current platform {this.Platform} through VC." +
                    $"Supported Platforms include:" +
                    $" Unix ",
                    ErrorReason.PlatformNotSupported);
            }

        }

        /// <summary>
        /// Executes docker installation steps.
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
                    await this.DockerInstallInUbuntuAsync(telemetryContext, cancellationToken)
                        .ConfigureAwait(false);

                    break;
            }
        }

        private async Task DockerInstallInUbuntuAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string updateAptPackageCommand = "apt update";
            string requiredPackagesCommand = "apt-get install ca-certificates curl gnupg lsb-release";
            string addOfficialGPGKeyCommand = @"bash -c ""curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /etc/apt/keyrings/docker.gpg --batch --yes""";
            string setUpRepositoryCommand = 
                @"bash -c ""echo """"deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu $(lsb_release -cs) stable"""" " +
                @"| sudo tee /etc/apt/sources.list.d/docker.list > /dev/null""";

            await this.ExecuteCommandAsync(updateAptPackageCommand, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync(requiredPackagesCommand, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync("mkdir -p /etc/apt/keyrings", Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync(addOfficialGPGKeyCommand, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync(setUpRepositoryCommand, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync(updateAptPackageCommand, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync(this.installDockerCommand, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);
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
                        await this.LogProcessDetailsAsync(process, telemetryContext)
                            .ConfigureAwait(false);

                        process.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.DependencyInstallationFailed);
                    }
                }
            });
        }
    }
}
