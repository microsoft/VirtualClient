// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// A dependency that stripes disks using a platform-specific script.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class StripeDisks : VirtualClientComponent
    {
        private const string LinuxScriptFileName = "stripe_disks.sh";
        private const string WindowsScriptFileName = "stripe_disks.cmd";
        private readonly ISystemManagement systemManagement;
        private readonly IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="StripeDisks"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public StripeDisks(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <summary>
        /// The number of disks to stripe. Default is 0, which means all eligible disks.
        /// </summary>
        public int DiskCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.DiskCount), 0);
            }
        }

        /// <summary>
        /// Disk filter string to filter disks to stripe.
        /// </summary>
        public string DiskFilter
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DiskFilter), "OSDisk:false");
            }
        }

        /// <summary>
        /// User defined mount point name prefix (e.g. "mnt_vc").
        /// </summary>
        public string MountPointPrefix
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.MountPointPrefix), out IConvertible prefix);
                return prefix?.ToString();
            }
        }

        /// <summary>
        /// Optional parameter to specify the root mount location directory.
        /// </summary>
        public string MountLocation
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.MountLocation), out IConvertible mountLocation);
                return mountLocation?.ToString();
            }
        }

        /// <summary>
        /// The resolved mount directory path for the striped volume.
        /// </summary>
        protected string MountDirectory { get; set; }

        /// <summary>
        /// The full path to the striping script.
        /// </summary>
        protected string ScriptPath { get; set; }

        /// <summary>
        /// The directory containing the striping script.
        /// </summary>
        protected string ScriptDirectory { get; set; }

        /// <summary>
        /// Initializes the component by locating the striping script in the system_config package.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.EvaluateParametersAsync(cancellationToken);

            DependencyPath package = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);

            string scriptFileName = this.Platform == PlatformID.Win32NT
                ? StripeDisks.WindowsScriptFileName
                : StripeDisks.LinuxScriptFileName;

            this.ScriptPath = this.Combine(package.Path, scriptFileName);

            if (!this.fileSystem.File.Exists(this.ScriptPath))
            {
                throw new DependencyException(
                    $"The expected striping script was not found at '{this.ScriptPath}'. The disk striping operation cannot " +
                    $"be executed without this script.",
                    ErrorReason.DependencyNotFound);
            }

            this.ScriptDirectory = this.fileSystem.Path.GetDirectoryName(this.ScriptPath);

            if (this.Platform != PlatformID.Win32NT)
            {
                this.MountDirectory = this.ResolveMountDirectory();
            }
        }

        /// <summary>
        /// Executes the striping script.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IEnumerable<Disk> allDisks = await this.systemManagement.DiskManager.GetDisksAsync(cancellationToken);
            IEnumerable<Disk> filteredDisks = DiskFilters.FilterDisks(allDisks, this.DiskFilter, this.Platform);

            if (!filteredDisks.Any())
            {
                throw new DependencyException(
                    $"No disks matching the filter '{this.DiskFilter}' were found on the system. The disk striping operation cannot proceed.",
                    ErrorReason.DependencyNotFound);
            }

            if (this.DiskCount > 0)
            {
                if (filteredDisks.Count() < this.DiskCount)
                {
                    throw new DependencyException(
                        $"The requested disk count ({this.DiskCount}) exceeds the number of eligible disks ({filteredDisks.Count()}) " +
                        $"matching the filter '{this.DiskFilter}'.",
                        ErrorReason.DependencyNotFound);
                }

                filteredDisks = filteredDisks
                    .OrderByDescending(d => d.SizeInBytes(this.Platform))
                    .Take(this.DiskCount);
            }

            string diskPaths = string.Join(",", filteredDisks.Select(d => d.DevicePath));

            string command;
            string commandArguments;

            if (this.Platform == PlatformID.Win32NT)
            {
                command = "cmd";
                commandArguments = $"/c {this.ScriptPath} --disks \"{diskPaths}\"";
            }
            else
            {
                command = "bash";
                commandArguments = $"{this.ScriptPath} --disks \"{diskPaths}\" --mountDirectory {this.MountDirectory}";
            }

            telemetryContext
                .AddContext("command", command)
                .AddContext("commandArguments", commandArguments);

            using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, this.ScriptDirectory, telemetryContext, cancellationToken, runElevated: true))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext);
                    process.ThrowIfErrored<DependencyException>(errorReason: ErrorReason.DependencyInstallationFailed);
                }
            }
        }

        /// <summary>
        /// Resolves the mount directory path using the same logic as <see cref="MountDisks"/>.
        /// Uses <see cref="MountLocation"/> if provided, otherwise determines the path based on
        /// the current platform and logged-in user.
        /// </summary>
        private string ResolveMountDirectory()
        {
            string mountPointPath = this.MountLocation?.Trim();

            if (string.IsNullOrWhiteSpace(mountPointPath))
            {
                string user = this.PlatformSpecifics.GetLoggedInUser();
                mountPointPath = string.Equals(user, "root") ? "/" : $"/home/{user}";
            }

            if (!mountPointPath.StartsWith("/"))
            {
                mountPointPath = $"/{mountPointPath}";
            }

            string prefix = this.MountPointPrefix ?? "mnt";
            return this.Combine(mountPointPath, $"{prefix}_raid0");
        }
    }
}
