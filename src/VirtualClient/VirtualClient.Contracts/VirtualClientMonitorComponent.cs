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
    /// An abstract class representing a monitoring component.
    /// </summary>
    public abstract class VirtualClientMonitorComponent : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientIntervalBasedMonitor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public VirtualClientMonitorComponent(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            if (this.MonitorEnabled && this.MonitorFrequency == null && this.MonitorIterations == null && this.MonitorStrategy == null)
            {
                this.MonitorStrategy = VirtualClient.MonitorStrategy.Once;
            }
        }

        /// <summary>
        /// Defines true/false whether the monitor is enabled.
        /// </summary>
        public bool MonitorEnabled
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.MonitorEnabled), true);
            }

            protected set
            {
                this.Parameters[nameof(this.MonitorEnabled)] = value;
            }
        }

        /// <summary>
        /// The frequency with which this monitor runs
        /// </summary>
        public TimeSpan? MonitorFrequency
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.MonitorFrequency));
            }

            protected set
            {
                this.Parameters[nameof(this.MonitorFrequency)] = value.ToString();
            }
        }

        /// <summary>
        /// The number of iterations with which this monitor runs
        /// Default is set to -1 for infinite loop.
        /// </summary>
        public long? MonitorIterations
        {
            get
            {
                return this.Parameters.GetValue<long>(nameof(this.MonitorIterations));
            }

            protected set
            {
                this.Parameters[nameof(this.MonitorIterations)] = value.ToString();
            }
        }

        /// <summary>
        /// Defines a monitoring strategy for more complex monitoring cadences.
        /// </summary>
        public MonitorStrategy? MonitorStrategy
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.MonitorStrategy), out IConvertible strategy);
                return strategy != null ? Enum.Parse<MonitorStrategy>(strategy.ToString()) : null;
            }

            protected set
            {
                this.Parameters[nameof(this.MonitorStrategy)] = value.ToString();
            }
        }

        /// <summary>
        /// The frequency with which this monitor runs
        /// </summary>
        public TimeSpan? MonitorWarmupPeriod
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.MonitorWarmupPeriod));
            }

            protected set
            {
                this.Parameters[nameof(this.MonitorWarmupPeriod)] = value.ToString();
            }
        }

        /// <summary>
        /// Returns true/false whether the current iterations provided exceeds the 
        /// configured iterations.
        /// </summary>
        /// <param name="currentIteration">The current number of iterations.</param>
        /// <returns>True if the expected iterations are completed. False if not.</returns>
        public bool IsIterationComplete(long currentIteration)
        {
            if (this.MonitorIterations <= 0)
            {
                return false;
            }

            return currentIteration > this.MonitorIterations;
        }

        /// <summary>
        /// Executes the monitoring workflow.
        /// </summary>
        /// <param name="telemetryContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // All background monitor ExecuteAsync methods should be either 'async' or should use a Task.Run() if running a 'while' loop or the
            // logic will block without returning. Monitors are typically expected to be fire-and-forget.

            return Task.Run(async () =>
            {
                if (this.MonitorEnabled)
                {
                    // Wait for the warmup period before proceeding.
                    await this.WaitAsync(this.MonitorWarmupPeriod ?? TimeSpan.Zero, cancellationToken);

                    if (this.MonitorFrequency != null)
                    {
                        await this.ExecuteMonitorOnIntervalAsync(telemetryContext, cancellationToken);
                    }
                }
            });
        }

        /// <summary>
        /// When implemented executes the monitoring operations.
        /// </summary>
        /// <param name="telemetryContext">Provides information to include with telemetry.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        protected abstract Task ExecuteMonitorAsync(EventContext telemetryContext, CancellationToken cancellationToken);

        /// <summary>
        /// Executes the monitor on an interval as defined by the 'MonitorFrequency' parameter.
        /// </summary>
        /// <param name="telemetryContext">Provides information to include with telemetry.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        protected Task ExecuteMonitorOnIntervalAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                }
                catch (OperationCanceledException)
                {
                    // Expected on ctrl-c or a cancellation is requested.
                }
                catch (Exception exc)
                {
                    // Do not let the monitor operations crash the application.
                    this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Validates the monitor requirements.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();

            if (this.MonitorFrequency != null && this.MonitorIterations != null)
            {
                throw new MonitorException(
                    $"Invalid parameter usage. The parameters '{nameof(this.MonitorFrequency)}' and '{nameof(this.MonitorIterations)}' cannot be used together.",
                    ErrorReason.NotSupported);
            }

            if (this.MonitorFrequency != null && this.MonitorStrategy != null)
            {
                throw new MonitorException(
                    $"Invalid parameter usage. The parameters '{nameof(this.MonitorFrequency)}' and '{nameof(this.MonitorStrategy)}' cannot be used together.",
                    ErrorReason.NotSupported);
            }

            if (this.MonitorIterations != null && this.MonitorStrategy != null)
            {
                throw new MonitorException(
                    $"Invalid parameter usage. The parameters '{nameof(this.MonitorIterations)}' and '{nameof(this.MonitorStrategy)}' cannot be used together.",
                    ErrorReason.NotSupported);
            }
        }
    }
}
