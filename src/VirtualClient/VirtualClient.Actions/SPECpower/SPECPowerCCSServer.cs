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
    /// Manages the SPEC Power "CCS" server. (Control and Collect System)
    /// </summary>
    [UnixCompatible]
    [WindowsCompatible]
    public class SPECPowerCCSServer : SPECPowerProcess
    {
        private const string WindowsClassPath = "./ccs.jar;./check.jar;../ssj/ssj.jar;../ssj/lib/jfreechart-1.0.13.jar;../ssj/lib/jcommon-1.0.16.jar";
        private const string UnixClassPath = "./ccs.jar:./check.jar:../ssj/ssj.jar:../ssj/lib/jfreechart-1.0.13.jar:../ssj/lib/jcommon-1.0.16.jar";
        private const string CCSFolder = "ccs";

        /// <summary>
        /// Initializes a new instance of the <see cref="SPECPowerCCSServer"/> class.
        /// </summary>
        public SPECPowerCCSServer(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// The CCSArguments parameter value as defined in the profile.
        /// </summary>
        public string CCSArguments
        {
            get
            {
                return this.ActionOwner.Parameters.GetValue<string>(nameof(SPECPowerCCSServer.CCSArguments));
            }

            set
            {
                this.ActionOwner.Parameters[nameof(SPECPowerCCSServer.CCSArguments)] = value;
            }
        }

        /// <summary>
        /// The name of the benchmark/test which is currently running.
        /// </summary>
        public string CurrentTestName { get; private set; }

        /// <summary>
        /// The time when the current benchmark/test started.
        /// </summary>
        public DateTime LastTestStartTime { get; private set; }

        /// <inheritdoc/>
        public override void Cleanup()
        {
            this.SPECProcesses?.ForEach(p => p.Kill());
            this.SPECProcesses.Clear();
            this.CurrentTestName = null;
            this.LastTestStartTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Starts the CCS server process.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.MetadataContract.AddForScenario("SPECpower", null);
            this.MetadataContract.Apply(telemetryContext);

            this.Cleanup();

            string classPath = this.Platform == PlatformID.Win32NT ? WindowsClassPath : UnixClassPath;
            string workloadDirectoryPath = this.PlatformSpecifics.Combine(this.ActionOwner.WorkloadPackagePath, CCSFolder);

            this.StartSPECProcessAsync(
                "SPEC Power CCS-Server",
                workloadDirectoryPath,
                classPath,
                this.CCSArguments,
                cancellationToken);

            this.Logger.LogTraceMessage("Started the SPECPower Server (CCS).");
            return Task.CompletedTask;
        }
    }
}