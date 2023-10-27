using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Services.Description;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using VirtualClient.Common;
using VirtualClient.Common.Contracts;
using VirtualClient.Common.Extensions;
using VirtualClient.Common.Telemetry;
using VirtualClient.Contracts;

namespace VirtualClient.Actions.Kafka
{
    /// <summary>
    /// Kafka server executor.
    /// </summary>
    public class KafkaServerExecutor : KafkaExecutor
    {
        private const int ZookeeperPort = 2181;
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;
        private List<Task> serverProcesses;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="KafkaServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public KafkaServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;
            this.serverProcesses = new List<Task>();
            this.ServerRetryPolicy = Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
                .WaitAndRetryAsync(10, (retries) => TimeSpan.FromSeconds(retries));
        }

        /// <summary>
        /// Parameter defines the Kafka server command line to execute.
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
        /// A retry policy to apply to the server when starting to handle transient issues that
        /// would otherwise prevent it from starting successfully.
        /// </summary>
        protected IAsyncPolicy ServerRetryPolicy { get; set; }

        /// <summary>
        /// Disposes of resources used by the executor including shutting down any
        /// instances of Kafka server running.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.disposed && this.serverProcesses.Any())
                {
                    try
                    {
                        Console.WriteLine("Disposed");
                        // We MUST stop the server instances from running before VC exits or they will
                        // continue running until explicitly stopped. This is a problem for running Redis
                        // workloads back to back because the requisite ports will be in use already on next
                        // VC startup.
                        /*this.KillServerInstancesAsync(CancellationToken.None)
                            .GetAwaiter().GetResult();*/
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

                    if (this.ResetServer(telemetryContext))
                    {
                        await this.DeleteStateAsync(telemetryContext, cancellationToken);
                        await this.KillServerInstancesAsync(cancellationToken);
                        await this.StartServerInstances(telemetryContext, cancellationToken);
                    }

                    await this.SaveStateAsync(telemetryContext, cancellationToken);
                    this.SetServerOnline(true);

                    if (this.IsMultiRoleLayout())
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
                catch (Exception exc)
                {
                    this.Logger.LogMessage(
                        $"{this.TypeName}.StartServerInstancesError",
                        LogLevel.Error,
                        telemetryContext.Clone().AddError(exc));
                    this.SetServerOnline(false);
                    throw;
                }
            });
        }

        /// <summary>
        /// Initializes the environment and dependencies for server of Kafka workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken);

            if (!this.fileSystem.File.Exists(this.ZookeeperScriptPath))
            {
                throw new DependencyException(
                    $"Kafka executable not found at path '{this.ZookeeperScriptPath}'",
                    ErrorReason.WorkloadDependencyMissing);
            }

            await this.ConfigurePropertiesFileAsync(telemetryContext, cancellationToken);
            await this.OpenKafkaPorts(cancellationToken);
            await this.SystemManagement.MakeFileExecutableAsync(this.ZookeeperScriptPath, this.Platform, cancellationToken);
        }

        private Task StartServerInstances(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.serverProcesses.Clear();

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("serverInstances", this.ServerInstances)
                .AddContext("portRange", $"{this.Port}-{this.Port + this.ServerInstances}");

            return this.Logger.LogMessageAsync($"{this.TypeName}.StartServerInstances", relatedContext, async () =>
            {
                try
                {
                    string workingDirectory = this.KafkaPackagePath;
                    List<string> commands = new List<string>();

                    relatedContext.AddContext("command", this.PlatformSpecificCommandType);
                    relatedContext.AddContext("commandArguments", commands);
                    relatedContext.AddContext("workingDir", workingDirectory);

                    // Start zookeeper server and wait for it start
                    string zookeeperCommandArgs = this.GetCommandArguement(this.ZookeeperScriptPath, "zookeeper.properties");
                    commands.Add(zookeeperCommandArgs);
                    Task zookeeperTask = this.StartZookeeperServerAsync(this.PlatformSpecificCommandType, zookeeperCommandArgs, this.KafkaPackagePath, relatedContext, cancellationToken);
                    this.serverProcesses.Add(zookeeperTask);
                    await zookeeperTask.ConfigureAwait(false);

                    List<IProcessProxy> kafkaProcesses = new List<IProcessProxy>();
                    // Start kafka servers once zookeeper server is started.
                    for (int i = 0; i < this.ServerInstances; i++)
                    {
                        int port = this.Port + i;
                        string commandArguments = this.GetCommandArguement(this.KafkaScriptPath, $"server-{i}.properties");
                        commands.Add(commandArguments);
                        IProcessProxy kafkaProcess = this.ProcessCommandAsync(this.PlatformSpecificCommandType, commandArguments, workingDirectory, relatedContext, cancellationToken);
                        kafkaProcess.Start();
                        kafkaProcesses.Add(kafkaProcess);
                    }

                    foreach (var process in kafkaProcesses)
                    {
                        Task kafkaTask = process.WaitForExitAsync(cancellationToken);
                        this.serverProcesses.Add(kafkaTask);
                        await kafkaTask.ConfigureAwait(false);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                }
                catch (Exception exc)
                {
                    this.Logger.LogMessage(
                        $"{this.TypeName}.StartServerInstancesError",
                        LogLevel.Error,
                        telemetryContext.Clone().AddError(exc));

                    throw;
                }
            });
        }

        private string GetCommandArguement(string scriptPath, string propertiesFile)
        {
            string propertiesFilePath = this.PlatformSpecifics.Combine(this.KafkaPackagePath, "config", propertiesFile);
            string commandArgs = $"{scriptPath} {propertiesFilePath}";
            if (this.Platform == PlatformID.Win32NT)
            {
                commandArgs = $"/c {scriptPath} {propertiesFilePath}";
            }

            return commandArgs;
        }

        private Task StartZookeeperServerAsync(string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return (this.ServerRetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
            {
                try
                {
                    using (IProcessProxy process = this.ProcessCommandAsync(command, commandArguments, workingDirectory, telemetryContext, cancellationToken))
                    {
                        if (process.Start())
                        {
                            await process.WaitForResponseAsync("INFO Created server", cancellationToken, timeout: TimeSpan.FromSeconds(30)).ConfigureAwait(false);
                        }

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            ConsoleLogger.Default.LogMessage($"zookeeper server process exited (port = {ZookeeperPort})...", telemetryContext);
                            // await this.LogProcessDetailsAsync(process, telemetryContext, "kafka", logToFile: true);
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

        private Task StartKafkaServerAsync(int port, string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return (this.ServerRetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
            {
                try
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            ConsoleLogger.Default.LogMessage($"Kafka server process exited (port = {port})...", telemetryContext);
                            // await this.LogProcessDetailsAsync(process, telemetryContext, "Kafka");

                            // Redis will give 137 if it thinks memory is constraint but will still accept connection, example:
                            // WARNING overcommit_memory is set to 0! Background save may fail under low memory condition. To fix this issue add 'vm.overcommit_memory = 1' to /etc/sysctl.conf and then reboot or run the command 'sysctl vm.overcommit_memory=1'
                            // for this to take effect.
                            // Ready to accept connections
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

        private async Task ConfigurePropertiesFileAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            for (int serverInstance = 0; serverInstance < this.ServerInstances; serverInstance++)
            {
                int port = this.Port + serverInstance;

                string configDirectoryPath = this.PlatformSpecifics.Combine(this.KafkaPackagePath, "config");
                string oldServerProperties = this.PlatformSpecifics.Combine(configDirectoryPath, "server.properties");
                string newServerProperties = this.PlatformSpecifics.Combine(configDirectoryPath, $"server-{serverInstance}.properties");
                this.fileSystem.File.Copy(oldServerProperties, newServerProperties, true);

                await this.fileSystem.File.ReplaceInFileAsync(
                        newServerProperties, @"broker.id *= *[^\n]*", $"broker.id = {serverInstance}", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        newServerProperties, @"log.dirs *= *[^\n]*", $"log.dirs = /tmp/kafka-logs-{serverInstance}", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        newServerProperties, @"#listeners *= *[^\n]*", $"listeners=PLAINTEXT://{this.ServerIpAddress}:{port}", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        newServerProperties, @"zookeeper.connect *= *[^\n]*", $"zookeeper.connect={this.ServerIpAddress}:{ZookeeperPort}", cancellationToken);

            }
        }

        private Task DeleteStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();
            return this.Logger.LogMessageAsync($"{this.TypeName}.DeleteState", relatedContext, async () =>
            {
                using (HttpResponseMessage response = await this.ApiClient.DeleteStateAsync(nameof(KafkaServerState), cancellationToken))
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
            IEnumerable<IProcessProxy> processes = this.SystemManagement.ProcessManager.GetProcesses("java");

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
                this.Logger.LogTraceMessage($"Restart Kafka Server(s)...", telemetryContext);
            }
            else
            {
                this.Logger.LogTraceMessage($"Kafka Server(s) Running...", telemetryContext);
            }

            return shouldReset;
        }

        private Task SaveStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();
            return this.Logger.LogMessageAsync($"{this.TypeName}.SaveState", relatedContext, async () =>
            {
                List<int> ports = new List<int>();
                for (int i = 0; i < this.ServerInstances; i++)
                {
                    ports.Add(this.Port + i);
                }

                var state = new Item<KafkaServerState>(nameof(KafkaServerState), new KafkaServerState(new Dictionary<string, IConvertible>
                {
                    [nameof(KafkaServerState.Ports)] = string.Join(",", ports),
                    [nameof(KafkaServerState.ZookeeperPort)] = ZookeeperPort
                }));

                using (HttpResponseMessage response = await this.ApiClient.UpdateStateAsync(nameof(KafkaServerState), state, cancellationToken))
                {
                    relatedContext.AddResponseContext(response);
                    response.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
                }
            });
        }

        private Task StartServerInstanceAsync(int port, string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return (this.ServerRetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
            {
                try
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, telemetryContext, cancellationToken))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            ConsoleLogger.Default.LogMessage($"Kafka server process exited (port = {port})...", telemetryContext);
                            // await this.LogProcessDetailsAsync(process, telemetryContext, "kafka", logToFile: true);
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

        private async Task OpenKafkaPorts(CancellationToken cancellationToken)
        {
            for (int serverInstance = 0; serverInstance < this.ServerInstances; serverInstance++)
            {
                int port = this.Port + serverInstance;
                await KafkaServerExecutor.OpenFirewallPortsAsync(port, this.systemManagement.FirewallManager, cancellationToken);
            }
        }

        private static Task OpenFirewallPortsAsync(int port, IFirewallManager firewallManager, CancellationToken cancellationToken)
        {
            return firewallManager.EnableInboundConnectionsAsync(
                new List<FirewallEntry>
                {
                    new FirewallEntry(
                        "PostgreSQL: Allow Multiple Machines communications",
                        "Allows individual machine instances to communicate with other machine in client-server scenario",
                        "tcp",
                        new List<int> { port })
                },
                cancellationToken);
        }
    }
}
