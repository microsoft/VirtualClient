﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Class encapsulating logic to execute and collect metrics for
    /// NASA Advanced Supercomputing parallel benchmarks. (NAS parallel benchmarks)
    /// </summary>
    [UnixCompatible]
    public class NASParallelBenchExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NASParallelBenchExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public NASParallelBenchExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Path to NAS Parallel Benchmark Package.
        /// </summary>
        protected string PackagePath { get; set; }

        /// <summary>
        /// An interface that can be used to communicate with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManager => this.Dependencies.GetService<ISystemManagement>();

        /// <summary>
        /// Nas Parallel Bench state defines whether it's build setup completed or not.
        /// </summary>
        protected State NpbBuildState { get; set; } = new State(new Dictionary<string, IConvertible>
        {
            [nameof(NpbBuildState)] = "completed"
        });

        /// <summary>
        /// Initializes the environment and dependencies for running the NAS Parallel workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.CheckDistroSupportAsync(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
            DependencyPath workloadPackage = await packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                .ConfigureAwait(false);

            this.PackagePath = workloadPackage.Path;

            await this.BuildBinariesAsync(telemetryContext, cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Executes NAS parallel benchmark.
        /// </summary>
        protected async override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!this.IsMultiRoleLayout() || this.IsInRole(ClientRole.Server))
            {
                using (var nasParallelBenchServerExecutor = this.CreateNASParallelBenchServer())
                {
                    await nasParallelBenchServerExecutor.ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);

                    this.Logger.LogMessage($"{nameof(NASParallelBenchExecutor)}.ServerExecutionCompleted", telemetryContext);
                }
            }

            if (!this.IsMultiRoleLayout() || this.IsInRole(ClientRole.Client))
            {
                using (var nasParallelBenchClientExecutor = this.CreateNASParallelBenchClient())
                {
                    await nasParallelBenchClientExecutor.ExecuteAsync(cancellationToken)
                        .ConfigureAwait(false);

                    this.Logger.LogMessage($"{nameof(NASParallelBenchExecutor)}.ClientExecutionCompleted", telemetryContext);
                }
            }

            // Keep the server-running if we are in a multi-role/system topology and this
            // instance of VC is the Server role.
            if (this.IsMultiRoleLayout() && this.IsInRole(ClientRole.Server))
            {
                this.Logger.LogMessage($"{nameof(NASParallelBenchExecutor)}.KeepServerAlive", telemetryContext);
                await this.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Get new NAS Parallel Bench client instance.
        /// </summary>
        protected virtual VirtualClientComponent CreateNASParallelBenchClient()
        {
            return new NASParallelBenchClientExecutor(this.Dependencies, this.Parameters);
        }

        /// <summary>
        /// Get new NAS Parallel Bench server instance.
        /// </summary>
        protected virtual VirtualClientComponent CreateNASParallelBenchServer()
        {
            return new NASParallelBenchServerExecutor(this.Dependencies, this.Parameters);
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = base.IsSupported()
                && (this.Platform == PlatformID.Unix)
                && (this.CpuArchitecture == Architecture.X64 || this.CpuArchitecture == Architecture.Arm64);

            if (!isSupported)
            {
                this.Logger.LogNotSupported("NASParallel", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        /// <summary>
        /// Gets name of the benchmark directory based on single or multi role layout.
        /// </summary>
        protected string GetBenchmarkDirectory()
        {
            string path = this.PackagePath;
            if (this.IsMultiRoleLayout())
            {
                // Uses MPI (Message Passing Interface) in MultiVM Scenario for Parallel processing.
                // Multiple processes used for Parallel processing.
                path = this.PlatformSpecifics.Combine(path, "NPB-MPI");
            }
            else
            {
                // Uses OMP (Open Multi-Processing) in Single VM Scenario for Parallel processing.
                // Multiple threads used for Parallel processing.
                path = this.PlatformSpecifics.Combine(path, "NPB-OMP");
            }

            return path;
        }

        private async Task BuildBinariesAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();
            var apiClient = clientManager.GetOrCreateApiClient(IPAddress.Loopback.ToString(), IPAddress.Loopback);

            HttpResponseMessage response = await apiClient.GetStateAsync(nameof(this.NpbBuildState), cancellationToken)
               .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                using (IProcessProxy makeProcess = this.SystemManager.ProcessManager.CreateElevatedProcess(this.Platform, "bash", $"-c \"make suite\"", this.GetBenchmarkDirectory()))
                {
                    await makeProcess.StartAndWaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(makeProcess, telemetryContext)
                            .ConfigureAwait();

                        makeProcess.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                    }
                }

                response = await apiClient.CreateStateAsync(nameof(this.NpbBuildState), JObject.FromObject(this.NpbBuildState), cancellationToken)
                    .ConfigureAwait(false);

                response.ThrowOnError<WorkloadException>();
            }
        }

        private async Task CheckDistroSupportAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                var linuxDistributionInfo = await this.SystemManager.GetLinuxDistributionAsync(cancellationToken)
                .ConfigureAwait(false);

                switch (linuxDistributionInfo.LinuxDistribution)
                {
                    case LinuxDistribution.Ubuntu:
                    case LinuxDistribution.Debian:
                        break;
                    default:
                        throw new WorkloadException(
                            $"The NAS parallel benchmark workload is not supported on the current Linux distro - " +
                            $"{linuxDistributionInfo.LinuxDistribution.ToString()}.  Supported distros include:" +
                            $" Ubuntu, Debian. ",
                            ErrorReason.LinuxDistributionNotSupported);
                }
            }
        }
    }
}
