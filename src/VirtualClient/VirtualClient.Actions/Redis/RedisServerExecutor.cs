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
            this.StateManager = this.Dependencies.GetService<IStateManager>();
        }

        /// <summary>
        /// Provides access to the local state management facilities.
        /// </summary>
        protected IStateManager StateManager { get; }

        /// <summary>
        /// Initializes the environment and dependencies for server of redis workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            this.Copies = Environment.ProcessorCount.ToString();
            this.InitializeApiClients(); 
        }

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
                    await this.SetOrUpdateServerCopiesParameter(cancellationToken).ConfigureAwait(false);

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
            await this.KillServerWorkload(cancellationToken).ConfigureAwait(false);

            for (int i = 1; i <= int.Parse(this.Copies); i++)
            {
                int port = int.Parse(this.Port) + i - 1;
                string precommand = string.Empty;
                if (long.Parse(this.Bind) == 1)
                {
                    int core = i - 1;
                    precommand = $"numactl -C {core}";
                }
                
                string startservercommand = $"bash -c \"{precommand} {this.RedisPackagePath}/src/redis-server --port {port} --protected-mode no --ignore-warnings ARM64-COW-BUG --save  --io-threads 4 --maxmemory-policy noeviction\"";

                using (IProcessProxy process = this.SystemManager.ProcessManager.CreateElevatedProcess(this.Platform, startservercommand, null, this.RedisPackagePath))
                {
                    if (!process.Start())
                    {
                        throw new WorkloadException($"The API server workload did not start as expected.", ErrorReason.WorkloadFailed);
                    }

                    await this.WarmUpServer(port, cancellationToken).ConfigureAwait(false);
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
            await this.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

            string warmupservercommand =
                $"{this.MemtierPackagePath}/memtier_benchmark --protocol=redis --server localhost --port={port} -c 1 -t 1 --pipeline 100 " +
                $"--data-size=32 --key-minimum=1 --key-maximum=10000000 --ratio=1:0 --requests=allkeys";

            await this.ExecuteCommandAsync(warmupservercommand, this.RedisPackagePath, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task SetOrUpdateServerCopiesParameter(CancellationToken cancellationToken)
        {
            this.ServerCopiesCount = new State(new Dictionary<string, IConvertible>
            {
                [nameof(this.ServerCopiesCount)] = this.Copies
            });

            HttpResponseMessage response = await this.ServerApiClient.GetOrCreateStateAsync(nameof(this.ServerCopiesCount), JObject.FromObject(this.ServerCopiesCount), cancellationToken)
                .ConfigureAwait(false);

            response.ThrowOnError<WorkloadException>();
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
                HttpResponseMessage response = await this.ServerApiClient.DeleteStateAsync(nameof(this.ServerCopiesCount), cancellationToken)
                    .ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    response.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
                }
            });
        }
    }
}
