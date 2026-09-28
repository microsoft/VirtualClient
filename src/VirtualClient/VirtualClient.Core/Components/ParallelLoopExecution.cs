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
        /// The maximum time to loop the child components. Once the minimum iterations are completed, an in-flight
        /// iteration is cancelled when this time elapses.
        /// </summary>
        public TimeSpan Duration
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.Duration), TimeSpan.FromMilliseconds(-1));
            }
        }

        /// <summary>
        /// The number of iterations each child component must complete, even if the duration elapses. Default = 0.
        /// </summary>
        public int MinimumIterations
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.MinimumIterations), 0);
            }
        }

        /// <summary>
        /// Executes all of the child components continuously in parallel, respecting the specified timeout.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            List<Task> componentTasks = new List<Task>();
            CancellationTokenSource durationSource = new CancellationTokenSource();

            try
            {
                if (this.Duration != Timeout.InfiniteTimeSpan)
                {
                    durationSource.CancelAfter(this.Duration);
                }

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
                    componentTasks.Add(this.ExecuteComponentLoopAsync(component, durationSource.Token, telemetryContext, cancellationToken));
                }

                // Await all tasks to run in parallel.
                await Task.WhenAll(componentTasks);
            }
            finally
            {
                durationSource.Dispose();
            }
        }

        /// <summary>
        /// Executes a component in an independent loop, restarting after completion while respecting the timeout.
        /// </summary>
        private async Task ExecuteComponentLoopAsync(
            VirtualClientComponent component,
            CancellationToken durationToken,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            int completedIterations = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                bool minimumSatisfied = completedIterations >= this.MinimumIterations;

                if (minimumSatisfied && durationToken.IsCancellationRequested)
                {
                    this.Logger.LogMessage(
                        $"Parallel execution timed out (timeout = {this.Duration}).",
                        LogLevel.Trace,
                        telemetryContext);

                    break;
                }

                EventContext iterationContext = telemetryContext.Clone()
                    .AddContext("currentIteration", completedIterations + 1);

                // Iterations required to satisfy MinimumIterations are allowed to run past the duration.
                using (CancellationTokenSource iterationSource = minimumSatisfied
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, durationToken)
                    : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    await component.ExecuteAsync(iterationSource.Token);

                    // Components swallow cancellation, so a timed-out iteration returns normally rather than throwing.
                    if (iterationSource.IsCancellationRequested)
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.Logger.LogMessage(
                                $"Parallel execution timed out (timeout = {this.Duration}).",
                                LogLevel.Trace,
                                iterationContext);
                        }

                        break;
                    }
                }

                completedIterations++;
            }
        }
    }
}
