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
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common;
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
            this.StateManager = this.Dependencies.GetService<IStateManager>();
        }

        /// <summary>
        /// Provides access to the local state management facilities.
        /// </summary>
        protected IStateManager StateManager { get; }

        /// <summary>
        /// Server item memory in megabytes.
        /// </summary>
        protected string ServerItemMemoryMB
        {
            get
            {
                this.Parameters.TryGetValue(nameof(MemcachedServerExecutor.ServerItemMemoryMB), out IConvertible serverItemMemoryMB);
                return serverItemMemoryMB?.ToString();
            }
        }

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
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            this.Copies = Environment.ProcessorCount.ToString();
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
            await this.KillProcessesWithNameAsync("memcached", cancellationToken).ConfigureAwait(false);

            for (int i = 0; i < int.Parse(this.Copies); i++)
            {
                int port = int.Parse(this.Port) + i;
                string precommand = string.Empty;
                if (long.Parse(this.Bind) == 1)
                {
                    int core = i;
                    precommand = $"numactl -C {core}";
                }

                string startservercommand = $"-u {this.Username} bash -c \"{precommand} {this.MemcachedPackagePath}/memcached -d -p {port} -t 4 -m {this.ServerItemMemoryMB}\"";

                this.Logger.LogTraceMessage($"Executing process '{startservercommand}'  at directory '{this.PackagePath}'.");
                this.process = this.ProcessManager.CreateElevatedProcess(this.Platform, startservercommand, null, this.PackagePath);
                if (!this.process.Start())
                {
                    throw new WorkloadException($"The API server workload did not start as expected.", ErrorReason.WorkloadFailed);
                }

                await this.WarmUpServer(port, cancellationToken).ConfigureAwait(false);

            }
        }

        private async Task WarmUpServer(int port, CancellationToken cancellationToken)
        {
            await this.WaitAsync(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

            string warmupservercommand = $"-u {this.Username} {this.MemtierPackagePath}/memtier_benchmark --protocol={this.Protocol} --server localhost --port={port} -c 1 -t 1 --pipeline 100 --data-size=32 --key-minimum=1 --key-maximum=10000000 --ratio=1:0 --requests=allkeys";
            await this.ExecuteCommandAsync<MemcachedServerExecutor>(warmupservercommand, null, this.PackagePath, cancellationToken)
            .ConfigureAwait(false);
        }

        private async Task SetOrUpdateServerCopiesParameter(CancellationToken cancellationToken)
        {
            Console.WriteLine("Setting or updating copies parameter");
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
            return this.Logger.LogMessageAsync($"{nameof(MemcachedServerExecutor)}.ResetState", telemetryContext, async () =>
            {
                HttpResponseMessage response = await this.ServerApiClient.DeleteStateAsync(
                                                            nameof(this.ServerCopiesCount),
                                                            cancellationToken).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    response.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
                }
            });
        }
    }
}
