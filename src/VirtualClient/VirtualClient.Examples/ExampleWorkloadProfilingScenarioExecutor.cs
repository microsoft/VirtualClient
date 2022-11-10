// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// 
    /// </summary>
    public class ExampleWorkloadProfilingScenarioExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleWorkloadProfilingScenarioExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ExampleWorkloadProfilingScenarioExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Defines the length of time to run the workload.
        /// </summary>
        public TimeSpan WorkloadRuntime
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.WorkloadRuntime));
            }

            protected set
            {
                this.Parameters[nameof(this.WorkloadRuntime)] = value.ToString();
            }
        }

        /// <summary>
        /// Executes the background monitoring process.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now}] WorkloadExecutor: Starting...");


                // Mimic a long-running workload. We are using the profiling period here for the
                // case where the profiler would be running as long as the workload runs.
                using (BackgroundProfiling profiling = BackgroundProfiling.Begin(this, cancellationToken))
                {
                    DateTime workloadFinishTime = DateTime.UtcNow.Add(this.WorkloadRuntime);
                    while (DateTime.UtcNow < workloadFinishTime)
                    {
                        await Task.Delay(5000, cancellationToken).ConfigureAwait(false);
                        Console.WriteLine($"[{DateTime.Now}] .......................................Workload Running...");
                    }

                    Console.WriteLine($"[{DateTime.Now}] WorkloadExecutor: Finished.");
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when a Task.Delay is cancelled.
            }
        }
    }
}
