// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Api;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    internal class ExampleWorkloadServerExecutor
    {
        private IDictionary<string, Workload> workloadServerTasks;

        public ExampleWorkloadServerExecutor()
        {
            this.workloadServerTasks = new Dictionary<string, Workload>();
        }

        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                this.ListenForEventNotifications(cancellationToken);

                while (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(100).ConfigureAwait(false);
                }
            });
        }

        private void ListenForEventNotifications(CancellationToken cancellationToken)
        {
            // Subscribe to the system-wide notifications. This is how we can receive events/notifications
            // from other clients in a direct push-based model.
            VirtualClientEventing.ReceiveInstructions += (sender, instructions) =>
            {
                State notification = instructions.ToObject<State>();

                if (notification.Properties.TryGetValue("WorkloadInstruction", out IConvertible instruction))
                {
                    string workload = notification.Properties.GetValue<string>("Workload");
                    if (string.Equals(instruction.ToString(), "Start", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[{DateTime.Now}] {workload} Server: Start instruction received.");
                        this.StartWorkload(workload, cancellationToken);
                    }
                    else if (string.Equals(instruction.ToString(), "Stop", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine($"[{DateTime.Now}] {workload} Server: Stop instruction received.");
                        this.StopWorkload(workload);
                    }
                }
            };
        }

        private void StartWorkload(string name, CancellationToken cancellationToken)
        {
            CancellationTokenSource cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            this.workloadServerTasks[name] = new Workload
            {
                Name = name,
                CancellationSource = cancellationSource,
                BackgroundTask = Task.Run(() =>
                {
                    try
                    {
                        using (ManualResetEventSlim waitHandle = new ManualResetEventSlim(false))
                        {
                            while (!cancellationSource.IsCancellationRequested)
                            {
                                Console.WriteLine($"[{DateTime.Now}] {name} Server: Running...");
                                waitHandle.Wait(2000, cancellationSource.Token);
                            }

                            Console.WriteLine($"[{DateTime.Now}] {name} Server: Workload stopped.");
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancellation
                    }
                })
            };
        }


        private void StopWorkload(string name)
        {
            if (this.workloadServerTasks.TryGetValue(name, out Workload workload))
            {
                try
                {
                    Console.WriteLine($"[{DateTime.Now}] {name} Server: Stopping Workload...");
                    workload.CancellationSource?.Cancel();
                    workload.BackgroundTask.GetAwaiter().GetResult();
                    workload.BackgroundTask.Dispose();
                }
                finally
                {
                    this.workloadServerTasks.Remove(name);
                }
            }
        }

        private class Workload
        {
            public string Name { get; set; }

            public Task BackgroundTask { get; set; }

            public CancellationTokenSource CancellationSource { get; set; }
        }
    }
}
