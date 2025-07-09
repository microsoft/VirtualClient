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
    /// A component that executes a set of child components sequentially in a loop for a specified number of iterations.
    /// </summary>
    public class SequentialExecution : VirtualClientComponentCollection
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SequentialExecution"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters"> Parameters defined in the execution profile or supplied to the Virtual Client on the command line. </param>
        public SequentialExecution(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// The number of times to execute the set of child components.
        /// </summary>
        public int LoopCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.LoopCount), 1);
            }
        }

        /// <summary>
        /// Executes all of the child components sequentially in a loop for the specified number of iterations.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            for (int i = 0; i < this.LoopCount && !cancellationToken.IsCancellationRequested; i++)
            {
                this.Logger.LogMessage(
                    $"{nameof(SequentialExecution)} Iteration '{i + 1}' of '{this.LoopCount}'",
                    LogLevel.Information,
                    telemetryContext);

                foreach (VirtualClientComponent component in this)
                {
                    if (!VirtualClientComponent.IsSupported(component))
                    {
                        this.Logger.LogMessage(
                            $"{nameof(SequentialExecution)} {component.TypeName} not supported on current platform: {this.PlatformArchitectureName}",
                            LogLevel.Information,
                            telemetryContext);
                        continue;
                    }

                    try
                    {
                        await component.ExecuteAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        throw new WorkloadException(
                            $"{component.TypeName} task execution failed.",
                            ex,
                            ErrorReason.WorkloadFailed);
                    }
                }
            }
        }
    }
}