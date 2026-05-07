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
    /// A dependency that sets the Windows SAN (Storage Area Network) policy to <c>OnlineAll</c>
    /// so that newly discovered JBOD disks are brought online and writable rather than being
    /// left offline or marked as read-only by the operating system.
    /// </summary>
    /// <remarks>
    /// On Windows, disks discovered through SAN controllers (including JBOD configurations)
    /// are sometimes left offline or marked read-only depending on the SAN policy in effect.
    /// Running the DiskPart commands <c>san</c> followed by <c>san policy=onlineall</c> configures
    /// Windows to automatically bring all newly discovered disks online and writable.
    /// This dependency is a no-op on Linux.
    /// </remarks>
    public class SetDiskSanPolicy : VirtualClientComponent
    {
        private ISystemManagement systemManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetDiskSanPolicy"/> class.
        /// </summary>
        public SetDiskSanPolicy(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// Executes the DiskPart SAN policy change. Only runs on Windows; skipped on Linux.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Win32NT)
            {
                await this.systemManagement.DiskManager.SetSanPolicyAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                this.Logger.LogTraceMessage(
                    $"{nameof(SetDiskSanPolicy)}: SAN policy is a Windows-only concept. Skipping on platform '{this.Platform}'.",
                    telemetryContext);
            }
        }
    }
}
