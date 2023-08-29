// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.OpenComputeProject
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// File class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/file.json
    /// </summary>
    public class File
    {
        /// <summary>
        /// Display Name
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// URI
        /// </summary>
        [JsonProperty("uri")]
        public string Uri { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [JsonProperty("description")]
        public string Description { get; set; }

        /// <summary>
        /// Content Type
        /// </summary>
        [JsonProperty("contentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// Is Snapshot
        /// </summary>
        [JsonProperty("isSnapshot")]
        public bool IsSnapshot { get; set; }

        /// <summary>
        /// Metadata
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, IConvertible> Metadata { get; set; }
    }
}