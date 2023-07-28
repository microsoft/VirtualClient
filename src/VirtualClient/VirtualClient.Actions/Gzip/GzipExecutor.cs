// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The Gzip workload executor.
    /// </summary>
    [UnixCompatible]
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
                    await this.ExecuteCommandAsync("wget", $"https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip", this.GzipDirectory, cancellationToken);
                    await this.ExecuteCommandAsync("unzip", "silesia.zip -d silesia", this.GzipDirectory, cancellationToken);
                }

                state.GzipStateInitialized = true;
            }

            await this.stateManager.SaveStateAsync<GzipState>($"{nameof(GzipState)}", state, cancellationToken);
        }

        /// <summary>
        /// Returns true/false whether the component should execute on the system/platform.
        /// </summary>
        /// <returns>Returns True or false</returns>
        protected override bool IsSupported()
        {
            return this.Platform == PlatformID.Unix;
        }

        private async Task<string> ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, CancellationToken cancellationToken)
        {
            string output = string.Empty;

            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(GzipExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    DateTime start = DateTime.Now;
                    using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait();

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext);
                            process.ThrowIfWorkloadFailed();
                        }

                        output = process.StandardOutput.ToString();
                    }
                }).ConfigureAwait();
            }

            return output;
        }

        private void CaptureMetrics(IProcessProxy process, string commandArguments, EventContext telemetryContext)
        {
            telemetryContext.AddScenarioMetadata(
                "Gzip",
                commandArguments,
                toolVersion: null,
                this.PackageName);

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
                    case LinuxDistribution.Mariner:
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