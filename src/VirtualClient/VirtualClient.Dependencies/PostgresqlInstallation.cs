// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualBasic;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static System.Net.WebRequestMethods;

    /// <summary>
    /// Provides functionality for installing specific version of Postgresql.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class PostgresqlInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresqlInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public PostgresqlInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The name of the postgresql package.
        /// </summary>
        public static string PostgresqlPackage
        {
            get
            {
                return nameof(PostgresqlPackage);
            }

        }

        /// <summary>
        /// The version of postgres to install.
        /// </summary>
        public string Version
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(PostgresqlInstallation.Version), string.Empty);
            }

            set
            {
                this.Parameters[nameof(PostgresqlInstallation.Version)] = value;
            }
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Path where postgresql exe is downloaded for windows.
        /// </summary>
        public string PostgreSQLExecutablePath { get; set; }

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
                var linuxDistributionInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken)
                                                .ConfigureAwait(false);
                switch (linuxDistributionInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                    case LinuxDistribution.Debian:
                    case LinuxDistribution.CentOS7:
                    case LinuxDistribution.RHEL7:
                        break;

                    default:
                        throw new WorkloadException(
                            $"Postgresql installation is not supported on the current Unix/Linux distro." +
                            $" Supported distros include:" +
                            $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Ubuntu)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Debian)}" +
                            $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.CentOS7)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.RHEL7)}",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                DependencyPath workloadPackage = await this.systemManager.PackageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

                this.PostgreSQLExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, $"postgresql-{this.Version}.exe");
            }
            else
            {
                throw new WorkloadException(
                            $"Postgresql Installation is not supported on the current platform '{this.Platform}'." +
                            $"Supported Platforms include:" +
                            $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64)}, " +
                            $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64)}",
                            ErrorReason.PlatformNotSupported);
            }

        }

        /// <summary>
        /// Executes postgresql installation steps.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                var linuxDistributionInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken)
                                                .ConfigureAwait(false);
                switch (linuxDistributionInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                    case LinuxDistribution.Debian:
                        await this.PostgresqlInstallInUbuntuOrDebianAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                        break;
                    case LinuxDistribution.CentOS7:
                    case LinuxDistribution.RHEL7:
                        await this.PostgresqlInstallInCentOSOrRHELAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                        break;
                }

                string postgreSQLInstallationPath = this.PlatformSpecifics.Combine("/etc", "postgresql", $"{this.Version}", "main");

                DependencyPath postgreSQLPackage = new DependencyPath(PostgresqlInstallation.PostgresqlPackage, postgreSQLInstallationPath);

                await this.systemManager.PackageManager.RegisterPackageAsync(postgreSQLPackage, cancellationToken).ConfigureAwait(false);
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                string postgresqlInstallCommandArguments = $"--mode \"unattended\" --serverport \"5432\" --superpassword \"postgres\"";
                
                await this.ExecuteCommandAsync(this.PostgreSQLExecutablePath, postgresqlInstallCommandArguments, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

                string postgreSQLInstallationPath = this.PlatformSpecifics.Combine("C:", "Program Files", "PostgreSQL", $"{this.Version}");

                DependencyPath postgreSQLPackage = new DependencyPath(PostgresqlInstallation.PostgresqlPackage, postgreSQLInstallationPath);

                await this.systemManager.PackageManager.RegisterPackageAsync(postgreSQLPackage, cancellationToken).ConfigureAwait(false);

                this.systemManager.AddDirectoryToPath(this.PlatformSpecifics.Combine(postgreSQLInstallationPath, "bin"), EnvironmentVariableTarget.Machine);
                this.systemManager.AddDirectoryToPath(this.PlatformSpecifics.Combine(postgreSQLInstallationPath, "data"), EnvironmentVariableTarget.Machine);

            }
        }

        private async Task PostgresqlInstallInCentOSOrRHELAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string preInstallCommand;
            if (this.PlatformSpecifics.CpuArchitecture == Architecture.X64)
            {
                preInstallCommand = @"yum install -y https://download.postgresql.org/pub/repos/yum/reporpms/EL-7-x86_64/pgdg-redhat-repo-latest.noarch.rpm";
            }
            else
            {
                preInstallCommand = @"yum install -y https://download.postgresql.org/pub/repos/yum/reporpms/EL-7-aarch64/pgdg-redhat-repo-latest.noarch.rpm";
            }

            string postgresqlInstallCommand = @$"yum install -y postgresql{this.Version}-server";
            await this.ExecuteCommandAsync(preInstallCommand, null, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync(postgresqlInstallCommand, null, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task PostgresqlInstallInUbuntuOrDebianAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string setUpRepositoryCommand = @"sh -c ""echo """"deb http://apt.postgresql.org/pub/repos/apt $(lsb_release -cs)-pgdg main"""" | sudo tee /etc/apt/sources.list.d/docker.list""";
            string adddKeyCommand = @"bash -c ""wget --quiet -O - https://www.postgresql.org/media/keys/ACCC4CF8.asc | sudo apt-key add -"""; // change this | with some other thing that works 
            string updateAptPackageCommand = @"apt-get update";
            string postgresqlInstallCommand = @$"apt-get -y install postgresql-{this.Version}";
            string startServerCommand = $@"systemctl restart postgresql";
  
            await this.ExecuteCommandAsync(setUpRepositoryCommand, null, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);
            await this.ExecuteCommandAsync(adddKeyCommand, null,  Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);
            await this.ExecuteCommandAsync(updateAptPackageCommand, null, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);
            await this.ExecuteCommandAsync(postgresqlInstallCommand, null, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);
            await this.ExecuteCommandAsync(startServerCommand, null, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);
        }

        private Task ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();
            return this.RetryPolicy.ExecuteAsync(async () =>
            {
                string output = string.Empty;
                using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                {
                    SystemManagement.CleanupTasks.Add(() => process.SafeKill());
                    this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.", EventContext.Persisted());

                    await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        this.Logger.LogProcessDetails<PostgresqlInstallation>(process, relatedContext);
                        process.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.DependencyInstallationFailed);
                    }
                }
            });
        }
    }
}
