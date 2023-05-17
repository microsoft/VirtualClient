// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Platform;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// The SPECJbb workload executor.
    /// </summary>
    [WindowsCompatible]
    [UnixCompatible]
    public class SpecJbbExecutor : VirtualClientComponent
    {
        private const string GcLogName = "gc.log";
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManagement;

        /// <summary>
        /// The path to the SPECJbb package.
        /// </summary>
        private string packageDirectory;

        /// <summary>
        /// The path to the SPECJbb GcLog.
        /// </summary>
        private string gcLogPath;

        /// <summary>
        /// The path to the Java executable package.
        /// </summary>
        private string javaExecutableDirectory;

        /// <summary>
        /// Constructor for <see cref="SpecJbbExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SpecJbbExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
                return this.Parameters.GetValue<string>(nameof(SpecJbbExecutor.JdkPackageName));
            }
        }

        /// <summary>
        /// Java flags for running SPECjbb.
        /// </summary>
        public string JavaFlags
        {
            // For SPECjbb, Java teams recommends these as general flags.
            // -XX:+AlwaysPreTouch -XX:+UseLargePages -XX:+UseParallelGC
            get
            {
                return this.Parameters.GetValue<string>(nameof(SpecJbbExecutor.JavaFlags), "-XX:+AlwaysPreTouch -XX:+UseLargePages -XX:+UseParallelGC");
            }
        }

        /// <summary>
        /// Executes the SPECJbb workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string commandLineArguments = this.GetCommandLineArguments();

                using (IProcessProxy process = await this.ExecuteCommandAsync(this.javaExecutableDirectory, commandLineArguments, this.packageDirectory, telemetryContext, cancellationToken, runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        if (process.IsErrored())
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "SPECjbb", logToFile: true);
                            process.ThrowIfWorkloadFailed();
                        }

                        await this.CaptureMetricsAsync(process, commandLineArguments, telemetryContext, cancellationToken);
                    }
                }
            }

            // If the content blob store is not defined, the monitor does nothing and exits.
            if (this.TryGetContentStoreManager(out IBlobManager blobManager))
            {
                await this.UploadGcLogAsync(blobManager, this.gcLogPath, DateTime.UtcNow, cancellationToken);
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the SPECJbb workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // No initiation needed, just check if the packages and JDK is there.
            DependencyPath workloadPackage = await this.packageManager.GetPackageAsync(this.PackageName, CancellationToken.None);

            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.packageDirectory = workloadPackage.Path;

            DependencyPath javaExecutable = await this.packageManager.GetPackageAsync(this.JdkPackageName, CancellationToken.None);

            if (javaExecutable == null || !javaExecutable.Metadata.ContainsKey(PackageMetadata.ExecutablePath))
            {
                throw new DependencyException(
                    $"The expected Java executable does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.javaExecutableDirectory = javaExecutable.Metadata[PackageMetadata.ExecutablePath].ToString();
            this.gcLogPath = this.PlatformSpecifics.Combine(this.packageDirectory, SpecJbbExecutor.GcLogName);
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                // specjbb2015-C-20220301-00002-reporter.out
                string resultsDirectory = this.PlatformSpecifics.Combine(this.packageDirectory, "result");
                string[] outputFiles = this.fileSystem.Directory.GetFiles(resultsDirectory, "specjbb2015*-reporter.out", SearchOption.AllDirectories);

                foreach (string file in outputFiles)
                {
                    string results = await this.LoadResultsAsync(file, cancellationToken);
                    await this.LogProcessDetailsAsync(process, telemetryContext, "SPECjbb", results: results.AsArray(), logToFile: true);

                    try
                    {
                        SpecJbbMetricsParser parser = new SpecJbbMetricsParser(results);

                        this.Logger.LogMetrics(
                            toolName: "SPECjbb",
                            scenarioName: "SPECjbb",
                            process.StartTime,
                            process.ExitTime,
                            parser.Parse(),
                            null,
                            commandArguments,
                            this.Tags,
                            telemetryContext);

                        await this.fileSystem.File.DeleteAsync(file);
                    }
                    catch (Exception exc)
                    {
                        throw new WorkloadException($"Failed to parse file at '{file}' with text '{results}'.", exc, ErrorReason.InvalidResults);
                    }
                }
            }
        }

        private async Task ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(SpecJbbExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        this.LogProcessTrace(process);
                        await process.StartAndWaitAsync(cancellationToken);

                        await this.ValidateProcessExitedAsync(process.Id, TimeSpan.FromMinutes(10), cancellationToken);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "SPECjbb");
                            process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                });
            }
        }

        private string GetCommandLineArguments()
        {
            string options = this.CalculateJavaOptions();

            // Looks like this: java -jar specjbb2015.jar -ikv
            return @$"{options} -jar specjbb2015.jar -m composite -ikv";
        }

        private string CalculateJavaOptions()
        {
            MemoryInfo memoryInfo = this.systemManagement.GetMemoryInfoAsync(CancellationToken.None)
                .GetAwaiter().GetResult();

            long totalMemoryKiloBytes = memoryInfo.TotalMemory;
            int jbbMemoryInMegaBytes = Convert.ToInt32(totalMemoryKiloBytes * 0.85 / 1024);
            int coreCount = Environment.ProcessorCount;

            // -Xms size in bytes Sets the initial size of the Java heap. The default size is 2097152(2MB).
            // -Xmx size in bytes Sets the maximum size to which the Java heap can grow. The default size is 64M.
            // -Xmn size in bytes Sets the initial Java heap size for the Eden generation. The default value is 640K.
            string gcLogFlags = $"-Xlog:gc*,gc+ref=debug,gc+phases=debug,gc+age=trace,safepoint:file={SpecJbbExecutor.GcLogName}";
            string dynamicFlags = string.Empty;

            // According to the MSFT Java Engineering Group (JEG) team. The memory here should be generally set to 85% of the system memory.
            // However, user will have the option to overwrite the GC threads and memory used.
            if (!this.JavaFlags.Contains("-XX:ParallelGCThreads=", StringComparison.CurrentCultureIgnoreCase))
            {
                dynamicFlags += $"-XX:ParallelGCThreads={coreCount} ";
            }

            if (!this.JavaFlags.Contains("-Xms", StringComparison.CurrentCultureIgnoreCase))
            {
                dynamicFlags += $"-Xms{jbbMemoryInMegaBytes}m ";
            }

            if (!this.JavaFlags.Contains("-Xmx", StringComparison.CurrentCultureIgnoreCase))
            {
                dynamicFlags += $"-Xmx{jbbMemoryInMegaBytes}m ";
            }

            string jbbArgument = $"{this.JavaFlags} {dynamicFlags.Trim()} {gcLogFlags}";
            return jbbArgument;
        }

        private Task UploadGcLogAsync(IBlobManager blobManager, string gcLogPath, DateTime logTime, CancellationToken cancellationToken)
        {
            // Example Blob Store Structure:
            // /7dfae74c-06c0-49fc-ade6-987534bb5169/anyagentid/specjbb/2022-04-30T20:13:23.3768938Z-gc.log
            FileUploadDescriptor descriptor = this.CreateFileUploadDescriptor(
                this.fileSystem.FileInfo.FromFileName(gcLogPath),
                HttpContentType.PlainText,
                Encoding.UTF8.WebName,
                "specjbb",
                DateTime.UtcNow);

            return this.UploadFileAsync(blobManager, this.fileSystem, descriptor, cancellationToken, deleteFile: true);

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

                await Task.Delay(1000);
            }
        }
    }
}