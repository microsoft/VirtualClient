// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
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
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Manages the execution runtime of the FIO workload.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class FioExecutor : VirtualClientComponent
    {
        /// <summary>
        /// TestFocus -> DataIntegrity
        /// </summary>
        public const string TestFocusDataIntegrity = "DataIntegrity";

        private const string FileNameParameterDelimiter = ",";

        /// <summary>
        /// Initializes a new instance of the <see cref="FioExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the workload.</param>
        /// <param name="parameters">The set of parameters defined for the action in the profile definition.</param>
        public FioExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
        /// Defines the job files specified in the profile.
        /// </summary>
        public string JobFiles
        {
            get
            {
                return this.Parameters.ContainsKey(nameof(this.JobFiles)) ? 
                    this.Parameters.GetValue<string>(nameof(this.JobFiles)) : null;
            }
        }

        /// <summary>
        /// Template for Job file.
        /// </summary>
        public string TemplateJobFile
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.TemplateJobFile));
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
        /// The path to the FIO executable in the packages directory.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// Workload Processes.
        /// </summary>
        protected List<DiskWorkloadProcess> WorkloadProcesses { get; } = new List<DiskWorkloadProcess>();

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
        /// Returns the IO engine to use with FIO on the platform specified (e.g. windowsaio, libaio).
        /// </summary>
        /// <param name="platform">The OS/system platform.</param>
        public static string GetIOEngine(PlatformID platform)
        {
            string ioEngine = null;
            switch (platform)
            {
                case PlatformID.Unix:
                    ioEngine = "libaio";
                    break;

                case PlatformID.Win32NT:
                    ioEngine = "windowsaio";
                    break;

                default:
                    throw new WorkloadException(
                        $"The platform '{platform.ToString()}' is not supported.",
                        ErrorReason.PlatformNotSupported);
            }

            return ioEngine;
        }

        /// <summary>
        /// Executes the FIO workload, captures performance results and logs them to telemetry.
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

                    // Apply parameters to the FIO command line options.
                    await this.EvaluateParametersAsync(telemetryContext);

                    string ioEngine = FioExecutor.GetIOEngine(Environment.OSVersion.Platform);

                    IEnumerable<Disk> disks = await this.SystemManagement.DiskManager.GetDisksAsync(cancellationToken)
                        .ConfigureAwait(false);

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

                    if (await this.CreateMountPointsAsync(disksToTest, cancellationToken).ConfigureAwait(false))
                    {
                        // Refresh the disks to pickup the mount point changes.
                        await Task.Delay(1000).ConfigureAwait(false);
                        IEnumerable<Disk> updatedDisks = await this.SystemManagement.DiskManager.GetDisksAsync(cancellationToken)
                            .ConfigureAwait(false);

                        disksToTest = this.GetDisksToTest(updatedDisks);
                    }

                    telemetryContext.AddContext(nameof(this.DiskFilter), this.DiskFilter);
                    telemetryContext.AddContext("executable", this.ExecutablePath);
                    telemetryContext.AddContext(nameof(ioEngine), ioEngine);
                    telemetryContext.AddContext(nameof(disks), disks);
                    telemetryContext.AddContext(nameof(disksToTest), disksToTest);

                    this.WorkloadProcesses.Clear();
                    List<Task> fioProcessTasks = new List<Task>();

                    if (this.JobFiles != null)
                    {
                        await this.SetCommandLineForJobFilesAsync(cancellationToken);
                    }

                    this.WorkloadProcesses.AddRange(this.CreateWorkloadProcesses(this.ExecutablePath, this.CommandLine, disksToTest, this.ProcessModel));

                    using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                    {
                        foreach (DiskWorkloadProcess process in this.WorkloadProcesses)
                        {
                            fioProcessTasks.Add(this.ExecuteWorkloadAsync(process, this.MetricScenario, telemetryContext, cancellationToken));
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await Task.WhenAll(fioProcessTasks).ConfigureAwait(false);
                        }
                    }
                }
                finally
                {
                    if (this.DiskFill)
                    {
                        await this.RegisterDiskFillCompleteAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }

                    foreach (DiskWorkloadProcess workload in this.WorkloadProcesses)
                    {
                        await this.DeleteTestVerificationFilesAsync(workload.TestFiles)
                            .ConfigureAwait(false);

                        if (this.DeleteTestFilesOnFinish)
                        {
                            await this.DeleteTestFilesAsync(workload.TestFiles)
                                .ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates mount points for any disks that do not have them already.
        /// </summary>
        /// <param name="disks">This disks on which to create the mount points.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected async Task<bool> CreateMountPointsAsync(IEnumerable<Disk> disks, CancellationToken cancellationToken)
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

                    await this.Logger.LogMessageAsync($"{this.TypeName}.CreateMountPoint", relatedContext, async () =>
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
        /// Creates a process to run FIO targeting the disks specified.
        /// </summary>
        /// <param name="executable">The full path to the FIO executable.</param>
        /// <param name="commandArguments">
        /// The command line arguments to supply to the FIO executable (e.g. --name=fio_randread_4GB_4k_d1_th1_direct --ioengine=libaio).
        /// </param>
         /// <param name="testedInstance">The disk instance under test (e.g. remote_disk, remote_disk_premium_lrs).</param>
        /// <param name="disksToTest">The disks under test.</param>
        protected virtual DiskWorkloadProcess CreateWorkloadProcess(string executable, string commandArguments, string testedInstance, params Disk[] disksToTest)
        {
            string ioEngine = FioExecutor.GetIOEngine(this.Platform);
            string[] testFiles = disksToTest.Select(disk => this.GetTestFile(disk.GetPreferredAccessPath(this.Platform))).ToArray();
            string fioArguments = $"{commandArguments} --ioengine={ioEngine} {string.Join(" ", testFiles.Select(file => $"--filename={this.SanitizeFilePath(file)}"))}".Trim();

            IProcessProxy process = this.SystemManagement.ProcessManager.CreateElevatedProcess(this.Platform, executable, fioArguments);

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
        /// Applies the configuration specificed to the parameters of the profile
        /// workload action.
        /// </summary>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        protected Task EvaluateParametersAsync(EventContext telemetryContext)
        {
            EventContext relatedContext = telemetryContext.Clone();

            return this.Logger.LogMessageAsync($"{this.TypeName}.EvaluateParameters", relatedContext, async () =>
            {
                await this.EvaluateParametersAsync(CancellationToken.None, true);

                relatedContext.AddContext("commandLine", this.CommandLine);
                relatedContext.AddContext("testScenario", this.MetricScenario);
            });
        }

        /// <summary>
        /// Kills the FIO process
        /// </summary>
        protected override async Task CleanupAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.CleanupAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            if (this.WorkloadProcesses?.Any() == true)
            {
                foreach (var process in this.WorkloadProcesses)
                {
                    try
                    {
                        await this.KillProcessAsync(process).ConfigureAwait(false);

                        if (this.DeleteTestFilesOnFinish)
                        {
                            await this.DeleteTestFilesAsync(process.TestFiles).ConfigureAwait(false);
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
        /// Override allows FIO to handle the delete of additional files used in data integrity verification
        /// tests (e.g. *-verify.state files).
        /// </summary>
        /// <param name="testFiles">The test files to delete.</param>
        /// <param name="retryPolicy">A retry policy to apply to file deletions to handle transient issues.</param>
        protected Task DeleteTestVerificationFilesAsync(IEnumerable<string> testFiles, IAsyncPolicy retryPolicy = null)
        {
            List<string> filesToDelete = new List<string>();
            if (testFiles?.Any() == true)
            {
                foreach (string file in testFiles)
                {
                    string fileDirectory = Path.GetDirectoryName(file);
                    string[] verificationStateFiles = this.FileSystem.Directory.GetFiles(fileDirectory, "*verify.state");
                    if (verificationStateFiles?.Any() == true)
                    {
                        filesToDelete.AddRange(verificationStateFiles);
                    }
                }
            }

            return this.DeleteTestFilesAsync(filesToDelete, retryPolicy);
        }

        /// <summary>
        /// Initializes the executor dependencies, package locations, etc...
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath workloadPackage = await packageManager.GetPackageAsync(this.PackageName, cancellationToken)
                .ConfigureAwait(false);

            if (workloadPackage == null)
            {
                // This is to allow user to use custom installed FIO
                this.ExecutablePath = this.Platform == PlatformID.Win32NT ? "fio.exe" : "fio";
            }
            else
            {
                workloadPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);

                this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, this.Platform == PlatformID.Win32NT ? "fio.exe" : "fio");

                this.SystemManagement.FileSystem.File.ThrowIfFileDoesNotExist(this.ExecutablePath);

                // Ensure the binary can execute (e.g. chmod +x)
                await this.SystemManagement.MakeFileExecutableAsync(this.ExecutablePath, this.Platform, cancellationToken)
                    .ConfigureAwait(false);
            }
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
                testFileNames.AddRange(Regex.Split(this.FileName, $@"\s*{FioExecutor.FileNameParameterDelimiter}\s*").Select(f => f.Trim())
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
        /// Gets the logging setting. Checks workload profile, then command line arguments, then defaults to READ
        /// </summary>
        /// <returns>Logging setting that controls which results are reported</returns>
        protected virtual void GetMetricsParsingDirectives(out bool parseReadMetrics, out bool parseWriteMetrics, string commandLine)
        {
            parseReadMetrics = false;
            parseWriteMetrics = false;

            string rwMode = commandLine.Split()
                .Where(param => param.Contains("--rw", StringComparison.OrdinalIgnoreCase))
                .Select(param => param.Replace("--rw=", string.Empty, StringComparison.OrdinalIgnoreCase))
                .DefaultIfEmpty(string.Empty)
                .FirstOrDefault();

            // If the --rw isn't provided FIO defaults to 'read' operations.
            switch (rwMode.ToLower())
            {
                case "read":
                case "randread":
                    parseReadMetrics = true;
                    break;

                case "write":
                case "randwrite":
                    parseWriteMetrics = true;
                    break;

                case "rw":
                case "readwrite":
                case "randrw":
                    parseReadMetrics = true;
                    parseWriteMetrics = true;
                    break;

                default:
                    parseReadMetrics = true;
                    break;
            }
        }

        /// <summary>
        /// Executes the provided workload.
        /// </summary>
        protected async Task ExecuteWorkloadAsync(DiskWorkloadProcess workload, string testScenario, EventContext telemetryContext, CancellationToken cancellationToken, Dictionary<string, IConvertible> metricMetadata = null)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Execute: {workload.Command} {workload.CommandArguments}");

                EventContext relatedContext = telemetryContext.Clone()
                    .AddContext("command", workload.Command)
                    .AddContext("commandArguments", workload.CommandArguments);

                await this.Logger.LogMessageAsync($"{nameof(FioExecutor)}.ExecuteProcess", relatedContext, async () =>
                {
                    await workload.Process.StartAndWaitAsync(cancellationToken).ConfigureAwait();

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(workload.Process, telemetryContext, "FIO", logToFile: true);

                        if (this.DiskFill)
                        {
                            workload.Process.ThrowIfWorkloadFailed(errorReason: ErrorReason.WorkloadUnexpectedAnomaly);
                        }
                        else if (!cancellationToken.IsCancellationRequested)
                        {
                            if (this.TestFocus != FioExecutor.TestFocusDataIntegrity)
                            {
                                // The FIO command will return a non-success exit code if there are
                                // data integrity/file verification errors. These are expected errors for tests
                                // that are running verifications. We only want to throw if there are not any verification
                                // errors and the exit code indicates error.
                                workload.Process.ThrowIfWorkloadFailed();
                            }

                            this.CaptureMetrics(workload.Process, testScenario, workload.Categorization, workload.CommandArguments, telemetryContext, metricMetadata);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Log Metrics to Kusto Cluster.
        /// </summary>
        protected virtual void CaptureMetrics(
            IProcessProxy workloadProcess, string testScenario, string metricCategorization, string commandArguments, EventContext telemetryContext, Dictionary<string, IConvertible> metricMetadata = null)
        {
            FioMetricsParser parser = null;
            if (this.TestFocus == FioExecutor.TestFocusDataIntegrity)
            {
                parser = new FioMetricsParser(workloadProcess.StandardError.ToString(), parseDataIntegrityErrors: true);
            }
            else
            {
                this.GetMetricsParsingDirectives(out bool parseReadMetrics, out bool parseWriteMetrics, commandArguments);

                string modifiedOutput = this.FilterWarnings(workloadProcess.StandardOutput.ToString());

                Console.WriteLine("Modified output:\n" + modifiedOutput);
                parser = new FioMetricsParser(modifiedOutput, parseReadMetrics, parseWriteMetrics);
            }

            IList<Metric> metrics = parser.Parse();
            if (this.MetricFilters?.Any() == true)
            {
                metrics = metrics.FilterBy(this.MetricFilters).ToList();
            }

            if (metricMetadata != null)
            {
                foreach (var metric in metrics)
                {
                    foreach (var metricMetadataValue in metricMetadata)
                    {
                        metric.Metadata[metricMetadataValue.Key] = metricMetadataValue.Value;
                    }
                }
            }

            this.MetadataContract.AddForScenario(
                "FIO",
                commandArguments,
                toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            this.Logger.LogMetrics(
               "FIO",
               testScenario,
               workloadProcess.StartTime,
               workloadProcess.ExitTime,
               metrics,
               metricCategorization,
               commandArguments,
               this.Tags,
               telemetryContext);
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
            if (string.IsNullOrWhiteSpace(this.CommandLine) && string.IsNullOrWhiteSpace(this.JobFiles))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. One or more of the actions in the profile does not contain the " +
                    $"required '{nameof(FioExecutor.CommandLine)}' or '{nameof(FioExecutor.JobFiles)}' arguments defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (!string.IsNullOrWhiteSpace(this.CommandLine) && !string.IsNullOrWhiteSpace(this.JobFiles))
            {
                throw new WorkloadException(
                    "Unexpected profile definition. Only one of JobFiles or CommandLine can be defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (string.IsNullOrWhiteSpace(this.MetricScenario))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. One or more of the actions in the profile does not contain the " +
                    $"required '{nameof(FioExecutor.MetricScenario)}' arguments defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (this.DiskFill && string.IsNullOrWhiteSpace(this.JobFiles) && string.IsNullOrWhiteSpace(this.DiskFillSize))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. One or more of the actions in the profile does not contain the " +
                    $"required '{nameof(FioExecutor.DiskFillSize)}' arguments defined. Disk fill actions require the disk fill size " +
                    $"to be defined (e.g. 496GB).",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        private string SanitizeFilePath(string filePath)
        {
            string sanitizedFilePath = filePath;
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // Note:
                // FIO expects file paths to be in a very specific format. On Linux there is no issue because paths do not have
                // a drive root or colon in them. For Windows, we have to sanitize them a bit.
                //
                // Examples:
                // C:\anyfiotest.dat -> C\:\anyfiotest.dat
                sanitizedFilePath = sanitizedFilePath.Replace(":", "\\:");
            }

            return sanitizedFilePath;
        }

        private async Task SetCommandLineForJobFilesAsync(CancellationToken cancellationToken)
        {
            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath workloadPackage = await packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

            this.CommandLine = string.Empty;

            string[] templateJobFilePaths = this.JobFiles.Split(new char[] { ';', ',' });
            foreach (string templateJobFilePath in templateJobFilePaths)
            {
                // Create/update new job file at runtime.
                string templateJobFileName = Path.GetFileName(templateJobFilePath);
                string updatedJobFilePath = this.PlatformSpecifics.Combine(workloadPackage.Path, templateJobFileName);
                this.CreateOrUpdateJobFile(templateJobFilePath, updatedJobFilePath);

                // Update command line to include the new job file.
                this.CommandLine += $"{updatedJobFilePath} ";
            }

            this.CommandLine = $"{this.CommandLine.Trim()} --output-format=json";
        }

        private void CreateOrUpdateJobFile(string sourcePath, string destinationPath)
        {
            string text = this.SystemManagement.FileSystem.File.ReadAllText(sourcePath);

            foreach (string key in this.Parameters.Keys)
            {
                // text = text.Replace($"${{{key.ToLower()}}}", this.Parameters.GetValue<string>(key));
                text = Regex.Replace(text, @$"\${{{key.ToLower()}}}", this.Parameters.GetValue<string>(key));
            }

            this.SystemManagement.FileSystem.File.WriteAllText(@destinationPath, text);
        }

        private string FilterWarnings(string fioOutput)
        {
            string modifiedOutput = Regex.Replace(fioOutput, @"^fio:.*$", string.Empty, RegexOptions.Multiline).Trim();

            return modifiedOutput;
        }

        private class WorkloadState
        {
            public bool DiskFillComplete { get; set; }
        }
    }
}
