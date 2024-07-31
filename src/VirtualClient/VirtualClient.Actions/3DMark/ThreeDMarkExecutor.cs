// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
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
    /// Executes the 3DMark workload.
    /// </summary>
    [WindowsCompatible]
    public class ThreeDMarkExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;
        private string psexecDir;

        /// <summary>
        /// ConstructorD
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public ThreeDMarkExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.OutFileName = $"{DateTimeOffset.Now.ToUnixTimeSeconds()}.out";
            this.Definitions = new List<string>();
            if (this.Scenario == "TimeSpy")
            {
                this.Definitions.Add("custom_TSGT1.3dmdef");
                this.Definitions.Add("custom_TSGT2.3dmdef");
                this.Definitions.Add("custom_TSCT.3dmdef");
            }
        }

        /// <summary>
        /// 3DMark enterprise lisence key
        /// </summary>
        public string LisenceKey
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ThreeDMarkExecutor.LisenceKey));
            }
        }

        /// <summary>
        /// PsExec session number
        /// </summary>
        public int PsExecSession
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(ThreeDMarkExecutor.PsExecSession));
            }
        }

        /// <summary>
        /// The 3D Mark defintion file to execute.
        /// </summary>
        public List<string> Definitions { get; set; }

        /// <summary>
        /// The path for the intermediate results file.
        /// </summary>
        public string OutFileName { get; set; }

        /// <summary>
        /// The path to the 3DMark executable.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// The path to the 3DMark executable.
        /// </summary>
        public string DLCPath { get; set; }

        /// <summary>
        /// Defines the name of the package associated with the component.
        /// </summary>
        public string PsExecPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ThreeDMarkExecutor.PsExecPackageName));
            }
        }

        /// <summary>
        /// Defines the path to the 3DMark package that contains the workload
        /// executable.
        /// </summary>
        protected DependencyPath Package { get; set; }

        /// <summary>
        /// Executes the 3DMark workload.
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

            this.ExecutablePath = this.PlatformSpecifics.Combine(this.Package.Path, "3DMark", "3DMarkCmd.exe");
            this.DLCPath = this.PlatformSpecifics.Combine(this.Package.Path, "DLC", "3DMark");
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
                this.Logger.LogNotSupported("ThreeDMark", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        /// <summary>
        /// Processes benchmark results
        /// </summary>
        private IList<Metric> CaptureResults(IProcessProxy workloadProcess, string commandArguments, string definition, EventContext telemetryContext)
        {
            if (workloadProcess.ExitCode == 0)
            {
                try
                {
                    string resultsFilePath = this.PlatformSpecifics.Combine(this.Package.Path, "3DMark", "result.xml");
                    string resultsContent = this.fileSystem.File.ReadAllText(resultsFilePath);
                    ThreeDMarkMetricsParser resultsParser = new ThreeDMarkMetricsParser(resultsContent, definition);
                    return resultsParser.Parse();

                }
                catch (SchemaException exc)
                {
                    EventContext relatedContext = telemetryContext.Clone()
                        .AddError(exc);

                    this.Logger.LogMessage($"{nameof(ThreeDMarkExecutor)}.WorkloadOutputParsingFailed", LogLevel.Warning, relatedContext);
                    return new List<Metric>();
                }
            }
            else
            {
                return new List<Metric>();
            }
        }

        /// <summary>
        /// Run the 3DMark Definitions
        /// </summary>
        private Task ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IList<Metric> metrics = new List<Metric>();

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("executable", this.ExecutablePath);

            string psexec = this.PlatformSpecifics.Combine(this.psexecDir, "PsExec.exe");
            string baseArg = @$"-s -i {this.PsExecSession} -w {this.psexecDir} -accepteula -nobanner";

            return this.Logger.LogMessageAsync($"{nameof(ThreeDMarkExecutor)}.ExecuteWorkload", relatedContext, async () =>
            {
                // Point 3DMark to DLC Path
                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(psexec, $"{baseArg} {this.ExecutablePath} --path={this.DLCPath}", this.psexecDir))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());

                    try
                    {
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogInformation("Registering 3DMark License");
                            await this.LogProcessDetailsAsync(process, telemetryContext);
                            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);

                        }
                    }
                    finally
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                }

                // Lisence Registry
                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(psexec, $"{baseArg} {this.ExecutablePath} --register={this.LisenceKey}", this.psexecDir))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());

                    try
                    {
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogInformation("Initializing 3DMark DLC");
                            await this.LogProcessDetailsAsync(process, telemetryContext);
                            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);

                        }
                    }
                    finally
                    {
                        if (!process.HasExited)
                        {
                            process.Kill();
                        }
                    }
                }

                DateTime startTime = DateTime.UtcNow;
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    // Run Workload
                    foreach (string definition in this.Definitions)
                    {
                        this.OutFileName = $"{DateTimeOffset.Now.ToUnixTimeSeconds()}.out";

                        // Workload execution
                        string arguments = this.GenerateCommandArguments(definition);
                        string commandArguments = $"{baseArg} {this.ExecutablePath} {arguments}";

                        using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(psexec, commandArguments, this.psexecDir))
                        {
                            this.CleanupTasks.Add(() => process.SafeKill());

                            try
                            {
                                await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    await this.LogProcessDetailsAsync(process, telemetryContext);
                                    process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);

                                }
                            }
                            finally
                            {
                                if (!process.HasExited)
                                {
                                    process.Kill();
                                }
                            }
                        }

                        // Result Preparation
                        string commandArguments2 = $"{baseArg} {this.ExecutablePath} --in={this.OutFileName} --export=result.xml";
                        using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(psexec, commandArguments2, this.psexecDir))
                        {
                            this.CleanupTasks.Add(() => process.SafeKill());

                            try
                            {
                                await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    await this.LogProcessDetailsAsync(process, telemetryContext);
                                    process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                                    foreach (Metric metric in this.CaptureResults(process, commandArguments, definition, telemetryContext))
                                    {
                                        metrics.Add(metric);
                                    }
                                }
                            }
                            finally
                            {
                                if (!process.HasExited)
                                {
                                    process.Kill();
                                }
                            }
                        }
                    }
                }

                DateTime endTime = DateTime.UtcNow;

                foreach (Metric metric in this.CalculateTimeSpyAggregates(metrics))
                {
                    metrics.Add(metric);
                }

                this.MetadataContract.AddForScenario(
                    "3DMark",
                    null,
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                this.Logger.LogMetrics(
                    "3DMark",
                    this.Scenario,
                    startTime,
                    endTime,
                    metrics,
                    null,
                    string.Empty,
                    this.Tags,
                    telemetryContext);
            });
        }

        /// <summary>
        /// Calculates the 3DMark TimeSpy aggregate scores
        /// </summary>
        private IList<Metric> CalculateTimeSpyAggregates(IList<Metric> metrics)
        {
            IList<Metric> aggregates = new List<Metric>();
            double? tsgt1 = 0;
            double? tsgt2 = 0;
            double? tsct = 0;
            foreach (Metric metric in metrics)
            {
                if (metric.Name == "timespy.graphics.1")
                {
                    tsgt1 = metric.Value;
                }
                else if (metric.Name == "timespy.graphics.2")
                {
                    tsgt2 = metric.Value;
                }
                else if (metric.Name == "timespy.cpu")
                {
                    tsct = metric.Value;
                }
            }

            // Weighted Harmonic Mean of Individual Scores
            if (tsgt1 != 0 && tsgt2 != 0 && tsct != 0)
            {
                double? graphicsScore = 165 * (2 / ((1 / tsgt1) + (1 / tsgt2)));
                double? cpuScore = 298 * tsct;
                double? aggScore = 1 / ((0.85 / graphicsScore) + (0.15 / cpuScore));
                aggregates.Add(new Metric("timespy.graphics.agg", graphicsScore, "score", MetricRelativity.HigherIsBetter));
                aggregates.Add(new Metric("timespy.cpu.agg", cpuScore, "score", MetricRelativity.HigherIsBetter));
                aggregates.Add(new Metric("timespy.finalscore", aggScore, "score", MetricRelativity.HigherIsBetter));
            }

            return aggregates;
        }

        /// <summary>
        /// Generate the 3DMark Command Arguments
        /// </summary>
        private string GenerateCommandArguments(string definition)
        {
            return $"--definition={definition} --out={this.OutFileName}";
        }

        /// <summary>
        /// Validate the 3DMark Package
        /// </summary>
        private async Task InitializePackageLocationAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                DependencyPath workloadPackage = await this.systemManagement.PackageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                    .ConfigureAwait(false);

                DependencyPath psExecPackage = await this.systemManagement.PackageManager.GetPackageAsync(this.PsExecPackageName, CancellationToken.None)
                    .ConfigureAwait(false);

                if (workloadPackage == null)
                {
                    throw new DependencyException(
                        $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                        ErrorReason.WorkloadDependencyMissing);
                }

                if (psExecPackage == null)
                {
                    throw new DependencyException(
                        $"The expected package '{this.psexecDir}' does not exist on the system or is not registered.",
                        ErrorReason.WorkloadDependencyMissing);
                }

                workloadPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);
                psExecPackage = this.PlatformSpecifics.ToPlatformSpecificPath(psExecPackage, this.Platform, this.CpuArchitecture);
                this.Package = workloadPackage;
                this.psexecDir = psExecPackage.Path;
            }
        }
    }
}