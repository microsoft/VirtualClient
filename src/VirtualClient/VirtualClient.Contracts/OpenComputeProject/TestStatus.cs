// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// TestStatus enum from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/test_status.json
    /// </summary>
    public enum TestStatus
    {
        /// <summary>
        /// Complete
        /// </summary>
        COMPLETE,

        /// <summary>
        /// Error
        /// </summary>
        ERROR,

        /// <summary>
        /// Skip
        /// </summary>
        SKIP
    }
}