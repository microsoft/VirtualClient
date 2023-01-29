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
                DateTime startTime = DateTime.UtcNow;
                string commandLineArguments = this.GetCommandLineArguments();

                string results = await this.ExecuteCommandAsync("bash", $"-c \"pbzip2 {commandLineArguments}\"", this.Pbzip2Directory, cancellationToken)
                    .ConfigureAwait(false);

                DateTime endTime = DateTime.UtcNow;
                this.LogMetrics(results, startTime, endTime, telemetryContext, cancellationToken);
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
                if (this.InputFiles == string.Empty)
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

        private async Task<string> ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, CancellationToken cancellationToken)
        {
            string output = string.Empty;

            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                await this.Logger.LogMessageAsync($"{nameof(Pbzip2Executor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    DateTime start = DateTime.Now;
                    using (IProcessProxy process = this.systemManager.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogProcessDetails<Pbzip2Executor>(process, telemetryContext);
                            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                        }

                        output = process.StandardError.ToString();
                    }
                }).ConfigureAwait(false);
            }

            return output;
        }

        private void LogMetrics(string results, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            bool compression = this.Scenario.Contains("Decompression") ? false : true;

            Pbzip2MetricsParser parser = new Pbzip2MetricsParser(results, compression);
            IList<Metric> metrics = parser.Parse();

            this.Logger.LogMetrics(
                "Pbzip2",
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
            string inputFiles = string.IsNullOrEmpty(this.InputFiles) ? this.PlatformSpecifics.Combine(this.Pbzip2Directory, "silesia/*") : this.InputFiles;
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
                            $"The Gzip benchmark workload is not supported on the current Linux distro - " +
                            $"{linuxDistributionInfo.LinuxDistribution.ToString()} through Virtual Client.  Supported distros include:" +
                            $" Ubuntu, Debian, CentOS8,RHEL8,Mariner,CentOS7,RHEL7. ",
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