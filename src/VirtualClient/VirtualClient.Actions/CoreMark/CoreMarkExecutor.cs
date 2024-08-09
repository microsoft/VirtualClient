// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The CoreMark workload executor.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class CoreMarkExecutor : VirtualClientComponent
    {
        private const string CoreMarkOutputFile1 = "run1.log";
        private const string CoreMarkOutputFile2 = "run2.log";

        private ISystemManagement systemManagement;
        private IPackageManager packageManager;

        /// <summary>
        /// Constructor for <see cref="CoreMarkExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public CoreMarkExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
        }

        /// <summary>
        /// The name of the compiler used to compile the CoreMark workload.
        /// </summary>
        public string CompilerName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CompilerName), string.Empty);
            }
        }

        /// <summary>
        /// The version of the compiler used to compile the CoreMark workload.
        /// </summary>
        public string CompilerVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CompilerVersion), string.Empty);
            }
        }

        /// <summary>
        /// Allos overwrite to Coremark process thread count. 
        /// </summary>
        public int ThreadCount
        {
            get
            {
                // Default to system logical core count, but overwritable with parameters.
                CpuInfo cpuInfo = this.systemManagement.GetCpuInfoAsync(CancellationToken.None).GetAwaiter().GetResult();
                int threadCount = cpuInfo.LogicalProcessorCount;

                if (this.Parameters.TryGetValue(nameof(this.ThreadCount), out IConvertible value) && value != null)
                {
                    threadCount = value.ToInt32(CultureInfo.InvariantCulture);
                }

                return threadCount;
            }
        }

        /// <summary>
        /// The path to CoreMark output file #1
        /// </summary>
        protected string PackagePath { get; set; }

        /// <summary>
        /// The path to CoreMark output file #1
        /// </summary>
        protected string OutputFile1Path { get; set; }

        /// <summary>
        /// The path to CoreMark output file #2
        /// </summary>
        protected string OutputFile2Path { get; set; }

        /// <summary>
        /// Executes CoreMark
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string argument = this.GetCommandLineArguments();
                string output = string.Empty;
                switch (this.Platform)
                {
                    case PlatformID.Unix:
                        using (IProcessProxy process = await this.ExecuteCommandAsync("make", argument, this.PackagePath, telemetryContext, cancellationToken))
                        {
                            await this.CaptureMetricsAsync(process, argument, telemetryContext, cancellationToken);
                        }

                        break;

                    case PlatformID.Win32NT:
                        DependencyPath cygwinPackage = await this.packageManager.GetPackageAsync("cygwin", CancellationToken.None)
                            .ConfigureAwait(false);

                        using (IProcessProxy process = await this.ExecuteCygwinBashAsync($"make {argument}", this.PackagePath, cygwinPackage.Path, telemetryContext, cancellationToken))
                        {
                            await this.CaptureMetricsAsync(process, argument, telemetryContext, cancellationToken);
                        }

                        break;
                }
            }
        }

        /// <summary>
        /// Initializes the workload environment.
        /// </summary>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.PackagePath = this.GetPackagePath(this.PackageName);
            this.OutputFile1Path = this.Combine(this.PackagePath, CoreMarkExecutor.CoreMarkOutputFile1);
            this.OutputFile2Path = this.Combine(this.PackagePath, CoreMarkExecutor.CoreMarkOutputFile2);

            return Task.CompletedTask;
        }

        private string GetCommandLineArguments()
        {
            return @$"XCFLAGS=""-DMULTITHREAD={this.ThreadCount} -DUSE_PTHREAD"" REBUILD=1 LFLAGS_END=-pthread";
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    this.MetadataContract.AddForScenario(
                       "CoreMark",
                       commandArguments,
                       toolVersion: null);

                    this.MetadataContract.Apply(telemetryContext);

                    IEnumerable<string> results = await this.LoadResultsAsync(
                        new string[] { this.OutputFile1Path, this.OutputFile2Path },
                        cancellationToken);

                    foreach (string result in results)
                    {
                        if (process.IsErrored())
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "CoreMark", logToFile: true);
                            process.ThrowIfWorkloadFailed();
                        }

                        await this.LogProcessDetailsAsync(process, telemetryContext, "CoreMark", results: result.AsArray(), logToFile: true);

                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            CoreMarkMetricsParser parser = new CoreMarkMetricsParser(result);
                            IList<Metric> metrics = parser.Parse();

                            this.Logger.LogMetrics(
                                toolName: "CoreMark",
                                scenarioName: "CoreMark",
                                process.StartTime,
                                process.ExitTime,
                                metrics,
                                null,
                                commandArguments,
                                this.Tags,
                                telemetryContext);
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                this.Logger.LogErrorMessage(exc, EventContext.Persisted());
            }
        }
    }
}