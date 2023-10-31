using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
        /// Path to Kafka server executable.
        /// </summary>
        protected string KafkaStartScriptPath { get; set; }

        /// <summary>
        /// Path to Kafka server executable.
        /// </summary>
        protected string KafkaStopScriptPath { get; set; }

        /// <summary>
        /// Path to zookeeper server package.
        /// </summary>
        protected string ZookeeperStartScriptPath { get; set; }

        /// <summary>
        /// Path to zookeeper server package.
        /// </summary>
        protected string ZookeeperStopScriptPath { get; set; }

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
                        // continue running until explicitly stopped. This is a problem for running Kafka
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

                    await this.DeleteStateAsync(telemetryContext, cancellationToken);
                    await this.KillServerInstancesAsync(cancellationToken);
                    await this.StartServerInstancesAsync(telemetryContext, cancellationToken);
                    await this.SaveStateAsync(telemetryContext, cancellationToken);

                    this.SetServerOnline(true);
                    if (this.IsMultiRoleLayout())
                    {
                        await Task.WhenAll(this.serverProcesses);
                        await this.StopServersAsync(telemetryContext, cancellationToken);
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

            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    this.KafkaStartScriptPath = this.Combine(this.KafkaPackagePath, "bin", "windows", "kafka-server-start.bat");
                    this.KafkaStopScriptPath = this.Combine(this.KafkaPackagePath, "bin", "windows", "kafka-server-stop.bat");
                    this.ZookeeperStartScriptPath = this.Combine(this.KafkaPackagePath, "bin", "windows", "zookeeper-server-start.bat");
                    this.ZookeeperStopScriptPath = this.Combine(this.KafkaPackagePath, "bin", "windows", "zookeeper-server-stop.bat");
                    break;

                case PlatformID.Unix:
                    this.KafkaStartScriptPath = this.Combine(this.KafkaPackagePath, "bin", "kafka-server-start.sh");
                    this.KafkaStopScriptPath = this.Combine(this.KafkaPackagePath, "bin", "kafka-server-stop.sh");
                    this.ZookeeperStartScriptPath = this.Combine(this.KafkaPackagePath, "bin", "zookeeper-server-start.sh");
                    this.ZookeeperStopScriptPath = this.Combine(this.KafkaPackagePath, "bin", "zookeeper-server-stop.sh");
                    break;
            }

            if (!this.fileSystem.File.Exists(this.ZookeeperStartScriptPath))
            {
                throw new DependencyException(
                    $"Zookeeper executable not found at path '{this.ZookeeperStartScriptPath}'",
                    ErrorReason.WorkloadDependencyMissing);
            }

            if (!this.fileSystem.File.Exists(this.KafkaStartScriptPath))
            {
                throw new DependencyException(
                    $"Kafka executable not found at path '{this.KafkaStartScriptPath}'",
                    ErrorReason.WorkloadDependencyMissing);
            }

            await this.ConfigurePropertiesFileAsync(telemetryContext, cancellationToken);
            await this.SystemManagement.MakeFileExecutableAsync(this.ZookeeperStartScriptPath, this.Platform, cancellationToken);
            await this.SystemManagement.MakeFileExecutableAsync(this.ZookeeperStopScriptPath, this.Platform, cancellationToken);
            await this.SystemManagement.MakeFileExecutableAsync(this.KafkaStartScriptPath, this.Platform, cancellationToken);
            await this.SystemManagement.MakeFileExecutableAsync(this.KafkaStopScriptPath, this.Platform, cancellationToken);
            this.OpenKafkaPorts(cancellationToken);
        }

        private Task StartServerInstancesAsync(EventContext telemetryContext, CancellationToken cancellationToken)
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
                    string zookeeperCommandArgs = this.GetCommandArguement(this.ZookeeperStartScriptPath, "zookeeper.properties");
                    commands.Add(zookeeperCommandArgs);
                    Task zookeeperTask = this.StartZookeeperServerAsync("INFO zookeeper.request_throttler", ZookeeperPort, this.PlatformSpecificCommandType, zookeeperCommandArgs, this.KafkaPackagePath, relatedContext, cancellationToken);
                    this.serverProcesses.Add(zookeeperTask);
                    await zookeeperTask;

                    List<IProcessProxy> kafkaProcesses = new List<IProcessProxy>();
                    // Start kafka servers once zookeeper server is started.
                    for (int i = 0; i < this.ServerInstances; i++)
                    {
                        int port = this.Port + i;
                        string commandArguments = this.GetCommandArguement(this.KafkaStartScriptPath, $"server-{i}.properties");
                        commands.Add(commandArguments); 
                        this.serverProcesses.Add(this.RunCommandAsync(
                            $"Kafka server process exited (port = {port})...", 
                            this.PlatformSpecificCommandType, 
                            commandArguments, 
                            workingDirectory, 
                            relatedContext, 
                            cancellationToken,
                            "Kafka"));
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

        private Task StartZookeeperServerAsync(string responseMessage, int port, string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return (this.ServerRetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
            {
                try
                {
                    using (IProcessProxy process = this.ProcessCommandAsync(command, commandArguments, workingDirectory, telemetryContext, cancellationToken))
                    {
                        process.Start();
                        await process.WaitForResponseAsync(responseMessage, cancellationToken, timeout: TimeSpan.FromSeconds(30)).ConfigureAwait(false);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            ConsoleLogger.Default.LogMessage($"zookeeper server process exited (port = {port})...", telemetryContext);
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

        private Task RunCommandAsync(string logMessage, string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken, string toolName = null)
        {
            return (this.ServerRetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
            {
                try
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, telemetryContext, cancellationToken, runElevated: true))
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            ConsoleLogger.Default.LogMessage(logMessage, telemetryContext);
                            await this.LogProcessDetailsAsync(process, telemetryContext, toolName);
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
                        $"{this.TypeName}.RunCommandError",
                        LogLevel.Error,
                        telemetryContext.Clone().AddError(exc));

                    throw;
                }
            });
        }

        private async Task ConfigurePropertiesFileAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string logDir = Directory.GetDirectoryRoot(this.KafkaPackagePath).TrimEnd('\\') + "/tmp";
            this.fileSystem.Directory.DeleteAsync(logDir);

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
                        newServerProperties, @"log.dirs *= *[^\n]*", $"log.dirs = {logDir}/kafka-logs-{serverInstance}", cancellationToken);

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

        private async Task StopServersAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.Logger.LogTraceMessage($"{this.TypeName}.StopServerInstances");
            string zookeeperStopArgs = this.ZookeeperStopScriptPath;
            string kafkaStopArgs = this.KafkaStopScriptPath;
            if (this.Platform == PlatformID.Win32NT)
            {
                zookeeperStopArgs = $"/c {this.ZookeeperStopScriptPath}";
                kafkaStopArgs = $"/c {this.KafkaStopScriptPath}";
            }
            
            await this.RunCommandAsync(
                            $"Stop Kafka servers...",
                            this.PlatformSpecificCommandType,
                            kafkaStopArgs,
                            this.KafkaPackagePath,
                            telemetryContext,
                            cancellationToken,
                            "Kafka");
            await this.RunCommandAsync(
                            $"Stop zookeeper server...",
                            this.PlatformSpecificCommandType,
                            zookeeperStopArgs,
                            this.KafkaPackagePath,
                            telemetryContext,
                            cancellationToken,
                            "Zookeeper");

            this.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
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

        private void OpenKafkaPorts(CancellationToken cancellationToken)
        {
            List<int> ports = new List<int>();
            for (int serverInstance = 0; serverInstance < this.ServerInstances; serverInstance++)
            {
                int port = this.Port + serverInstance;
                ports.Add(port);
            }

            this.systemManagement.FirewallManager.EnableInboundConnectionsAsync(
                new List<FirewallEntry>
                {
                    new FirewallEntry(
                        "Kafka: Allow Multiple Machines communications",
                        "Allows individual machine instances to communicate with other machine in client-server scenario",
                        "tcp",
                        new List<int>(ports))
                },
                cancellationToken);
        }
    }
}
