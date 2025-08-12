// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Extensibility
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides an enumerator for delimited metrics data content.
    /// </summary>
    [SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "Implemented sufficiently")]
    public class DelimitedMetricDataEnumerator : DelimitedDataEnumerator<MetricDataPoint>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DelimitedMetricDataEnumerator"/> class.
        /// </summary>
        /// <param name="content">Content containing delimited metrics data points.</param>
        /// <param name="format">The format of the metrics data points (e.g. CSV, JSON, YAML).</param>
        public DelimitedMetricDataEnumerator(string content, DataFormat format)
            : base(content, format)
        {
        }

        /// <inheritdoc />
        protected override MetricDataPoint ParseFields(string[] columns, string[] values)
        {
            MetricDataPoint dataPoint = new MetricDataPoint();
            this.ApplyTo(dataPoint, columns, values);

            for (int i = 0; i < values.Length; i++)
            {
                string fieldName = columns[i];
                string fieldValue = values[i];

                switch (fieldName.ToLowerInvariant())
                {
                    case "metriccategorization":
                        dataPoint.MetricCategorization = fieldValue;
                        break;

                    case "metricdescription":
                        dataPoint.MetricDescription = fieldValue;
                        break;

                    case "metricname":
                        dataPoint.MetricName = fieldValue;
                        break;

                    case "metricrelativity":
                        dataPoint.MetricRelativity = Enum.Parse<MetricRelativity>(fieldValue);
                        break;

                    case "metricunit":
                        dataPoint.MetricUnit = fieldValue;
                        break;

                    case "metricvalue":
                        dataPoint.MetricValue = double.Parse(fieldValue);
                        break;

                    case "metricverbosity":
                        dataPoint.MetricVerbosity = int.Parse(fieldValue);
                        break;

                    case "scenario":
                        dataPoint.Scenario = fieldValue;
                        break;

                    case "scenarioendtime":
                        dataPoint.ScenarioEndTime = DateTime.Parse(fieldValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                        break;

                    case "scenariostarttime":
                        dataPoint.ScenarioStartTime = DateTime.Parse(fieldValue, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                        break;

                    case "toolset":
                        dataPoint.Toolset = fieldValue;
                        break;

                    case "toolsetresults":
                        dataPoint.ToolsetResults = fieldValue;
                        break;

                    case "toolsetversion":
                        dataPoint.ToolsetVersion = fieldValue;
                        break;
                }
            }

            return dataPoint;
        }

        /// <inheritdoc />
        protected override MetricDataPoint ParseJson(string json)
        {
            json.ThrowIfNullOrWhiteSpace(nameof(json));
            return json.FromJson<MetricDataPoint>();
        }

        /// <inheritdoc />
        protected override MetricDataPoint ParseYaml(string yaml)
        {
            yaml.ThrowIfNullOrWhiteSpace(nameof(yaml));
            return YamlDeserializer.Deserialize<MetricDataPoint>(yaml);
        }
    }
}
