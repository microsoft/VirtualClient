// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The Sysbench Server workload executor.
    /// </summary>
    public class SysbenchOLTPServerExecutor : SysbenchOLTPExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SysbenchOLTPServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public SysbenchOLTPServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.StateManager = this.Dependencies.GetService<IStateManager>();
        }

        /// <summary>
        /// Provides access to the local state management facilities.
        /// </summary>
        protected IStateManager StateManager { get; }

        /// <summary>
        /// Initializes the environment and dependencies for server of sysbench workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            this.InitializeApiClients();
        }

        /// <summary>
        /// Executes server side of workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(SysbenchOLTPServerExecutor)}.ExecuteServer", telemetryContext, async () =>
            {
                using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    if (!this.IsMultiRoleLayout())
                    {
                        await this.WaitAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        try
                        {
                            IEnumerable<IProcessProxy> processProxyList = this.ProcessesRunning("mysqld");

                            this.Logger.LogTraceMessage($"API server workload online awaiting client requests...");

                            this.SetServerOnline(true);

                            if (processProxyList != null)
                            {
                                foreach (IProcessProxy process in processProxyList)
                                {
                                    this.CleanupTasks.Add(() => process.SafeKill());
                                    await process.WaitAsync(cancellationToken)
                                        .ConfigureAwait(false);
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                        }
                        finally
                        {
                            // Always signal to clients that the server is offline before exiting. This helps to ensure that the client
                            // and server have consistency in handshakes even if one side goes down and returns at some other point.
                            this.SetServerOnline(false);
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Returns list of processes running.
        /// </summary>
        /// <param name="processName">Name of the process.</param>
        /// <returns>List of processes running.</returns>
        private IEnumerable<IProcessProxy> ProcessesRunning(string processName)
        {
            IEnumerable<IProcessProxy> processProxyList = null;
            List<Process> processlist = new List<Process>(Process.GetProcesses());
            foreach (Process process in processlist)
            {
                if (process.ProcessName.Contains(processName))
                {
                    Process[] processesByName = Process.GetProcessesByName(process.ProcessName);
                    if (processesByName?.Any() ?? false)
                    {
                        if (processProxyList == null)
                        {
                            processProxyList = processesByName.Select((Process process) => new ProcessProxy(process));
                        }
                        else
                        {
                            foreach (Process proxy in processesByName)
                            {
                                processProxyList.Append(new ProcessProxy(proxy));
                            }
                        }
                    }
                }
            }

            return processProxyList;
        }
    }
}
