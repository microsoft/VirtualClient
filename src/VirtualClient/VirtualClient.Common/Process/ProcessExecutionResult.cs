// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents the result of the execution of a process
    /// </summary>
    public class ProcessExecutionResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProcessExecutionResult"/> class
        /// </summary>
        public ProcessExecutionResult()
        {
            this.Output = new List<string>();
            this.Error = new List<string>();
            this.ExitCode = null;
            this.TimedOut = false;
        }

        /// <summary>
        /// Gets the output of the process sent to standard output.
        /// </summary>
        public IList<string> Output { get; }

        /// <summary>
        /// Gets the output of the process sent to standard error.
        /// </summary>
        public IList<string> Error { get; }

        /// <summary>
        /// Gets or sets the exit code of the process. Only valid if <see cref="TimedOut"/><c>== false</c>.
        /// </summary>
        public int? ExitCode { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the process timed out or not.
        /// </summary>
        public bool TimedOut { get; set; }
    }
}
