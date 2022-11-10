// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides base functionality and features for running I/O-targeted workloads.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public abstract class DiskPerformanceWorkloadExecutor : VirtualClientComponent
    {
        /// <summary>
        /// TestFocus -> DataIntegrity
        /// </summary>
        public const string TestFocusDataIntegrity = "DataIntegrity";

        /// <summary>
        /// Initializes a new instance of the <see cref="DiskPerformanceWorkloadExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the workload.</param>
        /// <param name="parameters">The set of parameters defined for the action in the profile definition.</param>
        protected DiskPerformanceWorkloadExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Defines a specific configuration to apply to the workload.
        /// </summary>
        public string Configuration
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DiskPerformanceWorkloadExecutor.Configuration), out IConvertible configuration);
                return configuration?.ToString();
            }

            set
            {
                this.Parameters[nameof(DiskPerformanceWorkloadExecutor.Configuration)] = value;
            }
        }

        /// <summary>
        /// Defines the command line specified in the profile.
        /// </summary>
        public string CommandLine
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DiskPerformanceWorkloadExecutor.CommandLine), out IConvertible commandLine);
                return commandLine?.ToString();
            }

            set
            {
                this.Parameters[nameof(DiskPerformanceWorkloadExecutor.CommandLine)] = value;
            }
        }

        /// <summary>
        /// True/false whether the test files that FIO uses in benchmark tests should be deleted at the end
        /// of each individual round of test execution.
        /// </summary>
        public bool DeleteTestFilesOnFinish
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(DiskPerformanceWorkloadExecutor.DeleteTestFilesOnFinish), true);
            }

            set
            {
                this.Parameters[nameof(DiskPerformanceWorkloadExecutor.DeleteTestFilesOnFinish)] = value;
            }
        }

        /// <summary>
        /// True/false whether the current command is meant to be a disk fill. Disk fill operations
        /// initialize/fill the disk and do not have any metrics tracking.
        /// </summary>
        public bool DiskFill
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(DiskPerformanceWorkloadExecutor.DiskFill), false);
            }

            set
            {
                this.Parameters[nameof(DiskPerformanceWorkloadExecutor.DiskFill)] = value;
            }
        }

        /// <summary>
        /// The size of the file/data to write to the disk in order to fill it with data. This must
        /// be an exact number (e.g. DiskSpd -> 469G, FIO -> 496GB).
        /// </summary>
        public string DiskFillSize
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DiskPerformanceWorkloadExecutor.DiskFillSize), out IConvertible diskFillSize);
                return diskFillSize?.ToString();
            }

            set
            {
                this.Parameters[nameof(DiskPerformanceWorkloadExecutor.DiskFillSize)] = value;
            }
        }

        /// <summary>
        /// The name of the test file that should use in workload tests.
        /// </summary>
        public string FileName
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DiskPerformanceWorkloadExecutor.FileName), out IConvertible fileName);
                return fileName?.ToString();
            }

            set
            {
                this.Parameters[nameof(DiskPerformanceWorkloadExecutor.FileName)] = value;
            }
        }

        /// <summary>
        /// The size of the test file that should use in workload tests (e.g. 496GB).
        /// </summary>
        public string FileSize
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DiskPerformanceWorkloadExecutor.FileSize), out IConvertible fileSize);
                return fileSize?.ToString();
            }

            set
            {
                this.Parameters[nameof(DiskPerformanceWorkloadExecutor.FileSize)] = value;
            }
        }

        /// <summary>
        /// Defines the model/strategy for how the disks will be tested.
        /// (e.g. SingleProcess = 1 for the entire system, SingleProcessPerDrive = 1 for each drive on the system).
        /// </summary>
        public string ProcessModel
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DiskPerformanceWorkloadExecutor.ProcessModel), WorkloadProcessModel.SingleProcess);
            }

            set
            {
                this.Parameters[nameof(DiskPerformanceWorkloadExecutor.ProcessModel)] = value;
            }
        }

        /// <summary>
        /// The disk I/O queue depth to use for running disk I/O operations. 
        /// Default = 16.
        /// </summary>
        public int QueueDepth
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(DiskPerformanceWorkloadExecutor.QueueDepth), 16);
            }

            set
            {
                this.Parameters[nameof(DiskPerformanceWorkloadExecutor.QueueDepth)] = value;
            }
        }

        /// <summary>
        /// The specific focus of the test if applicable (e.g. DataIntegrity).
        /// </summary>
        public string TestFocus
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DiskPerformanceWorkloadExecutor.TestFocus), out IConvertible testFocus);
                return testFocus?.ToString();
            }

            set
            {
                this.Parameters[nameof(DiskPerformanceWorkloadExecutor.TestFocus)] = value;
            }
        }

        /// <summary>
        /// Name of the test defined in profile.
        /// </summary>
        public string TestName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DiskPerformanceWorkloadExecutor.TestName));
            }

            set
            {
                this.Parameters[nameof(DiskPerformanceWorkloadExecutor.TestName)] = value;
            }
        }

        /// <summary>
        /// Disk filter string to filter disks to test.
        /// </summary>
        public string DiskFilter
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DiskPerformanceWorkloadExecutor.DiskFilter), "BiggestSize");
            }

            set
            {
                this.Parameters[nameof(DiskPerformanceWorkloadExecutor.DiskFilter)] = value;
            }
        }

        /// <summary>
        /// The number of threads to use for running disk I/O operations. 
        /// Default = system logical processor count.
        /// </summary>
        public int Threads
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(DiskPerformanceWorkloadExecutor.Threads), Environment.ProcessorCount);
            }

            set
            {
                this.Parameters[nameof(DiskPerformanceWorkloadExecutor.Threads)] = value;
            }
        }

        /// <summary>
        /// Provides features for management of the system/environment.
        /// </summary>
        protected ISystemManagement SystemManagement
        {
            get
            {
                return this.Dependencies.GetService<ISystemManagement>();
            }
        }

        /// <summary>
        /// Provides methods for interacting with the local file system.
        /// </summary>
        protected IFileSystem FileSystem
        {
            get
            {
                return this.SystemManagement.FileSystem;
            }
        }

        /// <summary>
        /// Applies any placeholder replacements to the parameters of the profile
        /// workload action.
        /// </summary>
        protected string ApplyParameters(string text, string fileSize = null, string diskFillSize = null, int? queueDepth = null, int? threads = null)
        {
            string updatedText = text;

            if (!string.IsNullOrWhiteSpace(fileSize))
            {
                updatedText = ProfilePlaceholders.Replace(nameof(this.FileSize), fileSize, updatedText);
            }

            if (!string.IsNullOrWhiteSpace(diskFillSize))
            {
                updatedText = ProfilePlaceholders.Replace(nameof(this.DiskFillSize), diskFillSize, updatedText);
            }

            if (queueDepth > 0)
            {
                updatedText = ProfilePlaceholders.Replace(nameof(this.QueueDepth), queueDepth, updatedText);
            }

            if (threads > 0)
            {
                updatedText = ProfilePlaceholders.Replace(nameof(this.Threads), threads, updatedText);
            }

            return updatedText;
        }

        /// <summary>
        /// Executes the I/O workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates mount points for any disks that do not have them already.
        /// </summary>
        /// <param name="disks">This disks on which to create the mount points.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected virtual async Task<bool> CreateMountPointsAsync(IEnumerable<Disk> disks, CancellationToken cancellationToken)
        {
            bool mountPointsCreated = false;

            // Don't mount any partition in OS drive.
            foreach (Disk disk in disks.Where(d => !d.IsOperatingSystem()))
            {
                // mount every volume that doesn't have an accessPath.
                foreach (DiskVolume volume in disk.Volumes.Where(v => v.AccessPaths?.Any() != true))
                {
                    string newMountPoint = volume.GetDefaultMountPoint();
                    this.Logger.LogTraceMessage($"Create Mount Point: {newMountPoint}");

                    EventContext relatedContext = EventContext.Persisted().Clone()
                        .AddContext(nameof(volume), volume)
                        .AddContext("mountPoint", newMountPoint);

                    await this.Logger.LogMessageAsync($"{this.GetType().Name}.CreateMountPoint", relatedContext, async () =>
                    {
                        string newMountPoint = volume.GetDefaultMountPoint();
                        if (!this.SystemManagement.FileSystem.Directory.Exists(newMountPoint))
                        {
                            this.SystemManagement.FileSystem.Directory.CreateDirectory(newMountPoint).Create();
                        }

                        await this.SystemManagement.DiskManager.CreateMountPointAsync(volume, newMountPoint, cancellationToken)
                            .ConfigureAwait(false);

                        mountPointsCreated = true;

                    }).ConfigureAwait(false);
                }
            }

            return mountPointsCreated;
        }

        /// <summary>
        /// Creates a process to run an I/O workload targeting the test files specified.
        /// </summary>
        /// <param name="executable">The full path to the I/O workload executable.</param>
        /// <param name="commandArguments">
        /// The command line arguments to supply to the I/O workload executable (e.g. --name=fio_randread_4GB_4k_d1_th1_direct --ioengine=libaio).
        /// </param>
        /// <param name="testedInstance">A name for the disks under test (e.g. remote_disk, remote_disk_premium_lrs).</param>
        /// <param name="disksToTest">The disks under test.</param>
        protected abstract DiskPerformanceWorkloadProcess CreateWorkloadProcess(string executable, string commandArguments, string testedInstance, params Disk[] disksToTest);

        /// <summary>
        /// Create a set of <see cref="DiskPerformanceWorkloadProcess"/>.
        /// </summary>
        /// <param name="executable">The fully qualified path to the disk spd executable.</param>
        /// <param name="commandArguments">A templatized command to give to the disk spd executable.</param>
        /// <param name="disks">The formatted disks.</param>
        /// <param name="processModel">
        /// The process model/strategy to use for I/O operations against the disks. Valid values include: SingleProcess, SingleProcessPerDisk.
        /// </param>
        protected virtual IEnumerable<DiskPerformanceWorkloadProcess> CreateWorkloadProcesses(string executable, string commandArguments, IEnumerable<Disk> disks, string processModel)
        {
            executable.ThrowIfNullOrWhiteSpace(nameof(executable));
            commandArguments.ThrowIfNullOrWhiteSpace(nameof(commandArguments));
            processModel.ThrowIfNullOrWhiteSpace(nameof(processModel));
            disks.ThrowIfNullOrEmpty(nameof(disks));

            EventContext telemetryContext = EventContext.Persisted();
            return this.Logger.LogMessage($"{this.GetType().Name}.CreateProcesses", telemetryContext, () =>
            {
                List<DiskPerformanceWorkloadProcess> processes = new List<DiskPerformanceWorkloadProcess>();
                string testedInstance = this.DiskFilter;

                if (string.Equals(processModel, WorkloadProcessModel.SingleProcess, StringComparison.OrdinalIgnoreCase))
                {
                    processes.Add(this.CreateWorkloadProcess(executable, commandArguments, testedInstance, disks.Select(disk => disk).ToArray()));
                }
                else if (string.Equals(processModel, WorkloadProcessModel.SingleProcessPerDisk, StringComparison.OrdinalIgnoreCase))
                {
                    processes.AddRange(new List<DiskPerformanceWorkloadProcess>(disks.Select(disk =>
                    {
                        return this.CreateWorkloadProcess(executable, commandArguments, testedInstance, disk);
                    })));
                }

                return processes;
            });
        }

        /// <summary>
        /// Deletes any test files associated with the workload.
        /// </summary>
        protected virtual async Task DeleteTestFilesAsync(IEnumerable<string> testFiles, IAsyncPolicy retryPolicy = null)
        {
            if (this.DeleteTestFilesOnFinish)
            {
                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("files", testFiles);

                await this.Logger.LogMessageAsync($"{this.GetType().Name}.DeleteFiles", telemetryContext, async () =>
                {
                    if (testFiles?.Any() == true)
                    {
                        foreach (string file in testFiles)
                        {
                            try
                            {
                                if (this.SystemManagement.FileSystem.File.Exists(file))
                                {
                                    this.Logger.LogTraceMessage($"Delete test file '{file}'");
                                    await this.SystemManagement.FileSystem.File.DeleteAsync(file, retryPolicy)
                                        .ConfigureAwait(false);
                                }
                            }
                            catch (Exception exc)
                            {
                                telemetryContext.AddError(exc);
                            }
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Returns the disks to test from the set of all disks.
        /// </summary>
        /// <param name="disks">All disks on the system.</param>
        protected IEnumerable<Disk> GetDisksToTest(IEnumerable<Disk> disks)
        {
            List<Disk> disksToTest = new List<Disk>();
            this.DiskFilter = string.IsNullOrWhiteSpace(this.DiskFilter) ? DiskFilters.DefaultDiskFilter : this.DiskFilter;
            disksToTest = DiskFilters.FilterDisks(disks, this.DiskFilter, this.Platform).ToList();

            return disksToTest;
        }

        /// <summary>
        /// Returns the name of the test file given a mount point.
        /// </summary>
        /// <param name="mountPoint">A mount point to the disk under test.</param>
        /// <returns>
        /// The full path to the test file.
        /// </returns>
        protected virtual string GetTestFile(string mountPoint)
        {
            mountPoint.ThrowIfNullOrWhiteSpace(nameof(mountPoint));
            return this.PlatformSpecifics.Combine(mountPoint, this.FileName ?? Path.GetRandomFileName());
        }

        /// <summary>
        /// Returns the set of tests defined by the 'Tests' instruction supplied on
        /// the command line.
        /// </summary>
        /// <returns>Enumeration of specificed tests, after separated by delimiters.</returns>
        protected IEnumerable<string> GetSpecificTests()
        {
            const string testsKey = "tests";
            List<string> tests = new List<string>();
            if (this.Parameters?.Any() == true && this.Parameters.ContainsKey(testsKey))
            {
                string[] fioTests = this.Parameters.GetValue<string>(testsKey)?.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                if (fioTests?.Any() == true)
                {
                    foreach (string test in fioTests)
                    {
                        tests.Add(test.Trim());
                    }
                }
            }

            return tests;
        }

        /// <summary>
        /// Returns true if the executor has registered that a disk fill was completed.
        /// </summary>
        protected async Task<bool> IsDiskFillCompleteAsync(CancellationToken cancellationToken)
        {
            string stateId = $"{this.GetType().Name}.DiskFill";
            WorkloadState state = await this.SystemManagement.StateManager.GetStateAsync<WorkloadState>(stateId, cancellationToken)
                .ConfigureAwait(false);

            return state != null;
        }

        /// <summary>
        /// Kills the process associated with the workload.
        /// </summary>
        protected virtual Task KillProcessAsync(DiskPerformanceWorkloadProcess workload)
        {
            return Task.Run(() =>
            {
                IProcessProxy process = workload.Process;

                try
                {
                    if (!process.HasExited)
                    {
                        EventContext telemetryContext = EventContext.Persisted()
                            .AddContext("process", process.Id)
                            .AddContext("command", workload.Command)
                            .AddContext("commandArguments", workload.CommandArguments);

                        this.Logger.LogMessage($"{this.GetType().Name}.KillProcess", telemetryContext, () =>
                        {
                            try
                            {
                                DateTime exitTime = DateTime.Now.AddSeconds(30);
                                while (!process.HasExited && DateTime.Now < exitTime)
                                {
                                    this.Logger.LogTraceMessage($"Kill process ID={process.Id}: {workload.Command} {workload.CommandArguments}");
                                    process.Kill();
                                    Task.Delay(1000).GetAwaiter().GetResult();
                                }
                            }
                            catch (Exception exc)
                            {
                                telemetryContext.AddError(exc);
                            }
                        });
                    }
                }
                catch
                {
                }
            });
        }

        /// <summary>
        /// Returns true if the executor has registered that a disk fill was completed.
        /// </summary>
        protected Task RegisterDiskFillCompleteAsync(CancellationToken cancellationToken)
        {
            string stateId = $"{this.GetType().Name}.DiskFill";
            WorkloadState state = new WorkloadState
            {
                DiskFillComplete = true
            };

            return this.SystemManagement.StateManager.SaveStateAsync(stateId, JObject.FromObject(state), cancellationToken);
        }

        /// <summary>
        /// Validates the parameters provided to the profile.
        /// </summary>
        protected override void ValidateParameters()
        {
            if (this.ProcessModel != WorkloadProcessModel.SingleProcess && this.ProcessModel != WorkloadProcessModel.SingleProcessPerDisk)
            {
                throw new WorkloadException(
                    $"Invalid process model. The process model defined in the arguments '{this.ProcessModel}' is not a valid. " +
                    $"Valid values include: {WorkloadProcessModel.SingleProcess}, {WorkloadProcessModel.SingleProcessPerDisk}",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        private class WorkloadState
        {
            public bool DiskFillComplete { get; set; }
        }
    }
}
