// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System.Collections.Generic;
    using System.Linq;
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
        /// The set of client instances that are part of the experiment.
        /// </summary>
        [JsonProperty(PropertyName = "clients", Required = Required.Always)]
        public IEnumerable<ClientInstance> Clients { get; }

        /// <summary>
        /// Returns a string representation of the environment layout
        /// (e.g. client01,10.1.0.1,Client;client02,10.1.0.2,Server).
        /// </summary>
        public override string ToString()
        {
            return string.Join(";", this.Clients.Select(client => client.ToString()));
        }
    }
}
