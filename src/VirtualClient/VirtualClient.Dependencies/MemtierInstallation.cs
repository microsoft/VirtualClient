using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using VirtualClient.Common;
using VirtualClient.Common.Extensions;
using VirtualClient.Common.Telemetry;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    /// <summary>
    /// Provides functionality for installing specific version of memtier on linux.
    /// </summary>
    public class MemtierInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemtierInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public MemtierInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The name of the memtier package.
        /// </summary>
        public static string MemtierPackage
        {
            get
            {
                return nameof(MemtierPackage);
            }
        }

        /// <summary>
        /// The version of memtier to install.
        /// </summary>
        public string Version
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(MemtierInstallation.Version), string.Empty);
            }

            set
            {
                this.Parameters[nameof(MemtierInstallation.Version)] = value;
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
                var linuxDistributionInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken)
                                                .ConfigureAwait(false);
                switch (linuxDistributionInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                    case LinuxDistribution.Debian:
                    case LinuxDistribution.CentOS7:
                    case LinuxDistribution.RHEL7:
                    case LinuxDistribution.CentOS8:
                    case LinuxDistribution.RHEL8:
                    case LinuxDistribution.Mariner:
                        break;

                    default:
                        throw new WorkloadException(
                            $"Memtier Installation is not supported on the current Unix/Linux distro." +
                            $" Supported distros include:" +
                            $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Ubuntu)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Debian)}" + 
                            $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.CentOS8)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.RHEL8)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Mariner)}",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
            else
            {
                throw new WorkloadException(
                            $"Memtier Installation is not supported on the current platform '{this.Platform}'." +
                            $"Supported Platforms include:" +
                            $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64)}, " +
                            $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64)}",
                            ErrorReason.PlatformNotSupported);
            }
        }

        /// <summary>
        /// Executes Memtier installation steps.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string memtierInstallationPath = this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, $"memtier");

            DependencyPath memtierPackage = new DependencyPath(MemtierInstallation.MemtierPackage, memtierInstallationPath);

            await this.ExecuteCommandAsync("git", $"checkout {this.Version}", memtierPackage.Path, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            await this.ExecuteCommandAsync("autoreconf -ivf", null, memtierPackage.Path, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            await this.ExecuteCommandAsync("./configure", null, memtierPackage.Path, telemetryContext, cancellationToken)
                .ConfigureAwait(false);
            await this.ExecuteCommandAsync("make", null, memtierPackage.Path, telemetryContext, cancellationToken)
                .ConfigureAwait(false);
            await this.systemManager.PackageManager.RegisterPackageAsync(memtierPackage, cancellationToken).ConfigureAwait(false);
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
                        this.Logger.LogProcessDetails<MemtierInstallation>(process, relatedContext);
                        process.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.DependencyInstallationFailed);
                    }
                }
            });
        }
    }
}
