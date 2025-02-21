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
    /// The Gzip workload executor.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class GzipExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManager;

        /// <summary>
        /// Constructor for <see cref="GzipExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public GzipExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManager.PackageManager;
            this.stateManager = this.systemManager.StateManager;
            this.fileSystem = this.systemManager.FileSystem;
        }

        /// <summary>
        /// The options passed to Gzip.
        /// </summary>
        public string Options
        {
            get
            {
                this.Parameters.TryGetValue(nameof(GzipExecutor.Options), out IConvertible options);
                return options?.ToString();
            }
        }

        /// <summary>
        /// Gzip space separated input files or directories
        /// </summary>
        public string InputFilesOrDirs
        {
            get
            {
                this.Parameters.TryGetValue(nameof(GzipExecutor.InputFilesOrDirs), out IConvertible inputFilesOrDirs);
                return inputFilesOrDirs?.ToString();
            }
        }

        /// <summary>
        /// The name of the directory where the Gzip is executed.
        /// </summary>
        protected string GzipDirectory { get; set; }

        /// <summary>
        /// Executes the Gzip workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string commandLineArguments = this.GetCommandLineArguments();

                // Execute Gzip
                using (IProcessProxy process = await this.ExecuteCommandAsync("bash", $"-c \"gzip {commandLineArguments}\"", this.GzipDirectory, telemetryContext, cancellationToken, runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "GZip", logToFile: true);

                        process.ThrowIfWorkloadFailed();
                        this.CaptureMetrics(process, commandLineArguments, telemetryContext);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the Gzip workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.CheckDistroSupportAsync(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            this.GzipDirectory = this.GetPackagePath(this.PackageName);

            GzipState state = await this.stateManager.GetStateAsync<GzipState>($"{nameof(GzipState)}", cancellationToken)
                ?? new GzipState();

            if (!state.GzipStateInitialized)
            {
                if (!this.fileSystem.Directory.Exists(this.GzipDirectory))
                {
                    this.fileSystem.Directory.CreateDirectory(this.GzipDirectory);
                }

                // Choose default file for compression and decompression if files/dirs are not provided.
                if (string.IsNullOrWhiteSpace(this.InputFilesOrDirs))
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        "wget",
                        $"https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip",
                        this.GzipDirectory,
                        telemetryContext,
                        cancellationToken,
                        runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                        }
                    }

                    using (IProcessProxy process = await this.ExecuteCommandAsync(
                        "unzip",
                        "silesia.zip -d silesia",
                        this.GzipDirectory,
                        telemetryContext,
                        cancellationToken,
                        runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                        }
                    }
                }

                state.GzipStateInitialized = true;
            }

            await this.stateManager.SaveStateAsync<GzipState>($"{nameof(GzipState)}", state, cancellationToken);
        }

        private void CaptureMetrics(IProcessProxy process, string commandArguments, EventContext telemetryContext)
        {
            this.MetadataContract.AddForScenario(
                "Gzip",
                commandArguments,
                toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            // Gzip workload produces metrics in standard error
            GzipMetricsParser parser = new GzipMetricsParser(process.StandardError.ToString());
            IList<Metric> metrics = parser.Parse();

            this.Logger.LogMetrics(
                "Gzip",
                this.Scenario,
                process.StartTime,
                process.ExitTime,
                metrics,
                null,
                commandArguments,
                this.Tags,
                telemetryContext);
        }

        private string GetCommandLineArguments()
        {
            string inputFiles = string.IsNullOrWhiteSpace(this.InputFilesOrDirs)
                ? this.PlatformSpecifics.Combine(this.GzipDirectory, "silesia")
                : this.InputFilesOrDirs;

            return @$"{this.Options} {inputFiles}";
        }

        private async Task CheckDistroSupportAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                var linuxDistributionInfo = await this.systemManager.GetLinuxDistributionAsync(cancellationToken)
                .ConfigureAwait(false);

                switch (linuxDistributionInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                    case LinuxDistribution.Debian:
                    case LinuxDistribution.CentOS8:
                    case LinuxDistribution.RHEL8:
                    case LinuxDistribution.AzLinux:
                    case LinuxDistribution.CentOS7:
                    case LinuxDistribution.RHEL7:
                        break;
                    default:
                        throw new WorkloadException(
                            $"The Gzip benchmark workload is not supported on the current Linux distro.",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
        }

        internal class GzipState : State
        {
            public GzipState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool GzipStateInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(GzipState.GzipStateInitialized), false);
                }

                set
                {
                    this.Properties[nameof(GzipState.GzipStateInitialized)] = value;
                }
            }
        }
    }
}