// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides methods to manage disks on a Windows system.
    /// </summary>
    public class WindowsDiskManager : DiskManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsDiskManager"/> class.
        /// </summary>
        /// <param name="processManager">
        /// Manages the creation and execution of processes on the system.
        /// </param>
        /// <param name="logger">A logger for capturing disk management telemetry.</param>
        public WindowsDiskManager(ProcessManager processManager, ILogger logger = null)
            : base(logger)
        {
            processManager.ThrowIfNull(nameof(processManager));
            this.ProcessManager = processManager;
            this.RetryPolicy = Policy.Handle<Exception>(exc =>
            {
                bool retry = true;
                if (exc.Message.Contains("requires elevation.", StringComparison.OrdinalIgnoreCase))
                {
                    retry = false;
                }

                return retry;
            }).WaitAndRetryAsync(10, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.WaitTime = TimeSpan.FromMilliseconds(300);
        }

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
            volume.ThrowIfNull(nameof(volume));
            mountPoint.ThrowIfNullOrWhiteSpace(nameof(mountPoint));

            EventContext context = EventContext.Persisted()
              .AddContext(nameof(volume), volume)
              .AddContext(nameof(mountPoint), mountPoint.ToString());

            return this.Logger.LogMessageAsync($"{nameof(WindowsDiskManager)}.CreateMountPoint", context, async () =>
            {
                try
                {
                    IConvertible volumeIdentifier;
                    if (volume.Index != null)
                    {
                        volumeIdentifier = volume.Index.Value;
                    }
                    else
                    {
                        if (!volume.Properties.TryGetValue(Disk.WindowsDiskProperties.Letter, out volumeIdentifier))
                        {
                            throw new ProcessException(
                                $"The volume at index '{volume.Index}' does not have either a volume index nor label defined. A mount point " +
                                $"cannot be assigned to a partition that does not have volume information associated.");
                        }
                    }

                    string command = string.Empty;
                    int retries = -1;

                    await this.RetryPolicy.ExecuteAsync(async () =>
                    {
                        retries++;
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // We use DiskPart to format the disks. DiskPart required an interactive process
                            // as we will have to supply input/responses.
                            using (IProcessProxy process = this.ProcessManager.CreateProcess("DiskPart", string.Empty))
                            {
                                try
                                {
                                    // We need to be able to interact with the DiskPart console.
                                    process.Interactive();
                                    if (!process.Start())
                                    {
                                        throw new ProcessException("Failed to enter DiskPart session.", ErrorReason.DiskFormatFailed);
                                    }

                                    // ** Select the Volume by drive letter.
                                    command = $"select volume {volumeIdentifier.ToString()}";
                                    await process.WriteInput(command)
                                        .WaitForResponseAsync($"Volume [0-9]+ is the selected volume.", cancellationToken, timeout: TimeSpan.FromSeconds(30))
                                        .ConfigureAwait(false);

                                    // ** Assign a mount path to the volume.
                                    command = $"assign mount={mountPoint}";
                                    await process.WriteInput(command)
                                        .WaitForResponseAsync($"DiskPart successfully assigned the drive letter or mount point.", cancellationToken, timeout: TimeSpan.FromSeconds(30))
                                        .ConfigureAwait(false);
                                }
                                catch (TimeoutException exc)
                                {
                                    throw new ProcessException(
                                        $"Failed to create mount point. DiskPart command(s) to create a mount point timed out (command={command}, retries={retries}). {Environment.NewLine}{process.StandardOutput}",
                                        exc,
                                        ErrorReason.DiskMountFailed);
                                }
                                finally
                                {
                                    context.AddProcessDetails(process.ToProcessDetails("diskpart"), "diskpartProcess");
                                }
                            }
                        }
                    }).ConfigureAwait(false);
                }
                catch (Win32Exception exc) when (exc.Message.Contains("requires elevation"))
                {
                    throw new ProcessException(
                        $"Requires elevated permissions. The current operation set requires the application to be run with administrator privileges.",
                        ErrorReason.Unauthorized);
                }
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
            disk.ThrowIfNull(nameof(disk));

            string command = string.Empty;
            int retries = -1;

            EventContext context = EventContext.Persisted()
                .AddContext(nameof(partitionType), partitionType.ToString())
                .AddContext(nameof(fileSystemType), fileSystemType.ToString())
                .AddContext(nameof(disk), disk);

            return this.Logger.LogMessageAsync($"{nameof(WindowsDiskManager)}.FormatDisk", context, async () =>
            {
                try
                {
                    // ** Assign a Drive Letter to the Partition
                    string nextDriveLetter = Enumerable.Range('C', 'Z' - 'C' + 1)
                        .Select(i => (char)i)
                        .Except(DriveInfo.GetDrives()
                        .Select(s => s.Name.First()))
                        .First().ToString();

                    await this.RetryPolicy.ExecuteAsync(async () =>
                    {
                        retries++;
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // We use DiskPart to format the disks. DiskPart required an interactive process
                            // as we will have to supply input/responses.
                            using (IProcessProxy process = this.ProcessManager.CreateProcess("DiskPart", string.Empty))
                            {
                                this.Logger.LogTraceMessage($"Format disk attempt #{retries}", context);

                                try
                                {
                                    // We need to be able to interact with the DiskPart console.
                                    process.Interactive();
                                    if (!process.Start())
                                    {
                                        throw new ProcessException("Failed to enter DiskPart session.", ErrorReason.DiskFormatFailed);
                                    }

                                    // ** Select the Physical Disk
                                    command = $"select disk {disk.Index}";
                                    this.Logger.LogTraceMessage($"DiskPart Format: {command}", context);
                                    await process.WriteInput(command)
                                        .WaitForResponseAsync($"Disk {disk.Index} is now the selected disk.", cancellationToken, timeout: TimeSpan.FromSeconds(30))
                                        .ConfigureAwait(false);

                                    // ** Wipe the disk clean
                                    command = "clean";
                                    this.Logger.LogTraceMessage($"DiskPart: {command}", context);
                                    await process.WriteInput(command)
                                        .WaitForResponseAsync("DiskPart succeeded in cleaning the disk.", cancellationToken, timeout: TimeSpan.FromSeconds(60))
                                        .ConfigureAwait(false);

                                    // ** Create a Partition on the Disk
                                    command = "create partition primary";
                                    this.Logger.LogTraceMessage($"DiskPart: {command}", context);
                                    await process.WriteInput(command)
                                        .WaitForResponseAsync("DiskPart succeeded in creating the specified partition.", cancellationToken, timeout: TimeSpan.FromMinutes(60))
                                        .ConfigureAwait(false);

                                    // ** List the partitions so that we can find the primary partition just created
                                    command = "list partition";
                                    this.Logger.LogTraceMessage($"DiskPart: {command}", context);
                                    await process.WriteInput(command)
                                        .WaitForResponseAsync("Partition ###", cancellationToken, timeout: TimeSpan.FromSeconds(30))
                                        .ConfigureAwait(false);

                                    Match primaryPartitionMatch = Regex.Match(process.StandardOutput.ToString(), @"Partition ([0-9]+)\s*Primary");
                                    if (!primaryPartitionMatch.Success)
                                    {
                                        throw new DependencyException($"Primary partition not found in DiskPart output.", ErrorReason.DiskFormatFailed);
                                    }

                                    string primaryPartitionNumber = primaryPartitionMatch.Groups[1].Value.Trim();

                                    // ** Select the Primary Partition on the Disk
                                    command = $"select partition {primaryPartitionNumber}";
                                    this.Logger.LogTraceMessage($"DiskPart: {command}", context);
                                    await process.WriteInput(command)
                                        .WaitForResponseAsync($"Partition {primaryPartitionNumber} is now the selected partition.", cancellationToken, timeout: TimeSpan.FromSeconds(30))
                                        .ConfigureAwait(false);

                                    command = $"assign letter={nextDriveLetter}";
                                    this.Logger.LogTraceMessage($"DiskPart: {command}", context);
                                    await process.WriteInput(command)
                                        .WaitForResponseAsync("DiskPart successfully assigned the drive letter or mount point.", cancellationToken, timeout: TimeSpan.FromSeconds(30))
                                        .ConfigureAwait(false);

                                    // ** Select the Volume with the Drive Letter
                                    command = $"select volume {nextDriveLetter}";
                                    this.Logger.LogTraceMessage($"DiskPart: {command}", context);
                                    await process.WriteInput(command)
                                        .WaitForResponseAsync("Volume [0-9]+ is the selected volume.", cancellationToken, timeout: TimeSpan.FromSeconds(30))
                                        .ConfigureAwait(false);

                                    // ** Assign a mount point to the disk.
                                    command = $"format fs={fileSystemType.ToString().ToLowerInvariant()} quick";
                                    this.Logger.LogTraceMessage($"DiskPart: {command}", context);
                                    await process.WriteInput(command)
                                        .WaitForResponseAsync("DiskPart successfully formatted the volume.", cancellationToken, timeout: TimeSpan.FromMinutes(15))
                                        .ConfigureAwait(false);

                                    this.Logger.LogTraceMessage(process.StandardOutput.ToString(), context);
                                }
                                catch (TimeoutException exc)
                                {
                                    this.Logger.LogTraceMessage(exc.ToString(true, true));

                                    throw new ProcessException(
                                        $"Failed to format disk '{disk.Index}'. DiskPart command(s) to format the disk timed out (command={command}, retries={retries}). {Environment.NewLine}{process.StandardOutput}",
                                        exc,
                                        ErrorReason.DiskFormatFailed);
                                }
                                catch (Exception exc)
                                {
                                    this.Logger.LogTraceMessage(exc.ToString(true, true));

                                    throw new ProcessException(
                                        $"Failed to format disk '{disk.Index}'. DiskPart command(s) to format the disk failed (command={command}, retries={retries}). {Environment.NewLine}{process.StandardOutput}",
                                        exc,
                                        ErrorReason.DiskFormatFailed);
                                }
                                finally
                                {
                                    process.WriteInput($"exit");
                                    await Task.Delay(this.WaitTime).ConfigureAwait(false);
                                    context.AddProcessDetails(process.ToProcessDetails("diskpart"), "diskpartProcess");
                                }
                            }
                        }
                    }).ConfigureAwait(false);
                }
                catch (Win32Exception exc) when (exc.Message.Contains("requires elevation"))
                {
                    throw new ProcessException(
                        $"Requires elevated permissions. The current operation set requires the application to be run with administrator privileges.",
                        ErrorReason.Unauthorized);
                }
            });
        }

        /// <summary>
        /// Gets the set of physical disks that exist on the system.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public override async Task<IEnumerable<Disk>> GetDisksAsync(CancellationToken cancellationToken)
        {
            IEnumerable<Disk> disks = new List<Disk>();
            string command = string.Empty;
            int retries = -1;
            int waitModifier = 0;

            EventContext context = EventContext.Persisted();
            await this.Logger.LogMessageAsync($"{nameof(WindowsDiskManager)}.GetDisks", context, async () =>
            {
                try
                {
                    await this.RetryPolicy.ExecuteAsync(async () =>
                    {
                        retries++;
                        waitModifier++;
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // We use DiskPart to get the disk and partition information. DiskPart required an interactive process
                            // as we will have to supply input/responses.
                            using (IProcessProxy process = this.ProcessManager.CreateProcess("DiskPart", string.Empty))
                            {
                                try
                                {
                                    // We need to be able to interact with the DiskPart console.
                                    process.Interactive();
                                    if (!process.Start())
                                    {
                                        throw new ProcessException("Failed to enter DiskPart session.", ErrorReason.DiskInformationNotAvailable);
                                    }

                                    // List all physical disks on the system.
                                    command = "list disk";
                                    process.StandardOutput.Clear();
                                    await process.WriteInput(command)
                                        .WaitForResponseAsync(@"Disk ###", cancellationToken, timeout: TimeSpan.FromSeconds(30))
                                        .ConfigureAwait(false);

                                    // Allow time for the results to be fully output
                                    this.Logger.LogTraceMessage(process.StandardOutput.ToString(), context);
                                    await Task.Delay(this.WaitTime * waitModifier).ConfigureAwait(false);

                                    // Capture the list disk output to parse disk sizes
                                    string listDiskOutput = process.StandardOutput.ToString();

                                    IEnumerable<int> diskIndexes = WindowsDiskManager.ParseDiskIndexes(process.StandardOutput);
                                    if (diskIndexes?.Any() != true)
                                    {
                                        throw new ProcessException("DiskPart 'list disk' command did not return any disks.", ErrorReason.DiskInformationNotAvailable);
                                    }
                                    
                                    // Parse disk sizes from the list disk output
                                    IDictionary<int, string> diskSizes = WindowsDiskManager.ParseDiskSizes(listDiskOutput);
                                    
                                    foreach (int diskIndex in diskIndexes)
                                    {
                                        // ** Select the physical disk
                                        process.StandardOutput.Clear();
                                        command = $"select disk {diskIndex}";
                                        await process.WriteInput(command)
                                            .WaitForResponseAsync($"Disk {diskIndex} is now the selected disk.", cancellationToken, timeout: TimeSpan.FromSeconds(30))
                                            .ConfigureAwait(false);

                                        this.Logger.LogTraceMessage(process.StandardOutput.ToString(), context);
                                        await Task.Delay(this.WaitTime * waitModifier).ConfigureAwait(false);

                                        // ** Get the disk details
                                        process.StandardOutput.Clear();
                                        command = "detail disk";
                                        await process.WriteInput(command)
                                            .WaitForResponseAsync("(Volume ###)|(There are no volumes.)", cancellationToken, timeout: TimeSpan.FromSeconds(30))
                                            .ConfigureAwait(false);

                                        this.Logger.LogTraceMessage(process.StandardOutput.ToString(), context);
                                        await Task.Delay(this.WaitTime * waitModifier).ConfigureAwait(false);

                                        List<DiskVolume> diskVolumes = new List<DiskVolume>();
                                        IDictionary<string, IConvertible> diskProperties = WindowsDiskManager.ParseDiskProperties(process.StandardOutput);
                                        diskProperties[Disk.WindowsDiskProperties.Index] = diskIndex;

                                        // Add the size property from the list disk output
                                        if (diskSizes.TryGetValue(diskIndex, out string diskSize))
                                        {
                                            diskProperties[Disk.WindowsDiskProperties.Size] = diskSize;
                                        }

                                        if (!process.StandardOutput.ToString().Contains("There are no volumes", StringComparison.OrdinalIgnoreCase))
                                        {
                                            // ** List the partitions for this disk
                                            process.StandardOutput.Clear();
                                            command = "list partition";
                                            await process.WriteInput(command)
                                                .WaitForResponseAsync("(Partition ###)|(There are no partitions on this disk to show.)", cancellationToken, timeout: TimeSpan.FromSeconds(30))
                                                .ConfigureAwait(false);

                                            this.Logger.LogTraceMessage(process.StandardOutput.ToString(), context);
                                            await Task.Delay(this.WaitTime * waitModifier).ConfigureAwait(false);

                                            IEnumerable<int> diskPartitionIndexes = WindowsDiskManager.ParseDiskPartitionIndexes(process.StandardOutput);

                                            // Note that there will NOT be any partitions/indexes before the disks have been prepared.
                                            if (diskPartitionIndexes?.Any() == true)
                                            {
                                                foreach (int partitionIndex in diskPartitionIndexes)
                                                {
                                                    // ** Select the disk partition
                                                    process.StandardOutput.Clear();
                                                    command = $"select partition {partitionIndex}";
                                                    await process.WriteInput(command)
                                                        .WaitForResponseAsync($"Partition {partitionIndex} is now the selected partition.", cancellationToken, timeout: TimeSpan.FromSeconds(30))
                                                        .ConfigureAwait(false);

                                                    this.Logger.LogTraceMessage(process.StandardOutput.ToString(), context);
                                                    await Task.Delay(this.WaitTime * waitModifier).ConfigureAwait(false);

                                                    // ** Get the disk partition details
                                                    process.StandardOutput.Clear();
                                                    command = "detail partition";
                                                    await process.WriteInput(command)
                                                        .WaitForResponseAsync($"(Volume ###)|(There is no volume associated with this partition.)", cancellationToken, timeout: TimeSpan.FromSeconds(30))
                                                        .ConfigureAwait(false);

                                                    this.Logger.LogTraceMessage(process.StandardOutput.ToString(), context);
                                                    await Task.Delay(this.WaitTime * waitModifier).ConfigureAwait(false);

                                                    DiskVolume volume = WindowsDiskManager.ParseDiskVolume(process.StandardOutput);
                                                    diskVolumes.Add(volume);
                                                }
                                            }
                                        }

                                        process.ThrowIfErrored<ProcessException>(ProcessProxy.DefaultSuccessCodes, process.StandardError.ToString());

                                        if (!diskProperties.TryGetValue(Disk.WindowsDiskProperties.LogicalUnitId, out IConvertible logicalUnit))
                                        {
                                            WindowsDiskManager.ThrowOnDiskPropertyMissing(Disk.WindowsDiskProperties.LogicalUnitId);
                                        }

                                        Disk disk = new Disk(
                                            diskIndex,
                                            $@"\\.\PHYSICALDISK{diskIndex}",
                                            diskVolumes,
                                            diskProperties);

                                        disks = disks.Append(disk);
                                    }
                                }
                                catch (TimeoutException exc)
                                {
                                    throw new ProcessException(
                                        $"Failed to get disks. DiskPart command(s) to get disks timed out (command={command}, retries={retries}). {Environment.NewLine}{process.StandardOutput}",
                                        exc,
                                        ErrorReason.DiskInformationNotAvailable);
                                }
                            }
                        }

                        context.AddContext(nameof(disks), disks);
                        return disks;

                    }).ConfigureAwait(false);
                }
                catch (Win32Exception exc) when (exc.Message.Contains("requires elevation"))
                {
                    throw new ProcessException(
                        $"Requires elevated permissions. The current operation set requires the application to be run with administrator privileges.",
                        exc,
                        ErrorReason.Unauthorized);
                }
            });

            return disks;
        }

        /// <summary>
        /// Returns as set of lines that are all the exact same width.
        /// </summary>
        /// <param name="lines">The original lines</param>
        /// <returns>A set of lines that are all the exact same width.</returns>
        protected static IEnumerable<string> GetFixedWidthLines(IEnumerable<string> lines)
        {
            List<string> fixedWidthLines = new List<string>();
            int longestLine = lines.OrderByDescending(line => line.Length).First().Length;

            foreach (string line in lines)
            {
                string fixedWidthLine = line;
                if (line.Length < longestLine)
                {
                    int diff = longestLine - line.Length;
                    fixedWidthLine = string.Concat(line, new string(' ', diff));
                }

                fixedWidthLines.Add(fixedWidthLine);
            }

            return fixedWidthLines;
        }

        /// <summary>
        /// Parses the physical disk indexes from the output of the DiskPart 'list disk' command.
        /// </summary>
        /// <param name="diskPartOutput">The output of the DiskPart command.</param>
        /// <returns>The set of physical disk indexes.</returns>
        protected static IEnumerable<int> ParseDiskIndexes(ConcurrentBuffer diskPartOutput)
        {
            diskPartOutput.ThrowIfInvalid(nameof(diskPartOutput), (std) => std.Length > 0);

            List<int> diskIndexes = new List<int>();
            MatchCollection diskMatches = Regex.Matches(diskPartOutput.ToString(), @"Disk\s+([0-9]+)");
            if (diskMatches?.Any() == true)
            {
                diskMatches.ToList().ForEach(match => diskIndexes.Add(int.Parse(match.Groups[1].Value)));
            }

            return diskIndexes;
        }

        /// <summary>
        /// Parses the disk sizes from the output of the DiskPart 'list disk' command.
        /// </summary>
        /// <param name="diskPartOutput">The output of the DiskPart command.</param>
        /// <returns>A dictionary mapping disk index to disk size string.</returns>
        protected static IDictionary<int, string> ParseDiskSizes(string diskPartOutput)
        {
            diskPartOutput.ThrowIfNullOrWhiteSpace(nameof(diskPartOutput));

            IDictionary<int, string> diskSizes = new Dictionary<int, string>();

            // Example output:
            //   Disk ###  Status         Size     Free     Dyn  Gpt
            //   --------  -------------  -------  -------  ---  ---
            //   Disk 0    Online          127 GB      0 B
            //   Disk 1    Online         1024 GB      0 B        *
            //   Disk 2    Online         1024 GB  1024 GB

            string normalizedOutput = diskPartOutput.Replace("DISKPART>", string.Empty, StringComparison.OrdinalIgnoreCase);
            string[] lines = normalizedOutput.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                // Match lines that start with "Disk" followed by a number
                Match diskMatch = Regex.Match(line, @"^\s*Disk\s+(\d+)\s+(\S+)\s+([\d\s]+\s*[KMGT]?B)", RegexOptions.IgnoreCase);
                if (diskMatch.Success)
                {
                    int diskIndex = int.Parse(diskMatch.Groups[1].Value);
                    string diskSize = diskMatch.Groups[3].Value.Trim();
                    diskSizes[diskIndex] = diskSize;
                }
            }

            return diskSizes;
        }

        /// <summary>
        /// Parses the physical disk properties from the output of the DiskPart 'detail disk' command.
        /// </summary>
        /// <param name="diskPartOutput">The output of the DiskPart command.</param>
        /// <returns>The properties of the physical disk.</returns>
        protected static IDictionary<string, IConvertible> ParseDiskProperties(ConcurrentBuffer diskPartOutput)
        {
            diskPartOutput.ThrowIfInvalid(nameof(diskPartOutput), (std) => std.Length > 0);

            IDictionary<string, IConvertible> diskProperties = new Dictionary<string, IConvertible>();

            string diskPartResults = WindowsDiskManager.NormalizeDiskPartResults(diskPartOutput);
            string diskModel = diskPartResults.Trim().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)?.First();
            diskProperties[Disk.WindowsDiskProperties.Model] = diskModel;

            MatchCollection properties = Regex.Matches(diskPartOutput.ToString(), @"([\x20-\x7E]+:[\x20-\x7E]+)");
            if (properties?.Any() == true)
            {
                properties.ToList().ForEach(match =>
                {
                    string[] keyValuePair = match.Value?.Split(":", StringSplitOptions.RemoveEmptyEntries);
                    if (keyValuePair.Length <= 2)
                    {
                        string propertyName = keyValuePair[0].Trim();
                        string propertyValue = keyValuePair.Length == 2 ? keyValuePair[1]?.Trim() : null;

                        diskProperties[propertyName] = propertyValue;
                    }
                });
            }

            return diskProperties;
        }

        /// <summary>
        /// Parses the physical disk partition indexes from the output of the DiskPart 'list partition' command.
        /// </summary>
        /// <param name="diskPartOutput">The output of the DiskPart command.</param>
        /// <returns>The set of physical disk partition indexes.</returns>
        protected static IEnumerable<int> ParseDiskPartitionIndexes(ConcurrentBuffer diskPartOutput)
        {
            diskPartOutput.ThrowIfInvalid(nameof(diskPartOutput), (std) => std.Length > 0);

            List<int> diskIndexes = new List<int>();
            string diskPartResults = WindowsDiskManager.NormalizeDiskPartResults(diskPartOutput);
            MatchCollection diskMatches = Regex.Matches(diskPartResults, @"Partition ([0-9]+)");
            if (diskMatches?.Any() == true)
            {
                diskMatches.ToList().ForEach(match => diskIndexes.Add(int.Parse(match.Groups[1].Value)));
            }

            return diskIndexes;
        }

        /// <summary>
        /// Parses the physical disk partition properties from the output of the DiskPart 'detail partition' command.
        /// </summary>
        /// <param name="diskPartOutput">The output of the DiskPart command.</param>
        /// <returns>The properties of the physical disk partition.</returns>
        protected static DiskVolume ParseDiskVolume(ConcurrentBuffer diskPartOutput)
        {
            diskPartOutput.ThrowIfInvalid(nameof(diskPartOutput), (std) => std.Length > 0);

            IDictionary<string, IConvertible> partitionProperties = new Dictionary<string, IConvertible>();
            List<string> mountPoints = new List<string>();

            string diskPartResults = WindowsDiskManager.NormalizeDiskPartResults(diskPartOutput);
            MatchCollection properties = Regex.Matches(diskPartResults, @"([\x20-\x7E]+:[^\\][\x20-\x73]+)", RegexOptions.IgnoreCase);

            string partitionIndex = Regex.Match(diskPartResults, "Partition ([0-9]+)", RegexOptions.IgnoreCase)?.Groups[1]?.Value ?? null;
            partitionProperties[Disk.WindowsDiskProperties.PartitionIndex] = partitionIndex;

            if (properties?.Any() == true)
            {
                properties.ToList().ForEach(match =>
                {
                    string[] keyValuePair = match.Value?.Split(":", StringSplitOptions.RemoveEmptyEntries);
                    if (keyValuePair.Length <= 2)
                    {
                        string propertyName = keyValuePair[0].Trim();
                        string propertyValue = keyValuePair.Length == 2 ? keyValuePair[1]?.Trim() : null;

                        partitionProperties[propertyName] = propertyValue;
                    }
                });
            }

            // The DiskPart results do not make it very easy to parse out the volume information. We rely upon
            // the fact that the columns are fixed width. This allows us to use the column header underlines to determine
            // the substrings.  We also have to account for mount paths that might exist for a given volume.
            //
            // DISKPART> detail partition
            // 
            // Partition 1
            // Type: 07
            // Hidden: No
            // Active: No
            // Offset in Bytes: 1048576
            //
            //  Volume ###    Ltr   Label        Fs     Type        Size     Status     Info
            //  ------------  ---   -----------  -----  ----------  -------  ---------  ------
            // * Volume 3     E                  NTFS   Partition   1023 GB  Healthy
            //    C:\Users\vcvmadmin\Desktop\MountPoint1\
            //    C:\Users\vcvmadmin\Desktop\MountPoint2\

            Match volumeInformation = Regex.Match(diskPartResults, @"Volume #+[\x20-\x7E\r\n]+", RegexOptions.IgnoreCase);
            if (volumeInformation.Success)
            {
                Match columnUnderlines = Regex.Match(volumeInformation.Value, @"(\x20*-{2,}\x20*)+", RegexOptions.IgnoreCase);

                if (columnUnderlines.Success == true)
                {
                    // Technique:
                    // Because DiskPart does not actually output information in standard fixed-width lines, we are using a
                    // technique of "mark-and-split" to decipher the values. The one thing that is consistent is the column
                    // underlines represent the actual width of each column. To get to the column values, we do the following:
                    //
                    // 1) Get the delimiter marker indexes. In practice, this is the index in the column underlines line where
                    //    each column ends.
                    // 
                    //    e.g.
                    //    ------------  ---   -----------  ----- 
                    //                ^    ^             ^      ^
                    //
                    // 2) Using these delimiter marker indexes, mark the column name lines and volume property lines. This enables
                    //    us to "project" columns onto these lines.
                    //
                    //    e.g.
                    //    Volume ###  |  Ltr|   Label    |    Fs  |   Type     |   Size   |  Status  |   Info |
                    //    Volume 3    |   E |            |    NTFS|   Partition|   1023 GB|  Healthy |        |
                    //
                    // 3) Now we can use a simple string-split to get to the exact column names and values per column.

                    IEnumerable<int> delimiterMarkerIndexes = WindowsDiskManager.GetMarkerIndexes(columnUnderlines.Value.Trim());
                    IEnumerable<string> fixedWidthLines = WindowsDiskManager.GetFixedWidthLines(volumeInformation.Value.Split(Environment.NewLine));

                    if (delimiterMarkerIndexes?.Any() == true && fixedWidthLines?.Any() == true)
                    {
                        Match columnNames = Regex.Match(fixedWidthLines.First(), @"(Volume #+)([\x20-\x7E]+)+", RegexOptions.IgnoreCase);
                        IEnumerable<string> columns = WindowsDiskManager.InsertDelimiters(delimiterMarkerIndexes, columnNames.Value)
                            .Split("|", StringSplitOptions.RemoveEmptyEntries)
                            .Select(col => col.Trim())
                            .Where(col => !string.IsNullOrWhiteSpace(col));

                        foreach (string line in fixedWidthLines)
                        {
                            string normalizedLine = line.TrimStart(' ', '*');
                            Match volumeMatch = Regex.Match(normalizedLine, @"(Volume\s+[0-9]+)([\x20-\x7E]+)+", RegexOptions.IgnoreCase);
                            Match mountPathMatch = Regex.Match(normalizedLine, @"[A-Z]:\\[\x20-\x7E]+", RegexOptions.IgnoreCase);

                            if (volumeMatch.Success)
                            {
                                // e.g.
                                // Volume 3     E NTFS   Partition   1023 GB Healthy
                                IEnumerable<string> volumeValues = WindowsDiskManager.InsertDelimiters(delimiterMarkerIndexes, normalizedLine)
                                    .Split("|", StringSplitOptions.RemoveEmptyEntries)
                                    .Select(val => val.Trim());

                                if (volumeValues.Count() != columns.Count())
                                {
                                    throw new FormatException($"The Windows disk management logic cannot correctly parse the DiskPart partition volume results.");
                                }

                                for (int i = 0; i < columns.Count(); i++)
                                {
                                    string columnName = columns.ElementAt(i);
                                    string columnValue = volumeValues.ElementAt(i);

                                    // Column/Value remappings
                                    if (string.Equals(columnName, "Volume ###", StringComparison.OrdinalIgnoreCase))
                                    {
                                        columnName = Disk.WindowsDiskProperties.Index;
                                        columnValue = Regex.Match(columnValue, "[0-9]+", RegexOptions.IgnoreCase)?.Value ?? columnValue;
                                    }

                                    partitionProperties[columnName] = !string.IsNullOrWhiteSpace(columnValue) ? columnValue : null;
                                }
                            }
                            else if (mountPathMatch.Success)
                            {
                                mountPoints.Add(normalizedLine.Trim());
                            }
                        }
                    }
                }
            }

            partitionProperties.TryGetValue(Disk.WindowsDiskProperties.Index, out IConvertible volumeIndex);

            if (partitionProperties.TryGetValue(Disk.WindowsDiskProperties.Letter, out IConvertible letter))
            {
                // The default access path for all disks on a Windows OS is the volume with a drive letter.
                // The drive letter can be used to access the file system on that volume.
                string driveLetter = letter?.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(driveLetter))
                {
                    // The drive letter is the preferred access path over a mount point.
                    mountPoints.Insert(0, $"{driveLetter}:\\");
                }
            }

            DiskVolume volume = new DiskVolume(
                index: volumeIndex?.ToInt32(CultureInfo.InvariantCulture),
                accessPaths: mountPoints,
                properties: partitionProperties);

            volume.DevicePath = volume.AccessPaths.Any() ? volume.AccessPaths.First() : string.Empty;

            return volume;
        }

        private static IEnumerable<int> GetMarkerIndexes(string columnUnderlines)
        {
            List<int> markerIndexes = new List<int>();
            char currentChar = char.MinValue;
            string columnLine = columnUnderlines.TrimStart();
            for (int i = 0; i < columnLine.Length; i++)
            {
                currentChar = columnLine[i];
                if (currentChar == ' ')
                {
                    // Before the end of the line.
                    if (columnLine[i - 1] != ' ')
                    {
                        markerIndexes.Add(i);
                    }
                }
            }

            markerIndexes.Add(columnLine.Length);

            return markerIndexes;
        }

        private static string InsertDelimiters(IEnumerable<int> markerIndexes, string line)
        {
            // Example
            // If we have this to begin with:
            // Volume 3     E                NTFS   Partition    1023 GB  Healthy            
            //
            // We want this in the end:
            // Volume 3  |   E  |           |   NTFS |   Partition |   1023 GB|  Healthy |       |
            //
            // This delimits the line based on the marker indexes. This line can now be split on those
            // markers to get a set of values from the line that match the number of columns expected.

            string delimitedLine = line;
            for (int i = 0; i < markerIndexes.Count(); i++)
            {
                delimitedLine = delimitedLine.Insert(markerIndexes.ElementAt(i) + i, "|");
            }

            return delimitedLine.TrimEnd();
        }

        private static string NormalizeDiskPartResults(ConcurrentBuffer diskPartResults)
        {
            return diskPartResults.ToString().Replace("DISKPART>", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        private static void ThrowOnDiskPropertyMissing(string propertyName)
        {
            throw new ProcessException(
                $"A required/expected disk property '{propertyName}' is missing for a disk on the system. This is not " +
                $"expected to happen and indicates the system may be in a corrupt state.",
                ErrorReason.DiskInformationNotAvailable);
        }
    }
}
