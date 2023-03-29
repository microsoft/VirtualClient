// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The SPECjvm workload executor.
    /// </summary>
    public class SpecJvmExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManagement;

        /// <summary>
        /// The path to the SPECjvm package.
        /// </summary>
        private string packageDirectory;

        /// <summary>
        /// The path to the Java executable package.
        /// </summary>
        private string javaExecutableDirectory;

        /// <summary>
        /// Constructor for <see cref="SpecJvmExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SpecJvmExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
            this.stateManager = this.systemManagement.StateManager;
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <summary>
        /// Java Development Kit package name.
        /// </summary>
        public string JdkPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SpecJvmExecutor.JdkPackageName));
            }
        }

        /// <summary>
        /// The sub-benchmarks in SPECjvm.
        /// </summary>
        public string Workloads
        {
            get
            {
                this.Parameters.TryGetValue(nameof(SpecJvmExecutor.Workloads), out IConvertible workloads);
                // In the profile the workloads are delimitered by comma, replacing with space.
                return workloads?.ToString().Replace(",", " ");
            }
        }

        /// <summary>
        /// Executes the SPECjvm workload.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string commandLineArguments = this.GetCommandLineArguments();

            return this.ExecuteWorkloadAsync(this.javaExecutableDirectory, commandLineArguments, this.packageDirectory, cancellationToken);
        }

        /// <summary>
        /// Initializes the environment for execution of the SPECjvm workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // No initiation needed, just check if the packages and JDK is there.
            DependencyPath workloadPackage = await this.packageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                .ConfigureAwait(false);

            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.packageDirectory = workloadPackage.Path;

            DependencyPath javaExecutable = await this.packageManager.GetPackageAsync(this.JdkPackageName, CancellationToken.None)
                .ConfigureAwait(false);

            if (javaExecutable == null || !javaExecutable.Metadata.ContainsKey(PackageMetadata.ExecutablePath))
            {
                throw new DependencyException(
                    $"The expected Java executable does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.javaExecutableDirectory = javaExecutable.Metadata[PackageMetadata.ExecutablePath].ToString();
        }

        private async Task ExecuteWorkloadAsync(string pathToExe, string commandLineArguments, string workingDirectory, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(SpecJvmExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                    {
                        using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                        {
                            this.CleanupTasks.Add(() => process.SafeKill());
                            this.LogProcessTrace(process);
                            await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                            await this.ValidateProcessExitedAsync(process.Id, TimeSpan.FromMinutes(10), cancellationToken);

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                if (process.IsErrored())
                                {
                                    await this.LogProcessDetailsAsync(process, telemetryContext, "SPECjvm", logToFile: true);
                                    process.ThrowIfWorkloadFailed();
                                }

                                await this.CaptureMetricsAsync(process, commandLineArguments, telemetryContext, cancellationToken);
                            }
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                // SPECjvm2008.012.txt
                string resultsDirectory = this.PlatformSpecifics.Combine(this.packageDirectory, "results");
                string[] outputFiles = this.fileSystem.Directory.GetFiles(resultsDirectory, "SPECjvm2008.*.txt", SearchOption.AllDirectories);

                foreach (string file in outputFiles)
                {
                    string results = await this.LoadResultsAsync(file, cancellationToken);
                    await this.LogProcessDetailsAsync(process, telemetryContext, "SPECjvm", results: results.AsArray(), logToFile: true);

                    SpecJvmMetricsParser parser = new SpecJvmMetricsParser(results);

                    this.Logger.LogMetrics(
                        toolName: "SPECjvm",
                        scenarioName: "SPECjvm",
                        process.StartTime,
                        process.ExitTime,
                        parser.Parse(),
                        metricCategorization: "SPECjvm",
                        scenarioArguments: commandArguments,
                        this.Tags,
                        telemetryContext);

                    await this.fileSystem.File.DeleteAsync(file);
                }
            }
        }

        private string GetCommandLineArguments()
        {
            // Looks like this: java -jar SPECjvm2008.jar -ikv -ict xml.transform
            return @$"{this.CalculateJavaOptions()} -jar SPECjvm2008.jar -ikv -ict {this.Workloads}";
        }

        private string CalculateJavaOptions()
        {
            MemoryInfo memoryInfo = this.systemManagement.GetMemoryInfoAsync(CancellationToken.None)
                .GetAwaiter().GetResult();

            long totalMemoryKiloBytes = memoryInfo.TotalMemory;
            int jvmMemoryInMegaBytes = Convert.ToInt32(totalMemoryKiloBytes * 0.85 / 1024);
            int coreCount = Environment.ProcessorCount;

            // -Xms size in bytes Sets the initial size of the Java heap. The default size is 2097152(2MB).
            // -Xmx size in bytes Sets the maximum size to which the Java heap can grow. The default size is 64M.
            // -Xmn size in bytes Sets the initial Java heap size for the Eden generation. The default value is 640K.

            // According to the MSFT Java Engineering Group (JEG) team. The memory here should set to 85% of the system memory.
            string jvmArgument = $"-XX:ParallelGCThreads={coreCount} -XX:+UseParallelGC -XX:+UseAES -XX:+UseSHA -Xms{jvmMemoryInMegaBytes}m -Xmx{jvmMemoryInMegaBytes}m";
            return jvmArgument;
        }

        private async Task ValidateProcessExitedAsync(int processId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            DateTime exitTime = DateTime.UtcNow.Add(timeout);

            while (DateTime.UtcNow < exitTime)
            {
                IProcessProxy existingProcess = this.systemManagement.ProcessManager.GetProcess(processId);
                if (existingProcess == null)
                {
                    // Process has exited finally.
                    break;
                }

                await Task.Delay(1000).ConfigureAwait(false);
            }
        }
    }
}