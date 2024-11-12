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
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The SuperBenchmark workload executor.
    /// </summary>
    [SupportedPlatforms("linux-x64", true)]
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
        public string Username
        {
            get
            {
                string username = this.Parameters.GetValue<string>(nameof(SuperBenchmarkExecutor.Username), string.Empty);
                if (string.IsNullOrWhiteSpace(username))
                {
                    username = this.PlatformSpecifics.GetLoggedInUser();
                }

                return username;
            }
        }

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
                string commandArguments = this.GetCommandLineArguments();

                using (IProcessProxy process = await this.ExecuteCommandAsync("sb", commandArguments, this.SuperBenchmarkDirectory, telemetryContext, cancellationToken, runElevated: false))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        if (process.IsErrored())
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "SuperBench", logToFile: true);
                            process.ThrowIfWorkloadFailed();
                        }

                        await this.CaptureMetricsAsync(process, commandArguments, telemetryContext, cancellationToken);
                    }
                }
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

                string cloneDir = this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, "superbenchmark");
                if (!this.fileSystem.Directory.Exists(cloneDir))
                {
                    await this.ExecuteSbCommandAsync("git", $"clone -b v{this.Version} https://github.com/microsoft/superbenchmark", this.PlatformSpecifics.PackagesDirectory, telemetryContext, cancellationToken, true);
                }

                foreach (string file in this.fileSystem.Directory.GetFiles(this.PlatformSpecifics.GetScriptPath("superbenchmark")))
                {
                    this.fileSystem.File.Copy(
                        file,
                        this.Combine(this.SuperBenchmarkDirectory, Path.GetFileName(file)),
                        true);
                }

                await this.ExecuteSbCommandAsync("bash", $"initialize.sh {this.Username}", this.SuperBenchmarkDirectory, telemetryContext, cancellationToken, true);
                await this.ExecuteSbCommandAsync("sb", $"deploy --host-list localhost -i {this.ContainerVersion}", this.SuperBenchmarkDirectory, telemetryContext, cancellationToken, false);

                state.SuperBenchmarkInitialized = true;
            }

            await this.stateManager.SaveStateAsync<SuperBenchmarkState>($"{nameof(SuperBenchmarkState)}", state, cancellationToken);
        }

        private async Task ExecuteSbCommandAsync(string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken, bool runElevated)
        {
            IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, telemetryContext, cancellationToken, runElevated: runElevated);

            if (!cancellationToken.IsCancellationRequested)
            {
                await this.LogProcessDetailsAsync(process, telemetryContext, "Superbench", logToFile: true);
                process.ThrowIfWorkloadFailed();
            }
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    "SuperBenchmark",
                    process.FullCommand(),
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                string[] outputFiles = this.fileSystem.Directory.GetFiles(this.OutputDirectory, "results-summary.jsonl", SearchOption.AllDirectories);

                foreach (string file in outputFiles)
                {
                    string results = this.fileSystem.File.ReadAllText(file);
                    await this.LogProcessDetailsAsync(process, telemetryContext, "SuperBench", logToFile: true);

                    SuperBenchmarkMetricsParser parser = new SuperBenchmarkMetricsParser(results);
                    IList<Metric> metrics = parser.Parse();

                    this.Logger.LogMetrics(
                        toolName: "SuperBenchmark",
                        scenarioName: "SuperBenchmark",
                        process.StartTime,
                        process.ExitTime,
                        metrics,
                        metricCategorization: $"{this.ConfigurationFile}",
                        scenarioArguments: commandArguments,
                        this.Tags,
                        telemetryContext);

                    await this.fileSystem.File.DeleteAsync(file);
                }
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