// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// Class encapsulating logic to execute and collect metrics for
    /// NASA Advanced Supercomputing parallel benchmarks. (NAS parallel benchmarks)
    /// For Server side.
    /// </summary>
    public class NASParallelBenchServerExecutor : NASParallelBenchExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NASParallelBenchServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public NASParallelBenchServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Executes NAS parallel benchmark server side.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // No commands run on Server.
            // "mpiexec" uses binary on Server side from client using MPI and SSH.
            // Nothing required to be run on Server side.
            return Task.CompletedTask;
        }
    }
}
