// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents a set of instructions to supply to profilers/monitors running on the system.
    /// </summary>
    public class ProfilerInstructions : Instructions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Instructions"/> class.
        /// </summary>
        /// <param name="properties">Metadata properties associated with the state.</param>
        public ProfilerInstructions(IDictionary<string, IConvertible> properties = null)
            : base(InstructionsType.Profiling, properties)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Instructions"/> class.
        /// </summary>
        /// <param name="type">An identifier for the type of instructions.</param>
        /// <param name="properties">Metadata properties associated with the state.</param>
        [JsonConstructor]
        public ProfilerInstructions(InstructionsType type, IDictionary<string, IConvertible> properties = null)
            : base(type, properties)
        {
        }

        /// <summary>
        /// Defines the length of time to run profiling operations.
        /// </summary>
        public TimeSpan ProfilingPeriod
        {
            get
            {
                return this.Properties.GetTimeSpanValue(nameof(this.ProfilingPeriod), TimeSpan.Zero);
            }

            set
            {
                this.Properties[nameof(this.ProfilingPeriod)] = value.ToString();
            }
        }

        /// <summary>
        /// Defines the length of time to wait allowing the system to warm-up before running
        /// profiling operations.
        /// </summary>
        public TimeSpan ProfilingWarmUpPeriod
        {
            get
            {
                return this.Properties.GetTimeSpanValue(nameof(this.ProfilingWarmUpPeriod), TimeSpan.Zero);
            }

            set
            {
                this.Properties[nameof(this.ProfilingWarmUpPeriod)] = value.ToString();
            }
        }

        /// <summary>
        /// Defines true/false whether profiling is enabled.
        /// </summary>
        public bool ProfilingEnabled
        {
            get
            {
                return this.Properties.GetValue<bool>(nameof(this.ProfilingEnabled), false);
            }

            protected set
            {
                this.Properties[nameof(this.ProfilingEnabled)] = value;
            }
        }

        /// <summary>
        /// Defines the interval at which the monitor should be profiling. Within each of these intervals, the profiler
        /// will profile for a period of time as specified by the 'ProfilingPeriod' parameter. For example given the interval
        /// is 1 minute and the profiling period is 30 seconds, profiling will happen every 1 minute for 30 seconds.
        /// </summary>
        public TimeSpan ProfilingInterval
        {
            get
            {
                return this.Properties.GetTimeSpanValue(nameof(this.ProfilingInterval), TimeSpan.Zero);
            }

            protected set
            {
                this.Properties[nameof(this.ProfilingInterval)] = value.ToString();
            }
        }

        /// <summary>
        /// Defines the total number of iterations to execute the profiler on the system before stopping. 
        /// Profiler executions occur on specific intervals as defined by the 'ProfilingInterval' parameter. 
        /// Each of these executions counts as 1 towards the total number of iterations and profiling will stop 
        /// once the number of iterations defined is met.
        /// </summary>
        public int ProfilingIterations 
        {
            get
            {
                return this.Properties.GetValue<int>(nameof(this.ProfilingIterations), -1);
            }
            
            protected set
            {
                this.Properties[nameof(this.ProfilingIterations)] = value;
            }
        }

        /// <summary>
        /// Defines the scenario associated with the profiling request/operations.
        /// </summary>
        public string ProfilingScenario
        {
            get
            {
                this.Properties.TryGetValue(nameof(this.ProfilingScenario), out IConvertible profilingScenario);
                return profilingScenario?.ToString();
            }

            protected set
            {
                this.Properties[nameof(this.ProfilingScenario)] = value;
            }
        }
    }
}
