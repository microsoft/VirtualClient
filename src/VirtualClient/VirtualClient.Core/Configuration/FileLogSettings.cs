// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Configuration
{
    /// <summary>
    /// Represents log file setting for the application.
    /// </summary>
    public class FileLogSettings
    {
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
        /// True/false whether the settings are enabled by default.
        /// </summary>
        public bool IsEnabled { get; set; }

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

        /// <summary>
        /// The default settings for Virtual Client log files.
        /// </summary>
        public static FileLogSettings Default()
        {
            return new FileLogSettings
            {
                IsEnabled = true,
                CountersFileName = "vc.counters",
                EventsFileName = "vc.events",
                MetricsCsvFileName = "metrics.csv",
                MetricsFileName = "vc.metrics",
                TracesFileName = "vc.traces"
            };
        }
    }
}
