// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
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
        public string GeneratedResults { get; set; }

        /// <summary>
        /// Standard output of the command.
        /// </summary>
        public string StandardOutput { get; set; }

        /// <summary>
        /// Standard error of the command.
        /// </summary>
        public string StandardError { get; set; }

        /// <summary>
        /// Tool set ran by command.
        /// </summary>
        public string ToolSet { get; set; }

        /// <summary>
        /// Working Directory.
        /// </summary>
        public string WorkingDirectory { get; set; }
    }
}