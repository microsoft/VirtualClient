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
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// Profile executes a simple network ping test.
    /// </summary>
    [WindowsCompatible]
    [UnixCompatible]
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
        /// Parameter. Defines the target IP address to which to send ICMP ping requests.
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
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.IPAddress == "NotDefined")
            {
                throw new WorkloadException(
                    $"Required instructions missing. This profile requires the IP address of the server that will be pinged to be provided on the command line. " +
                    $"Include the IP address of the target server in the parameters (e.g. --parameters:{nameof(this.IPAddress)}=1.2.3.4).",
                    ErrorReason.InstructionsNotProvided);
            }

            if (!System.Net.IPAddress.TryParse(this.IPAddress, out IPAddress ipAddress))
            {
                throw new WorkloadException(
                    $"Invalid IP address format. The address provided '{this.IPAddress}' is not a valid IPv4 or IPv6 address.",
                    ErrorReason.InstructionsNotValid);
            }

            return this.ExecutePingServerAsync(ipAddress, telemetryContext, cancellationToken);
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
                while (iterations < this.PingIterations && !cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await this.PingRetryPolicy.ExecuteAsync(async () =>
                        {
                            PingReply reply = await networkPing.SendPingAsync(ipAddress, NetworkPingExecutor.BlipDurationMilliseconds).ConfigureAwait(false);

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
                                    timedOutPings++;
                                    iterations--;
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
                    this.MetadataContract.AddForScenario(
                        "NetworkPing",
                        null,
                        toolVersion: null);

                    this.MetadataContract.Apply(telemetryContext);

                    if (responseTimes.Any())
                    {
                        this.Logger.LogMetrics(
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
                        this.Logger.LogMetrics(
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
                        this.Logger.LogMetrics(
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

                        this.Logger.LogMetrics(
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
                            this.Logger.LogMetrics(
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
    }
}