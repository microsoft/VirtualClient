// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Defines the layout of the Virtual Client instances that are part
    /// of the experiment.
    /// </summary>
    public class EnvironmentLayout
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentLayout"/> class.
        /// </summary>
        /// <param name="clients">The set of clients that are part of the experiment.</param>
        [JsonConstructor]
        public EnvironmentLayout(IEnumerable<ClientInstance> clients)
        {
            clients.ThrowIfNullOrEmpty(nameof(clients));
            this.Clients = new List<ClientInstance>(clients);
        }

        /// <summary>
        /// The type of IP address of the Virtual Client instance
        /// (e.g. Public vs. Private).
        /// </summary>
        [JsonProperty(PropertyName = "clients", Required = Required.Always)]
        public IEnumerable<ClientInstance> Clients { get; }
    }
}
