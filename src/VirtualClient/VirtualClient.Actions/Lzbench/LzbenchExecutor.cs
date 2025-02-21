// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
    /// The Lzbench workload executor.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class LzbenchExecutor : VirtualClientComponent
    {
        private const string LzBench = nameof(LzbenchExecutor.LzBench);

        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManager;

        /// <summary>
        /// Constructor for <see cref="LzbenchExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public LzbenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManager.PackageManager;
            this.stateManager = this.systemManager.StateManager;
            this.fileSystem = this.systemManager.FileSystem;
        }

        /// <summary>
        /// The version of the Lzbench release (CLI and Github release).
        /// </summary>
        public string Version
        {
            get
            {
                this.Parameters.TryGetValue(nameof(LzbenchExecutor.Version), out IConvertible version);
                return version?.ToString();
            }
        }

        /// <summary>
        /// The options passed to Lzbench.
        /// </summary>
        public string Options
        {
            get
            {
                this.Parameters.TryGetValue(nameof(LzbenchExecutor.Options), out IConvertible options);
                return options?.ToString();
            }
        }

        /// <summary>
        /// Lzbench space separated input files and/or directories
        /// </summary>
        public string InputFilesOrDirs
        {
            get
            {
                this.Parameters.TryGetValue(nameof(LzbenchExecutor.InputFilesOrDirs), out IConvertible inputFilesOrDirs);
                return inputFilesOrDirs?.ToString();
            }
        }

        /// <summary>
        /// The name of the directory where the Lzbench is cloned.
        /// </summary>
        public string LzbenchDirectory
        {
            get
            {
                return this.GetPackagePath(this.PackageName);
            }
        }

        /// <summary>
        /// Executes the Lzbench workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string commandLineArguments = this.GetCommandLineArguments();

                // Note:
                // We are attempting to add in a common method for execution of commands as we have seen throughout many executors.
                // In the near term, we are going to add this in slowly and incrementally in order to avoid raising the likelihood
                // of regressions.
                using (IProcessProxy process = await this.ExecuteCommandAsync("bash", $"lzbenchexecutor.sh \"{commandLineArguments}\"", this.LzbenchDirectory, telemetryContext, cancellationToken, runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        if (process.IsErrored())
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "LZbench");
                            process.ThrowIfWorkloadFailed();
                        }

                        await this.CaptureMetricsAsync(process, commandLineArguments, telemetryContext, cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the Lzbench workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            LzbenchState state = await this.stateManager.GetStateAsync<LzbenchState>($"{nameof(LzbenchState)}", cancellationToken)
                ?? new LzbenchState();

            if (!state.LzbenchInitialized)
            {      
                using (IProcessProxy process = await this.ExecuteCommandAsync("git", $"clone -b v{this.Version} https://github.com/inikep/lzbench.git", this.PlatformSpecifics.PackagesDirectory, telemetryContext, cancellationToken, runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                    }
                }

                // Build Lzbench.
                using (IProcessProxy process = await this.ExecuteCommandAsync("make", this.LzbenchDirectory, telemetryContext, cancellationToken, runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                    }
                }

                // Choose default file for compression and decompression if files/dirs are not provided.
                if (string.IsNullOrWhiteSpace(this.InputFilesOrDirs))
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync("wget", $"https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip", this.LzbenchDirectory, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                        }
                    }

                    using (IProcessProxy process = await this.ExecuteCommandAsync("unzip", "silesia.zip -d silesia", this.LzbenchDirectory, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                        }
                    }
                }

                foreach (string file in this.fileSystem.Directory.GetFiles(this.PlatformSpecifics.GetScriptPath("lzbench")))
                {
                    this.fileSystem.File.Copy(
                        file,
                        this.Combine(this.LzbenchDirectory, Path.GetFileName(file)),
                        true);
                }

                state.LzbenchInitialized = true;
            }

            await this.stateManager.SaveStateAsync<LzbenchState>($"{nameof(LzbenchState)}", state, cancellationToken);
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    "Lzbench",
                    process.FullCommand(),
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                string[] resultsFiles = this.fileSystem.Directory.GetFiles(this.LzbenchDirectory, "results-summary.csv", SearchOption.AllDirectories);

                foreach (string file in resultsFiles)
                {
                    string results = await this.LoadResultsAsync(file, cancellationToken);
                    await this.LogProcessDetailsAsync(process, telemetryContext, "LZbench", results.AsArray(), logToFile: true);

                    LzbenchMetricsParser parser = new LzbenchMetricsParser(results);
                    IList<Metric> metrics = parser.Parse();

                    this.Logger.LogMetrics(
                        "Lzbench",
                        "Lzbench",
                        process.StartTime,
                        process.ExitTime,
                        metrics,
                        null,
                        commandArguments,
                        this.Tags,
                        telemetryContext);

                    await this.fileSystem.File.DeleteAsync(file);
                }
            }
        }

        private string GetCommandLineArguments()
        {
            string inputFilesOrDirs = string.IsNullOrWhiteSpace(this.InputFilesOrDirs)
                ? this.PlatformSpecifics.Combine(this.LzbenchDirectory, "silesia")
                : this.InputFilesOrDirs;

            return @$"{this.Options} {inputFilesOrDirs}";
        }

        internal class LzbenchState : State
        {
            public LzbenchState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool LzbenchInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(LzbenchState.LzbenchInitialized), false);
                }

                set
                {
                    this.Properties[nameof(LzbenchState.LzbenchInitialized)] = value;
                }
            }
        }
    }
}