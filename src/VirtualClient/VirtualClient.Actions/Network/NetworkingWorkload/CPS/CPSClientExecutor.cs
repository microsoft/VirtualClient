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
    /// CPS(Connections Per Second) Tool Client Executor. 
    /// </summary>
    public class CPSClientExecutor : CPSExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CPSClientExecutor"/> class.
        /// </summary>
        /// <param name="component">Component to copy.</param>
        public CPSClientExecutor(VirtualClientComponent component)
           : base(component)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CPSClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public CPSClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Returns the CPS client-side command line arguments.
        /// </summary>
        protected override string GetCommandLineArguments()
        {
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;

            return $"-c -r {this.Connections} " +
                $"{clientIPAddress},0,{serverIPAddress},{this.Port},{this.ConnectionsPerThread},{this.MaxPendingRequestsPerThread},{this.ConnectionDuration},{this.DataTransferMode} " +
                $"-i {this.DisplayInterval} -wt {this.WarmupTime.TotalSeconds} -t {this.TestDuration.TotalSeconds} " +
                $"{((this.DelayTime != TimeSpan.Zero) ? $"-ds {this.DelayTime.TotalSeconds}" : string.Empty)} " +
                $"{this.AdditionalParams}".Trim();
        }
        
        private void InitializeWindowsClientCommandline()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;

            // Ensure base string isn't null.
            this.CommandLineWindowsClient ??= string.Empty;

            // Normalize: keep a trailing space so appends don't glue together.
            if (this.CommandLineWindowsClient.Length > 0 && !char.IsWhiteSpace(this.CommandLineWindowsClient[^1]))
            {
                this.CommandLineWindowsClient += " ";
            }

            // -c (client mode)
            if (!this.CommandLineWindowsClient.Contains("-c", StringComparison.OrdinalIgnoreCase))
            {
                this.CommandLineWindowsClient += "-c ";
            }

            // -r {Connections}
            // Your reference includes "-c -r {Connections}"
            if (!this.CommandLineWindowsClient.Contains("-r", StringComparison.OrdinalIgnoreCase) && this.Connections != null)
            {
                this.CommandLineWindowsClient += $"-r {this.Connections} ";
            }

            // Endpoint tuple:
            // {clientIPAddress},0,{serverIPAddress},{Port},{ConnectionsPerThread},{MaxPendingRequestsPerThread},{ConnectionDuration},{DataTransferMode}
            // Add it only if we don't already see the server IP (good heuristic to avoid duplication).
            if (!this.CommandLineWindowsClient.Contains(serverIPAddress, StringComparison.OrdinalIgnoreCase))
            {
                this.CommandLineWindowsClient +=
                    $"{clientIPAddress},0,{serverIPAddress},{this.Port},{this.ConnectionsPerThread},{this.MaxPendingRequestsPerThread},{this.ConnectionDuration},{this.DataTransferMode} ";
            }

            // -i {DisplayInterval}
            if (!this.CommandLineWindowsClient.Contains("-i", StringComparison.OrdinalIgnoreCase) && this.DisplayInterval != null)
            {
                this.CommandLineWindowsClient += $"-i {this.DisplayInterval} ";
            }

            // -wt {WarmupTime.TotalSeconds}
            if (!this.CommandLineWindowsClient.Contains("-wt", StringComparison.OrdinalIgnoreCase) && this.WarmupTime != null)
            {
                this.CommandLineWindowsClient += $"-wt {this.WarmupTime.TotalSeconds} ";
            }

            // -t {TestDuration.TotalSeconds}
            if (!this.CommandLineWindowsClient.Contains("-t", StringComparison.OrdinalIgnoreCase) && this.TestDuration != null)
            {
                this.CommandLineWindowsClient += $"-t {this.TestDuration.TotalSeconds} ";
            }

            // Optional: -ds {DelayTime.TotalSeconds} only if DelayTime != 0
            if (!this.CommandLineWindowsClient.Contains("-ds", StringComparison.OrdinalIgnoreCase) &&
                this.DelayTime != TimeSpan.Zero)
            {
                this.CommandLineWindowsClient += $"-ds {this.DelayTime.TotalSeconds} ";
            }

            // Additional params (append once)
            if (!string.IsNullOrWhiteSpace(this.AdditionalParams))
            {
                // Optional: prevent double-appending if already present.
                // You can remove this block if AdditionalParams is expected to be dynamic.
                if (!this.CommandLineWindowsClient.Contains(this.AdditionalParams, StringComparison.OrdinalIgnoreCase))
                {
                    this.CommandLineWindowsClient += $"{this.AdditionalParams} ";
                }
            }

            this.CommandLineWindowsClient = this.CommandLineWindowsClient.Trim();
        }

    }
}
