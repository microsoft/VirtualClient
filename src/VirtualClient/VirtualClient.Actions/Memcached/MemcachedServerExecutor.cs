// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// MemcachedMemtier Server Executor
    /// </summary>
    public class MemcachedServerExecutor : MemcachedExecutor
    {
        private IProcessProxy process;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemcachedServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public MemcachedServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ServerCopies = Environment.ProcessorCount;
            this.StateManager = this.Dependencies.GetService<IStateManager>();
        }

        /// <summary>
        /// Parameter defines the name of the package that contains the benchmark workload
        /// toolsets (e.g. memtier).
        /// </summary>
        public string BenchmarkPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.BenchmarkPackageName));
            }
        }

        /// <summary>
        /// Parameter defines whether to bind the Memcached server process to cores on the system.
        /// </summary>
        public int Bind
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Bind));
            }
        }

        /// <summary>
        /// The size (in megabytes) to use for caching items in memory for the Memcached
        /// server.
        /// </summary>
        public int ServerMemoryCacheSizeInMB
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(MemcachedServerExecutor.ServerMemoryCacheSizeInMB));
            }
        }

        /// <summary>
        /// Number of copies of Memcached server instances to be created.
        /// </summary>
        protected int ServerCopies { get; set; }

        /// <summary>
        /// Provides access to the local state management facilities.
        /// </summary>
        protected IStateManager StateManager { get; }

        /// <summary>
        /// Path to the benchmark executable (e.g. memtier_benchmark).
        /// </summary>
        protected string BenchmarkExecutablePath { get; set; }

        /// <summary>
        /// Path to benchmark workload package used to warmup the server (e.g. memtier).
        /// </summary>
        protected string BenchmarkPackagePath { get; set; }

        /// <summary>
        /// Path to Memcached server executable.
        /// </summary>
        protected string MemcachedExecutablePath { get; set; }

        /// <summary>
        /// Path to Memcached server package.
        /// </summary>
        protected string MemcachedPackagePath { get; set; }

        /// <summary>
        /// Cancellation Token Source for Server.
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(MemcachedExecutor)}.ExecuteServer", telemetryContext, async () =>
            {
                using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    await this.DeleteWorkloadStateAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                    await this.SaveServerCopyStateAsync(cancellationToken).ConfigureAwait(false);

                    if (!this.IsMultiRoleLayout())
                    {
                        await this.ExecuteServerWorkload(cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        try
                        {
                            await this.ExecuteServerWorkload(cancellationToken).ConfigureAwait(false);
                            IEnumerable<IProcessProxy> memcachedProcessProxyList = GetRunningProcessByName("memcached");

                            this.Logger.LogTraceMessage($"API server workload online awaiting client requests...");

                            this.SetServerOnline(true);

                            if (memcachedProcessProxyList != null)
                            {
                                foreach (IProcessProxy process in memcachedProcessProxyList)
                                {
                                    this.CleanupTasks.Add(() => process.SafeKill());
                                    await process.WaitForExitAsync(cancellationToken)
                                        .ConfigureAwait(false);
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                        }
                        finally
                        {
                            // Always signal to clients that the server is offline before exiting. This helps to ensure that the client
                            // and server have consistency in handshakes even if one side goes down and returns at some other point.
                            this.SetServerOnline(false);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken);
            DependencyPath memcachedPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            DependencyPath benchmarkPackage = await this.GetPlatformSpecificPackageAsync(this.BenchmarkPackageName, cancellationToken);

            this.MemcachedPackagePath = memcachedPackage.Path;
            this.BenchmarkPackagePath = benchmarkPackage.Path;

            this.MemcachedExecutablePath = this.Combine(this.MemcachedPackagePath, "memcached");
            this.BenchmarkExecutablePath = this.Combine(this.BenchmarkPackagePath, "memtier_benchmark");

            await this.SystemManagement.MakeFileExecutableAsync(this.MemcachedExecutablePath, this.Platform, cancellationToken);
            await this.SystemManagement.MakeFileExecutableAsync(this.BenchmarkExecutablePath, this.Platform, cancellationToken);

            this.InitializeApiClients();
        }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (!this.disposed)
                {
                    this.process?.Dispose();
                    this.disposed = true;
                }
            }
        }

        private async Task ExecuteServerWorkload(CancellationToken cancellationToken)
        {
            await this.KillProcessesAsync("memcached", cancellationToken);

            for (int i = 0; i < this.ServerCopies; i++)
            {
                int port = this.Port + i;
                string precommand = string.Empty;

                if (this.Bind == 1)
                {
                    int core = i;
                    precommand = $"numactl -C {core}";
                }

                // https://docs.oracle.com/cd/E17952_01/mysql-5.6-en/ha-memcached-cmdline-options.html#:~:text=Set%20the%20amount%20of%20memory%20allocated%20to%20memcached,amount%20of%20RAM%20to%20be%20allocated%20%28in%20megabytes%29.

                string startservercommand = $"-u {this.Username} bash -c \"{precommand} {this.MemcachedExecutablePath} -d -p {port} -t 4 -m {this.ServerMemoryCacheSizeInMB}\"";

                this.Logger.LogTraceMessage($"Executing process '{startservercommand}'  at directory '{this.MemcachedPackagePath}'.");
                this.process = this.ProcessManager.CreateElevatedProcess(this.Platform, startservercommand, null, this.MemcachedPackagePath);

                if (!this.process.Start())
                {
                    throw new WorkloadException($"The Memcached server did not start as expected.", ErrorReason.WorkloadFailed);
                }

                await this.WarmUpServer(port, cancellationToken);

            }
        }

        private async Task WarmUpServer(int port, CancellationToken cancellationToken)
        {
            await this.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken);

            string warmupCommand = 
                $"-u {this.Username} {this.BenchmarkExecutablePath} --protocol=memcache_text --server localhost --port={port} -c 1 -t 1 " +
                $"--pipeline 100 --data-size=32 --key-minimum=1 --key-maximum=10000000 --ratio=1:0 --requests=allkeys";

            await this.ExecuteCommandAsync(warmupCommand, null, this.BenchmarkPackagePath, cancellationToken)
                .ConfigureAwait(false);
        }

        private Task SaveServerCopyStateAsync(CancellationToken cancellationToken)
        {
            return this.ServerApiClient.UpdateStateAsync(
                nameof(ServerState),
                new Item<ServerState>(nameof(ServerState), new ServerState
                {
                    ServerCopies = this.ServerCopies
                }),
                cancellationToken);
        }

        /// <summary>
        /// Deletes the existing states.
        /// </summary>
        /// <param name="telemetryContext">Provides context information to include with telemetry events emitted.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        private Task DeleteWorkloadStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(MemcachedServerExecutor)}.ResetState", telemetryContext, async () =>
            {
                HttpResponseMessage response = await this.ServerApiClient.DeleteStateAsync(nameof(ServerState), cancellationToken)
                    .ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    response.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
                }
            });
        }
    }
}
