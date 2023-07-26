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
    /// Causes the system to reboot.
    /// </summary>
    public class RebootSystem : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RebootSystem"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public RebootSystem(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Parameter determines true/false whether the reboot option is enabled. Default = true.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.IsEnabled), true);
            }
        }

        /// <summary>
        /// Required parameter defines the reason for the reboot. This is used not only to
        /// describe the purpose of the reboot but as a distinct identifier for the case that
        /// multiple reboots are used in a profile (for tracking state).
        /// </summary>
        public string Reason
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Reason));
            }
        }

        /// <summary>
        /// Parameter returns a period of time to wait before executing the reboot request.
        /// </summary>
        public TimeSpan WaitTime
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.WaitTime), TimeSpan.Zero);
            }
        }

        /// <summary>
        /// Execute the command(s) logic on the system.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext("rebootReason", this.Reason)
                .AddContext("waitTime", this.WaitTime)
                .AddContext("isEnabled", this.IsEnabled);

            if (!cancellationToken.IsCancellationRequested)
            {
                if (this.IsEnabled)
                {
                    IStateManager stateManager = this.Dependencies.GetService<IStateManager>();
                    RebootState state = await stateManager.GetStateAsync<RebootState>($"{nameof(RebootState)}_{this.Reason.RemoveWhitespace()}", cancellationToken);

                    // If the state exists, it means that we have already rebooted on a previous
                    // round.
                    if (state == null)
                    {
                        await Task.Delay(this.WaitTime, cancellationToken);
                        this.RequestReboot();
                    }
                }
            }
        }

        private class RebootState
        {
        }
    }
}
