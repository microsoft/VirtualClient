// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VirtualClient.Contracts.OpenComputeProject
{
    /// <summary>
    /// TestRunStart class from OCP contract
    /// https://github.com/opencomputeproject/ocp-diag-core/blob/main/json_spec/output/test_run_start.json
    /// </summary>
    public class TestRunStart : TestRunArtifact
    {
        /// <summary>
        /// The name of the test run.
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// The version of the test run.
        /// </summary>
        [JsonProperty("version", Required = Required.Always)]
        public string Version { get; set; }

        /// <summary>
        /// The command line used to start the test run.
        /// </summary>
        [JsonProperty("commandLine", Required = Required.Always)]
        public string CommandLine { get; set; }

        /// <summary>
        /// Test run parameters (custom object structure to be defined).
        /// </summary>
        [JsonProperty("parameters", Required = Required.Always)]
        public Dictionary<string, IConvertible> Parameters { get; set; }

        /// <summary>
        /// Device Under Test (DUT) information.
        /// </summary>
        [JsonProperty("dutInfo", Required = Required.Always)]
        public DutInfo DutInfo { get; set; }

        /// <summary>
        /// Additional metadata associated with the test run.
        /// </summary>
        [JsonProperty("metadata")]
        public Dictionary<string, IConvertible> Metadata { get; set; }
    }
}