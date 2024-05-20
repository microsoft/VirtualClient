// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Platform;

    /// <summary>
    /// NTttcp(Test Bandwith and Throughput) Tool Client Executor. 
    /// </summary>
    [WindowsCompatible]
    [UnixCompatible]
    public class NTttcpClientExecutor : NTttcpExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NTttcpClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NTttcpClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
        }
    }
}
