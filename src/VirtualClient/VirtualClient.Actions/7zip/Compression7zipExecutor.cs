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
    /// The 7zip compression workload executor.
    /// </summary>
    [SupportedPlatforms("win-x64,win-arm64")]
    public class Compression7zipExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManager;

        /// <summary>
        /// Constructor for <see cref="Compression7zipExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public Compression7zipExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManager.PackageManager;
            this.stateManager = this.systemManager.StateManager;
            this.fileSystem = this.systemManager.FileSystem;
        }

        /// <summary>
        /// The options passed to Compressor7zip.
        /// </summary>
        public string Options
        {
            get
            {
                this.Parameters.TryGetValue(nameof(Compression7zipExecutor.Options), out IConvertible options);
                return options?.ToString();
            }
        }

        /// <summary>
        /// Compressor7zip space separated input files or directories
        /// </summary>
        public string InputFilesOrDirs
        {
            get
            {
                this.Parameters.TryGetValue(nameof(Compression7zipExecutor.InputFilesOrDirs), out IConvertible inputFilesOrDirs);
                return inputFilesOrDirs?.ToString();
            }
        }

        /// <summary>
        /// The name of the directory where the Compressor7zip is executed.
        /// </summary>
        protected string Compressor7zipDirectory
        {
            get
            {
                return this.GetPackagePath(this.PackageName);
            }
        }

        /// <summary>
        /// Executes the Compressor7zip workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string commandLineArguments = this.GetCommandLineArguments();

                using (IProcessProxy process = await this.ExecuteCommandAsync("7z", commandLineArguments, this.Compressor7zipDirectory, telemetryContext, cancellationToken)
                   .ConfigureAwait(false))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "7Zip", logToFile: true);

                        process.ThrowIfWorkloadFailed();
                        this.CaptureMetrics(process, telemetryContext, commandLineArguments);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the Lzbench workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            Compression7zipState state = await this.stateManager.GetStateAsync<Compression7zipState>($"{nameof(Compression7zipState)}", cancellationToken)
                ?? new Compression7zipState();

            if (!state.Compressor7zipStateInitialized)
            {
                if (!this.fileSystem.Directory.Exists(this.Compressor7zipDirectory))
                {
                    this.fileSystem.Directory.CreateDirectory(this.Compressor7zipDirectory);
                }

                // Choose default file for compression and decompression if files/dirs are not provided.
                if (string.IsNullOrWhiteSpace(this.InputFilesOrDirs))
                {
                    await this.ExecuteCommandAsync("wget", $"--no-check-certificate https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip", this.Compressor7zipDirectory, cancellationToken);
                    await this.ExecuteCommandAsync("unzip", "silesia.zip -d silesia", this.Compressor7zipDirectory, cancellationToken);
                }

                state.Compressor7zipStateInitialized = true;
            }

            await this.stateManager.SaveStateAsync<Compression7zipState>($"{nameof(Compression7zipState)}", state, cancellationToken);
        }

        private void CaptureMetrics(IProcessProxy process, EventContext telemetryContext, string commandArguments)
        {
            process.ThrowIfNull(nameof(process));

            this.MetadataContract.AddForScenario(
                "7Zip",
                commandArguments,
                toolVersion: null);

            this.MetadataContract.Apply(telemetryContext);

            Compression7zipMetricsParser parser = new Compression7zipMetricsParser(process.StandardOutput.ToString());
            IList<Metric> metrics = parser.Parse();

            this.Logger.LogMetrics(
                "7zip",
                this.Scenario,
                process.StartTime,
                process.ExitTime,
                metrics,
                null,
                commandArguments,
                this.Tags,
                telemetryContext);
        }

        private async Task ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(Compression7zipExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    DateTime start = DateTime.Now;
                    using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken)
                            .ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext)
                                .ConfigureAwait(false);

                            process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        private string GetCommandLineArguments()
        {
            string inputFilesOrDirs = string.IsNullOrWhiteSpace(this.InputFilesOrDirs)
                ? this.PlatformSpecifics.Combine(this.Compressor7zipDirectory, "silesia/*")
                : this.InputFilesOrDirs;

            return @$"{this.Options} {inputFilesOrDirs}";
        }

        internal class Compression7zipState : State
        {
            public Compression7zipState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool Compressor7zipStateInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(Compression7zipState.Compressor7zipStateInitialized), false);
                }

                set
                {
                    this.Properties[nameof(Compression7zipState.Compressor7zipStateInitialized)] = value;
                }
            }
        }
    }
}