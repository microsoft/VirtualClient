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
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Actions.Memtier;
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
        private List<Task> serverProcesses;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public RedisServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
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
        /// Parameter defines the Redis server command line to execute.
        /// </summary>
        public string CommandLine
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.CommandLine));
            }
        }

        /// <summary>
        /// Port on which server runs.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port));
            }
        }

        /// <summary>
        /// True/false whether TLS should be enabled. Default = false.
        /// </summary>
        public bool IsTLSEnabled
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.IsTLSEnabled), false);
            }
        }

        /// <summary>
        /// Parameter defines the number of server instances/copies to run.
        /// </summary>
        public int ServerInstances
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ServerInstances));
            }
        }

        /// <summary>
        /// Parameter defines the number of server instances/copies to run.
        /// </summary>
        public string RedisResourcesPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.RedisResourcesPackageName));
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
        /// Path to Redis server executable.
        /// </summary>
        protected string RedisExecutablePath { get; set; }

        /// <summary>
        /// Path to Redis resources.
        /// </summary>
        protected string RedisResourcesPath { get; set; }

        /// <summary>
        /// Path to Redis server package.
        /// </summary>
        protected string RedisPackagePath { get; set; }

        /// <summary>
        /// A retry policy to apply to the server when starting to handle transient issues that
        /// would otherwise prevent it from starting successfully.
        /// </summary>
        protected IAsyncPolicy ServerRetryPolicy { get; set; }

        /// <summary>
        /// Disposes of resources used by the executor including shutting down any
        /// instances of Redis server running.
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
                        // continue running until explicitly stopped. This is a problem for running Redis
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
        /// Executes server side of workload.
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
                    await this.KillServerInstancesAsync(cancellationToken);
                    throw;
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
            await base.InitializeAsync(telemetryContext, cancellationToken);
            DependencyPath redisPackage = await this.GetPackageAsync(this.PackageName, cancellationToken);

            this.RedisPackagePath = redisPackage.Path;
            this.RedisExecutablePath = this.Combine(this.RedisPackagePath, "src", "redis-server");

            await this.SystemManagement.MakeFileExecutableAsync(this.RedisExecutablePath, this.Platform, cancellationToken);

            if (this.IsTLSEnabled)
            {
                DependencyPath redisResourcesPath = await this.GetPackageAsync(this.RedisResourcesPackageName, cancellationToken);
                this.RedisResourcesPath = redisResourcesPath.Path;
            }

            this.InitializeApiClients();
        }

        /// <summary>
        /// Validates the component definition for requirements.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            CpuInfo cpuInfo = this.SystemManagement.GetCpuInfoAsync(CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();

            if (this.BindToCores && this.ServerInstances > cpuInfo.LogicalProcessorCount)
            {
                throw new WorkloadException(
                    $"Invalid '{nameof(this.ServerInstances)}' parameter value. The number of server instances cannot exceed the number of logical cores/vCPUs on the system " +
                    $"when binding each of the servers to a logical core/vCPU. Set parameter '{nameof(this.BindToCores)}' = false to allow for additional server instances.",
                    ErrorReason.InvalidProfileDefinition);
            }
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
            IEnumerable<IProcessProxy> processes = this.SystemManagement.ProcessManager.GetProcesses("redis-server");

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
                this.Logger.LogTraceMessage($"Restart Redis Server(s)...", telemetryContext);
            }
            else
            {
                this.Logger.LogTraceMessage($"Redis Server(s) Running...", telemetryContext);
            }

            return shouldReset;
        }

        private Task SaveStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();
            return this.Logger.LogMessageAsync($"{this.TypeName}.SaveState", relatedContext, async () =>
            {
                List<PortDescription> ports = new List<PortDescription>();
                for (int i = 0; i < this.ServerInstances; i++)
                {
                    ports.Add(new PortDescription
                    {
                        CpuAffinity = this.BindToCores ? i.ToString() : null,
                        Port = this.Port + i
                    });
                }

                var state = new Item<ServerState>(nameof(ServerState), new ServerState(ports));

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
                .AddContext("serverInstances", this.ServerInstances)
                .AddContext("portRange", $"{this.Port}-{this.Port + this.ServerInstances}")
                .AddContext("bindToCores", this.BindToCores);

            this.Logger.LogMessage($"{this.TypeName}.StartServerInstances", relatedContext, () =>
            {
                try
                {
                    string command = "bash";
                    string workingDirectory = this.RedisPackagePath;
                    List<string> commands = new List<string>();

                    relatedContext.AddContext("command", command);
                    relatedContext.AddContext("commandArguments", commands);
                    relatedContext.AddContext("workingDir", workingDirectory);

                    for (int i = 0; i < this.ServerInstances; i++)
                    {
                        // We are starting the server instances in the background. Once they are running we
                        // will warm them up and then exit. We keep a reference to the server processes/tasks
                        // so that they remain running until the class is disposed.
                        int port = this.Port + i;
                        string commandArguments = null;

                        if (this.BindToCores)
                        {
                            commandArguments = $"-c \"numactl -C {i} {this.RedisExecutablePath}";
                        }
                        else
                        {
                            commandArguments = $"-c \"{this.RedisExecutablePath}";
                        }

                        if (this.IsTLSEnabled)
                        {
                            commandArguments += $" --tls-port {port} --port 0 --tls-cert-file {this.PlatformSpecifics.Combine(this.RedisResourcesPath, "redis.crt")}   --tls-key-file {this.PlatformSpecifics.Combine(this.RedisResourcesPath, "redis.key")} --tls-ca-cert-file {this.PlatformSpecifics.Combine(this.RedisResourcesPath, "ca.crt")}";
                        }
                        else
                        {
                            commandArguments += $" --port {port}";
                        }

                        commandArguments += $" {this.CommandLine}\"";

                        // We cannot use a Task.Run here. The Task is queued on the threadpool but does not get running
                        // until our counter 'i' is at the end. This will cause all server instances to use the same port
                        // and to try to bind to the same core.
                        commands.Add(commandArguments);
                        this.serverProcesses.Add(this.StartServerInstanceAsync(port, command, commandArguments, workingDirectory, relatedContext, cancellationToken));
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                }
            });
        }

        private Task StartServerInstanceAsync(int port, string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return (this.ServerRetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
            {
                try
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            ConsoleLogger.Default.LogMessage($"Redis server process exited (port = {port})...", telemetryContext);
                            await this.LogProcessDetailsAsync(process, telemetryContext, "Redis");

                            // Redis will give 137 if it thinks memory is constraint but will still accept connection, example:
                            // WARNING overcommit_memory is set to 0! Background save may fail under low memory condition. To fix this issue add 'vm.overcommit_memory = 1' to /etc/sysctl.conf and then reboot or run the command 'sysctl vm.overcommit_memory=1'
                            // for this to take effect.
                            // Ready to accept connections
                            process.ThrowIfWorkloadFailed(successCodes: new int[] { 0, 137 });
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
