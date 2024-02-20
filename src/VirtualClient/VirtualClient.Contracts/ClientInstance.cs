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
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents a Virtual Client instance (e.g. name, IP address, etc.)
    /// </summary>
    [DebuggerDisplay("{Name}({Role}):{IPAddress}")]
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class ClientInstance : IEquatable<ClientInstance>
    {
        private int? hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientInstance"/> class.
        /// </summary>
        /// <param name="name">
        /// The name of the Virtual Client instance. In practice this is typically
        /// the name of the VM/machine on which it is running.
        /// </param>
        /// <param name="ipAddress">The IP address of the client. </param>
        /// <param name="role">The role of the client instance (e.g. client, server).</param>
        /// <param name="privateIPAddress">
        /// Private IP Address of the client. Note that this is here to enable backwards compatibility with the original privateIPAddress parameter.
        /// This parameter will be deprecated in time.
        /// </param>
        [JsonConstructor]
        public ClientInstance(string name, string ipAddress = null, string role = null, string privateIPAddress = null)
        {
            name.ThrowIfNullOrWhiteSpace(nameof(name));

            if (string.IsNullOrWhiteSpace(privateIPAddress) && string.IsNullOrWhiteSpace(ipAddress))
            {
                ipAddress.ThrowIfNullOrWhiteSpace(nameof(ipAddress));
            }

            this.Name = name;
            this.Role = role;
            this.IPAddress = System.Net.IPAddress.Parse(ipAddress ?? privateIPAddress).ToString(); // Validate syntax

            this.Extensions = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The name of the Virtual Client instance. In practice this is typically
        /// the name of the VM/machine on which it is running.
        /// </summary>
        [JsonProperty(PropertyName = "name", Required = Required.Always, Order = 0)]
        public string Name { get; }

        /// <summary>
        /// The role of the Virtual Client instance (e.g. client, server).
        /// </summary>
        [JsonProperty(PropertyName = "role", Required = Required.Default, Order = 1)]
        public string Role { get; }

        /// <summary>
        /// The IP address of the Virtual Client instance.
        /// </summary>
        [JsonProperty(PropertyName = "ipAddress", Required = Required.Default, Order = 2)]
        public string IPAddress { get; }

        /// <summary>
        /// Gets extensions associated with the client instance. This is an extensibility point
        /// to support additional metadata/properties required to support the needs of the experiment.
        /// </summary>
        [JsonExtensionData(ReadData = true, WriteData = true)]
        public IDictionary<string, JToken> Extensions { get; }

        /// <summary>
        /// Determines if two objects are equal.
        /// </summary>
        /// <param name="lhs">The left hand side.</param>
        /// <param name="rhs">The right hand side.</param>
        /// <returns>True if the objects are equal. False otherwise.</returns>
        public static bool operator ==(ClientInstance lhs, ClientInstance rhs)
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
        public static bool operator !=(ClientInstance lhs, ClientInstance rhs)
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
                ClientInstance itemDescription = obj as ClientInstance;
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
        public virtual bool Equals(ClientInstance other)
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
                    .Append($"{this.Name},{this.Role},{this.IPAddress}");

                if (this.Extensions?.Any() == true)
                {
                    hashBuilder.Append(this.Extensions.Select(ext => $"{ext.Key}={ext.Value?.ToString().GetHashCode(StringComparison.OrdinalIgnoreCase)}"));
                }

                this.hashCode = hashBuilder.ToString().GetHashCode(StringComparison.OrdinalIgnoreCase);
            }

            return this.hashCode.Value;
        }
    }
}
