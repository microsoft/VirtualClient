// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Extensions for <see cref="EnvironmentLayout"/> instances.
    /// </summary>
    public static class EnvironmentLayoutExtensions
    {
        /// <summary>
        /// Returns the client instance from the layout that matches with the local sytem (i.e. matching IP address)
        /// or null if one is not found.
        /// </summary>
        /// <param name="clientId">The ID of the Virtual Client agent/instance.</param>
        /// <param name="layout">The environment layout containing all Virtual Client instance definitions.</param>
        /// <returns></returns>
        public static ClientInstance GetClientInstance(this EnvironmentLayout layout, string clientId)
        {
            IEnumerable<ClientInstance> clientInstances = layout?.Clients
                .Where(client => string.Equals(client.Name, clientId, StringComparison.OrdinalIgnoreCase));

            if (clientInstances?.Count() > 1)
            {
                throw new DependencyException(
                    $"Ambiguous environment layout scenario. There is more than one client instance defined in the environment layout " +
                    $"provided to the Virtual Client for agent/client ID '{clientId}'.",
                    ErrorReason.LayoutInvalid);
            }

            return clientInstances?.FirstOrDefault();
        }
    }
}
