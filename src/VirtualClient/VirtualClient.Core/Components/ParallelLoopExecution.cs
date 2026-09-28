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
        /// The maximum duration to loop each child component. Minimum iterations are allowed to exceed this duration.
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
            System.Diagnostics.Stopwatch durationTimer = System.Diagnostics.Stopwatch.StartNew();

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
                componentTasks.Add(this.ExecuteComponentLoopAsync(component, durationTimer, telemetryContext, cancellationToken));
            }

            // Await all tasks to run in parallel.
            return Task.WhenAll(componentTasks);
        }

        /// <summary>
        /// Executes a component in an independent loop, restarting after completion while respecting the timeout.
        /// </summary>
        private async Task ExecuteComponentLoopAsync(
            VirtualClientComponent component,
            System.Diagnostics.Stopwatch durationTimer,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            int completedIterations = 0;
            TimeSpan longestIterationDuration = TimeSpan.Zero;

            while (!cancellationToken.IsCancellationRequested)
            {
                TimeSpan remainingDuration = this.Duration == Timeout.InfiniteTimeSpan
                    ? Timeout.InfiniteTimeSpan
                    : this.Duration - durationTimer.Elapsed;

                if (completedIterations >= this.MinimumIterations
                    && (remainingDuration <= TimeSpan.Zero
                        || (longestIterationDuration > TimeSpan.Zero && remainingDuration < longestIterationDuration)))
                {
                    this.Logger.LogMessage(
                        $"Parallel execution completed (duration = {this.Duration}, iterations = {completedIterations}).",
                        LogLevel.Trace,
                        telemetryContext);

                    break;
                }

                EventContext iterationContext = telemetryContext.Clone()
                    .AddContext("currentIteration", completedIterations + 1);

                using (CancellationTokenSource iterationCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    if (completedIterations >= this.MinimumIterations && remainingDuration != Timeout.InfiniteTimeSpan)
                    {
                        iterationCancellationSource.CancelAfter(remainingDuration);
                    }

                    System.Diagnostics.Stopwatch iterationTimer = System.Diagnostics.Stopwatch.StartNew();
                    try
                    {
                        await component.ExecuteAsync(iterationCancellationSource.Token);
                        iterationTimer.Stop();

                        completedIterations++;
                        if (iterationTimer.Elapsed > longestIterationDuration)
                        {
                            longestIterationDuration = iterationTimer.Elapsed;
                        }
                    }
                    catch (OperationCanceledException)
                        when (!cancellationToken.IsCancellationRequested && iterationCancellationSource.IsCancellationRequested)
                    {
                        this.Logger.LogMessage(
                            $"Parallel execution timed out (timeout = {this.Duration}).",
                            LogLevel.Trace,
                            iterationContext);

                        break;
                    }
                }
            }
        }
    }
}
