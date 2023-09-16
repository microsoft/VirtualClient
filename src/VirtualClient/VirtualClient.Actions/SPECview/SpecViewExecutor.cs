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
    /// Executes the 3DMark workload.
    /// </summary>
    [WindowsCompatible]

    public class SpecViewExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;

        /// <summary>
        /// ConstructorD
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SpecViewExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.OutFileName = $"{DateTimeOffset.Now.ToUnixTimeSeconds()}.out";
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
        /// The path to the RunViewperf.exe.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// The path for the intermediate results file.
        /// </summary>
        public string OutFileName { get; set; }

        /// <summary>
        /// Defines the path to the SPEC view package that contains the workload
        /// executable.
        /// </summary>
        protected DependencyPath Package { get; set; }

        /// <summary>
        /// Executes the SPEC view workload.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.ExecuteWorkloadAsync(telemetryContext, cancellationToken);
        }

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
        /// Processes benchmark results
        /// </summary>
        private void CaptureMetrics(IProcessProxy workloadProcess, string commandArguments, EventContext telemetryContext)
        {
            // if (workloadProcess.ExitCode == 0)
            // {
                try
                {
                    // SPEC VIEW does not seem to support customized output folder. Results are outputted to a folder in the format of "results_20230913T052028"
                    string[] subdirectories = this.fileSystem.Directory.GetDirectories(this.Package.Path, "results_*", SearchOption.TopDirectoryOnly);

                    // Sort the "results_" subdirectories by creation time in descending order and take the first one
                    string resultsFileDir = subdirectories.OrderByDescending(d => this.fileSystem.Directory.GetCreationTime(d)).FirstOrDefault();
                    string resultsFilePath = this.PlatformSpecifics.Combine(resultsFileDir, "resultCSV.csv");
                    string resultsContent = this.fileSystem.File.ReadAllText(resultsFilePath);

                    SpecViewMetricsParser resultsParser = new (resultsContent);
                    // TODO: how to get the node id/vm Id and where to log the node id/ vm Id.
                    IList<Metric> metrics = resultsParser.Parse();

                    this.MetadataContract.AddForScenario(
                           this.Scenario,
                           "test args",
                           // workloadProcess.FullCommand(),
                           toolVersion: "2020 v3.0");
                    this.MetadataContract.Apply(telemetryContext);

                    // TODO: do we want a metric categorization.
                    this.Logger.LogMetrics(
                        "SPECview",
                        this.Scenario,
                        new DateTime(2099, 12, 31),
                        new DateTime(2099, 12, 31),
                        // workloadProcess.StartTime,
                        // workloadProcess.ExitTime,
                        metrics,
                        null,
                        commandArguments,
                        this.Tags,
                        telemetryContext);
                }
                catch (SchemaException exc)
                {
                    EventContext relatedContext = telemetryContext.Clone()
                        .AddError(exc);

                    this.Logger.LogMessage($"{nameof(SpecViewExecutor)}.WorkloadOutputParsingFailed", LogLevel.Warning, relatedContext);
                }

                // TODO: experiment null file and see if we need this block. I suspect that SchemaException will catch everything.
                catch (ArgumentNullException exc)
                {
                    EventContext relatedContext = telemetryContext.Clone()
                        .AddError(exc);

                    this.Logger.LogMessage($"{nameof(SpecViewExecutor)}.WorkloadOutputFileNotFound", LogLevel.Warning, relatedContext);
                }

            // }
    }

        /// <summary>
        /// Run the Spec View Workload
        /// </summary>
        private Task ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IList<Metric> metrics = new List<Metric>();

            string commandArguments = this.CommandArguments;

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("executable", this.ExecutablePath)
                .AddContext("commandArguments", commandArguments);

            return this.Logger.LogMessageAsync($"{nameof(SpecViewExecutor)}.ExecuteWorkload", relatedContext, async () =>
            {
                using IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(this.ExecutablePath, commandArguments);
                this.CleanupTasks.Add(() => process.SafeKill());

                try
                {
                    await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext);
                        process.ThrowIfWorkloadFailed();
                        this.CaptureMetrics(process, commandArguments, telemetryContext);
                    }
                }
                finally
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
            });
        }

            /// <summary>
            /// Validate the 3DMark Package
            /// </summary>
        private async Task InitializePackageLocationAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                DependencyPath workloadPackage = await this.systemManagement.PackageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                    .ConfigureAwait(false) ?? throw new DependencyException(
                        $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                        ErrorReason.WorkloadDependencyMissing);
                // TODO: do we plan to run spec on linux; should I create a folder like win-x64?
                // workloadPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);
                this.Package = workloadPackage;
            }
        }
    }

}
