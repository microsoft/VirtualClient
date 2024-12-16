// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Newtonsoft.Json;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides information on the location/path of a dependency that exists
    /// on the system.
    /// </summary>
    [DebuggerDisplay("{Name}: {Path}")]
    public class DependencyPath : DependencyMetadata, IEquatable<DependencyPath>
    {
        /// <summary>
        /// The path to the Virtual Client contracts assembly.
        /// </summary>
        public static readonly string ContractsAssemblyPath = System.IO.Path.GetDirectoryName(Assembly.GetAssembly(typeof(DependencyPath)).Location);
        private int? hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyPath"/> class.
        /// </summary>
        /// <param name="name">The unique name/ID of the dependency used to distinguish it from other dependencies.</param>
        /// <param name="path">The path to the dependency.</param>
        /// <param name="description">A description of the dependency.</param>
        /// <param name="version">The version of the package/dependency.</param>
        /// <param name="metadata">A set of additional platform-specific metadata/properties associated with the dependency.</param>
        /// <param name="timestamp">
        /// A timestamp that indicates the time-of-origin for the dependency package (e.g. when it was registered). This timestamp can
        /// be used to resolve conflicts between packages having the same name.
        /// </param>
        [JsonConstructor]
        public DependencyPath(string name, string path, string description = null, string version = null, IDictionary<string, IConvertible> metadata = null, DateTime? timestamp = null)
            : base(name, description, version, metadata)
        {
            path.ThrowIfNullOrWhiteSpace(nameof(path));
            this.Path = path;
            this.Timestamp = timestamp ?? DateTime.UtcNow;
        }

        /// <summary>
        /// The path to the dependency.
        /// </summary>
        [JsonProperty("path", Required = Required.Always, Order = 3)]
        public string Path { get; }

        /// <summary>
        /// A timestamp for when the dependency
        /// </summary>
        [JsonProperty("timestamp", Required = Required.Always, Order = 3)]
        public DateTime Timestamp { get; }

        /// <summary>
        /// Determines if two objects are equal.
        /// </summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public static bool operator ==(DependencyPath lhs, DependencyPath rhs)
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
        public static bool operator !=(DependencyPath lhs, DependencyPath rhs)
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
                DependencyPath itemDescription = obj as DependencyPath;
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
        public virtual bool Equals(DependencyPath other)
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
                    .Append($"{this.Name},{this.Path},{this.Description},{this.Version}");

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
