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
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Executes a DiskSpd workload profile.
    /// </summary>
    /// <remarks>
    /// DiskSpd Overview
    /// https://docs.microsoft.com/en-us/azure-stack/hci/manage/diskspd-overview
    /// 
    /// DiskSpd Command Line Parameters
    /// https://github.com/Microsoft/diskspd/wiki/Command-line-and-parameters
    /// </remarks>
    [SupportedPlatforms("win-arm64,win-x64")]
    public class DiskSpdExecutor : VirtualClientComponent
    {
        private const string FileNameParameterDelimiter = ",";
        private readonly List<DiskWorkloadProcess> workloadProcesses = new List<DiskWorkloadProcess>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DiskSpdExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the workload.</param>
        /// <param name="parameters">The set of parameters defined for the action in the profile definition.</param>
        public DiskSpdExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Defines the command line specified in the profile.
        /// </summary>
        public string CommandLine
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.CommandLine), out IConvertible commandLine);
                return commandLine?.ToString();
            }

            set
            {
                this.Parameters[nameof(this.CommandLine)] = value;
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
                return this.Parameters.GetValue<bool>(nameof(this.DeleteTestFilesOnFinish), true);
            }

            set
            {
                this.Parameters[nameof(this.DeleteTestFilesOnFinish)] = value;
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
                return this.Parameters.GetValue<bool>(nameof(this.DiskFill), false);
            }

            set
            {
                this.Parameters[nameof(this.DiskFill)] = value;
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
                this.Parameters.TryGetValue(nameof(this.DiskFillSize), out IConvertible diskFillSize);
                return diskFillSize?.ToString();
            }

            set
            {
                this.Parameters[nameof(this.DiskFillSize)] = value;
            }
        }

        /// <summary>
        /// Disk filter string to filter disks to test.
        /// </summary>
        public string DiskFilter
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DiskFilter), "BiggestSize");
            }

            set
            {
                this.Parameters[nameof(this.DiskFilter)] = value;
            }
        }

        /// <summary>
        /// The name of the test file that should use in workload tests.
        /// </summary>
        public string FileName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.FileName), "diskspd-test.dat");
            }

            set
            {
                this.Parameters[nameof(this.FileName)] = value;
            }
        }

        /// <summary>
        /// The size of the test file that should use in workload tests (e.g. 496GB).
        /// </summary>
        public string FileSize
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.FileSize), out IConvertible fileSize);
                return fileSize?.ToString();
            }

            set
            {
                this.Parameters[nameof(this.FileSize)] = value;
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
                return this.Parameters.GetValue<string>(nameof(this.ProcessModel), WorkloadProcessModel.SingleProcess);
            }

            set
            {
                this.Parameters[nameof(this.ProcessModel)] = value;
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
                return this.Parameters.GetValue<int>(nameof(this.QueueDepth), 16);
            }

            set
            {
                this.Parameters[nameof(this.QueueDepth)] = value;
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
                return this.Parameters.GetValue<int>(nameof(this.ThreadCount), Environment.ProcessorCount);
            }

            set
            {
                this.Parameters[nameof(this.ThreadCount)] = value;
            }
        }

        /// <summary>
        /// The path to the DiskSpd.exe executable in the packages directory.
        /// </summary>
        protected string ExecutablePath { get; set; }

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
        /// Applies the configuration specified to the parameters of the profile
        /// workload action.
        /// </summary>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        protected Task EvaluateParametersAsync(EventContext telemetryContext)
        {
            EventContext relatedContext = telemetryContext.Clone();

            return this.Logger.LogMessageAsync($"{this.TypeName}.EvaluateParameters", relatedContext, async () =>
            {
                string fileSize = this.FileSize;
                if (!string.IsNullOrWhiteSpace(fileSize))
                {
                    this.FileSize = this.SanitizeFileSize(fileSize);
                }

                string diskFillSize = this.DiskFillSize;
                if (!string.IsNullOrWhiteSpace(diskFillSize))
                {
                    this.DiskFillSize = this.SanitizeFileSize(diskFillSize);
                }

                await this.EvaluateParametersAsync(CancellationToken.None, true);

                relatedContext.AddContext("commandLine", this.CommandLine);
                relatedContext.AddContext("testScenario", this.MetricScenario);
            });
        }

        /// <summary>
        /// Kills DiskSpd processes and attempts to delete files that were in use.
        /// </summary>
        protected override async Task CleanupAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.CleanupAsync(telemetryContext, cancellationToken);

            if (this.workloadProcesses?.Any() == true)
            {
                foreach (var process in this.workloadProcesses)
                {
                    try
                    {
                        await this.KillProcessAsync(process);

                        if (this.DeleteTestFilesOnFinish)
                        {
                            await this.DeleteTestFilesAsync(process.TestFiles);
                        }
                    }
                    catch
                    {
                        // Terminating the processes is a best effort only.
                    }
                }
            }
        }

        /// <summary>
        /// Creates a process to run DiskSpd targeting the disks specified.
        /// </summary>
        /// <param name="executable">The full path to the DiskSpd executable.</param>
        /// <param name="commandArguments">
        /// The command line arguments to supply to the DiskSpd executable (e.g. -c4G -b4K -r4K -t1 -o1 -w100 -d480 -Suw -W15 -D -L -Rtext).
        /// </param>
        /// <param name="testedInstance">A name for the disks under test (e.g. remote_disk, remote_disk_premium_lrs).</param>
        /// <param name="disksToTest">The disks under test.</param>
        protected DiskWorkloadProcess CreateWorkloadProcess(string executable, string commandArguments, string testedInstance, params Disk[] disksToTest)
        {
            string[] testFiles = disksToTest.Select(disk => this.GetTestFiles(disk.GetPreferredAccessPath(this.Platform))).ToArray();
            string diskSpdArguments = $"{commandArguments} {string.Join(" ", testFiles)}";

            IProcessProxy process = this.SystemManagement.ProcessManager.CreateProcess(executable, diskSpdArguments);

            return new DiskWorkloadProcess(process, testedInstance, testFiles);
        }

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
                                    await this.SystemManagement.FileSystem.File.DeleteAsync(file, retryPolicy);
                                }
                            }
                            catch (Exception exc)
                            {
                                telemetryContext.AddError(exc);
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Executes the DiskSpd profile workload operations.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (this.DiskFill && await this.IsDiskFillCompleteAsync(cancellationToken))
                    {
                        return;
                    }

                    // Apply parameters to the DiskSpd command line options.
                    await this.EvaluateParametersAsync(telemetryContext);

                    IEnumerable<Disk> disks = await this.SystemManagement.DiskManager.GetDisksAsync(cancellationToken);

                    if (disks?.Any() != true)
                    {
                        throw new WorkloadException(
                            "Unexpected scenario. The disks defined for the system could not be properly enumerated.",
                            ErrorReason.WorkloadUnexpectedAnomaly);
                    }

                    IEnumerable<Disk> disksToTest = this.GetDisksToTest(disks);

                    if (disksToTest?.Any() != true)
                    {
                        throw new WorkloadException(
                            "Expected disks to test not found. Given the parameters defined for the profile action/step or those passed " +
                            "in on the command line, the requisite disks do not exist on the system or could not be identified based on the properties " +
                            "of the existing disks.",
                            ErrorReason.DependencyNotFound);
                    }

                    this.Logger.LogMessage($"{nameof(DiskSpdExecutor)}.SelectDisks", telemetryContext.Clone()
                        .AddContext("disks", disksToTest));

                    disksToTest.ToList().ForEach(disk => this.Logger.LogTraceMessage($"Disk Target: '{disk}'"));

                    telemetryContext.AddContext("executable", this.ExecutablePath);
                    telemetryContext.AddContext(nameof(disks), disks);
                    telemetryContext.AddContext(nameof(disksToTest), disksToTest);

                    this.workloadProcesses.AddRange(this.CreateWorkloadProcesses(this.ExecutablePath, this.CommandLine, disksToTest, this.ProcessModel));

                    telemetryContext.AddContext(
                        nameof(this.workloadProcesses),
                        this.workloadProcesses.Select(p => new { id = p.Process.Id, command = p.Command, arguments = p.CommandArguments }));

                    await this.ExecuteWorkloadsAsync(this.workloadProcesses, cancellationToken);
                }
                finally
                {
                    if (this.DiskFill)
                    {
                        await this.RegisterDiskFillCompleteAsync(cancellationToken);
                    }

                    if (this.DeleteTestFilesOnFinish)
                    {
                        foreach (DiskWorkloadProcess workload in this.workloadProcesses)
                        {
                            await this.DeleteTestFilesAsync(workload.TestFiles);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Executes the workload processes.
        /// </summary>
        /// <param name="workloads">The workload processes.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the workload operations.</param>
        protected Task ExecuteWorkloadsAsync(IEnumerable<DiskWorkloadProcess> workloads, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                // Execute processes and cleanup residuals if required.
                List<Task> workloadTasks = new List<Task>();
                foreach (DiskWorkloadProcess workload in workloads)
                {
                    workloadTasks.Add(this.ExecuteWorkloadAsync(workload, cancellationToken));
                }

                return Task.WhenAll(workloadTasks);
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
        /// Returns the name of the test files separated by whitespace given a mount point.
        /// </summary>
        /// <param name="mountPoint">A mount point to the disk under test.</param>
        /// <returns>
        /// The full path to the test files separated by whitespace.
        /// </returns>
        protected virtual string GetTestFiles(string mountPoint)
        {
            mountPoint.ThrowIfNullOrWhiteSpace(nameof(mountPoint));

            List<string> testFileNames = new List<string>();

            if (!string.IsNullOrWhiteSpace(this.FileName))
            {
                testFileNames.AddRange(Regex.Split(this.FileName, $@"\s*{DiskSpdExecutor.FileNameParameterDelimiter}\s*").Select(f => f.Trim())
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
            WorkloadState state = await this.SystemManagement.StateManager.GetStateAsync<WorkloadState>(stateId, cancellationToken);

            return state != null;
        }

        /// <summary>
        /// Initializes the executor dependencies, package locations, etc...
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);

            this.workloadProcesses.Clear();
            this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "diskspd.exe");
            this.SystemManagement.FileSystem.File.ThrowIfFileDoesNotExist(this.ExecutablePath);
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
        /// Removes any extraneous characters from the file size. DiskSpd does not use the
        /// same format for file sizes as other workloads (e.g. 496GB -> 496G).
        /// </summary>
        protected string SanitizeFileSize(string fileSize)
        {
            // Example:
            // 496GB -> 496G
            return fileSize.Replace("B", string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Validates the parameters provided to the profile.
        /// </summary>
        protected override void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.CommandLine))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. One or more of the actions in the profile does not contain the " +
                    $"required '{nameof(DiskSpdExecutor.CommandLine)}' arguments defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (string.IsNullOrWhiteSpace(this.MetricScenario))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. One or more of the actions in the profile does not contain the " +
                    $"required '{nameof(DiskSpdExecutor.MetricScenario)}' arguments defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (this.DiskFill && string.IsNullOrWhiteSpace(this.DiskFillSize))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. One or more of the actions in the profile does not contain the " +
                    $"required '{nameof(DiskSpdExecutor.DiskFillSize)}' arguments defined. Disk fill actions require the disk fill size " +
                    $"to be defined (e.g. 496G).",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        private void CaptureMetrics(DiskWorkloadProcess workload, EventContext telemetryContext)
        {
            this.MetadataContract.AddForScenario(
                "DiskSpd",
                workload.CommandArguments,
                toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            string result = workload.StandardOutput.ToString();
            DiskSpdMetricsParser parser = new DiskSpdMetricsParser(result, this.CommandLine);
            IList<Metric> metrics = parser.Parse();

            if (this.MetricFilters?.Any() == true)
            {
                metrics = metrics.FilterBy(this.MetricFilters).ToList();
            }

            metrics.LogConsole(this.MetricScenario ?? this.Scenario, "DiskSpd");

            this.Logger.LogMetrics(
                "DiskSpd",
                (this.MetricScenario ?? this.Scenario),
                workload.Process.StartTime,
                workload.Process.ExitTime,
                metrics,
                workload.Categorization,
                workload.CommandArguments,
                this.Tags,
                telemetryContext);
        }

        private async Task ExecuteWorkloadAsync(DiskWorkloadProcess workload, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Execute: {workload.Command} {workload.CommandArguments}");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", workload.Command)
                    .AddContext("commandArguments", workload.CommandArguments);

                await this.Logger.LogMessageAsync($"{nameof(DiskSpdExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    await workload.Process.StartAndWaitAsync(cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(workload.Process, telemetryContext, "DiskSpd", logToFile: true);

                        if (this.DiskFill)
                        {
                            workload.Process.ThrowIfWorkloadFailed(errorReason: ErrorReason.WorkloadUnexpectedAnomaly);
                        }
                        else if (!cancellationToken.IsCancellationRequested)
                        {
                            workload.Process.ThrowIfWorkloadFailed();
                            this.CaptureMetrics(workload, telemetryContext);
                        }
                    }
                });
            }
        }

        private class WorkloadState
        {
            public bool DiskFillComplete { get; set; }
        }
    }
}
