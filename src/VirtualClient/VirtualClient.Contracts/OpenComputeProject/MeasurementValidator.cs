// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// MeasurementValidator class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/validator.json
    /// </summary>
    public class MeasurementValidator
    {
        /// <summary>
        /// Name of the measurement validator.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Type of the measurement validator.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Value associated with the measurement validator. It can be of type string, boolean, or number.
        /// </summary>
        [JsonProperty("value")]
        public object Value { get; set; }

        /// <summary>
        /// Metadata associated with the measurement validator.
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, IConvertible> Metadata { get; set; }
    }
}