// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// TestStepArtifact class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/test_step_artifact.json
    /// </summary>
    public class TestStepArtifact
    {
        /// <summary>
        /// Test Step ID
        /// </summary>
        [JsonProperty("testStepId")]
        public string TestStepId { get; set; }

        /// <summary>
        /// Test Step Start
        /// </summary>
        [JsonProperty("testStepStart")]
        public TestStepStart TestStepStart { get; set; }

        /// <summary>
        /// Test Step End
        /// </summary>
        [JsonProperty("testStepEnd")]
        public TestStepEnd TestStepEnd { get; set; }

        /// <summary>
        /// Measurement
        /// </summary>
        [JsonProperty("measurement")]
        public Measurement Measurement { get; set; }

        /// <summary>
        /// Measurement Series Start
        /// </summary>
        [JsonProperty("measurementSeriesStart")]
        public MeasurementSeriesStart MeasurementSeriesStart { get; set; }

        /// <summary>
        /// Measurement Series End
        /// </summary>
        [JsonProperty("measurementSeriesEnd")]
        public MeasurementSeriesEnd MeasurementSeriesEnd { get; set; }

        /// <summary>
        /// Measurement Series Element
        /// </summary>
        [JsonProperty("measurementSeriesElement")]
        public MeasurementSeriesElement MeasurementSeriesElement { get; set; }

        /// <summary>
        /// Error
        /// </summary>
        [JsonProperty("error")]
        public Error Error { get; set; }

        /// <summary>
        /// Log
        /// </summary>
        [JsonProperty("log")]
        public Log Log { get; set; }

        /// <summary>
        /// Diagnosis
        /// </summary>
        [JsonProperty("diagnosis")]
        public Diagnosis Diagnosis { get; set; }

        /// <summary>
        /// File
        /// </summary>
        [JsonProperty("file")]
        public File File { get; set; }

        /// <summary>
        /// Extension
        /// </summary>
        [JsonProperty("extension")]
        public Extension Extension { get; set; }
    }

    /// <summary>
    /// Extension
    /// </summary>
    public class Extension
    {
        /// <summary>
        /// Name of the extension.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Content of the extension.
        /// </summary>
        [JsonProperty("content")]
        public object Content { get; set; }
    }
}