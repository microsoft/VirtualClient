// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Apache http server benchmarking workload executor
    /// </summary>
    public class ApacheBenchExecutor : VirtualClientComponent
    {
        private IPackageManager packageManager;
        private IFileSystem fileSystem;
        private ProcessManager processManager;
        private ISystemManagement systemManagement;
        private IStateManager stateManager;

        /// <summary>
        /// The ApacheBench workload executor
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ApacheBenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.packageManager = dependencies.GetService<IPackageManager>();
            this.processManager = this.systemManagement.ProcessManager;
            this.SystemManagement = dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManagement.StateManager;
        }

        /// <summary>
        /// Parameter defines the command line arguments to pass to the workload executable.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return $"-k -n {this.NoOfRequests} -c {this.NoOfConcurrentRequests} http://localhost:80/";
            }
        }

        /// <summary>
        /// Allows overwrite to ApacheBench param for number of requests. 
        /// </summary>
        public int NoOfRequests
        {
            get
            {
                int noOfRequests = 50000;

                if (this.Parameters.TryGetValue(nameof(this.NoOfRequests), out IConvertible value) && value != null)
                {
                    noOfRequests = value.ToInt32(CultureInfo.InvariantCulture);
                }

                return noOfRequests;
            }
        }

        /// <summary>
        /// Allows overwrite to ApacheBench param for number of requests. 
        /// </summary>
        public int NoOfConcurrentRequests
        {
            get
            {
                int noOfConcurrentRequests = 50;

                if (this.Parameters.TryGetValue(nameof(this.NoOfConcurrentRequests), out IConvertible value) && value != null)
                {
                    noOfConcurrentRequests = value.ToInt32(CultureInfo.InvariantCulture);
                }

                return noOfConcurrentRequests;
            }
        }

        /// <summary>
        /// Provides methods for managing system requirements.
        /// </summary>
        protected ISystemManagement SystemManagement { get; }

        /// <summary>
        /// It is common to use local member variables or properties to keep track of the names of 
        /// workload binaries/executables. Depending upon the OS platform (Linux vs. Windows) we are on
        /// the names of the binaries might be different.
        /// </summary>
        protected string WorkloadExecutablePath { get; set; }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = base.IsSupported()
                &&
                ((this.Platform == PlatformID.Win32NT && (this.CpuArchitecture == Architecture.X64 || this.CpuArchitecture == Architecture.Arm64))
                || (this.Platform == PlatformID.Unix && (this.CpuArchitecture == Architecture.X64 || this.CpuArchitecture == Architecture.Arm64)));

            if (!isSupported)
            {
                this.Logger.LogNotSupported("ApacheBench", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(this.WorkloadExecutablePath, this.CommandLine, string.Empty, telemetryContext, cancellationToken, runElevated: true))
                {
                    if (process.StandardError.Length > 0)
                    {
                        process.ThrowOnStandardError<WorkloadException>(
                            errorReason: ErrorReason.WorkloadFailed);
                    }

                    string workloadResults = process.StandardOutput.ToString();

                    await this.CaptureMetricsAsync(process, workloadResults, this.CommandLine, telemetryContext, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex)
            {
                telemetryContext.AddError(ex);
                this.Logger.LogErrorMessage(ex, telemetryContext, LogLevel.Warning);
            }
        }

        /// <summary>
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            ApacheBenchState state = await this.stateManager.GetStateAsync<ApacheBenchState>($"{nameof(ApacheBenchState)}", cancellationToken)
                ?? new ApacheBenchState();

            if (this.Platform == PlatformID.Win32NT)
            {
                DependencyPath workloadPackagePath = await this.packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, CancellationToken.None)
                    .ConfigureAwait(false);

                string apache24Directory = this.PlatformSpecifics.Combine(workloadPackagePath.Path, "Apache24");
                string httpdConfFilePath = this.PlatformSpecifics.Combine(apache24Directory, "conf", "httpd.conf");

                // Replacing the default path to the directory path.
                await this.fileSystem.File.ReplaceInFileAsync(
                   httpdConfFilePath, "Define SRVROOT \"c:\\/Apache24\"", $"Define SRVROOT \"{apache24Directory}\"", cancellationToken);

                string binPath = this.PlatformSpecifics.Combine(apache24Directory, "bin");
                string httpdExecutablePath = this.PlatformSpecifics.Combine(binPath, "httpd.exe");

                if (!state.ApacheBenchStateInitialized)
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(httpdExecutablePath, "-k install", binPath, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "ApacheBench");
                            process.ThrowIfWorkloadFailed();
                        }
                    }
                }

                this.WorkloadExecutablePath = this.PlatformSpecifics.Combine(binPath, "ab.exe");
            }
            else if (this.Platform == PlatformID.Unix)
            {
                var executionCommands = new List<string>
                {
                    "ufw allow 'Apache'",
                    "systemctl start apache2"
                };

                if (!state.ApacheBenchStateInitialized)
                {
                    foreach (var command in executionCommands)
                    {
                        using (IProcessProxy process = await this.ExecuteCommandAsync(command, "/usr/sbin/", telemetryContext, cancellationToken, runElevated: true))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                process.ThrowIfWorkloadFailed();
                                await this.LogProcessDetailsAsync(process, telemetryContext, "ApacheBench");
                            }
                        }
                    }
                }

                this.WorkloadExecutablePath = "/usr/bin/ab";
            }

            state.ApacheBenchStateInitialized = true;
            await this.stateManager.SaveStateAsync<ApacheBenchState>($"{nameof(ApacheBenchState)}", state, cancellationToken);
        }

        private Task CaptureMetricsAsync(IProcessProxy process, string results, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            process.ThrowIfNull(nameof(process));
            results.ThrowIfNullOrWhiteSpace(nameof(results));

            this.MetadataContract.AddForScenario(
                "ApacheBench",
                commandArguments,
                null);

            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogMessage($"{nameof(ApacheBenchExecutor)}.CaptureMetrics", telemetryContext.Clone()
                    .AddContext("results", results));

                var resultsParser = new ApacheBenchMetricsParser(results);
                IList<Metric> workloadMetrics = resultsParser.Parse();

                this.Logger.LogMetrics(
                    toolName: nameof(ApacheBenchExecutor),
                    scenarioName: this.Scenario,
                    scenarioStartTime: process.StartTime,
                    scenarioEndTime: process.ExitTime,
                    metrics: workloadMetrics,
                    metricCategorization: null,
                    scenarioArguments: commandArguments,
                    this.Tags,
                    telemetryContext);
            }

            return Task.CompletedTask;
        }

        internal class ApacheBenchState : State
        {
            public ApacheBenchState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool ApacheBenchStateInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(ApacheBenchState.ApacheBenchStateInitialized), false);
                }

                set
                {
                    this.Properties[nameof(ApacheBenchState.ApacheBenchStateInitialized)] = value;
                }
            }
        }
    }
}
