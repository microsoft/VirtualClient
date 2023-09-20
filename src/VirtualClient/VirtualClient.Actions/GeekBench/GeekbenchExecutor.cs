// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Platform;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using global::VirtualClient.Contracts.Metadata;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The Geekbench5 executor.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
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
        /// Executes Geek bench
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.DeleteResultsFile(telemetryContext);
            string commandLineArguments = this.GetCommandLineArguments();

            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                await this.ExecuteWorkloadAsync(this.ExecutablePath, commandLineArguments, telemetryContext, cancellationToken);
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

            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "geekbench5.exe");

                    this.SupportingExecutables.Add(this.ExecutablePath);
                    this.SupportingExecutables.Add(this.PlatformSpecifics.Combine(workloadPackage.Path, "geekbench_x86_64.exe"));
                    this.SupportingExecutables.Add(this.PlatformSpecifics.Combine(workloadPackage.Path, "geekbench_aarch64.exe"));
                    break;

                case PlatformID.Unix:
                    this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "geekbench5");

                    this.SupportingExecutables.Add(this.ExecutablePath);
                    this.SupportingExecutables.Add(this.PlatformSpecifics.Combine(workloadPackage.Path, "geekbench_x86_64"));
                    this.SupportingExecutables.Add(this.PlatformSpecifics.Combine(workloadPackage.Path, "geekbench_aarch64"));

                    foreach (string path in this.SupportingExecutables)
                    {
                        if (this.fileSystem.File.Exists(path))
                        {
                            await this.systemManagement.MakeFileExecutableAsync(path, this.Platform, CancellationToken.None);
                        }
                    }

                    break;
            }

            this.ResultsFilePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "geekbench5-output.txt");

            if (!this.fileSystem.File.Exists(this.ExecutablePath))
            {
                throw new DependencyException(
                    $"Geekbench executable not found at path '{this.ExecutablePath}'",
                    ErrorReason.WorkloadDependencyMissing);
            }
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = base.IsSupported()
                && 
                ((this.Platform == PlatformID.Win32NT && (this.CpuArchitecture == Architecture.X64 || this.CpuArchitecture == Architecture.Arm64))
                || (this.Platform == PlatformID.Unix && this.CpuArchitecture == Architecture.X64));

            if (!isSupported)
            {
                this.Logger.LogNotSupported("Geekbench", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, string resultsFilePath, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (!this.fileSystem.File.Exists(resultsFilePath))
                {
                    throw new WorkloadException(
                        $"The GeekBench results file was not found at path '{resultsFilePath}'.",
                        ErrorReason.WorkloadFailed);
                }

                string resultsJson = await this.fileSystem.File.ReadAllTextAsync(resultsFilePath);
                if (!GeekbenchResult.TryParseGeekbenchResult(resultsJson, out GeekbenchResult geekbenchResult))
                {
                    throw new WorkloadException(
                        $"The content of the GeekBench results file at path '{resultsFilePath}' content could not be parsed as valid JSON.",
                        ErrorReason.WorkloadFailed);
                }

                this.MetadataContract.AddForScenario(
                    "Geekbench5",
                    commandArguments,
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                // using workload name as testName
                IDictionary<string, Metric> metrics = geekbenchResult.GetResults();
                foreach (KeyValuePair<string, Metric> result in metrics)
                {
                    Metric metric = result.Value;
                    this.Logger.LogMetrics(
                        "Geekbench5",
                        result.Key,
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
                using (IProcessProxy process = this.processManager.CreateProcess(pathToExe, commandLineArguments))
                {
                    try
                    {
                        await process.StartAndWaitAsync(cancellationToken);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "Geekbench5", logToFile: true);

                            process.ThrowIfWorkloadFailed();
                            await this.CaptureMetricsAsync(process, this.ResultsFilePath, commandLineArguments, telemetryContext, cancellationToken);
                        }
                    }
                    finally
                    {
                        // GeekBench runs a secondary process on both Windows and Linux systems. When we
                        // kill the parent process, it does not kill the processes the parent spun off. This
                        // ensures that all of the process are stopped/killed.
                        this.processManager.SafeKill(this.SupportingExecutables.ToArray(), this.Logger);
                    }
                }
            });
        }

        private string GetCommandLineArguments()
        {
            return string.Format("{0} --export-json \"{1}\"", this.CommandLine, this.ResultsFilePath);
        }
    }
}