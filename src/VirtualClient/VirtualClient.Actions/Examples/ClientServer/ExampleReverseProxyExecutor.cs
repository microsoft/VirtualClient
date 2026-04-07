// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// An example Virtual Client component responsible for executing a client-side role responsibilities 
    /// in the client/server workload.
    /// </summary>
    /// <remarks>
    /// This is on implementation pattern for client/server workloads. In this model, the client and server
    /// roles inherit from the <see cref="ExampleClientServerExecutor"/> and override behavior as
    /// is required by the particular role. For this particular example, the client role is responsible for
    /// confirming the server-side is online followed by running a workload against it.
    /// </remarks>
    internal class ExampleReverseProxyExecutor : ExampleClientServerExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ExampleReverseProxyExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// 'Port' parameter defined in the profile action.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port));
            }
        }

        /// <summary>
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        protected IAsyncPolicy ClientFlowRetryPolicy { get; set; }

        /// <summary>
        /// The timespan at which the client will poll the server for responses before
        /// timing out.
        /// </summary>
        protected TimeSpan PollingTimeout { get; set; }

        /// <summary>
        /// The API client for communications with the server.
        /// </summary>
        protected IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// The timespan at which the client will poll the server for responses before
        /// timing out.
        /// </summary>
        protected TimeSpan StateConfirmationTimeout { get; set; }

        /// <summary>
        /// The path to the web server host executable/scripts.
        /// </summary>
        protected string WebServerExecutablePath { get; set; }

        /// <summary>
        /// The path to the package that contains the web server host executable/scripts
        /// </summary>
        protected DependencyPath WebServerPackage { get; set; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                // The client polls for this state. We try to keep this simple. The existence of the state means that the
                // web server is online. The absence of the state means that it is not.
                this.StateManager.DeleteStateAsync(ExampleClientServerExecutor.ServerReadyState, cancellationToken)
                    .ConfigureAwait(false);

                IEnumerable<IPAddress> serverIpAddresses = this.GetTargetServers();

                // The reverse proxy is itself a client of the web servers. It needs to poll them each to ensure they
                // are all online before it starts its own web host up to serve client requests (as a proxy).
                this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.PollForServersOnlineAsync(serverIpAddresses, cancellationToken)
                            .ConfigureAwait(false);
                    }
                });

                // 6) Execute the client workload.
                // ===========================================================================
                await this.StartWebServerAsync(serverIpAddresses, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                this.StateManager.DeleteStateAsync(ExampleClientServerExecutor.ServerReadyState, cancellationToken)
                   .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.WebServerPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);

            if (this.Platform == PlatformID.Win32NT)
            {
                // On Windows the binary has a .exe in the name.
                this.WebServerExecutablePath = this.Combine(this.WebServerPackage.Path, "ExampleWorkload.exe");
            }
            else
            {
                // PlatformID.Unix
                // On Unix/Linux the binary does NOT have a .exe in the name. 
                this.WebServerExecutablePath = this.Combine(this.WebServerPackage.Path, "ExampleWorkload");

                // Binaries on Unix/Linux must be attributed with an attribute that makes them "executable".
                await this.SystemManagement.MakeFileExecutableAsync(this.WebServerExecutablePath, this.Platform, cancellationToken);
            }
        }

        private IEnumerable<IPAddress> GetTargetServers()
        {
            IEnumerable<ClientInstance> targetServers = this.GetLayoutClientInstances(ClientRole.Server);
            List<IPAddress> serverClients = new List<IPAddress>();

            foreach (ClientInstance server in targetServers)
            {
                serverClients.Add(IPAddress.Parse(server.IPAddress));
            }

            return serverClients;
        }

        private async Task PollForServersOnlineAsync(IEnumerable<IPAddress> serverIpAddresses, CancellationToken cancellationToken)
        {
            foreach (IPAddress ipAddress in serverIpAddresses)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    IApiClient serverApiClient = this.ApiClientManager.GetOrCreateApiClient(ipAddress.ToString(), ipAddress);

                    // 1) Confirm server is online.
                    // ===========================================================================
                    this.Logger.LogTraceMessage("Synchronization: Poll server API for heartbeat...");

                    await serverApiClient.PollForHeartbeatAsync(this.PollingTimeout, cancellationToken)
                        .ConfigureAwait(false);

                    // 2) Confirm the server-side application (e.g. web server) is online.
                    // ===========================================================================
                    this.Logger.LogTraceMessage("Synchronization: Poll server for web host online...");

                    await serverApiClient.PollForExpectedStateAsync<State>(
                        ExampleClientServerExecutor.ServerReadyState, (serverState) => serverState.Online(), this.StateConfirmationTimeout, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        private async Task StartWebServerAsync(IEnumerable<IPAddress> serverIpAddresses, CancellationToken cancellationToken)
        {
            // Run the web server
            // e.g.
            // ExampleWorkload.exe Api --port=4501 --apiServers=10.1.0.10,10.1.0.11,10.1.0.12
            string commandArguments = $"Api --port={this.Port} --apiServers={string.Join(",", serverIpAddresses.Select(ip => ip.ToString()))}";
            using (IProcessProxy webHostProcess = this.ProcessManager.CreateElevatedProcess(this.Platform, this.WebServerExecutablePath, commandArguments))
            {
                if (!webHostProcess.Start())
                {
                    throw new WorkloadException($"The API reverse proxy workload did not start as expected.", ErrorReason.WorkloadFailed);
                }

                this.CleanupTasks.Add(() => webHostProcess.SafeKill(this.Logger));

                // Notify clients that the reverse proxy server is online.
                Item<State> serverOnline = new Item<State>(ExampleClientServerExecutor.ServerReadyState, new State());
                serverOnline.Definition.Online(true);

                await this.StateManager.SaveStateAsync(ExampleClientServerExecutor.ServerReadyState, serverOnline, cancellationToken)
                    .ConfigureAwait(false);

                await webHostProcess.WaitForExitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            // If you do not have a process that you are running for the web server, you can still perform a simple sleep/waite.
            // It is important to keep the Virtual Client itself up and running in client/server workload scenarios because it
            // allows for transient issues on either side of the equation that cause VC itself to crash...handshake mechanics!
            // await this.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}