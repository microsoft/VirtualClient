// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides functionality to wait for given time..
    /// </summary>
    public class BufferTimeWaiter : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BufferTimeWaiter"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public BufferTimeWaiter(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        { 
        }

        /// <summary>
        /// The time in seconds to wait.
        /// </summary>
        public int BufferTimeInSec
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.BufferTimeInSec), 2);
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
            TimeSpan waitTime = TimeSpan.FromSeconds(this.BufferTimeInSec);
            await Task.Delay(waitTime, cancellationToken).ConfigureAwait();
            return;
        }
    }
}
