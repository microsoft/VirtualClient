// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Platform;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using global::VirtualClient.Contracts.Metadata;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Manages the SPEC Power "Director" program.
    /// </summary>
    public class SPECPowerDirector : SPECPowerProcess
    {
        private const string SSJFolder = "ssj";
        private const string WindowsClassPath = "ssj.jar;check.jar;lib/jcommon-1.0.16.jar;lib/jfreechart-1.0.13.jar";
        private const string UnixClassPath = "ssj.jar:check.jar:lib/jcommon-1.0.16.jar:lib/jfreechart-1.0.13.jar";

        /// <summary>
        /// Initializes a new instance of the <see cref="SPECPowerDirector"/> class.
        /// </summary>
        public SPECPowerDirector(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// The DirectorArguments parameter value as defined in the profile.
        /// </summary>
        public string DirectorArguments
        {
            get
            {
                return this.ActionOwner.Parameters.GetValue<string>(nameof(SPECPowerDirector.DirectorArguments));
            }

            set
            {
                this.ActionOwner.Parameters[nameof(SPECPowerDirector.DirectorArguments)] = value;
            }
        }

        /// <inheritdoc/>
        public override void Cleanup()
        {
            this.SPECProcesses?.ForEach(process => process.Kill());
            this.SPECProcesses.Clear();
        }

        /// <summary>
        /// Starts the director program.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.MetadataContract.AddForScenario("SPECpower", null);
            this.MetadataContract.Apply(telemetryContext);

            string classPath = this.Platform == PlatformID.Win32NT ? WindowsClassPath : UnixClassPath;
            string workloadDirectoryPath = this.PlatformSpecifics.Combine(this.ActionOwner.WorkloadPackagePath, SSJFolder);

            this.StartSPECProcessAsync(
                "SPEC Power Director",
                workloadDirectoryPath,
                classPath,
                this.DirectorArguments,
                cancellationToken);

            this.Logger.LogTraceMessage("Started SPECPower Director.");

            return Task.CompletedTask;
        }
    }
}