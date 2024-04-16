// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient;
    using VirtualClient.Actions.Memtier;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Redis Benchmark Client Executor.
    /// </summary>
    public class AspNetBenchServerExecutor : AspNetBenchBaseExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetBenchServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public AspNetBenchServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="telemetryContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.BuildAspNetBenchAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            await this.StartAspNetServerAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            await this.WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task<ServerState> GetServerStateAsync(IApiClient serverApiClient, CancellationToken cancellationToken)
        {
            Item<ServerState> state = await serverApiClient.GetStateAsync<ServerState>(
                nameof(ServerState),
                cancellationToken);

            if (state == null)
            {
                throw new WorkloadException(
                    $"Expected server state information missing. The server did not return state indicating the details for the Memcached server(s) running.",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            return state.Definition;
        }
    }
}
