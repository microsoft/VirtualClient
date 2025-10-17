// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Configuration
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents default settings for the application.
    /// </summary>
    public class DefaultSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultSettings"/> class.
        /// </summary>
        public DefaultSettings()
        {
        }

        /// <summary>
        /// The default directory to write log files.
        /// </summary>
        public string LogDirectory { get; set; }

        /// <summary>
        /// The default log-to-file instruction.
        /// </summary>
        public bool LogToFile { get; set; }

        /// <summary>
        /// The default loggers for the application.
        /// </summary>
        public IEnumerable<string> Loggers { get; set; }

        /// <summary>
        /// The default directory to install (and search for) packages.
        /// </summary>
        public string PackageDirectory { get; set; }

        /// <summary>
        /// The default directory for state files/state management.
        /// </summary>
        public string StateDirectory { get; set; }

        /// <summary>
        /// The default directory for temp files.
        /// </summary>
        public string TempDirectory { get; set; }

        /// <summary>
        /// Creates the defaults for the application.
        /// </summary>
        public static DefaultSettings Create()
        {
            return new DefaultSettings
            {
                LogToFile = false,
                LogDirectory = "./logs",
                PackageDirectory = "./packages",
                StateDirectory = "./state",
                TempDirectory = "./temp",
                Loggers = new string[] { "console" }
            };
        }
    }
}
