// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Represents a set of instructions that can be supplied on-demand to different
    /// components within the Virtual Client running process to request profiling.
    /// </summary>
    public class Instructions : State, IEquatable<Instructions>
    {
        private const string ComponentExtension = "component";

        /// <summary>
        /// Initializes a new instance of the <see cref="Instructions"/> class.
        /// </summary>
        /// <param name="type">An identifier for the type of instructions.</param>
        /// <param name="properties">Metadata properties associated with the state.</param>
        [JsonConstructor]
        public Instructions(InstructionsType type, IDictionary<string, IConvertible> properties = null)
            : base(properties)
        {
            this.Type = type;
        }

        /// <summary>
        /// An identifier for the type of instructions (e.g. Profiling).
        /// </summary>
        [JsonProperty(PropertyName = "type", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public InstructionsType Type { get; set; }

        /// <summary>
        /// Determines if two objects are equal.
        /// </summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public static bool operator ==(Instructions lhs, Instructions rhs)
        {
            if (object.ReferenceEquals(lhs, rhs))
            {
                return true;
            }

            if (object.ReferenceEquals(null, lhs) || object.ReferenceEquals(null, rhs))
            {
                return false;
            }

            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Determines if two objects are NOT equal.
        /// </summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>True if the objects are NOT equal. False otherwise.</returns>
        public static bool operator !=(Instructions lhs, Instructions rhs)
        {
            return !(lhs == rhs);
        }

        /// <summary>
        /// Adds the details for the component to the extensions for the instructions
        /// object instance.
        /// </summary>
        /// <param name="typeName">The type of the component (e.g. ServerExecutor, VirtualClient.Actions.ServerExecutor).</param>
        /// <param name="parameters">The parameters associated with the component.</param>
        public void AddComponent(string typeName, IDictionary<string, IConvertible> parameters)
        {
            this.Extensions.Add(
                Instructions.ComponentExtension,
                JToken.FromObject(new ExecutionProfileElement(typeName, parameters)));
        }

        /// <summary>
        /// Override method determines if the two objects are equal
        /// </summary>
        /// <param name="obj">Defines the object to compare against the current instance.</param>
        /// <returns>
        /// Type:  System.Boolean
        /// True if the objects are equal or False if not
        /// </returns>
        public override bool Equals(object obj)
        {
            bool areEqual = false;

            if (object.ReferenceEquals(this, obj))
            {
                areEqual = true;
            }
            else
            {
                // Apply value-type semantics to determine
                // the equality of the instances
                Instructions itemDescription = obj as Instructions;
                if (itemDescription != null)
                {
                    areEqual = this.Equals(itemDescription);
                }
            }

            return areEqual;
        }

        /// <summary>
        /// Method determines if the other object is equal to this instance
        /// </summary>
        /// <param name="other">Defines the object to compare against the current instance.</param>
        /// <returns>
        /// Type:  System.Boolean
        /// True if the objects are equal or False if not
        /// </returns>
        public virtual bool Equals(Instructions other)
        {
            return other != null && this.GetHashCode() == other.GetHashCode();
        }

        /// <summary>
        /// Override method returns a unique integer hash code
        /// identifier for the class instance
        /// </summary>
        /// <returns>
        /// Type:  System.Int32
        /// A unique integer identifier for the class instance
        /// </returns>
        public override int GetHashCode()
        {
            int hashCode = $"{this.Type},{string.Join(',', this.Properties.Select(entry => $"{entry.Key}={entry.Value?.ToString()}"))}"
                .ToUpperInvariant()
                .GetHashCode();

            return hashCode;
        }

        /// <summary>
        /// Adds the details for the component to the extensions for the instructions
        /// object instance.
        /// </summary>
        /// <param name="component">The component to add to the instructions details.</param>
        public bool TryGetComponent(out ExecutionProfileElement component)
        {
            component = null;
            if (this.Extensions.TryGetValue(Instructions.ComponentExtension, out JToken profileElement))
            {
                component = profileElement.ToObject<ExecutionProfileElement>();
            }

            return component != null;
        }
    }
}
