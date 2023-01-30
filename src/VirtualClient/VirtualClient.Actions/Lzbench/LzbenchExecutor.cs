// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
    /// The Lzbench workload executor.
    /// </summary>
    [UnixCompatible]
    public class LzbenchExecutor : VirtualClientComponent
    {
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
                DateTime startTime = DateTime.UtcNow;
                string commandLineArguments = this.GetCommandLineArguments();

                // Execute Lzbench
                await this.ExecuteCommandAsync("bash", $"lzbenchexecutor.sh \"{commandLineArguments}\"", this.LzbenchDirectory, cancellationToken)
                .ConfigureAwait(false);

                DateTime endTime = DateTime.UtcNow;
                this.LogMetrics(startTime, endTime, telemetryContext, cancellationToken);
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
                // Clone Lzbench code from git.
                await this.ExecuteCommandAsync("git", $"clone -b v{this.Version} https://github.com/inikep/lzbench.git", this.PlatformSpecifics.PackagesDirectory, cancellationToken);
                
                // Build Lzbench.
                await this.ExecuteCommandAsync("make", string.Empty, this.LzbenchDirectory, cancellationToken);

                // Choose default file for compression and decompression if files/dirs are not provided.
                if (this.InputFilesOrDirs == string.Empty)
                {
                    await this.ExecuteCommandAsync("wget", $"https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip", this.LzbenchDirectory, cancellationToken);
                    await this.ExecuteCommandAsync("unzip", "silesia.zip -d silesia", this.LzbenchDirectory, cancellationToken);
                }

                // Copy script to run Lzbench from script folder to Lzbench folder.
                Console.WriteLine(this.PlatformSpecifics.GetScriptPath("lzbench"));
                foreach (string file in this.fileSystem.Directory.GetFiles(this.PlatformSpecifics.GetScriptPath("lzbench")))
                {
                    Console.WriteLine(file);
                    Console.WriteLine(this.LzbenchDirectory);
                    Console.WriteLine(Path.GetFileName(file));
                    this.fileSystem.File.Copy(
                        file,
                        this.Combine(this.LzbenchDirectory, Path.GetFileName(file)),
                        true);
                }

                state.LzbenchInitialized = true;
            }

            await this.stateManager.SaveStateAsync<LzbenchState>($"{nameof(LzbenchState)}", state, cancellationToken);
        }

        /// <summary>
        /// Returns true/false whether the component should execute on the system/platform.
        /// </summary>
        /// <returns>Returns True or false</returns>
        protected override bool IsSupported()
        {
            bool isSupported = this.Platform == PlatformID.Unix;

            return isSupported;
        }

        private async Task ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(LzbenchExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    DateTime start = DateTime.Now;
                    using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogProcessDetails<LzbenchExecutor>(process, telemetryContext);
                            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        private void LogMetrics(DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string[] outputFiles = this.fileSystem.Directory.GetFiles(this.LzbenchDirectory, "results-summary.csv", SearchOption.AllDirectories);

            foreach (string file in outputFiles)
            {
                string text = this.fileSystem.File.ReadAllText(file);
                LzbenchMetricsParser parser = new LzbenchMetricsParser(text);
                IList<Metric> metrics = parser.Parse();

                this.Logger.LogMetrics(
                    "Lzbench",
                    "Lzbench",
                    startTime,
                    endTime,
                    metrics,
                    null,
                    this.GetCommandLineArguments(),
                    this.Tags,
                    EventContext.Persisted());
                
                this.fileSystem.File.Delete(file);
            }
        }

        private string GetCommandLineArguments()
        {
            string inputFilesOrDirs = string.IsNullOrEmpty(this.InputFilesOrDirs) ? this.PlatformSpecifics.Combine(this.LzbenchDirectory, "silesia") : this.InputFilesOrDirs;
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