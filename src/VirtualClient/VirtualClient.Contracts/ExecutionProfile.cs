// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// A profile for one run of this tool
    /// </summary>
    public class ExecutionProfile : IEquatable<ExecutionProfile>
    {
        /// <summary>
        /// The JPath prefix to items located in the parameter dictionary of a <see cref="ExecutionProfile"/>
        /// </summary>
        public const string ParameterPrefix = "$.Parameters.";
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionProfile"/> class.
        /// </summary>
        [JsonConstructor]
        public ExecutionProfile(
            string description, 
            TimeSpan? minimumExecutionInterval,  
            IEnumerable<ExecutionProfileElement> actions,
            IEnumerable<ExecutionProfileElement> dependencies,
            IEnumerable<ExecutionProfileElement> monitors,
            IDictionary<string, IConvertible> metadata,
            IDictionary<string, IConvertible> parameters)
        {
            description.ThrowIfNullOrWhiteSpace(nameof(description));

            this.Description = description;
            this.MinimumExecutionInterval = minimumExecutionInterval;

            this.Actions = actions != null
                ? new List<ExecutionProfileElement>(actions)
                : new List<ExecutionProfileElement>();

            this.Dependencies = dependencies != null
                ? new List<ExecutionProfileElement>(dependencies)
                : new List<ExecutionProfileElement>();

            this.Monitors = monitors != null
                ? new List<ExecutionProfileElement>(monitors)
                : new List<ExecutionProfileElement>();

            this.Metadata = metadata != null
                ? new Dictionary<string, IConvertible>(metadata, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);

            this.Parameters = parameters != null
                ? new Dictionary<string, IConvertible>(parameters, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);

            if (this.Actions?.Any() == true)
            {
                this.Actions.ForEach(action => action.ComponentType = ComponentType.Action);
            }

            if (this.Dependencies?.Any() == true)
            {
                this.Dependencies.ForEach(dependency => dependency.ComponentType = ComponentType.Dependency);
            }

            if (this.Monitors?.Any() == true)
            {
                this.Monitors.ForEach(monitor => monitor.ComponentType = ComponentType.Monitor);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionProfile"/> class.
        /// </summary>
        /// <param name="other">The instance to create the new instance from.</param>
        public ExecutionProfile(ExecutionProfile other)
            : this(
                  other?.Description,  
                  other.MinimumExecutionInterval, 
                  other?.Actions, 
                  other?.Dependencies, 
                  other?.Monitors, 
                  other?.Metadata, 
                  other?.Parameters)
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionProfile"/> class.
        /// </summary>
        /// <param name="other">The instance to create the new instance from.</param>
        public ExecutionProfile(ExecutionProfileYamlShim other)
            : this(
                  other?.Description,
                  other.MinimumExecutionInterval,
                  other?.Actions?.Select(a => new ExecutionProfileElement(a)),
                  other?.Dependencies?.Select(d => new ExecutionProfileElement(d)),
                  other?.Monitors?.Select(m => new ExecutionProfileElement(m)),
                  other?.Metadata,
                  other?.Parameters)
        {
        }

        /// <summary>
        /// Workload profile description.
        /// </summary>
        [JsonProperty(PropertyName = "Description", Required = Required.Always, Order = 10)]
        public string Description { get; }

        /// <summary>
        /// The minimum amount of time between executing actions
        /// </summary>
        [JsonProperty(PropertyName = "MinimumExecutionInterval", Required = Required.Default, Order = 30)]
        public TimeSpan? MinimumExecutionInterval { get; }

        /// <summary>
        /// The set of supported platform/architectures for the profile.
        /// </summary>
        [JsonProperty(PropertyName = "SupportedPlatforms", Required = Required.Default, Order = 45)]
        public List<string> SupportedPlatforms { get; }

        /// <summary>
        /// The set of supported operating systems for the profile.
        /// </summary>
        [JsonProperty(PropertyName = "SupportedOperatingSystems", Required = Required.Default, Order = 50)]
        public List<string> SupportedOperatingSystems { get; }

        /// <summary>
        /// Metadata properties associated with the profile.
        /// </summary>
        [JsonProperty(PropertyName = "Metadata", Required = Required.Default, Order = 60)]
        [JsonConverter(typeof(ParameterDictionaryJsonConverter))]
        public IDictionary<string, IConvertible> Metadata { get; }

        /// <summary>
        /// Collection of parameters that are associated with the profile.
        /// </summary>
        [JsonProperty(PropertyName = "Parameters", Required = Required.Default, Order = 70)]
        [JsonConverter(typeof(ParameterDictionaryJsonConverter))]
        public IDictionary<string, IConvertible> Parameters { get; }

        /// <summary>
        /// Workload actions to run as part of the profile execution.
        /// </summary>
        [JsonProperty(PropertyName = "Actions", Required = Required.Default, Order = 80)]
        public List<ExecutionProfileElement> Actions { get; }

        /// <summary>
        /// Dependencies required by the profile actions or monitors.
        /// </summary>
        [JsonProperty(PropertyName = "Dependencies", Required = Required.Default, Order = 90)]
        public List<ExecutionProfileElement> Dependencies { get; }

        /// <summary>
        /// Monitors to run as part of the profile execution.
        /// </summary>
        [JsonProperty(PropertyName = "Monitors", Required = Required.Default, Order = 100)]
        public List<ExecutionProfileElement> Monitors { get; }

        /// <summary>
        /// The format of the profile file/source (e.g. JSON, YAML).
        /// </summary>
        [JsonIgnore]
        public string ProfileFormat { get; internal set; }

        /// <summary>
        /// Reads the profile from the file system.
        /// </summary>
        /// <param name="profilePath">The path to the profile JSON file.</param>
        /// <param name="fileSystem">Provides methods for interfacing with the file system.</param>
        /// <returns>An <see cref="ExecutionProfile"/> instance representing the deserialized version of the profile JSON.</returns>
        public static async Task<ExecutionProfile> ReadProfileAsync(string profilePath, IFileSystem fileSystem = null)
        {
            profilePath.ThrowIfNullOrWhiteSpace(nameof(profilePath));

            IFileSystem fileSystemProvider = fileSystem ?? new FileSystem();
            if (!fileSystemProvider.File.Exists(profilePath))
            {
                throw new FileNotFoundException($"A workload profile does not exist at the path provided '{profilePath}'.");
            }

            string profileContent = await fileSystemProvider.File.ReadAllTextAsync(profilePath);
            return JsonConvert.DeserializeObject<ExecutionProfile>(profileContent);
        }

        /// <summary>
        /// Determines equality between this instance and another instance.
        /// </summary>
        /// <param name="other">The other instance to determine equality against.</param>
        /// <returns>True/False if the two instances are equal.</returns>
        public bool Equals(ExecutionProfile other)
        {
            return other != null && this.GetHashCode() == other.GetHashCode();
        }

        /// <summary>
        /// Determines equality between this instance and another object.
        /// </summary>
        /// <param name="obj">The object to determine equality against.</param>
        /// <returns>True/False if the two objects are equal.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is ExecutionProfile))
            {
                return false;
            }

            return this.Equals(obj as ExecutionProfile);
        }

        /// <summary>
        /// Calculates the hash code of this instance.
        /// </summary>
        /// <returns>The hash code of this instance.</returns>
        public override int GetHashCode()
        {
            return this.ToString().ToUpperInvariant().GetHashCode();
        }

        /// <summary>
        /// Generates a unique string representation of this.
        /// </summary>
        /// <returns>A string representation of this.</returns>
        public override string ToString()
        {
            return new StringBuilder()
                .Append(this.Description)
                .Append(this.MinimumExecutionInterval)
                .AppendJoin(";", this.Parameters.Select(p => $"{p.Key};{p.Value}"))
                .AppendJoin(";", this.Metadata.Select(m => $"{m.Key};{m.Value}"))
                .AppendJoin(";", this.Actions)
                .AppendJoin(";", this.Dependencies)
                .AppendJoin(";", this.Monitors).ToString();
        }
    }
}