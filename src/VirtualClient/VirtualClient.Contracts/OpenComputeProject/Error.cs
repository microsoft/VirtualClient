// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.OpenComputeProject
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Error class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/error.json
    /// </summary>
    public class Error
    {
        /// <summary>
        /// Symptom
        /// </summary>
        [JsonProperty("symptom")]
        public string Symptom { get; set; }

        /// <summary>
        /// Message
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }

        /// <summary>
        /// Software Info Ids
        /// </summary>
        [JsonProperty("softwareInfoIds")]
        public List<string> SoftwareInfoIds { get; set; }

        /// <summary>
        /// Source Location
        /// </summary>
        [JsonProperty("sourceLocation")]
        public SourceLocation SourceLocation { get; set; }
    }
}