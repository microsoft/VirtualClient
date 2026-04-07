// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Settings to apply to the operations of a given component.
    /// </summary>
    public class ComponentSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentSettings"/> class.
        /// </summary>
        /// <param name="parameters">Optional parameters to use for defining the settings.</param>
        public ComponentSettings(IDictionary<string, IConvertible> parameters = null)
        {
            if (parameters?.Any() == true)
            {
                if (parameters.TryGetValue(nameof(this.ContentPathTemplate), out IConvertible contentPathTemplate))
                {
                    this.ContentPathTemplate = contentPathTemplate?.ToString();
                }

                if (parameters.TryGetValue(nameof(this.ExitWait), out IConvertible exitWaitValue) && TimeSpan.TryParse(exitWaitValue?.ToString(), out TimeSpan exitWait))
                {
                    this.ExitWait = exitWait;
                }

                if (parameters.TryGetValue(nameof(this.FailFast), out IConvertible failFastValue) && bool.TryParse(failFastValue?.ToString(), out bool failFast))
                {
                    this.FailFast = failFast;
                }

                if (parameters.TryGetValue(nameof(this.LogToFile), out IConvertible logToFileValue) && bool.TryParse(logToFileValue?.ToString(), out bool logToFile))
                {
                    this.LogToFile = logToFile;
                }

                if (parameters.TryGetValue(nameof(this.Seed), out IConvertible seedValue) && int.TryParse(seedValue?.ToString(), out int seed))
                {
                    this.Seed = seed;
                }
            }
        }

        /// <summary>
        /// The content path template to use when uploading content
        /// to target storage resources. When not defined the default template will be used.
        /// </summary>
        public string ContentPathTemplate { get; set; }

        /// <summary>
        /// Defines an explicit time for which the application will wait before exiting. This is correlated with
        /// the exit/flush wait supplied by the user on the command line.
        /// </summary>
        public TimeSpan? ExitWait { get; set; }

        /// <summary>
        /// True if VC should exit/crash on first/any error(s) regardless of 
        /// their severity. Default = false.
        /// </summary>
        public bool? FailFast { get; set; }

        /// <summary>
        /// True if VC should log output to file.
        /// </summary>
        public bool? LogToFile { get; set; }

        /// <summary>
        /// A seed to use with profile actions to ensure consistency.
        /// </summary>
        public int? Seed { get; set; }
    }
}
