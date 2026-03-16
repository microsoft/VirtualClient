// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.CommandLine;

    public static partial class OptionFactory
    {
        /// <summary>
        /// Container image for workload execution.
        /// When provided, VC runs workloads inside this container.
        /// </summary>
        public static Option CreateImageOption(bool required = false)
        {
            return new Option<string>(
                aliases: new[] { "--image", "-i" },
                description: "Docker image for containerized execution. When provided, workloads run inside the container.")
            {
                IsRequired = required
            };
        }

        /// <summary>
        /// Image pull policy.
        /// </summary>
        public static Option CreatePullPolicyOption(bool required = false)
        {
            return new Option<string>(
                aliases: new[] { "--pull-policy" },
                getDefaultValue: () => "IfNotPresent",
                description: "Image pull policy: Always, IfNotPresent, Never. Default: IfNotPresent");
        }
    }
}