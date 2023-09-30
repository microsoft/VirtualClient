// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
// in opensoruce to download redis from apt package manager. 
namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.VisualBasic;
    using Newtonsoft.Json;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides functionality for installing latest version of Redis from Apt package manager on specific OS distribution version.
    /// </summary>
    [UnixCompatible]
    public class RedisPackageInstallation : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManager;
        private string installRedisCommand;
        private IPackageManager packageManager;
        private IStateManager stateManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisPackageInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public RedisPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManager = dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManager.PackageManager;
            this.stateManager = this.systemManager.StateManager;
            this.fileSystem = this.systemManager.FileSystem;
        }

        /// <summary>
        /// The version of redis to install from the apt repository.This version should have exact version number and release information. e.g: 5:6.0.16-1ubuntu1
        /// </summary>
        public string Version
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(RedisPackageInstallation.Version), string.Empty);
            }

            set
            {
                this.Parameters[nameof(RedisPackageInstallation.Version)] = value;
            }
        }

        /// <summary>
        /// A policy that defines how the component will retry when
        /// it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// The path to the Redis package for installation.
        /// </summary>
        protected string PackagePath { get; set; }

        /// <summary>
        /// Initializes redis installation requirements.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform != PlatformID.Unix)
            {
                throw new WorkloadException($"Unsupported platform. The platform '{this.Platform}' is not supported.", ErrorReason.NotSupported);
            }

            if (this.Platform == PlatformID.Unix)
            {
                LinuxDistributionInfo distroInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken);

                switch (distroInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                        break;

                    default:
                        throw new WorkloadException(
                            $"Redis installation is not supported by Virtual Client on the current Unix/Linux distro '{distroInfo.LinuxDistribution}'.",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }

            this.PackagePath = this.PlatformSpecifics.GetPackagePath(this.PackageName);
        }

        /// <summary>
        /// Executes Redis installation steps.
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
                        await this.InstallOnUbuntuAsync(telemetryContext, cancellationToken);
                        break;
                }
            }
        }

        private async Task InstallOnUbuntuAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Version != string.Empty)
            {
                this.installRedisCommand = $"install redis={this.Version} -y";
            }
            else
            {
                this.installRedisCommand = $"install redis -y";

            }

            await this.ExecuteCommandAsync("apt", "update", Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);
            await this.ExecuteCommandAsync("apt", this.installRedisCommand, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            this.fileSystem.Directory.CreateDirectory(this.PackagePath);
            this.fileSystem.Directory.CreateDirectory(this.PlatformSpecifics.Combine(this.PackagePath, "src"));

            await this.ExecuteCommandAsync("cp", $"/usr/bin/redis-server {this.PlatformSpecifics.Combine(this.PackagePath, "src")}", Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            DependencyPath redisPackage = new DependencyPath(this.PackageName, this.PackagePath);
            await this.systemManager.PackageManager.RegisterPackageAsync(redisPackage, cancellationToken)
                            .ConfigureAwait(false);

        }
    }
}
