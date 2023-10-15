﻿// Copyright (c) Microsoft Corporation.
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
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
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
        private string historyResultsPath;

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
        /// The command line argument defined in the profile.
        /// </summary>
        public string GUIOption
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SpecViewExecutor.GUIOption));
            }
        }

        /// <summary>
        /// The viewset that will be run by SPECviewperf.
        /// </summary>
        public string Viewset
        {
            get
            {
                // Remove whitespaces in the argument (e.g. "3dsmax, catia" -> "3dsmax,catia")
                return this.Parameters.GetValue<string>(nameof(SpecViewExecutor.Viewset)).Replace(" ", string.Empty);
            }
        }

        /// <summary>
        /// The path to the RunViewperf.exe.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// Defines the path to the SPECview package that contains the workload
        /// executable.
        /// </summary>
        protected DependencyPath Package { get; set; }

        /// <summary>
        /// Initializes the environment
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            PlatformSpecifics.ThrowIfNotSupported(this.Platform);

            await this.InitializePackageLocationAsync(cancellationToken)
                .ConfigureAwait(false);

            this.ExecutablePath = this.PlatformSpecifics.Combine(this.Package.Path, "RunViewperf.exe");
        }

        /// <summary>
        /// Executes the SPECview workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IList<Metric> metrics = new List<Metric>();

            string commandArguments = this.GenerateCommandArguments();

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("executable", this.ExecutablePath)
                .AddContext("commandArguments", commandArguments);

            await this.SetUpEnvironmentVariable().ConfigureAwait(false);

            using (IProcessProxy process = await this.ExecuteCommandAsync(this.ExecutablePath, commandArguments, this.Package.Path, relatedContext, cancellationToken).ConfigureAwait(false))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext);
                    process.ThrowIfWorkloadFailed();
                    this.CaptureMetrics(process, commandArguments, relatedContext);
                }
            }

            if (this.TryGetContentStoreManager(out IBlobManager blobManager))
            {
                // specview logs are distributed in subdirectories corresponding to the viewsets
                string[] viewsetArray = this.Viewset.Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                string specviewOriginalLogPath;
                string specviewRenamedLogPath;
                List<Task> tasks = new List<Task>();
                foreach (string viewset in viewsetArray)
                {
                    // log file is inside a folder that starts with the viewset name e.g. 3dsmax-07
                    string? viewsetLogDir = this.fileSystem.Directory.GetDirectories(this.historyResultsPath, $"{viewset}*", SearchOption.TopDirectoryOnly).FirstOrDefault();
                    if (viewsetLogDir == null)
                    {
                        throw new WorkloadResultsException(
                            $"The expected SPECviewperf viewset log directory was not found in '{this.historyResultsPath}'.",
                            ErrorReason.WorkloadResultsNotFound);
                    }

                    specviewOriginalLogPath = this.PlatformSpecifics.Combine(viewsetLogDir, "log.txt");
                    specviewRenamedLogPath = this.PlatformSpecifics.Combine(viewsetLogDir, viewset + "-" + Path.GetFileName(specviewOriginalLogPath));
                    this.fileSystem.Directory.Move(specviewOriginalLogPath, specviewRenamedLogPath);
                    tasks.Add(this.UploadSpecviewLogAsync(blobManager, specviewRenamedLogPath, DateTime.UtcNow, cancellationToken));
                }

                await Task.WhenAll(tasks);
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
        private void CaptureMetrics(IProcessProxy workloadProcess, string commandArguments, EventContext telemetryContext)
        {
            if (workloadProcess.ExitCode == 0)
            {
                try
                {
                    // SPEC VIEW does not seem to support customized output folder. Results are outputted to a folder in the format of "results_20230913T052028"
                    string[] subdirectories = this.fileSystem.Directory.GetDirectories(this.Package.Path, "results_*", SearchOption.TopDirectoryOnly);

                    // Sort the "results_" subdirectories by creation time in descending order and take the first one
                    string resultsFileDir = subdirectories.OrderByDescending(d => this.fileSystem.Directory.GetCreationTime(d)).FirstOrDefault();
                    if (resultsFileDir == null) 
                    {
                        throw new WorkloadResultsException(
                            $"The expected SPECviewperf result directory was not found in '{this.Package.Path}'.",
                            ErrorReason.WorkloadResultsNotFound);
                    }

                    string resultsFilePath = this.PlatformSpecifics.Combine(resultsFileDir, "resultCSV.csv");
                    string resultsContent = this.fileSystem.File.ReadAllText(resultsFilePath);

                    SpecViewMetricsParser resultsParser = new (resultsContent);
                    IList<Metric> metrics = resultsParser.Parse();

                    this.MetadataContract.AddForScenario(
                           this.Scenario,
                           workloadProcess.FullCommand(),
                           toolVersion: "2020 v3.0");
                    this.MetadataContract.Apply(telemetryContext);

                    this.Logger.LogMetrics(
                        "SPECview",
                        this.Scenario,
                        workloadProcess.StartTime,
                        workloadProcess.ExitTime,
                        metrics,
                        null,
                        commandArguments,
                        this.Tags,
                        telemetryContext);

                    // rename the result file to avoid confusions on future runs
                    this.historyResultsPath = this.PlatformSpecifics.Combine(this.Package.Path, RenamePrefix + Path.GetFileName(resultsFileDir));
                    this.fileSystem.Directory.Move(resultsFileDir, this.historyResultsPath);
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
                this.Package = workloadPackage;
            }
        }

        /// <summary>
        /// Generate the SPECview Command Arguments
        /// </summary>
        private string GenerateCommandArguments()
        {      
            return $"-viewset \"{this.Viewset}\" {this.GUIOption}";
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