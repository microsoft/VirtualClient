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
    /// One element in the execution profile
    /// </summary>
    public class ExecutionProfileElementYamlShim
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionProfileElement"/> class.
        /// </summary>
        public ExecutionProfileElementYamlShim()
        {
            this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            this.Metadata = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionProfileElement"/> class.
        /// </summary>
        public ExecutionProfileElementYamlShim(ExecutionProfileElement other)
            : this()
        {
            other.ThrowIfNull(nameof(other));

            this.Type = other.Type;
            if (other?.Metadata?.Any() == true)
            {
                this.Metadata.AddRange(other.Metadata);
            }

            if (other?.Parameters?.Any() == true)
            {
                this.Parameters.AddRange(other.Parameters);
                ExecutionProfileYamlShim.StandardizeParameterReferences(this.Parameters);
            }

            if (other?.Components?.Any() == true)
            {
                this.Components = new List<ExecutionProfileElementYamlShim>(other.Components.Select(c => new ExecutionProfileElementYamlShim(c)));
            }
        }

        /// <summary>
        /// The type of profile component (e.g. Action, Dependency, Monitor).
        /// </summary>
        [YamlIgnore]
        public ComponentType ComponentType { get; internal set; }

        /// <summary>
        /// The type of this element
        /// </summary>
        [YamlMember(Alias = "type", Order = 0, ScalarStyle = ScalarStyle.Plain)]
        public string Type { get; set; }

        /// <summary>
        /// Parameters for this element
        /// </summary>
        [YamlMember(Alias = "metadata", Order = 10, ScalarStyle = ScalarStyle.Plain)]
        public IDictionary<string, IConvertible> Metadata { get; set; }

        /// <summary>
        /// Parameters for this element
        /// </summary>
        [YamlMember(Alias = "parameters", Order = 20, ScalarStyle = ScalarStyle.Plain)]
        public IDictionary<string, IConvertible> Parameters { get; set; }

        /// <summary>
        /// Child/sub-components of the element.
        /// </summary>
        [YamlMember(Alias = "components", Order = 30, ScalarStyle = ScalarStyle.Plain)]
        public IEnumerable<ExecutionProfileElementYamlShim> Components { get; set; }
    }
}
