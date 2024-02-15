// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Represents an item that contains a document/data object and that can be stored
    /// in a backing data store.
    /// </summary>
    public abstract class ItemBase : IIdentifiable, IJsonExtensible, IEquatable<ItemBase>
    {
        private int? hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemBase"/> class.
        /// </summary>
        /// <param name="item">The item to use as the source definition.</param>
        public ItemBase(ItemBase item)
            : this(
                  item?.Id ?? throw new ArgumentException("The item parameter must be defined", nameof(item)),
                  item.Created,
                  item.LastModified)
        {
            this.Extensions = new Dictionary<string, JToken>(item.Extensions, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemBase"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the item.</param>
        public ItemBase(string id)
            : this(id, DateTime.UtcNow, DateTime.UtcNow)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemBase"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the item.</param>
        /// <param name="created">The date at which the item was created.</param>
        /// <param name="lastModified">The data at which the item was last modified.</param>
        [JsonConstructor]
        public ItemBase(string id, DateTime created, DateTime lastModified)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("The item ID parameter must be defined", nameof(id));
            }

            this.Id = id;
            this.Created = created;
            this.LastModified = lastModified;
            this.Extensions = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets the item id.
        /// </summary>
        [JsonProperty("id", Order = 1)]
        public string Id { get; }

        /// <summary>
        /// Gets the date/time at which the item was created.
        /// </summary>
        [JsonProperty("created", Order = 2)]
        public DateTime Created { get; }

        /// <summary>
        /// Gets the date/time at which the item was last modified.
        /// </summary>
        [JsonProperty("lastModified", Order = 3)]
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Gets extension data/objects associated with the item definition. This is used
        /// to provide an extension point to the fundamental item properties for more
        /// complex item definitions.
        /// </summary>
        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, JToken> Extensions { get; }

        /// <summary>
        /// Determines if two objects are equal.
        /// </summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public static bool operator ==(ItemBase lhs, ItemBase rhs)
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
        public static bool operator !=(ItemBase lhs, ItemBase rhs)
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
                ItemBase itemDescription = obj as ItemBase;
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
        public virtual bool Equals(ItemBase other)
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
                StringBuilder hashBuilder = new StringBuilder($"{this.Id},{this.Created},{this.LastModified}");

                if (this.Extensions?.Any() == true)
                {
                    hashBuilder.Append(this.Extensions.Select(ext => $",{ext.Key}={ext.Value?.ToString()}"));
                }

                this.hashCode = hashBuilder.ToString().GetHashCode(StringComparison.OrdinalIgnoreCase);
            }

            return this.hashCode.Value;
        }
    }
}