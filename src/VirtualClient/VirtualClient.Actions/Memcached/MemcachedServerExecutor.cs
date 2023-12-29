// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Polly;
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
        private List<Task> serverProcesses;
        private bool disposed;
        // private long maxConnections;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemcachedServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public MemcachedServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.serverProcesses = new List<Task>();

            this.ServerRetryPolicy = Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
                .WaitAndRetryAsync(10, (retries) => TimeSpan.FromSeconds(retries));
        }

        /// <summary>
        /// Parameter defines whether to bind the Memcached server process to cores on the system.
        /// </summary>
        public bool BindToCores
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.BindToCores), true);
            }
        }

        /// <summary>
        /// Parameter defines the Memcached server command line to execute.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CommandLine));
            }
        }

        /// <summary>
        /// Parameter defines the initial port on which server instances will run.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port));
            }
        }

        /// <summary>
        /// Parameter defines the maximum number of connections to set for the
        /// server.
        /// </summary>
        public int ServerMaxConnections
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(MemcachedServerExecutor.ServerMaxConnections));
            }
        }

        /// <summary>
        /// Parameter defines the number of threads to allocate to each server instance
        /// running. Default = # of logical cores/vCPUs on the system.
        /// </summary>
        public int ServerThreadCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ServerThreadCount), Environment.ProcessorCount);
            }
        }

        /// <summary>
        /// Client used to communicate with the locally hosted instance of the
        /// Virtual Client API.
        /// </summary>
        protected IApiClient ApiClient
        {
            get
            {
                return this.ServerApiClient;
            }
        }

        /// <summary>
        /// Path to Memcached server executable.
        /// </summary>
        protected string MemcachedExecutablePath { get; set; }

        /// <summary>
        /// Path to Memcached server package.
        /// </summary>
        protected string MemcachedPackagePath { get; set; }

        /// <summary>
        /// A retry policy to apply to the server when starting to handle transient issues that
        /// would otherwise prevent it from starting successfully.
        /// </summary>
        protected IAsyncPolicy ServerRetryPolicy { get; set; }

        /// <summary>
        /// Disposes of resources used by the executor including shutting down any
        /// instances of Memcached server running.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.disposed && this.serverProcesses.Any())
                {
                    try
                    {
                        // We MUST stop the server instances from running before VC exits or they will
                        // continue running until explicitly stopped. This is a problem for running Memcached
                        // workloads back to back because the requisite ports will be in use already on next
                        // VC startup.
                        this.KillServerInstancesAsync(CancellationToken.None)
                            .GetAwaiter().GetResult();
                    }
                    catch
                    {
                        // Best effort
                    }

                    this.disposed = true;
                }
            }
        }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteServer", telemetryContext, async () =>
            {
                try
                {
                    this.SetServerOnline(false);

                    await this.ServerApiClient.PollForHeartbeatAsync(TimeSpan.FromMinutes(5), cancellationToken);

                    if (this.ResetServer(telemetryContext))
                    {
                        await this.DeleteStateAsync(telemetryContext, cancellationToken);
                        await this.KillServerInstancesAsync(cancellationToken);
                        this.StartServerInstances(telemetryContext, cancellationToken);
                    }

                    await this.SaveStateAsync(telemetryContext, cancellationToken);
                    this.SetServerOnline(true);

                    if (this.IsMultiRoleLayout())
                    {
                        using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                        {
                            await Task.WhenAny(this.serverProcesses);

                            // A cancellation is request, then we allow each of the server instances
                            // to gracefully exit. If a cancellation was not requested, it means that one 
                            // or more of the server instances exited and we will want to allow the component
                            // to start over restarting the servers.
                            if (cancellationToken.IsCancellationRequested)
                            {
                                await Task.WhenAll(this.serverProcesses);
                            }
                        }
                    }
                }
                catch
                {
                    this.SetServerOnline(false);
                    throw;
                }
            });
        }

        /// <summary>
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken);
            DependencyPath memcachedPackage = await this.GetPackageAsync(this.PackageName, cancellationToken);

            this.MemcachedPackagePath = memcachedPackage.Path;
            this.MemcachedExecutablePath = this.Combine(this.MemcachedPackagePath, "memcached");

            await this.SystemManagement.MakeFileExecutableAsync(this.MemcachedExecutablePath, this.Platform, cancellationToken);

            this.InitializeApiClients();
        }

        private Task DeleteStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();
            return this.Logger.LogMessageAsync($"{this.TypeName}.DeleteState", relatedContext, async () =>
            {
                using (HttpResponseMessage response = await this.ApiClient.DeleteStateAsync(nameof(ServerState), cancellationToken))
                {
                    relatedContext.AddResponseContext(response);
                    if (response.StatusCode != HttpStatusCode.NoContent)
                    {
                        response.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
                    }
                }
            });
        }

        private Task KillServerInstancesAsync(CancellationToken cancellationToken)
        {
            this.Logger.LogTraceMessage($"{this.TypeName}.KillServerInstances");
            IEnumerable<IProcessProxy> processes = this.ProcessManager.GetProcesses("memcached");

            if (processes?.Any() == true)
            {
                foreach (IProcessProxy process in processes)
                {
                    process.SafeKill();
                }
            }

            return this.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
        }

        private bool ResetServer(EventContext telemetryContext)
        {
            bool shouldReset = true;
            if (this.serverProcesses?.Any() == true)
            {
                // Depending upon how the server Task instances are created, the Task may be in a status
                // of Running or WaitingForActivation. The server is running in either of these 2 states.
                shouldReset = !this.serverProcesses.All(p => p.Status == TaskStatus.Running || p.Status == TaskStatus.WaitingForActivation);
            }

            if (shouldReset)
            {
                this.Logger.LogTraceMessage($"Restart Memcached Server(s)...", telemetryContext);
            }
            else
            {
                this.Logger.LogTraceMessage($"Memcached Server(s) Running...", telemetryContext);
            }

            return shouldReset;
        }

        private Task SaveStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();
            return this.Logger.LogMessageAsync($"{this.TypeName}.SaveState", relatedContext, async () =>
            {
                var state = new Item<ServerState>(nameof(ServerState), new ServerState(new Dictionary<string, IConvertible>
                {
                    [nameof(ServerState.Ports)] = this.Port
                }));

                using (HttpResponseMessage response = await this.ApiClient.UpdateStateAsync(nameof(ServerState), state, cancellationToken))
                {
                    relatedContext.AddResponseContext(response);
                    response.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
                }
            });
        }

        private void StartServerInstances(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.serverProcesses.Clear();

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("port", this.Port)
                .AddContext("bindToCores", this.BindToCores)
                .AddContext("serverMaxConnections", this.ServerMaxConnections)
                .AddContext("serverThreadCount", this.ServerThreadCount);

            this.Logger.LogMessage($"{this.TypeName}.StartServerInstances", relatedContext, () =>
            {
                try
                {
                    string command = "bash";
                    string commandArguments = null;
                    string workingDirectory = this.MemcachedPackagePath;

                    if (this.BindToCores)
                    {
                        IEnumerable<int> coreBindings = Enumerable.Range(0, Environment.ProcessorCount);

                        // e.g.
                        // bash -c "numactl -C 1 /home/user/VirtualClient/linux-x64/packages/memcached/memcached --port 6389 -t 4 -m 1024
                        // https://github.com/memcached/memcached/wiki/Commands#flushall
                        // https://docs.oracle.com/cd/E17952_01/mysql-5.6-en/ha-memcached-cmdline-options.html#:~:text=Set%20the%20amount%20of%20memory%20allocated%20to%20memcached,amount%20of%20RAM%20to%20be%20allocated%20%28in%20megabytes%29.

                        commandArguments = $"-c \"numactl -C {string.Join(',', coreBindings)} {this.MemcachedExecutablePath} {this.CommandLine}\"";

                    }
                    else
                    {
                        commandArguments = $"-c \"{this.MemcachedExecutablePath} {this.CommandLine}\"";
                    }

                    relatedContext.AddContext("command", command);
                    relatedContext.AddContext("commandArguments", commandArguments);
                    relatedContext.AddContext("workingDir", workingDirectory);

                    this.serverProcesses.Add(this.StartServerInstanceAsync(this.Port, command, commandArguments, workingDirectory, relatedContext, cancellationToken));
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                }
            });
        }

        private Task StartServerInstanceAsync(int serverPort, string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return (this.ServerRetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
            {
                try
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, telemetryContext, cancellationToken, runElevated: true, username: this.Username))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            ConsoleLogger.Default.LogMessage($"Memcached server process exited (port = {serverPort})...", telemetryContext);
                            process.ThrowIfWorkloadFailed();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                }
                catch (Exception exc)
                {
                    this.Logger.LogMessage(
                        $"{this.TypeName}.StartServerInstanceError",
                        LogLevel.Error,
                        telemetryContext.Clone().AddError(exc));

                    throw;
                }
            });
        }
    }
}
