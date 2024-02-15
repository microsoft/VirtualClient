// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Configuration
{
    /// <summary>
    /// Represents the 'FileLogSettings' section of the appsetting.json file
    /// for the application.
    /// </summary>
    public class FileLogSettings
    {
        /// <summary>
        /// True/false whether file logging is enabled.
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// The file name to use for the file that contains workload performance counter
        /// output.
        /// </summary>
        public string CountersFileName { get; set; }

        /// <summary>
        /// The file name to use for the file that contains system event monitoring
        /// output.
        /// </summary>
        public string EventsFileName { get; set; }

        /// <summary>
        /// The file name to use for the file that contains workload metrics/results
        /// output.
        /// </summary>
        public string MetricsFileName { get; set; }

        /// <summary>
        /// The file name to use for the csv file that contains workload metrics/results
        /// output.
        /// </summary>
        public string MetricsCsvFileName { get; set; }

        /// <summary>
        /// The file name to use for the file that contains general traces and
        /// errors.
        /// </summary>
        public string TracesFileName { get; set; }
    }
}
