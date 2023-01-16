using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using VirtualClient.Common;
using VirtualClient.Common.Extensions;
using VirtualClient.Common.Platform;
using VirtualClient.Common.Telemetry;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    /// <summary>
    /// Provides functionality for installing specific version of HammerDb on linux.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class HammerDbInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="HammerDbInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public HammerDbInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(3, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The name of the HammerDb package.
        /// </summary>
        public static string HammerDbPackage
        {
            get
            {
                return nameof(HammerDbPackage);
            }

        }

        /// <summary>
        /// The version of hammerDb to install.
        /// </summary>
        public string Version
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(HammerDbInstallation.Version), string.Empty);
            }

            set
            {
                this.Parameters[nameof(HammerDbInstallation.Version)] = value;
            }
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Path where HammerDB executable is downloaded.
        /// </summary>
        public string HammerDBExecutablePath { get; set; }

        /// <summary>
        /// Initializes HammerDb installation requirements.
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
                    case LinuxDistribution.RHEL7:
                    case LinuxDistribution.RHEL8:
                    case LinuxDistribution.CentOS7:
                        break;

                    default:
                        throw new WorkloadException(
                            $"HammerDb installation is not supported on the current Unix/Linux distro." +
                            $" Supported distros include:" +
                            $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Ubuntu)}, {Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Debian)}" +
                            $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.RHEL7)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.RHEL8)}" +
                            $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.CentOS7)}",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                DependencyPath workloadPackage = await this.systemManager.PackageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

                this.HammerDBExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "HammerDB-4.6.exe");
            }
            else
            {
                throw new WorkloadException(
                            $"HammerDb Installation is not supported on the current platform '{this.Platform}'." +
                            $"Supported Platforms include:" +
                            $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64)}, " +
                            $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64)}",
                            ErrorReason.PlatformNotSupported);
            }

        }

        /// <summary>
        /// Executes HammerDb installation steps.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                await this.RetryPolicy.ExecuteAsync(async () =>
                {
                    // sudo wget https://github.com/TPC-Council/HammerDB/releases/download/v4.5/HammerDB-4.5-Linux.tar.gz
                    await this.ExecuteCommandAsync("wget", $"https://github.com/TPC-Council/HammerDB/releases/download/v{this.Version}/HammerDB-{this.Version}-Linux.tar.gz", this.PlatformSpecifics.PackagesDirectory, telemetryContext, cancellationToken)
                       .ConfigureAwait(false);

                }).ConfigureAwait(false);

                await this.ExecuteCommandAsync("tar", $"-xvzf HammerDB-{this.Version}-Linux.tar.gz", this.PlatformSpecifics.PackagesDirectory, telemetryContext, cancellationToken)
                        .ConfigureAwait(false);

                string hammerDbInstallationPath = this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, $"HammerDB-{this.Version}");

                DependencyPath hammerDbPackage = new DependencyPath(HammerDbInstallation.HammerDbPackage, hammerDbInstallationPath);

                await this.systemManager.PackageManager.RegisterPackageAsync(hammerDbPackage, cancellationToken).ConfigureAwait(false);
            }
            else if (this.Platform == PlatformID.Win32NT)
            {
                string hammerDBInstallCommandArguments = $"--mode \"unattended\"";

                // string postgresqlInstallCommand = $"Start-Process -NoNewWindow -FilePath \"{this.HammerDBExecutablePath}\" -PassThru -Wait -ArgumentList  '--mode \"unattended\"'";
                await this.ExecuteCommandAsync(this.HammerDBExecutablePath, hammerDBInstallCommandArguments, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

                string hammerDBInstallationPath = this.PlatformSpecifics.Combine("C:", "Program Files", "HammerDB-4.6");

                DependencyPath hammerDbPackage = new DependencyPath(HammerDbInstallation.HammerDbPackage, hammerDBInstallationPath);

                await this.systemManager.PackageManager.RegisterPackageAsync(hammerDbPackage, cancellationToken).ConfigureAwait(false);
            }

            /*await this.ExecuteCommandAsync("make", null, hammerDbPackage.Path, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);*/
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
                        this.Logger.LogProcessDetails<CompilerInstallation>(process, relatedContext);
                        process.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.DependencyInstallationFailed);
                    }
                }
            });
        }
    }
}
