// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents a description for a dependency that can be used to download
    /// or install it.
    /// </summary>
    public class DependencyDescriptor : Dictionary<string, IConvertible>, IEquatable<DependencyDescriptor>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyDescriptor"/> class.
        /// </summary>
        public DependencyDescriptor()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DependencyDescriptor"/> class.
        /// </summary>
        /// <param name="properties">Properties that describe the dependency.</param>
        public DependencyDescriptor(IDictionary<string, IConvertible> properties)
            : base(properties, StringComparer.OrdinalIgnoreCase)
        {
            properties.ThrowIfInvalid(
                nameof(properties),
                props => this.ContainsKey(nameof(this.Name)),
                $"The dependency '{nameof(this.Name)}' property is required.");
        }

        /// <summary>
        /// True if the dependency/package must be extracted.
        /// </summary>
        public ArchiveType ArchiveType
        {
            get
            {
                return this.GetEnumValue<ArchiveType>(nameof(this.ArchiveType), VirtualClient.ArchiveType.Undefined);
            }

            set
            {
                this[nameof(this.ArchiveType)] = value.ToString();
            }
        }

        /// <summary>
        /// The actual name of the dependency/package blob/file (e.g. geekbench5.1.0.0.zip).
        /// </summary>
        public string Name
        {
            get
            {
                this.TryGetValue(nameof(this.Name), out IConvertible name);
                return name?.ToString();
            }

            set
            {
                this[nameof(this.Name)] = value;
            }
        }

        /// <summary>
        /// True if the dependency/package must be extracted.
        /// </summary>
        public bool Extract
        {
            get
            {
                return this.GetValue<bool>(nameof(this.Extract), false);
            }

            set
            {
                this[nameof(this.Extract)] = value;
            }
        }

        /// <summary>
        /// The logical name of the dependency/package as it should be represented to
        /// the runtime platform (e.g. geekbench5).
        /// </summary>
        public string PackageName
        {
            get
            {
                this.TryGetValue(nameof(this.PackageName), out IConvertible packageName);
                return packageName?.ToString();
            }

            set
            {
                this[nameof(this.PackageName)] = value;
            }
        }

        /// <summary>
        /// Determines if two objects are equal.
        /// </summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public static bool operator ==(DependencyDescriptor lhs, DependencyDescriptor rhs)
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
        public static bool operator !=(DependencyDescriptor lhs, DependencyDescriptor rhs)
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
                DependencyDescriptor descriptor = obj as DependencyDescriptor;
                if (descriptor != null)
                {
                    areEqual = this.Equals(descriptor);
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
        public virtual bool Equals(DependencyDescriptor other)
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
            return string.Join(",", this.Select(entry => $"{entry.Key}={entry.Value}"))
                .GetHashCode(StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Enables the properties for the description to be validated. An exception should be
        /// thrown if required properties or missing or have invalid values.
        /// </summary>
        public void Validate(params string[] requiredProperties)
        {
            if (requiredProperties?.Any() == true)
            {
                foreach (string property in requiredProperties)
                {
                    if (!this.TryGetValue(property, out IConvertible value) || string.IsNullOrWhiteSpace(value?.ToString()))
                    {
                        throw new ArgumentException($"The description is missing the required '{property}' property.");
                    }
                }
            }
        }
    }
}
