// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    /// <summary>
    /// Constants that define workload process models/strategies.
    /// </summary>
    public static class WorkloadProcessModel
    {
        /// <summary>
        /// A single process to run the workload.
        /// </summary>
        public const string SingleProcess = "SingleProcess";

        /// <summary>
        /// A single process per disk under test by the workload.
        /// </summary>
        public const string SingleProcessPerDisk = "SingleProcessPerDisk";
    }
}
