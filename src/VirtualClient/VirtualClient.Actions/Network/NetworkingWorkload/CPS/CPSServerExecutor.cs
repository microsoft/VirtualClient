// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Platform;
    using VirtualClient.Contracts;

    /// <summary>
    /// CPS(Connections Per Second) Tool Server Executor 
    /// </summary>
    [WindowsCompatible]
    [UnixCompatible]
    public class CPSServerExecutor : CPSExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CPSServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public CPSServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Returns the CPS client-side command line arguments.
        /// </summary>
        protected override string GetCommandLineArguments()
        {
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            return $"-s -r {this.Connections} {serverIPAddress},{this.Port} -i {this.DisplayInterval} -wt {this.WarmupTime} -t {this.TestDuration} " +
                $"{((this.DelayTime != 0) ? $"-ds {this.DelayTime}" : string.Empty)} ";
        }
    }
}
