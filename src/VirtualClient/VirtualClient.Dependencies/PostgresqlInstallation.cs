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
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides functionality for installing specific version of PostgreSQL.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class PostgreSQLInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManager;
        private IPackageManager packageManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSQLInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public PostgreSQLInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManager.PackageManager;
        }

        /// <summary>
        /// The version of PostgreSQL to install.
        /// </summary>
        public int Version
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(PostgreSQLInstallation.Version));
            }

            set
            {
                this.Parameters[nameof(PostgreSQLInstallation.Version)] = value;
            }
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// The path to the PostgreSQL package for installation.
        /// </summary>
        protected string PackagePath { get; set; }

        /// <summary>
        /// Initializes PostgreSQL installation requirements.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform != PlatformID.Unix && this.Platform != PlatformID.Win32NT)
            {
                throw new WorkloadException($"Unsupported platform. The platform '{this.Platform}' is not supported.", ErrorReason.NotSupported);
            }

            if (this.Platform == PlatformID.Unix)
            {
                LinuxDistributionInfo distroInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken);

                switch (distroInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                    case LinuxDistribution.Debian:
                    case LinuxDistribution.CentOS7:
                    case LinuxDistribution.RHEL7:
                        break;

                    default:
                        throw new WorkloadException(
                            $"PostgreSQL installation is not supported by Virtual Client on the current Unix/Linux distro '{distroInfo.LinuxDistribution}'.",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }

            DependencyPath package = await this.packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken);
            this.PackagePath = package.Path;
        }

        /// <summary>
        /// Executes PostgreSQL installation steps.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                LinuxDistributionInfo distroInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken);

                switch (distroInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                    case LinuxDistribution.Debian:
                        await this.InstallOnUbuntuOrDebianAsync(telemetryContext, cancellationToken);
                        break;

                    case LinuxDistribution.CentOS7:
                    case LinuxDistribution.RHEL7:
                        await this.InstallOnCentOSOrRHELAsync(telemetryContext, cancellationToken);
                        break;
                }
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                await this.InstallOnWindowsAsync(telemetryContext, cancellationToken);
            }
        }

        /// <summary>
        /// Validates the parameters supplied in the profile.
        /// </summary>
        protected override void ValidateParameters()
        {
            base.ValidateParameters();

            // It is risky to allow the user to change the version given that the version of the installer in the
            // 'postgresql' package for win-x64 and win-arm64 allows only 1 version (e.g. postgresql-14.exe).
            if (this.Version != 14)
            {
                throw new DependencyException(
                    $"Unsupported version. PostgreSQL version '{this.Version}' is not supported by the Virtual Client. The following versions are currently supported: 14.",
                    ErrorReason.DependencyDescriptionInvalid);
            }
        }

        private Task InstallOnCentOSOrRHELAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string installationScript = this.Combine(this.PackagePath, "install_postgresql_rhel_centos.sh");
            telemetryContext.AddContext(nameof(installationScript), installationScript);

            return this.RetryPolicy.ExecuteAsync(async () =>
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync("bash", installationScript, Environment.CurrentDirectory, telemetryContext, cancellationToken, runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "PostgreSQLInstallation", logToFile: true);
                        process.ThrowIfDependencyInstallationFailed();
                    }
                }
            });
        }

        private Task InstallOnUbuntuOrDebianAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string installationScript = this.Combine(this.PackagePath, "install_postgresql_ubuntu.sh");
            telemetryContext.AddContext(nameof(installationScript), installationScript);

            return this.RetryPolicy.ExecuteAsync(async () =>
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync("bash", installationScript, Environment.CurrentDirectory, telemetryContext, cancellationToken, runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "PostgreSQLInstallation", logToFile: true);
                        process.ThrowIfDependencyInstallationFailed();
                    }
                }
            });
        }

        private Task InstallOnWindowsAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string installerPath = this.Combine(this.PackagePath, $"postgresql-{this.Version}.exe");
            telemetryContext.AddContext(nameof(installerPath), installerPath);

            return this.RetryPolicy.ExecuteAsync(async () =>
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    installerPath, $@"--mode ""unattended"" --serverport ""5432"" --superpassword ""postgres""", Environment.CurrentDirectory, telemetryContext, cancellationToken, runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "PostgreSQLInstallation", logToFile: true);
                        process.ThrowIfDependencyInstallationFailed();
                    }
                }
            });
        }
    }
}
