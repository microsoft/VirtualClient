// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// A component that executes a set of child components continuously in parallel and independently.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class ParallelLoopExecution : VirtualClientComponentCollection
    {
        private Task timeoutTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelLoopExecution"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ParallelLoopExecution(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// The maximum duration to allow each child component to run.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.Duration), TimeSpan.FromMilliseconds(-1));
            }
        }

        /// <summary>
        /// The minimum number of times each child component should run. Default set to 1.
        /// </summary>
        public int MinimumIterations
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.MinimumIterations), 1);
            }
        }

        /// <summary>
        /// Executes all of the child components continuously in parallel, respecting the specified timeout.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            List<Task> componentTasks = new List<Task>();
            this.timeoutTask = Task.Delay(this.Duration, cancellationToken);

            foreach (VirtualClientComponent component in this)
            {
                if (!VirtualClientComponent.IsSupported(component))
                {
                    this.Logger.LogMessage(
                        $"{nameof(ParallelLoopExecution)} {component.TypeName} not supported on current platform: {this.PlatformArchitectureName}", 
                        LogLevel.Information, 
                        telemetryContext);

                    continue;
                }

                // Wrap each component execution in a loop, and ensure we respect the timeout.
                component.OutputComponentStart();
                componentTasks.Add(this.ExecuteComponentLoopAsync(component, telemetryContext, cancellationToken));
            }

            // Await all tasks to run in parallel.
            return Task.WhenAll(componentTasks);
        }

        /// <summary>
        /// Executes a component in an independent loop, restarting after completion while respecting the timeout.
        /// </summary>
        private async Task ExecuteComponentLoopAsync(VirtualClientComponent component, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int currentIteration = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    currentIteration++;
                    telemetryContext.AddContext("currentIteration", currentIteration);

                    if (this.timeoutTask.IsCompleted && currentIteration > this.MinimumIterations)
                    {
                        this.Logger.LogMessage(
                            $"Parallel execution timed out (timeout = {this.Duration}).",
                            LogLevel.Trace,
                            telemetryContext);

                        break;
                    }

                    // Execute the component task with timeout handling.
                    Task componentExecutionTask = component.ExecuteAsync(cancellationToken);
                    Task completedTask = await Task.WhenAny(componentExecutionTask, this.timeoutTask);

                    if (completedTask == this.timeoutTask && currentIteration >= this.MinimumIterations)
                    {
                        break;
                    }

                    await componentExecutionTask;
                }
                finally
                {
                    currentIteration++;
                }
            }
        }
    }
}
