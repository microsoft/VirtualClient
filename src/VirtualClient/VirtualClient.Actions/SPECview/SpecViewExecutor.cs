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
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor
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
        public string CommandArguments
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SpecViewExecutor.CommandArguments));
            }
        }

        /// <summary>
        /// The viewset that will be run by SPECviewperf.
        /// </summary>
        public string Viewset
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SpecViewExecutor.Viewset));
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

            string viewset = this.GenerateCommandArguments(this.Viewset);

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("executable", this.ExecutablePath)
                .AddContext("commandArguments", this.CommandArguments);

            await this.SetEnvironmentVariable().ConfigureAwait(false);

            IProcessProxy process = await this.ExecuteCommandAsync(this.ExecutablePath, $"{viewset} {this.CommandArguments}", Environment.CurrentDirectory, relatedContext, cancellationToken).ConfigureAwait(false);
            this.CaptureMetrics(process, this.CommandArguments, relatedContext);
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
                    this.fileSystem.File.Move(resultsFilePath, "hist_" + resultsFilePath);
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
        /// Generate the SPECview viewset Command Arguments
        /// </summary>
        private string GenerateCommandArguments(string viewset)
        {      
            return $"-viewset \"{viewset}\"";
        }

        private async Task SetEnvironmentVariable()
        {
            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath visualStudioCRuntimePackage = await packageManager.GetPackageAsync(VisualStudioCRuntimePackageName, CancellationToken.None).ConfigureAwait(false);
            string visualStudioCRuntimeDllPath = this.PlatformSpecifics.ToPlatformSpecificPath(visualStudioCRuntimePackage, this.Platform, this.CpuArchitecture).Path;
            this.SetEnvironmentVariable(EnvironmentVariable.PATH, visualStudioCRuntimeDllPath, EnvironmentVariableTarget.Machine, append: true);
        }
    }

}
