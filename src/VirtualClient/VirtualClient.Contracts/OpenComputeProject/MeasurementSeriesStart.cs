// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// MeasurementSeriesStart class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/measurement_series_start.json
    /// </summary>
    public class MeasurementSeriesStart
    {
        /// <summary>
        /// Name of the measurement series.
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Unit of measurement for the series.
        /// </summary>
        [JsonProperty("unit")]
        public string Unit { get; set; }

        /// <summary>
        /// Measurement Series Id associated with the measurement series start.
        /// </summary>
        [JsonProperty("measurementSeriesId", Required = Required.Always)]
        public string MeasurementSeriesId { get; set; }

        /// <summary>
        /// Validators associated with the measurement series.
        /// </summary>
        [JsonProperty("validators")]
        public List<MeasurementValidator> Validators { get; set; }

        /// <summary>
        /// Hardware Info Id associated with the measurement series.
        /// </summary>
        [JsonProperty("hardwareInfoId")]
        public string HardwareInfoId { get; set; }

        /// <summary>
        /// Subcomponent information related to the measurement series.
        /// </summary>
        [JsonProperty("subcomponent")]
        public Subcomponent Subcomponent { get; set; }

        /// <summary>
        /// Metadata associated with the measurement series.
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, IConvertible> Metadata { get; set; }
    }
}