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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Executes the SPECView workload.
    /// </summary>
    [WindowsCompatible]
    public class SpecViewExecutor : VirtualClientComponent
    {
        private const string VisualStudioCRuntimePackageName = "visualstudiocruntime";
        private const string RenamePrefix = "hist_";
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor for <see cref="SpecViewExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SpecViewExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.systemManagement = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The viewsets that will be run by SPECviewperf.
        /// </summary>
        public string[] Viewsets
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SpecViewExecutor.Viewsets)).Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }

        /// <summary>
        /// Using PsExec to run specviewperf in session 1.
        /// </summary>
        public string PsExecPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SpecViewExecutor.PsExecPackageName));
            }
        }

        /// <summary>
        /// PsExec session number
        /// </summary>
        public int PsExecSession
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(SpecViewExecutor.PsExecSession));
            }
        }

        /// <summary>
        /// The path to the RunViewperf.exe.
        /// </summary>
        protected string SpecviewExecutablePath { get; set; }

        /// <summary>
        /// Defines the path to the SPECview package that contains the workload
        /// executable.
        /// </summary>
        protected DependencyPath SpecviewPackage { get; set; }

        /// <summary>
        /// The path to the PsExec.exe.
        /// </summary>
        protected string PsExecExecutablePath { get; set; }

        /// <summary>
        /// Defines the path to the PsExec package that contains the workload
        /// executable.
        /// </summary>
        protected DependencyPath PsExecPackage { get; set; }

        /// <summary>
        /// Initializes the environment
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            PlatformSpecifics.ThrowIfNotSupported(this.Platform);

            await this.InitializePackageLocationAsync(cancellationToken)
                .ConfigureAwait(false);

            this.SpecviewExecutablePath = this.PlatformSpecifics.Combine(this.SpecviewPackage.Path, "RunViewperf.exe");
            this.PsExecExecutablePath = this.PlatformSpecifics.Combine(this.PsExecPackage.Path, "PsExec.exe");
        }

        /// <summary>
        /// Executes the SPECview workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string executablePath = this.PsExecSession == -1 ? this.SpecviewExecutablePath : this.PsExecExecutablePath;
            string workingDir = this.PsExecSession == -1 ? this.SpecviewPackage.Path : this.PsExecPackage.Path;
            await this.SetUpEnvironmentVariable().ConfigureAwait(false);

            foreach (string viewset in this.Viewsets)
            {
                string commandArguments = this.GenerateCommandArguments(viewset);

                EventContext relatedContext = telemetryContext.Clone()
                    .AddContext("executable", this.SpecviewExecutablePath)
                    .AddContext("commandArguments", commandArguments);

                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(executablePath, commandArguments, workingDir, relatedContext, cancellationToken).ConfigureAwait(false))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext);
                            process.ThrowIfWorkloadFailed();
                            this.CaptureMetrics(process, commandArguments, relatedContext, cancellationToken, viewset);
                        }
                    }
                }
            }          
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = base.IsSupported()
                && this.Platform == PlatformID.Win32NT
                && this.CpuArchitecture == Architecture.X64;

            if (!isSupported)
            {
                this.Logger.LogNotSupported("SPECviewperf", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        /// <summary>
        /// Processes benchmark results
        /// </summary>
        private void CaptureMetrics(IProcessProxy workloadProcess, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken, string scenario)
        {
            if (workloadProcess.ExitCode == 0)
            {
                try
                {
                    // SPEC VIEW does not seem to support customized output folder. Results are outputted to a folder in the format of "results_20230913T052028"
                    string[] subdirectories = this.fileSystem.Directory.GetDirectories(this.SpecviewPackage.Path, "results_*", SearchOption.TopDirectoryOnly);

                    // Sort the "results_" subdirectories by creation time in descending order and take the first one
                    string resultsFileDir = subdirectories.OrderByDescending(d => this.fileSystem.Directory.GetCreationTime(d)).FirstOrDefault();

                    string resultsFilePath = this.PlatformSpecifics.Combine(resultsFileDir, "resultCSV.csv");
                    string resultsContent = this.fileSystem.File.ReadAllText(resultsFilePath);

                    if (resultsContent == null)
                    {
                        throw new WorkloadResultsException(
                            $"The expected SPECviewperf result directory was not found in '{this.SpecviewPackage.Path}'.",
                            ErrorReason.WorkloadResultsNotFound);
                    }

                    SpecViewMetricsParser resultsParser = new (resultsContent);
                    IList<Metric> metrics = resultsParser.Parse();

                    this.MetadataContract.AddForScenario(
                           scenario,
                           workloadProcess.FullCommand(),
                           toolVersion: this.SpecviewPackage.Version);
                    this.MetadataContract.Apply(telemetryContext);

                    this.Logger.LogMetrics(
                        this.PackageName,
                        scenario,
                        workloadProcess.StartTime,
                        workloadProcess.ExitTime,
                        metrics,
                        null,
                        commandArguments,
                        this.Tags,
                        telemetryContext);

                    // rename the result file to avoid confusions on future runs
                    string historyResultsPath = this.PlatformSpecifics.Combine(this.SpecviewPackage.Path, RenamePrefix + Path.GetFileName(resultsFileDir));
                    this.fileSystem.Directory.Move(resultsFileDir, historyResultsPath);

                    if (this.TryGetContentStoreManager(out IBlobManager blobManager))
                    {
                        string specviewOriginalLogPath = this.fileSystem.Directory.GetFiles(historyResultsPath, "log.txt", SearchOption.AllDirectories).FirstOrDefault();
                        string specviewRenamedLogPath = this.PlatformSpecifics.Combine(Path.GetDirectoryName(specviewOriginalLogPath), scenario + "-" + "log.txt");
                        this.fileSystem.Directory.Move(specviewOriginalLogPath, specviewRenamedLogPath);
                        // other viewsets can start while the previous viewset's log file is uploading.
                        this.UploadSpecviewLogAsync(blobManager, specviewRenamedLogPath, DateTime.UtcNow, cancellationToken);
                    }
                }
                catch (SchemaException exc)
                {
                    EventContext relatedContext = telemetryContext.Clone()
                        .AddError(exc);

                    this.Logger.LogMessage($"{nameof(SpecViewExecutor)}.WorkloadOutputParsingFailed", LogLevel.Warning, relatedContext);
                }
            }
        }

        /// <summary>
        /// Validate the SPECview Package
        /// </summary>
        private async Task InitializePackageLocationAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                DependencyPath workloadPackage = await this.systemManagement.PackageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                    .ConfigureAwait(false) ?? throw new DependencyException(
                        $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                        ErrorReason.WorkloadDependencyMissing);

                DependencyPath psExecPackage = await this.systemManagement.PackageManager.GetPackageAsync(this.PsExecPackageName, CancellationToken.None).
                    ConfigureAwait(false) ?? throw new DependencyException(
                        $"The expected package '{this.PsExecPackageName}' does not exist on the system or is not registered.",
                        ErrorReason.WorkloadDependencyMissing);

                this.SpecviewPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);
                this.PsExecPackage = this.PlatformSpecifics.ToPlatformSpecificPath(psExecPackage, this.Platform, this.CpuArchitecture);
            }
        }

        /// <summary>
        /// Generate the SPECview Command Arguments
        /// </summary>
        private string GenerateCommandArguments(string viewset)
        {
            if (this.PsExecSession == -1)
            {
                // not using psexec - run specviewperf directly.
                return $"-viewset {viewset} -nogui";
            }
            else
            {
                // using psexec and run specviewperf in the specified session.
                string baseArg = @$"-s -i {this.PsExecSession} -w {this.SpecviewPackage.Path} -accepteula -nobanner";
                string specViewPerfCmd = @$"{this.SpecviewExecutablePath} -viewset {viewset} -nogui";
                return $"{baseArg} {specViewPerfCmd}";
            }

        }

        private async Task SetUpEnvironmentVariable()
        {
            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath visualStudioCRuntimePackage = await packageManager.GetPackageAsync(VisualStudioCRuntimePackageName, CancellationToken.None).ConfigureAwait(false);
            string visualStudioCRuntimeDllPath = this.PlatformSpecifics.ToPlatformSpecificPath(visualStudioCRuntimePackage, this.Platform, this.CpuArchitecture).Path;
            this.SetEnvironmentVariable(EnvironmentVariable.PATH, visualStudioCRuntimeDllPath, EnvironmentVariableTarget.Machine, append: true);
        }

        private Task UploadSpecviewLogAsync(IBlobManager blobManager, string specviewLogPath, DateTime logTime, CancellationToken cancellationToken)
        {
            // Example Blob Store Structure:
            // 9ed58814-435b-4900-8eb2-af86393e0059/my-vc/specview/specviewperf/2023-10-11T22-07-41-73235Z-log.txt
            FileUploadDescriptor descriptor = this.CreateFileUploadDescriptor(
                new FileContext(
                    this.fileSystem.FileInfo.New(specviewLogPath),
                    HttpContentType.PlainText,
                    Encoding.UTF8.WebName,
                    this.ExperimentId,
                    this.AgentId,
                    "specview",
                    this.Scenario,
                    null,
                    this.Roles?.FirstOrDefault()));
            return this.UploadFileAsync(blobManager, this.fileSystem, descriptor, cancellationToken, deleteFile: false);
        }
    }
}