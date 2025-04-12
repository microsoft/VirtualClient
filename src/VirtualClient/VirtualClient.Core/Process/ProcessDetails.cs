// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace VirtualClient
{
    /// <summary>
    /// Process details.
    /// </summary>
    public class ProcessDetails
    {
        /// <summary>
        /// Id of the process that ran.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Command line executed.
        /// </summary>
        public string CommandLine { get; set; }

        /// <summary>
        /// Exit code of the command executed.
        /// </summary>
        public int ExitCode { get; set; }

        /// <summary>
        /// Generated Results of the command.
        /// </summary>
        public IEnumerable<string> GeneratedResults { get; set; }

        /// <summary>
        /// Standard output of the command.
        /// </summary>
        public string StandardOutput { get; set; }

        /// <summary>
        /// Standard error of the command.
        /// </summary>
        public string StandardError { get; set; }

        /// <summary>
        /// Tool Name ran by command.
        /// </summary>
        public string ToolName { get; set; }

        /// <summary>
        /// Working Directory.
        /// </summary>
        public string WorkingDirectory { get; set; }
    }
}