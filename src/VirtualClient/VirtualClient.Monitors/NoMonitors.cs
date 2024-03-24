// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Does nothing. This is used to enable profiles to be created that have no
    /// monitoring requirements (e.g. avoiding the assignment of the MONITORS-DEFAULT.json
    /// profile).
    /// </summary>
    public class NoMonitors : VirtualClientComponent
    {
        /// <summary>
        /// Initilializes a new instance of the <see cref="NoMonitors"/> class.
        /// </summary>
        public NoMonitors(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
