// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides methods to manage disks on the system.
    /// </summary>
    public interface IDiskManager
    {
        /// <summary>
        /// Creates a mount point for the volume provided.
        /// </summary>
        /// <param name="volume">The partition/volume to which the mount point will be created.</param>
        /// <param name="mountPoint">The mount point to associate with the partition.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        Task CreateMountPointAsync(DiskVolume volume, string mountPoint, CancellationToken cancellationToken);

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
        Task FormatDiskAsync(Disk disk, PartitionType partitionType, FileSystemType fileSystemType, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the set of physical disks that exist on the system.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        Task<IEnumerable<Disk>> GetDisksAsync(CancellationToken cancellationToken);
    }
}
