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
    /// SockPerf workload Executor. 
    /// </summary>
    public class SockPerfExecutor : NetworkingWorkloadToolExecutor
    {
        private const string OutputFileName = "sockperf-results.txt";

        /// <summary>
        /// Initializes a new instance of the <see cref="SockPerfExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public SockPerfExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
           : base(dependencies, parameters)
        {
            this.ProcessStartRetryPolicy = Policy.Handle<Exception>(exc => exc.Message.Contains("sockwiz_tcp_listener_open bind"))
               .WaitAndRetryAsync(5, retries => TimeSpan.FromSeconds(retries * 3));

            this.Parameters.SetIfNotDefined(nameof(this.Port), 6100);
            this.Parameters.SetIfNotDefined(nameof(this.MessagesPerSecond), "max");
            this.Parameters.SetIfNotDefined(nameof(this.ConfidenceLevel), 99);
        }

        /// <summary>
        /// Get buffer size value in bytes.
        /// </summary>
        public int MessageSize
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.MessageSize));
            }
        }

        /// <summary>
        /// Parameter defines the Duration (in seconds) for running the SockPerf workload.
        /// </summary>
        public int TestDuration
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.TestDuration), 60);
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
        /// get test mode value (ping-pong or under-load)
        /// </summary>
        public string TestMode
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.TestMode)).ToString();
            }
        }

        /// <summary>
        /// get messages-per-second value
        /// </summary>
        public string MessagesPerSecond
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.MessagesPerSecond), "max").ToString();
            }
        }

        /// <summary>
        /// gets the confidence level used for calculating the confidence intervals.
        /// </summary>
        public double ConfidenceLevel
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.ConfidenceLevel), 99);
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
                throw new NotSupportedException($"The network protocol '{this.Protocol}' is not supported for the SockPerf workload.");
            }

            if (string.IsNullOrWhiteSpace(this.Scenario))
            {
                throw new WorkloadException(
                    $"Scenario parameter missing. The profile supplied is missing the required '{nameof(this.Scenario)}' parameter " +
                    $"for one or more of the '{nameof(SockPerfExecutor)}' steps.",
                    ErrorReason.InvalidProfileDefinition);
            }

            DependencyPath workloadPackage = this.GetDependencyPath(this.PackageName, cancellationToken);

            this.IsInClientRole = this.IsInRole(ClientRole.Client);
            this.IsInServerRole = !this.IsInClientRole;
            this.Role = this.IsInClientRole ? ClientRole.Client : ClientRole.Server;

            // e.g.
            // SockPerf_TCP_Ping_Pong Client, SockPerf_TCP_Ping_Pong Server
            this.Name = $"{this.Scenario} {this.Role}";
            this.ProcessName = "sockperf";
            this.Tool = NetworkingWorkloadTool.SockPerf;
            this.ResultsPath = this.PlatformSpecifics.Combine(workloadPackage.Path, SockPerfExecutor.OutputFileName);
            this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "sockperf");

            return this.SystemManagement.MakeFileExecutableAsync(this.ExecutablePath, this.Platform, cancellationToken);
        }

        /// <summary>
        /// Execute on Unix/Linux platform only.
        /// </summary>
        protected override bool IsSupported()
        {
            return base.IsSupported() && this.Platform == PlatformID.Unix;
        }
    }
}
