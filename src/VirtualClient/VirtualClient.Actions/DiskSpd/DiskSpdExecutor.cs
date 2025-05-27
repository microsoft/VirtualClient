// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
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
    public class DiskSpdExecutor : DiskWorkloadExecutor
    {
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
        /// The path to the DiskSpd.exe executable in the packages directory.
        /// </summary>
        public string ExecutablePath { get; set; }

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
                    if (this.DiskFill && await this.IsDiskFillCompleteAsync(cancellationToken))
                    {
                        return;
                    }

                    // Apply parameters to the DiskSpd command line options.
                    await this.EvaluateParametersAsync(telemetryContext);

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
                        foreach (DiskWorkloadProcess workload in this.workloadProcesses)
                        {
                            await this.DeleteTestFilesAsync(workload.TestFiles).ConfigureAwait(false);
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
        protected override async Task<bool> CreateMountPointsAsync(IEnumerable<Disk> disks, CancellationToken cancellationToken)
        {
            bool mountPointsCreated = false;

            // Don't mount any partition in OS drive.
            foreach (Disk disk in disks.Where(d => !d.IsOperatingSystem()))
            {
                // mount every volume that doesn't have an accessPath so long as it does have an index defined. On
                // Windows systems, all volumes that are valid for disk I/O operations (i.e. not reserved, not hidden) will
                // have indexes associated.
                foreach (DiskVolume volume in disk.Volumes.Where(v => v.Index != null && v.AccessPaths?.Any() != true))
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
        /// Creates a process to run DiskSpd targeting the disks specified.
        /// </summary>
        /// <param name="executable">The full path to the DiskSpd executable.</param>
        /// <param name="commandArguments">
        /// The command line arguments to supply to the DiskSpd executable (e.g. -c4G -b4K -r4K -t1 -o1 -w100 -d480 -Suw -W15 -D -L -Rtext).
        /// </param>
        /// <param name="testedInstance">A name for the disks under test (e.g. remote_disk, remote_disk_premium_lrs).</param>
        /// <param name="disksToTest">The disks under test.</param>
        protected override DiskWorkloadProcess CreateWorkloadProcess(string executable, string commandArguments, string testedInstance, params Disk[] disksToTest)
        {
            string[] testFiles = disksToTest.Select(disk => this.GetTestFiles(disk.GetPreferredAccessPath(this.Platform))).ToArray();
            string diskSpdArguments = $"{commandArguments} {string.Join(" ", testFiles)}";

            IProcessProxy process = this.SystemManagement.ProcessManager.CreateProcess(executable, diskSpdArguments);

            return new DiskWorkloadProcess(process, testedInstance, testFiles);
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
                }).ConfigureAwait(false);
            }
        }
    }
}
