// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// A mock/test firewall manager
    /// </summary>
    public class InMemoryFirewallManager : List<FirewallEntry>, IFirewallManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryFirewallManager"/> class.
        /// </summary>
        public InMemoryFirewallManager()
            : base()
        {
        }

        /// <summary>
        /// Delegate allows the user to define custom logic when an inbound app is enabled.
        /// </summary>
        public Action<FirewallEntry> OnEnableInboundApp { get; set; }

        /// <summary>
        /// Delegate allows the user to define custom logic when an inbound connection is enabled.
        /// </summary>
        public Action<FirewallEntry> OnEnableInboundConnection { get; set; }

        /// <inheritdoc />
        public Task EnableInboundAppAsync(FirewallEntry firewallEntry, CancellationToken cancellationToken)
        {
            this.OnEnableInboundApp?.Invoke(firewallEntry);
            this.Add(firewallEntry);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task EnableInboundConnectionAsync(FirewallEntry firewallEntry, CancellationToken cancellationToken)
        {
            firewallEntry.ThrowIfNull(nameof(firewallEntry));
            this.OnEnableInboundConnection?.Invoke(firewallEntry);
            this.Add(firewallEntry);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task EnableInboundConnectionsAsync(IEnumerable<FirewallEntry> firewallEntries, CancellationToken cancellationToken)
        {
            firewallEntries.ThrowIfEmpty(nameof(firewallEntries));

            foreach (FirewallEntry entry in firewallEntries)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.EnableInboundConnectionAsync(entry, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}
