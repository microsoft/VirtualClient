// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides metadata information for a package/dependency that exists on the system.
    /// </summary>
    [DebuggerDisplay("{Name}: {Path}")]
    public class DependencyMetadata : IEquatable<DependencyMetadata>
    {
        private int? hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyPath"/> class.
        /// </summary>
        /// <param name="name">The unique name/ID of the package/dependency used to distinguish it from other dependencies.</param>
        /// <param name="description">A description of the package/dependency.</param>
        /// <param name="version">The version of the package/dependency.</param>
        /// <param name="metadata">A set of additional platform-specific metadata/properties associated with the package/dependency.</param>
        [JsonConstructor]
        public DependencyMetadata(string name, string description = null, string version = null, IDictionary<string, IConvertible> metadata = null)
        {
            name.ThrowIfNullOrWhiteSpace(nameof(name));

            this.Name = name;
            this.Description = description;
            this.Version = version;
            this.Metadata = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);

            if (metadata != null)
            {
                this.Metadata.AddRange(metadata);
            }
        }

        /// <summary>
        /// The unique name/ID of the package/dependency used to distinguish it from 
        /// other dependencies.
        /// </summary>
        [JsonProperty("name", Required = Required.Always, Order = 0)]
        public string Name { get; }

        /// <summary>
        /// A description of the package/dependency.
        /// </summary>
        [JsonProperty("description", Required = Required.Default, Order = 1)]
        public string Description { get; }

        /// <summary>
        /// The version of the package/dependency.
        /// </summary>
        [JsonProperty("version", Required = Required.Default, Order = 2)]
        public string Version { get; }

        /// <summary>
        /// A set of additional platform-specific metadata/properties associated with
        /// the package/dependency
        /// </summary>
        [JsonProperty("metadata", Required = Required.Default, Order = 10)]
        [JsonConverter(typeof(ParameterDictionaryJsonConverter))]
        public IDictionary<string, IConvertible> Metadata { get; }

        /// <summary>
        /// Returns true/false whether the dependency package contains extensions to the platform
        /// runtime.
        /// </summary>
        [JsonIgnore]
        public bool IsExtensions
        {
            get
            {
                return this.Metadata.TryGetValue(PackageMetadata.Extensions, out IConvertible isExtensions)
                    && Convert.ToBoolean(isExtensions) == true;
            }
        }

        /// <summary>
        /// Determines if two objects are equal.
        /// </summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public static bool operator ==(DependencyMetadata lhs, DependencyMetadata rhs)
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
        public static bool operator !=(DependencyMetadata lhs, DependencyMetadata rhs)
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
                DependencyMetadata itemDescription = obj as DependencyMetadata;
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
        public virtual bool Equals(DependencyMetadata other)
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
            if (this.hashCode == null)
            {
                StringBuilder hashBuilder = new StringBuilder()
                    .Append($"{this.Name},{this.Description},{this.Version}");

                if (this.Metadata.Any())
                {
                    hashBuilder.Append(string.Join(",", this.Metadata.Select(entry => $"{entry.Key}={entry.Value?.ToString()}")));
                }

                this.hashCode = hashBuilder.ToString().ToLowerInvariant().GetHashCode(StringComparison.OrdinalIgnoreCase);
            }

            return this.hashCode.Value;
        }
    }
}
