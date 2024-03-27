// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The base class for all Virtual Client profile actions and monitors.
    /// </summary>
    public abstract class VirtualClientMultiRoleComponent : VirtualClientComponent
    {


        /// <summary>
        /// 
        /// </summary>
        /// <param name="dependencies"></param>
        /// <param name="parameters"></param>
        protected VirtualClientMultiRoleComponent(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null) 
            : base(dependencies, parameters)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected Task WaitForRoleAsync(string role, CancellationToken cancellationToken)
        {
            if (!this.WarmUp || !this.IsServerWarmedUp)
            {
                IPAddress ipAddress;
                List<Task> clientWorkloadTasks = new List<Task>();

                if (this.IsMultiRoleLayout())
                {
                    IEnumerable<ClientInstance> targetServers = this.GetLayoutClientInstances(ClientRole.Server);
                    foreach (ClientInstance server in targetServers)
                    {
                        // Reliability/Recovery:
                        // The pattern here is to allow for any steps within the workflow to fail and to simply start the entire workflow
                        // over again.
                        clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                IApiClient serverApiClient = this.ApiClientManager.GetOrCreateApiClient(server.Name, server);

                                // 1) Confirm server is online.
                                // ===========================================================================
                                this.Logger.LogTraceMessage("Synchronization: Poll server API for heartbeat...");
                                await serverApiClient.PollForHeartbeatAsync(this.PollingTimeout, cancellationToken);

                                // 2) Confirm the server-side application (e.g. web server) is online.
                                // ===========================================================================
                                this.Logger.LogTraceMessage("Synchronization: Poll server for online signal...");
                                await serverApiClient.PollForServerOnlineAsync(TimeSpan.FromSeconds(30), cancellationToken);

                                this.Logger.LogTraceMessage("Synchronization: Server online signal confirmed...");
                                this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                                // 3) Get Parameters required.
                                ServerState serverState = await this.GetServerStateAsync(serverApiClient, cancellationToken);

                                // 4) Execute the client workload.
                                // ===========================================================================
                                ipAddress = IPAddress.Parse(server.IPAddress);
                                await this.ExecuteWorkloadsAsync(ipAddress, serverState, telemetryContext, cancellationToken);
                            }
                        }));
                    }
                }
                else
                {
                    ipAddress = IPAddress.Loopback;
                    clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            ServerState serverState = await this.GetServerStateAsync(this.ServerApiClient, cancellationToken);
                            await this.ExecuteWorkloadsAsync(ipAddress, serverState, telemetryContext, cancellationToken);
                        }
                    }));
                }
            }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="role"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected Task TerminateRoleAsync(string role, CancellationToken cancellationToken)
        {

        }


    }
}