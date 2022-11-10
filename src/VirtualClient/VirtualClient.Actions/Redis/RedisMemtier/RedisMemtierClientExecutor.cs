// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.IO.Pipelines;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Amqp.Framing;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient;
    using VirtualClient.Actions;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// RedisMemtier Client Executor.
    /// </summary>
    public class RedisMemtierClientExecutor : RedisExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisMemtierClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public RedisMemtierClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.PollingTimeout = TimeSpan.FromMinutes(40);
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);
        }

        /// <summary>
        /// Number of clients per thread.
        /// </summary>
        public string NumberOfClients
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.NumberOfClients), out IConvertible numberOfClients);
                return numberOfClients?.ToString();
            }
        }

        /// <summary>
        /// Number of threads.
        /// </summary>
        public string NumberOfThreads
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.NumberOfThreads), out IConvertible numberOfThreads);
                return numberOfThreads?.ToString();
            }
        }

        /// <summary>
        /// Number of concurrent requests from client.
        /// </summary>
        public string PipelineDepth
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.PipelineDepth), out IConvertible pipelineDepth);
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
                this.Parameters.TryGetValue(nameof(this.DurationInSecs), out IConvertible durationInSecs);
                return durationInSecs?.ToString();
            }
        }

        /// <summary>
        /// Number of runs the client executes load on server.
        /// </summary>
        public string NumberOfRuns
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.NumberOfRuns), out IConvertible numberOfRuns);
                return numberOfRuns?.ToString();
            }
        }

        /// <summary>
        /// Path to RedisMemtier Script.
        /// </summary>
        protected string ClientExecutorPath { get; set; }

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
        /// Initializes the environment and dependencies for client of redis Memtier workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            this.ClientExecutorPath = this.PlatformSpecifics.Combine(this.MemtierPackagePath, "memtier_benchmark");
            this.InitializeApiClients();
        }

        /// <summary>
        /// Executes  client side.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
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

                            await serverApiClient.PollForHeartbeatAsync(this.PollingTimeout, cancellationToken)
                                .ConfigureAwait(false);

                            // 2) Confirm the server-side application (e.g. web server) is online.
                            // ===========================================================================
                            this.Logger.LogTraceMessage("Synchronization: Poll server for online signal...");

                            await serverApiClient.PollForServerOnlineAsync(TimeSpan.FromSeconds(30), cancellationToken)
                                .ConfigureAwait(false);

                            this.Logger.LogTraceMessage("Synchronization: Server online signal confirmed...");

                            this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                            // 3) Get Parameters required.

                            this.Copies = await this.GetServerCopiesCount(serverApiClient, cancellationToken)
                                                        .ConfigureAwait(false);

                            // 4) Execute the client workload.
                            // ===========================================================================
                            ipAddress = IPAddress.Parse(server.PrivateIPAddress);
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
                        this.Copies = await this.GetServerCopiesCount(this.ServerApiClient, cancellationToken)
                                                        .ConfigureAwait(false);

                        await this.ExecuteWorkloadAsync(ipAddress, telemetryContext, cancellationToken).ConfigureAwait(false);
                    }
                }));
            }

            return Task.WhenAll(clientWorkloadTasks);
        }

        private Task ExecuteWorkloadAsync(IPAddress ipAddress, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteWorkload", telemetryContext.Clone(), async () =>
            {
                string results = string.Empty;
                this.StartTime = DateTime.Now;
                for (int i = 1; i <= int.Parse(this.Copies); i++)
                {
                    int port = int.Parse(this.Port) + i - 1;
                    string clientCommand = $"{this.ClientExecutorPath} --server {ipAddress} --port {port} --protocol redis --clients {this.NumberOfClients} --threads {this.NumberOfThreads} --ratio 1:9 --data-size 32 --pipeline {this.PipelineDepth} --key-minimum 1 --key-maximum 10000000 --key-pattern R:R --run-count {this.NumberOfRuns} --test-time {this.DurationInSecs} --print-percentile 50,90,95,99,99.9 --random-data";

                    this.Logger.LogTraceMessage($"Executing process '{clientCommand}'  at directory '{this.MemtierPackagePath}'.");
                    results += await this.ExecuteCommandAsync<RedisMemtierClientExecutor>(clientCommand, this.MemtierPackagePath, cancellationToken)
                            .ConfigureAwait(false) + Environment.NewLine;
                }

                this.CaptureWorkloadResultsAsync(results, this.StartTime, DateTime.Now, telemetryContext, cancellationToken);

            });
        }

        /// <summary>
        /// Gets the parameters that define the scale configuration for the Redis memtier.
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

        private void CaptureWorkloadResultsAsync(string results, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    MemtierMetricsParser memtierMetricsParser = new MemtierMetricsParser(results);
                    IList<Metric> metrics = memtierMetricsParser.Parse();
                    this.Logger.LogMetrics(
                                "RedisMemtier",
                                scenarioName: this.Scenario,
                                startTime,
                                endTime,
                                metrics,
                                string.Empty,
                                this.Parameters.ToString(),
                                this.Tags,
                                telemetryContext);
                }
                catch (SchemaException exc)
                {
                    throw new WorkloadResultsException($"Failed to parse workload results.", exc, ErrorReason.WorkloadResultsParsingFailed);
                }
            }
        }
    }
}
