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
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// A dependency to mount each volume of each disk at a user specified mount point.
    /// </summary>
    public class MountDisks : VirtualClientComponent
    {
        private ISystemManagement systemManager;
        private IDiskManager diskManager;
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="MountDisks"/> class.
        /// </summary>
        public MountDisks(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.diskManager = this.systemManager.DiskManager;
            this.fileSystem = this.systemManager.FileSystem;
        }

        /// <summary>
        /// Disk filter string to filter disks to mount.
        /// </summary>
        public string DiskFilter
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DiskFilter), "OSDisk:false");
            }
        }

        /// <summary>
        /// User Defined Mount Point Name
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
        /// Optional Parameter to make Mount Location at the Root
        /// </summary>
        public string? MountLocation
        {
            get
            {
                this.Parameters.TryGetValue(nameof(MountDisks.MountLocation), out IConvertible mountLocation);
                return mountLocation?.ToString();
            }
        }

        /// <summary>
        /// Provides components and services for managing the system.
        /// </summary>
        protected ISystemManagement SystemManagement { get; private set; }

        /// <summary>
        /// Executes and monitors the Partition tool
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IEnumerable<Disk> disks = await this.diskManager.GetDisksAsync(cancellationToken).ConfigureAwait(false);

            if (disks?.Any() != true)
            {
                throw new WorkloadException(
                    "Unexpected scenario. The disks defined for the system could not be properly enumerated.",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            IEnumerable<Disk> filteredDisks = this.GetTargetDisks(disks, this.DiskFilter);

            if (filteredDisks?.Any() != true)
            {
                throw new WorkloadException(
                    "Expected disks based on filter not found. Given the parameters defined for the profile action/step or those passed " +
                    "in on the command line, the requisite disks do not exist on the system or could not be identified based on the properties " +
                    "of the existing disks.",
                    ErrorReason.DependencyNotFound);
            }

            if (await this.CreateMountPointsAsync(disks, telemetryContext, this.MountPointPrefix, this.MountLocation, cancellationToken))
            {
                // Refresh the disks to pickup the mount point changes.
                await Task.Delay(1000);

                IEnumerable<Disk> updatedDisks = await this.diskManager.GetDisksAsync(cancellationToken);
                filteredDisks = this.GetTargetDisks(updatedDisks, this.DiskFilter);
            }

            try
            {
                filteredDisks.ToList().ForEach(disk => disk.Volumes.ToList().ForEach(
                    volume => this.Logger.LogTraceMessage($"Disk Target to Mount: '{disk.DevicePath ?? string.Empty},{volume.DevicePath ?? string.Empty},{volume.AccessPaths?.First()}'")));
            }
            catch (Exception)
            {
                // Trying best to log
            }
        }

        private async Task<bool> CreateMountPointsAsync(IEnumerable<Disk> disks, EventContext telemetryContext, string mountPrefix = null, string mountDirectory = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            bool mountPointsCreated = false;

            // Notes on Windows vs. Linux Volumes:
            // Window uses letter such as C:, D:, E: for each unique volume. As such each distinct volume can
            // be expected to have a unique letter up until the letter Z. After that, NTFS folder mounts must be
            // used.
            //
            // Unix generally uses a device path that matches the physical disk path such as /dev/sdc -> /dev/sdc1, /dev/sdc2 and /dev/sdd -> /dev/sdd1
            // for each unique volume. However, this format is merely a convention and not required.

            // Don't mount any partition in OS drive.
            foreach (Disk disk in disks.Where(d => !d.IsOperatingSystem()))
            {
                IEnumerable<DiskVolume> diskVolumes = disk.Volumes.Where(v => v.AccessPaths?.Any() != true);

                if (diskVolumes?.Any() == true)
                {
                    // mount every volume that doesn't have an accessPath.
                    foreach (DiskVolume volume in disk.Volumes.Where(v => v.AccessPaths?.Any() != true))
                    {
                        string newMountPoint = null;
                        string mountPointName = volume.GetDefaultMountPointName(prefix: mountPrefix);
                        string mountPointPath = mountDirectory?.Trim();

                        if (string.IsNullOrWhiteSpace(mountPointPath))
                        {
                            switch (this.Platform)
                            {
                                case PlatformID.Unix:
                                    string user = this.PlatformSpecifics.GetLoggedInUser();
                                    if (string.Equals(user, "root"))
                                    {
                                        // When running as root:
                                        // /mnt_dev_sdc1
                                        // /mnt_dev_sdd1
                                        mountPointPath = "/";
                                    }
                                    else
                                    {
                                        // e.g.
                                        // When running as a given user (including when sudo is used):
                                        // /home/user/mnt_dev_sdc1
                                        // /home/user/mnt_dev_sdd1
                                        // /home/user/mnt_dev_sdd2
                                        mountPointPath = $"/home/{user}";
                                    }

                                    break;

                                case PlatformID.Win32NT:
                                    // e.g.
                                    // C:\Users\User\mnt_c
                                    // C:\Users\User\mnt_d
                                    mountPointPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                                    break;
                            }
                        }

                        if (this.Platform == PlatformID.Unix && !mountPointPath.StartsWith("/"))
                        {
                            mountPointPath = $"/{mountPointPath}";
                        }

                        newMountPoint = this.Combine(mountPointPath, mountPointName);

                        if (!this.fileSystem.Directory.Exists(newMountPoint))
                        {
                            this.fileSystem.Directory.CreateDirectory(newMountPoint);
                        }

                        await this.diskManager.CreateMountPointAsync(
                            volume, 
                            newMountPoint, 
                            CancellationToken.None);

                        // We want the mount point and directory structure to be owned by the user executing
                        // the application. This helps to prevent permissions issues.
                        string loggedInUser = this.PlatformSpecifics.GetLoggedInUser();

                        await this.systemManager.SetFullPermissionsAsync(
                            newMountPoint, 
                            this.Platform, 
                            telemetryContext,
                            CancellationToken.None, 
                            loggedInUser,
                            this.Logger);

                        mountPointsCreated = true;
                    }
                }
            }

            return mountPointsCreated;
        }

        private IEnumerable<Disk> GetTargetDisks(IEnumerable<Disk> disks, string diskFilter)
        {
            List<Disk> filteredDisks = new List<Disk>();
            diskFilter = string.IsNullOrWhiteSpace(diskFilter) ? DiskFilters.DefaultDiskFilter : diskFilter;
            filteredDisks = DiskFilters.FilterDisks(disks, diskFilter, this.Platform).ToList();

            return filteredDisks;
        }
    }
}