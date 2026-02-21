// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Contracts;

    /// <summary>
    /// NTttcp(Test Bandwith and Throughput) Tool Client Executor. 
    /// </summary>
    public class NTttcpClientExecutor : NTttcpExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NTttcpClientExecutor"/> class.
        /// </summary>
        /// <param name="component">Component to copy.</param>
        public NTttcpClientExecutor(VirtualClientComponent component)
           : base(component)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NTttcpClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NTttcpClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
            this.IntializeLinuxClientCommandline();
            this.IntializeWindowsClientCommandline();
        }

        private void IntializeWindowsClientCommandline()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;

            if (!this.CommandLineWindowsClient.Contains("-r") && !this.CommandLineWindowsClient.Contains("-s"))
            {
                this.CommandLineWindowsClient = this.CommandLineWindowsClient + " -s";
            }

            if (!this.CommandLineWindowsClient.Contains("-t") && this.TestDuration != null)
            {
                this.CommandLineWindowsClient = this.CommandLineWindowsClient + $" -t {this.TestDuration.TotalSeconds}";
            }

            if (!this.CommandLineWindowsClient.Contains("-l") && this.BufferSizeServer != null)
            {
                this.CommandLineWindowsClient = this.CommandLineWindowsClient + $" -l {this.BufferSizeClient}";
            }

            if (!this.CommandLineWindowsClient.Contains("-p"))
            {
                this.CommandLineWindowsClient = this.CommandLineWindowsClient + $" -p {this.Port}";
            }

            if (!this.CommandLineWindowsClient.Contains("-xml") && this.ResultsPath != null)
            {
                this.CommandLineWindowsClient = this.CommandLineWindowsClient + $" -xml {this.ResultsPath}";
            }

            if (!this.CommandLineWindowsClient.Contains("-u") && this.Protocol != null)
            {
                this.CommandLineWindowsClient = this.CommandLineWindowsClient + $"{(this.Protocol.ToLowerInvariant() == "udp" ? " -u" : string.Empty)} ";
            }

            if (!this.CommandLineWindowsClient.Contains("-ns") && this.NoSyncEnabled != null)
            {
                this.CommandLineWindowsClient = this.CommandLineWindowsClient + $"{(this.NoSyncEnabled == true ? " -ns" : string.Empty)} ";
            }

            if (!this.CommandLineWindowsClient.Contains("-m"))
            {
                this.CommandLineWindowsClient = this.CommandLineWindowsClient + $" -m {this.ThreadCount},*,{serverIPAddress} ";
            }

            if (!this.CommandLineWindowsClient.Contains("-nic"))
            {
                this.CommandLineWindowsClient = this.CommandLineWindowsClient + $" -nic {clientIPAddress}";
            }
        }

        private void IntializeLinuxClientCommandline()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;

            if (!this.CommandLineLinuxClient.Contains("-r") && !this.CommandLineLinuxClient.Contains("-s"))
            {
                this.CommandLineLinuxClient = this.CommandLineLinuxClient + " -s";
            }

            if (!this.CommandLineLinuxClient.Contains("-t") && this.TestDuration != null)
            {
                this.CommandLineLinuxClient = this.CommandLineLinuxClient + $" -t {this.TestDuration.TotalSeconds}";
            }

            if (!this.CommandLineLinuxClient.Contains("-l") && this.BufferSizeClient != null)
            {
                this.CommandLineLinuxClient = this.CommandLineLinuxClient + $" -l {this.BufferSizeClient}";
            }

            if (!this.CommandLineLinuxClient.Contains("-p"))
            {
                this.CommandLineLinuxClient = this.CommandLineLinuxClient + $" -p {this.Port}";
            }

            if (!this.CommandLineLinuxClient.Contains("-xml") && this.ResultsPath != null)
            {
                this.CommandLineLinuxClient = this.CommandLineLinuxClient + $" -xml {this.ResultsPath}";
            }

            if (!this.CommandLineLinuxClient.Contains("-u") && this.Protocol != null)
            {
                this.CommandLineLinuxClient = this.CommandLineLinuxClient + $"{(this.Protocol.ToLowerInvariant() == "udp" ? " -u" : string.Empty)} ";
            }

            if (!this.CommandLineLinuxClient.Contains("-ns") && this.NoSyncEnabled != null)
            {
                this.CommandLineLinuxClient = this.CommandLineLinuxClient + $"{(this.NoSyncEnabled == true ? " -ns" : string.Empty)} ";
            }

            if (!this.CommandLineLinuxClient.Contains("-m"))
            {
                this.CommandLineLinuxClient = this.CommandLineLinuxClient + $" -m {this.ThreadCount},*,{serverIPAddress} ";
            }

            if (!this.CommandLineLinuxClient.Contains("-L"))
            {
                this.CommandLineLinuxClient = this.CommandLineLinuxClient + $"{(this.SenderLastClient == true ? " -L" : string.Empty)} ";
            }

            if (!this.CommandLineLinuxClient.Contains("-n"))
            {
                this.CommandLineLinuxClient = this.CommandLineLinuxClient + $"{((this.ThreadsPerServerPort != null) ? $" -n {this.ThreadsPerServerPort}" : string.Empty)} ";
            }

            if (!this.CommandLineLinuxClient.Contains("-l"))
            {
                this.CommandLineLinuxClient = this.CommandLineLinuxClient + $"{((this.ConnectionsPerThread != null) ? $" -l {this.ConnectionsPerThread}" : string.Empty)} ";
            }

            if (!this.CommandLineLinuxClient.Contains("-N") && this.NoSyncEnabled != null)
            {
                this.CommandLineLinuxClient = this.CommandLineLinuxClient + $"{(this.NoSyncEnabled == true ? " -N" : string.Empty)} ";
            }

            if (!this.CommandLineLinuxClient.Contains("--show-dev-interrupts") && this.DevInterruptsDifferentiator != null)
            {
                this.CommandLineLinuxClient = this.CommandLineLinuxClient + $"{((this.DevInterruptsDifferentiator != null) ? $" --show-dev-interrupts {this.DevInterruptsDifferentiator}" : string.Empty)}".Trim();
            }
        }
    }
}
