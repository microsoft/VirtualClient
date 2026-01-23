// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using global::VirtualClient.Contracts.Metadata;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The Geekbench executor.
    /// </summary>
    public class GeekbenchExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ProcessManager processManager;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public GeekbenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.packageManager = dependencies.GetService<IPackageManager>();
            this.processManager = dependencies.GetService<ProcessManager>();
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.SupportingExecutables = new List<string>();
        }

        /// <summary>
        /// The command line argument defined in the profile.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(GeekbenchExecutor.CommandLine));
            }
        }

        /// <summary>
        /// The path to the Geekbench executable.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// The path where the Geekbench JSON results file should be output.
        /// </summary>
        public string ResultsFilePath { get; set; }

        /// <summary>
        /// A set of paths for supporting executables of the main process 
        /// (e.g. geekbench_x86_64, geekbench_aarch64). These typically need to 
        /// be cleaned up/terminated at the end of each round of processing.
        /// </summary>
        protected IList<string> SupportingExecutables { get; }

        /// <summary>
        /// Executes Geek bench
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.DeleteResultsFile(telemetryContext);

            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                await this.ExecuteWorkloadAsync(this.ExecutablePath, this.CommandLine, telemetryContext, cancellationToken);
            }
        }

        /// <summary>
        /// Initializes the environment and dependencies for running the Geekbench workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.packageManager.GetPackageAsync(this.PackageName, CancellationToken.None);

            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            workloadPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);

            switch (this.PlatformArchitectureName)
            {
                case "win-x64":
                    this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "geekbench_x86_64.exe");
                    break;

                case "win-arm64":
                    this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "geekbench_aarch64.exe");
                    break;

                case "linux-x64":
                    this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "geekbench_x86_64");
                    await this.systemManagement.MakeFilesExecutableAsync(workloadPackage.Path, this.Platform, CancellationToken.None);

                    break;

                case "linux-arm64":
                    this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "geekbench_aarch64");
                    await this.systemManagement.MakeFilesExecutableAsync(workloadPackage.Path, this.Platform, CancellationToken.None);

                    break;
            }

            this.ResultsFilePath = this.PlatformSpecifics.Combine(workloadPackage.Path, $"{this.PackageName}-output.txt");

            if (!this.fileSystem.File.Exists(this.ExecutablePath))
            {
                throw new DependencyException(
                    $"Geekbench executable not found at path '{this.ExecutablePath}'",
                    ErrorReason.WorkloadDependencyMissing);
            }

            // If we are executing Geekbench6 (vs. Geekbench5), there is a requirement to unlock the license
            // using a license file that is present only in Geekbench6.
            string preferencesFilePath = this.Combine(workloadPackage.Path, "Geekbench_6.preferences");

            if (this.fileSystem.File.Exists(preferencesFilePath))
            {
                string preferences = null;
                using (StreamReader sr = new StreamReader(preferencesFilePath))
                {
                    // Read the first line from the file
                    preferences = await sr.ReadLineAsync();
                }

                string licenseKey = Regex.Match(preferences, TextParsingExtensions.GUIDRegex).Groups[0].Value;
                string email = Regex.Match(preferences, TextParsingExtensions.EmailRegex).Groups[0].Value;

                if (string.IsNullOrWhiteSpace(preferences))
                {
                    throw new DependencyException(
                        $"Invalid Geekbench6 licensing file. The licence file at the path '{preferencesFilePath}' does not have valid licensing information.",
                        ErrorReason.InvalidOrMissingLicense);
                }

                try
                {
                    using (IProcessProxy process = this.processManager.CreateProcess(this.ExecutablePath, $"--unlock {email} {licenseKey}"))
                    {
                        try
                        {
                            await process.StartAndWaitAsync(cancellationToken, withExitConfirmation: true);

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, this.PackageName);
                                process.ThrowIfDependencyInstallationFailed();
                            }
                        }
                        finally
                        {
                            process.Close();
                            process.SafeKill(this.Logger, TimeSpan.FromSeconds(30));
                        }
                    }
                }
                catch (Exception exc)
                {
                    this.Logger.LogMessage(
                        $"{this.TypeName}.WorkloadStartError",
                        LogLevel.Warning,
                        telemetryContext.Clone().AddError(exc));

                    throw;
                }
            }
        }

        private void CaptureMetrics(IProcessProxy process, string standardOutput, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (string.IsNullOrEmpty(standardOutput))
                {
                    throw new WorkloadException(
                        $"GeekBench did not write metrics to console.",
                        ErrorReason.WorkloadFailed);
                }

                this.MetadataContract.AddForScenario(
                    this.PackageName,
                    commandArguments,
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                // using workload name as testName
                GeekBenchMetricsParser geekbenchMetricsParser = new GeekBenchMetricsParser(standardOutput);
                IList<Metric> metrics = geekbenchMetricsParser.Parse();

                foreach (Metric metric in metrics)
                {
                    this.Logger.LogMetric(
                        this.PackageName,
                        this.Scenario,
                        process.StartTime,
                        process.ExitTime,
                        metric.Name,
                        metric.Value,
                        metric.Unit,
                        null,
                        commandArguments,
                        this.Tags,
                        telemetryContext,
                        metric.Relativity);
                }
            }
        }

        private void DeleteResultsFile(EventContext telemetryContext)
        {
            try
            {
                if (this.fileSystem.File.Exists(this.ResultsFilePath))
                {
                    this.fileSystem.File.Delete(this.ResultsFilePath);
                }
            }
            catch (IOException exc)
            {
                this.Logger.LogErrorMessage(exc, telemetryContext);
            }
        }

        private Task ExecuteWorkloadAsync(string pathToExe, string commandLineArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("executable", pathToExe)
                .AddContext("commandArguments", commandLineArguments);

            return this.Logger.LogMessageAsync($"{nameof(GeekbenchExecutor)}.ExecuteWorkload", relatedContext, async () =>
            {
                try
                {
                    using (IProcessProxy process = this.processManager.CreateProcess(pathToExe, commandLineArguments))
                    {
                        try
                        {
                            await process.StartAndWaitAsync(cancellationToken, withExitConfirmation: true);

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, this.PackageName);
                                process.ThrowIfWorkloadFailed();

                                string standardOutput = process.StandardOutput.ToString();
                                this.CaptureMetrics(process, standardOutput, commandLineArguments, telemetryContext, cancellationToken);
                            }
                        }
                        finally
                        {
                            process.Close();
                            process.SafeKill(this.Logger, TimeSpan.FromSeconds(30));
                        }
                    }
                }
                catch (Exception exc)
                {
                    this.Logger.LogMessage(
                        $"{this.TypeName}.WorkloadStartError",
                        LogLevel.Warning,
                        telemetryContext.Clone().AddError(exc));

                    throw;
                }
            });
        }
    }
}