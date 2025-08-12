// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts.Extensibility
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Newtonsoft.Json;
    using YamlDotNet.Core;
    using YamlDotNet.Serialization;

    /// <summary>
    /// Provides descriptive information for a telemetry data point.
    /// </summary>
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "This is a pure data contract object.")]
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "This is a pure data contract object.")]
    [SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "This is a pure data contract object.")]
    public class TelemetryDataPoint
    {
        /// <summary>
        /// A deserializer for YAML-formatted content;
        /// </summary>
        protected static readonly IDeserializer YamlDeserializer = new Deserializer();

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryDataPoint"/> class.
        /// </summary>
        public TelemetryDataPoint()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryDataPoint"/> class.
        /// </summary>
        public TelemetryDataPoint(TelemetryDataPoint dataPoint)
        {
            this.AppHost = dataPoint.AppHost;
            this.AppName = dataPoint.AppName;
            this.AppVersion = dataPoint.AppVersion;
            this.ClientId = dataPoint.ClientId;
            this.ExecutionProfile = dataPoint.ExecutionProfile;
            this.ExecutionSystem = dataPoint.ExecutionSystem;
            this.ExperimentId = dataPoint.ExperimentId;
            this.OperatingSystemPlatform = dataPoint.OperatingSystemPlatform;
            this.OperationId = dataPoint.OperationId;
            this.OperationParentId = dataPoint.OperationParentId;
            this.PlatformArchitecture = dataPoint.PlatformArchitecture;
            this.SeverityLevel = dataPoint.SeverityLevel;
            this.Tags = dataPoint.Tags;
            this.Timestamp = dataPoint.Timestamp;

            if (dataPoint.Metadata?.Any() == true)
            {
                this.Metadata = new SortedMetadataDictionary(dataPoint.Metadata);
            }

            if (dataPoint.HostMetadata?.Any() == true)
            {
                this.HostMetadata = new SortedMetadataDictionary(dataPoint.HostMetadata);
            }
        }

        /// <summary>
        /// The application host system name (e.g. machine name).
        /// </summary>
        [JsonProperty("appHost", Required = Required.Default)]
        [YamlMember(Alias = "appHost", ScalarStyle = ScalarStyle.Plain)]

        public string AppHost;

        /// <summary>
        /// The application name.
        /// </summary>
        [JsonProperty("appName", Required = Required.Default)]
        [YamlMember(Alias = "appName", ScalarStyle = ScalarStyle.Plain)]
        public string AppName;

        /// <summary>
        /// The application version.
        /// </summary>
        [JsonProperty("appVersion", Required = Required.Default)]
        [YamlMember(Alias = "appVersion", ScalarStyle = ScalarStyle.Plain)]
        public string AppVersion;

        /// <summary>
        /// The ID of the application instance/agent with which the data point 
        /// is associated.
        /// </summary>
        /// <remarks>
        /// This property allows for multiple instances of the application running on a
        /// single system to be distinguishable from one another.
        /// </remarks>
        [JsonProperty("clientId", Required = Required.Default)]
        [YamlMember(Alias = "clientId", ScalarStyle = ScalarStyle.Plain)]
        public string ClientId;

        /// <summary>
        /// A name describing the operations profile. This may be the name of a JSON profile file
        /// or may be any other logical name describing the overall purpose of the operations workflow.
        /// </summary>
        [JsonProperty("executionProfile", Required = Required.Default)]
        [YamlMember(Alias = "executionProfile", ScalarStyle = ScalarStyle.Plain)]
        public string ExecutionProfile;

        /// <summary>
        /// A name describing an execution system in which the application is running.
        /// </summary>
        [JsonProperty("executionSystem", Required = Required.Default)]
        [YamlMember(Alias = "executionSystem", ScalarStyle = ScalarStyle.Plain)]
        public string ExecutionSystem;

        /// <summary>
        /// The ID of the experiment with which the data point is associated.
        /// </summary>
        [JsonProperty("experimentId", Required = Required.Default)]
        [YamlMember(Alias = "experimentId", ScalarStyle = ScalarStyle.Plain)]
        public string ExperimentId;

        /// <summary>
        /// Metadata associated with the host system.
        /// </summary>
        [JsonProperty("metadata_host", Required = Required.Default)]
        [YamlMember(Alias = "metadata_host", ScalarStyle = ScalarStyle.Plain)]
        public SortedMetadataDictionary HostMetadata;

        /// <summary>
        /// Metadata associated with the scenario that produced the data point.
        /// </summary>
        [JsonProperty("metadata", Required = Required.Default)]
        [YamlMember(Alias = "metadata", ScalarStyle = ScalarStyle.Plain)]
        public SortedMetadataDictionary Metadata;

        /// <summary>
        /// The OS platform identifier (i.e. Win32NT, Unix).
        /// </summary>
        [JsonProperty("operatingSystemPlatform", Required = Required.Default)]
        [YamlMember(Alias = "operatingSystemPlatform", ScalarStyle = ScalarStyle.Plain)]
        public string OperatingSystemPlatform;

        /// <summary>
        /// An identifier that correlates the data point with other
        /// data points related to the same operation within the application.
        /// </summary>
        [JsonProperty("operationId", Required = Required.Default)]
        [YamlMember(Alias = "operationId", ScalarStyle = ScalarStyle.Plain)]
        public Guid OperationId;

        /// <summary>
        /// An identifier that correlates the data point with a parent operation
        /// within the application.
        /// </summary>
        [JsonProperty("operationParentId", Required = Required.Default)]
        [YamlMember(Alias = "operationParentId", ScalarStyle = ScalarStyle.Plain)]
        public Guid OperationParentId;

        /// <summary>
        /// The OS platform and CPU architecture identifier (i.e. linux-arm64, linux-x64, win-arm64, win-x64).
        /// </summary>
        [JsonProperty("platformArchitecture", Required = Required.Default)]
        [YamlMember(Alias = "platformArchitecture", ScalarStyle = ScalarStyle.Plain)]
        public string PlatformArchitecture;

        /// <summary>
        /// The severity/concern level represented by the data point. 
        /// Recommended Values = 0 (Trace), 1 (Debug), 2 (Information), 3 (Warning), 4 (Error), 5 (Critical).
        /// </summary>
        [JsonProperty("severityLevel", Required = Required.Default)]
        [YamlMember(Alias = "severityLevel", ScalarStyle = ScalarStyle.Plain)]
        public int? SeverityLevel;

        /// <summary>
        /// Tags to associate with the data point.
        /// </summary>
        [JsonProperty("tags", Required = Required.Default)]
        [YamlMember(Alias = "tags", ScalarStyle = ScalarStyle.Plain)]
        public string Tags;

        /// <summary>
        /// A timestamp for the data point.
        /// </summary>
        [JsonProperty("timestamp", Required = Required.Default)]
        [YamlMember(Alias = "timestamp", ScalarStyle = ScalarStyle.Plain)]
        public DateTime? Timestamp;

        /// <summary>
        /// Validates the schema-required properties and values.
        /// </summary>
        /// <remarks>
        /// Depending upon the scenario, different schemas are required. Additionally, the
        /// various serialization libraries and implementations do not consistently support
        /// validation in the most desirable ways. We implement the validation logic to ensure
        /// we can account for the requirements regardless of the serialization base.
        /// </remarks>
        public void Validate()
        {
            IList<string> validationErrors = this.GetValidationErrors();

            if (validationErrors.Any())
            {
                throw new SchemaException(
                    $"Invalid data point. The following validation errors exist:{Environment.NewLine}- " +
                    $"{string.Join($"{Environment.NewLine}- ", validationErrors)}");
            }
        }

        /// <summary>
        /// Returns a set of validation errors for incorrect information defined
        /// (or missing) in the data point instance.
        /// </summary>
        protected virtual IList<string> GetValidationErrors()
        {
            List<string> validationErrors = new List<string>();

            // Part A requirements
            if (string.IsNullOrWhiteSpace(this.AppHost))
            {
                validationErrors.Add("The application host is required (appHost).");
            }

            if (string.IsNullOrWhiteSpace(this.AppName))
            {
                validationErrors.Add("The application name is required (appName).");
            }

            if (string.IsNullOrWhiteSpace(this.AppVersion))
            {
                validationErrors.Add("The application version is required (appVersion).");
            }

            if (this.SeverityLevel == null)
            {
                validationErrors.Add("The severity level is required (severityLevel).");
            }

            if (this.Timestamp == null)
            {
                validationErrors.Add("A timestamp is required (timestamp).");
            }

            // Part B requirements
            if (string.IsNullOrWhiteSpace(this.ClientId))
            {
                validationErrors.Add("A client ID is required (clientId).");
            }

            if (string.IsNullOrWhiteSpace(this.ExecutionProfile))
            {
                validationErrors.Add("An execution profile is required (executionProfile).");
            }

            if (string.IsNullOrWhiteSpace(this.ExperimentId))
            {
                validationErrors.Add("An experiment ID is required (experimentId).");
            }

            if (string.IsNullOrWhiteSpace(this.OperatingSystemPlatform))
            {
                validationErrors.Add("The operating system platform is required (operatingSystemPlatform).");
            }

            return validationErrors;
        }
    }
}
