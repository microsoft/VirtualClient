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
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The Furmark workload executor.
    /// </summary>
    [WindowsCompatible]
    public class FurmarkExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="FurmarkExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public FurmarkExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <summary>
        /// The command line to use when running the FurMark workload.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CommandLine));
            }
        }

        /// <summary>
        /// Parameter defines the duration of time in which to run the FurMark workload
        /// scenario/action.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.Duration));
            }
        }

        /// <summary>
        /// Parameter defines the name of the package that contains the PsExec executable/application.
        /// </summary>
        public string PsExecPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(FurmarkExecutor.PsExecPackageName));
            }
        }

        /// <summary>
        /// Parameter defines the session ID to use for running FurMark via the PsExec executable/application.
        /// Default = 1.
        /// </summary>
        public int SessionId
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.SessionId), 1);
            }
        }

        /// <summary>
        /// The package containing the FurMark toolsets.
        /// </summary>
        protected DependencyPath FurMarkPackage { get; private set; }

        /// <summary>
        /// path to FurMark executable.
        /// </summary>
        protected string ExecutablePath { get; set; }

        /// <summary>
        /// The package containing the PsExec toolsets.
        /// </summary>
        protected DependencyPath PsExecPackage { get; private set; }

        /// <summary>
        /// path to scorefile.
        /// </summary>
        protected string ResultsFilePath { get; set; }

        /// <summary>
        /// path to FurmarkMonitor.xml.
        /// </summary>
        protected string ResultsXMLFilePath { get; set; }

        /// <summary>
        /// path to PSexec.exe .
        /// </summary>
        protected string PSexecExecutablePath { get; set; }

        /// <summary>
        /// Executes cleanup operations. Because FurMark can run in a separate session (i.e. via PSExec), we need to 
        /// be explicit about ensuring the process is stopped before exiting.
        /// </summary>
        protected override async Task CleanupAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.CleanupAsync(telemetryContext, cancellationToken);
            ProcessManager processManager = this.Dependencies.GetService<ProcessManager>();

            string processName = "FurMark";
            IEnumerable<IProcessProxy> runningProcesses = processManager.GetProcesses(Path.GetFileNameWithoutExtension(processName));

            if (runningProcesses?.Any() == true)
            {
                foreach (IProcessProxy processProxy in runningProcesses)
                {
                    processProxy.SafeKill();
                }
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the FurMark workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.FurMarkPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            this.PsExecPackage = await this.GetPlatformSpecificPackageAsync(this.PsExecPackageName, cancellationToken);

            this.ExecutablePath = this.Combine(this.FurMarkPackage.Path, "Geeks3D", "Benchmarks", "FurMark", "FurMark.exe");
            this.ResultsFilePath = this.Combine(this.FurMarkPackage.Path, "FurMark-Scores.txt");
            this.ResultsXMLFilePath = this.Combine(this.FurMarkPackage.Path, "Geeks3D", "Benchmarks", "FurMark", "furmark-gpu-monitoring.xml");
            this.PSexecExecutablePath = this.Combine(this.PsExecPackage.Path, "PsExec.exe");
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            return base.IsSupported() 
                && this.Platform == PlatformID.Win32NT 
                && this.CpuArchitecture == Architecture.X64;
        }

        /// <summary>
        /// Executes the FURMARK workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                if (this.fileSystem.File.Exists(this.ResultsFilePath))
                {
                    this.fileSystem.File.Delete(this.ResultsFilePath);
                }

                if (this.fileSystem.File.Exists(this.ResultsXMLFilePath))
                {
                    this.fileSystem.File.Delete(this.ResultsXMLFilePath);
                }

                await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken);
            }
        }

        private async Task ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // The first part of the command line arguments here is the PsExec options. The FurMark command
            // is included at the end.
            string commandArguments = $"-accepteula -s -i {this.SessionId} -w {this.FurMarkPackage.Path} {this.ExecutablePath} {this.CommandLine}";

            using (IProcessProxy process = await this.ExecuteCommandAsync(this.PSexecExecutablePath, commandArguments, this.FurMarkPackage.Path, telemetryContext, cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    string[] furmarkResults = null;

                    try
                    {
                        process.ThrowIfWorkloadFailed();

                        // The PsExec process unconventionally emits standard output to standard error in
                        // this scenario.
                        if (process.StandardError.Length > 0)
                        {
                            if (!this.fileSystem.File.Exists(this.ResultsFilePath))
                            {
                                throw new WorkloadResultsException(
                                    $"The expected FurMark results file was not found at path '{this.ResultsFilePath}'.",
                                    ErrorReason.WorkloadResultsNotFound);
                            }

                            if (!this.fileSystem.File.Exists(this.ResultsXMLFilePath))
                            {
                                throw new WorkloadResultsException(
                                    $"The expected FurMark results XML file was not found at path '{this.ResultsXMLFilePath}'.",
                                    ErrorReason.WorkloadResultsNotFound);
                            }

                            string results = await this.LoadResultsAsync(this.ResultsFilePath, cancellationToken);
                            string xmlResults = await this.LoadResultsAsync(this.ResultsXMLFilePath, cancellationToken);
                            furmarkResults = new string[] { results, xmlResults };

                            this.CaptureMetrics(process, results, xmlResults, telemetryContext);
                        }
                    }
                    finally
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "FurMark", furmarkResults, logToFile: true);
                    }
                }
            }
        }

        private void CaptureMetrics(IProcessProxy process, string results, string xmlResults, EventContext telemetryContext)
        {
            this.MetadataContract.AddForScenario(
                  "FurMark",
                  process.FullCommand(),
                  toolVersion: this.FurMarkPackage.Version);

            this.MetadataContract.Apply(telemetryContext);

            FurmarkMetricsParser resultsParser = new FurmarkMetricsParser(results);
            IList<Metric> metrics1 = resultsParser.Parse();

            this.Logger.LogMetrics(
                "FurMark",
                this.MetricScenario ?? this.Scenario,
                process.StartTime,
                process.ExitTime,
                metrics1,
                null,
                process.FullCommand(),
                this.Tags,
                telemetryContext);

            FurmarkXmlMetricsParser xmlResultsParser = new FurmarkXmlMetricsParser(xmlResults);
            IList<Metric> metrics2 = xmlResultsParser.Parse();

            this.Logger.LogMetrics(
                "FurMark",
                this.MetricScenario ?? this.Scenario,
                process.StartTime,
                process.ExitTime,
                metrics2,
                null,
                process.FullCommand(),
                this.Tags,
                telemetryContext);
        }
    }
}