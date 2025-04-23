// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// A component that executes a set of child components continuously in parallel and independently.
    /// </summary>
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
                    this.Logger.LogMessage($"{nameof(ParallelLoopExecution)} {component.TypeName} not supported on current platform: {this.PlatformArchitectureName}", LogLevel.Information, telemetryContext);
                    continue;
                }

                // Wrap each component execution in a loop, and ensure we respect the timeout.
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
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string scenarioMessage = string.IsNullOrWhiteSpace(component.Scenario)
                    ? $"{nameof(ParallelLoopExecution)} Component = {component.TypeName}"
                    : $"{nameof(ParallelLoopExecution)} Component = {component.TypeName} (scenario={component.Scenario})";

                    this.Logger.LogMessage(scenarioMessage, LogLevel.Information, telemetryContext);

                    // Execute the component task with timeout handling.
                    Task componentExecutionTask = component.ExecuteAsync(cancellationToken);
                    Task completedTask = await Task.WhenAny(componentExecutionTask, this.timeoutTask);

                    if (completedTask == this.timeoutTask)
                    {
                        break;
                    }

                    await componentExecutionTask;
                }
                catch (Exception ex)
                {
                    throw new WorkloadException($"{component.TypeName} task execution failed.", ex, ErrorReason.WorkloadFailed);
                }
            }
        }
    }
}
