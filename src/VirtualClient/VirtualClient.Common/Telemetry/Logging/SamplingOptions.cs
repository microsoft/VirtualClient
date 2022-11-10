// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System.Collections.Generic;

    /// <summary>
    /// Provides options/settings to used when applying a sampling mechanic
    /// to the capture of telemetry.
    /// </summary>
    public class SamplingOptions
    {
        /// <summary>
        /// Used to define a global set of sampling definitions that can be used across
        /// an application.
        /// </summary>
        public static List<SamplingOptions> Definitions { get; } = new List<SamplingOptions>();

        /// <summary>
        /// Gets the sample count which is the number of times
        /// an event has been sampled.
        /// </summary>
        public int EventCount { get; internal set; }

        /// <summary>
        /// Gets a name/identifier for the sampling options definition.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the sampling rate which defines how often an
        /// event will be sampled before it is logged.
        /// </summary>
        public int SamplingRate { get; set; }

        /// <summary>
        /// Gets true/false whether the telemetry event should be sampled/captured
        /// based upon the event count and sampling rate.
        /// </summary>
        public bool Sample
        {
            get
            {
                if (this.EventCount == int.MaxValue)
                {
                    this.EventCount = 0;
                }

                return this.EventCount == 0 || this.EventCount % this.SamplingRate == 0;
            }
        }
    }
}