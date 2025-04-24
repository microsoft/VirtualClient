// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using VirtualClient.Common.Extensions;
    using YamlDotNet.Core;
    using YamlDotNet.Serialization;

    /// <summary>
    /// A shim used to convert between JSON and YAML formats for execution profiles.
    /// </summary>
    public class ExecutionProfileYamlShim
    {
        /// <summary>
        /// The JPath prefix to items located in the parameter dictionary of a <see cref="ExecutionProfile"/>
        /// </summary>
        public const string ParameterPrefix = "$.parameters.";

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionProfileYamlShim"/> class.
        /// </summary>
        public ExecutionProfileYamlShim()
        {
            this.Actions = new List<ExecutionProfileElementYamlShim>();
            this.Dependencies = new List<ExecutionProfileElementYamlShim>();
            this.Monitors = new List<ExecutionProfileElementYamlShim>();
            this.Metadata = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionProfileYamlShim"/> class.
        /// </summary>
        public ExecutionProfileYamlShim(ExecutionProfile other)
            : this()
        {
            other.ThrowIfNull(nameof(other));

            this.Description = other.Description;
            this.MinimumExecutionInterval = other.MinimumExecutionInterval;

            if (other?.Actions?.Any() == true)
            {
                this.Actions.AddRange(other.Actions.Select(a => new ExecutionProfileElementYamlShim(a)));
            }

            if (other?.Dependencies?.Any() == true)
            {
                this.Dependencies.AddRange(other.Dependencies.Select(d => new ExecutionProfileElementYamlShim(d)));
            }

            if (other?.Monitors?.Any() == true)
            {
                this.Monitors.AddRange(other.Monitors.Select(m => new ExecutionProfileElementYamlShim(m)));
            }

            if (other?.Metadata?.Any() == true)
            {
                this.Metadata.AddRange(other.Metadata);
            }

            if (other?.Parameters?.Any() == true)
            {
                this.Parameters.AddRange(other.Parameters);
            }

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
        /// Workload profile description.
        /// </summary>
        [YamlMember(Alias = "description", Order = 0, ScalarStyle = ScalarStyle.Plain)]
        public string Description { get; set; }

        /// <summary>
        /// The minimum amount of time between executing actions
        /// </summary>
        [YamlMember(Alias = "minimum_execution_interval", Order = 5, ScalarStyle = ScalarStyle.Plain)]
        public TimeSpan? MinimumExecutionInterval { get; set; }

        /// <summary>
        /// Metadata properties associated with the profile.
        /// </summary>
        [YamlMember(Alias = "metadata", Order = 10, ScalarStyle = ScalarStyle.Plain)]
        public IDictionary<string, IConvertible> Metadata { get; set; }

        /// <summary>
        /// Collection of parameters that are associated with the profile.
        /// </summary>
        [YamlMember(Alias = "parameters", Order = 20, ScalarStyle = ScalarStyle.Plain)]
        public IDictionary<string, IConvertible> Parameters { get; set; }

        /// <summary>
        /// Workload actions to run as part of the profile execution.
        /// </summary>
        [YamlMember(Alias = "actions", Order = 30, ScalarStyle = ScalarStyle.Plain)]
        public List<ExecutionProfileElementYamlShim> Actions { get; set; }

        /// <summary>
        /// Monitors to run as part of the profile execution.
        /// </summary>
        [YamlMember(Alias = "monitors", Order = 40, ScalarStyle = ScalarStyle.Plain)]
        public List<ExecutionProfileElementYamlShim> Monitors { get; set; }

        /// <summary>
        /// Dependencies required by the profile actions or monitors.
        /// </summary>
        [YamlMember(Alias = "dependencies", Order = 50, ScalarStyle = ScalarStyle.Plain)]
        public List<ExecutionProfileElementYamlShim> Dependencies { get; set; }

        internal static void StandardizeParameterReferences(IDictionary<string, IConvertible> parameters, bool jsonToYaml = false, bool yamlToJson = false)
        {
            if (parameters?.Any() == true)
            {
                foreach (var entry in parameters)
                {
                    string value = entry.Value?.ToString();
                    if (jsonToYaml)
                    {
                        if (!string.IsNullOrWhiteSpace(value) && value.Contains(ExecutionProfile.ParameterPrefix))
                        {
                            value = value.Replace(ExecutionProfile.ParameterPrefix, ExecutionProfileYamlShim.ParameterPrefix, StringComparison.OrdinalIgnoreCase);
                            parameters[entry.Key] = value;
                        }
                    }
                    else if (yamlToJson)
                    {
                        if (!string.IsNullOrWhiteSpace(value) && value.Contains(ExecutionProfileYamlShim.ParameterPrefix))
                        {
                            value = value.Replace(ExecutionProfileYamlShim.ParameterPrefix, ExecutionProfile.ParameterPrefix, StringComparison.OrdinalIgnoreCase);
                            parameters[entry.Key] = value;
                        }
                    }
                }
            }
        }
    }
}