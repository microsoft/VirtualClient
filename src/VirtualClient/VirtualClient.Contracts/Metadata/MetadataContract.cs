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
        /// <summary>
        /// Default Metadata Category = metadata
        /// </summary>
        public const string DefaultCategory = "metadata";

        /// <summary>
        /// Dependencies Metadata Category = metadata_dependencies
        /// </summary>
        public const string DependenciesCategory = "metadata_dependencies";

        /// <summary>
        /// Host/System Metadata Category = metadata_host
        /// </summary>
        public const string HostCategory = "metadata_host";

        /// <summary>
        /// Runtime Metadata Category = metadata_runtime
        /// </summary>
        public const string RuntimeCategory = "metadata_runtime";

        /// <summary>
        /// Scenario-specific Metadata Category = metadata_scenario
        /// </summary>
        public const string ScenarioCategory = "metadata_scenario";

        /// <summary>
        /// Scenario-specific Extensions Metadata Category = metadata_scenario_ext
        /// </summary>
        public const string ScenarioExtensionsCategory = "metadata_scenario_ext";

        /// <summary>
        /// Application Host/System
        /// </summary>
        internal const string AppHost = "appHost";

        /// <summary>
        /// Application Name
        /// </summary>
        internal const string AppName = "appName";

        /// <summary>
        /// Application Version
        /// </summary>
        internal const string AppVersion = "appVersion";

        /// <summary>
        /// Application Platform Version
        /// </summary>
        internal const string AppPlatformVersion = "appPlatformVersion";

        /// <summary>
        /// Client/Agent ID
        /// </summary>
        internal const string ClientId = "clientId";

        /// <summary>
        /// Client/Agent Instance (unique identifier for each client running).
        /// </summary>
        internal const string ClientInstance = "clientInstance";

        /// <summary>
        /// Application execution arguments (e.g. command line).
        /// </summary>
        internal const string ExecutionArguments = "executionArguments";

        /// <summary>
        /// The profile that describes the overall execution workflow (e.g. PERF-CPU-OPENSSL (win-x64)).
        /// </summary>
        internal const string ExecutionProfile = "executionProfile";

        /// <summary>
        /// A description of the execution profile.
        /// </summary>
        internal const string ExecutionProfileDescription = "executionProfileDescription";

        /// <summary>
        /// The name of the profile that describes the overall execution workflow.
        /// </summary>
        internal const string ExecutionProfileName = "executionProfileName";

        /// <summary>
        /// The path to the profile that describes the overall execution workflow.
        /// </summary>
        internal const string ExecutionProfilePath = "executionProfilePath";

        /// <summary>
        /// The execution system launching the application.
        /// </summary>
        internal const string ExecutionSystem = "executionSystem";

        /// <summary>
        /// Experiment ID
        /// </summary>
        internal const string ExperimentId = "experimentId";

        /// <summary>
        /// A timestamp representing the point at which a set of data is actually
        /// ingested into a target data store.
        /// </summary>
        internal const string IngestionTimestamp = "ingestionTimestamp";

        /// <summary>
        /// The Linux distro information.
        /// </summary>
        internal const string LinuxDistribution = "linuxDistribution";

        /// <summary>
        /// The OS platform (Win32NT, Unix).
        /// </summary>
        internal const string OperatingSystemPlatform = "operatingSystemPlatform";

        /// <summary>
        /// Parameters supplied to the application or component.
        /// </summary>
        internal const string Parameters = "parameters";

        /// <summary>
        /// The OS platform and CPU architecture (e.g. linux-arm64, linux-x64, win-arm64, win-x64).
        /// </summary>
        internal const string PlatformArchitecture = "platformArchitecture";

        /// <summary>
        /// A timestamp
        /// </summary>
        internal const string Timestamp = "timestamp";

        /// <summary>
        /// Metadata properties available during the lifetime of a single VC execution.
        /// </summary>
        private static IDictionary<string, IDictionary<string, object>> persistedMetadata = new ConcurrentDictionary<string, IDictionary<string, object>>(StringComparer.OrdinalIgnoreCase);

        private readonly object lockObject = new object();

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
        /// <param name="persisted">Apply persisted/global metadata.</param>
        public void Apply(EventContext telemetryContext, bool persisted = true)
        {
            lock (this.lockObject)
            {
                if (persisted)
                {
                    MetadataContract.ApplyMetadata(telemetryContext, MetadataContract.persistedMetadata);
                }

                // Component-scope properties take precedence over global properties in the case that there
                // is a conflict. Any existing global properties will be overridden in the case of a conflict.
                MetadataContract.ApplyMetadata(telemetryContext, this.instanceMetadata);
            }
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
        /// Returns the set of metadata for the specific category and scope.
        /// </summary>
        /// <param name="category">The name of the metadata category (e.g. metadata_hw, metadata_host).</param>
        public IDictionary<string, object> Get(string category)
        {
            category.ThrowIfNullOrWhiteSpace(nameof(category));
            return MetadataContract.GetCategoryMetadata(category, this.instanceMetadata);
        }

        /// <summary>
        /// Resets the metadata contract instance and underlying properties.
        /// </summary>
        public void Reset()
        {
            this.instanceMetadata.Clear();
        }

        private static void ApplyMetadata(EventContext telemetryContext, IDictionary<string, IDictionary<string, object>> metadata, string category = null)
        {
            foreach (var entry in metadata)
            {
                if (!string.IsNullOrWhiteSpace(category) && !string.Equals(category, entry.Key, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

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
