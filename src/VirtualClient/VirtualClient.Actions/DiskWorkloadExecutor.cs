// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
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
    public abstract class DiskWorkloadExecutor : VirtualClientComponent
    {
        /// <summary>
        /// TestFocus -> DataIntegrity
        /// </summary>
        public const string TestFocusDataIntegrity = "DataIntegrity";

        private const string FileNameParameterDelimiter = ",";

        /// <summary>
        /// Initializes a new instance of the <see cref="DiskWorkloadExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the workload.</param>
        /// <param name="parameters">The set of parameters defined for the action in the profile definition.</param>
        protected DiskWorkloadExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
                this.Parameters.TryGetValue(nameof(DiskWorkloadExecutor.Configuration), out IConvertible configuration);
                return configuration?.ToString();
            }

            set
            {
                this.Parameters[nameof(DiskWorkloadExecutor.Configuration)] = value;
            }
        }

        /// <summary>
        /// Defines the command line specified in the profile.
        /// </summary>
        public string CommandLine
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DiskWorkloadExecutor.CommandLine), out IConvertible commandLine);
                return commandLine?.ToString();
            }

            set
            {
                this.Parameters[nameof(DiskWorkloadExecutor.CommandLine)] = value;
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
                return this.Parameters.GetValue<bool>(nameof(DiskWorkloadExecutor.DeleteTestFilesOnFinish), true);
            }

            set
            {
                this.Parameters[nameof(DiskWorkloadExecutor.DeleteTestFilesOnFinish)] = value;
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
                return this.Parameters.GetValue<bool>(nameof(DiskWorkloadExecutor.DiskFill), false);
            }

            set
            {
                this.Parameters[nameof(DiskWorkloadExecutor.DiskFill)] = value;
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
                this.Parameters.TryGetValue(nameof(DiskWorkloadExecutor.DiskFillSize), out IConvertible diskFillSize);
                return diskFillSize?.ToString();
            }

            set
            {
                this.Parameters[nameof(DiskWorkloadExecutor.DiskFillSize)] = value;
            }
        }

        /// <summary>
        /// The name of the test file that should use in workload tests.
        /// </summary>
        public string FileName
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DiskWorkloadExecutor.FileName), out IConvertible fileName);
                return fileName?.ToString();
            }

            set
            {
                this.Parameters[nameof(DiskWorkloadExecutor.FileName)] = value;
            }
        }

        /// <summary>
        /// The size of the test file that should use in workload tests (e.g. 496GB).
        /// </summary>
        public string FileSize
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DiskWorkloadExecutor.FileSize), out IConvertible fileSize);
                return fileSize?.ToString();
            }

            set
            {
                this.Parameters[nameof(DiskWorkloadExecutor.FileSize)] = value;
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
                return this.Parameters.GetValue<string>(nameof(DiskWorkloadExecutor.ProcessModel), WorkloadProcessModel.SingleProcess);
            }

            set
            {
                this.Parameters[nameof(DiskWorkloadExecutor.ProcessModel)] = value;
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
                return this.Parameters.GetValue<int>(nameof(DiskWorkloadExecutor.QueueDepth), 16);
            }

            set
            {
                this.Parameters[nameof(DiskWorkloadExecutor.QueueDepth)] = value;
            }
        }

        /// <summary>
        /// The specific focus of the test if applicable (e.g. DataIntegrity).
        /// </summary>
        public string TestFocus
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DiskWorkloadExecutor.TestFocus), out IConvertible testFocus);
                return testFocus?.ToString();
            }

            set
            {
                this.Parameters[nameof(DiskWorkloadExecutor.TestFocus)] = value;
            }
        }

        /// <summary>
        /// Name of the test defined in profile.
        /// </summary>
        public string TestName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DiskWorkloadExecutor.TestName));
            }

            set
            {
                this.Parameters[nameof(DiskWorkloadExecutor.TestName)] = value;
            }
        }

        /// <summary>
        /// Disk filter string to filter disks to test.
        /// </summary>
        public string DiskFilter
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(DiskWorkloadExecutor.DiskFilter), "BiggestSize");
            }

            set
            {
                this.Parameters[nameof(DiskWorkloadExecutor.DiskFilter)] = value;
            }
        }

        /// <summary>
        /// The number of threads to use for running disk I/O operations. 
        /// Default = system logical processor count.
        /// </summary>
        public int ThreadCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(DiskWorkloadExecutor.ThreadCount), Environment.ProcessorCount);
            }

            set
            {
                this.Parameters[nameof(DiskWorkloadExecutor.ThreadCount)] = value;
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
                updatedText = ProfilePlaceholders.Replace(nameof(this.ThreadCount), threads, updatedText);
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
        protected abstract Task<bool> CreateMountPointsAsync(IEnumerable<Disk> disks, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a process to run an I/O workload targeting the test files specified.
        /// </summary>
        /// <param name="executable">The full path to the I/O workload executable.</param>
        /// <param name="commandArguments">
        /// The command line arguments to supply to the I/O workload executable (e.g. --name=fio_randread_4GB_4k_d1_th1_direct --ioengine=libaio).
        /// </param>
        /// <param name="testedInstance">A name for the disks under test (e.g. remote_disk, remote_disk_premium_lrs).</param>
        /// <param name="disksToTest">The disks under test.</param>
        protected abstract DiskWorkloadProcess CreateWorkloadProcess(string executable, string commandArguments, string testedInstance, params Disk[] disksToTest);

        /// <summary>
        /// Create a set of <see cref="DiskWorkloadProcess"/>.
        /// </summary>
        /// <param name="executable">The fully qualified path to the disk spd executable.</param>
        /// <param name="commandArguments">A templatized command to give to the disk spd executable.</param>
        /// <param name="disks">The formatted disks.</param>
        /// <param name="processModel">
        /// The process model/strategy to use for I/O operations against the disks. Valid values include: SingleProcess, SingleProcessPerDisk.
        /// </param>
        protected virtual IEnumerable<DiskWorkloadProcess> CreateWorkloadProcesses(string executable, string commandArguments, IEnumerable<Disk> disks, string processModel)
        {
            executable.ThrowIfNullOrWhiteSpace(nameof(executable));
            commandArguments.ThrowIfNullOrWhiteSpace(nameof(commandArguments));
            processModel.ThrowIfNullOrWhiteSpace(nameof(processModel));
            disks.ThrowIfNullOrEmpty(nameof(disks));

            EventContext telemetryContext = EventContext.Persisted();
            return this.Logger.LogMessage($"{this.GetType().Name}.CreateProcesses", telemetryContext, () =>
            {
                List<DiskWorkloadProcess> processes = new List<DiskWorkloadProcess>();

                if (string.Equals(processModel, WorkloadProcessModel.SingleProcess, StringComparison.OrdinalIgnoreCase))
                {
                    // Example Metric Categorization
                    // SingleProcess,BiggestSize,16
                    processes.Add(this.CreateWorkloadProcess(executable, commandArguments, $"{WorkloadProcessModel.SingleProcess},{this.DiskFilter},{disks.Count()}", disks.ToArray()));
                }
                else if (string.Equals(processModel, WorkloadProcessModel.SingleProcessPerDisk, StringComparison.OrdinalIgnoreCase))
                {
                    // Example Metric Categorization
                    // SingleProcessPerDisk,BiggestSize,16
                    processes.AddRange(new List<DiskWorkloadProcess>(disks.Select(disk =>
                    {
                        return this.CreateWorkloadProcess(executable, commandArguments, $"{WorkloadProcessModel.SingleProcessPerDisk},{this.DiskFilter},1", disk);
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
        /// Returns the name of the test files seperated by whitespace given a mount point.
        /// </summary>
        /// <param name="mountPoint">A mount point to the disk under test.</param>
        /// <returns>
        /// The full path to the test files seperated by whitespace.
        /// </returns>
        protected virtual string GetTestFiles(string mountPoint)
        {
            mountPoint.ThrowIfNullOrWhiteSpace(nameof(mountPoint));

            List<string> testFileNames = new List<string>();

            if (!string.IsNullOrWhiteSpace(this.FileName))
            {
                testFileNames.AddRange(Regex.Split(this.FileName, $@"\s*{DiskWorkloadExecutor.FileNameParameterDelimiter}\s*").Select(f => f.Trim())
                            .Where(f => !string.IsNullOrWhiteSpace(f)));
            }

            if (testFileNames.Count() <= 1)
            {
                return this.GetTestFile(mountPoint);
            }

            List<string> testFiles = new List<string>();
            foreach (string fileName in testFileNames)
            {
                testFiles.Add(this.PlatformSpecifics.Combine(mountPoint, fileName));
            }

            return string.Join(' ', testFiles);
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
        protected virtual Task KillProcessAsync(DiskWorkloadProcess workload)
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
        protected override void Validate()
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
