// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// Subcomponent class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/subcomponent.json
    /// </summary>
    public class Subcomponent
    {
        /// <summary>
        /// The type of the subcomponent.
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// The name of the subcomponent.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// The location of the subcomponent.
        /// </summary>
        [JsonProperty("location")]
        public string Location { get; set; }

        /// <summary>
        /// The version of the subcomponent.
        /// </summary>
        [JsonProperty("version")]
        public string Version { get; set; }

        /// <summary>
        /// The revision of the subcomponent.
        /// </summary>
        [JsonProperty("revision")]
        public string Revision { get; set; }
    }
}