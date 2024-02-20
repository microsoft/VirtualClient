// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides methods for managing system/OS firewall operations.
    /// </summary>
    public abstract class FirewallManager : IFirewallManager
    {
        /// <summary>
        /// Sets up a new inbound connection/rule in the system/OS firewall.
        /// </summary>
        /// <param name="firewallEntry">The name of the firewall connection entry/rule.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public abstract Task EnableInboundConnectionAsync(FirewallEntry firewallEntry, CancellationToken cancellationToken);

        /// <summary>
        /// Sets up a new inbound connection/rule in the system/OS firewall.
        /// </summary>
        /// <param name="firewallEntries">The name of the firewall connection entries/rules.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public abstract Task EnableInboundConnectionsAsync(IEnumerable<FirewallEntry> firewallEntries, CancellationToken cancellationToken);

        /// <summary>
        /// Set up a new inbound rule for program/app in the system/OS firewall.
        /// </summary>
        /// <param name="firewallEntry">Details of firewall entry/rule.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        public abstract Task EnableInboundAppAsync(FirewallEntry firewallEntry, CancellationToken cancellationToken);
    }
}
