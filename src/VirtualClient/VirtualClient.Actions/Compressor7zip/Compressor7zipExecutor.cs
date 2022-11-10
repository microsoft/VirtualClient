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

    /// <summary>
    /// The Compressor7zip workload executor.
    /// </summary>
    [WindowsCompatible]
    public class Compressor7zipExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManager;

        /// <summary>
        /// Constructor for <see cref="Compressor7zipExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public Compressor7zipExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
                this.Parameters.TryGetValue(nameof(Compressor7zipExecutor.Options), out IConvertible options);
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
                this.Parameters.TryGetValue(nameof(Compressor7zipExecutor.InputFilesOrDirs), out IConvertible inputFilesOrDirs);
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
            DateTime startTime = DateTime.UtcNow;

            string commandLineArguments = this.GetCommandLineArguments();

            // Execute Compressor7zip
            string results = await this.ExecuteCommandAsync("7z", $"{commandLineArguments}", this.Compressor7zipDirectory, cancellationToken)
                .ConfigureAwait(false);

            DateTime endTime = DateTime.UtcNow;

            this.LogMetrics(results, startTime, endTime, telemetryContext, cancellationToken);            
        }

        /// <summary>
        /// Initializes the environment for execution of the Lzbench workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            Compressor7zipState state = await this.stateManager.GetStateAsync<Compressor7zipState>($"{nameof(Compressor7zipState)}", cancellationToken)
            ?? new Compressor7zipState();

            if (!state.Compressor7zipStateInitialized)
            {
                if (!this.fileSystem.Directory.Exists(this.Compressor7zipDirectory))
                {
                    this.fileSystem.Directory.CreateDirectory(this.Compressor7zipDirectory);
                }

                // Choose default file for compression and decompression if files/dirs are not provided.
                if (this.InputFilesOrDirs == string.Empty)
                {
                    await this.ExecuteCommandAsync("wget", $"https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip", this.Compressor7zipDirectory, cancellationToken);
                    await this.ExecuteCommandAsync("unzip", "silesia.zip -d silesia", this.Compressor7zipDirectory, cancellationToken);
                }

                state.Compressor7zipStateInitialized = true;
            }

            await this.stateManager.SaveStateAsync<Compressor7zipState>($"{nameof(Compressor7zipState)}", state, cancellationToken);            
        }

        /// <summary>
        /// Returns true/false whether the component should execute on the system/platform.
        /// </summary>
        /// <returns>Returns True or false</returns>
        protected override bool IsSupported()
        {
            bool isSupported = this.Platform == PlatformID.Win32NT;

            return isSupported;
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

                await this.Logger.LogMessageAsync($"{nameof(Compressor7zipExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    DateTime start = DateTime.Now;
                    using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogProcessDetails<Compressor7zipExecutor>(process, telemetryContext);
                            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                        }

                        output = process.StandardOutput.ToString();
                    }
                }).ConfigureAwait(false);
            }

            return output;
        }

        private void LogMetrics(string results, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            Compressor7zipMetricsParser parser = new Compressor7zipMetricsParser(results);
            IList<Metric> metrics = parser.Parse();

            this.Logger.LogMetrics(
                "7zip",
                this.Scenario,
                startTime,
                endTime,
                metrics,
                null,
                this.GetCommandLineArguments(),
                this.Tags,
                EventContext.Persisted());
        }

        private string GetCommandLineArguments()
        {
            string inputFilesOrDirs = string.IsNullOrEmpty(this.InputFilesOrDirs) ? this.PlatformSpecifics.Combine(this.Compressor7zipDirectory, "silesia/*") : this.InputFilesOrDirs;
            return @$"{this.Options} {inputFilesOrDirs}";
        }

        internal class Compressor7zipState : State
        {
            public Compressor7zipState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool Compressor7zipStateInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(Compressor7zipState.Compressor7zipStateInitialized), false);
                }

                set
                {
                    this.Properties[nameof(Compressor7zipState.Compressor7zipStateInitialized)] = value;
                }
            }
        }
    }
}