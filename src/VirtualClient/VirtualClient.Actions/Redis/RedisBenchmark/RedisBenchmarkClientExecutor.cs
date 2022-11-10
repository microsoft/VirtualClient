// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Redis Benchmark Client Executor.
    /// </summary>
    public class RedisBenchmarkClientExecutor : RedisExecutor
    { 
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisBenchmarkClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public RedisBenchmarkClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null) 
            : base(dependencies, parameters)
        {
            this.PollingTimeout = TimeSpan.FromMinutes(40);
            this.ClientFlowRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);
        }

        /// <summary>
        /// Number of requests from client.
        /// </summary>
        public string NumberOfRequests
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.NumberOfRequests), out IConvertible numberOfRequests);
                return numberOfRequests?.ToString();
            }
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
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        protected IAsyncPolicy ClientFlowRetryPolicy { get; set; }

        /// <summary>
        /// The timespan at which the client will poll the server for responses before
        /// timing out.
        /// </summary>
        protected TimeSpan PollingTimeout { get; set; }

        /// <summary>
        /// Path to RedisBenchmark Script.
        /// </summary>
        protected string ClientExecutablePath { get; set; }

        /// <summary>
        /// Initializes the environment and dependencies for client of redis Benchmark workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            this.ClientExecutablePath = this.PlatformSpecifics.Combine(this.RedisPackagePath, "src", "redis-benchmark");
            await this.SystemManager.MakeFileExecutableAsync(this.ClientExecutablePath, this.Platform, cancellationToken)
            .ConfigureAwait(false);
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

                            // 3) Execute the client workload.
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
                string clientCommand = @$"bash -c ""{this.ClientExecutablePath} -h {ipAddress} -p {this.Port} -c {this.NumberOfClients} -n {this.NumberOfRequests} -P {this.PipelineDepth} -q --csv""";

                this.StartTime = DateTime.Now;
                string results = await this.ExecuteCommandAsync<RedisMemtierClientExecutor>(clientCommand, this.PlatformSpecifics.Combine(this.RedisPackagePath, "src"), cancellationToken)
                                .ConfigureAwait(false);

                this.CaptureWorkloadResultsAsync(results, this.StartTime, DateTime.Now, telemetryContext, cancellationToken);

            });
        }

        private void CaptureWorkloadResultsAsync(string results, DateTime startTime, DateTime endTime, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    RedisBenchmarkMetricsParser redisBenchmarkMetricsParser = new RedisBenchmarkMetricsParser(results);
                    IList<Metric> metrics = redisBenchmarkMetricsParser.Parse();
                    this.Logger.LogMetrics(
                                "RedisBenchmark",
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
