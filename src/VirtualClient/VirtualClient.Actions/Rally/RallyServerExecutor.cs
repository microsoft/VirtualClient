// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The Rally Server workload executor.
    /// </summary>
    public class RallyServerExecutor : RallyExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RallyServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public RallyServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Initializes the environment for execution of the Rally workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            string ipAddress = string.Empty;

            if (this.IsMultiRoleLayout())
            {
                ClientInstance serverInstance = this.GetLayoutClientInstance();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);
                ipAddress = serverIPAddress.ToString();
            }
            else
            {
                ipAddress = IPAddress.Loopback.ToString();
            }

            await this.Logger.LogMessageAsync($"{this.TypeName}.ConfigureServer", telemetryContext.Clone(), async () =>
            {
                RallyState state = await this.StateManager.GetStateAsync<RallyState>(nameof(RallyState), cancellationToken)
                    ?? new RallyState();

                if (!state.RallyConfigured)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // configure-server.py does the following:
                        //      - increases the vm.max_map_count variable
                        //      - assigns ownership of the data directory to current user (esrally cannot run as root)
                        //      - initializes rally.ini config file
                        //      - assigns the rally directory to the chosen disk data directory (too many documents to keep in memory)
                        //      - starts the rally daemon

                        string configArguments = $"--directory {this.DataDirectory} --user {this.CurrentUser} --clientIp {this.ClientIpAddress} --serverIp {ipAddress}";
                        string arguments = $"{this.RallyPackagePath}/configure-server.py ";

                        using (IProcessProxy process = await this.ExecuteCommandAsync(
                            RallyExecutor.PythonCommand,
                            arguments + configArguments,
                            this.RallyPackagePath,
                            telemetryContext,
                            cancellationToken))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "ElasticsearchRally", logToFile: true);
                                process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadUnexpectedAnomaly);
                            }
                        }

                        state.RallyConfigured = true;
                        await this.StateManager.SaveStateAsync<RallyState>(nameof(RallyState), state, cancellationToken);
                    }
                }
            });
        }

        /// <summary>
        /// Executes server side of workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(RallyServerExecutor)}.ExecuteServer", telemetryContext, async () =>
            {
                using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    try
                    {
                        this.SetServerOnline(true);

                        if (this.IsMultiRoleLayout())
                        {
                            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                            {
                                await this.WaitAsync(cancellationToken);
                            }
                        }
                    }
                    finally
                    {
                        this.SetServerOnline(false);
                    }
                }
            });
        }
    }
}
