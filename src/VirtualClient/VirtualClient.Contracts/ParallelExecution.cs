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
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// A component that executes a set of child components in parallel.
    /// </summary>
    public class ParallelExecution : VirtualClientComponentCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelExecution"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ParallelExecution(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Executes all of the child components in-parallel.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            List<Task> componentTasks = new List<Task>();
            foreach (VirtualClientComponent component in this)
            {
                if (!VirtualClientComponent.IsSupported(component))
                {
                    this.Logger.LogMessage($"{nameof(ParallelExecution)} {component.TypeName} not supported on current platform: {this.PlatformArchitectureName}", LogLevel.Information, telemetryContext);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(component.Scenario))
                {
                    this.Logger.LogMessage($"{nameof(ParallelExecution)} Component = {component.TypeName} (scenario={component.Scenario})", LogLevel.Information, telemetryContext);
                }
                else
                {
                    this.Logger.LogMessage($"{nameof(ParallelExecution)} Component = {component.TypeName}", LogLevel.Information, telemetryContext);
                }

                componentTasks.Add(component.ExecuteAsync(cancellationToken));
            }

            return Task.WhenAll(componentTasks);
        }
    }
}
