// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Manages the execution runtime of the FIO workload.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class FioExecutor : DiskWorkloadExecutor
    {
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
        /// The path to the FIO executable in the packages directory.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// Workload Processes.
        /// </summary>
        protected List<DiskWorkloadProcess> WorkloadProcesses { get; } = new List<DiskWorkloadProcess>();

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
        /// Applies the configuration specificed to the parameters of the profile
        /// workload action.
        /// </summary>
        /// <param name="configuration">The name of the configuration (e.g. Stress).</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        protected void ApplyConfiguration(string configuration, EventContext telemetryContext)
        {
            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("configuration", configuration);

            this.Logger.LogMessage($"{nameof(FioExecutor)}.ApplyConfiguration", relatedContext, () =>
            {
                switch (configuration)
                {
                    case "Stress":
                        int logicalCores = Environment.ProcessorCount;
                        int threads = logicalCores / 2;
                        threads = (threads == 0) ? 1 : threads;
                        int queueDepth = 512 / threads;

                        this.CommandLine = this.ApplyParameters(
                            this.CommandLine,
                            this.FileSize,
                            this.DiskFillSize,
                            queueDepth,
                            threads);

                        this.TestName = this.ApplyParameters(
                            this.TestName,
                            this.FileSize,
                            this.DiskFillSize,
                            queueDepth,
                            threads);

                        relatedContext.AddContext("commandLine", this.CommandLine);
                        relatedContext.AddContext("testName", this.TestName);
                        relatedContext.AddContext(nameof(logicalCores), logicalCores);
                        relatedContext.AddContext(nameof(threads), threads);
                        relatedContext.AddContext(nameof(queueDepth), queueDepth);

                        break;

                    default:
                        throw new WorkloadException(
                            $"Invalid configuration. The configuration '{configuration}' defined in the profile arguments is not a supported configuration.",
                            ErrorReason.InvalidProfileDefinition);
                }
            });
        }

        /// <summary>
        /// Applies any placeholder replacements to the parameters of the profile
        /// workload action.
        /// </summary>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        protected void ApplyParameters(EventContext telemetryContext)
        {
            EventContext relatedContext = telemetryContext.Clone();
            this.Logger.LogMessage($"{nameof(FioExecutor)}.ApplyParameters", relatedContext, () =>
            {
                if (!string.IsNullOrWhiteSpace(this.CommandLine))
                {
                    this.CommandLine = this.ApplyParameters(this.CommandLine, this.FileSize, this.DiskFillSize, this.QueueDepth, this.Threads);
                    relatedContext.AddContext("commandLine", this.CommandLine);
                }

                if (!string.IsNullOrWhiteSpace(this.TestName))
                {
                    this.TestName = this.ApplyParameters(this.TestName, this.FileSize, this.DiskFillSize, this.QueueDepth, this.Threads);
                    relatedContext.AddContext("testName", this.TestName);
                }
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

                    if (this.Configuration != null)
                    {
                        this.ApplyConfiguration(this.Configuration, telemetryContext);
                    }

                    this.ApplyParameters(telemetryContext);

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
                    this.WorkloadProcesses.AddRange(this.CreateWorkloadProcesses(this.ExecutablePath, this.CommandLine, disksToTest, this.ProcessModel));

                    using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                    {
                        foreach (DiskWorkloadProcess process in this.WorkloadProcesses)
                        {
                            fioProcessTasks.Add(this.ExecuteWorkloadAsync(process, this.TestName, telemetryContext, cancellationToken));
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
        /// Initializes the executor dependencies, package locations, etc...
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath workloadPackage = await packageManager.GetPackageAsync(this.PackageName, cancellationToken)
                .ConfigureAwait(false);

            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The FIO workload package was not found in the packages directory.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            workloadPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);

            this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, this.Platform == PlatformID.Win32NT ? "fio.exe" : "fio");
            this.SystemManagement.FileSystem.File.ThrowIfFileDoesNotExist(this.ExecutablePath);

            // Ensure the binary can execute (e.g. chmod +x)
            await this.SystemManagement.MakeFileExecutableAsync(this.ExecutablePath, this.Platform, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Creates mount points for any disks that do not have them already.
        /// </summary>
        /// <param name="disks">This disks on which to create the mount points.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task<bool> CreateMountPointsAsync(IEnumerable<Disk> disks, CancellationToken cancellationToken)
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
        protected override DiskWorkloadProcess CreateWorkloadProcess(string executable, string commandArguments, string testedInstance, params Disk[] disksToTest)
        {
            string ioEngine = FioExecutor.GetIOEngine(this.Platform);
            string[] testFiles = disksToTest.Select(disk => this.GetTestFile(disk.GetPreferredAccessPath(this.Platform))).ToArray();
            string fioArguments = $"{commandArguments} --ioengine={ioEngine} {string.Join(" ", testFiles.Select(file => $"--filename={this.SanitizeFilePath(file)}"))}".Trim();

            IProcessProxy process = this.SystemManagement.ProcessManager.CreateElevatedProcess(this.Platform, executable, fioArguments);

            return new DiskWorkloadProcess(process, testedInstance, testFiles);
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
        /// Validates the parameters provided to the profile.
        /// </summary>
        protected override void Validate()
        {
            if (string.IsNullOrWhiteSpace(this.CommandLine))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. One or more of the actions in the profile does not contain the " +
                    $"required '{nameof(FioExecutor.CommandLine)}' arguments defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (string.IsNullOrWhiteSpace(this.TestName))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. One or more of the actions in the profile does not contain the " +
                    $"required '{nameof(FioExecutor.TestName)}' arguments defined.",
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

        /// <summary>
        /// Executes the provided workload.
        /// </summary>
        protected async Task ExecuteWorkloadAsync(DiskWorkloadProcess workload, string testName, EventContext telemetryContext, CancellationToken cancellationToken, Dictionary<string, IConvertible> metricMetadata = null)
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
                        workload.Process.ProcessDetails.ToolSet = "FIO";
                        await this.LogProcessDetailsAsync(workload.Process.ProcessDetails, telemetryContext, logToFile: true);

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

                            this.CaptureMetrics(workload.Process, testName, workload.Categorization, workload.CommandArguments, telemetryContext, metricMetadata);
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Log Metrics to Kusto Cluster.
        /// </summary>
        protected virtual void CaptureMetrics(
            IProcessProxy workloadProcess, string testName, string metricCategorization, string commandArguments, EventContext telemetryContext, Dictionary<string, IConvertible> metricMetadata = null)
        {
            FioMetricsParser parser = null;
            if (this.TestFocus == FioExecutor.TestFocusDataIntegrity)
            {
                parser = new FioMetricsParser(workloadProcess.StandardError.ToString(), parseDataIntegrityErrors: true);
            }
            else
            {
                this.GetMetricsParsingDirectives(out bool parseReadMetrics, out bool parseWriteMetrics, commandArguments);
                parser = new FioMetricsParser(workloadProcess.StandardOutput.ToString(), parseReadMetrics, parseWriteMetrics);
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

            this.Logger.LogMetrics(
               "FIO",
               testName,
               workloadProcess.StartTime,
               workloadProcess.ExitTime,
               metrics,
               metricCategorization,
               commandArguments,
               this.Tags,
               telemetryContext);
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
    }
}