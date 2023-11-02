// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// LogSeverity
    /// </summary>
    public enum LogSeverity
    {
        /// <summary>
        /// Info
        /// </summary>
        INFO,

        /// <summary>
        /// Debug
        /// </summary>
        DEBUG,

        /// <summary>
        /// Warning
        /// </summary>
        WARNING,

        /// <summary>
        /// Error
        /// </summary>
        ERROR,

        /// <summary>
        /// Fatal
        /// </summary>
        FATAL
    }

    /// <summary>
    /// Log class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/log.json
    /// </summary>
    public class Log : TestRunArtifact
    {
        /// <summary>
        /// Severity of the log message (e.g., INFO, DEBUG, WARNING, ERROR, FATAL).
        /// </summary>
        [JsonProperty("severity")]
        public string Severity { get; set; }

        /// <summary>
        /// Log message text.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Source location information for debugging or tracing program execution.
        /// </summary>
        [JsonProperty("sourceLocation")]
        public SourceLocation SourceLocation { get; set; }
    }
}