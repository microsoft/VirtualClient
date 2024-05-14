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
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;
    using static System.Net.Mime.MediaTypeNames;

    /// <summary>
    /// The AspNetBench workload executor.
    /// </summary>
    public abstract class AspNetBenchBaseExecutor : VirtualClientMultiRoleComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManagement;

        private string dotnetExePath;
        private string aspnetBenchDirectory;
        private string aspnetBenchDllPath;
        private string bombardierFilePath;
        private string wrkFilePath;
        private string serverArgument;
        private string clientArgument;

        /// <summary>
        /// Constructor for <see cref="AspNetBenchExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public AspNetBenchBaseExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
                return this.Parameters.GetValue<string>(nameof(AspNetBenchBaseExecutor.TargetFramework)).ToLower();
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
                return this.Parameters.GetValue<string>(nameof(AspNetBenchBaseExecutor.Port), "9876");
            }
        }

        /// <summary>
        /// The name of the package where the bombardier package is downloaded.
        /// </summary>
        public string BombardierPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(AspNetBenchBaseExecutor.BombardierPackageName), "bombardier");
            }
        }

        /// <summary>
        /// The name of the package where the DotNetSDK package is downloaded.
        /// </summary>
        public string DotNetSdkPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(AspNetBenchBaseExecutor.DotNetSdkPackageName), "dotnetsdk");
            }
        }

        /// <summary>
        /// ASPNETCORE_threadCount
        /// </summary>
        public string AspNetCoreThreadCount
        {
            get
            {
                // Lower case to prevent build path issue.
                return this.Parameters.GetValue<string>(nameof(AspNetBenchBaseExecutor.AspNetCoreThreadCount), 1);
            }
        }

        /// <summary>
        /// DOTNET_SYSTEM_NET_SOCKETS_THREAD_COUNT
        /// </summary>
        public string DotNetSystemNetSocketsThreadCount
        {
            get
            {
                // Lower case to prevent build path issue.
                return this.Parameters.GetValue<string>(nameof(AspNetBenchBaseExecutor.DotNetSystemNetSocketsThreadCount), 1);
            }
        }

        /// <summary>
        /// wrk commandline
        /// </summary>
        public string WrkCommandLine
        {
            get
            {
                // Lower case to prevent build path issue.
                return this.Parameters.GetValue<string>(nameof(AspNetBenchBaseExecutor.WrkCommandLine), string.Empty);
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the AspNetBench workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // This workload needs three packages: aspnetbenchmarks, dotnetsdk, bombardier
            DependencyPath workloadPackage = await this.packageManager.GetPackageAsync(this.PackageName, CancellationToken.None)
                .ConfigureAwait(false);

            if (workloadPackage != null)
            {
                // the directory we are looking for is at the src/Benchmarks
                this.aspnetBenchDirectory = this.Combine(workloadPackage.Path, "src", "Benchmarks");
            }

            DependencyPath bombardierPackage = await this.packageManager.GetPlatformSpecificPackageAsync(this.BombardierPackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

            if (bombardierPackage != null)
            {
                this.bombardierFilePath = this.Combine(bombardierPackage.Path, this.Platform == PlatformID.Unix ? "bombardier" : "bombardier.exe");
                await this.systemManagement.MakeFileExecutableAsync(this.bombardierFilePath, this.Platform, cancellationToken)
                    .ConfigureAwait(false);
            }

            DependencyPath wrkPackage = await this.packageManager.GetPackageAsync("wrk", cancellationToken)
                .ConfigureAwait(false);

            if (wrkPackage != null)
            {
                this.wrkFilePath = this.Combine(wrkPackage.Path, "wrk");
                await this.systemManagement.MakeFileExecutableAsync(this.wrkFilePath, this.Platform, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        /// <exception cref="DependencyException"></exception>
        protected async Task BuildAspNetBenchAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath dotnetSdkPackage = await this.packageManager.GetPackageAsync(this.DotNetSdkPackageName, CancellationToken.None)
                .ConfigureAwait(false);

            if (dotnetSdkPackage == null)
            {
                throw new DependencyException(
                    $"The expected DotNet SDK package does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            this.dotnetExePath = this.Combine(dotnetSdkPackage.Path, this.Platform == PlatformID.Unix ? "dotnet" : "dotnet.exe");
            // ~/vc/packages/dotnet/dotnet build -c Release -p:BenchmarksTargetFramework=net8.0
            // Build the aspnetbenchmark project
            string buildArgument = $"build -c Release -p:BenchmarksTargetFramework={this.TargetFramework}";
            await this.ExecuteCommandAsync(this.dotnetExePath, buildArgument, this.aspnetBenchDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            // "C:\Users\vcvmadmin\Benchmarks\src\Benchmarks\bin\Release\net8.0\Benchmarks.dll"
            this.aspnetBenchDllPath = this.Combine(
                this.aspnetBenchDirectory,
                "bin",
                "Release",
                this.TargetFramework,
                "Benchmarks.dll");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="process"></param>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <exception cref="WorkloadResultsException"></exception>
        protected void CaptureMetrics(IProcessProxy process, EventContext telemetryContext)
        {
            try
            {
                this.MetadataContract.AddForScenario(
                    "AspNetBench",
                    $"{this.clientArgument},{this.serverArgument}",
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);

                WrkMetricParser parser = new WrkMetricParser(process.StandardOutput.ToString());

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected Task StartAspNetServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // Example:
            // dotnet <path_to_binary>\Benchmarks.dll --nonInteractive true --scenarios json --urls http://localhost:5000 --server Kestrel --kestrelTransport Sockets --protocol http
            // --header "Accept: application/json,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7" --header "Connection: keep-alive" 

            string options = $"--nonInteractive true --scenarios json --urls http://*:{this.Port} --server Kestrel --kestrelTransport Sockets --protocol http";
            string headers = @"--header ""Accept: application/json,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7"" --header ""Connection: keep-alive""";
            this.serverArgument = $"{this.aspnetBenchDllPath} {options} {headers}";

            return this.ExecuteCommandAsync(this.dotnetExePath, this.serverArgument, this.aspnetBenchDirectory, telemetryContext, cancellationToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected async Task RunBombardierAsync(string ipAddress, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                // https://pkg.go.dev/github.com/codesenberg/bombardier
                // ./bombardier --duration 15s --connections 256 --timeout 10s --fasthttp --insecure -l http://localhost:5000/json --print r --format json
                this.clientArgument = $"--duration 15s --connections 256 --timeout 10s --fasthttp --insecure -l http://{ipAddress}:{this.Port}/json --print r --format json";

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected async Task RunWrkAsync(string ipAddress, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                // https://pkg.go.dev/github.com/codesenberg/bombardier
                // ./wrk -t 256 -c 256 -d 15s --timeout 10s http://10.1.0.23:9876/json --header "Accept: application/json,text/html;q=0.9,application/xhtml+xml;q=0.9,application/xml;q=0.8,*/*;q=0.7"
                this.clientArgument = this.WrkCommandLine;
                this.clientArgument = this.clientArgument.Replace("{ipAddress}", ipAddress);
                this.clientArgument = this.clientArgument.Replace("{port}", this.Port);

                using (IProcessProxy process = await this.ExecuteCommandAsync(this.wrkFilePath, this.clientArgument, this.aspnetBenchDirectory, telemetryContext, cancellationToken, runElevated: true)
                    .ConfigureAwait(false))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "wrk", logToFile: true);

                        process.ThrowIfWorkloadFailed();
                        this.CaptureMetrics(process, telemetryContext);
                    }
                }
            }
        }
    }
}