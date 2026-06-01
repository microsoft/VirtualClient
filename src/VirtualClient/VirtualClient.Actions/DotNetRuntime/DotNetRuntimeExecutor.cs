// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// DotNetRuntime workload executor.
    /// </summary>
    [WindowsCompatible]
    public class DotNetRuntimeExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ProcessManager processManager;
        private ISystemManagement systemManagement;

        /// <summary>
        /// The path to the dotNet.props file in DotNetRuntime workload.
        /// </summary>
        private string propsFilePath;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public DotNetRuntimeExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.processManager = dependencies.GetService<ProcessManager>();
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.SupportingExecutables = new List<string>();
        }

        /// <summary>
        /// Path to DotNetRuntime Package.
        /// </summary>
        public string PackagePath { get; set; }

        /// <summary>
        /// The path to the DotNetRuntime executable.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// The number of JVM(Java Virtual Machine) instances of the DotNetRuntime workload to be intialized.
        /// </summary>
        public string NumberOfJvmInstances
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DotNetRuntimeExecutor.NumberOfJvmInstances), out IConvertible numberOfJvmInstances);
                return numberOfJvmInstances?.ToString();
            }
        }

        /// <summary>
        /// The number of Warehouses to be intialized.
        /// </summary>
        public string NumberOfWarehouses
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DotNetRuntimeExecutor.NumberOfWarehouses), out IConvertible numberOfWarehouses);
                return numberOfWarehouses?.ToString();
            }
        }

        /// <summary>
        /// The time in seconds that the workload will ramp up/warm up.
        /// </summary>
        public string RampUpSeconds
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DotNetRuntimeExecutor.RampUpSeconds), out IConvertible rampUpSeconds);
                return rampUpSeconds?.ToString();
            }
        }

        /// <summary>
        /// The time in seconds that the workload will run.
        /// </summary>
        public string MeasurementSeconds
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DotNetRuntimeExecutor.MeasurementSeconds), out IConvertible measurementSeconds);
                return measurementSeconds?.ToString();
            }
        }

        /// <summary>
        /// The the target throughput when we run the workload.
        /// </summary>
        public string FixedThroughput
        {
            get
            {
                this.Parameters.TryGetValue(nameof(DotNetRuntimeExecutor.FixedThroughput), out IConvertible fixedThroughput);
                return fixedThroughput?.ToString();
            }
        }

        /// <summary>
        /// Path to the results file after executing dotNetRuntime workload.
        /// </summary>
        public string ResultsFilePath { get; set; }

        /// <summary>
        /// A set of paths for supporting executables of the main process 
        /// cleaned up/terminated at the end of each round of processing.
        /// </summary>
        protected IList<string> SupportingExecutables { get; }

        /// <summary>
        /// Executes DotNet Runtime.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.ResetResultsFile(telemetryContext);

            try
            {
                DateTime startTime = DateTime.UtcNow;
                await this.ExecuteWorkloadAsync(this.ExecutablePath, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                DateTime endTime = DateTime.UtcNow;
            }
            finally
            {
                // Ensure any dotnet.exe instances running are stopped.
                this.processManager.SafeKill(this.SupportingExecutables, this.Logger);
            }
        }

        /// <summary>
        /// Initializes the environment and dependencies for running the DotNetRuntime workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath workloadPackage = await packageManager.GetPackageAsync(this.PackageName, cancellationToken)
                .ConfigureAwait(false);

            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            workloadPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);

            this.PackagePath = workloadPackage.Path;
            this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "dotnet.bat");
            this.SupportingExecutables.Add(this.PlatformSpecifics.Combine(workloadPackage.Path, "dotnet.exe"));
            this.propsFilePath = this.PlatformSpecifics.Combine(workloadPackage.Path, @"dotNet.props");

            await this.UpdatePropsFile(this.propsFilePath);

            if (!this.fileSystem.File.Exists(this.ExecutablePath))
            {
                throw new DependencyException(
                    $"DotNetRuntime executable not found at path '{this.ExecutablePath}'",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.ResultsFilePath = this.PlatformSpecifics.Combine(this.PackagePath, @"results\results.txt");
        }

        /// <summary>
        /// Updates the parameters value in dotnet.props file with the values provided through command line.
        /// </summary>
        private async Task UpdatePropsFile(string propsFilePath)
        {
            if (this.NumberOfJvmInstances != null)
            {
                await this.ReplaceInFileAsync(
                    propsFilePath, @"input.jvm_instances=[0-9]+", $"input.jvm_instances={this.NumberOfJvmInstances}");
            }

            if (this.NumberOfWarehouses != null)
            {
                await this.ReplaceInFileAsync(
                    propsFilePath,
                    @"input.sequence_of_number_of_warehouses=[0-9]+",
                    $"input.sequence_of_number_of_warehouses={this.NumberOfWarehouses}");
            }

            if (this.RampUpSeconds != null)
            {
                await this.ReplaceInFileAsync(
                    propsFilePath,
                    @"input.ramp_up_seconds=[0-9]+",
                    $"input.ramp_up_seconds={this.RampUpSeconds}");
            }

            if (this.MeasurementSeconds != null)
            {
                await this.ReplaceInFileAsync(
                    propsFilePath,
                    @"input.measurement_seconds=[0-9]+",
                    $"input.measurement_seconds={this.MeasurementSeconds}");
            }

            if (this.FixedThroughput != null)
            {
                await this.ReplaceInFileAsync(
                    propsFilePath,
                    @"input.fixed_throughput=[0-9]+",
                    $"input.fixed_throughput={this.FixedThroughput}");
            }
        }

        /// <summary>
        /// Replaces text in a file.
        /// </summary>
        /// <param name="filePath">Path of the text file.</param>
        /// <param name="searchText">Text to search for.</param>
        /// <param name="replaceText">Text to replace the search text.</param>
        private async Task ReplaceInFileAsync(string filePath, string searchText, string replaceText)
        {
            using (Stream stream = this.fileSystem.FileStream.New(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                byte[] fileContent = new byte[stream.Length];
                await stream.ReadAsync(fileContent, 0, fileContent.Length).ConfigureAwait(false);

                string content = Regex.Replace(Encoding.Default.GetString(fileContent), searchText, replaceText);

                stream.SetLength(0);
                await stream.WriteAsync(Encoding.UTF8.GetBytes(content), 0, content.Length);
            }
        }

        private void ResetResultsFile(EventContext telemetryContext)
        {
            try
            {
                if (this.fileSystem.File.Exists(this.ResultsFilePath))
                {
                    this.fileSystem.File.Delete(this.ResultsFilePath);
                }

                string resultsDirectory = Path.GetDirectoryName(this.ResultsFilePath);
                if (!this.fileSystem.Directory.Exists(resultsDirectory))
                {
                    this.fileSystem.Directory.CreateDirectory(resultsDirectory);
                }

                this.fileSystem.File.WriteAllText(this.ResultsFilePath, string.Empty);
            }
            catch (IOException exc)
            {
                this.Logger.LogErrorMessage(exc, telemetryContext);
            }
        }

        private Task ExecuteWorkloadAsync(string executablePath, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("executable", executablePath);

            return this.Logger.LogMessageAsync($"{nameof(DotNetRuntimeExecutor)}.ExecuteWorkload", relatedContext, async () =>
            {
                DateTime startTime = DateTime.UtcNow;
                ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();

                using (IProcessProxy process = systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, executablePath))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());
                    await process.StartAndWaitAsync(cancellationToken);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            process.ThrowIfWorkloadFailed();

                            // Allow the dotnet.exe application to finish its work calculating results and writing them to
                            // the results file.
                            KeyValuePair<string, string> results = await this.WaitForResultsAsync(this.ResultsFilePath, TimeSpan.FromMinutes(30), cancellationToken);

                            await this.LogProcessDetailsAsync(process, telemetryContext, "DotNetRuntime", logToFile: true, results: results);
                            this.CaptureMetrics(results.Value, executablePath, startTime, DateTime.UtcNow, telemetryContext, cancellationToken);
                        }
                        catch
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "DotNetRuntime", logToFile: true);
                        }
                    }
                }
            });
        }

        private void CaptureMetrics(string results, string commandArguments, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                if (!string.IsNullOrWhiteSpace(results))
                {
                    try
                    {
                        this.MetadataContract.AddForScenario(
                           "DotNetRuntime",
                           commandArguments,
                           toolVersion: null);

                        this.MetadataContract.Apply(telemetryContext);

                        DotNetRuntimeMetricsParser dotNetParser = new DotNetRuntimeMetricsParser(results);
                        IList<Metric> metrics = dotNetParser.Parse();

                        this.Logger.LogMetrics(
                        "DotNetRuntime",
                        "DotNetRuntime",
                        startTime,
                        endTime,
                        metrics,
                        metricCategorization: string.Empty,
                        scenarioArguments: commandArguments,
                        this.Tags,
                        telemetryContext);
                    }
                    catch (SchemaException exc)
                    {
                        throw new WorkloadException($"Failed to parse workload results file.", exc, ErrorReason.WorkloadFailed);
                    }
                }
                else
                {
                    throw new WorkloadException(
                        $"Missing results. Workload results were not emitted by the workload.",
                        ErrorReason.WorkloadFailed);
                }
            }
        }

        private async Task<KeyValuePair<string, string>> WaitForResultsAsync(string resultsFilePath, TimeSpan timeout, CancellationToken cancellationToken)
        {
            KeyValuePair<string, string> results = default;
            DateTime waitTimeout = DateTime.UtcNow.Add(timeout);

            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < waitTimeout)
            {
                string content = await this.fileSystem.File.ReadAllTextAsync(resultsFilePath, cancellationToken);

                if (!string.IsNullOrWhiteSpace(content))
                {
                    results = new KeyValuePair<string, string>(resultsFilePath, content);
                    break;
                }

                await Task.Delay(500);
            }

            return results;
        }
    }
}