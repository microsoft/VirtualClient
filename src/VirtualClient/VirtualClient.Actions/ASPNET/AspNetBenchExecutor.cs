// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The AspNetBench workload executor.
    /// </summary>
    public class AspNetBenchExecutor : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManagement;

        private string dotnetExePath;
        private string aspnetBenchDirectory;
        private string aspnetBenchDllPath;
        private string bombardierFilePath;
        private string serverArgument;
        private string clientArgument;

        private Action killServer;

        /// <summary>
        /// Constructor for <see cref="AspNetBenchExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public AspNetBenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManagement.PackageManager;
            this.stateManager = this.systemManagement.StateManager;
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <summary>
        /// The name of the package where the AspNetBench package is downloaded.
        /// </summary>
        public string TargetFramework
        {
            get
            {
                // Lower case to prevent build path issue.
                return this.Parameters.GetValue<string>(nameof(AspNetBenchExecutor.TargetFramework)).ToLower();
            }
        }

        /// <summary>
        /// The port for ASPNET to run.
        /// </summary>
        public string Port
        {
            get
            {
                // Lower case to prevent build path issue.
                return this.Parameters.GetValue<string>(nameof(AspNetBenchExecutor.Port), "9876");
            }
        }

        /// <summary>
        /// The name of the package where the bombardier package is downloaded.
        /// </summary>
        public string BombardierPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(AspNetBenchExecutor.BombardierPackageName), "bombardier");
            }
        }

        /// <summary>
        /// The name of the package where the DotNetSDK package is downloaded.
        /// </summary>
        public string DotNetSdkPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(AspNetBenchExecutor.DotNetSdkPackageName), "dotnetsdk");
            }
        }

        /// <summary>
        /// Executes the AspNetBench workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            Task serverTask = this.StartAspNetServerAsync(cancellationToken);
            await this.RunBombardierAsync(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            this.killServer.Invoke();
        }

        /// <summary>
        /// Initializes the environment for execution of the AspNetBench workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // This workload needs three packages: aspnetbenchmarks, dotnetsdk, bombardier
            DependencyPath workloadPackage = await this.packageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                .ConfigureAwait(false);

            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            // the directory we are looking for is at the src/Benchmarks
            this.aspnetBenchDirectory = this.Combine(workloadPackage.Path, "src", "Benchmarks");

            DependencyPath bombardierPackage = await this.packageManager.GetPlatformSpecificPackageAsync(this.BombardierPackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

            this.bombardierFilePath = this.Combine(bombardierPackage.Path, this.Platform == PlatformID.Unix ? "bombardier" : "bombardier.exe");

            await this.systemManagement.MakeFileExecutableAsync(this.bombardierFilePath, this.Platform, cancellationToken)
                .ConfigureAwait(false);

            DependencyPath dotnetSdkPackage = await this.packageManager.GetPackageAsync(this.DotNetSdkPackageName, CancellationToken.None)
                .ConfigureAwait(false);

            if (dotnetSdkPackage == null)
            {
                throw new DependencyException(
                    $"The expected DotNet SDK package does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.dotnetExePath = this.Combine(dotnetSdkPackage.Path, this.Platform == PlatformID.Unix ? "dotnet" : "dotnet.exe");

            // ~/vc/packages/dotnet/dotnet build -c Release -p:BenchmarksTargetFramework=net6.0
            // Build the aspnetbenchmark project
            string buildArgument = $"build -c Release -p:BenchmarksTargetFramework={this.TargetFramework}";
            await this.ExecuteCommandAsync(this.dotnetExePath, buildArgument, this.aspnetBenchDirectory, cancellationToken)
                .ConfigureAwait(false);

            // "C:\Users\vcvmadmin\Benchmarks\src\Benchmarks\bin\Release\net6.0\Benchmarks.dll"
            this.aspnetBenchDllPath = this.Combine(
                this.aspnetBenchDirectory, 
                "bin", 
                "Release", 
                this.TargetFramework, 
                "Benchmarks.dll");
        }

        private void CaptureMetrics(IProcessProxy process, EventContext telemetryContext)
        {
            try
            {
                this.MetadataContract.AddForScenario(
                    "AspNetBench",
                    $"{this.clientArgument},{this.serverArgument}",
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                BombardierMetricsParser parser = new BombardierMetricsParser(process.StandardOutput.ToString());

                this.Logger.LogMetrics(
                    toolName: "AspNetBench",
                    scenarioName: $"ASP.NET_{this.TargetFramework}_Performance",
                    process.StartTime,
                    process.ExitTime,
                    parser.Parse(),
                    metricCategorization: "json",
                    scenarioArguments: $"Client: {this.clientArgument} | Server: {this.serverArgument}",
                    this.Tags,
                    telemetryContext);
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException($"Failed to parse bombardier output.", exc, ErrorReason.InvalidResults);
            }
        }

        private Task StartAspNetServerAsync(CancellationToken cancellationToken)
        {
            // Example:
            // dotnet <path_to_binary>\Benchmarks.dll --nonInteractive true --scenarios json --urls http://localhost:5000 --server Kestrel --kestrelTransport Sockets --protocol http
            // --header "Accept: application/json,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7" --header "Connection: keep-alive" 

            string options = $"--nonInteractive true --scenarios json --urls http://localhost:{this.Port} --server Kestrel --kestrelTransport Sockets --protocol http";
            string headers = @"--header ""Accept: application/json,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7"" --header ""Connection: keep-alive""";
            this.serverArgument = $"{this.aspnetBenchDllPath} {options} {headers}";

            return this.ExecuteCommandAsync(this.dotnetExePath, this.serverArgument, this.aspnetBenchDirectory, cancellationToken, isServer: true);
        }

        private async Task RunBombardierAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                // https://pkg.go.dev/github.com/codesenberg/bombardier
                // ./bombardier --duration 15s --connections 256 --timeout 10s --fasthttp --insecure -l http://localhost:5000/json --print r --format json
                this.clientArgument = $"--duration 15s --connections 256 --timeout 10s --fasthttp --insecure -l http://localhost:{this.Port}/json --print r --format json";

                using (IProcessProxy process = await this.ExecuteCommandAsync(this.bombardierFilePath, this.clientArgument, this.aspnetBenchDirectory, telemetryContext, cancellationToken, runElevated: true)
                    .ConfigureAwait(false))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "AspNetBench", logToFile: true);

                        process.ThrowIfWorkloadFailed();
                        this.CaptureMetrics(process, telemetryContext);
                    }
                }
            }
        }

        private async Task ExecuteCommandAsync(string pathToExe, string commandLineArguments, string workingDirectory, CancellationToken cancellationToken, bool isServer = false)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{pathToExe}' '{commandLineArguments}' at directory '{workingDirectory}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("command", pathToExe)
                    .AddContext("commandArguments", commandLineArguments);

                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, pathToExe, commandLineArguments, workingDirectory))
                {
                    if (isServer)
                    {
                        this.killServer = () => process.SafeKill();
                    }

                    this.CleanupTasks.Add(() => process.SafeKill());
                    await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                        
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext);

                        if (!isServer)
                        {
                            // We will kill the server at the end, exit code is -1, and we don't want it to log as failure.
                            process.ThrowIfWorkloadFailed();
                        }
                    }
                }
            }
        }
    }
}