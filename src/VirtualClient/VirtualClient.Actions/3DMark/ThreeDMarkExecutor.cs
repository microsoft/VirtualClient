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
        }

        /// <summary>
        /// The benchmarks that will be run by 3dmark.
        /// </summary>
        public string[] Benchmarks
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ThreeDMarkExecutor.Benchmarks)).Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
        /// 3DMark enterprise lisence key
        /// </summary>
        public string LicenseKey
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(ThreeDMarkExecutor.LicenseKey));
            }
        }

        /// <summary>
        /// The path to the 3DMark executable.
        /// </summary>
        protected string ThreeDMarkExecutablePath { get; set; }

        /// <summary>
        /// The path to the PsExec.exe.
        /// </summary>
        protected string PsExecExecutablePath { get; set; }

        /// <summary>
        /// Defines the path to the PsExec package that contains the workload
        /// executable.
        /// </summary>
        protected DependencyPath PsExecPackage { get; set; }

        /// <summary>
        /// Defines the path to the 3DMark package that contains the workload
        /// executable.
        /// </summary>
        protected DependencyPath ThreeDMarkPackage { get; set; }

        /// <summary>
        /// The path for the 3dmark DLC files
        /// </summary>
        private string DLCPath { get; set; }

        /// <summary>
        /// Initializes the environment
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            PlatformSpecifics.ThrowIfNotSupported(this.Platform);

            await this.InitializePackageLocationAsync(cancellationToken)
                .ConfigureAwait(false);

            this.ThreeDMarkExecutablePath = this.PlatformSpecifics.Combine(this.ThreeDMarkPackage.Path, "3DMark", "3DMarkCmd.exe");
            this.PsExecExecutablePath = this.PlatformSpecifics.Combine(this.PsExecPackage.Path, "PsExec.exe");
            this.DLCPath = this.PlatformSpecifics.Combine(this.ThreeDMarkPackage.Path, "DLC", "3DMark");
        }

        /// <summary>
        /// Executes the 3DMark workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string executablePath = this.PsExecSession == -1 ? this.ThreeDMarkExecutablePath : this.PsExecExecutablePath;
            string workingDir = this.PsExecSession == -1 ? this.ThreeDMarkPackage.Path : this.PsExecPackage.Path;
            string baseArg = this.PsExecSession == -1 ? string.Empty : @$"-s -i {this.PsExecSession} -w {this.ThreeDMarkPackage.Path} -accepteula -nobanner {this.ThreeDMarkExecutablePath}";
            string commandArg, workloadCommandArg;
            EventContext relatedContext = telemetryContext.Clone().AddContext("executable", this.ThreeDMarkExecutablePath);

            // Point to DLC Path
            commandArg = $"{baseArg} --path={this.DLCPath}";
            EventContext dlcPathContext = relatedContext.AddContext("commandArguments", commandArg);
            using (IProcessProxy process = await this.ExecuteCommandAsync(executablePath, commandArg, workingDir, dlcPathContext, cancellationToken).ConfigureAwait(false))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext);
                    process.ThrowIfWorkloadFailed();
                }
            }

            // Register license key
            commandArg = $"{baseArg} --register={this.LicenseKey}";
            EventContext licenseKeyContext = relatedContext.AddContext("commandArguments", commandArg);
            using (IProcessProxy process = await this.ExecuteCommandAsync(executablePath, commandArg, workingDir, licenseKeyContext, cancellationToken).ConfigureAwait(false))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext);
                    process.ThrowIfWorkloadFailed();
                }
            }

            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string outFileName, resultfileName, definitionFileName;
                EventContext workloadContext, resultContext;

                // run 3dmark benchmarks
                foreach (string benchmark in this.Benchmarks)
                {
                    outFileName = $"{DateTimeOffset.Now.ToUnixTimeSeconds()}.out";
                    resultfileName = $"{DateTimeOffset.Now.ToUnixTimeSeconds()}.xml";

                    definitionFileName = $"custom_{benchmark.ToLower()}.3dmdef";

                    workloadCommandArg = $"{baseArg} --definition={definitionFileName} --out={outFileName} --systeminfo=off --systeminfomonitor=off --log=log.txt --trace";

                    workloadContext = telemetryContext.AddContext("commandArguments", commandArg);
                    IProcessProxy workloadProcess;
                    using (workloadProcess = await this.ExecuteCommandAsync(executablePath, commandArg, workingDir, workloadContext, cancellationToken).ConfigureAwait(false))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(workloadProcess, telemetryContext);
                            workloadProcess.ThrowIfWorkloadFailed();
                        }
                    }

                    // result preparation
                    commandArg = $"{baseArg} --in={outFileName} --export={resultfileName}";
                    resultContext = telemetryContext.AddContext("commandArguments", commandArg);
                    using (IProcessProxy process = await this.ExecuteCommandAsync(executablePath, commandArg, workingDir, resultContext, cancellationToken).ConfigureAwait(false))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext);
                            process.ThrowIfWorkloadFailed();
                            this.CaptureMetrics(workloadProcess, workloadCommandArg, workloadContext, benchmark, outFileName);
                        }
                    }

                }
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
                this.Logger.LogNotSupported("ThreeDMark", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        /// <summary>
        /// Processes benchmark results
        /// </summary>
        private void CaptureMetrics(IProcessProxy workloadProcess, string commandArguments, EventContext telemetryContext, string scenario, string outFileName)
        {
            if (workloadProcess.ExitCode == 0)
            {
                try
                {
                    string resultsFilePath = this.PlatformSpecifics.Combine(this.ThreeDMarkPackage.Path, "3DMark", outFileName);
                    string resultsContent = this.fileSystem.File.ReadAllText(resultsFilePath);
                    ThreeDMarkMetricsParser resultsParser = new ThreeDMarkMetricsParser(resultsContent, scenario);
                    IList<Metric> metrics = resultsParser.Parse();

                    this.MetadataContract.AddForScenario(
                        scenario,
                        workloadProcess.FullCommand(),
                        toolVersion: this.ThreeDMarkPackage.Version);

                    this.MetadataContract.Apply(telemetryContext);

                    this.Logger.LogMetrics(
                        this.PackageName,
                        scenario,
                        workloadProcess.StartTime,
                        workloadProcess.ExitTime,
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

                    this.Logger.LogMessage($"{nameof(ThreeDMarkExecutor)}.WorkloadOutputParsingFailed", LogLevel.Warning, relatedContext);
                }
            }
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

                DependencyPath psExecPackage = await this.systemManagement.PackageManager.GetPackageAsync(this.PsExecPackageName, CancellationToken.None).
                    ConfigureAwait(false) ?? throw new DependencyException(
                        $"The expected package '{this.PsExecPackageName}' does not exist on the system or is not registered.",
                        ErrorReason.WorkloadDependencyMissing);

                this.ThreeDMarkPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);
                this.PsExecPackage = this.PlatformSpecifics.ToPlatformSpecificPath(psExecPackage, this.Platform, this.CpuArchitecture);
            }
        }
    }
}