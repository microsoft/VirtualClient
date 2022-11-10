// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Contracts;

    internal class ExampleWorkloadClientExecutor
    {
        public ExampleWorkloadClientExecutor()
        {
        }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    VirtualClientApiClient apiClient = DependencyFactory.CreateVirtualClientApiClient(IPAddress.Loopback, 5000);

                    await this.ExecuteNttcpWorkload(apiClient, cancellationToken).ConfigureAwait(false);
                    await this.ExecuteLatteWorkload(apiClient, cancellationToken).ConfigureAwait(false);
                }
            });
        }

        private async Task ExecuteLatteWorkload(VirtualClientApiClient apiClient, CancellationToken cancellationToken)
        {
            // 1) Poll to ensure the Virtual Client server-side API is online
            await this.PollForServerHeartbeat(apiClient, cancellationToken).ConfigureAwait(false);

            State startWorkloadInstruction = new State(new Dictionary<string, IConvertible>
            {
                ["Workload"] = "Latte",
                ["WorkloadInstruction"] = "Start"
            });

            // 2) Send a request to the server to start up the NTTCP server-side workload. The server should not
            //    return until it has confirmed the workload is running. It returns an HTTP OK/200 response to 
            //    indicate success.
            HttpResponseMessage response = await apiClient.SendInstructionsAsync(JObject.FromObject(startWorkloadInstruction), cancellationToken)
              .ConfigureAwait(false);

            response.ThrowOnError<WorkloadException>();

            // 3) Mimic the workload running. In a real-life scenario, we would be running the workload and
            //    waiting for it to finish where we would then read the results and write the metrics to telemetry.
            DateTime exitTime = DateTime.Now.AddSeconds(20);
            while (DateTime.Now < exitTime)
            {
                // Mimic the workload running.
                Console.WriteLine($"[{DateTime.Now}] Latte Client: Workload Running...");
                await Task.Delay(2000).ConfigureAwait(false);
            }

            State stopWorkloadInstruction = new State(new Dictionary<string, IConvertible>
            {
                ["Workload"] = "Latte",
                ["WorkloadInstruction"] = "Stop"
            });

            // 4) To ensure a clean shutdown on the server-side, we make an explicit call to stop
            //    the server-side workload.
            response = await apiClient.SendInstructionsAsync(JObject.FromObject(stopWorkloadInstruction), cancellationToken)
                .ConfigureAwait(false);

            response.ThrowOnError<WorkloadException>();
        }

        private async Task ExecuteNttcpWorkload(VirtualClientApiClient apiClient, CancellationToken cancellationToken)
        {
            // 1) Poll to ensure the Virtual Client server-side API is online
            await this.PollForServerHeartbeat(apiClient, cancellationToken).ConfigureAwait(false);

            State startWorkloadInstruction = new State(new Dictionary<string, IConvertible>
            {
                ["Workload"] = "NTTCP",
                ["WorkloadInstruction"] = "Start"
            });

            // 2) Send a request to the server to start up the NTTCP server-side workload. The server should not
            //    return until it has confirmed the workload is running. It returns an HTTP OK/200 response to 
            //    indicate success.
            HttpResponseMessage response = await apiClient.SendInstructionsAsync(JObject.FromObject(startWorkloadInstruction), cancellationToken)
              .ConfigureAwait(false);

            response.ThrowOnError<WorkloadException>();

            // 3) Mimic the workload running. In a real-life scenario, we would be running the workload and
            //    waiting for it to finish where we would then read the results and write the metrics to telemetry.
            DateTime exitTime = DateTime.Now.AddSeconds(20);
            while (DateTime.Now < exitTime)
            {
                // Mimic the workload running.
                Console.WriteLine($"[{DateTime.Now}] NTTCP Client: Workload Running...");
                await Task.Delay(2000).ConfigureAwait(false);
            }

            State stopWorkloadInstruction = new State(new Dictionary<string, IConvertible>
            {
                ["Workload"] = "NTTCP",
                ["WorkloadInstruction"] = "Stop"
            });

            // 4) To ensure a clean shutdown on the server-side, we make an explicit call to stop
            //    the server-side workload.
            response = await apiClient.SendInstructionsAsync(JObject.FromObject(stopWorkloadInstruction), cancellationToken)
                .ConfigureAwait(false);

            response.ThrowOnError<WorkloadException>();
        }

        private async Task PollForServerHeartbeat(VirtualClientApiClient apiClient, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                HttpResponseMessage response = null;

                try
                {
                    
                    response = await apiClient.GetHeartbeatAsync(CancellationToken.None).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[{DateTime.Now}] Workload server confirmed online...");
                        break;
                    }
                }
                catch
                {
                    // Expected when the API is not online.
                }
                finally
                {
                    Task.Delay(5000).GetAwaiter().GetResult();
                }
            }
        }
    }
}
