// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The SPEC CPU 2026 workload executor.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class SpecCpu2026Executor : SpecCpuExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpecCpu2026Executor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SpecCpu2026Executor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecCpu2026Executor"/> class.
        /// </summary>
        /// <param name="component">The profile-facing SPEC CPU component.</param>
        internal SpecCpu2026Executor(SpecCpuExecutor component)
            : base(component)
        {
        }

        /// <inheritdoc/>
        protected override string GetResultsFileSearchPattern(string extension = null)
        {
            return string.IsNullOrWhiteSpace(extension)
                ? "CPU2026.*"
                : $"CPU2026.*.{extension}";
        }

        /// <inheritdoc/>
        protected override string GetSpecCpuVersion()
        {
            return "2026";
        }

        /// <inheritdoc/>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.ExecuteSpecCpuAsync(telemetryContext, cancellationToken);
        }

        /// <inheritdoc/>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.InitializeSpecCpuAsync(telemetryContext, cancellationToken);
        }
    }
}
