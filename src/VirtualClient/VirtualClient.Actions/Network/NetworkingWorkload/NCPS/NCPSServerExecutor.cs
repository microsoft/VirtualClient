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
    /// NCPS(New Connections Per Second) Tool Server Executor 
    /// </summary>
    public class NCPSServerExecutor : NCPSExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NCPSServerExecutor"/> class.
        /// </summary>
        /// <param name="component">Component to copy.</param>
        public NCPSServerExecutor(VirtualClientComponent component)
           : base(component)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NCPSServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NCPSServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Returns the NCPS server-side command line arguments.
        /// </summary>
        protected override string GetCommandLineArguments()
        {
            // Build the command line based on NCPS recipe
            string command = $"-s " +
                $"-r {this.ThreadCount} " +
                $"-bp {this.Port} " +
                $"-np {this.PortCount} " +
                $"-i {this.DisplayInterval} -wt {this.WarmupTime.TotalSeconds} -t {this.TestDuration.TotalSeconds} " +
                $"{((this.DelayTime != TimeSpan.Zero) ? $"-ds {this.DelayTime.TotalSeconds}" : string.Empty)}";

            if (!string.IsNullOrWhiteSpace(this.DataTransferMode) && 
                (this.DataTransferMode.Equals("s", StringComparison.OrdinalIgnoreCase) || 
                 this.DataTransferMode.Equals("r", StringComparison.OrdinalIgnoreCase)))
            {
                command += $"-M {this.DataTransferMode} ";
            }

            if (!string.IsNullOrWhiteSpace(this.AdditionalParams))
            {
                command += this.AdditionalParams;
            }

            return command.Trim();
        }
    }
}
