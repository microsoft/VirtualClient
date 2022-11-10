// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents a directory or file in the in-memory file system.
    /// </summary>
    public class InMemoryFileSystemEntry : IEquatable<InMemoryFileSystemEntry>
    {
        private int? hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryFileSystemEntry"/> class.
        /// </summary>
        protected InMemoryFileSystemEntry(string path, FileAttributes attributes, DateTime createdTime)
        {
            path.ThrowIfNullOrWhiteSpace(nameof(path));

            this.Path = path;
            this.Attributes = attributes;
            this.CreatedTime = createdTime;
            this.LastWriteTime = createdTime;
            this.LastAccessTime = createdTime;
        }

        /// <summary>
        /// Attributes associated with the file system entry.
        /// </summary>
        public FileAttributes Attributes { get; internal set; }

        /// <summary>
        /// The time at which the file system entry was created.
        /// </summary>
        public DateTime CreatedTime { get; internal set; }

        /// <summary>
        /// The time at which the file system entry was last modified.
        /// </summary>
        public DateTime LastAccessTime { get; internal set; }

        /// <summary>
        /// The time at which the file system entry was last modified.
        /// </summary>
        public DateTime LastWriteTime { get; internal set; }

        /// <summary>
        /// The full path of the file system entry.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Determines if two objects are equal.
        /// </summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public static bool operator ==(InMemoryFileSystemEntry lhs, InMemoryFileSystemEntry rhs)
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
        public static bool operator !=(InMemoryFileSystemEntry lhs, InMemoryFileSystemEntry rhs)
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
                InMemoryFileSystemEntry itemDescription = obj as InMemoryFileSystemEntry;
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
        public virtual bool Equals(InMemoryFileSystemEntry other)
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
                this.hashCode = this.Path.GetHashCode();
            }

            return this.hashCode.Value;
        }
    }
}
