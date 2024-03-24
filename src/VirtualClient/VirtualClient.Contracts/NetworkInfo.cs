// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Provides information about the network interfaces on the system.
    /// </summary>
    public class NetworkInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkInfo" /> class.
        /// </summary>
        public NetworkInfo(IEnumerable<NetworkInterfaceInfo> interfaces)
        {
            if (interfaces?.Any() == true)
            {
                this.Interfaces = new List<NetworkInterfaceInfo>(interfaces);
            }
        }

        /// <summary>
        /// The set of network interfaces on the system.
        /// </summary>
        public IEnumerable<NetworkInterfaceInfo> Interfaces { get; }
    }
}
