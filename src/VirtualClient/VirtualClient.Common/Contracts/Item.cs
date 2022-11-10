// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Contracts
{
    using System;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents an item that contains a document/data object and that can be stored
    /// in a backing data store.
    /// </summary>
    /// <typeparam name="TData">The data type of the document item/object that is contained.</typeparam>
    public class Item<TData> : ItemBase, IEquatable<Item<TData>>
    {
        private int? hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="Item{TData}"/> class.
        /// </summary>
        /// <param name="item">The item to use as the source definition.</param>
        public Item(Item<TData> item)
            : base(item)
        {
            if (item.Definition == null)
            {
                throw new ArgumentException("The item data parameter must be defined", nameof(item));
            }

            this.Definition = item.Definition;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Item{TData}"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the item.</param>
        /// <param name="definition">The data associated with the item.</param>
        public Item(string id, TData definition)
            : base(id, DateTime.UtcNow, DateTime.UtcNow)
        {
            if (definition == null)
            {
                throw new ArgumentException("The item data parameter must be defined", nameof(definition));
            }

            this.Definition = definition;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Item{TData}"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the item.</param>
        /// <param name="created">The date at which the item was created.</param>
        /// <param name="lastModified">The data at which the item was last modified.</param>
        /// <param name="definition">The data associated with the item.</param>
        [JsonConstructor]
        public Item(string id, DateTime created, DateTime lastModified, TData definition)
            : base(id, created, lastModified)
        {
            if (definition == null)
            {
                throw new ArgumentException("The item data parameter must be defined", nameof(definition));
            }

            this.Definition = definition;
        }

        /// <summary>
        /// Gets the data object/definition contained by the item.
        /// </summary>
        [JsonProperty("definition", Order = 100)]
        public TData Definition { get; }

        /// <summary>
        /// Determines if two objects are equal.
        /// </summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public static bool operator ==(Item<TData> lhs, Item<TData> rhs)
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
        public static bool operator !=(Item<TData> lhs, Item<TData> rhs)
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
                Item<TData> itemDescription = obj as Item<TData>;
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
        public virtual bool Equals(Item<TData> other)
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
                StringBuilder hashBuilder = new StringBuilder();
                hashBuilder.Append(base.GetHashCode());
                hashBuilder.Append(this.Definition.GetHashCode());
                this.hashCode = hashBuilder.ToString().GetHashCode(StringComparison.OrdinalIgnoreCase);
            }

            return this.hashCode.Value;
        }
    }
}