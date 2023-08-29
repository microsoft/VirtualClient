// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// TestRunEnd class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/test_run_end.json
    /// </summary>
    public class TestRunEnd
    {
        /// <summary>
        /// Status of the test run end.
        /// </summary>
        [JsonProperty("status")]
        public TestStatus Status { get; set; }

        /// <summary>
        /// Result of the test run end.
        /// </summary>
        [JsonProperty("result")]
        public TestResult Result { get; set; }
    }

    /// <summary>
    /// Test Result
    /// </summary>
    public class TestResult
    {
        /// <summary>
        /// Test Result
        /// </summary>
        [JsonProperty("result")]
        public string Result { get; set; }
    }
}