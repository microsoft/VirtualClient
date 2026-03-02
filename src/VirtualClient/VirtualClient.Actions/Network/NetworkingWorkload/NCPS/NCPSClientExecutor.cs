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
    /// NCPS(New Connections Per Second) Tool Client Executor. 
    /// </summary>
    public class NCPSClientExecutor : NCPSExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NCPSClientExecutor"/> class.
        /// </summary>
        /// <param name="component">Component to copy.</param>
        public NCPSClientExecutor(VirtualClientComponent component)
           : base(component)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NCPSClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NCPSClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Returns the NCPS client-side command line arguments.
        /// </summary>
        protected override string GetCommandLineArguments()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;

            // Build the command line based on NCPS recipe
            // ncps -c <serverIP> -wt 30 -t 330
            string command = $"-c {serverIPAddress} " +
                $"-r {this.ThreadCount} " +
                $"-bp {this.Port} " +
                $"-np {this.PortCount} " +
                $"-N {this.TotalConnectionsToOpen} " +
                $"-P {this.MaxPendingRequests} " +
                $"-D {this.ConnectionDuration} " +
                $"-M {this.DataTransferMode} " +
                $"-i {this.DisplayInterval} " +
                $"-wt {this.WarmupTime.TotalSeconds} " +
                $"-t {this.TestDuration.TotalSeconds} ";

            if (this.DelayTime != TimeSpan.Zero)
            {
                command += $"-ds {this.DelayTime.TotalSeconds} ";
            }

            if (!string.IsNullOrWhiteSpace(this.AdditionalParams))
            {
                command += this.AdditionalParams;
            }

            return command.Trim();
        }
    }
}
