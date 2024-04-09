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
    /// AspNetBench Benchmark Client Executor.
    /// </summary>
    public class AspNetBenchClientExecutor : AspNetBenchBaseExecutor
    {
        private string bombardierFilePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetBenchClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        public AspNetBenchClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.PollingTimeout = TimeSpan.FromMinutes(40);
        }

        /// <summary>
        /// Executes  client side.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.WaitForRoleAsync(ClientRole.Server, telemetryContext, cancellationToken).ConfigureAwait(false);
            string serverIPAddress = this.GetLayoutClientInstances(ClientRole.Server).First().IPAddress;
            await this.RunBombardierAsync(serverIPAddress, telemetryContext, cancellationToken).ConfigureAwait(false);
            await this.TerminateRoleAsync(ClientRole.Server, telemetryContext, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Initializes the environment and dependencies for client of AspNetBench Benchmark workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            DependencyPath bombardierPackage = await this.GetPlatformSpecificPackageAsync(this.BombardierPackageName, cancellationToken)
                .ConfigureAwait(false);

            this.bombardierFilePath = this.Combine(bombardierPackage.Path, this.Platform == PlatformID.Unix ? "bombardier" : "bombardier.exe");
        }
    }
}
