// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Contracts;

    /// <summary>
    /// CPS(Connections Per Second) Tool Server Executor 
    /// </summary>
    public class CPSServerExecutor : CPSExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CPSServerExecutor"/> class.
        /// </summary>
        /// <param name="component">Component to copy.</param>
        public CPSServerExecutor(VirtualClientComponent component)
           : base(component)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CPSServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public CPSServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
        }

        private void InitializeLinuxServerCommandline()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;

            // Ensure base string isn't null.
            this.CommandLineLinuxServer ??= string.Empty;

            // Normalize: keep a trailing space so appends don't glue together.
            if (this.CommandLineLinuxServer.Length > 0 && !char.IsWhiteSpace(this.CommandLineLinuxServer[^1]))
            {
                this.CommandLineLinuxServer += " ";
            }

            // -c (client mode)
            if (!this.CommandLineLinuxServer.Contains("-s", StringComparison.OrdinalIgnoreCase))
            {
                this.CommandLineLinuxServer += " -s";
            }

            // -r {Connections}
            // Your reference includes "-c -r {Connections}"
            if (!this.CommandLineLinuxServer.Contains("-r", StringComparison.OrdinalIgnoreCase))
            {
                this.CommandLineLinuxServer += $" -r {this.Connections} ";
            }

            // Endpoint tuple:
            // {clientIPAddress},0,{serverIPAddress},{Port},{ConnectionsPerThread},{MaxPendingRequestsPerThread},{ConnectionDuration},{DataTransferMode}
            // Add it only if we don't already see the server IP (good heuristic to avoid duplication).
            if (!this.CommandLineLinuxServer.Contains(serverIPAddress, StringComparison.OrdinalIgnoreCase))
            {
                this.CommandLineLinuxServer +=
                    $"{serverIPAddress},{this.Port} ";
            }

            // -i {DisplayInterval}
            if (!this.CommandLineLinuxServer.Contains("-i", StringComparison.OrdinalIgnoreCase))
            {
                this.CommandLineLinuxServer += $" -i {this.DisplayInterval} ";
            }

            // -wt {WarmupTime.TotalSeconds}
            if (!this.CommandLineLinuxServer.Contains("-wt", StringComparison.OrdinalIgnoreCase) && this.WarmupTime != null)
            {
                this.CommandLineLinuxServer += $" -wt {this.WarmupTime.TotalSeconds} ";
            }

            // -t {TestDuration.TotalSeconds}
            if (!this.CommandLineLinuxServer.Contains("-t", StringComparison.OrdinalIgnoreCase) && this.TestDuration != null)
            {
                this.CommandLineLinuxServer += $" -t {this.TestDuration.TotalSeconds} ";
            }

            // Optional: -ds {DelayTime.TotalSeconds} only if DelayTime != 0
            if (!this.CommandLineLinuxServer.Contains("-ds", StringComparison.OrdinalIgnoreCase) &&
                this.DelayTime != TimeSpan.Zero)
            {
                this.CommandLineLinuxServer += $" -ds {this.DelayTime.TotalSeconds} ";
            }

            this.CommandLineLinuxServer = this.CommandLineLinuxServer.Trim();
        }

        private void InitializeWindowsServerCommandline()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;

            // Ensure base string isn't null.
            this.CommandLineWindowsServer ??= string.Empty;

            // Normalize: keep a trailing space so appends don't glue together.
            if (this.CommandLineWindowsServer.Length > 0 && !char.IsWhiteSpace(this.CommandLineWindowsServer[^1]))
            {
                this.CommandLineWindowsServer += " ";
            }

            // -c (client mode)
            if (!this.CommandLineWindowsServer.Contains("-s", StringComparison.OrdinalIgnoreCase))
            {
                this.CommandLineWindowsServer += " -s";
            }

            // -r {Connections}
            // Your reference includes "-c -r {Connections}"
            if (!this.CommandLineWindowsServer.Contains("-r", StringComparison.OrdinalIgnoreCase))
            {
                this.CommandLineWindowsServer += $" -r {this.Connections} ";
            }

            // Endpoint tuple:
            // {clientIPAddress},0,{serverIPAddress},{Port},{ConnectionsPerThread},{MaxPendingRequestsPerThread},{ConnectionDuration},{DataTransferMode}
            // Add it only if we don't already see the server IP (good heuristic to avoid duplication).
            if (!this.CommandLineWindowsServer.Contains(serverIPAddress, StringComparison.OrdinalIgnoreCase))
            {
                this.CommandLineWindowsServer +=
                    $"{serverIPAddress},{this.Port} ";
            }

            // -i {DisplayInterval}
            if (!this.CommandLineWindowsServer.Contains("-i", StringComparison.OrdinalIgnoreCase))
            {
                this.CommandLineWindowsServer += $" -i {this.DisplayInterval} ";
            }

            // -wt {WarmupTime.TotalSeconds}
            if (!this.CommandLineWindowsServer.Contains("-wt", StringComparison.OrdinalIgnoreCase) && this.WarmupTime != null)
            {
                this.CommandLineWindowsServer += $" -wt {this.WarmupTime.TotalSeconds} ";
            }

            // -t {TestDuration.TotalSeconds}
            if (!this.CommandLineWindowsServer.Contains("-t", StringComparison.OrdinalIgnoreCase) && this.TestDuration != null)
            {
                this.CommandLineWindowsServer += $" -t {this.TestDuration.TotalSeconds} ";
            }

            // Optional: -ds {DelayTime.TotalSeconds} only if DelayTime != 0
            if (!this.CommandLineWindowsServer.Contains("-ds", StringComparison.OrdinalIgnoreCase) &&
                this.DelayTime != TimeSpan.Zero)
            {
                this.CommandLineWindowsServer += $" -ds {this.DelayTime.TotalSeconds} ";
            }

            this.CommandLineWindowsServer = this.CommandLineWindowsServer.Trim();
        }
    }
}
