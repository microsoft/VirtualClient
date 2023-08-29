// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// Measurement class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/measurement.json
    /// </summary>
    public class Measurement
    {
        /// <summary>
        /// Name of the measurement.
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Value of the measurement, which can be a string, boolean, or number.
        /// </summary>
        [JsonProperty("value", Required = Required.Always)]
        public IConvertible Value { get; set; }

        /// <summary>
        /// Unit of the measurement.
        /// </summary>
        [JsonProperty("unit")]
        public string Unit { get; set; }

        /// <summary>
        /// Validators associated with the measurement.
        /// </summary>
        [JsonProperty("validators")]
        public List<MeasurementValidator> Validators { get; set; }

        /// <summary>
        /// Hardware Info Id associated with the measurement.
        /// </summary>
        [JsonProperty("hardwareInfoId")]
        public string HardwareInfoId { get; set; }

        /// <summary>
        /// Subcomponent information related to the measurement.
        /// </summary>
        [JsonProperty("subcomponent")]
        public Subcomponent Subcomponent { get; set; }

        /// <summary>
        /// Metadata associated with the measurement.
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, IConvertible> Metadata { get; set; }
    }
}