// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// Test Result
    /// </summary>
    public enum TestResult
    {
        /// <summary>
        /// Not applicable
        /// </summary>
        NOT_APPLICABLE,

        /// <summary>
        /// Pass
        /// </summary>
        PASS,

        /// <summary>
        /// Fail
        /// </summary>
        FAIL
    }

    /// <summary>
    /// TestRunEnd class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/test_run_end.json
    /// </summary>
    public class TestRunEnd : TestRunArtifact
    {
        /// <summary>
        /// Status of the test run end.
        /// </summary>
        [JsonProperty("status", Required = Required.Always)]
        public TestStatus Status { get; set; }

        /// <summary>
        /// Result of the test run end.
        /// </summary>
        [JsonProperty("result", Required = Required.Always)]
        public TestResult Result { get; set; }
    }
}