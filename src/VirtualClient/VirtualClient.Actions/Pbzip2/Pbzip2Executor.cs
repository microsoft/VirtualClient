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
    /// The pbzip2 workload executor.
    /// </summary>
    [UnixCompatible]
    public class Pbzip2Executor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManager;

        /// <summary>
        /// Constructor for <see cref="Pbzip2Executor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public Pbzip2Executor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManager.PackageManager;
            this.stateManager = this.systemManager.StateManager;
            this.fileSystem = this.systemManager.FileSystem;
        }

        /// <summary>
        /// The options passed to pbzip2.
        /// </summary>
        public string Options
        {
            get
            {
                this.Parameters.TryGetValue(nameof(Pbzip2Executor.Options), out IConvertible options);
                return options?.ToString();
            }
        }

        /// <summary>
        /// Lzbench space separated input files
        /// </summary>
        public string InputFiles
        {
            get
            {
                this.Parameters.TryGetValue(nameof(Pbzip2Executor.InputFiles), out IConvertible inputFiles);
                return inputFiles?.ToString();
            }
        }

        /// <summary>
        /// The name of the directory where the pbzip2 is executed.
        /// </summary>
        protected string Pbzip2Directory
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

                using (IProcessProxy process = await this.ExecuteCommandAsync("bash", $"-c \"pbzip2 {commandLineArguments}\"", this.Pbzip2Directory, telemetryContext, cancellationToken, runElevated: true)
                    .ConfigureAwait())
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        process.ProcessDetails.ToolSet = "PBZip2";
                        await this.LogProcessDetailsAsync(process.ProcessDetails, telemetryContext, logToFile: true);

                        process.ThrowIfWorkloadFailed();
                        this.CaptureMetrics(process, commandLineArguments, telemetryContext, cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the Lzbench workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.CheckDistroSupportAsync(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            Pbzip2State state = await this.stateManager.GetStateAsync<Pbzip2State>($"{nameof(Pbzip2State)}", cancellationToken)
                ?? new Pbzip2State();

            if (!state.Pbzip2StateInitialized)
            {
                if (!this.fileSystem.Directory.Exists(this.Pbzip2Directory))
                {
                    this.fileSystem.Directory.CreateDirectory(this.Pbzip2Directory);
                }

                // Choose default file for compression and decompression if files/dirs are not provided.
                if (string.IsNullOrWhiteSpace(this.InputFiles))
                {
                    await this.ExecuteCommandAsync("wget", $"https://sun.aei.polsl.pl//~sdeor/corpus/silesia.zip", this.Pbzip2Directory, cancellationToken);
                    await this.ExecuteCommandAsync("unzip", "silesia.zip -d silesia", this.Pbzip2Directory, cancellationToken);
                }

                state.Pbzip2StateInitialized = true;
            }

            await this.stateManager.SaveStateAsync<Pbzip2State>($"{nameof(Pbzip2State)}", state, cancellationToken);
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

        private void CaptureMetrics(IProcessProxy process, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                bool compression = this.Scenario.Contains("Decompression") ? false : true;

                // Pbzip2 workload produces metrics in standard error
                Pbzip2MetricsParser parser = new Pbzip2MetricsParser(process.StandardError.ToString(), compression);
                IList<Metric> metrics = parser.Parse();

                this.Logger.LogMetrics(
                    "Pbzip2",
                    this.Scenario,
                    process.StartTime,
                    process.ExitTime,
                    metrics,
                    null,
                    commandArguments,
                    this.Tags,
                    telemetryContext);
            }
        }

        private async Task ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(Pbzip2Executor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.LogProcessDetailsAsync(process.ProcessDetails, telemetryContext)
                                .ConfigureAwait();

                            process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        private string GetCommandLineArguments()
        {
            string inputFiles = string.IsNullOrEmpty(this.InputFiles)
                ? this.PlatformSpecifics.Combine(this.Pbzip2Directory, "silesia/*")
                : this.InputFiles;

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
                            $"The PBZip2 benchmark workload is not supported on the current Linux distro - " +
                            $"{linuxDistributionInfo.LinuxDistribution.ToString()} through Virtual Client.",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
        }

        internal class Pbzip2State : State
        {
            public Pbzip2State(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool Pbzip2StateInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(Pbzip2State.Pbzip2StateInitialized), false);
                }

                set
                {
                    this.Properties[nameof(Pbzip2State.Pbzip2StateInitialized)] = value;
                }
            }
        }
    }
}