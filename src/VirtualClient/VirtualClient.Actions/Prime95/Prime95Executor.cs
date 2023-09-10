// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The Prime95 workload executor.
    /// </summary>
    public class Prime95Executor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ISystemManagement systemManagement;
        private List<int> successExitCodes;

        /// <summary>
        /// Constructor for <see cref="Prime95Executor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public Prime95Executor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
            this.fileSystem = this.systemManagement.FileSystem;

            // The exit code on SafeKill is -1 which is not a part of the default success codes.
            this.successExitCodes = new List<int>(ProcessProxy.DefaultSuccessCodes) { -1 };
        }

        /// <summary>
        /// The length of time in which to run the Prime95 workload.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.Duration));
            }
        }

        /// <summary>
        /// The argument for Mininum FFTSize defined in the profile.
        /// <list type="bullet">
        /// <item>
        /// <term>Smallest FFT values</term>
        /// <description>4K-32K</description>
        /// </item>
        /// <item>
        /// <term>Small FFT values</term>
        /// <description>32K-1024K</description>
        /// </item>
        /// <item>
        /// <term>Large FFT values</term>
        /// <description>2048K-8192K</description>
        /// </item>
        /// </list>
        /// </summary>
        public int MinTortureFFT
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.MinTortureFFT));
            }

            set
            {
                this.Parameters[nameof(this.MinTortureFFT)] = value;
            }
        }

        /// <summary>
        /// The argument for Maximum FFTSize defined in the profile.
        /// <list type="bullet">
        /// <item>
        /// <term>Smallest FFT values</term>
        /// <description>4K-32K</description>
        /// </item>
        /// <item>
        /// <term>Small FFT values</term>
        /// <description>32K-1024K</description>
        /// </item>
        /// <item>
        /// <term>Large FFT values</term>
        /// <description>2048K-8192K</description>
        /// </item>
        /// </list>
        /// </summary>
        public int MaxTortureFFT
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.MaxTortureFFT));
            }

            set
            {
                this.Parameters[nameof(this.MaxTortureFFT)] = value;
            }
        }

        /// <summary>
        /// The ThreadCountargument defined in the profile.
        /// </summary>
        public int ThreadCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ThreadCount), (Environment.ProcessorCount / 2));
            }

            set
            {
                this.Parameters[nameof(this.ThreadCount)] = value;
            }
        }

        /// <summary>
        /// True to use Intel/AMD hyperthreading.
        /// </summary>
        public bool UseHyperthreading
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.UseHyperthreading), true);
            }
        }

        /// <summary>
        /// The path to the Prime95 executable file.
        /// </summary>
        protected string ExecutablePath { get; private set; }

        /// <summary>
        /// The path to the Prime95 results file.
        /// </summary>
        protected string ResultsFilePath { get; private set; }

        /// <summary>
        /// The path to the prime95.txt file.
        /// </summary>
        protected string SettingsFilePath { get; private set; }

        /// <summary>
        /// The path to the Prime95 workload package.
        /// </summary>
        protected DependencyPath Prime95Package { get; private set; }

        /// <summary>
        /// Executes cleanup operations.
        /// </summary>
        protected override async Task CleanupAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.CleanupAsync(telemetryContext, cancellationToken);
            ProcessManager processManager = this.Dependencies.GetService<ProcessManager>();

            string processName = Path.GetFileNameWithoutExtension(this.ExecutablePath);
            IEnumerable<IProcessProxy> runningProcesses = processManager.GetProcesses(Path.GetFileNameWithoutExtension(processName));

            if (runningProcesses?.Any() == true)
            {
                foreach (IProcessProxy processProxy in runningProcesses)
                {
                    processProxy.SafeKill();
                }
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the Prime95 workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.EvaluateParametersAsync(cancellationToken);

            this.Prime95Package = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);

            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    this.ExecutablePath = this.Combine(this.Prime95Package.Path, "prime95.exe");
                    break;

                case PlatformID.Unix:
                    this.ExecutablePath = this.Combine(this.Prime95Package.Path, "mprime");
                    break;

                default:
                    throw new WorkloadException(
                        $"The Prime95 workload is not supported on the current platform/architecture " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture)}." +
                        ErrorReason.PlatformNotSupported);
            }

            await this.systemManagement.MakeFileExecutableAsync(this.ExecutablePath, this.Platform, cancellationToken);

            if (!this.fileSystem.File.Exists(this.ExecutablePath))
            {
                throw new DependencyException(
                    $"The expected workload binary/executable was not found in the '{this.PackageName}' package. The workload cannot be executed " +
                    $"successfully without this binary/executable. Check that the workload package was installed successfully and that the executable " +
                    $"exists in the path expected '{this.ExecutablePath}'.",
                    ErrorReason.DependencyNotFound);
            }

            this.SettingsFilePath = this.Combine(this.Prime95Package.Path, "prime.txt");
            this.ResultsFilePath = this.Combine(this.Prime95Package.Path, "results.txt");
        }

        /// <summary>
        /// Executes the Prime95 workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                await this.CreatePrime95SettingsFileAsync(this.SettingsFilePath);
                await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken);
            }
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// operating system and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = base.IsSupported() && (this.Platform == PlatformID.Win32NT || this.Platform == PlatformID.Unix);

            if (!isSupported)
            {
                this.Logger.LogNotSupported("Prime95", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        /// <summary>
        /// Validates the parameters provided to the profile.
        /// </summary>
        protected override void Validate()
        {
            if (this.Duration <= TimeSpan.Zero)
            {
                throw new WorkloadException(
                    $"Invalid '{nameof(this.Duration)}' parameter value. The duration parameter must be greater than zero." +
                    ErrorReason.InvalidProfileDefinition);
            }

            if (this.MinTortureFFT <= 0)
            {
                throw new WorkloadException(
                    $"Invalid '{nameof(this.MinTortureFFT)}' parameter value. The minimum torture FFT value must be greater than zero.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (this.MaxTortureFFT < this.MinTortureFFT)
            {
                throw new WorkloadException(
                    $"Invalid '{nameof(this.MaxTortureFFT)}' parameter value. The maximum torture FFT value must be greater than or equal to the '{nameof(this.MinTortureFFT)}' parameter value.",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        /// <summary>
        /// Executes the Prime95 workload command and generates the results file for the logs
        /// </summary>
        private async Task ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                string commandArguments = "-t";

                EventContext relatedContext = telemetryContext.Clone()
                    .AddContext("command", this.ExecutablePath)
                    .AddContext("commandArguments", commandArguments);

                await this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(this.ExecutablePath, commandArguments, this.Prime95Package.Path))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());

                        // Prime95 does not stop on it's own. It will run until you tell it to stop.
                        // We have to definitively stop the program.
                        DateTime explicitTimeout = DateTime.UtcNow.Add(this.Duration);

                        if (process.Start())
                        {
                            await this.WaitAsync(explicitTimeout, cancellationToken);
                            process.SafeKill();

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                string results = null;

                                try
                                {
                                    if (this.fileSystem.File.Exists(this.ResultsFilePath))
                                    {
                                        results = await this.fileSystem.File.ReadAllTextAsync(this.ResultsFilePath);
                                    }
                                    
                                    if (string.IsNullOrWhiteSpace(results))
                                    {
                                        throw new WorkloadResultsException(
                                            $"Prime95 results file not found at path '{this.ResultsFilePath}'.",
                                            ErrorReason.WorkloadResultsNotFound);
                                    }

                                    // The exit code on SafeKill is -1 which is not a part of the default success codes.
                                    process.ThrowIfWorkloadFailed(this.successExitCodes);
                                    this.CaptureMetrics(process, results, telemetryContext, cancellationToken);
                                }
                                finally
                                {
                                    await this.LogProcessDetailsAsync(process, telemetryContext, "Prime95", results?.AsArray());
                                }
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Creates prime.txt file in working directory for providing configuration arguments to prime95
        /// </summary>
        /// <param name="settingsFilePath">The file path to write the argument file to.</param>
        private Task CreatePrime95SettingsFileAsync(string settingsFilePath)
        {
            string prime95Arguments =
                $"ErrorCheck=1{Environment.NewLine}" +
                $"SumInputsErrorCheck=1{Environment.NewLine}" +
                $"V24OptionsConverted=1{Environment.NewLine}" +
                $"TortureHyperthreading={(this.UseHyperthreading ? 1 : 0)}{Environment.NewLine}" +
                $"StressTester=1{Environment.NewLine}" +
                $"TortureThreads={this.ThreadCount}{Environment.NewLine}" +
                $"MinTortureFFT={this.MinTortureFFT}{Environment.NewLine}" +
                $"MaxTortureFFT={this.MaxTortureFFT}{Environment.NewLine}" +
                $"TortureTime=1{Environment.NewLine}" +
                $"UsePrimenet=0\n";

            return this.fileSystem.File.WriteAllTextAsync(settingsFilePath, prime95Arguments);
        }

        /// <summary>
        /// Logs the Prime95 workload metrics.
        /// </summary>
        private void CaptureMetrics(IProcessProxy process, string results, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    "Prime95",
                    process.FullCommand(),
                    toolVersion: this.Prime95Package.Version);

                this.MetadataContract.Apply(telemetryContext);

                Prime95MetricsParser parser = new Prime95MetricsParser(results);
                IList<Metric> workloadMetrics = parser.Parse();

                this.Logger.LogMetrics(
                    "Prime95",
                    // e.g.
                    // cpustress_t32_fft4-8192_20mins
                    $"cpustress_t{this.ThreadCount}_fft{this.MinTortureFFT}-{this.MaxTortureFFT}_{this.Duration.TotalMinutes}mins",
                    process.StartTime,
                    DateTime.UtcNow,
                    workloadMetrics,
                    null,
                    process.FullCommand(),
                    this.Tags,
                    telemetryContext);
            }
        }
    }
}