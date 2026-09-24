// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The SPEC CPU 2017 workload executor.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class SpecCpu2017Executor : SpecCpuExecutor
    {
        /// <summary>
        /// Constructor for <see cref="SpecCpu2017Executor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SpecCpu2017Executor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Constructor for <see cref="SpecCpu2017Executor"/>.
        /// </summary>
        /// <param name="component">The profile-facing SPEC CPU component.</param>
        internal SpecCpu2017Executor(SpecCpuExecutor component)
            : base(component)
        {
        }

        /// <inheritdoc/>
        protected override string GetResultsFileSearchPattern(string extension = null)
        {
            return string.IsNullOrWhiteSpace(extension)
                ? "CPU2017.*"
                : $"CPU2017.*.{extension}";
        }

        /// <inheritdoc/>
        protected override string GetSpecCpuVersion()
        {
            return "2017";
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

        /// <inheritdoc/>
        protected override async Task<string> CustomizeConfigurationAsync(string templateText, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                string compilerVersion = await this.GetInstalledCompilerDumpVersionAsync("gcc", cancellationToken);

                if (string.IsNullOrEmpty(compilerVersion))
                {
                    throw new WorkloadException("gcc version not found.");
                }

                templateText = templateText.Replace(
                    SpecCpu2017ConfigPlaceholder.Gcc10Workaround,
                    Convert.ToInt32(compilerVersion) >= 10 ? SpecCpu2017ConfigPlaceholder.Gcc10WorkaroundContent : string.Empty,
                    StringComparison.OrdinalIgnoreCase);

                templateText = templateText.Replace(
                    SpecCpu2017ConfigPlaceholder.Gcc15Workaround,
                    Convert.ToInt32(compilerVersion) >= 15 ? SpecCpu2017ConfigPlaceholder.Gcc15WorkaroundContent : string.Empty,
                    StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                templateText = templateText.Replace(
                    SpecCpu2017ConfigPlaceholder.Gcc10Workaround,
                    SpecCpu2017ConfigPlaceholder.Gcc10WorkaroundContent,
                    StringComparison.OrdinalIgnoreCase);

                templateText = templateText.Replace(
                    SpecCpu2017ConfigPlaceholder.Gcc15Workaround,
                    SpecCpu2017ConfigPlaceholder.Gcc15WorkaroundContent,
                    StringComparison.OrdinalIgnoreCase);
            }

            return templateText;
        }

        private static class SpecCpu2017ConfigPlaceholder
        {
            public const string Gcc10Workaround = "$Gcc10Workaround$";
            public const string Gcc10WorkaroundContent = "%define GCCge10";
            public const string Gcc15Workaround = "$Gcc15Workaround$";
            public const string Gcc15WorkaroundContent = "%define GCCge15";
        }
    }
}
