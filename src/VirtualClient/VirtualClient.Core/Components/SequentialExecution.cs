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
    using VirtualClient.Logging;

    /// <summary>
    /// A component that executes a set of child components sequentially in a loop for a specified number of iterations.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
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
        /// The strategy to use to control the flow of child component execution. Default = Undefined.
        /// </summary>
        public string ExecutionStrategy
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.ExecutionStrategy), out IConvertible strategy);
                return strategy?.ToString() ?? Strategy.Undefined;
            }
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
                EventContext relatedContext = telemetryContext.Clone();
                relatedContext.AddContext("iteration", i + 1);
                relatedContext.AddContext("loopCount", this.LoopCount);

                string executionStrategy = this.ExecutionStrategy ?? Strategy.Undefined;
                switch (executionStrategy)
                {
                    case Strategy.Undefined:
                        await this.ExecuteDefaultStrategyAsync(telemetryContext, cancellationToken);
                        break;

                    case Strategy.Deterministic:
                        await this.ExecuteDeterministicStrategyAsync(telemetryContext, cancellationToken);
                        break;

                    default:
                        throw new NotSupportedException($"The execution strategy '{executionStrategy}' is not supported.");
                }
            }
        }

        private async Task ExecuteDefaultStrategyAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
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

                component.OutputComponentStart();
                await component.ExecuteAsync(cancellationToken);
            }
        }

        private async Task ExecuteDeterministicStrategyAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // With the deterministic strategy, we want to attempt to execute all of the child components even if some of them fail.
            // We will capture all of the exceptions and throw an exception at the end if any of the components failed.
            var errors = new List<Exception>();

            foreach (VirtualClientComponent component in this)
            {
                try
                {
                    if (!VirtualClientComponent.IsSupported(component))
                    {
                        this.Logger.LogMessage(
                            $"'{component.TypeName}' not supported on current platform: {this.PlatformArchitectureName}",
                            LogLevel.Information,
                            telemetryContext);

                        continue;
                    }

                    component.OutputComponentStart();
                    await component.ExecuteAsync(cancellationToken);
                }
                catch (Exception exc)
                {
                    errors.Add(exc);
                }
            }

            if (errors.Count > 0)
            {
                // Take the highest priority error reason if defined from the exceptions if available.
                ErrorReason? errorReason = null;
                foreach (Exception error in errors)
                {
                    VirtualClientException vcError = error as VirtualClientException;
                    if (vcError != null)
                    {
                        if (errorReason == null || vcError.Reason > errorReason)
                        {
                            errorReason = vcError.Reason;
                        }
                    }
                }

                // If not defined, assign a default for the component type with which the
                // sequential execution is defined.
                if (errorReason == null)
                {
                    if (this.ComponentType == ComponentType.Dependency)
                    {
                        errorReason = ErrorReason.DependencyInstallationFailed;
                    }
                    else if (this.ComponentType == ComponentType.Monitor)
                    {
                        errorReason = ErrorReason.MonitorFailed;
                    }
                    else
                    {
                        errorReason = ErrorReason.WorkloadFailed;
                    }
                }

                throw new ComponentException("Sequential operations failed.", new AggregateException(errors), errorReason.Value);
            }
        }

        private class Strategy
        {
            public const string Undefined = nameof(Undefined);
            public const string Deterministic = nameof(Deterministic);
        }
    }
}