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
    /// Provides functionality for installing specific version of Memcached on linux.
    /// </summary>
    public class MemcachedInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemcachedInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public MemcachedInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The name of the memcached package.
        /// </summary>
        public static string MemcachedPackage
        {
            get
            {
                return nameof(MemcachedPackage);
            }

        }

        /// <summary>
        /// The version of memcached to install.
        /// </summary>
        public string Version
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(MemcachedInstallation.Version), string.Empty);
            }

            set
            {
                this.Parameters[nameof(MemcachedInstallation.Version)] = value;
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
                    case LinuxDistribution.CentOS8:
                    case LinuxDistribution.RHEL8:
                    case LinuxDistribution.Mariner:
                        break;

                    default:
                        throw new WorkloadException(
                            $"Memcached installation is not supported on the current Unix/Linux distro." +
                            $" Supported distros include:" +
                            $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Ubuntu)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Debian)}" + 
                            $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.CentOS8)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.RHEL8)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Mariner)}",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
            else
            {
                throw new WorkloadException(
                            $"Memcached Installation is not supported on the current platform '{this.Platform}'." +
                            $"Supported Platforms include:" +
                            $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64)}, " +
                            $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64)}",
                            ErrorReason.PlatformNotSupported);
            }

        }

        /// <summary>
        /// Executes Memcached installation steps.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.RetryPolicy.ExecuteAsync(async () =>
            {
                // sudo wget https://memcached.org/files/memcached-1.6.17.tar.gz
                await this.ExecuteCommandAsync("wget", $"https://memcached.org/files/memcached-{this.Version}.tar.gz", this.PlatformSpecifics.PackagesDirectory, telemetryContext, cancellationToken)
                   .ConfigureAwait(false);

            }).ConfigureAwait(false);

            await this.ExecuteCommandAsync("tar", $"-xvzf memcached-{this.Version}.tar.gz", this.PlatformSpecifics.PackagesDirectory, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

            string memcachedInstallationPath = this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, $"memcached-{this.Version}");

            DependencyPath memcachedPackage = new DependencyPath(MemcachedInstallation.MemcachedPackage, memcachedInstallationPath);

            await this.ExecuteCommandAsync("./configure", null, memcachedPackage.Path, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.ExecuteCommandAsync("make", null, memcachedPackage.Path, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

            await this.systemManager.PackageManager.RegisterPackageAsync(memcachedPackage, cancellationToken).ConfigureAwait(false);
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
