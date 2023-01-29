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
    using VirtualClient.Dependencies;

    /// <summary>
    /// The SuperBenchmark workload executor.
    /// </summary>
    [UnixCompatible]
    public class SuperBenchmarkExecutor : VirtualClientComponent
    {
        private const string SuperBenchmarkRunShell = "RunSuperBenchmark.sh";

        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManager;

        /// <summary>
        /// Constructor for <see cref="SuperBenchmarkExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SuperBenchmarkExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManager.PackageManager;
            this.stateManager = this.systemManager.StateManager;
            this.fileSystem = this.systemManager.FileSystem;
        }

        /// <summary>
        /// The version of the superbench release (CLI and Github release).
        /// </summary>
        public string Version
        {
            get
            {
                this.Parameters.TryGetValue(nameof(SuperBenchmarkExecutor.Version), out IConvertible version);
                return version?.ToString();
            }
        }

        /// <summary>
        /// The version of the superbench docker.
        /// </summary>
        public string ContainerVersion
        {
            get
            {
                this.Parameters.TryGetValue(nameof(SuperBenchmarkExecutor.ContainerVersion), out IConvertible containerVersion);
                return containerVersion?.ToString();
            }
        }

        /// <summary>
        /// The superbench config name.
        /// </summary>
        public string ConfigurationFile
        {
            get
            {
                this.Parameters.TryGetValue(nameof(SuperBenchmarkExecutor.ConfigurationFile), out IConvertible config);
                return config?.ToString();
            }
        }

        /// <summary>
        /// The username to execute superbench, required.
        /// </summary>
        public string Username => this.Parameters.GetValue<string>(nameof(SuperBenchmarkExecutor.Username));

        /// <summary>
        /// The name of the package where the SuperBenchmark is cloned.
        /// </summary>
        public string SuperBenchmarkDirectory
        {
            get
            {
                return this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, "superbenchmark");
            }
        }

        /// <summary>
        /// The output directory of superbench.
        /// </summary>
        public string OutputDirectory
        {
            get
            {
                return this.PlatformSpecifics.Combine(this.SuperBenchmarkDirectory, "outputs");
            }
        }

        /// <summary>
        /// Executes the SuperBenchmark workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                DateTime startTime = DateTime.UtcNow;
                await this.ExecuteCommandAsync("sb", this.GetCommandLineArguments(), this.SuperBenchmarkDirectory, cancellationToken)
                    .ConfigureAwait(false);

                DateTime endTime = DateTime.UtcNow;

                this.LogSuperBenchmarkOutput(startTime, endTime);
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the SuperBenchmark workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            SuperBenchmarkState state = await this.stateManager.GetStateAsync<SuperBenchmarkState>($"{nameof(SuperBenchmarkState)}", cancellationToken)
                ?? new SuperBenchmarkState();

            if (!state.SuperBenchmarkInitialized)
            {
                // This is to grant directory folders for 
                await this.systemManager.MakeFilesExecutableAsync(this.PlatformSpecifics.CurrentDirectory, this.Platform, cancellationToken);
                await this.ExecuteCommandAsync("git", $"clone -b v{this.Version} https://github.com/microsoft/superbenchmark", this.PlatformSpecifics.PackagesDirectory, cancellationToken);

                foreach (string file in this.fileSystem.Directory.GetFiles(this.PlatformSpecifics.GetScriptPath("superbenchmark")))
                {
                    this.fileSystem.File.Copy(
                        file,
                        this.Combine(this.SuperBenchmarkDirectory, Path.GetFileName(file)),
                        true);
                }

                await this.ExecuteCommandAsync("bash", $"initialize.sh {this.Username}", this.SuperBenchmarkDirectory, cancellationToken);
                await this.ExecuteCommandAsync("sb", $"deploy --host-list localhost -i {this.ContainerVersion}", this.SuperBenchmarkDirectory, cancellationToken);

                state.SuperBenchmarkInitialized = true;
            }

            await this.stateManager.SaveStateAsync<SuperBenchmarkState>($"{nameof(SuperBenchmarkState)}", state, cancellationToken);
        }

        private async Task ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(SuperBenchmarkExecutor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    DateTime start = DateTime.Now;
                    using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                    {
                        SystemManagement.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogProcessDetails<SuperBenchmarkExecutor>(process, telemetryContext);
                            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        private void LogSuperBenchmarkOutput(DateTime startTime, DateTime endTime)
        {
            string[] outputFiles = this.fileSystem.Directory.GetFiles(this.OutputDirectory, "results-summary.jsonl", SearchOption.AllDirectories);

            foreach (string file in outputFiles)
            {
                string text = this.fileSystem.File.ReadAllText(file);
                SuperBenchmarkMetricsParser parser = new SuperBenchmarkMetricsParser(text);
                parser.Parse();
                Console.WriteLine(parser.Parse().Count);
                this.Logger.LogMetrics(
                    toolName: "SuperBenchmark",
                    scenarioName: "SuperBenchmark",
                    startTime,
                    endTime,
                    parser.Parse(),
                    metricCategorization: $"{this.ConfigurationFile}",
                    scenarioArguments: this.GetCommandLineArguments(),
                    this.Tags,
                    EventContext.Persisted());

                this.fileSystem.File.Delete(file);
            }
        }

        private string GetCommandLineArguments()
        {
            return @$"run --host-list localhost -c {this.ConfigurationFile}";
        }

        internal class SuperBenchmarkState : State
        {
            public SuperBenchmarkState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool SuperBenchmarkInitialized
            { 
                get
                {
                    return this.Properties.GetValue<bool>(nameof(SuperBenchmarkState.SuperBenchmarkInitialized), false);
                }

                set
                {
                    this.Properties[nameof(SuperBenchmarkState.SuperBenchmarkInitialized)] = value;
                }
            }
        }
    }
}