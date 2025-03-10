// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// A DiskManager that simply presents a OS disk, and does not try to do any
    /// discovery. Useful in containers.
    /// </summary>
    public class DummyDiskManager : DiskManager
    {
        private readonly DiskVolume volume;
        private readonly Disk disk;

        /// <summary>
        /// Create a new <see cref="DummyDiskManager"/> instance.
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="logger"></param>
        public DummyDiskManager(PlatformID platform, ILogger logger = null)
            : base(logger)
        {
            string defaultMountPath = platform == PlatformID.Win32NT ? "C:\\" : "/";
            string devicePath = platform == PlatformID.Win32NT ? "C:\\" : "/dev/sda";

            var volumeProps = new Dictionary<string, IConvertible>();

            // TODO: determine size with `df` or whatever on Windows

            if (platform == PlatformID.Win32NT)
            {
                volumeProps.Add(Disk.WindowsDiskProperties.Info, "Boot");
                volumeProps.Add(Disk.WindowsDiskProperties.Size, 1ul << 30);
            }
            else
            {
                volumeProps.Add(Disk.UnixDiskProperties.Size, 1ul << 30);
            }

            this.volume = new DiskVolume(0, devicePath, new[] { defaultMountPath }, volumeProps);
            this.disk = new Disk(0, devicePath, new[] { this.volume });
        }

        /// <summary>
        /// Get the set of disks.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task<IEnumerable<Disk>> GetDisksAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<Disk>>(new[] { this.disk });
        }

        /// <summary>
        /// No-op
        /// </summary>
        /// <param name="disk"></param>
        /// <param name="partitionType"></param>
        /// <param name="fileSystemType"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task FormatDiskAsync(Disk disk, PartitionType partitionType, FileSystemType fileSystemType, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// No-op
        /// </summary>
        /// <param name="volume"></param>
        /// <param name="mountPoint"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public override Task CreateMountPointAsync(DiskVolume volume, string mountPoint, CancellationToken cancellationToken)
        {
            if (volume != this.volume)
            {
                throw new ArgumentException("Invalid volume provided to DummyDiskManager::CreateMountPointAsync");
            }

            return Task.CompletedTask;
        }
    }
}
