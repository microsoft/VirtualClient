// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Text;

    /// <summary>
    /// Represents an event captured on the system.
    /// </summary>
    public class SystemEvent : IEquatable<SystemEvent>
    {
        private int? hashCode;

        /// <summary>
        /// Event id of a found event
        /// </summary>
        public int EventId { get; set; }

        /// <summary>
        /// Event time of a found event
        /// </summary>
        public DateTime EventTime { get; set; }

        /// <summary>
        /// Xml of a found event
        /// </summary>
        public string Details { get; set; }

        /// <summary>
        /// The ID of the process that logged the event.
        /// </summary>
        public int ProcessId { get; set; }

        /// <summary>
        /// The record ID of the event in the event log.
        /// </summary>
        public long RecordId { get; set; }

        /// <summary>
        /// Determines if two objects are equal.
        /// </summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public static bool operator ==(SystemEvent lhs, SystemEvent rhs)
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
        public static bool operator !=(SystemEvent lhs, SystemEvent rhs)
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
                SystemEvent itemDescription = obj as SystemEvent;
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
        public virtual bool Equals(SystemEvent other)
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
                this.hashCode = new StringBuilder()
                    .Append($"{this.EventId},{this.RecordId},{this.ProcessId},{this.EventTime.Ticks},{this.Details}")
                    .ToString().GetHashCode(StringComparison.OrdinalIgnoreCase);
            }

            return this.hashCode.Value;
        }
    }
}
