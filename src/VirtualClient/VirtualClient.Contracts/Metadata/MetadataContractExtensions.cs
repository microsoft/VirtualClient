// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Metadata
{
    using System.Collections.Generic;
    using System.Linq;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extensions to the <see cref="MetadataContract"/> class.
    /// </summary>
    public static class MetadataContractExtensions
    {
        /// <summary>
        /// Adds the metadata to the scenario category at the component scope.
        /// </summary>
        /// <param name="metadataContract">The metadata contract instance to which to add the metadata.</param>
        /// <param name="toolName">The name of the tool used in the scenario.</param>
        /// <param name="toolArguments">The arguments passed to the tool used in the scenario.</param>
        /// <param name="toolVersion">The version of the tool used in the scenario.</param>
        /// <param name="packageName">The name of the package that contained the tool.</param>
        /// <param name="packageVersion">The version of the package that contained the too.</param>
        /// <param name="additionalMetadata">Additional/supplemental metadata to include.</param>
        public static void AddForScenario(this MetadataContract metadataContract, string toolName, string toolArguments, string toolVersion = null, string packageName = null, string packageVersion = null, IDictionary<string, object> additionalMetadata = null)
        {
            toolName.ThrowIfNullOrWhiteSpace(nameof(toolName));

            IDictionary<string, object> metadata = new Dictionary<string, object>
            {
                { "toolName", toolName },
                { "toolArguments", SensitiveData.ObscureSecrets(toolArguments) },
                { "toolVersion", toolVersion },
                { "packageName", packageName },
                { "packageVersion", packageVersion }
            };

            if (packageName != null)
            {
                metadata["packageName"] = packageName;
                metadata["packageVersion"] = packageVersion;
            }

            if (additionalMetadata?.Any() == true)
            {
                foreach (var entry in additionalMetadata)
                {
                    metadata[entry.Key] = entry.Value;
                }
            }

            metadataContract.Add(metadata, MetadataContractCategory.Scenario, true);
        }
    }
}
