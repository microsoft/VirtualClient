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
    /// NTttcp(Test Bandwith and Throughput) Tool Server Executor. 
    /// </summary>
    public class NTttcpServerExecutor : NTttcpExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NTttcpServerExecutor"/> class.
        /// </summary>
        /// <param name="component">Component to copy.</param>
        public NTttcpServerExecutor(VirtualClientComponent component)
           : base(component)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NTttcpServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NTttcpServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
            this.IntializeLinuxServerCommandline();
            this.IntializeWindowsServerCommandline();
        }

        private void IntializeWindowsServerCommandline()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            if (!this.CommandLineWindowsServer.Contains("-r") && !this.CommandLineWindowsServer.Contains("-s"))
            {
                this.CommandLineWindowsServer = this.CommandLineWindowsServer + " -r";
            }

            if (!this.CommandLineWindowsServer.Contains("-t") && this.TestDuration != null)
            {
                this.CommandLineWindowsServer = this.CommandLineWindowsServer + $" -t {this.TestDuration.TotalSeconds}";
            }

            if (!this.CommandLineWindowsServer.Contains("-l") && this.BufferSizeServer != null)
            {
                this.CommandLineWindowsServer = this.CommandLineWindowsServer + $" -l {this.BufferSizeServer}";
            }

            if (!this.CommandLineWindowsServer.Contains("-p"))
            {
                this.CommandLineWindowsServer = this.CommandLineWindowsServer + $" -p {this.Port}";
            }

            if (!this.CommandLineWindowsServer.Contains("-xml") && this.ResultsPath != null)
            {
                this.CommandLineWindowsServer = this.CommandLineWindowsServer + $" -xml {this.ResultsPath}";
            }

            if (!this.CommandLineWindowsServer.Contains("-u") && this.Protocol != null)
            {
                this.CommandLineWindowsServer = this.CommandLineWindowsServer + $"{(this.Protocol.ToLowerInvariant() == "udp" ? " -u" : string.Empty)} ";
            }

            if (!this.CommandLineWindowsServer.Contains("-ns") && this.NoSyncEnabled != null)
            {
                this.CommandLineWindowsServer = this.CommandLineWindowsServer + $"{(this.NoSyncEnabled == true ? " -ns" : string.Empty)} ";
            }

            if (!this.CommandLineWindowsServer.Contains("-m"))
            {
                this.CommandLineWindowsServer = this.CommandLineWindowsServer + $" -m {this.ThreadCount},*,{serverIPAddress} ";
            }
        }

        private void IntializeLinuxServerCommandline()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            string clientIPAddress = this.GetLayoutClientInstances(ClientRole.Client).First().IPAddress;

            if (!this.CommandLineLinuxClient.Contains("-r") && !this.CommandLineLinuxServer.Contains("-s"))
            {
                this.CommandLineLinuxServer = this.CommandLineLinuxServer + " -r";
            }

            if (!this.CommandLineLinuxServer.Contains("-t") && this.TestDuration != null)
            {
                this.CommandLineLinuxServer = this.CommandLineLinuxServer + $" -t {this.TestDuration.TotalSeconds}";
            }

            if (!this.CommandLineLinuxServer.Contains("-l") && this.BufferSizeClient != null)
            {
                this.CommandLineLinuxServer = this.CommandLineLinuxServer + $" -l {this.BufferSizeServer}";
            }

            if (!this.CommandLineWindowsServer.Contains("-p"))
            {
                this.CommandLineLinuxServer = this.CommandLineLinuxServer + $" -p {this.Port}";
            }

            if (!this.CommandLineLinuxServer.Contains("-xml") && this.ResultsPath != null)
            {
                this.CommandLineLinuxServer = this.CommandLineLinuxServer + $" -xml {this.ResultsPath}";
            }

            if (!this.CommandLineLinuxServer.Contains("-u") && this.Protocol != null)
            {
                this.CommandLineLinuxServer = this.CommandLineLinuxServer + $"{(this.Protocol.ToLowerInvariant() == "udp" ? " -u" : string.Empty)} ";
            }

            if (!this.CommandLineLinuxServer.Contains("-ns") && this.NoSyncEnabled != null)
            {
                this.CommandLineLinuxServer = this.CommandLineLinuxServer + $"{(this.NoSyncEnabled == true ? " -ns" : string.Empty)} ";
            }

            if (!this.CommandLineLinuxServer.Contains("-m"))
            {
                this.CommandLineLinuxServer = this.CommandLineLinuxServer + $" -m {this.ThreadCount},*,{serverIPAddress} ";
            }

            if (!this.CommandLineLinuxServer.Contains("-M"))
            {
                this.CommandLineLinuxServer = this.CommandLineLinuxServer + $"{((this.ReceiverMultiClientMode == true) ? " -M" : string.Empty)} ";
            }

            if (!this.CommandLineLinuxServer.Contains("-N") && this.NoSyncEnabled != null)
            {
                this.CommandLineLinuxServer = this.CommandLineLinuxServer + $"{(this.NoSyncEnabled == true ? " -N" : string.Empty)} ";
            }

            if (!this.CommandLineLinuxServer.Contains("--show-dev-interrupts") && this.DevInterruptsDifferentiator != null)
            {
                this.CommandLineLinuxServer = this.CommandLineLinuxServer + $"{((this.DevInterruptsDifferentiator != null) ? $" --show-dev-interrupts {this.DevInterruptsDifferentiator}" : string.Empty)}".Trim();
            }
        }
    }
}
