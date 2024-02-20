// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    /// <summary>
    /// A mock/test disk manager
    /// </summary>
    public class InMemoryDiskManager : List<Disk>, IDiskManager
    {
        private int lastPartitionIndex = 2;
        private char lastWindowsDriveLetter = 'D';
        private string lastLinuxDevice = "/dev/sdb1";

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryDiskManager"/> class.
        /// </summary>
        public InMemoryDiskManager()
        {
        }

        /// <summary>
        /// Mimics the behavior of creating a mount point.
        /// </summary>
        public Action<DiskVolume, string> OnCreateMountPoint { get; set; }

        /// <summary>
        /// Mimics the behaviour of creating mount points.
        /// </summary>
        public Func<IEnumerable<Disk>, ISystemManagement, bool> OnCreateMountPoints { get; set; }

        /// <summary>
        /// Mimics the behavior of creating a mount point.
        /// </summary>
        public Action<Disk, PartitionType, FileSystemType> OnFormatDisk { get; set; }

        /// <summary>
        /// Mimics the behavior of creating a mount point.
        /// </summary>
        public Action OnGetDisks { get; set; }

        /// <summary>
        /// Creates a mount point for the volume provided.
        /// </summary>
        /// <param name="volume">The partition/volume to which the mount point will be created.</param>
        /// <param name="mountPoint">The mount point to associate with the partition.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public Task CreateMountPointAsync(DiskVolume volume, string mountPoint, CancellationToken cancellationToken)
        {
            (volume.AccessPaths as List<string>).Add(mountPoint);
            this.OnCreateMountPoint?.Invoke(volume, mountPoint);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates a mount point for all the volumes for disks provided.
        /// </summary>
        /// <param name="disks">The partition/volume to which the mount point will be created.</param>
        /// <param name="systemManager">The mount point to associate with the partition.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public bool CreateMountPointsAsync(IEnumerable<Disk> disks, ISystemManagement systemManager, CancellationToken cancellationToken)
        {
            bool mountPointsCreated = false;
            if (this.OnCreateMountPoints != null)
            {
                mountPointsCreated = this.OnCreateMountPoints.Invoke(disks, systemManager);
            }
            else
            {
                foreach (Disk disk in disks.Where(d => !d.IsOperatingSystem()))
                {
                    // mount every volume that doesn't have an accessPath.
                    foreach (DiskVolume volume in disk.Volumes.Where(v => v.AccessPaths?.Any() != true))
                    {
                        string newMountPoint = volume.GetDefaultMountPoint();

                        this.CreateMountPointAsync(volume, newMountPoint, cancellationToken);

                        mountPointsCreated = true;
                    }
                }
            }            

            return mountPointsCreated;
        }

        /// <summary>
        /// Partitions and formats the disk for file system operations.
        /// </summary>
        /// <param name="disk">The disk to partition and format.</param>
        /// <param name="partitionType">The partition table type (e.g. GPT).</param>
        /// <param name="fileSystemType">The file system type to put on the partition (e.g. NTFS).</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// The set of disks with all properties after being partitioned and formatted.
        /// </returns>
        public Task FormatDiskAsync(Disk disk, PartitionType partitionType, FileSystemType fileSystemType, CancellationToken cancellationToken)
        {
            if (disk.IsOperatingSystem())
            {
                throw new NotSupportedException("An attempt to format the operating system disk should never happen.");
            }

            this.OnFormatDisk?.Invoke(disk, partitionType, fileSystemType);
            this.AddVolumeToDisk(disk, fileSystemType);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the set of physical disks that exist on the system.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public Task<IEnumerable<Disk>> GetDisksAsync(CancellationToken cancellationToken)
        {
            this.OnGetDisks?.Invoke();
            return Task.FromResult((IEnumerable<Disk>)this);
        }

        private void AddVolumeToDisk(Disk disk, FileSystemType fileSystemType)
        {
            DiskVolume newVolume = null;
            this.lastPartitionIndex++;

            switch (fileSystemType)
            {
                // Common Windows file system types.
                case FileSystemType.Ntfs:
                case FileSystemType.MsDos:
                    this.lastWindowsDriveLetter = (char)(this.lastWindowsDriveLetter + 1);

                    newVolume = new DiskVolume(
                        index: this.lastPartitionIndex,
                        properties: new Dictionary<string, IConvertible>
                        {
                            { "PartitionIndex", $"{this.lastPartitionIndex}" },
                            { "Type", "Partition" },
                            { "Hidden", "No" },
                            { "Active", "Yes" },
                            { "Offset in Bytes", "1048576" },
                            { "Index", $"{this.lastPartitionIndex + 3}" },
                            { "Ltr", $"{this.lastWindowsDriveLetter}" },
                            { "Label", null },
                            { "Fs", $"{fileSystemType.ToString().ToUpperInvariant()}" },
                            { "Size", "1023 GB" },
                            { "Status", "Healthy" },
                            { "Info", null }
                        },
                        accessPaths: new List<string> { $"{this.lastWindowsDriveLetter}:\\" });

                    break;

                default:
                    this.lastLinuxDevice = $"{this.lastLinuxDevice.Substring(0, this.lastLinuxDevice.Length - 2)}{this.lastLinuxDevice.Last() + 1}";
                    newVolume = new DiskVolume(
                       0,
                       devicePath: this.lastLinuxDevice,
                       properties: new Dictionary<string, IConvertible>
                       {
                            { "id", "volume" },
                            { "claimed", "true" },
                            { "class", "volume" },
                            { "handle", $"GUID:{Guid.NewGuid()}" },
                            { "description", $"{fileSystemType.ToString().ToUpperInvariant()} volume" },
                            { "vendor", "Linux" },
                            { "physid", "1" },
                            { "businfo", $"scsi@1:0.0.0,{this.lastPartitionIndex}" },
                            { "logicalname", $"{this.lastLinuxDevice}" },
                            { "dev", "8:49" },
                            { "version", "1.0" },
                            { "serial", Guid.NewGuid().ToString() },
                            { "size", "1099509530624" },
                            { "capacity", null },
                            { "created", "2021-05-05 19:29:17" },
                            { "filesystem", $"{fileSystemType.ToString().ToLowerInvariant()}" },
                            { "modified", "2021-05-11 22:48:47" },
                            { "mounted", "2021-05-05 19:29:29" },
                            { "name", "primary" },
                            { "state", "clean" },
                            { "journaled", string.Empty },
                            { "extended_attributes", "Extended Attributes" },
                            { "large_files", "4GB+ files" },
                            { "huge_files", "16TB+ files" },
                            { "dir_nlink", "directories with 65000+ subdirs" },
                            { "64bit", "64bit filesystem" },
                            { "extents", "extent-based allocation" },
                            { "ext4", string.Empty },
                            { "ext2", "EXT2/EXT3" },
                            { "initialized", "initialized volume" }
                       });
                    break;
            }

            (disk.Volumes as List<DiskVolume>).Add(newVolume);
        }
    }
}
