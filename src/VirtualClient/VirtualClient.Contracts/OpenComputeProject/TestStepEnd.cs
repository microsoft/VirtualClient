// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// TestStepEnd class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/test_step_end.json
    /// </summary>
    public class TestStepEnd
    {
        /// <summary>
        /// Status of the test step end.
        /// </summary>
        [JsonProperty("status", Required = Required.Always)]
        public TestStatus Status { get; set; }
    }
}