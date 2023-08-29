// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// SourceLocation class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/source_location.json
    /// </summary>
    public class SourceLocation
    {
        /// <summary>
        /// A part of the full path of the code that generates the output.
        /// </summary>
        [JsonProperty("file")]
        public string File { get; set; }

        /// <summary>
        /// The line number in the file that generates the output.
        /// </summary>
        [JsonProperty("line")]
        public int Line { get; set; }
    }
}