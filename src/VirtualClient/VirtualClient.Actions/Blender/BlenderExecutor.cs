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
    /// Executes the Blender workload.
    /// </summary>
    [WindowsCompatible]
    public class BlenderExecutor : VirtualClientComponent
    {
        private const string ResultFilePrefix = "blender_results";
        private const string BlenderExecutableName = "benchmark-launcher-cli.exe";
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Constructor for <see cref="BlenderExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public BlenderExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.systemManagement = dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The Scenes that will be run by Blender.
        /// </summary>
        public string Scenes
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(BlenderExecutor.Scenes));
            }
        }

        /// <summary>
        /// The Blender Version being used.
        /// </summary>
        public string BlenderVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(BlenderExecutor.BlenderVersion));
            }
        }

        /// <summary>
        /// The device types to be tested on.
        /// </summary>
        public string[] DeviceTypes
        {
            get
            {
                // Trim and remove any whitespaces
                return this.Parameters.GetValue<string>(nameof(BlenderExecutor.DeviceTypes)).Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }

        /// <summary>
        /// The path to the blender-benchmark-cli.exe.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// Defines the path to the Blender package that contains the workload
        /// executable.
        /// </summary>
        protected DependencyPath Package { get; set; }

        /// <summary>
        /// Initializes the environment
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            PlatformSpecifics.ThrowIfNotSupported(this.Platform);

            await this.InitializePackageLocationAsync(cancellationToken)
                .ConfigureAwait(false);

            this.ExecutablePath = this.PlatformSpecifics.Combine(this.Package.Path, BlenderExecutableName);
        }

        /// <summary>
        /// Executes the Blender workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            var metrics = new List<Metric>();

            string[] commandArgumentsArray = this.GenerateCommandArgumentsArray();

            foreach (string commandArguments in commandArgumentsArray)
            {
                EventContext relatedContext = telemetryContext.Clone()
                .AddContext("executable", this.ExecutablePath)
                .AddContext("commandArguments", commandArguments);

                using (IProcessProxy process = await this.ExecuteCommandAsync(this.ExecutablePath, commandArguments, this.Package.Path, relatedContext, cancellationToken).ConfigureAwait(false))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext);
                        process.ThrowIfWorkloadFailed();

                        // Blender benchmark's results are outputted to Stdout directly as a json.
                        this.CaptureMetrics(process, commandArguments, relatedContext, process.StandardOutput.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = base.IsSupported()
                && this.Platform == PlatformID.Win32NT
                && this.CpuArchitecture == Architecture.X64;

            if (!isSupported)
            {
                this.Logger.LogNotSupported("Blender", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        /// <summary>
        /// Processes benchmark results
        /// </summary>
        private void CaptureMetrics(IProcessProxy workloadProcess, string commandArguments, EventContext telemetryContext, string resultsContent)
        {
            if (workloadProcess.ExitCode == 0)
            {
                try
                {
                    if (resultsContent == null)
                    {
                        throw new WorkloadResultsException(
                            "No results output from Blender Benchmark",
                            ErrorReason.WorkloadResultsNotFound);
                    }

                    BlenderMetricsParser resultsParser = new BlenderMetricsParser(resultsContent);
                    var metrics = resultsParser.Parse();

                    this.MetadataContract.AddForScenario(
                           this.Scenario,
                           workloadProcess.FullCommand(),
                           toolVersion: "3.1.0");
                    this.MetadataContract.Apply(telemetryContext);

                    this.Logger.LogMetrics(
                        "Blender",
                        this.Scenario,
                        workloadProcess.StartTime,
                        workloadProcess.ExitTime,
                        metrics,
                        null,
                        commandArguments,
                        this.Tags,
                        telemetryContext);
                }
                catch (SchemaException exc)
                {
                    EventContext relatedContext = telemetryContext.Clone()
                        .AddError(exc);

                    this.Logger.LogMessage($"{nameof(BlenderExecutor)}.WorkloadOutputParsingFailed", LogLevel.Warning, relatedContext);
                }
            }
        }

        /// <summary>
        /// Validate the Blender Package
        /// </summary>
        private async Task InitializePackageLocationAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                DependencyPath workloadPackage = await this.systemManagement.PackageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                    .ConfigureAwait(false) ?? throw new DependencyException(
                        $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                        ErrorReason.WorkloadDependencyMissing);
                this.Package = this.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);
            }
        }

        /// <summary>
        /// Generate the Blender Command Arguments in an array. Each entry is a different set of cmd args.
        /// </summary>
        private string[] GenerateCommandArgumentsArray()
        {
            string[] commandArgumentsArray = new string[this.DeviceTypes.Length];
            string commandArgument;

            // Blender benchmark needs to run once for each device type.
            for (int i = 0; i < this.DeviceTypes.Length; i++)
            {
                // --verbosity 3 gives detailed error messsage if Blender fails to run
                commandArgument = $"benchmark --blender-version {this.BlenderVersion} --device-type {this.DeviceTypes[i]} {this.Scenes} --json --verbosity 3";
                commandArgumentsArray[i] = commandArgument;
            }

            return commandArgumentsArray;
        }
    }
}