// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Metadata
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// Contains metadata properties and collections that are part of the telemetry
    /// data contract emitted by the Virtual Client.
    /// </summary>
    public class MetadataContract
    {
        internal const string CategoryDefault = "metadata";
        internal const string CategoryDependencies = "metadata_dependencies";
        internal const string CategoryHost = "metadata_host";
        internal const string CategoryRuntime = "metadata_runtime";
        internal const string CategoryScenario = "metadata_scenario";

        /// <summary>
        /// Metadata properties available during the lifetime of a single VC execution.
        /// </summary>
        private static IDictionary<string, IDictionary<string, object>> persistedMetadata = new ConcurrentDictionary<string, IDictionary<string, object>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Metadata properties for an instance of the metadata contract typically associated with
        /// an individual VC component lifetime.
        /// </summary>
        private IDictionary<string, IDictionary<string, object>> instanceMetadata = new ConcurrentDictionary<string, IDictionary<string, object>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns the set of persisted metadata for the specific category and scope.
        /// </summary>
        /// <param name="category">The name of the metadata category (e.g. metadata_hw, metadata_host).</param>
        public static IDictionary<string, object> GetPersisted(string category)
        {
            category.ThrowIfNullOrWhiteSpace(nameof(category));
            return MetadataContract.GetCategoryMetadata(category, MetadataContract.persistedMetadata);
        }

        /// <summary>
        /// Returns the set of persisted metadata for the specific category.
        /// </summary>
        /// <param name="category">The metadata category (e.g. metadata_host, metadata_runtime, metadata_scenario).</param>
        public static IDictionary<string, object> GetPersisted(MetadataContractCategory category)
        {
            string categoryName = MetadataContract.GetCategoryName(category);
            return MetadataContract.GetCategoryMetadata(categoryName, MetadataContract.persistedMetadata);
        }

        /// <summary>
        /// Persists the property (name/value) to the global metadata contract for the specific category
        /// (e.g. metadata_host, metadata_runtime, metadata_scenario).
        /// </summary>
        /// <param name="name">The name of the metadata property.</param>
        /// <param name="value">The value for the metadata property.</param>
        /// <param name="category">
        /// The category of metadata. Note that the category name must follow the standard convention.
        /// By convention the category name must start with the term 'metadata_' (e.g. metadata_ext).
        /// </param>
        /// <param name="replace">True to replace the property for the category of metadata. False to leave any existing value as-is.</param>
        public static void Persist(string name, object value, string category, bool replace = true)
        {
            name.ThrowIfNullOrWhiteSpace(nameof(name));
            category.ThrowIfNullOrWhiteSpace(nameof(category));

            MetadataContract.ThrowIfInvalid(category);
            IDictionary<string, object> existingMetadata = MetadataContract.GetCategoryMetadata(category, MetadataContract.persistedMetadata);

            if (existingMetadata == null)
            {
                existingMetadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                MetadataContract.persistedMetadata[category] = existingMetadata;
            }

            if (replace || !existingMetadata.ContainsKey(name))
            {
                existingMetadata[name] = value;
            }
        }

        /// <summary>
        /// Persists the property (name/value) to the global metadata contract for the specific category
        /// (e.g. metadata_host, metadata_runtime, metadata_scenario).
        /// </summary>
        /// <param name="name">The name of the metadata property.</param>
        /// <param name="value">The value for the metadata property.</param>
        /// <param name="category">The category of metadata.</param>
        /// <param name="replace">True to replace the property for the category of metadata. False to leave any existing value as-is.</param>
        public static void Persist(string name, object value, MetadataContractCategory category, bool replace = true)
        {
            name.ThrowIfNullOrWhiteSpace(nameof(name));

            string categoryName = MetadataContract.GetCategoryName(category);
            MetadataContract.Persist(name, value, categoryName, replace);
        }

        /// <summary>
        /// Persists the properties to the global metadata contract for the specific category
        /// (e.g. metadata_host, metadata_runtime, metadata_scenario).
        /// </summary>
        /// <param name="metadata">A set of metadata properties.</param>
        /// <param name="category">
        /// The category of metadata. Note that the category name must follow the standard convention.
        /// By convention the category name must start with the term 'metadata_' (e.g. metadata_ext).
        /// </param>
        /// <param name="replace">True to replace the property for the category of metadata. False to leave any existing value as-is.</param>
        public static void Persist(IDictionary<string, object> metadata, string category, bool replace = true)
        {
            metadata.ThrowIfNull(nameof(metadata));
            MetadataContract.ThrowIfInvalid(category);

            if (metadata.Any())
            {
                IDictionary<string, object> existingMetadata = MetadataContract.GetCategoryMetadata(category, MetadataContract.persistedMetadata);

                if (existingMetadata == null)
                {
                    existingMetadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    MetadataContract.persistedMetadata[category] = existingMetadata;
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
        /// Adds the properties to the "global" metadata contract for the specific category
        /// (e.g. metadata_hw, metadata_os, metadata_host).
        /// </summary>
        /// <param name="metadata">A set of metadata properties.</param>
        /// <param name="category">The category of metadata.</param>
        /// <param name="replace">True to replace the property for the category of metadata. False to leave any existing value as-is.</param>
        public static void Persist(IDictionary<string, object> metadata, MetadataContractCategory category, bool replace = true)
        {
            metadata.ThrowIfNull(nameof(metadata));

            if (metadata.Any())
            {
                string categoryName = MetadataContract.GetCategoryName(category);
                MetadataContract.Persist(metadata, categoryName, replace);
            }
        }

        /// <summary>
        /// Resets the underlying metadata sets for the scope specified.
        /// </summary>
        public static void ResetPersisted()
        {
            MetadataContract.persistedMetadata.Clear();
        }

        /// <summary>
        /// Applies the metadata contract to the telemetry event context adding all
        /// categories of metadata to the properties.
        /// </summary>
        /// <param name="telemetryContext">The telemetry event context to which the metadata contract should be applied.</param>
        public void Apply(EventContext telemetryContext)
        {
            MetadataContract.ApplyMetadata(telemetryContext, MetadataContract.persistedMetadata);

            // Component-scope properties take precedence over global properties in the case that there
            // is a conflict. Any existing global properties will be overridden in the case of a conflict.
            MetadataContract.ApplyMetadata(telemetryContext, this.instanceMetadata);
        }

        /// <summary>
        /// Adds the property (name/value) to the "global" metadata contract for the specific category
        /// (e.g. metadata_hw, metadata_os, metadata_host).
        /// </summary>
        /// <param name="name">The name of the metadata property.</param>
        /// <param name="value">The value for the metadata property.</param>
        /// <param name="category">
        /// The category of metadata. Note that the category name must follow the standard convention.
        /// By convention the category name must start with the term 'metadata_' (e.g. metadata_ext).
        /// </param>
        /// <param name="replace">True to replace the property for the category of metadata. False to leave any existing value as-is.</param>
        public void Add(string name, object value, string category, bool replace = true)
        {
            name.ThrowIfNullOrWhiteSpace(nameof(name));
            category.ThrowIfNullOrWhiteSpace(nameof(category));

            MetadataContract.ThrowIfInvalid(category);
            IDictionary<string, object> existingMetadata = this.Get(category);

            if (existingMetadata == null)
            {
                existingMetadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                this.instanceMetadata[category] = existingMetadata;
            }

            if (replace || !existingMetadata.ContainsKey(name))
            {
                existingMetadata[name] = value;
            }
        }

        /// <summary>
        /// Adds the property (name/value) to the "global" metadata contract for the specific category
        /// (e.g. metadata_hw, metadata_os, metadata_host).
        /// </summary>
        /// <param name="name">The name of the metadata property.</param>
        /// <param name="value">The value for the metadata property.</param>
        /// <param name="category">The category of metadata.</param>
        /// <param name="replace">True to replace the property for the category of metadata. False to leave any existing value as-is.</param>
        public void Add(string name, object value, MetadataContractCategory category, bool replace = true)
        {
            name.ThrowIfNullOrWhiteSpace(nameof(name));

            string categoryName = MetadataContract.GetCategoryName(category);
            this.Add(name, value, categoryName, replace);
        }

        /// <summary>
        /// Adds the properties to the "global" metadata contract for the specific category
        /// (e.g. metadata_hw, metadata_os, metadata_host).
        /// </summary>
        /// <param name="metadata">A set of metadata properties.</param>
        /// <param name="category">
        /// The category of metadata. Note that the category name must follow the standard convention.
        /// By convention the category name must start with the term 'metadata_' (e.g. metadata_ext).
        /// </param>
        /// <param name="replace">True to replace the property for the category of metadata. False to leave any existing value as-is.</param>
        public void Add(IDictionary<string, object> metadata, string category, bool replace = true)
        {
            metadata.ThrowIfNull(nameof(metadata));
            MetadataContract.ThrowIfInvalid(category);

            if (metadata.Any())
            {
                IDictionary<string, object> existingMetadata = MetadataContract.GetCategoryMetadata(category, this.instanceMetadata);

                if (existingMetadata == null)
                {
                    existingMetadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                    this.instanceMetadata[category] = existingMetadata;
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
        /// Adds the properties to the "global" metadata contract for the specific category
        /// (e.g. metadata_hw, metadata_os, metadata_host).
        /// </summary>
        /// <param name="metadata">A set of metadata properties.</param>
        /// <param name="category">The category of metadata.</param>
        /// <param name="replace">True to replace the property for the category of metadata. False to leave any existing value as-is.</param>
        public void Add(IDictionary<string, object> metadata, MetadataContractCategory category, bool replace = true)
        {
            metadata.ThrowIfNull(nameof(metadata));

            if (metadata.Any())
            {
                string categoryName = MetadataContract.GetCategoryName(category);
                this.Add(metadata, categoryName, replace);
            }
        }

        /// <summary>
        /// Returns the set of metadata for the specific category and scope.
        /// </summary>
        /// <param name="category">The name of the metadata category (e.g. metadata_hw, metadata_host).</param>
        public IDictionary<string, object> Get(string category)
        {
            category.ThrowIfNullOrWhiteSpace(nameof(category));
            return MetadataContract.GetCategoryMetadata(category, this.instanceMetadata);
        }

        /// <summary>
        /// Returns the set of metadata for the specific category.
        /// </summary>
        /// <param name="category">The name of the metadata category (e.g. metadata_hw, metadata_host).</param>
        public IDictionary<string, object> Get(MetadataContractCategory category)
        {
            string categoryName = MetadataContract.GetCategoryName(category);
            return MetadataContract.GetCategoryMetadata(categoryName, this.instanceMetadata);
        }

        /// <summary>
        /// Resets the metadata contract instance and underlying properties.
        /// </summary>
        public void Reset()
        {
            this.instanceMetadata.Clear();
        }

        private static void ApplyMetadata(EventContext telemetryContext, IDictionary<string, IDictionary<string, object>> metadata)
        {
            foreach (var entry in metadata)
            {
                IDictionary<string, object> metadataSet = null;
                if (!telemetryContext.Properties.TryGetValue(entry.Key, out object properties))
                {
                    telemetryContext.Properties[entry.Key] = new Dictionary<string, object>(entry.Value as IDictionary<string, object>, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    metadataSet = properties as IDictionary<string, object>;

                    if (metadataSet == null)
                    {
                        throw new SchemaException($"Invalid metadata category data type for category '{entry.Key}'. The object is not a valid dictionary type.");
                    }

                    metadataSet.AddRange(entry.Value, withReplace: true);
                }
            }
        }

        private static string GetCategoryName(MetadataContractCategory category)
        {
            string categoryName = null;
            switch (category)
            {
                case MetadataContractCategory.Default:
                    categoryName = MetadataContract.CategoryDefault;
                    break;

                case MetadataContractCategory.Dependencies:
                    categoryName = MetadataContract.CategoryDependencies;
                    break;

                case MetadataContractCategory.Host:
                    categoryName = MetadataContract.CategoryHost;
                    break;

                case MetadataContractCategory.Scenario:
                    categoryName = MetadataContract.CategoryScenario;
                    break;

                case MetadataContractCategory.Runtime:
                    categoryName = MetadataContract.CategoryRuntime;
                    break;

                default:
                    throw new NotSupportedException($"Metadata category '{category}' is not supported.");
            }

            return categoryName;
        }

        private static IDictionary<string, object> GetCategoryMetadata(string category, IDictionary<string, IDictionary<string, object>> propertySet)
        {
            propertySet.TryGetValue(category, out IDictionary<string, object> metadata);
            return metadata;
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
