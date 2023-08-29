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
    public class MeasurementSeriesElement
    {
        /// <summary>
        /// Index of the measurement series element.
        /// </summary>
        [JsonProperty("index")]
        public int Index { get; set; }

        /// <summary>
        /// Value of the measurement series element, which can be a string, boolean, or number.
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }

        /// <summary>
        /// Timestamp associated with the measurement series element.
        /// </summary>
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Measurement Series Id associated with the measurement series element.
        /// </summary>
        [JsonProperty("measurementSeriesId")]
        public string MeasurementSeriesId { get; set; }

        /// <summary>
        /// Metadata associated with the measurement series element.
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, IConvertible> Metadata { get; set; }
    }
}