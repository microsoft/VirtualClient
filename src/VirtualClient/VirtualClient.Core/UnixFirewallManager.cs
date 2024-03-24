// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;

    /// <summary>
    /// Provides methods for managing Unix/Linux system/OS firewall operations.
    /// </summary>
    [UnixCompatible]
    public class UnixFirewallManager : FirewallManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnixFirewallManager"/> class.
        /// </summary>
        public UnixFirewallManager(ProcessManager processManager)
            : base()
        {
            processManager.ThrowIfNull(nameof(processManager));
            this.ProcessManager = processManager;
        }

        /// <summary>
        /// Handles the creation and execution of the Unix firewall command line.
        /// toolset process.
        /// </summary>
        public ProcessManager ProcessManager { get; }

        /// <inheritdoc />
        public override Task EnableInboundAppAsync(FirewallEntry firewallEntry, CancellationToken cancellationToken)
        {
            // Not needed in Linux.
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async override Task EnableInboundConnectionAsync(FirewallEntry firewallEntry, CancellationToken cancellationToken)
        {
            firewallEntry.ThrowIfNull(nameof(firewallEntry));
            if (!cancellationToken.IsCancellationRequested)
            {
                string ports = null;
                if (firewallEntry.Ports?.Any() == true)
                {
                    ports = string.Join(',', firewallEntry.Ports);
                }
                else if (firewallEntry.PortRange.Start.Value != 0 && firewallEntry.PortRange.End.Value != 0)
                {
                    ports = $"{firewallEntry.PortRange.Start.Value}:{firewallEntry.PortRange.End.Value}";
                }

                string command = "sudo";
                string arguments = $"iptables -A INPUT -p {firewallEntry.Protocol} --match multiport --dports {ports} -j ACCEPT";

                using (IProcessProxy process = this.ProcessManager.CreateElevatedProcess(PlatformID.Unix, command, arguments))
                {
                    await process.StartAndWaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        process.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorReason: ErrorReason.DependencyInstallationFailed);
                    }
                }
            }
        }

        /// <inheritdoc />
        public async override Task EnableInboundConnectionsAsync(IEnumerable<FirewallEntry> firewallEntries, CancellationToken cancellationToken)
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
