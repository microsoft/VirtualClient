// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Latte workload executor.
    /// </summary>
    public class LatteExecutor : NetworkingWorkloadToolExecutor
    {
        private const string OutputFileName = "latte-results.xml";

        /// <summary>
        /// Initializes a new instance of the <see cref="SockPerfExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public LatteExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
            this.ProcessStartRetryPolicy = Policy.Handle<Exception>(exc => exc.Message.Contains("sockwiz_tcp_listener_open bind"))
                .WaitAndRetryAsync(5, retries => TimeSpan.FromSeconds(retries * 3));
        }

        /// <summary>
        /// The number of iterations for the network send/receive operations.
        /// </summary>
        public int Iterations
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Iterations), 100100);
            }
        }

        /// <summary>
        /// The starting port for the range of ports that will be used for client/server 
        /// network connections.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port), 6100);
            }
        }

        /// <summary>
        /// The type of the protocol that should be used for the workload. (e.g. TCP,UDP)
        /// </summary>
        public string Protocol
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Protocol));
            }
        }

        /// <summary>
        /// The number of times to poll before waiting on RIO CQ.
        /// </summary>
        public int RioPoll
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.RioPoll), 100000);
            }
        }

        /// <summary>
        /// The retry policy to apply to the startup of the Latte workload to handle
        /// transient issues.
        /// </summary>
        protected IAsyncPolicy ProcessStartRetryPolicy { get; set; }

        /// <summary>
        /// Produces powershell script parameters using the workload parameters provided.
        /// </summary>
        /// <returns>Powershell script parameters as a string.</returns>
        protected override string GetCommandLineArguments()
        {
            return string.Empty;
        }

        /// <summary>
        /// Initializes the environment and dependencies for running the tool.
        /// </summary>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string protocol = this.Protocol.ToLowerInvariant();
            if (protocol != "tcp" && protocol != "udp")
            {
                throw new NotSupportedException($"The network protocol '{this.Protocol}' is not supported for the Latte workload.");
            }

            if (string.IsNullOrWhiteSpace(this.Scenario))
            {
                throw new WorkloadException(
                    $"Scenario parameter missing. The profile supplied is missing the required '{nameof(this.Scenario)}' parameter " +
                    $"for one or more of the '{nameof(LatteExecutor)}' steps.",
                    ErrorReason.InvalidProfileDefinition);
            }

            DependencyPath workloadPackage = this.GetDependencyPath(this.PackageName, cancellationToken);

            this.IsInClientRole = this.IsInRole(ClientRole.Client);
            this.IsInServerRole = !this.IsInClientRole;
            this.Role = this.IsInClientRole ? ClientRole.Client : ClientRole.Server;

            // e.g.
            // Latte_TCP Client
            this.Name = $"{this.Scenario} {this.Role}";
            this.ProcessName = "latte";
            this.Tool = NetworkingWorkloadTool.Latte;
            this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "latte.exe");

            return Task.CompletedTask;
        }

        /// <summary>
        /// Execute on Windows platform only.
        /// </summary>
        protected override bool IsSupported()
        {
            return base.IsSupported() && this.Platform == PlatformID.Win32NT;
        }
    }
}
