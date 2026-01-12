// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    /// <summary>
    /// Contract class for describing Linux Distribution information.
    /// </summary>
    public class LinuxDistributionInfo
    {
        /// <summary>
        /// Full name of the OS
        /// </summary>
        public string OperationSystemFullName { get; set; }

        /// <summary>
        /// Linux Distribution category.
        /// </summary>
        public LinuxDistribution LinuxDistribution { get; set; }
    }
}
