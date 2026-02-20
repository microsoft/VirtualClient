// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;
    using YamlDotNet.Core;
    using YamlDotNet.Serialization;

    /// <summary>
    /// Provides descriptive information for a metric/measurement data point.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "This is a pure data contract object.")]
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "This is a pure data contract object.")]
    public class MetricDataPoint : TelemetryDataPoint
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MetricDataPoint"/> class.
        /// </summary>
        public MetricDataPoint()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetricDataPoint"/> class.
        /// </summary>
        public MetricDataPoint(TelemetryDataPoint dataPoint)
            : base(dataPoint)
        {
        }

        /// <summary>
        /// A category/categorization for the metric (e.g. Cryptographic Operations).
        /// </summary>
        [JsonProperty("metricCategorization", Required = Required.Default)]
        [YamlMember(Alias = "metricCategorization", ScalarStyle = ScalarStyle.Plain)]
        public string MetricCategorization;

        /// <summary>
        /// A description of the metric.
        /// </summary>
        [JsonProperty("metricDescription", Required = Required.Default)]
        [YamlMember(Alias = "metricDescription", ScalarStyle = ScalarStyle.Plain)]
        public string MetricDescription;

        /// <summary>
        /// The name of the metric (e.g. bandwidth_write).
        /// </summary>
        [JsonProperty("metricName", Required = Required.Default)]
        [YamlMember(Alias = "metricName", ScalarStyle = ScalarStyle.Plain)]
        public string MetricName;

        /// <summary>
        /// Describes whether higher or lower values indicate better outcomes
        /// for the metric.
        /// </summary>
        [JsonProperty("metricRelativity", Required = Required.Default)]
        [YamlMember(Alias = "metricRelativity", ScalarStyle = ScalarStyle.Plain)]
        public MetricRelativity? MetricRelativity;

        /// <summary>
        /// The unit of measurement for the metric (e.g. kilobytes/sec).
        /// </summary>
        [JsonProperty("metricUnit", Required = Required.Default)]
        [YamlMember(Alias = "metricUnit", ScalarStyle = ScalarStyle.Plain)]
        public string MetricUnit;

        /// <summary>
        /// The value for the metric.
        /// </summary>
        [JsonProperty("metricValue", Required = Required.Default)]
        [YamlMember(Alias = "metricValue", ScalarStyle = ScalarStyle.Plain)]
        public double? MetricValue;

        /// <summary>
        /// The priority/verbosity of the metric. Recommended Values:
        /// - 1 (Standard/Critical): Most important metrics
        /// - 2 (Detailed): Additional detailed metrics
        /// - 5 (Verbose): All diagnostic/internal metrics
        /// Verbosity level 3 is reserved for future expansion and should not be used.
        /// </summary>
        /// <remarks>
        /// Allows the user to ascribe different levels of priority/verbosity to a set of metrics that can 
        /// be used for queries/filtering. Lower values indicate higher priority. For example, metrics considered 
        /// to be the most critical for decision making would be set with verbosity = 1 (Critical).
        /// For backward compatibility, verbosity = 0 is mapped to verbosity = 1.
        /// </remarks>
        [JsonProperty("metricVerbosity", Required = Required.Default)]
        [YamlMember(Alias = "metricVerbosity", ScalarStyle = ScalarStyle.Plain)]
        public int MetricVerbosity;

        /// <summary>
        /// The name of the scenario in which the metric was produced. This might be for example
        /// a specific usage of the toolset (e.g. different command line arguments).
        /// </summary>
        [JsonProperty("scenarioName", Required = Required.Default)]
        [YamlMember(Alias = "scenarioName", ScalarStyle = ScalarStyle.Plain)]
        public string ScenarioName;

        /// <summary>
        /// The time at which the scenario execution ended/completed. This date/time should
        /// always be in UTC.
        /// </summary>
        [JsonProperty("scenarioEndTime", Required = Required.Default)]
        [YamlMember(Alias = "scenarioEndTime", ScalarStyle = ScalarStyle.Plain)]
        public DateTime? ScenarioEndTime;

        /// <summary>
        /// The time at which the scenario execution began/started. This date/time should
        /// always be in UTC.
        /// </summary>
        [JsonProperty("scenarioStartTime", Required = Required.Default)]
        [YamlMember(Alias = "scenarioStartTime", ScalarStyle = ScalarStyle.Plain)]
        public DateTime? ScenarioStartTime;

        /// <summary>
        /// The toolset that produced the metric. Multiple toolsets can be defined separated by
        /// a semi-colon.
        /// </summary>
        [JsonProperty("toolName", Required = Required.Default)]
        [YamlMember(Alias = "toolName", ScalarStyle = ScalarStyle.Plain)]
        public string ToolName;

        /// <summary>
        /// The raw output from the toolset that produced the metric (and potentially from which
        /// the metric was parsed).
        /// </summary>
        [JsonProperty("toolResults", Required = Required.Default)]
        [YamlMember(Alias = "toolResults", ScalarStyle = ScalarStyle.Plain)]
        public string ToolResults;

        /// <summary>
        /// The version of the toolset that produced the metric.
        /// </summary>
        [JsonProperty("toolVersion", Required = Required.Default)]
        [YamlMember(Alias = "toolVersion", ScalarStyle = ScalarStyle.Plain)]
        public string ToolVersion;

        /// <inheritdoc />
        protected override IList<string> GetValidationErrors()
        {
            IList<string> validationErrors = base.GetValidationErrors() ?? new List<string>();

            // Part C requirements
            if (string.IsNullOrWhiteSpace(this.MetricName))
            {
                validationErrors.Add("The metric name is required (metricName).");
            }

            if (this.MetricValue == null)
            {
                validationErrors.Add("The metric value is required (metricValue).");
            }

            if (this.MetricRelativity == null)
            {
                validationErrors.Add("The metric relativity is required (metricRelativity).");
            }

            if (string.IsNullOrWhiteSpace(this.ScenarioName))
            {
                validationErrors.Add("The scenario name is required (scenarioName).");
            }

            if (this.ScenarioEndTime == null)
            {
                validationErrors.Add("The scenario end time/timestamp is required (scenarioEndTime).");
            }

            if (this.ScenarioStartTime == null)
            {
                validationErrors.Add("The scenario start time/timestamp is required (scenarioStartTime).");
            }

            if (string.IsNullOrWhiteSpace(this.ToolName))
            {
                validationErrors.Add("The tool name is required (toolName).");
            }

            return validationErrors;
        }
    }
}
