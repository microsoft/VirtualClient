// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// MeasurementSeriesElement class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/measurement_series_element.json
    /// </summary>
    public class MeasurementSeriesElement : TestStepArtifact
    {
        /// <summary>
        /// Index of the measurement series element.
        /// </summary>
        [JsonProperty("index", Required = Required.Always)]
        public int Index { get; set; }

        /// <summary>
        /// Value of the measurement series element, which can be a string, boolean, or number.
        /// </summary>
        [JsonProperty("value", Required = Required.Always)]
        public object Value { get; set; }

        /// <summary>
        /// Timestamp associated with the measurement series element.
        /// </summary>
        [JsonProperty("timestamp", Required = Required.Always)]
        public new string Timestamp { get; set; }

        /// <summary>
        /// Measurement Series Id associated with the measurement series element.
        /// </summary>
        [JsonProperty("measurementSeriesId", Required = Required.Always)]
        public string MeasurementSeriesId { get; set; }

        /// <summary>
        /// Metadata associated with the measurement series element.
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, IConvertible> Metadata { get; set; }
    }
}