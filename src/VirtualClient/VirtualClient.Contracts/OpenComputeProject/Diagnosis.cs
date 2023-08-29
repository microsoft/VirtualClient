// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.OpenComputeProject
{
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Dut Info class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/diagnosis.json
    /// </summary>
    public class Diagnosis
    {
        /// <summary>
        /// Verdict
        /// </summary>
        [JsonProperty("verdict")]
        public string Verdict { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Hardware Info Id
        /// </summary>
        [JsonProperty("hardwareInfoId")]
        public string HardwareInfoId { get; set; }

        /// <summary>
        /// Subcomponent
        /// </summary>
        [JsonProperty("subcomponent")]
        public Subcomponent Subcomponent { get; set; }

        /// <summary>
        /// Source Location
        /// </summary>
        [JsonProperty("sourceLocation")]
        public SourceLocation SourceLocation { get; set; }
    }
}