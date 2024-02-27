// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Rest;

    /// <summary>
    /// Example command to run a fake workload
    /// </summary>
    internal class RunWorkloadCommand
    {
        internal const string WorkloadDefault = "Default";
        internal const string WorkloadClientServer = "ClientServer";

        /// <summary>
        /// The timeout at which point the workload execution should stop.
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// The IP address of the server API against which the workload should run.
        /// </summary>
        public IPAddress IPAddress { get; set; }

        /// <summary>
        /// The port for the server API against which the workload should run.
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// The type of workload to run (e.g. Default, ClientServer, ReverseProxyServer).
        /// </summary>
        public string Workload { get; set; }

        /// <summary>
        /// Executes the default workload execution command.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public async Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            int exitCode = 0;

            try
            {
                CancellationToken cancellationToken = cancellationTokenSource.Token;
                if (this.Workload == RunWorkloadCommand.WorkloadClientServer)
                {
                    await this.RunClientServerWorkloadAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    await this.RunLocalWorkloadAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            catch
            {
                exitCode = 1;
            }

            return exitCode;
        }

        private async Task RunLocalWorkloadAsync(CancellationToken cancellationToken)
        {
            DateTime finishTime = DateTime.UtcNow.Add(this.Duration);

            // Mimic the workload doing actual work on the system.
            while (DateTime.UtcNow < finishTime && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(50).ConfigureAwait(false);
            }

            // Emit fake workload metrics.
            Random randomGen = new Random();

            JObject metricsOutput = JObject.Parse(
                $"{{ " +
                $"'workload_metric_1': {randomGen.Next(1000, 2000)}, " +
                $"'workload_metric_2': {randomGen.Next(2000, 3000)}, " +
                $"'workload_metric_3': {randomGen.Next(3000, 4000)}, " +
                $"'workload_metric_4': {randomGen.Next(4000, 5000)}, " +
                $"'workload_metric_5': {randomGen.Next(5000, 6000)}, " +
                $"'workload_metric_6': {randomGen.Next(6000, 10000)}, " +
                $"}}");

            Console.WriteLine(metricsOutput.ToString());
        }

        private async Task RunClientServerWorkloadAsync(CancellationToken cancellationToken)
        {
            IRestClient restClient = new RestClientBuilder()
                .AlwaysTrustServerCertificate()
                .AddAcceptedMediaType(MediaType.Json)
                .Build();

            ExampleApiClient apiClient = new ExampleApiClient(restClient, new Uri($"http://{this.IPAddress}:{this.Port}"));

            int requests = 0;
            int requestErrors = 0;
            List<double> responseTimes = new List<double>();

            DateTime finishTime = DateTime.UtcNow.Add(this.Duration);
            while (DateTime.UtcNow < finishTime)
            {
                try
                {
                    DateTime requestStart = DateTime.UtcNow;
                    await apiClient.GetSomethingAsync(cancellationToken)
                        .ConfigureAwait(false);

                    DateTime requestEnd = DateTime.UtcNow;
                    requests++;

                    responseTimes.Add((requestEnd - requestStart).TotalMilliseconds);
                }
                catch
                {
                    requestErrors++;
                }
            }

            JObject metricsOutput = JObject.Parse(
                $"{{ " +
                $"'workload_metric_1': {requests}, " +
                $"'workload_metric_2': {requestErrors}, " +
                $"'workload_metric_3': {this.Duration.TotalSeconds / requests}, " +
                $"'workload_metric_4': {responseTimes.Average()}, " +
                $"'workload_metric_5': {responseTimes.Min()}, " +
                $"'workload_metric_6': {responseTimes.Max()}, " +
                $"}}");

            Console.WriteLine(metricsOutput.ToString());
        }
    }
}
