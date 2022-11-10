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
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// An abstract virtual client action that formats the disks before execution
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class FormatDisks : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FormatDisks"/> class.
        /// </summary>
        public FormatDisks(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.SystemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.WaitTime = TimeSpan.FromSeconds(2);
        }

        /// <summary>
        /// Disk filter string to filter disks to format.
        /// </summary>
        public string DiskFilter
        {
            // Disk filter is removed from FormatDisk dependency parameters, because NVME disk doesn't return size information
            // for disks pre-formatting. TODO: Investigate if DiskPart can return size before formating.
            get
            {
                // Enforce filter to remove OS disk.
                return "OSDisk:false";
            }
        }

        /// <summary>
        /// The interval of time to wait in-between each individual disk
        /// formatting. This is used to avoid race conditions with the OS kernel
        /// subsystem.
        /// </summary>
        public TimeSpan WaitTime { get; set; }

        /// <summary>
        /// Provides components and services for managing the system.
        /// </summary>
        protected ISystemManagement SystemManagement { get; private set; }

        /// <summary>
        /// Executes and monitors the Partition tool
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IEnumerable<Disk> systemDisks = await this.SystemManagement.DiskManager.GetDisksAsync(cancellationToken)
                .ConfigureAwait(false);

            systemDisks = DiskFilters.FilterDisks(systemDisks, this.DiskFilter, this.Platform).ToList();

            systemDisks.OrderBy(d => d.Index).ToList().ForEach(disk => this.Logger.LogTraceMessage(
                $"Disk: Index={disk.Index}{Environment.NewLine}" +
                $"- IsFormatted={disk.Volumes.Any()}{Environment.NewLine}" +
                $"- IsOperatingSystemDisk={disk.IsOperatingSystem()}{Environment.NewLine}"));

            this.Logger.LogMessage($"{nameof(FormatDisks)}.Disks", LogLevel.Information, telemetryContext.Clone().AddContext("disks", systemDisks));

            if (systemDisks?.Any(disk => !disk.Volumes.Any()) == true)
            {
                // Only disks that do not have any partitions/volumes are formatted. Disks that already have a file system
                // are left alone. The operating system disk is never formatted. There should never be a case where it is not already
                // formatted, but we put a check in place to be 100% sure.
                IEnumerable<Disk> disksToFormat = systemDisks.Where(disk => !disk.Volumes.Any() && !disk.IsOperatingSystem());
                this.Logger.LogMessage($"{nameof(FormatDisks)}.DisksToFormat", LogLevel.Information, telemetryContext.Clone().AddContext("disks", disksToFormat));
                disksToFormat.OrderBy(d => d.Index).ToList().ForEach(disk => this.Logger.LogTraceMessage($"Format: {$"Disk Index={disk.Index}"}"));

                PartitionType partitionType = PartitionType.Gpt;
                FileSystemType fileSystemType = this.GetFileSystemType(this.Platform);

                telemetryContext.AddContext(nameof(partitionType), partitionType.ToString());
                telemetryContext.AddContext(nameof(fileSystemType), fileSystemType.ToString());
                telemetryContext.AddContext("disks", disksToFormat.Select(disk => new
                {
                    index = disk.Index,
                    isOperatingSystemDisk = disk.IsOperatingSystem()
                }));

                foreach (Disk disk in disksToFormat)
                {
                    await this.SystemManagement.DiskManager.FormatDiskAsync(disk, partitionType, fileSystemType, cancellationToken)
                        .ConfigureAwait(false);

                    await Task.Delay(this.WaitTime, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Returns the file system to use for newly partitioned and formatted disks.
        /// </summary>
        /// <param name="platformId">The system/OS platform.</param>
        protected FileSystemType GetFileSystemType(PlatformID platformId)
        {
            FileSystemType fileSystemType = FileSystemType.MsDos;
            switch (platformId)
            {
                case PlatformID.Win32NT:
                    fileSystemType = FileSystemType.Ntfs;
                    break;

                case PlatformID.Unix:
                    fileSystemType = FileSystemType.Ext4;
                    break;

                default:
                    throw new WorkloadException(
                        $"The system platform '{platformId}' is not currently supported for disk partitioning/formatting.");
            }

            return fileSystemType;
        }
    }
}
