// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Redis Server Executor
    /// </summary>
    public class RedisServerExecutor : RedisExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public RedisServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ServerCopies = Environment.ProcessorCount;
            this.StateManager = this.Dependencies.GetService<IStateManager>();
        }

        /// <summary>
        /// Parameter defines the name of the package that contains the memtier benchmark workload
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
        /// Parameter defines whether to bind the Redis server process to cores on the system.
        /// </summary>
        public int Bind
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Bind));
            }
        }

        /// <summary>
        /// Path to the benchmark executable (e.g. memtier_benchmark).
        /// </summary>
        protected string BenchmarkExecutablePath { get; set; }

        /// <summary>
        /// Path to benchmark workload package used to warmup the server (e.g. memtier).
        /// </summary>
        protected string BenchmarkPackagePath { get; set; }

        /// <summary>
        /// Path to Redis server executable.
        /// </summary>
        protected string RedisExecutablePath { get; set; }

        /// <summary>
        /// Path to Redis server package.
        /// </summary>
        protected string RedisPackagePath { get; set; }

        /// <summary>
        /// Number of copies of Redis server instances to be created.
        /// </summary>
        protected int ServerCopies { get; set; }

        /// <summary>
        /// Provides access to the local state management facilities.
        /// </summary>
        protected IStateManager StateManager { get; }

        /// <summary>
        /// Executes server side of workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(RedisExecutor)}.ExecuteServer", telemetryContext, async () =>
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
                            IEnumerable<IProcessProxy> processProxyList = this.ProcessesRunning("redis-server");

                            this.Logger.LogTraceMessage($"API server workload online awaiting client requests...");

                            this.SetServerOnline(true);

                            if (processProxyList != null)
                            {
                                foreach (IProcessProxy process in processProxyList)
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
        /// Initializes the environment and dependencies for server of redis workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            DependencyPath redisPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            DependencyPath memtierPackage = await this.GetPlatformSpecificPackageAsync(this.BenchmarkPackageName, CancellationToken.None);

            this.RedisPackagePath = redisPackage.Path;
            this.RedisExecutablePath = this.Combine(this.RedisPackagePath, "src", "redis-server");

            this.BenchmarkPackagePath = memtierPackage.Path;
            this.BenchmarkExecutablePath = this.Combine(this.BenchmarkPackagePath, "memtier_benchmark");

            await this.SystemManagement.MakeFileExecutableAsync(this.RedisExecutablePath, this.Platform, cancellationToken);
            await this.SystemManagement.MakeFileExecutableAsync(this.BenchmarkExecutablePath, this.Platform, cancellationToken);

            this.InitializeApiClients();
        }

        /// <summary>
        /// Returns list of processes running.
        /// </summary>
        /// <param name="processName">Name of the process.</param>
        /// <returns>List of processes running.</returns>
        private IEnumerable<IProcessProxy> ProcessesRunning(string processName)
        {
            IEnumerable<IProcessProxy> processProxyList = null;
            List<Process> processlist = new List<Process>(Process.GetProcesses());
            foreach (Process process in processlist)
            {
                if (process.ProcessName.Contains(processName))
                {
                    Process[] processesByName = Process.GetProcessesByName(process.ProcessName);
                    if (processesByName?.Any() ?? false)
                    {
                        if (processProxyList == null)
                        {
                            processProxyList = processesByName.Select((Process process) => new ProcessProxy(process));
                        }
                        else
                        {
                            foreach (Process proxy in processesByName)
                            {
                                processProxyList.Append(new ProcessProxy(proxy));
                            }
                        }
                    }
                }
            }

            return processProxyList;
        }

        private async Task ExecuteServerWorkload(CancellationToken cancellationToken)
        {
            await this.KillServerWorkload(cancellationToken);

            for (int i = 1; i <= this.ServerCopies; i++)
            {
                int port = this.Port + i - 1;
                string precommand = string.Empty;
                if (this.Bind == 1)
                {
                    int core = i - 1;
                    precommand = $"numactl -C {core}";
                }
                
                // e.g.
                // bash -c "numactl -c 1 /home/user/VirtualClient/linux-x64/packages/redis/src/redis-server --port 6389 --protected-mode no
                //       --ignore-warnings ARM64-COW-BUG --save --io-threads 4 --maxmemory-policy noeviction
                string startservercommand = 
                    $"bash -c \"{precommand} {this.RedisExecutablePath} --port {port} --protected-mode no --ignore-warnings ARM64-COW-BUG --save " +
                    $"--io-threads 4 --maxmemory-policy noeviction\"";

                using (IProcessProxy process = this.SystemManagement.ProcessManager.CreateElevatedProcess(this.Platform, startservercommand, null, this.RedisPackagePath))
                {
                    if (!process.Start())
                    {
                        throw new WorkloadException($"The Redis server did not start as expected.", ErrorReason.WorkloadFailed);
                    }

                    await this.WarmUpServer(port, cancellationToken);
                }
            }
        }

        private async Task KillServerWorkload(CancellationToken cancellationToken)
        {
            await this.ExecuteCommandAsync("pkill -f redis-server", this.RedisPackagePath, cancellationToken)
                .ConfigureAwait(false);

            await this.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken).ConfigureAwait(false);
        }

        private async Task WarmUpServer(int port, CancellationToken cancellationToken)
        {
            await this.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken);

            string warmupCommand =
                $"{this.BenchmarkExecutablePath} --protocol=redis --server localhost --port={port} -c 1 -t 1 --pipeline 100 --data-size=32 --key-minimum=1 --key-maximum=10000000 " +
                $"--ratio=1:0 --requests=allkeys";

            await this.ExecuteCommandAsync(warmupCommand, this.BenchmarkPackagePath, cancellationToken)
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
            return this.Logger.LogMessageAsync($"{nameof(RedisServerExecutor)}.ResetState", telemetryContext, async () =>
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
