// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Profile executes a simple network ping test.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class NetworkPingExecutor : VirtualClientComponent
    {
        private static readonly int BlipDurationMilliseconds = 1000;
        private static readonly IPGlobalProperties IPProperties = IPGlobalProperties.GetIPGlobalProperties();

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkPingExecutor"/> class.
        /// </summary>
        public NetworkPingExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Parameter. Defines the target IP address or host name to which to send ICMP ping requests.
        /// </summary>
        public string IPAddress
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(NetworkPingExecutor.IPAddress));
            }
        }

        /// <summary>
        /// Parameter. Defines the number of individual network pings that will be conducted to determine
        /// a rollup average on each round of profile action execution.
        /// </summary>
        public int PingIterations
        {
            get
            {
                const int defaultIterations = 50;
                return this.Parameters.GetValue<int>(nameof(NetworkPingExecutor.PingIterations), defaultIterations);
            }
        }

        /// <summary>
        /// Parameter. Defines the duration for which the network ping test will run.
        /// This can be a valid timespan (e.g. 00:10:00) or a simple numeric value representing total seconds (e.g. 600).
        /// </summary>
        public TimeSpan? Duration
        {
            get
            {
                // Check if the parameter exists and is not empty
                if (this.Parameters.ContainsKey(nameof(NetworkPingExecutor.Duration)))
                {
                    string durationValue = this.Parameters[nameof(NetworkPingExecutor.Duration)]?.ToString();
                    if (!string.IsNullOrWhiteSpace(durationValue))
                    {
                        return this.Parameters.GetTimeSpanValue(nameof(NetworkPingExecutor.Duration));
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// The retry policy to apply to ping operations for handling transient
        /// issues/errors.
        /// </summary>
        public IAsyncPolicy PingRetryPolicy { get; set; } = Policy
            .Handle<WorkloadException>(exc => exc.Reason == ErrorReason.NetworkTargetDoesNotExist)
            .WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries * 2));

        /// <summary>
        /// The retry policy to apply to connection count/tracking operations for handling transient
        /// issues/errors.
        /// </summary>
        public ISyncPolicy ConnectionCaptureRetryPolicy { get; set; } = Policy
            .Handle<NetworkInformationException>()
            .WaitAndRetry(5, (retries) => TimeSpan.FromMilliseconds(retries * 10));

        /// <summary>
        /// Executes the network ping test.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(this.IPAddress) || this.IPAddress == "NotDefined")
            {
                throw new WorkloadException(
                    $"Required instructions missing. This profile requires the IP address or host name of the server that will be pinged to be provided on the command line. " +
                    $"Include the target server in the parameters (e.g. --parameters:{nameof(this.IPAddress)}=1.2.3.4 or --parameters:{nameof(this.IPAddress)}=example.com).",
                    ErrorReason.InstructionsNotProvided);
            }

            if (!System.Net.IPAddress.TryParse(this.IPAddress, out IPAddress ipAddress))
            {
                if (Uri.CheckHostName(this.IPAddress) != UriHostNameType.Dns
                    || this.IPAddress.All(character => char.IsDigit(character) || character == '.'))
                {
                    throw new WorkloadException(
                        $"Invalid target format. The address provided '{this.IPAddress}' is not a valid IPv4 address, IPv6 address or DNS host name.",
                        ErrorReason.InstructionsNotValid);
                }

                try
                {
                    IPAddress[] resolvedAddresses = await this.ResolveHostAddressesAsync(this.IPAddress, cancellationToken).ConfigureAwait(false);
                    ipAddress = resolvedAddresses.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork)
                        ?? resolvedAddresses.FirstOrDefault();
                }
                catch (SocketException exc)
                {
                    throw new WorkloadException(
                        $"The target host name '{this.IPAddress}' could not be resolved to an IP address.",
                        exc,
                        ErrorReason.NetworkTargetDoesNotExist);
                }

                if (ipAddress == null)
                {
                    throw new WorkloadException(
                        $"The target host name '{this.IPAddress}' did not resolve to an IPv4 or IPv6 address.",
                        ErrorReason.NetworkTargetDoesNotExist);
                }

                telemetryContext.AddContext("hostName", this.IPAddress);
                telemetryContext.AddContext("resolvedIPAddress", ipAddress.ToString());
            }

            await this.ExecutePingServerAsync(ipAddress, telemetryContext, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Resolves a DNS host name to its IP addresses.
        /// </summary>
        /// <param name="hostName">The DNS host name to resolve.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>The IP addresses resolved for the host name.</returns>
        protected virtual Task<IPAddress[]> ResolveHostAddressesAsync(string hostName, CancellationToken cancellationToken)
        {
            return Dns.GetHostAddressesAsync(hostName, cancellationToken);
        }

        /// <summary>
        /// Sends a single ICMP ping to the target and returns the resulting status and round trip time.
        /// Provided as an overridable seam so the ping loop can be unit-tested deterministically without
        /// requiring live network access.
        /// </summary>
        protected virtual async Task<PingResult> SendPingAsync(Ping networkPing, IPAddress ipAddress, int timeoutMilliseconds)
        {
            PingReply reply = await networkPing.SendPingAsync(ipAddress, timeoutMilliseconds).ConfigureAwait(false);
            return new PingResult(reply.Status, reply.RoundtripTime);
        }

        private async Task ExecutePingServerAsync(IPAddress ipAddress, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (Ping networkPing = new Ping())
            {
                int iterations = 0;
                int timedOutPings = 0;

                ConcurrentBag<long> responseTimes = new ConcurrentBag<long>();
                ConcurrentBag<NetworkBlip> networkBlips = new ConcurrentBag<NetworkBlip>();
                ConcurrentBag<int> networkConnections = new ConcurrentBag<int>();

                DateTime startTime = DateTime.UtcNow;
                Stopwatch blipTimer = Stopwatch.StartNew();

                // Use Duration if specified, otherwise use PingIterations
                DateTime stopTime = startTime.Add(this.Duration ?? TimeSpan.Zero);

                while (!cancellationToken.IsCancellationRequested)
                {
                    if ((this.Duration != null && DateTime.UtcNow >= stopTime) ||
                        (this.Duration == null && iterations >= this.PingIterations))
                    {
                        break;
                    }

                    try
                    {
                        await this.PingRetryPolicy.ExecuteAsync(async () =>
                        {
                            PingResult reply = await this.SendPingAsync(networkPing, ipAddress, NetworkPingExecutor.BlipDurationMilliseconds).ConfigureAwait(false);

                            switch (reply.Status)
                            {
                                case IPStatus.Success:
                                    blipTimer.Stop();
                                    long duration = blipTimer.ElapsedMilliseconds;
                                    if (NetworkPingExecutor.HasBlipOccurred(timedOutPings, duration))
                                    {
                                        networkBlips.Add(new NetworkBlip(timedOutPings, duration));
                                        timedOutPings = 0;
                                    }

                                    responseTimes.Add(reply.RoundtripTime);
                                    this.AddCurrentNetworkConnectionCount(networkConnections, telemetryContext);

                                    await Task.Delay(TimeSpan.FromMilliseconds(200)).ConfigureAwait(false);
                                    blipTimer.Restart();

                                    break;

                                case IPStatus.TimedOut:
                                case IPStatus.TimeExceeded:
                                case IPStatus.TtlExpired:
                                    // Note: unlike a successful ping, we intentionally do NOT decrement the
                                    // iteration counter here. Every ping attempt (successful or not) must advance
                                    // the loop so that a target that never replies cannot spin the loop until the
                                    // overall profile timeout (~hours). The loop is bounded by total attempts
                                    // (PingIterations) or the configured Duration.
                                    timedOutPings++;
                                    break;

                                default:
                                    throw new WorkloadException(
                                        $"The target endpoint for IP address '{ipAddress}' does not exist or does not have ICMP ports/replies enabled.",
                                        ErrorReason.NetworkTargetDoesNotExist);
                            }
                        }).ConfigureAwait(false);
                    }
                    catch (Exception exc)
                    {
                        this.Logger.LogErrorMessage(exc, telemetryContext);
                        throw;
                    }
                    finally
                    {
                        iterations++;
                    }
                }

                DateTime endTime = DateTime.UtcNow;

                if (!cancellationToken.IsCancellationRequested)
                {
                    // If at least one ping attempt was made but the target never responded successfully,
                    // surface this as an explicit failure. Previously the run would exit successfully with
                    // zero metrics, which was silently scored as a missing-metrics run rather than the real
                    // failure it represents (target unreachable / ICMP replies disabled).
                    if (iterations > 0 && !responseTimes.Any())
                    {
                        throw new WorkloadException(
                            $"The target endpoint for IP address '{ipAddress}' did not respond to any of the {iterations} ICMP ping attempts " +
                            $"({timedOutPings} timed out). The endpoint does not exist, is unreachable or does not have ICMP replies enabled.",
                            ErrorReason.NetworkTargetDoesNotExist);
                    }

                    this.MetadataContract.AddForScenario(
                        "NetworkPing",
                        null,
                        toolVersion: null);

                    this.MetadataContract.Apply(telemetryContext);

                    if (responseTimes.Any())
                    {
                        this.Logger.LogMetric(
                            "NetworkPing",
                            "Network Ping",
                            startTime,
                            endTime,
                            "avg. round trip time",
                            responseTimes.Average(),
                            MetricUnit.Milliseconds,
                            string.Empty,
                            string.Empty,
                            this.Tags,
                            telemetryContext,
                            MetricRelativity.LowerIsBetter);

                        responseTimes.Clear();
                    }

                    if (networkConnections.Any())
                    {
                        this.Logger.LogMetric(
                            "NetworkPing",
                            "Network Ping",
                            startTime,
                            endTime,
                            "avg. number of connections",
                            networkConnections.Average(),
                            "count",
                            string.Empty,
                            string.Empty,
                            this.Tags,
                            telemetryContext,
                            MetricRelativity.Undefined);

                        networkConnections.Clear();
                    }

                    if (networkBlips.Any())
                    {
                        this.Logger.LogMetric(
                            "NetworkPing",
                            "Network Ping",
                            startTime,
                            endTime,
                            "# of blips",
                            networkBlips.Count,
                            "count",
                            string.Empty,
                            string.Empty,
                            this.Tags,
                            telemetryContext,
                            MetricRelativity.LowerIsBetter);

                        this.Logger.LogMetric(
                            "NetworkPing",
                            "Network Ping",
                            startTime,
                            endTime,
                            "dropped pings",
                            networkBlips.Select(blip => blip.DroppedAttempts).Aggregate((total, attempts) => total += attempts),
                            "count",
                            string.Empty,
                            string.Empty,
                            this.Tags,
                            telemetryContext,
                            MetricRelativity.LowerIsBetter);

                        foreach (NetworkBlip blip in networkBlips)
                        {
                            this.Logger.LogMetric(
                                "NetworkPing",
                                "Network Ping",
                                startTime,
                                endTime,
                                "blip duration",
                                blip.Duration,
                                MetricUnit.Milliseconds,
                                string.Empty,
                                string.Empty,
                                this.Tags,
                                telemetryContext,
                                MetricRelativity.LowerIsBetter);
                        }

                        networkBlips.Clear();
                    }
                }
            }
        }

        private void AddCurrentNetworkConnectionCount(ConcurrentBag<int> connections, EventContext telemetryContext)
        {
            try
            {
                int currentConnections = this.ConnectionCaptureRetryPolicy.Execute(() => NetworkPingExecutor.IPProperties.GetActiveTcpConnections()).Length;
                connections.Add(currentConnections);
            }
            catch (NetworkInformationException exc)
            {
                this.Logger.LogMessage(
                    $"{nameof(NetworkPingExecutor)}.GetConnectionsError",
                    LogLevel.Warning,
                    telemetryContext.Clone().AddError(exc));
            }
        }

        private static bool HasBlipOccurred(int timedOutPings, long duration)
        {
            return timedOutPings > 0 && duration > NetworkPingExecutor.BlipDurationMilliseconds;
        }

        /// <summary>
        /// Represents the outcome of a single ICMP ping attempt.
        /// </summary>
        protected readonly struct PingResult
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PingResult"/> struct.
            /// </summary>
            public PingResult(IPStatus status, long roundtripTime)
            {
                this.Status = status;
                this.RoundtripTime = roundtripTime;
            }

            /// <summary>
            /// The status of the ping reply (e.g. Success, TimedOut).
            /// </summary>
            public IPStatus Status { get; }

            /// <summary>
            /// The round trip time (in milliseconds) for a successful ping.
            /// </summary>
            public long RoundtripTime { get; }
        }
    }
}