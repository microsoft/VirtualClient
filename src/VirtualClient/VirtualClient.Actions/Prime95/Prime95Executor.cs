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
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The Prime95 workload executor.
    /// </summary>
    public class Prime95Executor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ISystemManagement systemManagement;

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
        }

        /// <summary>
        /// The command line argument defined in the profile.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(Prime95Executor.CommandLine));
            }
        }

        /// <summary>
        /// The TimeInMins argument defined in the profile.
        /// </summary>
        public int TimeInMins
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(Prime95Executor.TimeInMins));
            }
        }

        /// <summary>
        /// The TortureHyperthreading argument defined in the profile, Switch to toggle Prime95 built-in hyperthreading option
        /// </summary>
        public int TortureHyperthreading
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(Prime95Executor.TortureHyperthreading));
            }
        }

        /// <summary>
        /// The FFT Configuration argument defined in the profile.
        /// </summary>
        public int FFTConfiguration
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(Prime95Executor.FFTConfiguration));
            }
        }

        /// <summary>
        /// The argument for Mininum FFTSize defined in the profile.
        /// </summary>
        public int MinTortureFFT
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(Prime95Executor.MinTortureFFT));
            }

            set
            {
                this.Parameters[nameof(this.MinTortureFFT)] = value;
            }
        }

        /// <summary>
        /// The argument for Maximum FFTSize defined in the profile.
        /// </summary>
        public int MaxTortureFFT
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(Prime95Executor.MaxTortureFFT));
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
                string numThread = this.Parameters.GetValue<string>(nameof(Prime95Executor.ThreadCount));
                return string.IsNullOrWhiteSpace(numThread) ? 0 : int.Parse(numThread);
            }

            set
            {
                this.Parameters[nameof(this.ThreadCount)] = value;
            }
        }

        /// <summary>
        /// The path to the Prime95 package.
        /// </summary>
        private string PackageDirectory { get; set; }

        /// <summary>
        /// The path to the Prime95 executable file.
        /// </summary>
        private string ExecutableName { get; set; }

        /// <summary>
        /// Validates the parameters provided to the profile.
        /// </summary>
        protected override void ValidateParameters()
        {
            if (string.IsNullOrWhiteSpace(this.Scenario))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition. The action in the profile does not contain the " +
                    $"required '{nameof(this.Scenario)}' arguments defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (string.IsNullOrWhiteSpace(this.CommandLine))
            {
                throw new WorkloadException(
                    $"Unexpected profile definition.The action in the profile does not contain the " +
                    $"required '{nameof(this.CommandLine)}' arguments defined.",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (this.TimeInMins <= 0)
            {
                throw new WorkloadException(
                    $"Unexpected profile definition.The action in the profile does not contain the " +
                    $"required value for'{nameof(this.TimeInMins)}' arguments defined. {nameof(this.TimeInMins)} should be greater than 0",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (this.MinTortureFFT <= 0)
            {
                throw new WorkloadException(
                    $"Unexpected profile definition.The action in the profile does not contain the " +
                    $"required value for'{nameof(this.MinTortureFFT)}' arguments defined. {nameof(this.MinTortureFFT)} should be greater than 0",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (this.MaxTortureFFT <= this.MinTortureFFT)
            {
                throw new WorkloadException(
                    $"Unexpected profile definition.The action in the profile does not contain the " +
                    $"required value for '{nameof(this.MaxTortureFFT)}' arguments defined. {nameof(this.MaxTortureFFT)} should be greater than {nameof(this.MinTortureFFT)}",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (this.FFTConfiguration < 0 || this.FFTConfiguration > 3)
            {
                throw new WorkloadException(
                    $"Unexpected profile definition.The action in the profile does not contain the " +
                    $"required value for '{nameof(this.FFTConfiguration)}' arguments defined. Expected Range of {nameof(this.FFTConfiguration)} is 0-3 " +
                    $"0 -  Custom/Blend, 1- Smallest FFTs, 2- Small FFTs, 3- Large FFTs",
                    ErrorReason.InvalidProfileDefinition);
            }

            if (this.TortureHyperthreading < 0 || this.TortureHyperthreading > 1)
            {
                throw new WorkloadException(
                    $"Unexpected profile definition.The action in the profile does not contain the " +
                    $"required value for '{nameof(this.TortureHyperthreading)}' arguments defined. Expected Range of {nameof(this.TortureHyperthreading)} is 0-1 " +
                    $"0- false, 1- true",
                    ErrorReason.InvalidProfileDefinition);
            }

            // SetDefaultThreadValue if not set and Validate Parameter
            int numberOfLogicalCores = Environment.ProcessorCount;
            if (this.ThreadCount <= 0 ||
                this.ThreadCount > numberOfLogicalCores ||
                (this.ThreadCount > numberOfLogicalCores / 2 && this.TortureHyperthreading == 1))
            {
                switch (this.TortureHyperthreading)
                {
                    case 0:
                        this.ThreadCount = numberOfLogicalCores;
                        break;
                    case 1:
                        this.ThreadCount = numberOfLogicalCores / 2;
                        break;
                }
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the Prime95 workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.packageManager.GetPlatformSpecificPackageAsync(
                this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

            this.PackageDirectory = workloadPackage.Path;

            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    this.ExecutableName = this.PlatformSpecifics.Combine(this.PackageDirectory, "prime95.exe");
                    break;

                case PlatformID.Unix:
                    this.ExecutableName = this.PlatformSpecifics.Combine(this.PackageDirectory, "mprime");
                    break;

                default:
                    throw new WorkloadException(
                        $"The Prime95 workload is not supported on the current platform/architecture " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture)}." +
                        ErrorReason.PlatformNotSupported);
            }

            await this.systemManagement.MakeFileExecutableAsync(this.ExecutableName, this.Platform, cancellationToken)
                .ConfigureAwait(false);

            if (!this.fileSystem.File.Exists(this.ExecutableName))
            {
                throw new DependencyException(
                    $"The expected workload binary/executable was not found in the '{this.PackageName}' package. The workload cannot be executed " +
                    $"successfully without this binary/executable. Check that the workload package was installed successfully and that the executable " +
                    $"exists in the path expected '{this.ExecutableName}'.",
                    ErrorReason.DependencyNotFound);
            }
        }

        /// <summary>
        /// Executes the Prime95 workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string commandLineArguments = this.CommandLine;
            DateTime startTime = DateTime.UtcNow;
            this.ApplyFFTConfiguration();
            this.ValidateParameters();

            await this.ExecuteCommandAsync(
                this.ExecutableName, commandLineArguments, this.PackageDirectory, cancellationToken)
                .ConfigureAwait(false);

            DateTime endTime = DateTime.UtcNow;
            this.LogPrime95Output(startTime, endTime, telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Executes the Prime95 workload command and generates the results file for the logs
        /// </summary>
        private async Task ExecuteCommandAsync(
            string pathToExe,
            string commandLineArguments,
            string workingDirectory,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                string prime95ParameterFilePath = this.PlatformSpecifics.Combine(this.PackageDirectory, "prime.txt");
                this.CreateFileForPrime95Parameters(prime95ParameterFilePath);

                await this.Logger.LogMessageAsync($"{nameof(Prime95Executor)}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(
                        pathToExe,
                        commandLineArguments,
                        workingDirectory))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        System.TimeSpan timeSpanInMins = TimeSpan.FromMinutes(this.TimeInMins);

                        bool procSafelyStopped = false;
                        try
                        {
                            await process.StartAndWaitAsync(cancellationToken, timeSpanInMins)
                                .ConfigureAwait(false);
                        }
                        catch (System.TimeoutException)
                        {
                            if (!process.HasExited)
                            {
                                process.SafeKill();
                                procSafelyStopped = true;
                            }
                        }
                        finally
                        {
                            process.SafeKill();
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogProcessDetails<Prime95Executor>(process, telemetryContext);
                            // ExitCode for SafeKill is -1, not a part of DefaultSuccessCodes
                            if (!procSafelyStopped)
                            {
                                process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.WorkloadFailed);
                            }
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Creates prime.txt file in working directory for providing configuration arguments to prime95
        /// </summary>
        /// <param name="argumentsFilePath">The file path to write the argument file to.</param>
        private void CreateFileForPrime95Parameters(string argumentsFilePath)
        {
            string prime95Arguments = 
                $"ErrorCheck=1\n" +
                $"SumInputsErrorCheck=1\n" +
                $"V24OptionsConverted=1\n" +
                $"TortureHyperthreading={this.TortureHyperthreading}\n" +
                $"StressTester=1\n" +
                $"TortureThreads={this.ThreadCount}\n" +
                $"MinTortureFFT={this.MinTortureFFT}\n" +
                $"MaxTortureFFT={this.MaxTortureFFT}\n" +
                $"TortureTime=5\n" +
                $"UsePrimenet=0\n";

            this.fileSystem.File.WriteAllText(argumentsFilePath, prime95Arguments);

            if (!this.fileSystem.File.Exists(argumentsFilePath))
            {
                throw new WorkloadException(
                    "The Prime95 workload couldn't create the prime.txt for setting arguments and configurations of WL.",
                    ErrorReason.WorkloadDependencyMissing);
            }
        }

        /// <summary>
        /// Sets MinTortureFFT and MaxTortureFFT as per FFTConfiguration provided
        /// FFTConfiguration 0: Custom values or Default Values (4K-8192K)
        /// FFTConfiguration 1: Smallest FFT values (4K-32K)
        /// FFTConfiguration 2: Small FFT values (32K-1024K)
        /// FFTConfiguration 3: Large FFT values (2048K-8192K)
        /// </summary>
        private void ApplyFFTConfiguration()
        {
            switch (this.FFTConfiguration)
            {
                case 0:
                    break;
                case 1:
                    this.MinTortureFFT = 4;
                    this.MaxTortureFFT = 32;
                    break;
                case 2:
                    this.MinTortureFFT = 32;
                    this.MaxTortureFFT = 1024;
                    break;
                case 3:
                    this.MinTortureFFT = 2048;
                    this.MaxTortureFFT = 8192;
                    break;
            }
        }

        /// <summary>
        /// Logs the Prime95 workload metrics.
        /// </summary>
        private void LogPrime95Output(
            DateTime startTime,
            DateTime endTime,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                string resultsPath = this.PlatformSpecifics.Combine(this.PackageDirectory, "results.txt");
                if (!this.fileSystem.File.Exists(resultsPath))
                {
                    throw new WorkloadResultsException(
                        $"The Prime95 results file was not found at path '{resultsPath}'.",
                        ErrorReason.WorkloadResultsNotFound);
                }

                string rawText = this.fileSystem.File.ReadAllText(resultsPath);

                if (string.IsNullOrWhiteSpace(rawText))
                {
                    throw new WorkloadResultsException(
                        "The Prime95 workload did not produce valid results.",
                        ErrorReason.WorkloadResultsNotFound);
                }

                Prime95MetricsParser parser = new Prime95MetricsParser(rawText);
                IList<Metric> workloadMetrics = parser.Parse();
                double runTimeInSeconds = endTime.Subtract(startTime).TotalSeconds;
                workloadMetrics.Add(new Metric("testTime", runTimeInSeconds, "seconds", MetricRelativity.HigherIsBetter));

                foreach (Metric metric in workloadMetrics)
                {
                    this.Logger.LogMetrics(
                        "Prime95",
                        // example Scenario: ApplyStress_60mins_4K-8192K_8threads
                        scenarioName: this.Scenario + "_" + this.TimeInMins + "mins_" + this.MinTortureFFT + "K-" + this.MaxTortureFFT + "K_" + this.ThreadCount + "threads",
                        startTime,
                        endTime,
                        metric.Name,
                        metric.Value,
                        metric.Unit,
                        metricCategorization: "Prime95",
                        scenarioArguments: this.CommandLine,
                        this.Tags,
                        telemetryContext,
                        metric.Relativity);
                }

                this.fileSystem.File.Delete(resultsPath);
            }
        }
    }
}