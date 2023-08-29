// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.OpenComputeProject
{
    using Newtonsoft.Json;

    /// <summary>
    /// Output Artifact class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/root.json
    /// </summary>
    public class OutputArtifact
    {
        /// <summary>
        /// Sequence Number
        /// </summary>
        [JsonProperty("sequenceNumber")]
        public int SequenceNumber { get; set; }

        /// <summary>
        /// Timestamp
        /// </summary>
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        /// <summary>
        /// Schema Version
        /// </summary>
        [JsonProperty("schemaVersion")]
        public SchemaVersion SchemaVersion { get; set; }

        /// <summary>
        /// Test Run Artifact
        /// </summary>
        [JsonProperty("testRunArtifact")]
        public TestRunArtifact TestRunArtifact { get; set; }

        /// <summary>
        /// Test Step Artifact
        /// </summary>
        [JsonProperty("testStepArtifact")]
        public TestStepArtifact TestStepArtifact { get; set; }
    }

    /// <summary>
    /// Schema Version
    /// </summary>
    public class SchemaVersion
    {
        /// <summary>
        /// Major
        /// </summary>
        [JsonProperty("major")]
        public int Major { get; set; }

        /// <summary>
        /// Minor
        /// </summary>
        [JsonProperty("minor")]
        public int Minor { get; set; }
    }
}