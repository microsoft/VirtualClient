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
    /// CPS(Connections Per Second) Tool Client Executor. 
    /// </summary>
    [WindowsCompatible]
    [UnixCompatible]
    public class CPSClientExecutor : CPSExecutor
    {
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
                $"-i {this.DisplayInterval} -wt {this.WarmupTime} -t {this.TestDuration} " +
                $"{((this.DelayTime != 0) ? $"-ds {this.DelayTime}" : string.Empty)}".Trim();
        }
    }
}
