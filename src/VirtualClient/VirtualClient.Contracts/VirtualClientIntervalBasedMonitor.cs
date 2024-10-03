// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// An abstract class representing one monitor of the virtual client
    /// </summary>
    public abstract class VirtualClientIntervalBasedMonitor : VirtualClientComponent
    {
        /// <summary>
        /// The default frequency that monitors run at (every five minutes)
        /// </summary>
        private static TimeSpan defaultFrequency = TimeSpan.FromMinutes(5);
        private static TimeSpan defaultWarmupPeriod = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientIntervalBasedMonitor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public VirtualClientIntervalBasedMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Defines true/false whether the monitor is enabled.
        /// </summary>
        public bool MonitorEnabled
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.MonitorEnabled), false);
            }

            protected set
            {
                this.Parameters[nameof(this.MonitorEnabled)] = value;
            }
        }

        /// <summary>
        /// The frequency with which this monitor runs
        /// </summary>
        public TimeSpan MonitorFrequency
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(
                    nameof(VirtualClientIntervalBasedMonitor.MonitorFrequency), VirtualClientIntervalBasedMonitor.defaultFrequency);
            }
        }

        /// <summary>
        /// The frequency with which this monitor runs
        /// </summary>
        public TimeSpan MonitorWarmupPeriod
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(
                    nameof(VirtualClientIntervalBasedMonitor.MonitorWarmupPeriod), VirtualClientIntervalBasedMonitor.defaultWarmupPeriod);
            }
        }

        /// <summary>
        /// The number of iterations with which this monitor runs
        /// Default is set to -1 for infinite loop.
        /// </summary>
        public long MonitorIterations
        {
            get
            {
                return this.Parameters.GetValue<long>(
                    nameof(VirtualClientIntervalBasedMonitor.MonitorIterations), -1);
            }
        }
    }
}
