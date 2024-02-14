// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Platform;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Executes the SPEC Power workload used to stress the system.
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class SPECPowerExecutor : VirtualClientComponent
    {
        private readonly SPECPowerDirector directorComponent;
        private readonly SPECPowerCCSServer serverComponent;
        private readonly SPECPowerSSJClient clientComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="SPECPowerExecutor"/> class.
        /// </summary>
        public SPECPowerExecutor(
            IServiceCollection dependencies,
            IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.directorComponent = new SPECPowerDirector(dependencies, parameters);
            this.serverComponent = new SPECPowerCCSServer(dependencies, parameters);
            this.clientComponent = new SPECPowerSSJClient(dependencies, parameters);
            this.GetAllProcessComponents().ForEach(component => component.ActionOwner = this); // Setup each component to know this is the parent. (The readability gain is worth the coupling.)
        }

        /// <summary>
        /// Gets or sets the Java executablePath
        /// </summary>
        public static string JavaExecutablePath { get; set; }

        /// <summary>
        /// The path to the SPECPower workload package.
        /// </summary>
        public string WorkloadPackagePath { get; set; }

        /// <summary>
        /// Whether or not process affinity should be set for SSJ instances.
        /// </summary>
        public bool ShouldSetProcessAffinity
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(SPECPowerExecutor.ShouldSetProcessAffinity), false);
            }
        }

        /// <summary>
        /// Gets the number of instances to spin up.
        /// -1 means calculate the amount instead of using an override.
        /// </summary>
        public int OverrideInstanceCount
        {
            get
            {
                return this.Parameters.GetValue<int>("InstanceCount", -1);
            }
        }

        /// <summary>
        /// Gets the package name of the JavaJDK
        /// </summary>
        public string JdkPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>("JdkPackageName", "microsoft-jdk");
            }
        }

        /// <summary>
        /// Gets the number of logical processors to tell SPEC Power to use.
        /// </summary>
        public int ProcessorCount
        {
            get
            {
                return this.Parameters.GetValue<int>("ProcessorCount", -1);
            }
        }

        /// <summary>
        /// The load percentage sequence which SPEC power will run with. SPEC Power configuration 'input.load_level.percentage_sequence'.
        /// </summary>
        public string LoadPercentageSequence
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SPECPowerExecutor.LoadPercentageSequence));
            }
        }

        /// <summary>
        /// Gets the configuration argument for the SPEC Power configuration 'input.load_level.length_seconds'.
        /// </summary>
        public string LoadLevelLengthSeconds
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SPECPowerExecutor.LoadLevelLengthSeconds));
            }
        }

        /// <summary>
        /// Executes and monitors the programs which make up SPEC Power.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.GetAllProcessComponents().ForEach(component => component.Cleanup());

            this.Logger.LogTraceMessage($"Beginning SPEC-Power execution.");

            this.MetadataContract.AddForScenario("SPECpower", null);
            this.MetadataContract.Apply(telemetryContext);

            this.serverComponent.ExecuteAsync(cancellationToken);
            this.directorComponent.ExecuteAsync(cancellationToken);
            this.clientComponent.ExecuteAsync(cancellationToken);

            return this.MonitorProcessesAsync(telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Stops execution of SPEC Power.
        /// </summary>
        protected override async Task CleanupAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.CleanupAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            this.GetAllProcessComponents().ForEach(component => component.Cleanup());
        }

        /// <summary>
        /// Initializes the executor dependencies, package locations, etc...
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath workloadPackage = await packageManager.GetPackageAsync(this.PackageName, cancellationToken)
                .ConfigureAwait(false);

            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The SPECPower workload package was not found in the packages directory.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            telemetryContext.AddContext("workloadPackage", workloadPackage);
            this.WorkloadPackagePath = workloadPackage.Path;

            DependencyPath javaExecutable = await packageManager.GetPackageAsync(this.JdkPackageName, CancellationToken.None)
                .ConfigureAwait(false);

            if (javaExecutable == null || !javaExecutable.Metadata.ContainsKey(PackageMetadata.ExecutablePath))
            {
                throw new DependencyException(
                    $"The expected Java executable does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            SPECPowerExecutor.JavaExecutablePath = javaExecutable.Metadata[PackageMetadata.ExecutablePath].ToString();
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = base.IsSupported()
                && (this.Platform == PlatformID.Win32NT || this.Platform == PlatformID.Unix)
                && (this.CpuArchitecture == Architecture.X64 || this.CpuArchitecture == Architecture.Arm64);

            if (!isSupported)
            {
                this.Logger.LogNotSupported("SPECPower", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        /// <summary>
        /// Get a list of all of the processes currently being run.
        /// </summary>
        /// <returns>SPEC Processes</returns>
        private ImmutableList<IProcessProxy> GetAllProcesses()
        {
            List<IProcessProxy> processes = new List<IProcessProxy>();
            processes.AddRange(this.directorComponent.SPECProcesses);
            processes.AddRange(this.serverComponent.SPECProcesses);
            processes.AddRange(this.clientComponent.SPECProcesses);
            return processes.ToImmutableList();
        }

        /// <summary>
        /// Get a list of all of the process components.
        /// </summary>
        /// <returns>SPEC Process Components</returns>
        private ImmutableList<SPECPowerProcess> GetAllProcessComponents()
        {
            List<SPECPowerProcess> processes = new List<SPECPowerProcess>();
            processes.Add(this.directorComponent);
            processes.Add(this.serverComponent);
            processes.Add(this.clientComponent);
            return processes.ToImmutableList();
        }

        /// <summary>
        /// Monitors SPEC Power to ensure it is running.
        /// </summary>
        private async Task MonitorProcessesAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                // VC will be running SpecPower at a constant load at extended duration. Typically a day.
                const int testTimeoutMinutes = 1440;

                ImmutableList<IProcessProxy> allProcesses = this.GetAllProcesses();

                bool testTimedOut = false;
                bool foundErrorOutput = false;
                while (!allProcesses.Any(process => process.HasExited) && !testTimedOut)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    // This is reported as a metric so SPEC Power shows up on the dashboard
                    this.Logger.LogMetrics(
                        "SPECpower",
                        "MonitorProcess",
                        this.StartTime,
                        DateTime.UtcNow,
                        "Heartbeat",
                        1.0,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        this.Tags,
                        telemetryContext,
                        MetricRelativity.HigherIsBetter);

                    await Task.Delay(30000, cancellationToken).ConfigureAwait(false);

                    // If the test hasn't changed in 30 minutes, assume it has failed.
                    testTimedOut = (DateTime.UtcNow - this.serverComponent.LastTestStartTime).TotalMinutes >= testTimeoutMinutes;
                    foundErrorOutput = allProcesses.Any(process => !string.IsNullOrEmpty(process.StandardError.ToString()));
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    // Ensure if the programs are going to exit, they do so of their own accord.
                    await Task.Delay(10000, cancellationToken);

                    // Kill any which didn't shutdown themselves.
                    allProcesses.ForEach(process => process.Kill());
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    if (foundErrorOutput)
                    {
                        allProcesses.ForEach(process => this.Logger.LogMessage(
                            $"{this.TypeName}.ProcessOutput",
                            telemetryContext.Clone().AddContext("output", process.StandardOutput.ToString())));

                        allProcesses.Where(p => !string.IsNullOrEmpty(p.StandardError.ToString())).ToList()
                            .ForEach(process => this.Logger.LogMessage(
                            $"{this.TypeName}.ProcessError",
                            telemetryContext.Clone().AddContext("processError", process.StandardError.ToString())));

                        string processNames = string.Join(',', allProcesses.Where(process => !string.IsNullOrEmpty(process.StandardError.ToString())).Select(process => process.Name));
                        throw new WorkloadException($"SPEC Process(es) '{processNames}' encountered an error.", ErrorReason.WorkloadFailed);
                    }
                    else if (testTimedOut)
                    {
                        allProcesses.ForEach(process => this.Logger.LogMessage(
                            $"{this.TypeName}.ProcessOutput",
                            telemetryContext.Clone().AddContext("output", process.StandardOutput.ToString())));

                        throw new WorkloadException(
                            $"Benchmark '{this.serverComponent.CurrentTestName}' has not completed in {testTimeoutMinutes} minutes. Assuming something has gone wrong.",
                            ErrorReason.WorkloadFailed);
                    }
                    else if (allProcesses.Any(process => process.ExitCode != 0))
                    {
                        allProcesses.ForEach(process => this.Logger.LogMessage(
                            $"{this.TypeName}.ProcessOutput",
                            telemetryContext.Clone().AddContext("output", process.StandardOutput.ToString())));

                        allProcesses.ForEach(process => Console.WriteLine(process.StandardOutput.ToString()));
                        throw new WorkloadException("One or more processes failed with a non-zero exit code!", ErrorReason.WorkloadFailed);
                    }

                    this.Logger.LogTraceMessage("SPEC Power Completed Successfully.");
                }
            }
        }
    }
}