// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

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
    [WindowsCompatible]
    public class DiskSpdExecutor : DiskPerformanceWorkloadExecutor
    {
        private readonly List<DiskPerformanceWorkloadProcess> workloadProcesses = new List<DiskPerformanceWorkloadProcess>();

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
        /// The path to the DiskSpd.exe executable in the packages directory.
        /// </summary>
        public string ExecutablePath { get; set; }

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

            this.Logger.LogMessage($"{nameof(DiskSpdExecutor)}.ApplyConfiguration", relatedContext, () =>
            {
                string fileSize = this.FileSize;
                if (!string.IsNullOrWhiteSpace(fileSize))
                {
                    fileSize = this.SanitizeFileSize(fileSize);
                }

                string diskFillSize = this.DiskFillSize;
                if (!string.IsNullOrWhiteSpace(diskFillSize))
                {
                    diskFillSize = this.SanitizeFileSize(diskFillSize);
                }

                switch (configuration)
                {
                    case "Stress":
                        int logicalCores = Environment.ProcessorCount;
                        int threads = logicalCores / 2;
                        int queueDepth = 512 / threads;

                        this.CommandLine = this.ApplyParameters(
                            this.CommandLine,
                            fileSize,
                            diskFillSize,
                            queueDepth,
                            threads);

                        this.TestName = this.ApplyParameters(
                            this.TestName,
                            fileSize,
                            diskFillSize,
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
            this.Logger.LogMessage($"{nameof(DiskSpdExecutor)}.ApplyParameters", relatedContext, () =>
            {
                string fileSize = this.FileSize;
                if (!string.IsNullOrWhiteSpace(fileSize))
                {
                    fileSize = this.SanitizeFileSize(fileSize);
                }

                string diskFillSize = this.DiskFillSize;
                if (!string.IsNullOrWhiteSpace(diskFillSize))
                {
                    diskFillSize = this.SanitizeFileSize(diskFillSize);
                }

                if (!string.IsNullOrWhiteSpace(this.CommandLine))
                {
                    this.CommandLine = this.ApplyParameters(
                        this.CommandLine,
                        fileSize,
                        diskFillSize,
                        this.QueueDepth,
                        this.Threads);

                    relatedContext.AddContext("commandLine", this.CommandLine);
                }

                if (!string.IsNullOrWhiteSpace(this.TestName))
                {
                    this.TestName = this.ApplyParameters(
                        this.TestName,
                        fileSize,
                        diskFillSize,
                        this.QueueDepth,
                        this.Threads);

                    relatedContext.AddContext("testName", this.TestName);
                }
            });
        }

        /// <summary>
        /// Kills DiskSpd processes and attempts to delete files that were in use.
        /// </summary>
        protected override async Task CleanupAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.CleanupAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            if (this.workloadProcesses?.Any() == true)
            {
                foreach (var process in this.workloadProcesses)
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
                    this.ValidateParameters();

                    if (this.DiskFill && await this.IsDiskFillCompleteAsync(cancellationToken))
                    {
                        return;
                    }

                    if (this.Configuration != null)
                    {
                        this.ApplyConfiguration(this.Configuration, telemetryContext);
                    }

                    this.ApplyParameters(telemetryContext);

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

                    this.Logger.LogMessage($"{nameof(DiskSpdExecutor)}.SelectDisks", telemetryContext.Clone()
                        .AddContext("disks", disksToTest));

                    if (await this.CreateMountPointsAsync(disksToTest, cancellationToken).ConfigureAwait(false))
                    {
                        // Refresh the disks to pickup the mount point changes.
                        await Task.Delay(1000).ConfigureAwait(false);
                        IEnumerable<Disk> updatedDisks = await this.SystemManagement.DiskManager.GetDisksAsync(cancellationToken)
                            .ConfigureAwait(false);

                        disksToTest = this.GetDisksToTest(updatedDisks);
                    }

                    disksToTest.ToList().ForEach(disk => this.Logger.LogTraceMessage($"Disk Target: '{disk}'"));

                    telemetryContext.AddContext("executable", this.ExecutablePath);
                    telemetryContext.AddContext(nameof(disks), disks);
                    telemetryContext.AddContext(nameof(disksToTest), disksToTest);

                    this.workloadProcesses.AddRange(this.CreateWorkloadProcesses(this.ExecutablePath, this.CommandLine, disksToTest, this.ProcessModel));

                    telemetryContext.AddContext(
                        nameof(this.workloadProcesses),
                        this.workloadProcesses.Select(p => new { id = p.Process.Id, command = p.Command, arguments = p.CommandArguments }));

                    await this.ExecuteWorkloadsAsync(this.workloadProcesses, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    if (this.DiskFill)
                    {
                        await this.RegisterDiskFillCompleteAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }

                    if (this.DeleteTestFilesOnFinish)
                    {
                        foreach (DiskPerformanceWorkloadProcess workload in this.workloadProcesses)
                        {
                            await this.DeleteTestFilesAsync(workload.TestFiles).ConfigureAwait(false);
                        }
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
        protected override DiskPerformanceWorkloadProcess CreateWorkloadProcess(string executable, string commandArguments, string testedInstance, params Disk[] disksToTest)
        {
            string[] testFiles = disksToTest.Select(disk => this.GetTestFile(disk.GetPreferredAccessPath(this.Platform))).ToArray();
            string diskSpdArguments = $"{commandArguments} {string.Join(" ", testFiles)}";

            IProcessProxy process = this.SystemManagement.ProcessManager.CreateProcess(executable, diskSpdArguments);

            return new DiskPerformanceWorkloadProcess(process, testedInstance, testFiles);
        }

        /// <summary>
        /// Executes the workload processes.
        /// </summary>
        /// <param name="workloads">The workload processes.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the workload operations.</param>
        protected Task ExecuteWorkloadsAsync(IEnumerable<DiskPerformanceWorkloadProcess> workloads, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                // Execute processes and cleanup residuals if required.
                List<Task> workloadTasks = new List<Task>();
                foreach (DiskPerformanceWorkloadProcess workload in workloads)
                {
                    workloadTasks.Add(this.ExecuteWorkloadAsync(workload, cancellationToken));
                }

                return Task.WhenAll(workloadTasks);
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
                    $"The DiskSpd workload package was not found in the packages directory.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            workloadPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);

            this.workloadProcesses.Clear();
            this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "diskspd.exe");
            this.SystemManagement.FileSystem.File.ThrowIfFileDoesNotExist(this.ExecutablePath);
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
        protected override void ValidateParameters()
        {
            if (string.IsNullOrWhiteSpace(this.CommandLine))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. One or more of the actions in the profile does not contain the " +
                    $"required '{nameof(DiskSpdExecutor.CommandLine)}' arguments defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (string.IsNullOrWhiteSpace(this.TestName))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. One or more of the actions in the profile does not contain the " +
                    $"required '{nameof(DiskSpdExecutor.TestName)}' arguments defined.",
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

        private void CaptureMetrics(DiskPerformanceWorkloadProcess workload, EventContext telemetryContext)
        {
            string result = workload.StandardOutput.ToString();
            IList<Metric> metrics = new List<Metric>()
                .AddDiskIOMetrics(result)
                .AddDiskIOPercentileMetrics(result);

            this.Logger.LogMetrics(
                "DiskSpd",
                this.TestName,
                workload.Process.StartTime,
                workload.Process.ExitTime,
                metrics,
                workload.Categorization,
                workload.CommandArguments,
                this.Tags,
                telemetryContext);
        }

        private async Task ExecuteWorkloadAsync(DiskPerformanceWorkloadProcess workload, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Execute: {workload.Command} {workload.CommandArguments}");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", workload.Command)
                    .AddContext("commandArguments", workload.CommandArguments);

                await this.Logger.LogMessageAsync($"{nameof(DiskSpdExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    DateTime start = DateTime.Now;
                    await workload.Process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                    DateTime end = DateTime.Now;

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(workload.Process, telemetryContext, "DiskSpd", logToFile: true)
                            .ConfigureAwait(false);

                        if (this.DiskFill)
                        {
                            workload.Process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadUnexpectedAnomaly);
                        }
                        else if (!cancellationToken.IsCancellationRequested)
                        {
                            workload.Process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                            this.CaptureMetrics(workload, telemetryContext);
                        }
                    }
                }).ConfigureAwait(false);
            }
        }
    }
}
