using System.IO;

namespace VirtualClient.Actions.Wrathmark
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.DependencyInjection;

    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Class for the Wrathmark workload.
    /// </summary>
    public class WrathmarkWorkloadExecutor : VirtualClientComponent
    {
        private readonly ISystemManagement systemManagement;
        private readonly ProcessManager processManager;
        private readonly IPackageManager packageManager;

        private string benchmarkDirectory;
        private string dotnetExePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="WrathmarkWorkloadExecutor"/> class.
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="parameters"></param>
        public WrathmarkWorkloadExecutor(
            IServiceCollection dependencies,
            IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            // Follows the example, but is wrong. The services should be injected to this class, not resolved here
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.processManager = this.systemManagement.ProcessManager;
            this.packageManager = this.systemManagement.PackageManager;
        }

        /// <summary>
        /// The name of the package where the DotNetSDK package is downloaded.
        /// </summary>
        public string DotNetSdkPackageName => this.Parameters.GetValue<string>(nameof(WrathmarkWorkloadExecutor.DotNetSdkPackageName), "dotnetsdk");

        /// <summary>
        /// The target version of the .NET Framework
        /// </summary>
        public string TargetFramework =>
            // Lower case to prevent build path issue.
            this.Parameters.GetValue<string>(nameof(WrathmarkWorkloadExecutor.TargetFramework), "net7.0").ToLower();

        /// <summary>
        /// The arguments to pass to the wrathmark program
        /// </summary>
        public string WrathmarkArgs => this.Parameters.GetValue<string>(nameof(WrathmarkWorkloadExecutor.WrathmarkArgs), string.Empty);

        /// <summary>
        /// The name of the subfolder in the Git repo to use for the benchmark.
        /// </summary>
        public string Subfolder => this.Parameters.GetValue<string>(nameof(WrathmarkWorkloadExecutor.Subfolder), "wrath-sharp");

        /// <summary>
        /// Name of the tool.
        /// </summary>
        protected string ToolName => "Wrathmark";

        /// <inheritdoc />
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.packageManager.GetPackageAsync(
                    this.PackageName,
                    cancellationToken);

            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            DependencyPath dotnetSdkPackage = await this.packageManager.GetPackageAsync(
                    this.DotNetSdkPackageName,
                    cancellationToken)
                .ConfigureAwait(false);

            if (dotnetSdkPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.DotNetSdkPackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.dotnetExePath = this.Combine(dotnetSdkPackage.Path, this.Platform == PlatformID.Unix ? "dotnet" : "dotnet.exe");

            this.benchmarkDirectory = this.Combine(workloadPackage.Path, this.Subfolder);

            if (!Directory.Exists(this.benchmarkDirectory))
            {
                throw new DependencyException(
                    $"The expected benchmark directory '{this.benchmarkDirectory}' does not exist.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            string benchmarkProject = this.Combine(this.benchmarkDirectory, $"{this.Subfolder}.csproj");

            // Build the wrath sharp project
            // To make native libraries that can be used, enumerate the SupportedPlatforms metadata and call publish for each
            // Outputs
            //    bin/Release/net6.0/linux-x64/publish/wrath-sharp
            //    bin/Release/net6.0/win-x64/publish/wrath-sharp.exe
            string publishArgument = $"publish {benchmarkProject} -c Release -r {this.PlatformArchitectureName} -f {this.TargetFramework} /p:UseSharedCompilation=false /p:BuildInParallel=false /m:1 /p:Deterministic=true /p:Optimize=true";
            await this.ExecuteCommandAsync(
                    this.dotnetExePath,
                    publishArgument,
                    this.benchmarkDirectory,
                    true,
                    false,
                    true,
                    true,
                    telemetryContext,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the Wrathmark component logic.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger.LogTraceMessage(
                $"{nameof(WrathmarkWorkloadExecutor)}.Starting",
                telemetryContext);

            // Example: ./bin/Release/net6.0/linux-x64/publish
            string outputDirectory = Path.Combine(
                this.benchmarkDirectory,
                "bin",
                "Release",
                this.TargetFramework,
                this.PlatformArchitectureName,
                "publish");

            string programName = this.Platform == PlatformID.Unix ? this.Subfolder : $"{this.Subfolder}.exe";

            string benchmarkPath = Path.Combine(outputDirectory, programName);

            if (!File.Exists(benchmarkPath))
            {
                throw new DependencyException(
                    $"The expected benchmark executable '{benchmarkPath}' does not exist.",
                    ErrorReason.DependencyNotFound);
            }

            DateTime startTime = DateTime.UtcNow;
            string results = string.Empty;
            try
            {
                results = await this.ExecuteCommandAsync(
                    benchmarkPath,
                    this.WrathmarkArgs,
                    this.benchmarkDirectory,
                    false,
                    true,
                    true,
                    true,
                    telemetryContext,
                    cancellationToken);
            }
            finally
            {
                DateTime endTime = DateTime.UtcNow;
                await this.CaptureMetricsAsync(results, startTime, endTime, telemetryContext, cancellationToken);
            }
        }

        private Task<string> ExecuteCommandAsync(
            string command,
            string arguments,
            string workingDirectory,
            bool runElevated,
            bool enableProfiling,
            bool requireStandardOut,
            bool failureIsCritical,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            this.Logger.LogTraceMessage(
                $"Executing process '{command}' '{arguments}' at directory '{workingDirectory}'.");

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("packageName", this.PackageName)
                .AddContext(nameof(workingDirectory), workingDirectory)
                .AddContext(nameof(command), command)
                .AddContext(nameof(arguments), arguments)
                .AddContext(nameof(enableProfiling), enableProfiling)
                .AddContext(nameof(requireStandardOut), requireStandardOut)
                .AddContext(nameof(failureIsCritical), failureIsCritical)
                ;

            return this.Logger.LogMessageAsync($"{nameof(WrathmarkWorkloadExecutor)}.ExecuteWorkload", relatedContext, async () =>
            {
                BackgroundOperations profiler = null;
                if (enableProfiling)
                {
                    profiler = BackgroundOperations.BeginProfiling(this, cancellationToken);
                }

                try
                {
                    // We create a operating system process to host the executing workload, start it and
                    // wait for it to exit.
                    IProcessProxy process = runElevated
                        ? this.processManager.CreateElevatedProcess(this.Platform, command, arguments, workingDirectory)
                        : this.processManager.CreateProcess(command, arguments, workingDirectory);

                    try
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());

                        await process.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            // ALWAYS log the details for the process. This helper method will ensure we capture the exit code, standard output, standard
                            // error etc... This is very helpful for triage/debugging.
                            await this.LogProcessDetailsAsync(
                                    process,
                                    telemetryContext,
                                    this.ToolName,
                                    logToFile: true)
                                .ConfigureAwait(false);

                            // If the workload process returned a non-success exit code, we throw an exception
                            ErrorReason errorReason = failureIsCritical
                                                        ? ErrorReason.CriticalWorkloadFailure
                                                        : ErrorReason.WorkloadFailed;

                            process.ThrowIfErrored<WorkloadException>(
                                ProcessProxy.DefaultSuccessCodes,
                                errorReason: errorReason);

                            if (requireStandardOut && process.StandardOutput.Length == 0)
                            {
                                throw new WorkloadException(
                                    "The command did not produce any results to standard output.",
                                    errorReason);
                            }
                        }

                        return process.StandardOutput.ToString();
                    }
                    finally
                    {
                        process?.Dispose();
                    }
                }
                finally
                {
                    profiler?.Dispose();
                }
            });
        }

        private Task CaptureMetricsAsync(
            string results,
            DateTime start,
            DateTime end,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                results.ThrowIfNullOrWhiteSpace(nameof(results));

                this.Logger.LogMessage(
                    $"{nameof(WrathmarkWorkloadExecutor)}.CaptureMetrics",
                    telemetryContext.Clone().AddContext("results", results));

                // TODO: Seems weird to new this up here. Shouldn't this be injected?
                var resultsParser = new WrathmarkMetricsParser(results);
                var metrics = resultsParser.Parse();

                this.Logger.LogMetrics(
                    toolName: this.ToolName,
                    scenarioName: this.Scenario,
                    scenarioStartTime: start,
                    scenarioEndTime: end,
                    metrics: metrics,
                    metricCategorization: null,
                    scenarioArguments: null,
                    this.Tags,
                    telemetryContext);
            }

            return Task.CompletedTask;
        }
    }
}
