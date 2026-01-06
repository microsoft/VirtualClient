// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
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
    /// Manages the execution runtime of the FIO workload.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-x64")]
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
        /// The cool down period for Virtual Client Component.
        /// </summary>
        public TimeSpan CoolDownPeriod
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.CoolDownPeriod), TimeSpan.FromSeconds(0));
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
        /// The name of the I/O engine to use (e.g. libaio, posixaio, windowsaio).
        /// </summary>
        public string Engine
        {
            get
            {
                this.Parameters.TryGetValue(nameof(FioExecutor.Engine), out IConvertible engine);
                return engine?.ToString();
            }
        }

        /// <summary>
        /// The name of the test file that should use in workload tests.
        /// </summary>
        public string FileName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.FileName), "fio-test.dat");
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
        /// <br/><br/>
        /// <b>Supported Values:</b><br/>
        /// <list type="bullet">
        /// <item>SingleProcess = 1 process targeting all matching disks using a single job.</item>
        /// <item>SingleProcessAggregated = 1 process targeting all matching disks but running a separate job per disk.</item>
        /// <item>SingleProcessPerDisk = 1 process per disk running a single job per process.</item>
        /// </list>
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
        /// The specific focus of the test if applicable (e.g. DataIntegrity).
        /// </summary>
        public string TestFocus
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.TestFocus), out IConvertible testFocus);
                return testFocus?.ToString();
            }

            set
            {
                this.Parameters[nameof(this.TestFocus)] = value;
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
        /// Creates the file content for a job file that distributes the FIO workload across the disks
        /// provided using 1 job per disk.
        /// </summary>
        /// <param name="platformSpecifics">Provides platform-specific functionality for cross-platform/architecture operation.</param>
        /// <param name="targetDisks">The target disks on which to run the FIO workload (1 job per disk).</param>
        /// <param name="jobNamePrefix">A prefix to use for the name of each job in the job file.</param>
        /// <param name="testFileName">The name of the test file to use to conduct I/O operations on each disk.</param>
        /// <returns>Content that can be written to an FIO job file.</returns>
        protected static string CreateJobFileContent(PlatformSpecifics platformSpecifics, IEnumerable<Disk> targetDisks, string jobNamePrefix, string testFileName)
        {
            StringBuilder jobFileContent = new StringBuilder();
            jobFileContent.AppendLine("# Dynamically created job file.");
            jobFileContent.AppendLine("#");
            jobFileContent.AppendLine("# Description:");
            jobFileContent.AppendLine("# Distributes the command line definition across each of the target disks");
            jobFileContent.AppendLine("# running 1 job per disk. This is intended to produce results that are aggregated");
            jobFileContent.AppendLine("# across all disks.");

            int jobNumber = 0;
            foreach (Disk disk in targetDisks)
            {
                jobNumber++;
                string jobName = $"{jobNamePrefix}_{jobNumber}";
                jobFileContent.AppendLine();
                jobFileContent.AppendLine($"[{jobName}]");

                string fileName = platformSpecifics.Combine(disk.GetPreferredAccessPath(platformSpecifics.Platform), testFileName);
                string sanitizedFileName = platformSpecifics.Platform == PlatformID.Win32NT ? fileName.Replace(":", "\\:") : fileName;
                jobFileContent.AppendLine($"filename={sanitizedFileName}");
            }

            return jobFileContent.ToString();
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

                    telemetryContext.AddContext(nameof(this.DiskFilter), this.DiskFilter);
                    telemetryContext.AddContext("executable", this.ExecutablePath);
                    telemetryContext.AddContext("ioEngine", this.Engine);
                    telemetryContext.AddContext(nameof(disks), disks);
                    telemetryContext.AddContext(nameof(disksToTest), disksToTest);

                    this.WorkloadProcesses.Clear();
                    List<Task> fioProcessTasks = new List<Task>();

                    this.WorkloadProcesses.AddRange(this.CreateWorkloadProcesses(
                        this.ExecutablePath, 
                        this.CommandLine, 
                        disksToTest, 
                        this.ProcessModel, 
                        telemetryContext));

                    using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                    {
                        foreach (DiskWorkloadProcess process in this.WorkloadProcesses)
                        {
                            fioProcessTasks.Add(this.ExecuteWorkloadAsync(process, this.MetricScenario, telemetryContext, cancellationToken));
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await Task.WhenAll(fioProcessTasks);
                        }
                    }
                }
                finally
                {
                    if (this.DiskFill)
                    {
                        await this.RegisterDiskFillCompleteAsync(cancellationToken);
                    }

                    foreach (DiskWorkloadProcess workload in this.WorkloadProcesses)
                    {
                        await this.DeleteTestVerificationFilesAsync(workload.TestFiles);

                        if (this.DeleteTestFilesOnFinish)
                        {
                            await this.DeleteTestFilesAsync(workload.TestFiles);
                        }
                    }

                    // TO DO: Remove once we have Loop Executor.
                    await this.WaitAsync(this.CoolDownPeriod, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Create the FIO workload processes to execute against one or more disks.
        /// </summary>
        /// <param name="executable">The fully qualified path to the disk spd executable.</param>
        /// <param name="commandArguments">A templatized command to give to the disk spd executable.</param>
        /// <param name="disks">The formatted disks on which the FIO workload will be executed.</param>
        /// <param name="processModel">
        /// The process model/strategy to use for I/O operations against the disks. Valid values include: SingleProcess, SingleProcessPerDisk, MultipleJobsAggregated
        /// </param>
        /// <param name="telemetryContext">Provides context to telemetry events.</param>
        protected virtual IEnumerable<DiskWorkloadProcess> CreateWorkloadProcesses(string executable, string commandArguments, IEnumerable<Disk> disks, string processModel, EventContext telemetryContext)
        {
            executable.ThrowIfNullOrWhiteSpace(nameof(executable));
            commandArguments.ThrowIfNullOrWhiteSpace(nameof(commandArguments));
            processModel.ThrowIfNullOrWhiteSpace(nameof(processModel));
            disks.ThrowIfNullOrEmpty(nameof(disks));

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("executable", executable)
                .AddContext("commandArguments", commandArguments)
                .AddContext("processModel", processModel);

            return this.Logger.LogMessage($"{this.GetType().Name}.CreateProcesses", relatedContext, () =>
            {
                IEnumerable<DiskWorkloadProcess> processes = new List<DiskWorkloadProcess>();

                if (string.Equals(processModel, WorkloadProcessModel.SingleProcess, StringComparison.OrdinalIgnoreCase))
                {
                    processes = this.CreateSingleProcessWorkloadProcesses(executable, commandArguments, disks);
                }
                else if (string.Equals(processModel, WorkloadProcessModel.SingleProcessAggregated, StringComparison.OrdinalIgnoreCase))
                {
                    string jobFilePath = this.GetTempPath($"{this.ExperimentId}.fio".ToLowerInvariant());
                    this.CreateSingleProcessAggregatedJobFile(commandArguments, jobFilePath, disks);

                    // The --name (job name) parameter must be removed or it will override the job specifics
                    // in the job file causing FIO to run the workload different than expected.
                    string effectiveCommandArguments = this.RemoveOption(commandArguments, "--name");
                    processes = this.CreateSingleProcessAggregatedWorkloadProcesses(executable, effectiveCommandArguments, jobFilePath, disks);
                }
                else if (string.Equals(processModel, WorkloadProcessModel.SingleProcessPerDisk, StringComparison.OrdinalIgnoreCase))
                {
                    processes = this.CreateSingleProcessPerDiskWorkloadProcesses(executable, commandArguments, disks);
                }

                relatedContext.AddContext("commands", processes.Select(proc => proc.Process.FullCommand()));

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
            await base.CleanupAsync(telemetryContext, cancellationToken);

            if (this.WorkloadProcesses?.Any() == true)
            {
                foreach (var process in this.WorkloadProcesses)
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
                    if (string.IsNullOrEmpty(fileDirectory))
                    {
                        continue;
                    }

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
            DependencyPath workloadPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken, throwIfNotfound: false);

            if (workloadPackage == null)
            {
                // This is to allow user to use custom installed FIO
                this.ExecutablePath = this.Platform == PlatformID.Win32NT ? "fio.exe" : "fio";
            }
            else
            {
                this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, this.Platform == PlatformID.Win32NT ? "fio.exe" : "fio");
                this.SystemManagement.FileSystem.File.ThrowIfFileDoesNotExist(this.ExecutablePath);

                // Ensure the binary can execute (e.g. chmod +x)
                await this.SystemManagement.MakeFileExecutableAsync(this.ExecutablePath, this.Platform, cancellationToken);
            }
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
                return this.Combine(mountPoint, this.FileName);
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
        /// Returns the target device/file test path. Note that this may be either a file
        /// or a direct path to the physical device (e.g. /dev/sda, /mnt_dev_sda1/fio-test.dat).
        /// </summary>
        protected virtual string GetTestDevicePath(Disk disk)
        {
            return this.Combine(disk.GetPreferredAccessPath(this.Platform), this.FileName);
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
                parser = new FioMetricsParser(modifiedOutput, parseReadMetrics, parseWriteMetrics);
            }

            IList<Metric> metrics = parser.Parse();
            string fioVersion = null;

            if (this.TestFocus != FioExecutor.TestFocusDataIntegrity)
            {
                var fioVersionMetric = metrics.FirstOrDefault(m => m.Name != "data_integrity_errors");
                if (fioVersionMetric != null && fioVersionMetric.Metadata.TryGetValue("fio_version", out var versionValue))
                {
                    fioVersion = versionValue?.ToString();
                }

                if (!string.IsNullOrEmpty(fioVersion))
                {
                    this.MetadataContract.Add("fio_version", fioVersion, MetadataContract.DependenciesCategory);
                }
            }
            
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
               telemetryContext,
               toolVersion: fioVersion);
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
            if (string.IsNullOrWhiteSpace(this.CommandLine))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. One or more of the actions in the profile does not contain the " +
                    $"required '{nameof(FioExecutor.CommandLine)}' argument defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (string.IsNullOrWhiteSpace(this.MetricScenario))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. One or more of the actions in the profile does not contain the " +
                    $"required '{nameof(FioExecutor.MetricScenario)}' arguments defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (this.DiskFill && string.IsNullOrWhiteSpace(this.DiskFillSize))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. One or more of the actions in the profile does not contain the " +
                    $"required '{nameof(FioExecutor.DiskFillSize)}' arguments defined. Disk fill actions require the disk fill size " +
                    $"to be defined (e.g. 496GB).",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1115:Parameter should follow comma", Justification = "Not applicable for this case.")]
        private IEnumerable<DiskWorkloadProcess> CreateSingleProcessWorkloadProcesses(string executable, string commandArguments, IEnumerable<Disk> disks)
        {
            // Process Model: SingleProcess
            // The default scenario is to run 1 FIO process with 1 job targeting each of the disks
            // that match the 'DiskFilter'.

            List<DiskWorkloadProcess> processes = new List<DiskWorkloadProcess>();

            string[] testFiles = disks.Select(disk => this.GetTestDevicePath(disk)).ToArray();
            string fioArguments = $"{commandArguments} {string.Join(" ", testFiles.Select(file => $"--filename={this.SanitizeFilePath(file)}"))}".Trim();

            IProcessProxy fioProcess = this.SystemManagement.ProcessManager.CreateElevatedProcess(this.Platform, executable, fioArguments);

            processes.Add(new DiskWorkloadProcess(
                fioProcess,

                // e.g.
                // SingleProcess,BiggestSize,16
                $"{WorkloadProcessModel.SingleProcess},{this.DiskFilter},{disks.Count()}",
                testFiles));

            return processes;
        }

        private void CreateSingleProcessAggregatedJobFile(string commandArguments, string jobFilePath, IEnumerable<Disk> disks)
        {
            /* Example of Job File:
                # Dynamically created job file.
                #
                # Description:
                # Distributes the command line definition across each of the target disks
                # running 1 job per disk. This is intended to produce results that are aggregated
                # across all disks.

                [fio_randwrite_496GB_4k_d32_th16_1]
                filename=/home/user/mnt_dev_sdc1/fio-test.dat

                [fio_randwrite_496GB_4k_d32_th16_2]
                filename=/home/user/mnt_dev_sdd1/fio-test.dat

                [fio_randwrite_496GB_4k_d32_th16_3]
                filename=/home/user/mnt_dev_sde1/fio-test.dat
             */

            string jobNamePrefix = "job";
            Match jobNameMatch = Regex.Match(commandArguments, @"--name=([\x21-\x7E]+)\s*", RegexOptions.IgnoreCase);
            if (jobNameMatch.Success)
            {
                jobNamePrefix = jobNameMatch.Groups[1].Value.Trim();
            }

            string jobFileContent = FioExecutor.CreateJobFileContent(this.PlatformSpecifics, disks, jobNamePrefix, this.FileName);
            string tempDirectory = this.PlatformSpecifics.GetTempPath();

            if (!this.FileSystem.Directory.Exists(tempDirectory))
            {
                this.FileSystem.Directory.CreateDirectory(tempDirectory);
            }

            RetryPolicies.FileOperations.ExecuteAsync(async () =>
            {
                await this.FileSystem.File.WriteAllTextAsync(jobFilePath, jobFileContent, CancellationToken.None);
            }).GetAwaiter().GetResult();
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1115:Parameter should follow comma", Justification = "Not applicable for this case.")]
        private IEnumerable<DiskWorkloadProcess> CreateSingleProcessAggregatedWorkloadProcesses(string executable, string commandArguments, string jobFilePath, IEnumerable<Disk> disks)
        {
            // Process Model: SingleProcessAggregated
            // The default scenario is to run 1 FIO process with a separate job targeting each of the disks
            // that match the 'DiskFilter'. We use a dynamically generated job file to accomplish this.
            // This causes the results to be aggregated as a sum of all disk I/O performance metrics across the
            // disks (vs. separate metrics for each disk).

            List<DiskWorkloadProcess> processes = new List<DiskWorkloadProcess>();

            string[] testFiles = disks.Select(disk => this.GetTestDevicePath(disk)).ToArray();

            // e.g.
            // The command line defines the specifics of each indvidual job. The job file defines each job-per-disk.
            //
            // --size={FileSize} --numjobs=16 --rw=randwrite --bs=4k --iodepth=32 --ioengine=libaio --direct=1 --ramp_time=30 --runtime=600 --time_based 
            // --overwrite=1 --thread --group_reporting --output-format=json /home/user/virtualclient.1.0.0/content/linux-arm64/temp/2d2218cd-4862-458e-bc91-c332a6d6aae9.fio
            string fioArguments = $"{commandArguments} {jobFilePath}";

            IProcessProxy fioProcess = this.SystemManagement.ProcessManager.CreateElevatedProcess(this.Platform, executable, fioArguments);

            processes.Add(new DiskWorkloadProcess(
                fioProcess,

                // e.g.
                // SingleProcessAggregated,BiggestSize,16
                $"{WorkloadProcessModel.SingleProcessAggregated},{this.DiskFilter},{disks.Count()}",
                testFiles));

            return processes;
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1115:Parameter should follow comma", Justification = "Not applicable for this case.")]
        private IEnumerable<DiskWorkloadProcess> CreateSingleProcessPerDiskWorkloadProcesses(string executable, string commandArguments, IEnumerable<Disk> disks)
        {
            // Process Model: SingleProcessPerDisk
            // The default scenario is to run 1 FIO process with a separate job targeting each of the disks
            // that match the 'DiskFilter'. This causes the results to be aggregated as a sum of all disk I/O
            // performance metrics across the disks (vs. separate metrics for each disk).

            List<DiskWorkloadProcess> processes = new List<DiskWorkloadProcess>(disks.Select(disk =>
            {
                string testFile = this.GetTestDevicePath(disk);
                string fioArguments = $"{commandArguments} --filename={this.SanitizeFilePath(testFile)}".Trim();

                IProcessProxy fioProcess = this.SystemManagement.ProcessManager.CreateElevatedProcess(this.Platform, executable, fioArguments);

                return new DiskWorkloadProcess(
                    fioProcess,

                    // e.g.
                    // SingleProcessPerDisk,BiggestSize,16
                    $"{WorkloadProcessModel.SingleProcessAggregated},{this.DiskFilter},{disks.Count()}",
                    testFile);
            }));

            return processes;
        }

        private string FilterWarnings(string fioOutput)
        {
            string modifiedOutput = Regex.Replace(fioOutput, @"^fio:.*$", string.Empty, RegexOptions.Multiline).Trim();

            return modifiedOutput;
        }

        private string RemoveOption(string commandLine, string option)
        {
            return Regex.Replace(commandLine, $@"{option}=[\x21-\x7E]+\s", string.Empty, RegexOptions.IgnoreCase);
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

        private class WorkloadState
        {
            public bool DiskFillComplete { get; set; }
        }
    }
}
