// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents the base/fundamental state object.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class State : IEquatable<State>
    {
        /// <summary>
        /// Initializes a new instance of the 
        /// </summary>
        /// <param name="properties">Metadata properties associated with the state.</param>
        [JsonConstructor]
        public State(IDictionary<string, IConvertible> properties = null)
        {
            this.Properties = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            this.Extensions = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);

            if (properties?.Any() == true)
            {
                this.Properties.AddRange(properties);
            }
        }

        /// <summary>
        /// Metadata properties associated with the state.
        /// </summary>
        [JsonProperty(PropertyName = "properties", Required = Required.Default)]
        [JsonConverter(typeof(ParameterDictionaryJsonConverter))]
        public IDictionary<string, IConvertible> Properties { get; }

        /// <summary>
        /// Extension data/objects associated with the instructions. This is used
        /// to provide an extensibility point for more complex definitions (e.g. passing
        /// an action in a profile).
        /// </summary>
        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, JToken> Extensions { get; }

        /// <summary>
        /// Determines if two objects are equal.
        /// </summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public static bool operator ==(State lhs, State rhs)
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
        public static bool operator !=(State lhs, State rhs)
        {
            return !(lhs == rhs);
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
                State itemDescription = obj as State;
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
        public virtual bool Equals(State other)
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
            int hashCode = string.Join(',', this.Properties.Select(entry => $"{entry.Key}={entry.Value?.ToString()}"))
                .ToUpperInvariant()
                .GetHashCode();

            return hashCode;
        }
    }
}
