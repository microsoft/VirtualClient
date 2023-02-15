// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
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
    /// MemcachedMemtier Client Executor.
    /// </summary>
    public class MemcachedMemtierClientExecutor : MemcachedExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemcachedMemtierClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public MemcachedMemtierClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);
            this.PollingTimeout = TimeSpan.FromMinutes(40);
        }

        /// <summary>
        /// Number of clients.
        /// </summary>
        public string ClientCountPerThread
        {
            get
            {
                this.Parameters.TryGetValue(nameof(MemcachedMemtierClientExecutor.ClientCountPerThread), out IConvertible clientCountPerThread);
                return clientCountPerThread?.ToString();
            }
        }

        /// <summary>
        /// Number of threads to be created at client side.
        /// </summary>
        public string ThreadCount
        {
            get
            {
                this.Parameters.TryGetValue(nameof(MemcachedMemtierClientExecutor.ThreadCount), out IConvertible threadCount);
                return threadCount?.ToString();
            }
        }

        /// <summary>
        /// Pipeline depth at client side.
        /// </summary>
        public string PipelineDepth 
        {
            get
            {
                this.Parameters.TryGetValue(nameof(MemcachedMemtierClientExecutor.PipelineDepth), out IConvertible pipelineDepth);
                return pipelineDepth?.ToString();
            }
        }

        /// <summary>
        /// Time for which client executes load on server.
        /// </summary>
        public string DurationInSecs
        {
            get
            {
                this.Parameters.TryGetValue(nameof(MemcachedMemtierClientExecutor.DurationInSecs), out IConvertible durationInSecs);
                return durationInSecs?.ToString();
            }
        }

        /// <summary>
        /// Number of runs the client executes load on server.
        /// </summary>
        public string RunCount
        {
            get
            {
                this.Parameters.TryGetValue(nameof(MemcachedMemtierClientExecutor.RunCount), out IConvertible runCount);
                return runCount?.ToString();
            }
        }

        /// <summary>
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        protected IAsyncPolicy ClientFlowRetryPolicy { get; set; }

        /// <summary>
        /// Path to RedisMemtier Script.
        /// </summary>
        protected string ClientExecutorPath { get; set; }

        /// <summary>
        /// The timespan at which the client will poll the server for responses before
        /// timing out.
        /// </summary>
        protected TimeSpan PollingTimeout { get; set; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IPAddress ipAddress;
            List<Task> clientWorkloadTasks = new List<Task>();

            if (this.IsMultiRoleLayout())
            {
                IEnumerable<ClientInstance> targetServers = this.GetLayoutClientInstances(ClientRole.Server);

                foreach (ClientInstance server in targetServers)
                {
                    clientWorkloadTasks.Add(this.ClientFlowRetryPolicy.ExecuteAsync(async () =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            IApiClient serverApiClient = this.ApiClientManager.GetOrCreateApiClient(server.Name, server);

                            // 1) Confirm server is online.
                            // ===========================================================================
                            this.Logger.LogTraceMessage("Synchronization: Poll server API for heartbeat...");

                            await serverApiClient.PollForHeartbeatAsync(this.PollingTimeout, cancellationToken)
                                .ConfigureAwait(false);

                            // 2) Confirm the server-side application (e.g. web server) is online.
                            // ===========================================================================
                            this.Logger.LogTraceMessage("Synchronization: Poll server for online signal...");

                            await serverApiClient.PollForServerOnlineAsync(TimeSpan.FromMinutes(10), cancellationToken)
                                .ConfigureAwait(false);

                            this.Logger.LogTraceMessage("Synchronization: Server online signal confirmed...");
                            this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                            // 3) Get Parameters required.
                            // ===========================================================================
                            this.Copies = await this.GetServerCopiesCount(serverApiClient, cancellationToken)
                                                        .ConfigureAwait(false);

                            // 4) Execute the client workload.
                            // ===========================================================================
                            ipAddress = IPAddress.Parse(server.IPAddress);
                            await this.ExecuteWorkloadAsync(ipAddress, telemetryContext, cancellationToken)
                                .ConfigureAwait(false);
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
                        this.Copies = await this.GetServerCopiesCount(this.ServerApiClient, cancellationToken);
                        await this.ExecuteWorkloadAsync(ipAddress, telemetryContext, cancellationToken);
                    }
                }));
            }

            return Task.WhenAll(clientWorkloadTasks);
        }

        /// <summary>
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            this.ClientExecutorPath = this.PlatformSpecifics.Combine(this.MemtierPackagePath, "memtier_benchmark");
            this.InitializeApiClients();
        }

        private void CaptureMetrics(string results, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                results.ThrowIfNullOrWhiteSpace(nameof(results));

                try
                {
                    MemcachedMemtierMetricsParser resultsParser = new MemcachedMemtierMetricsParser(results);
                    IList<Metric> workloadMetrics = resultsParser.Parse();

                    this.Logger.LogMetrics(
                        "Memcached-Memtier",
                        this.Scenario,
                        startTime,
                        endTime,
                        workloadMetrics,
                        null,
                        null,
                        this.Tags,
                        telemetryContext);
                }
                catch (SchemaException exc)
                {
                    throw new WorkloadResultsException($"Failed to parse workload results file.", exc, ErrorReason.WorkloadResultsParsingFailed);
                }
            }
        }

        private Task ExecuteWorkloadAsync(IPAddress serverIpAddress, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", telemetryContext.Clone(), async () =>
            {
                DateTime startTime = DateTime.UtcNow;
                string results = string.Empty;

                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    for (int i = 0; i < int.Parse(this.Copies); i++)
                    {
                        int port = int.Parse(this.Port) + i;
                        string command = $"-u {this.Username} {this.ClientExecutorPath}";
                        string commandArguments = 
                            $"--server {serverIpAddress} --port {port} --protocol {this.Protocol} --clients {this.ClientCountPerThread} --threads {this.ThreadCount} --ratio 1:9 --data-size 32 " +
                            $"--pipeline {this.PipelineDepth} --key-minimum 1 --key-maximum 10000000 --key-pattern R:R --run-count {this.RunCount} --test-time {this.DurationInSecs} --print-percentiles 50,90,95,99,99.9 --random-data";

                        using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, this.MemtierPackagePath, telemetryContext, cancellationToken, runElevated: true))
                        {
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                await this.LogProcessDetailsAsync(process, telemetryContext, "Memcached-Memtier", results: process.StandardOutput.ToString().AsArray(), logToFile: true);
                                process.ThrowIfWorkloadFailed();

                                results += $"{process.StandardOutput.ToString()}{Environment.NewLine}";
                            }
                        }
                    }

                    this.CaptureMetrics(results, startTime, DateTime.UtcNow, telemetryContext, cancellationToken);
                }
            });
        }

        /// <summary>
        /// Gets the parameters that define the scale configuration for the Memcahed memtier.
        /// </summary>
        private async Task<string> GetServerCopiesCount(IApiClient serverApiClient, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await serverApiClient.GetStateAsync(nameof(this.ServerCopiesCount), cancellationToken)
               .ConfigureAwait(false);

            response.ThrowOnError<WorkloadException>();

            string responseContent = await response.Content.ReadAsStringAsync()
                .ConfigureAwait(false);

            State state = responseContent.FromJson<Item<State>>().Definition;

            return state.Properties[nameof(this.ServerCopiesCount)].ToString();
        }
    }
}
