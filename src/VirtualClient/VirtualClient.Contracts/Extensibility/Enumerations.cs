// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Extensibility
{
    /// <summary>
    /// The format of the telemetry/data (e.g. CSV, JSON, YAML).
    /// </summary>
    public enum DataFormat
    {
        /// <summary>
        /// Undefined
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// CSV format
        /// </summary>
        Csv,

        /// <summary>
        /// JSON format
        /// </summary>
        Json,

        /// <summary>
        /// YAML format
        /// </summary>
        Yaml
    }

    /// <summary>
    /// The telemetry/data schema (e.g. Events, Metrics).
    /// </summary>
    public enum DataSchema
    {
        /// <summary>
        /// Undefined
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Events schema
        /// </summary>
        Events,

        /// <summary>
        /// Metrics schema
        /// </summary>
        Metrics
    }
}
