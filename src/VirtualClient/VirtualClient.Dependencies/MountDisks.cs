// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// A dependency to mount each volume of each disk at a user specified mount point.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class MountDisks : VirtualClientComponent
    {
        private ISystemManagement systemManager;
        private IDiskManager diskManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="MountDisks"/> class.
        /// </summary>
        public MountDisks(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.diskManager = this.systemManager.DiskManager;
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
                return this.Parameters.GetValue<string>(nameof(this.MountPointPrefix), "mountPoint");
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

            IEnumerable<Disk> filteredDisks = this.GetFilteredDisks(disks, this.DiskFilter);

            if (filteredDisks?.Any() != true)
            {
                throw new WorkloadException(
                    "Expected disks based on filter not found. Given the parameters defined for the profile action/step or those passed " +
                    "in on the command line, the requisite disks do not exist on the system or could not be identified based on the properties " +
                    "of the existing disks.",
                    ErrorReason.DependencyNotFound);
            }

            if (await this.GenerateMountPointsAsync(filteredDisks, this.systemManager, cancellationToken))
            {
                // Refresh the disks to pickup the mount point changes.
                await Task.Delay(1000).ConfigureAwait(false);

                IEnumerable<Disk> updatedDisks = await this.diskManager.GetDisksAsync(cancellationToken);

                filteredDisks = this.GetFilteredDisks(updatedDisks, this.DiskFilter);
            }

            try
            {
                filteredDisks.ToList().ForEach(disk => disk.Volumes.ToList().ForEach(volume => this.Logger.LogTraceMessage($"Disk Target to Mount: '{disk.DevicePath ?? string.Empty},{volume.DevicePath ?? string.Empty},{volume.AccessPaths?.First()}'")));
            }
            catch (Exception)
            {
                // Trying best to log
            }
        }

        /// <summary>
        /// List all Filtered Disks after applying Disk Filter
        /// </summary>
        /// <param name="disks"></param>
        /// <param name="diskFilter"></param>
        /// <returns></returns>
        private IEnumerable<Disk> GetFilteredDisks(IEnumerable<Disk> disks, string diskFilter)
        {
            List<Disk> filteredDisks = new List<Disk>();
            diskFilter = string.IsNullOrWhiteSpace(diskFilter) ? DiskFilters.DefaultDiskFilter : diskFilter;
            filteredDisks = DiskFilters.FilterDisks(disks, diskFilter, this.Platform).ToList();

            return filteredDisks;
        }

        /// <summary>
        /// Mount each volume of each disk at the user specified mount point
        /// </summary>
        private async Task<bool> GenerateMountPointsAsync(IEnumerable<Disk> disks, ISystemManagement systemManager, CancellationToken cancellationToken)
        {
            bool mountPointsCreated = false;

            double counter = 0;
            // Don't mount any partition in OS drive.
            foreach (Disk disk in disks.Where(d => !d.IsOperatingSystem()))
            {
                // mount every volume that doesn't have an accessPath.
                foreach (DiskVolume volume in disk.Volumes.Where(v => v.AccessPaths?.Any() != true))
                {
                    string newMountPoint = $"{this.MountPointPrefix}{counter++}";
                    
                    if (!systemManager.FileSystem.Directory.Exists(newMountPoint))
                    {
                        systemManager.FileSystem.Directory.CreateDirectory(newMountPoint).Create();
                    }

                    await this.diskManager.CreateMountPointAsync(volume, newMountPoint, cancellationToken);

                    mountPointsCreated = true;
                }
            }

            return mountPointsCreated;
        }
    }
}
