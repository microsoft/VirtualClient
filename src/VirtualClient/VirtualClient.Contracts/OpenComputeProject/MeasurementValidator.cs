// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// Represents the type of a measurement validator: Equal.
    /// </summary>
    public enum MeasurementValidatorType
    {
        /// <summary>
        /// Represents the "Equal" type.
        /// </summary>
        EQUAL,

        /// <summary>
        /// Represents the "Not Equal" type.
        /// </summary>
        NOT_EQUAL,

        /// <summary>
        /// Represents the "Less Than" type.
        /// </summary>
        LESS_THAN,

        /// <summary>
        /// Represents the "Less Than or Equal" type.
        /// </summary>
        LESS_THAN_OR_EQUAL,

        /// <summary>
        /// Represents the "Greater Than" type.
        /// </summary>
        GREATER_THAN,

        /// <summary>
        /// Represents the "Greater Than or Equal" type.
        /// </summary>
        GREATER_THAN_OR_EQUAL,

        /// <summary>
        /// Represents the "Regex Match" type.
        /// </summary>
        REGEX_MATCH,

        /// <summary>
        /// Represents the "Regex No Match" type.
        /// </summary>
        REGEX_NO_MATCH,

        /// <summary>
        /// Represents the "In Set" type.
        /// </summary>
        IN_SET,

        /// <summary>
        /// Represents the "Not In Set" type.
        /// </summary>
        NOT_IN_SET
    }

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
        [JsonProperty("type", Required = Required.Always)]
        public MeasurementValidatorType Type { get; set; }

        /// <summary>
        /// Value associated with the measurement validator. It can be of type string, boolean, or number.
        /// </summary>
        [JsonProperty("value", Required = Required.Always)]
        public IConvertible Value { get; set; }

        /// <summary>
        /// Metadata associated with the measurement validator.
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, IConvertible> Metadata { get; set; }
    }
}