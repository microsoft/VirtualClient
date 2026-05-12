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
        /// Generates a unique string representation of this instance
        /// <list type="bullet">
        /// <item>
        /// <term>Example 1</term>
        /// <description>Type:ParallelExecution,,,ComponentType:Action,,,Metadata:Key1=Value1;Key2=Value2,,,Parameters:Key1=Value1;Key2=Value2</description>
        /// </item>
        /// <item>
        /// <term>Example 2</term>
        /// <description>Type:ParallelExecution,,,ComponentType:Action,,,Metadata:Key1=Value1;Key2=Value2,,,Parameters:Key1=Value1;Key2=Value2,,,Component:(Type:OpenSslExecutor,,,ComponentType:Action,,,Metadata:Key1=Value1;Key2=Value2,,,Parameters:Key1=Value1;Key2=Value2).</description>
        /// </item>
        /// <item>
        /// <term>Example 3</term>
        /// <description>Type:ParallelExecution,,,ComponentType:Action,,,Metadata:Key1=Value1;Key2=Value2,,,Parameters:Key1=Value1;Key2=Value2,,,Component:(Type:OpenSslExecutor,,,ComponentType:Action,,,Metadata:Key1=Value1;Key2=Value2,,,Parameters:Key1=Value1;Key2=Value2),,,Extension:(Contacts={'email':'example@example.com'})</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <returns>A string representation of this.</returns>
        public override string ToString()
        {
            return this.ToString(includeMetadata: true);
        }

        /// <summary>
        /// Generates a unique string representation of this instance
        /// <list type="bullet">
        /// <item>
        /// <term>Example 1</term>
        /// <description>Type:ParallelExecution,,,ComponentType:Action,,,Metadata:Key1=Value1;Key2=Value2,,,Parameters:Key1=Value1;Key2=Value2</description>
        /// </item>
        /// <item>
        /// <term>Example 2</term>
        /// <description>Type:ParallelExecution,,,ComponentType:Action,,,Metadata:Key1=Value1;Key2=Value2,,,Parameters:Key1=Value1;Key2=Value2,,,Component:(Type:OpenSslExecutor,,,ComponentType:Action,,,Metadata:Key1=Value1;Key2=Value2,,,Parameters:Key1=Value1;Key2=Value2).</description>
        /// </item>
        /// <item>
        /// <term>Example 3</term>
        /// <description>Type:ParallelExecution,,,ComponentType:Action,,,Metadata:Key1=Value1;Key2=Value2,,,Parameters:Key1=Value1;Key2=Value2,,,Component:(Type:OpenSslExecutor,,,ComponentType:Action,,,Metadata:Key1=Value1;Key2=Value2,,,Parameters:Key1=Value1;Key2=Value2),,,Extension:(Contacts={'email':'example@example.com'})</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <returns>A string representation of this.</returns>
        public string ToString(bool includeMetadata)
        {
            StringBuilder builder = new StringBuilder();
            builder.Append($"Type:{this.Type},,,ComponentType:{this.ComponentType}");

            if (includeMetadata && this.Metadata?.Any() == true)
            {
                builder.Append($",,,Metadata:[{string.Join(";", this.Metadata.Select(m => $"{m.Key}={m.Value}"))}]");
            }

            if (this.Parameters?.Any() == true)
            {
                builder.Append($",,,Parameters:[{string.Join(";", this.Parameters.Select(p => $"{p.Key}={p.Value}"))}]");
            }

            if (this.Components?.Any() == true)
            {
                builder.Append($",,,Components:[{string.Join(";", this.Components.Select(c => $"({c.ToString(includeMetadata)})"))}]");
            }

            if (this.Extensions?.Any() == true)
            {
                builder.Append($",,,Extensions:[{string.Join(";", this.Extensions.Select(e => $"({e.Key}={e.Value?.ToString()})"))}]");
            }

            return builder.ToString().RemoveWhitespace();
        }
    }
}
