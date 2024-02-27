// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;

    /// <summary>
    /// Provides methods for managing Windows system/OS firewall operations.
    /// </summary>
    /// <remarks>
    /// Windows netsh.exe advfirewall Overview
    /// https://docs.microsoft.com/en-us/troubleshoot/windows-server/networking/netsh-advfirewall-firewall-control-firewall-behavior
    /// 
    /// Windows netsh.exe firewall vs. netsh.exe firewall
    /// https://support.microsoft.com/en-us/topic/44af15a8-72a1-e699-7290-569726b39d4a#:~:text=The%20netsh%20advfirewall%20firewall%20command-line%20context%20is%20available,netsh%20firewall%20context%20in%20earlier%20Windows%20operating%20systems.
    /// </remarks>
    [WindowsCompatible]
    public class WindowsFirewallManager : FirewallManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsFirewallManager"/> class.
        /// </summary>
        public WindowsFirewallManager(ProcessManager processManager)
            : base()
        {
            processManager.ThrowIfNull(nameof(processManager));
            this.ProcessManager = processManager;
        }

        /// <summary>
        /// Handles the creation and execution of the Windows firewall command line
        /// toolset process.
        /// </summary>
        public ProcessManager ProcessManager { get; }

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
                    ports = $"{firewallEntry.PortRange.Start.Value}-{firewallEntry.PortRange.End.Value}";
                }

                bool ruleAlreadyExists = false;

                // This command DOES NOT require administrative privilege
                string netshCommandArguments = $"advfirewall firewall show rule name=\"{firewallEntry.Name}\"";

                using (IProcessProxy process = this.ProcessManager.CreateProcess("netsh", netshCommandArguments))
                {
                    await process.StartAndWaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (process.ExitCode == 0)
                    {
                        ruleAlreadyExists = Regex.IsMatch(process.StandardOutput.ToString(), $@"LocalPort:\s*{ports}", RegexOptions.IgnoreCase);

                        if (!ruleAlreadyExists)
                        {
                            try
                            {
                                await this.DeleteFirewallRuleAsync(firewallEntry.Name, cancellationToken).ConfigureAwait(false);
                            }
                            catch
                            {
                                // Attempted to delete if already exists.
                                // Should not block VC on failure.
                            }
                        }
                    }
                }

                if (!ruleAlreadyExists)
                {
                    // This command DOES require administrative privilege
                    netshCommandArguments = $"advfirewall firewall add rule name=\"{firewallEntry.Name}\" dir=in protocol={firewallEntry.Protocol} localport={ports} action=allow";

                    using (IProcessProxy process = this.ProcessManager.CreateElevatedProcess(PlatformID.Win32NT, "netsh", netshCommandArguments))
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
        }

        /// <inheritdoc />
        public async override Task EnableInboundAppAsync(FirewallEntry firewallEntry, CancellationToken cancellationToken)
        {
            firewallEntry.ThrowIfNull(nameof(firewallEntry));

            if (!cancellationToken.IsCancellationRequested)
            {
                bool ruleAlreadyExists = false;

                // This command DOES NOT require administrative privilege
                string netshCommandArguments = $"advfirewall firewall show rule name=\"{firewallEntry.Name}\" verbose";

                using (IProcessProxy process = this.ProcessManager.CreateProcess("netsh", netshCommandArguments))
                {
                    await process.StartAndWaitAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (process.ExitCode == 0)
                    {
                        ruleAlreadyExists = Regex.IsMatch(process.StandardOutput.ToString(), $@"Program:\s*{Regex.Escape(firewallEntry.AppPath)}", RegexOptions.IgnoreCase);

                        if (!ruleAlreadyExists)
                        {
                            try
                            {
                                await this.DeleteFirewallRuleAsync(firewallEntry.Name, cancellationToken).ConfigureAwait(false);
                            }
                            catch
                            {
                                // Attempted to delete if already exists.
                                // Should not block VC on failure.
                            }
                        }
                    }
                }

                if (!ruleAlreadyExists)
                {
                    // This command DOES require administrative privilege
                    netshCommandArguments = $"advfirewall firewall add rule name=\"{firewallEntry.Name}\" dir=in action=allow program=\"{firewallEntry.AppPath}\" enable=yes";

                    using (IProcessProxy process = this.ProcessManager.CreateElevatedProcess(PlatformID.Win32NT, "netsh", netshCommandArguments))
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

        /// <summary>
        /// Deletes the Firewall rule.
        /// </summary>
        /// <param name="name">Deletes all Firewall rules with this name.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        private Task DeleteFirewallRuleAsync(string name, CancellationToken cancellationToken)
        {
            name.ThrowIfNullOrWhiteSpace(nameof(name));
            
            string deleteRule = $"advfirewall firewall delete rule name=\"{name}\"";
            IProcessProxy process = this.ProcessManager.CreateElevatedProcess(PlatformID.Win32NT, "netsh", deleteRule);

            return process.StartAndWaitAsync(cancellationToken);
        }
    }
}
