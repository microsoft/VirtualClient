// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
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
        private IStateManager stateManager;

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
            this.stateManager = this.systemManager.StateManager;
        }

        /// <summary>
        /// Parameter defines the password to use for the PostgreSQL accounts that will be used
        /// to create the DB and to run transactions against it.
        /// </summary>
        public string Password
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.Password), out IConvertible password);
                return password?.ToString();
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
        /// The password to use for the superuser account.
        /// </summary>
        protected string SuperuserPassword { get; set; }

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
                        break;

                    default:
                        throw new WorkloadException(
                            $"PostgreSQL installation is not supported by Virtual Client on the current Unix/Linux distro '{distroInfo.LinuxDistribution}'.",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }

            DependencyPath package = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            this.PackagePath = package.Path;
        }

        /// <summary>
        /// Executes PostgreSQL installation steps.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            InstallationState state = await this.stateManager.GetStateAsync<InstallationState>(nameof(PostgreSQLInstallation), cancellationToken);

            if (state == null)
            {
                this.SuperuserPassword = this.Password;
                if (string.IsNullOrWhiteSpace(this.SuperuserPassword))
                {
                    // Use the default that is defined within the PostgreSQL package.
                    this.SuperuserPassword = await this.GetServerCredentialAsync(cancellationToken);
                }

                if (this.Platform == PlatformID.Unix)
                {
                    LinuxDistributionInfo distroInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken);

                    switch (distroInfo.LinuxDistribution)
                    {
                        case LinuxDistribution.Ubuntu:
                        case LinuxDistribution.Debian:
                            await this.InstallOnUbuntuOrDebianAsync(telemetryContext, cancellationToken);
                            break;
                    }
                }
                else if (this.Platform == PlatformID.Win32NT)
                {
                    await this.InstallOnWindowsAsync(telemetryContext, cancellationToken);
                }

                await this.stateManager.SaveStateAsync(
                    nameof(PostgreSQLInstallation),
                    new Item<InstallationState>(nameof(PostgreSQLInstallation), new InstallationState()),
                    cancellationToken);
            }
        }

        private async Task<string> GetServerCredentialAsync(CancellationToken cancellationToken)
        {
            string fileName = "superuser.txt";
            string path = this.Combine(this.PackagePath, fileName);
            if (!this.systemManager.FileSystem.File.Exists(path))
            {
                throw new DependencyException(
                    $"Required file '{fileName}' missing in package '{this.PackagePath}'. The PostgreSQL server cannot be initialized. " +
                    $"As an alternative, you can supply the '{nameof(this.Password)}' parameter on the command line.",
                    ErrorReason.DependencyNotFound);
            }

            return (await this.systemManager.FileSystem.File.ReadAllTextAsync(path, cancellationToken)).Trim();
        }

        private async Task InstallOnUbuntuOrDebianAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string installationScript = this.Combine(this.PackagePath, "ubuntu", "install.sh");

            string command = "bash";
            string commandArguments = $"-c \"{EnvironmentVariable.VC_PASSWORD}={this.SuperuserPassword} sh {installationScript}\"";
            string workingDirectory = this.Combine(this.PackagePath, "ubuntu");

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("command", command)
                .AddContext("commandArguments", commandArguments)
                .AddContext("workingDirectory", workingDirectory);

            await this.systemManager.MakeFileExecutableAsync(installationScript, this.Platform, cancellationToken);

            await this.RetryPolicy.ExecuteAsync(async () =>
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, relatedContext, cancellationToken, runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, relatedContext, logToFile: true);
                        process.ThrowIfDependencyInstallationFailed();
                    }
                }
            });
        }

        private Task InstallOnWindowsAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string installerPath = this.Combine(this.PackagePath, $"postgresql.exe");
            telemetryContext.AddContext(nameof(installerPath), installerPath);

            return this.RetryPolicy.ExecuteAsync(async () =>
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    installerPath,
                    $@"--mode ""unattended"" --serverport ""5432"" --superpassword ""{this.SuperuserPassword}""",
                    this.PackagePath,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, logToFile: true);
                        process.ThrowIfDependencyInstallationFailed();
                    }
                }
            });
        }

        internal class InstallationState : State
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="InstallationState"/> object.
            /// </summary>
            public InstallationState()
                : base()
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="InstallationState"/> object.
            /// </summary>
            [JsonConstructor]
            public InstallationState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }
        }
    }
}
