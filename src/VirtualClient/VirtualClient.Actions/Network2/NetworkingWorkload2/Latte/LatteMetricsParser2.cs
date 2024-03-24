// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    ///  Latte toolset results parser.
    /// </summary>
    public class LatteMetricsParser2 : MetricsParser
    {
        private static readonly Regex HistogramEntryExpression = new Regex(
            @"\s*(\d+)\s*(\d+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Initializes a new instance of the <see cref="LatteMetricsParser2"/> class.
        /// </summary>
        /// <param name="results">The raw Latte results from standard output.</param>
        public LatteMetricsParser2(string results)
            : base(results)
        {
        }

        /// <summary>
        /// Parses the Latte results and returns a list of metrics from them.
        /// </summary>
        public override IList<Metric> Parse()
        {
            try
            {
                /*
                    [Example Output]
                    Protocol      UDP
                    SendMethod    RIO
                    ReceiveMethod RIO
                    SO_SNDBUF     Default
                    SO_RCVBUF     Default
                    MsgSize(byte) 4
                    Iterations    100100
                    Latency(usec) 224.79
                    CPU(%)        11.3
                    CtxSwitch/sec 2164     (0.62/iteration)
                    SysCall/sec   15482     (3.26/iteration)
                    Interrupt/sec 8125     (2.39/iteration)

                    Interval(usec)     Frequency
                    0      0
                    1      0
                    2      0
                    ...
                    103      1664
                    104      1522
                    105      1086
                 */

                List<Metric> metrics = new List<Metric>();
                IEnumerable<HistogramEntry> latencyHistogramData = this.GetLatencyHistogramData();
                this.AddLatencyMetrics(metrics, latencyHistogramData);
                this.AddLatencyPercentileMetrics(metrics, latencyHistogramData);
                this.AddOperatingSystemEfficiencyMetrics(metrics);

                return metrics;
            }
            catch (VirtualClientException)
            {
                throw;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException(
                    $"Results parsing operation failed. The Latte parser failed to parse the results of the Latte workload.",
                    exc,
                    ErrorReason.WorkloadResultsParsingFailed);
            }
        }

        private void AddLatencyMetrics(IList<Metric> metrics, IEnumerable<HistogramEntry> histogramData)
        {
            Match latencyText = Regex.Match(
                this.RawText,
                @"^Latency\(usec\)\s*(.*)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            if (!latencyText.Success || !double.TryParse(latencyText.Groups[1].Value, out double latency))
            {
                throw new WorkloadResultsException(
                    "Invalid Latte results. The results provided from the execution of the Latte workload do not have required latency measurements.",
                    ErrorReason.WorkloadResultsParsingFailed);
            }

            metrics.Add(new Metric(
                "Latency-Avg",
                latency,
                MetricUnit.Microseconds,
                MetricRelativity.LowerIsBetter,
                description: "Average observed network latency."));

            double minLatency = histogramData.Min(entry => entry.LatencyInterval);

            metrics.Add(new Metric(
                "Latency-Min",
                minLatency,
                MetricUnit.Microseconds,
                MetricRelativity.LowerIsBetter,
                description: "Minimum observed network latency."));

            double maxLatency = histogramData.Max(entry => entry.LatencyInterval);

            metrics.Add(new Metric(
                "Latency-Max",
                maxLatency,
                MetricUnit.Microseconds,
                MetricRelativity.LowerIsBetter,
                description: "Maximum observed network latency."));
        }

        private void AddLatencyPercentileMetrics(IList<Metric> metrics, IEnumerable<HistogramEntry> histogramData)
        {
            double totalObservations = histogramData.Max(entry => entry.FrequencyInTotal);

            double latencyP25 = histogramData.Where(entry => entry.FrequencyInTotal <= totalObservations * .25)
                .OrderByDescending(entry => entry.LatencyInterval)
                .First().LatencyInterval;

            metrics.Add(new Metric(
                "Latency-P25",
                latencyP25,
                MetricUnit.Microseconds,
                MetricRelativity.LowerIsBetter,
                description: "Network latencies for 25% of observations are at this latency or lower (P25)."));

            double latencyP50 = histogramData.Where(entry => entry.FrequencyInTotal <= totalObservations * .5)
                .OrderByDescending(entry => entry.LatencyInterval)
                .First().LatencyInterval;

            metrics.Add(new Metric(
                "Latency-P50",
                latencyP50,
                MetricUnit.Microseconds,
                MetricRelativity.LowerIsBetter,
                description: "Network latencies for 50% of observations are at this latency or lower (P50)."));

            double latencyP75 = histogramData.Where(entry => entry.FrequencyInTotal <= totalObservations * .75)
                .OrderByDescending(entry => entry.LatencyInterval)
                .First().LatencyInterval;

            metrics.Add(new Metric(
                "Latency-P75",
                latencyP75,
                MetricUnit.Microseconds,
                MetricRelativity.LowerIsBetter,
                description: "Network latencies for 75% of observations are at this latency or lower (P75)."));

            double latencyP90 = histogramData.Where(entry => entry.FrequencyInTotal <= totalObservations * .90)
                .OrderByDescending(entry => entry.LatencyInterval)
                .First().LatencyInterval;

            metrics.Add(new Metric(
                "Latency-P90",
                latencyP90,
                MetricUnit.Microseconds,
                MetricRelativity.LowerIsBetter,
                description: "Network latencies for 90% of observations are at this latency or lower (P90)."));

            double latencyP99 = histogramData.Where(entry => entry.FrequencyInTotal <= totalObservations * .99)
                .OrderByDescending(entry => entry.LatencyInterval)
                .First().LatencyInterval;

            metrics.Add(new Metric(
                "Latency-P99",
                latencyP99,
                MetricUnit.Microseconds,
                MetricRelativity.LowerIsBetter,
                description: "Network latencies for 99% of observations are at this latency or lower (P99)."));

            double latencyP99_9 = histogramData.Where(entry => entry.FrequencyInTotal <= totalObservations * .999)
                .OrderByDescending(entry => entry.LatencyInterval)
                .First().LatencyInterval;

            metrics.Add(new Metric(
                "Latency-P99.9",
                latencyP99_9,
                MetricUnit.Microseconds,
                MetricRelativity.LowerIsBetter,
                description: "Network latencies for 99.9% of observations are at this latency or lower (P99.9)."));

            double latencyP99_99 = histogramData.Where(entry => entry.FrequencyInTotal <= totalObservations * .9999)
                .OrderByDescending(entry => entry.LatencyInterval)
                .First().LatencyInterval;

            metrics.Add(new Metric(
                "Latency-P99.99",
                latencyP99_99,
                MetricUnit.Microseconds,
                MetricRelativity.LowerIsBetter,
                description: "Network latencies for 99.99% of observations are at this latency or lower (P99.99)."));

            double latencyP99_999 = histogramData.Where(entry => entry.FrequencyInTotal <= totalObservations * .99999)
                .OrderByDescending(entry => entry.LatencyInterval)
                .First().LatencyInterval;

            metrics.Add(new Metric(
                "Latency-P99.999",
                latencyP99_999,
                MetricUnit.Microseconds,
                MetricRelativity.LowerIsBetter,
                description: "Network latencies for 99.999% of observations are at this latency or lower (P99.999)."));
        }

        private void AddOperatingSystemEfficiencyMetrics(IList<Metric> metrics)
        {
            Match interruptsText = Regex.Match(
                this.RawText,
                @"^Interrupt/sec\s*(\d+)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            if (interruptsText.Success && double.TryParse(interruptsText.Groups[1].Value, out double interruptsPerSec))
            {
                metrics.Add(new Metric(
                    "Interrupts/sec",
                    interruptsPerSec,
                    MetricRelativity.LowerIsBetter,
                    description: "Operating system interrupts per second."));
            }

            Match sysCallsText = Regex.Match(
                this.RawText,
                @"^SysCall/sec\s*(\d+)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            if (sysCallsText.Success && double.TryParse(sysCallsText.Groups[1].Value, out double sysCallsPerSec))
            {
                metrics.Add(new Metric(
                    "SystemCalls/sec",
                    sysCallsPerSec,
                    MetricRelativity.LowerIsBetter,
                    description: "Operating system system calls per second."));
            }

            Match contextSwitchesText = Regex.Match(
                this.RawText,
                @"^CtxSwitch/sec\s*(\d+)",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            if (contextSwitchesText.Success && double.TryParse(contextSwitchesText.Groups[1].Value, out double contextSwitchesPerSec))
            {
                metrics.Add(new Metric(
                    "ContextSwitches/sec",
                    contextSwitchesPerSec,
                    MetricRelativity.LowerIsBetter,
                    description: "Operating system context switches per second."));
            }
        }

        private IEnumerable<HistogramEntry> GetLatencyHistogramData()
        {
            Match latencyHistogram = Regex.Match(
                this.RawText,
                @"(?<=\s*Interval\(usec\)\s*Frequency\s*)(\s*[0-9]+\s+[0-9]+\s*$)+",
                RegexOptions.IgnoreCase | RegexOptions.Multiline);

            if (!latencyHistogram.Success)
            {
                throw new WorkloadResultsException(
                    "Invalid Latte results. The results provided from the execution of the Latte workload do not have required " +
                    "latency interval/frequency histogram measurements.",
                    ErrorReason.WorkloadResultsParsingFailed);
            }

            string[] lines = latencyHistogram.Value.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
            if (lines?.Any() != true)
            {
                throw new WorkloadResultsException(
                    "Invalid Latte results. The results provided from the execution of the Latte workload do not have required " +
                    "latency interval/frequency histogram measurements.",
                    ErrorReason.WorkloadResultsParsingFailed);
            }

            // The Latte results histogram shows intervals (in microseconds/usec) and how many
            // network operations fell within that latency interval range.
            //
            // Example:
            // Interval(usec)     Frequency
            // 0      0
            // 1      0
            // 2      0
            // ...
            // 103      1664
            // 104      1522
            // 105      1086
            List<HistogramEntry> histogramData = new List<HistogramEntry>();

            int totalObservations = 0;
            foreach (string line in lines)
            {
                Match histogramEntry = LatteMetricsParser2.HistogramEntryExpression.Match(line);
                if (histogramEntry.Success)
                {
                    if (double.TryParse(histogramEntry.Groups[1].Value, out double interval) && int.TryParse(histogramEntry.Groups[2].Value, out int frequency))
                    {
                        if (frequency > 0)
                        {
                            totalObservations += frequency;
                            histogramData.Add(new HistogramEntry(interval, frequency, totalObservations));
                        }
                    }
                }
            }

            return histogramData;
        }

        private class HistogramEntry
        {
            public HistogramEntry(double latencyInterval, int frequency, int frequencyInTotal)
            {
                this.LatencyInterval = latencyInterval;
                this.Frequency = frequency;
                this.FrequencyInTotal = frequencyInTotal;
            }

            /// <summary>
            /// The latency interval. This describes a specific latency (in microseconds) where some number
            /// of operations were at this specific latency.
            /// </summary>
            public double LatencyInterval { get; }

            /// <summary>
            /// The number of operations that were at this specific latency.
            /// </summary>
            public int Frequency { get; }

            /// <summary>
            /// The running total of all frequencies up until this point/latency. Used to calculate
            /// percentiles (e.g. P25, P50, P90).
            /// </summary>
            public int FrequencyInTotal { get; }
        }
    }
}
