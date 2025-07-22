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

            string mountLocation = null;
            if (string.Equals(this.MountLocation, "Root") && this.Platform == PlatformID.Unix)
            {
                mountLocation = $"/";
            }

            if (await this.CreateMountPointsAsync(disks, this.MountPointPrefix, mountLocation, cancellationToken))
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

        /// <summary>
        /// Creates mount points for any disks that do not have them already.
        /// </summary>
        /// <param name="disks">This disks for which mount points need to be created.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="mountPrefix">The prefix to use for the mount points (e.g. mnt_vc).</param>
        /// <param name="mountDirectory">The parent directory in which the mount points will be created. Default is the user home/profile directory.</param>
        /// <returns>True if any 1 or more mount points are created, false if none.</returns>
        private async Task<bool> CreateMountPointsAsync(IEnumerable<Disk> disks, string mountPrefix = null, string mountDirectory = null, CancellationToken cancellationToken = default(CancellationToken))
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
                    foreach (DiskVolume diskVolume in disk.Volumes.Where(v => v.AccessPaths?.Any() != true))
                    {
                        string newMountPoint = null;
                        string mountPointName = diskVolume.GetDefaultMountPointName(prefix: mountPrefix);
                        string mountPointPath = mountDirectory ?? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                        // e.g.
                        // C:\Users\User\mnt_c
                        // C:\Users\User\mnt_d
                        // /home/user/mnt_dev_sdc1
                        // /home/user/mnt_dev_sdd1
                        // /home/user/mnt_dev_sdd2
                        newMountPoint = this.Combine(mountPointPath, mountPointName);

                        if (!this.systemManager.FileSystem.Directory.Exists(newMountPoint))
                        {
                            this.systemManager.FileSystem.Directory.CreateDirectory(newMountPoint).Create();
                        }

                        await this.systemManager.DiskManager.CreateMountPointAsync(diskVolume, newMountPoint, cancellationToken);
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
