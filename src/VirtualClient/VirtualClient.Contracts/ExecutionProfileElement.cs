// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// One element in the execution profile
    /// </summary>
    public class ExecutionProfileElement : IEquatable<ExecutionProfileElement>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionProfileElement"/> class.
        /// </summary>
        [JsonConstructor]
        public ExecutionProfileElement(string type, IDictionary<string, IConvertible> parameters, IDictionary<string, IConvertible> metadata = null, IEnumerable<ExecutionProfileElement> components = null)
        {
            type.ThrowIfNullOrWhiteSpace(nameof(type));

            this.Type = type;

            this.Parameters = parameters != null 
                ? new Dictionary<string, IConvertible>(parameters, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);

            this.Metadata = metadata != null
                ? new Dictionary<string, IConvertible>(metadata, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);

            this.Extensions = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);

            if (components?.Any() == true)
            {
                this.Components = new List<ExecutionProfileElement>(components);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionProfileElement"/> class.
        /// </summary>
        /// <param name="other">The instance to create a new object from.</param>
        public ExecutionProfileElement(ExecutionProfileElement other)
            : this(other?.Type, other?.Parameters, other?.Metadata, other?.Components)
        { 
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionProfileElement"/> class.
        /// </summary>
        /// <param name="other">The instance to create a new object from.</param>
        public ExecutionProfileElement(ExecutionProfileElementYamlShim other)
            : this(other?.Type, other?.Parameters, other?.Metadata, other?.Components?.Select(c => new ExecutionProfileElement(c)))
        {
        }

        /// <summary>
        /// The type of profile component (e.g. Action, Dependency, Monitor).
        /// </summary>
        [JsonIgnore]
        public ComponentType ComponentType { get; internal set; }

        /// <summary>
        /// The name of this element
        /// </summary>
        [JsonProperty(PropertyName = "Type", Required = Required.Always, Order = 10)]
        public string Type { get; }

        /// <summary>
        /// Parameters for this element
        /// </summary>
        [JsonProperty(PropertyName = "Metadata", Required = Required.Default, Order = 20)]
        [JsonConverter(typeof(ParameterDictionaryJsonConverter))]
        public IDictionary<string, IConvertible> Metadata { get; }

        /// <summary>
        /// Parameters for this element
        /// </summary>
        [JsonProperty(PropertyName = "Parameters", Required = Required.Default, Order = 30)]
        [JsonConverter(typeof(ParameterDictionaryJsonConverter))]
        public IDictionary<string, IConvertible> Parameters { get; }

        /// <summary>
        /// Child/sub-components of the element.
        /// </summary>
        [JsonProperty(PropertyName = "Components", Required = Required.Default, Order = 31)]
        public IEnumerable<ExecutionProfileElement> Components { get; }

        /// <summary>
        /// Extension data/objects associated with the profile element definition. This is used
        /// to provide an extensibility point for more complex definitions.
        /// </summary>
        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, JToken> Extensions { get; }

        /// <summary>
        /// Determines equality between this instance and another instance.
        /// </summary>
        /// <param name="other">The other instance to determine equality against.</param>
        /// <returns>True/False if the two instances are equal.</returns>
        public bool Equals(ExecutionProfileElement other)
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
            if (obj == null || !(obj is ExecutionProfileElement))
            {
                return false;
            }

            return this.Equals(obj as ExecutionProfileElement);
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
            StringBuilder builder = new StringBuilder();
            builder.Append(this.Type)
                .AppendJoin(";", this.Parameters.Select(p => $"{p.Key};{p.Value}"));

            return builder.ToString();
        }
    }
}
