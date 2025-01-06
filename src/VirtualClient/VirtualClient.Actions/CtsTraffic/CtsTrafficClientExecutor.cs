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
    using MathNet.Numerics;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// CtsTraffic Client executor.
    /// Executes the transactions to the server.
    /// </summary>
    [SupportedPlatforms("win-arm64,win-x64")]
    public class CtsTrafficClientExecutor : CtsTrafficExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CtsTrafficClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public CtsTrafficClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.PollingInterval = TimeSpan.FromSeconds(20);
            this.PollingTimeout = TimeSpan.FromHours(1);
        }

        /// <summary>
        /// Parameter controls the total number of concurrent connections being made by client to the server(s) specified by –Target.
        /// </summary>
        public int Connections
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Connections), 1);
            }
        }

        /// <summary>
        /// Parameter controls the number of times ctsTraffic will cycle across all the connections specified with Connections.
        /// </summary>
        public int Iterations
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Iterations), 1);
            }
        }

        /// <summary>
        /// The interval at which polling requests should be made against the target server-side
        /// API for state changes.
        /// </summary>
        protected TimeSpan PollingInterval { get; set; }

        /// <summary>
        /// A time range at which the client will poll for expected state before timing out.
        /// </summary>
        protected TimeSpan PollingTimeout { get; set; }

        /// <summary>
        /// Executes the client workload.
        /// 1. Polls and waits for server to be started on primary port.
        /// 2. Executes the client workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // The VC server-side instance/API must be confirmed online.
            await this.ServerApiClient.PollForHeartbeatAsync(
                this.PollingTimeout,
                cancellationToken,
                this.PollingInterval);

            // The VC server-side instance/role must be confirmed ready for the client to operate (e.g. the database is initialized).
            await this.ServerApiClient.PollForServerOnlineAsync(
                this.PollingTimeout,
                cancellationToken,
                this.PollingInterval);

            // The CtsTraffic server must be confirmed to be initialized and ready.
            await this.ServerApiClient.PollForExpectedStateAsync<CtsTrafficServerState>(
                nameof(CtsTrafficServerState),
                (state => state.ServerSetupCompleted == true),
                this.PollingTimeout,
                cancellationToken,
                this.PollingInterval);

            ClientInstance instance = this.Layout.GetClientInstance(this.AgentId);
            string targetIPAddress = (instance.Role == ClientRole.Server) ? "localhost" : this.GetServerIpAddress();

            string ctsTrafficCommandArgs = $"-Target:{targetIPAddress} -Consoleverbosity:1 -StatusFilename:{this.StatusFileName} " +
            $@"-ConnectionFilename:{this.ConnectionsFileName} -ErrorFileName:{this.ErrorFileName} -Port:{this.Port} " +
            $@"-Connections:{this.Connections} -Pattern:{this.Pattern} -Iterations:{this.Iterations} -Transfer:{this.BytesToTransfer} " +
            $@"-Buffer:{this.BufferInBytes} -TimeLimit:150000";

            string numaNodeCommandArgs = $@"{this.NumaNodeIndex} ""{this.CtsTrafficExe} {ctsTrafficCommandArgs}""";

            if (this.NumaNodeIndex == -1)
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    this.CtsTrafficExe,
                    ctsTrafficCommandArgs,
                    this.CtsTrafficPackagePath,
                    telemetryContext,
                    cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "CtsTraffic", logToFile: true);
                        process.ThrowIfWorkloadFailed();

                        await this.CaptureMetricsAsync(process, ctsTrafficCommandArgs, telemetryContext, cancellationToken);
                    }
                }
            }
            else
            {
                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    this.ProcessInNumaNodeExe,
                    numaNodeCommandArgs,
                    this.CtsTrafficPackagePath,
                    telemetryContext,
                    cancellationToken))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "CtsTraffic", logToFile: true);
                        process.ThrowIfWorkloadFailed();

                        await this.CaptureMetricsAsync(process, numaNodeCommandArgs, telemetryContext, cancellationToken);
                    }
                }
            }
            
        }

        private string GetServerIpAddress()
        {
            string serverIPAddress = IPAddress.Loopback.ToString();

            if (this.IsMultiRoleLayout())
            {
                ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                serverIPAddress = serverInstance.IPAddress;
            }

            return serverIPAddress;
        }
    }
}