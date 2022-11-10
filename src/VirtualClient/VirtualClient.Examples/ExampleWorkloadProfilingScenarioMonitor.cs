// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// 
    /// </summary>
    public class ExampleWorkloadProfilingScenarioMonitor : VirtualClientComponent
    {
        private readonly object lockObject = new object();
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientComponent"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ExampleWorkloadProfilingScenarioMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            if (this.ProfilingMode == ProfilingMode.OnDemand)
            {
                VirtualClientEventing.SendReceiveInstructions += this.OnProfileSystemOnDemand;
            }
        }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                if (!this.disposed)
                {
                    if (this.ProfilingMode == ProfilingMode.OnDemand)
                    {
                        VirtualClientEventing.SendReceiveInstructions -= this.OnProfileSystemOnDemand;
                    }

                    this.disposed = true;
                }
            }
        }

        /// <summary>
        /// Executes the background monitoring process.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // All background monitors should return a background Task immediately to avoid blocking
            // the thread.
            return Task.Run(async () =>
            {
                try
                {
                    if (this.ProfilingEnabled && this.ProfilingMode != ProfilingMode.None)
                    {
                        if (this.ProfilingMode == ProfilingMode.OnDemand)
                        {
                            await this.ExecuteProfilingOnDemandAsync(cancellationToken)
                                .ConfigureAwait(false);
                        }
                        else if (this.ProfilingMode == ProfilingMode.Interval)
                        {
                            await this.ExecuteProfilingOnIntervalAsync(cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when a Task.Delay is cancelled.
                }
            });
        }

        private async Task ExecuteProfilingOnDemandAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // The profiler will cycle around doing nothing when it is in on-demand mode.
                    // It is waiting for notifications to profile during this time and will take action
                    // when it receives those notifications.
                    await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when a Task.Delay is cancelled.
                }
                catch
                {
                    // Continue to attempt running the profiler.
                }
            }
        }

        private async Task ExecuteProfilingOnIntervalAsync(CancellationToken cancellationToken)
        {
            DateTime nextProfilingTime = DateTime.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (DateTime.UtcNow >= nextProfilingTime)
                    {
                        nextProfilingTime = nextProfilingTime.Add(this.ProfilingInterval);

                        try
                        {
                            Console.WriteLine($"[{DateTime.Now}] Interval Monitor: Profiling Begin (interval={this.ProfilingInterval}, period={this.ProfilingPeriod})...");

                            DateTime profilingTime = DateTime.UtcNow.Add(this.ProfilingPeriod);
                            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow < profilingTime)
                            {
                                // Mimic running a profiler on an interval.
                                await Task.Delay(500, cancellationToken).ConfigureAwait(false);
                            }
                        }
                        finally
                        {
                            Console.WriteLine($"[{DateTime.Now}] Interval Monitor: Profiling End...");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when a Task.Delay is cancelled.
                }
                catch
                {
                    // Continue to attempt running the profiler.
                }
            }
        }

        private void OnProfileSystemOnDemand(object sender, InstructionsEventArgs args)
        {
            lock (this.lockObject)
            {
                try
                {
                    ProfilerInstructions instructions = (args.Instructions as ProfilerInstructions);

                    // We only care about requests/instructions to profile the system.
                    if (instructions != null)
                    {
                        Console.WriteLine($"[{DateTime.Now}] On-Demand Monitor: Profiling Begin (period={instructions.ProfilingPeriod}, warm-up={instructions.ProfilingWarmUpPeriod})...");

                        // Allow for the warm-up period.
                        Task.Delay(instructions.ProfilingWarmUpPeriod).GetAwaiter().GetResult();
                        Task profilingTask = Task.CompletedTask;

                        // There are 2 scenarios that we can support here depending upon how we want to implement
                        // the monitor/profiling.
                        if (instructions.ProfilingPeriod > TimeSpan.Zero)
                        {
                            // 1) The profiling runs for the period of time specified in the instructions (e.g.ProfilingPeriod)
                            //    irrespective of how long the caller/workload runs. Some workloads run for a long period of time
                            //    and we might not want to profile for that long.
                            profilingTask = Task.Run(() =>
                            {
                                Task.Delay(instructions.ProfilingPeriod).GetAwaiter().GetResult();
                            });

                            // Notice here that we are paying no attention to the cancellation token supplied in the
                            // instructions by the workload. We run until the profiler itself is done based upon the 
                            // profiling period defined/supplied in the instructions.
                            while (!profilingTask.IsCompleted)
                            {
                                Task.Delay(500, args.CancellationToken).GetAwaiter().GetResult();
                            }
                        }
                        else
                        {
                            // 2) The profiling runs for the period of time the workload runs. Some workloads do not have set
                            //    times for which they run and thus defining an accurate profiling period that corresponds with the
                            //    start and end of the workload would by challenging. In these cases, we want to run for the period
                            //    of time the workload runs and then exit gracefully as soon as possible. We can tell when the workload
                            //    has finished because the cancellation token supplied in the instructions will be signalled as cancellation
                            //    requested.
                            profilingTask = Task.Run(() =>
                            {
                                Random random = new Random();
                                TimeSpan randomProfilingPeriod = TimeSpan.FromSeconds(random.Next(30, 61));
                                Task.Delay(randomProfilingPeriod).GetAwaiter().GetResult();
                            });

                            // Notice here that we are paying attention to the cancellation token supplied in the 
                            // instructions by the workload. If the workload exits before the profiling is complete,
                            // the profiling should gracefully exit as well.
                            while (!profilingTask.IsCompleted && !args.CancellationToken.IsCancellationRequested)
                            {
                                // Typically, we would request the profiler to exit and allow it time to do so gracefully.
                                Task.Delay(500, args.CancellationToken).GetAwaiter().GetResult();
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected when a Task.Delay is cancelled.
                }
                catch
                {
                    // Continue to attempt running the profiler on subsequent requests.
                }
                finally
                {
                    Console.WriteLine($"[{DateTime.Now}] On-Demand Monitor: Profiling End...");
                }
            }
        }
    }
}
