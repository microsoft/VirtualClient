// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
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
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Logging;

    /// <summary>
    /// Executes the OpenSSL TLS server workload. Inherits from OpenSslExecutor.
    /// </summary>
    public class OpenSslServerExecutor : OpenSslExecutor
    {
        private List<Task> serverProcesses;
        private bool disposed;
        private ISystemManagement systemManagement;
        private IFileSystem fileSystem;

        /// <summary>
        /// Constructor for the OpenSSL server executor.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public OpenSslServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.ApiClientManager = dependencies.GetService<IApiClientManager>();
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.serverProcesses = new List<Task>();
            this.disposed = false;
            this.fileSystem = dependencies.GetService<IFileSystem>();
        }

        /// <summary>
        /// Server Port used for communication.
        /// </summary>
        public int ServerPort
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ServerPort));
            }
        }

        /// <summary>
        /// Client used to communicate with the hosted instance of the
        /// Virtual Client API at server side.
        /// </summary>
        protected IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// Provides the ability to create API clients for interacting with local as well as remote instances
        /// of the Virtual Client API service.
        /// </summary>
        protected IApiClientManager ApiClientManager { get; }

        /// <summary>
        /// Cancellation Token Source for Server.
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// Server IpAddress on which openssl s_server runs.
        /// </summary>
        protected string ServerIpAddress { get; set; }

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
        /// Disposes of resources used by the executor including shutting down any
        /// instances of openssl s_server running.
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
                        // continue running until explicitly stopped. This is a problem for running server
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
        /// Initializes the API clients used to communicate with the server instance.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken);

            this.InitializeApiClients(telemetryContext, cancellationToken);

            if (this.IsMultiRoleLayout())
            {
                ClientInstance clientInstance = this.GetLayoutClientInstance();
                string layoutIPAddress = clientInstance.IPAddress;

                this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);
                this.ThrowIfRoleNotSupported(clientInstance.Role);
            }

        }

        /// <summary>
        /// Initializes the API clients used to communicate with the server instance.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(OpenSslServerExecutor)}.ExecuteServer", telemetryContext, async () =>
            {
                using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    try
                    {
                        Console.WriteLine("calling PollForHearbeatAsync...");
                        await this.ServerApiClient.PollForHeartbeatAsync(TimeSpan.FromMinutes(5), cancellationToken);
                        await this.SaveStateAsync(telemetryContext, cancellationToken);
                        this.SetServerOnline(true);

                        if (this.IsMultiRoleLayout())
                        {
                            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                            {
                                await this.StartServerInstancesAsync(telemetryContext, cancellationToken);
                            }
                        }
                    }
                    finally
                    {
                        this.SetServerOnline(false);
                        await this.KillServerInstancesAsync(cancellationToken);
                    }
                }
            });
        }

        private Task DeleteStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();
            return this.Logger.LogMessageAsync($"{this.TypeName}.DeleteState", relatedContext, async () =>
            {
                using (HttpResponseMessage response = await this.ApiClient.DeleteStateAsync(nameof(State), cancellationToken))
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
            IEnumerable<IProcessProxy> processes = this.systemManagement.ProcessManager.GetProcesses("openssl");

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
                this.Logger.LogTraceMessage($"Restart openssl s_server Server(s)...", telemetryContext);
            }
            else
            {
                this.Logger.LogTraceMessage($"openssl s_server Running...", telemetryContext);
            }

            return shouldReset;
        }

        /// <summary>
        /// Initializes API client.
        /// </summary>
        private void InitializeApiClients(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();
                bool isSingleVM = !this.IsMultiRoleLayout();

                if (isSingleVM)
                {
                    this.ServerApiClient = clientManager.GetOrCreateApiClient(IPAddress.Loopback.ToString(), IPAddress.Loopback);
                }
                else
                {
                    ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                    IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                    this.ServerApiClient = clientManager.GetOrCreateApiClient(serverIPAddress.ToString(), serverIPAddress);
                    this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", this.ServerApiClient);
                }
            }
        }

        private Task SaveStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();
            return this.Logger.LogMessageAsync($"{this.TypeName}.SaveState", relatedContext, async () =>
            {
                // TODO : Add logic to save state for the server instance.
                var state = new Item<State>(nameof(State), new State());

                using (HttpResponseMessage response = await this.ApiClient.UpdateStateAsync(nameof(State), state, cancellationToken))
                {
                    relatedContext.AddResponseContext(response);
                    response.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
                }
            });
        }

        private Task StartServerInstancesAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.serverProcesses.Clear();

            Console.WriteLine("Starting OpenSSL Server Workload...");

            string commandArguments = this.Parameters.GetValue<string>(nameof(this.CommandArguments));
            // prefix path to cert and key files
            // "s_server -accept {ServerPort} -cert server.crt -key server.key -tls1_3 -WWW"

            string certPath = this.Package.Path + "/../../../tls-resources/";

            commandArguments = Regex.Replace(commandArguments, @"-cert\s+(\S+)", $"-cert {certPath}$1");
            commandArguments = Regex.Replace(commandArguments, @"-key\s+(\S+)", $"-key {certPath}$1");

            // copy resource files to openssl package directory
            string sourceDir = Path.Combine(this.Package.Path, "../../../tls-resources");
            string destDir = Path.Combine(this.Package.Path, "bin");

            if (this.fileSystem.Directory.Exists(sourceDir))
            {
                var htmlFiles = this.fileSystem.Directory.GetFiles(sourceDir, "*.html");
                foreach (var file in htmlFiles)
                {
                    string destFile = this.fileSystem.Path.Combine(destDir, this.fileSystem.Path.GetFileName(file));
                    this.fileSystem.File.Copy(file, destFile, overwrite: true);
                }
            } 
            else
            {
                throw new FileNotFoundException($"The source directory '{sourceDir}' does not exist. Cannot copy TLS resource files.");
            }

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext("executable", this.ExecutablePath)
                .AddContext("commandArguments", commandArguments);

            return this.Logger.LogMessageAsync($"{nameof(OpenSslServerExecutor)}.ExecuteWorkload", relatedContext, async () =>
            {
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    using (IProcessProxy process = this.systemManagement.ProcessManager.CreateProcess(this.ExecutablePath, commandArguments))
                    {
                        this.SetEnvironmentVariables(process);
                        this.CleanupTasks.Add(() => process.SafeKill());

                        try
                        {
                            await process.StartAndWaitAsync(cancellationToken).ConfigureAwait();

                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "OpenSSL", logToFile: true);

                                process.ThrowIfWorkloadFailed(successCodes: new int[] { 0, 137 });
                            }
                        }
                        finally
                        {
                            if (!process.HasExited)
                            {
                                process.Kill();
                            }
                        }
                    }
                }
            });

            /* EventContext relatedContext = telemetryContext.Clone()
                .AddContext("executable", this.ExecutablePath)
                .AddContext("commandArguments", commandArguments);

            Console.WriteLine($"exePath: {this.ExecutablePath}");
            Console.WriteLine($"cmdArgs: {this.CommandArguments}");
            return this.Logger.LogMessageAsync($"{nameof(OpenSslServerExecutor)}.ExecuteOpenSSL_Server_Workload", relatedContext, async () =>
            {
                 using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                 {
                     try
                     {
                         using (IProcessProxy process = await this.ExecuteCommandAsync("openssl", commandArguments, this.ExecutablePath, telemetryContext, cancellationToken, runElevated: true))
                         {
                             this.SetEnvironmentVariables(process);
                             Console.WriteLine("openssl server threw error");
                             if (!cancellationToken.IsCancellationRequested)
                             {
                                 ConsoleLogger.Default.LogMessage($"openssl  s_server process exited ", telemetryContext);
                                 await this.LogProcessDetailsAsync(process, telemetryContext, "openssl s_server");

                                 process.ThrowIfWorkloadFailed(successCodes: new int[] { 0, 137 });
                             }

                             // await this.CaptureMetricsAsync(process, commandArguments, telemetryContext, cancellationToken);
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

                 }
             }); 
            */
        }
    }
}