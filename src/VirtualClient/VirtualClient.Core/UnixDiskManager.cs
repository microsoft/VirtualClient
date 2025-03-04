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
    /// Provides methods to manage disks on a Unix/Linux system.
    /// </summary>
    public class UnixDiskManager : DiskManager
    {
        private const string BusInfo = "businfo";
        private const string Capacity = "capacity";
        private const string Capabilities = "capabilities";
        private const string Claimed = "claimed";
        private const string Class = "class";
        private const string Device = "dev";
        private const string Description = "description";
        private const string FileSystem = "filesystem";
        private const string Handle = "handle";
        private const string Id = "id";
        private const string LogicalName = "logicalname";
        private const string PhysicalId = "physid";
        private const string Product = "product";
        private const string Serial = "serial";
        private const string Size = "size";
        private const string Vendor = "vendor";
        private const string Version = "version";

        // Key   = The mount point/path
        // Value = The relative priority in relation to other paths or mount points when accessing the disk. < 0 = not typically accessible.
        private static readonly Dictionary<string, int> SystemDefinedMountPoints = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "/", 100 },
            { "/mnt", 100 },
            { "/boot/efi", -1 } // Not typically accessible
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="UnixDiskManager"/> class.
        /// </summary>
        /// <param name="processManager">
        /// Manages the creation and execution of processes on the system.
        /// </param>
        /// <param name="logger">A logger for capturing disk management telemetry.</param>
        public UnixDiskManager(ProcessManager processManager, ILogger logger = null)
            : base(logger)
        {
            processManager.ThrowIfNull(nameof(processManager));
            this.ProcessManager = processManager;
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(10, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.WaitTime = TimeSpan.FromSeconds(3);
            this.FormatTimeout = TimeSpan.FromHours(2);
        }

        /// <summary>
        /// A timeout to apply to the formatting of disks. The operation will timeout if the
        /// disk is not initialized and formatted within this period of time.
        /// </summary>
        public TimeSpan FormatTimeout { get; set; }

        /// <summary>
        /// Enables a custom built package of the "lshw" command used to get disk 
        /// information to be used instead of the default installation on the system.
        /// Note that a bug was found in the version of "lshw" (B.02.18) that is installed on some Ubuntu images. The bug causes the
        /// lshw application to return a "Segmentation Fault" error. We built the "lshw" command from the
        /// GitHub site where it is maintained that has the bug fix for this.
        /// </summary>
        public string LshwExecutable { get; set; }

        /// <summary>
        /// Manages the creation and execution of processes on the system.
        /// </summary>
        public ProcessManager ProcessManager { get; }

        /// <summary>
        /// The amount of time to wait after a disk command to wait for results
        /// to stream to standard output. Default = 1 second.
        /// </summary>
        public TimeSpan WaitTime { get; set; }

        /// <summary>
        /// Creates a mount point for the volume provided.
        /// </summary>
        /// <param name="volume">The partition/volume to which the mount point will be created.</param>
        /// <param name="mountPoint">The mount point to associate with the partition.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public override Task CreateMountPointAsync(DiskVolume volume, string mountPoint, CancellationToken cancellationToken)
        {
            EventContext context = EventContext.Persisted()
               .AddContext(nameof(volume), volume)
               .AddContext(nameof(mountPoint), mountPoint.ToString());

            return this.Logger.LogMessageAsync($"{nameof(UnixDiskManager)}.CreateMountPoint", context, async () =>
            {
                await this.AssignMountPointAsync(volume, mountPoint, context, cancellationToken).ConfigureAwait(false);
            });
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
        public override Task FormatDiskAsync(Disk disk, PartitionType partitionType, FileSystemType fileSystemType, CancellationToken cancellationToken)
        {
            EventContext context = EventContext.Persisted()
                .AddContext(nameof(partitionType), partitionType.ToString())
                .AddContext(nameof(fileSystemType), fileSystemType.ToString())
                .AddContext(nameof(disk), disk);

            return this.Logger.LogMessageAsync($"{nameof(UnixDiskManager)}.FormatDisk", context, async () =>
            {
                if (disk.Volumes.Any())
                {
                    // Clear any active mount points from the disk partitions.
                    foreach (DiskVolume partition in disk.Volumes)
                    {
                        await this.DeleteMountPointsAsync(partition, context, cancellationToken).ConfigureAwait(false);
                    }
                }

                await this.DeletePartitionsAsync(disk, context, cancellationToken).ConfigureAwait(false);
                await this.CreatePartitionAsync(disk, partitionType, context, cancellationToken).ConfigureAwait(false);
                await this.FormatDiskAsync(disk, fileSystemType, context, cancellationToken).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Gets the set of physical disks that exist on the system.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public override Task<IEnumerable<Disk>> GetDisksAsync(CancellationToken cancellationToken)
        {
            EventContext context = EventContext.Persisted()
                .AddContext("lshwCommand", !string.IsNullOrWhiteSpace(this.LshwExecutable) ? this.LshwExecutable : "System Installed");

            return this.Logger.LogMessageAsync($"{nameof(UnixDiskManager)}.GetDisks", context, async () =>
            {
                string lshwOutput = await this.ExecuteLshwDiskCommand(cancellationToken).ConfigureAwait(false);
                LshwDiskParser parser = new LshwDiskParser(lshwOutput);
                IEnumerable<Disk> disks = parser.Parse();

                context.AddContext(nameof(disks), disks);
                if (disks?.Any() != true)
                {
                    throw new ProcessException(
                        $"Failed to physical disk drives (and volumes) from the output of the lshw command.",
                        ErrorReason.DiskInformationNotAvailable);
                }

                return disks;
            });
        }

        private Task AssignMountPointAsync(DiskVolume volume, string mountPoint, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int retries = -1;
            string command = string.Empty;

            try
            {
                return this.RetryPolicy.ExecuteAsync(async () =>
                {
                    EventContext localContext = telemetryContext.Clone()
                        .AddContext(nameof(volume), volume)
                        .AddContext(nameof(mountPoint), mountPoint);

                    await this.Logger.LogMessageAsync($"{nameof(UnixDiskManager)}.AssignMountPoint", localContext, async () =>
                    {
                        retries++;
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // Allow time for the results to be fully output
                            await Task.Delay(this.WaitTime).ConfigureAwait(false);

                            // Example:
                            // sudo mount /dev/sdc1 /home/azureuser/VirtualClient.1.0.1585.119/linux-x64/local_sdc
                            command = $"mount {volume.DevicePath} {mountPoint}";
                            using (IProcessProxy process = this.ProcessManager.CreateProcess("sudo", command))
                            {
                                try
                                {
                                    await process.StartAndWaitAsync(cancellationToken, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                                    this.Logger.LogTraceMessage(process.StandardOutput.ToString());
                                    process.ThrowIfErrored<ProcessException>(ProcessProxy.DefaultSuccessCodes, process.StandardError.ToString());
                                }
                                finally
                                {
                                    localContext.AddContext(nameof(retries), retries);
                                    localContext.AddProcessContext(process);
                                }
                            }
                        }
                    }).ConfigureAwait(false);
                });
            }
            catch (Exception exc)
            {
                throw new ProcessException(
                    $"Failed to create mount point '{mountPoint}' on device '{volume.AccessPaths.First()}'. Unix command failed (command=sudo {command}, retries={retries})",
                    exc,
                    ErrorReason.DiskMountFailed);
            }
        }

        private async Task CreatePartitionAsync(Disk disk, PartitionType partitionType, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int retries = -1;
            string command = string.Empty;

            try
            {
                await this.RetryPolicy.ExecuteAsync(async () =>
                {
                    EventContext localContext = telemetryContext.Clone()
                        .AddContext(nameof(disk), disk)
                        .AddContext(nameof(partitionType), partitionType.ToString());

                    await this.Logger.LogMessageAsync($"{nameof(UnixDiskManager)}.CreatePartitionLabel", localContext, async () =>
                    {
                        retries++;
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(this.WaitTime).ConfigureAwait(false);

                            // Ensure the kernel is aware of the changes we just made to partitions on the
                            // system.
                            command = $"partprobe";
                            using (IProcessProxy process = this.ProcessManager.CreateProcess("sudo", command))
                            {
                                try
                                {
                                    await process.StartAndWaitAsync(cancellationToken, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                                    this.Logger.LogTraceMessage(process.StandardOutput.ToString());
                                    process.ThrowIfErrored<ProcessException>(ProcessProxy.DefaultSuccessCodes, process.StandardError.ToString());
                                }
                                finally
                                {
                                    localContext.AddContext(nameof(retries), retries);
                                    localContext.AddProcessContext(process, "partprobeProcess");
                                }
                            }
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // Example:
                            // sudo parted /dev/sdc mklabel gpt
                            command = $"parted -s {disk.DevicePath} mklabel {partitionType.ToString().ToLowerInvariant()}";
                            using (IProcessProxy process = this.ProcessManager.CreateProcess("sudo", command))
                            {
                                try
                                {
                                    await process.StartAndWaitAsync(cancellationToken, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                                    this.Logger.LogTraceMessage(process.StandardOutput.ToString());
                                    process.ThrowIfErrored<ProcessException>(ProcessProxy.DefaultSuccessCodes, process.StandardError.ToString());
                                }
                                finally
                                {
                                    localContext.AddContext(nameof(retries), retries);
                                    localContext.AddProcessContext(process, "mklabelProcess");
                                }
                            }
                        }
                    }).ConfigureAwait(false);
                }).ConfigureAwait(false);

                retries = -1;
                await this.RetryPolicy.ExecuteAsync(async () =>
                {
                    EventContext localContext = telemetryContext.Clone()
                       .AddContext(nameof(disk), disk)
                       .AddContext(nameof(partitionType), partitionType.ToString());

                    await this.Logger.LogMessageAsync($"{nameof(UnixDiskManager)}.CreatePartition", localContext, async () =>
                    {
                        retries++;
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // Allow time in-between each process. We found that this created more reliability
                            // in the execution of the processes 1 right after another.
                            await Task.Delay(this.WaitTime).ConfigureAwait(false);

                            // Example:
                            // sudo parted -a optimal /dev/sdc mkpart primary 0% 100%
                            command = $"parted -s -a optimal {disk.DevicePath} mkpart primary 0% 100%";
                            using (IProcessProxy process = this.ProcessManager.CreateProcess("sudo", command))
                            {
                                try
                                {
                                    await process.StartAndWaitAsync(cancellationToken, TimeSpan.FromMinutes(15)).ConfigureAwait(false);
                                    this.Logger.LogTraceMessage(process.StandardOutput.ToString());
                                    process.ThrowIfErrored<ProcessException>(ProcessProxy.DefaultSuccessCodes, process.StandardError.ToString());
                                }
                                finally
                                {
                                    localContext.AddContext(nameof(retries), retries);
                                    localContext.AddProcessContext(process, "partedProcess");
                                }
                            }
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(this.WaitTime).ConfigureAwait(false);

                            // Ensure the kernel is aware of the changes we just made to partitions on the
                            // system.
                            command = $"partprobe";
                            using (IProcessProxy process = this.ProcessManager.CreateProcess("sudo", command))
                            {
                                try
                                {
                                    await process.StartAndWaitAsync(cancellationToken, TimeSpan.FromMinutes(5)).ConfigureAwait(false);
                                    this.Logger.LogTraceMessage(process.StandardOutput.ToString());
                                    process.ThrowIfErrored<ProcessException>(ProcessProxy.DefaultSuccessCodes, process.StandardError.ToString());
                                }
                                finally
                                {
                                    localContext.AddContext(nameof(retries), retries);
                                    localContext.AddProcessContext(process, "partprobeProcess");
                                }
                            }
                        }
                    }).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            catch (Exception exc)
            {
                throw new ProcessException(
                    $"Failed to create disk partition '{partitionType.ToString()}' on disk '{disk.DevicePath}'. Unix command failed (command=sudo {command}, retries={retries})",
                    exc,
                    ErrorReason.DiskFormatFailed);
            }
        }

        private async Task DeleteMountPointsAsync(DiskVolume volume, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (volume.AccessPaths?.Any() == true)
            {
                foreach (string path in volume.AccessPaths)
                {
                    int retries = -1;
                    string command = string.Empty;

                    try
                    {
                        await this.RetryPolicy.ExecuteAsync(async () =>
                        {
                            EventContext localContext = telemetryContext.Clone()
                                .AddContext(nameof(volume), volume)
                                .AddContext(nameof(path), path);

                            await this.Logger.LogMessageAsync($"{nameof(UnixDiskManager)}.DeleteMountPoint", localContext, async () =>
                            {
                                retries++;
                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    // Allow time for the results to be fully output
                                    await Task.Delay(this.WaitTime).ConfigureAwait(false);

                                    // Example:
                                    // sudo umount -l /home/azureuser/VirtualClient.1.0.1585.119/linux-x64/local_sdc
                                    command = $"umount -l {path}";
                                    using (IProcessProxy process = this.ProcessManager.CreateProcess("sudo", command))
                                    {
                                        try
                                        {
                                            await process.StartAndWaitAsync(cancellationToken, TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                                            this.Logger.LogTraceMessage(process.StandardOutput.ToString());
                                            process.ThrowIfErrored<ProcessException>(ProcessProxy.DefaultSuccessCodes, process.StandardError.ToString());
                                        }
                                        finally
                                        {
                                            localContext.AddContext(nameof(retries), retries);
                                            localContext.AddProcessContext(process, "umountProcess");
                                        }
                                    }
                                }
                            }).ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    }
                    catch (Exception exc)
                    {
                        throw new ProcessException(
                            $"Failed to remove mount point from device '{volume.AccessPaths.First()}'. Unix command failed (command=sudo {command}, retries={retries})",
                            exc,
                            ErrorReason.DiskFormatFailed);
                    }
                }
                
            }
        }

        private async Task DeletePartitionsAsync(Disk disk, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int retries = -1;
            string command = string.Empty;

            try
            {
                foreach (DiskVolume volume in disk.Volumes)
                {
                    await this.RetryPolicy.ExecuteAsync(async () =>
                    {
                        EventContext localContext = telemetryContext.Clone()
                           .AddContext(nameof(disk), disk)
                           .AddContext(nameof(volume), volume);

                        await this.Logger.LogMessageAsync($"{nameof(UnixDiskManager)}.DeletePartition", localContext, async () =>
                        {
                            retries++;
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                // Allow time for the results to be fully output
                                await Task.Delay(this.WaitTime).ConfigureAwait(false);

                                command = $"parted {volume.DevicePath}";
                                using (IProcessProxy process = this.ProcessManager.CreateProcess("sudo", command))
                                {
                                    try
                                    {
                                        process.Interactive();
                                        if (!process.Start())
                                        {
                                            process.ThrowIfErrored<ProcessException>(ProcessProxy.DefaultSuccessCodes, process.StandardError.ToString());
                                        }

                                        // Allow time for the results to be fully output
                                        await Task.Delay(this.WaitTime).ConfigureAwait(false);

                                        this.Logger.LogTraceMessage(process.StandardOutput.ToString());
                                        process.StandardOutput.Clear();
                                        process.WriteInput($"rm {volume.Index}");

                                        await process.WaitForResponseAsync(@"\(parted\)", cancellationToken, timeout: TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                                        process.WriteInput("quit");
                                        this.Logger.LogTraceMessage(process.StandardOutput.ToString());

                                        await Task.Delay(this.WaitTime).ConfigureAwait(false);
                                        process.ThrowIfErrored<ProcessException>(ProcessProxy.DefaultSuccessCodes, process.StandardError.ToString());
                                    }
                                    finally
                                    {
                                        localContext.AddContext(nameof(retries), retries);
                                        localContext.AddProcessContext(process, "partedProcess");
                                    }
                                }
                            }
                        }).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                }
            }
            catch (Exception exc)
            {
                throw new ProcessException(
                    $"Failed to delete disk partition(s) for disk '{disk.DevicePath}'. Unix command failed (command=sudo {command}, retries={retries})",
                    exc,
                    ErrorReason.DiskFormatFailed);
            }
        }

        private Task FormatDiskAsync(Disk disk, FileSystemType fileSystemType, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int retries = -1;
            string command = string.Empty;

            try
            {
                return this.RetryPolicy.ExecuteAsync(async () =>
                {
                    EventContext localContext = telemetryContext.Clone()
                           .AddContext(nameof(disk), disk)
                           .AddContext(nameof(fileSystemType), fileSystemType.ToString());

                    await this.Logger.LogMessageAsync($"{nameof(UnixDiskManager)}.FormatDiskPartition", localContext, async () =>
                    {
                        retries++;
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // Allow time for the results to be fully output
                            await Task.Delay(this.WaitTime).ConfigureAwait(false);

                            // Example:
                            // sudo mkfs =t ext4 /dev/sdc1
                            // sudo mkfs =t ntfs --fast /dev/sdc1
                            // sudo mkfs -t ext4 /dev/nvmep1
                            string partitionPath = string.Empty;

                            if (disk.DevicePath.Contains("nvme"))
                            {
                                partitionPath = $"{disk.DevicePath}p1";
                            }
                            else
                            {
                                partitionPath = disk.DevicePath + "1";
                            }

                            command = $"mkfs -t {fileSystemType.ToString().ToLowerInvariant()}{(fileSystemType == FileSystemType.Ntfs ? " --fast" : string.Empty)} {partitionPath}";

                            using (IProcessProxy process = this.ProcessManager.CreateProcess("sudo", command))
                            {
                                try
                                {
                                    await process.StartAndWaitAsync(cancellationToken, this.FormatTimeout).ConfigureAwait(false);
                                    this.Logger.LogTraceMessage(process.StandardOutput.ToString());
                                    process.ThrowIfErrored<ProcessException>(ProcessProxy.DefaultSuccessCodes, process.StandardError.ToString());
                                }
                                finally
                                {
                                    localContext.AddProcessContext(process, "mkfsProcess");
                                }
                            }
                        }
                    }).ConfigureAwait(false);
                });
            }
            catch (Exception exc)
            {
                throw new ProcessException(
                    $"Failed to format disk partition on disk '{disk.DevicePath}'. Unix command failed (command=sudo {command}, retries={retries})",
                    exc,
                    ErrorReason.DiskFormatFailed);
            }
        }

        private async Task<string> ExecuteLshwDiskCommand(CancellationToken cancellationToken)
        {
            int retries = -1;
            string command = !string.IsNullOrWhiteSpace(this.LshwExecutable)
                ? $"{this.LshwExecutable} -xml -c disk -c volume" // Use custom built version of lshw
                : "lshw -xml -c disk -c volume";                  // Use default installation of lshw

            try
            {
                string output = string.Empty;
                await this.RetryPolicy.ExecuteAsync(async () =>
                {
                    retries++;
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        using (IProcessProxy process = this.ProcessManager.CreateProcess("sudo", $"{command}"))
                        {
                            await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                            process.ThrowIfErrored<ProcessException>(ProcessProxy.DefaultSuccessCodes, process.StandardError.ToString());

                            if (process.StandardOutput.Length <= 0)
                            {
                                throw new ProcessException(
                                    $"The lshw command did not return any disk drive information.",
                                    ErrorReason.DiskInformationNotAvailable);
                            }

                            output = process.StandardOutput.ToString();
                        }
                    }
                }).ConfigureAwait(false);

                return output;
            }
            catch (Exception exc)
            {
                throw new ProcessException(
                    $"Failed to get disk information. Unix command failed (command=sudo {command}, retries={retries})",
                    exc,
                    ErrorReason.DiskInformationNotAvailable);
            }
        }
    }
}
