// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides functionality to wait for given time.
    /// </summary>
    public class WaitExecutor : VirtualClientComponent
    {
        private static readonly TimeSpan DefaultTimeInterval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Initializes a new instance of the <see cref="WaitExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public WaitExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        { 
        }

        /// <summary>
        /// The performance snapshot interval defined in the profile arguments.
        /// </summary>
        public TimeSpan TimeInterval
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(WaitExecutor.TimeInterval), WaitExecutor.DefaultTimeInterval);
            }
        }

        /// <summary>
        /// Executes Wait operation step.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await Task.Delay(this.TimeInterval, cancellationToken).ConfigureAwait();
            return;
        }
    }
}
