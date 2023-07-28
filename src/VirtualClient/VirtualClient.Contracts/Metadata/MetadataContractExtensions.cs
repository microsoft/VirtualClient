// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// Contains metadata properties and collections that are part of the telemetry
    /// data contract emitted by the Virtual Client.
    /// </summary>
    public static class MetadataContractExtensions
    {
        internal const string CategoryDefault = "metadata";
        internal const string CategoryHardware = "metadata_hw";
        internal const string CategoryHost = "metadata_host";
        internal const string CategoryRuntime = "metadata_runtime";
        internal const string CategoryScenario = "metadata_scenario";

        /// <summary>
        /// Extension adds the metadata property (name/value) to the specific category of metadata
        /// contract properties (e.g. metadata_hw, metadata_os, metadata_host).
        /// </summary>
        /// <param name="telemetryContext">The telemetry context information to which to add the property.</param>
        /// <param name="name">The name of the metadata property.</param>
        /// <param name="value">The value for the metadata property.</param>
        /// <param name="category">
        /// The category of metadata. Note that the category name must follow the standard convention.
        /// By convention the category name must start with the term 'metadata_' (e.g. metadata_ext).
        /// </param>
        /// <param name="replace">True to replace the property for the category of metadata. False to leave any existing value as-is.</param>
        public static void AddMetadata(this EventContext telemetryContext, string name, object value, string category, bool replace = true)
        {
            telemetryContext.ThrowIfNull(nameof(telemetryContext));
            name.ThrowIfNullOrWhiteSpace(nameof(name));
            category.ThrowIfNullOrWhiteSpace(nameof(category));

            MetadataContractExtensions.ThrowIfInvalid(category);
            IDictionary<string, object> existingMetadata = telemetryContext.GetMetadata(category);

            if (existingMetadata == null)
            {
                existingMetadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                telemetryContext.Properties[category] = existingMetadata;
            }

            if (replace || !existingMetadata.ContainsKey(name))
            {
                existingMetadata[name] = value;
            }
        }

        /// <summary>
        /// Extension adds the metadata property (name/value) to the specific category of metadata
        /// contract properties (e.g. metadata_hw, metadata_os, metadata_host).
        /// </summary>
        /// <param name="telemetryContext">The telemetry context information to which to add the property.</param>
        /// <param name="name">The name of the metadata property.</param>
        /// <param name="value">The value for the metadata property.</param>
        /// <param name="category">The category of metadata.</param>
        /// <param name="replace">True to replace the property for the category of metadata. False to leave any existing value as-is.</param>
        public static void AddMetadata(this EventContext telemetryContext, string name, object value, MetadataContractCategory category, bool replace = true)
        {
            telemetryContext.ThrowIfNull(nameof(telemetryContext));
            name.ThrowIfNullOrWhiteSpace(nameof(name));

            string categoryName = MetadataContractExtensions.GetCategoryName(category);
            MetadataContractExtensions.AddMetadata(telemetryContext, name, value, categoryName, replace);
        }

        /// <summary>
        /// Extension adds the metadata property (name/value) to the specific category of metadata
        /// contract properties (e.g. metadata_hw, metadata_os, metadata_host).
        /// </summary>
        /// <param name="telemetryContext">The telemetry context information to which to add the property.</param>
        /// <param name="metadata">A set of metadata properties.</param>
        /// <param name="category">
        /// The category of metadata. Note that the category name must follow the standard convention.
        /// By convention the category name must start with the term 'metadata_' (e.g. metadata_ext).
        /// </param>
        /// <param name="replace">True to replace the property for the category of metadata. False to leave any existing value as-is.</param>
        public static void AddMetadata(this EventContext telemetryContext, IDictionary<string, object> metadata, string category, bool replace = true)
        {
            telemetryContext.ThrowIfNull(nameof(telemetryContext));
            metadata.ThrowIfNull(nameof(metadata));

            MetadataContractExtensions.ThrowIfInvalid(category);

            if (metadata.Any())
            {
                IDictionary<string, object> existingMetadata = telemetryContext.GetMetadata(category);

                if (existingMetadata == null)
                {
                    existingMetadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    telemetryContext.Properties[category] = existingMetadata;
                }

                foreach (var entry in metadata)
                {
                    if (replace || !existingMetadata.ContainsKey(entry.Key))
                    {
                        existingMetadata[entry.Key] = entry.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Extension adds the metadata property (name/value) to the specific category of metadata
        /// contract properties (e.g. metadata_hw, metadata_os, metadata_host).
        /// </summary>
        /// <param name="telemetryContext">The telemetry context information to which to add the property.</param>
        /// <param name="metadata">A set of metadata properties.</param>
        /// <param name="category">The category of metadata.</param>
        /// <param name="replace">True to replace the property for the category of metadata. False to leave any existing value as-is.</param>
        public static void AddMetadata(this EventContext telemetryContext, IDictionary<string, object> metadata, MetadataContractCategory category, bool replace = true)
        {
            telemetryContext.ThrowIfNull(nameof(telemetryContext));
            metadata.ThrowIfNull(nameof(metadata));

            if (metadata.Any())
            {
                string categoryName = MetadataContractExtensions.GetCategoryName(category);
                MetadataContractExtensions.AddMetadata(telemetryContext, metadata, categoryName, replace);
            }
        }

        /// <summary>
        /// Extension adds scenario metadata properties from the dependency package to the related category.
        /// </summary>
        /// <param name="telemetryContext">The telemetry context information to which to add the property.</param>
        /// <param name="toolName">The name of the tool used in the scenario.</param>
        /// <param name="toolArguments">The arguments passed to the tool used in the scenario.</param>
        /// <param name="toolVersion">The version of the tool used in the scenario.</param>
        /// <param name="packageName">The name of the package that contained the tool.</param>
        /// <param name="packageVersion">The version of the package that contained the too.</param>
        /// <param name="additionalMetadata">Additional/supplemental metadata to include.</param>
        public static void AddScenarioMetadata(this EventContext telemetryContext, string toolName, string toolArguments, string toolVersion = null, string packageName = null, string packageVersion = null, IDictionary<string, object> additionalMetadata = null)
        {
            telemetryContext.ThrowIfNull(nameof(telemetryContext));
            toolName.ThrowIfNullOrWhiteSpace(nameof(toolName));

            IDictionary<string, object> metadata = new Dictionary<string, object>
            {
                { "toolName", toolName },
                { "toolArguments", toolArguments },
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

            telemetryContext.AddMetadata(metadata, MetadataContractCategory.Scenario, true);
        }

        /// <summary>
        /// Extension returns true/false if the category of metadata exist in the contract properties.
        /// </summary>
        /// <param name="telemetryContext">The telemetry context information to which to add the property.</param>
        /// <param name="category">The name of the metadata category (e.g. metadata_hw, metadata_host).</param>
        /// <returns></returns>
        public static IDictionary<string, object> GetMetadata(this EventContext telemetryContext, string category)
        {
            IDictionary<string, object> metadata = null;
            object existingMetadata;
            if (telemetryContext.Properties.TryGetValue(category, out existingMetadata))
            {
                metadata = existingMetadata as IDictionary<string, object>;
                if (metadata == null)
                {
                    throw new SchemaException($"Invalid metadata category data type for category '{category}'. The object is not a valid dictionary type.");
                }
            }

            return metadata;
        }

        /// <summary>
        /// Extension returns true/false if the category of metadata exist in the contract properties.
        /// </summary>
        /// <param name="telemetryContext">The telemetry context information to which to add the property.</param>
        /// <param name="category">The name of the metadata category (e.g. metadata_hw, metadata_host).</param>
        /// <returns></returns>
        public static IDictionary<string, object> GetMetadata(this EventContext telemetryContext, MetadataContractCategory category)
        {
            string categoryName = MetadataContractExtensions.GetCategoryName(category);
            return telemetryContext.GetMetadata(categoryName);
        }

        private static string GetCategoryName(MetadataContractCategory category)
        {
            string categoryName = null;
            switch (category)
            {
                case MetadataContractCategory.Default:
                    categoryName = MetadataContractExtensions.CategoryDefault;
                    break;

                case MetadataContractCategory.Hardware:
                    categoryName = MetadataContractExtensions.CategoryHardware;
                    break;

                case MetadataContractCategory.Host:
                    categoryName = MetadataContractExtensions.CategoryHost;
                    break;

                case MetadataContractCategory.Scenario:
                    categoryName = MetadataContractExtensions.CategoryScenario;
                    break;

                case MetadataContractCategory.Runtime:
                    categoryName = MetadataContractExtensions.CategoryRuntime;
                    break;

                default:
                    throw new NotSupportedException($"Metadata category '{category}' is not supported.");
            }

            return categoryName;
        }

        private static void ThrowIfInvalid(string category)
        {
            if (!category.StartsWith("metadata", StringComparison.OrdinalIgnoreCase))
            {
                throw new SchemaException(
                    $"Invalid metadata contract category name '{category}'. All metadata contract category names must being with " +
                    $"the prefix 'metadata' (e.g. metadata, metadata_hw, metdata_os).");
            }
        }
    }
}
