// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using MathNet.Numerics.Distributions;
    using MathNet.Numerics.Statistics;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for SockPerf output document
    /// </summary>
    public class SockPerfMetricsParser : MetricsParser
    {
        private static readonly Regex ObservatiosnExpression = new Regex(@"^sockperf.* (\d+) observations.*", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly int HeaderCount = 20;
        private static readonly string Seperator = "------------------------------";
        private static readonly string ObservationsHeader = "packet, txTime(sec), rxTime(sec), rtt(usec)";

        private double confidenceLevel;

        /// <summary>
        /// Constructor for <see cref="SockPerfMetricsParser"/>
        /// </summary>
        public SockPerfMetricsParser(string rawText, double confidenceLevel)
            : base(rawText)
        {
            this.confidenceLevel = confidenceLevel;
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            IList<Metric> metrics = new List<Metric>();

            List<double> packetsLatencyValues = SockPerfMetricsParser.GetPacketsLatency(this.PreprocessedText);

            if (packetsLatencyValues.Count > 0)
            {
                SockPerfMetricsParser.AddStatisticalMetrics(metrics, packetsLatencyValues, this.confidenceLevel);
            }

            return metrics;
        }

        private static List<double> GetPacketsLatency(string preprocessedText)
        {
            // If the line doesn't have column, it's individual result.
            // If the line has column, it's the summary line.
            List<string> lines = preprocessedText.Split(Environment.NewLine).ToList();
            int headerCount = HeaderCount;
            int observationsNumber = 0;
            List<double> packetsLatencyValues = new List<double>();

            foreach (string line in lines)
            {
                if (headerCount != 0)
                {
                    Match match = Regex.Match(line, ObservatiosnExpression.ToString());
                    if (match.Success)
                    {
                        observationsNumber = Convert.ToInt32(match.Groups[1].Value);
                    }

                    headerCount--;

                    if (headerCount == 0 && line.Trim() != Seperator)
                    {
                        throw new WorkloadResultsException(
                            "Results parsing operation failed. The Sockperf parser failed to parse the complete header.",
                            ErrorReason.WorkloadResultsNotFound);
                    }
                }
                else
                {
                    if (line.Trim() == ObservationsHeader)
                    {
                        continue;
                    }
                    else if (line.Trim() == Seperator)
                    {
                        break;
                    }

                    string[] values = line.Split(", ");
                    packetsLatencyValues.Add(Convert.ToDouble(values[3]));
                }
            }

            return packetsLatencyValues;
        }

        private static void AddStatisticalMetrics(IList<Metric> metrics, List<double> packetsLatencyValues, double confidenceLevel)
        {
            Normal normal = new Normal();
            double theta = ((confidenceLevel / 100.0) + 1.0) / 2;
            double mean = packetsLatencyValues.Mean();
            double sd = packetsLatencyValues.StandardDeviation();
            double inverserCDF = normal.InverseCumulativeDistribution(theta);
            double sem = sd / (double)Math.Sqrt(packetsLatencyValues.Count);
            double t = inverserCDF * sem;
            double lowerCI = mean - t;
            double upperCI = mean + t;

            metrics.Add(new Metric("Latency-Min", packetsLatencyValues.Min(), MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 2));
            metrics.Add(new Metric("Latency-Max", packetsLatencyValues.Max(), MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 5));
            metrics.Add(new Metric("Latency-Avg", packetsLatencyValues.Average(), MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 1));
            metrics.Add(new Metric("Latency-P25", packetsLatencyValues.Percentile(25), MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 2));
            metrics.Add(new Metric("Latency-P50", packetsLatencyValues.Percentile(50), MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 1));
            metrics.Add(new Metric("Latency-P75", packetsLatencyValues.Percentile(75), MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 2));
            metrics.Add(new Metric("Latency-P90", packetsLatencyValues.Percentile(90), MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 2));
            metrics.Add(new Metric("Latency-P99", packetsLatencyValues.Percentile(99), MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 1));
            metrics.Add(new Metric("Latency-P99.9", Statistics.QuantileCustom(packetsLatencyValues, 1d - 0.001d, QuantileDefinition.R3), MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 2));
            metrics.Add(new Metric("Latency-P99.99", Statistics.QuantileCustom(packetsLatencyValues, 1d - 0.0001d, QuantileDefinition.R3), MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 5));
            metrics.Add(new Metric("Latency-P99.999", Statistics.QuantileCustom(packetsLatencyValues, 1d - 0.00001d, QuantileDefinition.R3), MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 5));
            double median = Statistics.Median(packetsLatencyValues);
            double[] absoluteDeviations = packetsLatencyValues.Select(x => Math.Abs(x - median)).ToArray();
            metrics.Add(new Metric("Latency-Mad", Statistics.Median(absoluteDeviations), MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 5));
            metrics.Add(new Metric("Latency-StandardErrorMean", sem, MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 5));
            metrics.Add(new Metric("Latency-Stdev", sd, MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 5));
            metrics.Add(new Metric("Latency-LowerCI", lowerCI, MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 5));
            metrics.Add(new Metric("Latency-UpperCI", upperCI, MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 5));
        }
    }
}