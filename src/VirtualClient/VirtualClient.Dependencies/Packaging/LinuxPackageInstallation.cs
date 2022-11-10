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
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides functionality for downloading and installing Linux packages
    /// on the system.
    /// </summary>
    public class LinuxPackageInstallation : VirtualClientComponent
    {
        private ISystemManagement systemManagement;
        private LinuxDistributionInfo linuxDistroInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinuxPackageInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public LinuxPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The retry policy to apply to package install for handling transient errors.
        /// </summary>
        public IAsyncPolicy InstallRetryPolicy { get; set; } = Policy
            .Handle<WorkloadException>(exc => exc.Reason == ErrorReason.DependencyInstallationFailed)
            .WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries * 2));

        /// <summary>
        /// Repositories that are specific to your distro.
        /// </summary>
        public string DistroSpecificRepositories
        {
            get
            {
                // example: Repositories-Ubuntu, Repository-RHEL8
                return this.Parameters.GetValue<string>($"Repositories-{this.linuxDistroInfo.LinuxDistribution.ToString()}", string.Empty);
            }
        }

        /// <summary>
        /// Packages that are specific to your distro.
        /// </summary>
        public string DistroSpecificPackages
        {
            get
            {
                // example: Packages-Ubuntu
                return this.Parameters.GetValue<string>($"Packages-{this.linuxDistroInfo.LinuxDistribution.ToString()}", string.Empty);
            }
        }

        /// <summary>
        /// Packages that have universal name cross package managers.
        /// </summary>
        public string Packages
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(LinuxPackageInstallation.Packages), string.Empty);
            }
        }

        /// <summary>
        /// Packages to install using apt.
        /// </summary>
        public string AptPackages
        {
            get
            {
                return this.Parameters.GetValue<string>("Packages-Apt", string.Empty);
            }
        }

        /// <summary>
        /// Apt package repositories to add.
        /// </summary>
        public string AptRepositories
        {
            get
            {
                return this.Parameters.GetValue<string>("Repositories-Apt", string.Empty);
            }
        }

        /// <summary>
        /// Packages to install using dnf.
        /// </summary>
        public string DnfPackages
        {
            get
            {
                return this.Parameters.GetValue<string>("Packages-Dnf", string.Empty);
            }
        }

        /// <summary>
        /// Dnf package repositories to add.
        /// </summary>
        public string DnfRepositories
        {
            get
            {
                return this.Parameters.GetValue<string>("Repositories-Dnf", string.Empty);
            }
        }

        /// <summary>
        /// Packages to install using yum.
        /// </summary>
        public string YumPackages
        {
            get
            {
                return this.Parameters.GetValue<string>("Packages-Yum", string.Empty);
            }
        }

        /// <summary>
        /// Yum package repositories to add.
        /// </summary>
        public string YumRepositories
        {
            get
            {
                return this.Parameters.GetValue<string>("Repositories-Yum", string.Empty);
            }
        }

        /// <summary>
        /// Packages to install using zypper.
        /// </summary>
        public string ZypperPackages
        {
            get
            {
                return this.Parameters.GetValue<string>("Packages-Zypper", string.Empty);
            }
        }

        /// <summary>
        /// Zypper package repositories to add.
        /// </summary>
        public string ZypperRepositories
        {
            get
            {
                return this.Parameters.GetValue<string>("Repositories-Zypper", string.Empty);
            }
        }

        /// <summary>
        /// Boolean value for installing interactive or not.
        /// </summary>
        public bool Interactive
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(AptPackageInstallation.Interactive), true);
            }
        }

        /// <summary>
        /// Executes the Linux package download/installation operation.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                this.linuxDistroInfo = await this.systemManagement.GetLinuxDistributionAsync(cancellationToken).ConfigureAwait(false);
                VirtualClientComponent installer;
                switch (this.linuxDistroInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                    case LinuxDistribution.Debian:
                        installer = this.InstantiateAptInstaller();
                        break;

                    case LinuxDistribution.CentOS8:
                    case LinuxDistribution.RHEL8:
                    case LinuxDistribution.Mariner:
                        installer = this.InstantiateDnfInstaller();
                        break;

                    case LinuxDistribution.CentOS7:
                    case LinuxDistribution.RHEL7:
                        installer = this.InstantiateYumInstaller();
                        break;

                    case LinuxDistribution.SUSE:
                        installer = this.InstantiateZypperInstaller();
                        break;

                    case LinuxDistribution.Flatcar:
                        throw new DependencyException(
                            $"You are on Linux distrubution {this.linuxDistroInfo.LinuxDistribution.ToString()}, which does not have a package manager.",
                            ErrorReason.LinuxDistributionNotSupported);

                    default:
                        throw new DependencyException(
                            $"You are on Linux distrubution {this.linuxDistroInfo.LinuxDistribution.ToString()}, which has not been onboarded to VirtualClient.",
                            ErrorReason.LinuxDistributionNotSupported);
                }

                if (!string.IsNullOrWhiteSpace(installer.Parameters.GetValue<string>("Packages", string.Empty))
                    || !string.IsNullOrWhiteSpace(installer.Parameters.GetValue<string>("Repositories", string.Empty)))
                {
                    await this.InstallPackageAsync(installer, cancellationToken).ConfigureAwait(false);
                }
                
            }
        }

        /// <summary>
        /// Invoke the Package installer component and install packages.
        /// </summary>
        /// <param name="installer">Corresponding Package installer.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns></returns>
        protected virtual Task InstallPackageAsync(VirtualClientComponent installer, CancellationToken cancellationToken)
        {
            return installer.ExecuteAsync(cancellationToken);
        }

        /// <inheritdoc />
        protected override bool IsSupported()
        {
            bool shouldExecute = false;
            if (base.IsSupported())
            {
                shouldExecute = this.Platform == PlatformID.Unix;
            }

            return shouldExecute;
        }

        private VirtualClientComponent InstantiateAptInstaller()
        {
            string allPackages = string.Join(",", this.Packages, this.AptPackages, this.DistroSpecificPackages).Trim(',');
            string allRepository = string.Join(",", this.AptRepositories, this.DistroSpecificRepositories).Trim(',');

            Dictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
            {
                { nameof(AptPackageInstallation.Packages), allPackages },
                { nameof(AptPackageInstallation.Repositories), allRepository },
                { nameof(AptPackageInstallation.Interactive), this.Interactive }
            };

            return new AptPackageInstallation(this.Dependencies, parameters);
        }

        private VirtualClientComponent InstantiateDnfInstaller()
        {
            string allPackages = string.Join(",", this.Packages, this.DnfPackages, this.DistroSpecificPackages).Trim(',');
            string allRepository = string.Join(",", this.DnfRepositories, this.DistroSpecificRepositories).Trim(',');

            Dictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
            {
                { nameof(DnfPackageInstallation.Packages), allPackages },
                { nameof(DnfPackageInstallation.Repositories), allRepository }
            };

            return new DnfPackageInstallation(this.Dependencies, parameters);
        }

        private VirtualClientComponent InstantiateYumInstaller()
        {
            string allPackages = string.Join(",", this.Packages, this.YumPackages, this.DistroSpecificPackages).Trim(',');
            string allRepository = string.Join(",", this.YumRepositories, this.DistroSpecificRepositories).Trim(',');

            Dictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
            {
                { nameof(YumPackageInstallation.Packages), allPackages },
                { nameof(YumPackageInstallation.Repositories), allRepository }
            };

            return new YumPackageInstallation(this.Dependencies, parameters);
        }

        private VirtualClientComponent InstantiateZypperInstaller()
        {
            string allPackages = string.Join(",", this.Packages, this.ZypperPackages, this.DistroSpecificPackages).Trim(',');
            string allRepository = string.Join(",", this.ZypperRepositories, this.DistroSpecificRepositories).Trim(',');

            Dictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
            {
                { nameof(ZypperPackageInstallation.Packages), allPackages },
                { nameof(ZypperPackageInstallation.Repositories), allRepository }
            };

            return new ZypperPackageInstallation(this.Dependencies, parameters);
        }
    }
}
