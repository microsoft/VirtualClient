// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// TestRunArtifact class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/test_run_artifact.json
    /// </summary>
    public class TestRunArtifact : OutputArtifact
    {
        /// <summary>
        /// Test Run Start
        /// </summary>
        [JsonProperty("testRunStart")]
        public TestRunStart TestRunStart { get; set; }

        /// <summary>
        /// Test Run End
        /// </summary>
        [JsonProperty("testRunEnd")]
        public TestRunEnd TestRunEnd { get; set; }

        /// <summary>
        /// Log
        /// </summary>
        [JsonProperty("log")]
        public Log Log { get; set; }

        /// <summary>
        /// Error
        /// </summary>
        [JsonProperty("error")]
        public Error Error { get; set; }
    }
}