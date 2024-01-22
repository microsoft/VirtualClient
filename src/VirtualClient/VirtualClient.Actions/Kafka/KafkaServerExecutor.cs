using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
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
        /// Path to Kafka storage package.
        /// </summary>
        protected string KafkaStorageScriptPath { get; set; }

        /// <summary>
        /// Path to Kafka kraft folder.
        /// </summary>
        protected string KafkaKraftDirectoryPath { get; set; }

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

                    await this.ServerApiClient.PollForHeartbeatAsync(TimeSpan.FromMinutes(5), cancellationToken);

                    if (this.ResetServer(telemetryContext))
                    {
                        await this.DeleteStateAsync(telemetryContext, cancellationToken);
                        await this.KillServerInstancesAsync(cancellationToken);
                        await this.ConfigurePropertiesFileAsync(telemetryContext, cancellationToken);
                        await this.StartServerInstancesAsync(telemetryContext, cancellationToken);
                    }

                    await this.SaveStateAsync(telemetryContext, cancellationToken);
                    this.SetServerOnline(true);

                    if (this.IsMultiRoleLayout())
                    {
                        await Task.WhenAny(this.serverProcesses);
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
            this.KafkaKraftDirectoryPath = this.Combine(this.KafkaPackagePath, "config", "kraft");
            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    this.KafkaStartScriptPath = this.Combine(this.KafkaPackagePath, "bin", "windows", "kafka-server-start.bat");
                    this.KafkaStopScriptPath = this.Combine(this.KafkaPackagePath, "bin", "windows", "kafka-server-stop.bat");
                    this.KafkaStorageScriptPath = this.Combine(this.KafkaPackagePath, "bin", "windows", "kafka-storage.bat");
                    break;

                case PlatformID.Unix:
                    this.KafkaStartScriptPath = this.Combine(this.KafkaPackagePath, "bin", "kafka-server-start.sh");
                    this.KafkaStopScriptPath = this.Combine(this.KafkaPackagePath, "bin", "kafka-server-stop.sh");
                    this.KafkaStorageScriptPath = this.Combine(this.KafkaPackagePath, "bin", "kafka-storage.sh");
                    break;
            }

            if (!this.fileSystem.File.Exists(this.KafkaStartScriptPath))
            {
                throw new DependencyException(
                    $"Kafka executable not found at path '{this.KafkaStartScriptPath}'",
                    ErrorReason.WorkloadDependencyMissing);
            }

            await this.SystemManagement.MakeFileExecutableAsync(this.KafkaStorageScriptPath, this.Platform, cancellationToken);
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
                    List<string> commands = new List<string>();

                    relatedContext.AddContext("command", this.PlatformSpecificCommandType);
                    relatedContext.AddContext("commandArguments", commands);
                    relatedContext.AddContext("workingDir", this.KafkaPackagePath);

                    // Get Cluster Id
                    string clusterIdCmdArgs = this.GetPlatformFormattedCommandArguement(this.KafkaStorageScriptPath, "random-uuid");
                    commands.Add(clusterIdCmdArgs);
                    string clusterId = await this.GetClusterId(this.PlatformSpecificCommandType, clusterIdCmdArgs, this.KafkaPackagePath, relatedContext, cancellationToken);
                    clusterId = Regex.Replace(clusterId, $"(\n\r)|(\r\n)", " ");

                    // Start kafka servers once zookeeper server is started.
                    for (int i = 0; i < this.ServerInstances; i++)
                    {
                        string propertiesFilePath = this.PlatformSpecifics.Combine(this.KafkaKraftDirectoryPath, $"server-{i + 1}.properties");

                        // Format log directories
                        string formatLogDirCmdArgs = $"format -t {clusterId} -c {propertiesFilePath}";
                        formatLogDirCmdArgs = this.GetPlatformFormattedCommandArguement(this.KafkaStorageScriptPath, formatLogDirCmdArgs);
                        commands.Add(formatLogDirCmdArgs);
                        await this.StartServerAndWaitForExitAsync("Format Directory: Complete", this.PlatformSpecificCommandType, formatLogDirCmdArgs, this.KafkaPackagePath, relatedContext, cancellationToken);

                        // Start kafka servers
                        int port = this.Port + i;
                        string commandArguments = this.GetPlatformFormattedCommandArguement(this.KafkaStartScriptPath, propertiesFilePath);
                        commands.Add(commandArguments);
                        this.serverProcesses.Add(this.StartServerAndWaitForExitAsync(
                            $"Kafka server process exited (port = {port})...",
                            this.PlatformSpecificCommandType,
                            commandArguments,
                            this.KafkaPackagePath,
                            relatedContext,
                            cancellationToken,
                            "Kafka",
                            addCleanupTasks: false));
                    }

                    await Task.Delay(10000);
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

        private async Task<string> GetClusterId(string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                DateTime startTime = DateTime.UtcNow;
                string output;
                using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, telemetryContext, cancellationToken, runElevated: true, timeout: TimeSpan.FromMinutes(5)))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "ClusterId");
                        process.ThrowIfWorkloadFailed();
                    }

                    output = process.StandardOutput.ToString();
                    ConsoleLogger.Default.LogMessage($"Cluster Id - {output}", telemetryContext);
                }

                return output;
            }
            catch (OperationCanceledException)
            {
                // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                return null;
            }
            catch (Exception exc)
            {
                this.Logger.LogMessage(
                    $"{this.TypeName}.GetClusterIdError",
                    LogLevel.Error,
                    telemetryContext.Clone().AddError(exc));

                throw;
            }
        }

        private Task StartServerAndWaitForExitAsync(string logMessage, string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken, string toolName = null, bool addCleanupTasks = true)
        {
            return (this.ServerRetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
            {
                try
                {
                    using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, telemetryContext, cancellationToken, runElevated: true, addCleanupTasks: addCleanupTasks))
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
            string logDir = "/tmp";
            this.fileSystem.Directory.DeleteAsync(logDir);
            telemetryContext.AddContext("DeletedLogDirectory", logDir);

            for (int serverInstance = 1; serverInstance <= this.ServerInstances; serverInstance++)
            {
                int port = this.Port + ((serverInstance - 1) * 2);

                string oldServerProperties = this.PlatformSpecifics.Combine(this.KafkaKraftDirectoryPath, "server.properties");
                string newServerProperties = this.PlatformSpecifics.Combine(this.KafkaKraftDirectoryPath, $"server-{serverInstance}.properties");
                this.fileSystem.File.Copy(oldServerProperties, newServerProperties, true);

                await this.fileSystem.File.ReplaceInFileAsync(
                        newServerProperties, @"node.id *= *[^\n]*", $"node.id={serverInstance}", cancellationToken);

                await this.fileSystem.File.ReplaceInFileAsync(
                        newServerProperties, @"log.dirs *= *[^\n]*", $"log.dirs={logDir}/kraft-combined-logs-{serverInstance}", cancellationToken);

                if (this.IsMultiRoleLayout())
                {
                    await this.fileSystem.File.ReplaceInFileAsync(
                        newServerProperties, @"listeners *= *[^\n]*", $"listeners=PLAINTEXT://{this.ServerIpAddress}:{port},CONTROLLER://{this.ServerIpAddress}:{port + 1}", cancellationToken);
                    await this.fileSystem.File.ReplaceInFileAsync(
                        newServerProperties, @"controller.quorum.voters *= *[^\n]*", $"controller.quorum.voters={serverInstance}@{this.ServerIpAddress}:{port + 1}", cancellationToken);
                }
                else
                {
                    await this.fileSystem.File.ReplaceInFileAsync(
                        newServerProperties, @"listeners *= *[^\n]*", $"listeners=PLAINTEXT://:{port},CONTROLLER://:{port + 1}", cancellationToken);
                    await this.fileSystem.File.ReplaceInFileAsync(
                        newServerProperties, @"controller.quorum.voters *= *[^\n]*", $"controller.quorum.voters={serverInstance}@localhost:{port + 1}", cancellationToken);
                }

                telemetryContext.AddContext(nameof(newServerProperties), newServerProperties);
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
            string kafkaStopArgs = this.KafkaStopScriptPath;
            if (this.Platform == PlatformID.Win32NT)
            {
                kafkaStopArgs = $"/c {this.KafkaStopScriptPath}";
            }
            
            await this.StartServerAndWaitForExitAsync(
                            $"Stop Kafka servers...",
                            this.PlatformSpecificCommandType,
                            kafkaStopArgs,
                            this.KafkaPackagePath,
                            telemetryContext,
                            cancellationToken,
                            "Kafka");

            this.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
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
                for (int serverInstance = 1; serverInstance <= this.ServerInstances; serverInstance++)
                {
                    int port = this.Port + ((serverInstance - 1) * 2);
                    ports.Add(port);
                    ports.Add(port + 1);
                }

                var state = new Item<KafkaServerState>(nameof(KafkaServerState), new KafkaServerState(new Dictionary<string, IConvertible>
                {
                    [nameof(KafkaServerState.Ports)] = string.Join(",", ports)
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
            for (int serverInstance = 1; serverInstance <= this.ServerInstances; serverInstance++)
            {
                int port = this.Port + ((serverInstance - 1) * 2);
                ports.Add(port);
                ports.Add(port + 1);
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
