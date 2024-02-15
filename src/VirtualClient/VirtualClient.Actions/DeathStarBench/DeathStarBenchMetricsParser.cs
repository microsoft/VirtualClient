// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    ///  Parser for DeathStarBench output document.
    /// </summary>
    public class DeathStarBenchMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize the text by one or more empty lines.
        /// </summary>
        private static readonly Regex DeathStarBenchSectionDelimiter = new Regex($"({Environment.NewLine})(\\s)*({Environment.NewLine})", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Matches lines with all dashes.
        /// </summary>
        private static readonly Regex DashLineRegex = new Regex(@"(-){2,}(\s)*", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Matches numeric expression.
        /// </summary>
        private static readonly Regex NumericExpression = new Regex(@"[0-9\.]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Constructor for <see cref="DeathStarBenchMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public DeathStarBenchMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            try
            {
                this.Preprocess();
                this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, DeathStarBenchMetricsParser.DeathStarBenchSectionDelimiter);
                this.ThrowIfInvalidOutputFormat();

                List<Metric> metrics = new List<Metric>();

                metrics.AddRange(this.ParseThreadStatsResult());
                metrics.AddRange(this.ParseLatencyDistributionResult());
                metrics.AddRange(this.ParseTransferPerSecResult());

                return metrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse DeathStarBench metrics from results.", exc, ErrorReason.InvalidResults);
            }
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, @"[\r\n|\n]+", $"{Environment.NewLine}");
            this.PreprocessedText = TextParsingExtensions.RemoveRows(this.PreprocessedText, DeathStarBenchMetricsParser.DashLineRegex);
            this.PreprocessedText = this.PreprocessedText.Replace("Thread Stats", $@"{Environment.NewLine}Thread Stats{Environment.NewLine}Thread Stats");
            this.PreprocessedText = this.PreprocessedText.Replace("Latency Distribution (HdrHistogram - Recorded Latency)", $"{Environment.NewLine}Latency Distribution");
            this.PreprocessedText = this.PreprocessedText.Replace("Detailed Percentile spectrum:", $"{Environment.NewLine}Detailed Percentile spectrum");
            this.PreprocessedText = this.PreprocessedText.Replace("Transfer/sec:", $@"{Environment.NewLine}Transfer/sec{Environment.NewLine}Transfer/sec");
        }

        private IList<Metric> ParseThreadStatsResult()
        {
            string sectionName = "Thread Stats";
            return this.GetThreadStatsResult(this.Sections[sectionName]);
        }

        private IList<Metric> ParseLatencyDistributionResult()
        {
            string sectionName = "Latency Distribution";
            return this.GetLatencyDistributionResult(this.Sections[sectionName]);
        }

        private IList<Metric> ParseTransferPerSecResult()
        {
            string sectionName = "Transfer/sec";
            return this.GetTransferPerSecResult(this.Sections[sectionName]);
        }

        private IList<Metric> GetThreadStatsResult(string resultsText)
        {
            IList<Metric> threadStatsResult = new List<Metric>();
            /*
             Thread Stats
              Thread Stats  Avg      Stdev     99%   +/- Stdev
                Latency   540.95us   66.96us 729.00us   72.50%
                Req/Sec     1.07k     10.83     0.00     99.01%
             */
            MatchCollection matches = Regex.Matches(
               resultsText,
               @"(Latency|Req/Sec)[0-9\x20\.\(us|s|ms|m|h|a-z)]+",
               RegexOptions.IgnoreCase);

            if (matches.Any())
            {
                // Latency
                IEnumerable<string> networkLatency = Regex.Split(matches[0].Value, @"\s+");

                if (networkLatency?.Count() == 5)
                {
                    Match valueUnitMatch;
                    valueUnitMatch = Regex.Match(networkLatency.ElementAt(1), @"(\d+([.,]\d+)?) *(us|ms|s|m|h)");
                    threadStatsResult.Add(new Metric("Avg network Latency", DeathStarBenchMetricsParser.ParseNumericValue(valueUnitMatch.Groups[1].Value), valueUnitMatch.Groups[3].Value, MetricRelativity.LowerIsBetter));

                    valueUnitMatch = Regex.Match(networkLatency.ElementAt(2), @"(\d+([.,]\d+)?) *(us|ms|s|m|h)");
                    threadStatsResult.Add(new Metric("Stdev network Latency", DeathStarBenchMetricsParser.ParseNumericValue(valueUnitMatch.Groups[1].Value), valueUnitMatch.Groups[3].Value, MetricRelativity.LowerIsBetter));

                }

                // Req/sec
                IEnumerable<string> requestsPerSec = Regex.Split(matches[1].Value, @"\s+");

                if (requestsPerSec?.Count() == 5)
                {
                    Match valueUnitMatch;
                    Dictionary<int, string> metricNameMatch = new Dictionary<int, string>
                    {
                        { 1, "Avg Req/sec" },
                        { 2,  "Stdev Req/sec" },
                        { 3,  "99% Req/sec" }
                    };

                    foreach (int i in Enumerable.Range(1, 3))
                    {
                        valueUnitMatch = Regex.Match(requestsPerSec.ElementAt(i), @"(\d+([.,]\d+)?) *(k)*");
                        string unit = valueUnitMatch.Groups[3].Value;
                        double value;
                        if (unit == "k")
                        {
                            value = DeathStarBenchMetricsParser.ParseNumericValue(valueUnitMatch.Groups[1].Value) * 1000;
                        }
                        else
                        {
                            value = DeathStarBenchMetricsParser.ParseNumericValue(valueUnitMatch.Groups[1].Value);
                        }
                        
                        if (metricNameMatch[i] == "Stdev Req/sec")
                        {
                            threadStatsResult.Add(new Metric(metricNameMatch[i], value, MetricRelativity.LowerIsBetter));
                        }
                        else
                        {
                            threadStatsResult.Add(new Metric(metricNameMatch[i], value, MetricRelativity.HigherIsBetter));
                        }
                    }

                }

            }

            return threadStatsResult;
        }

        private IList<Metric> GetLatencyDistributionResult(string resultsText)
        {
            IList<Metric> latencyDistributionResult = new List<Metric>();

            /*Latency Distribution
                 50.000%  524.00us
                 75.000%  573.00us
                 90.000%  620.00us
                 99.000%  729.00us
                 99.900%  729.00us
                 99.990%  729.00us
                 99.999%  729.00us
                100.000%  729.00us*/

            MatchCollection matches = Regex.Matches(
               resultsText,
               @"(50.000%|75.000%|90.000%|99.000%|99.990%|100.000%)[0-9\.\x20\(us|ms|s|m|h)]+",
               RegexOptions.IgnoreCase);
            if (matches.Any())
            {
                string valueUnit;

                // 50.000%
                IEnumerable<string> percentiles50 = Regex.Split(matches[0].Value, @"\s+");

                if (percentiles50?.Count() == 2)
                {
                    valueUnit = percentiles50.ElementAt(1);
                    Match valueUnitMatch = Regex.Match(valueUnit, @"(\d+([.,]\d+)?) *(us|ms|s|m|h)");
                    latencyDistributionResult.Add(new Metric("50% Network Latency", DeathStarBenchMetricsParser.ParseNumericValue(valueUnitMatch.Groups[1].Value), valueUnitMatch.Groups[3].Value, MetricRelativity.LowerIsBetter));
                }

                // 75.000%
                IEnumerable<string> percentiles75 = Regex.Split(matches[1].Value, @"\s+");

                if (percentiles75?.Count() == 2)
                {
                    valueUnit = percentiles75.ElementAt(1);
                    Match valueUnitMatch = Regex.Match(valueUnit, @"(\d+([.,]\d+)?) *(us|ms|s|m|h)");
                    latencyDistributionResult.Add(new Metric("75% Network Latency", DeathStarBenchMetricsParser.ParseNumericValue(valueUnitMatch.Groups[1].Value), valueUnitMatch.Groups[3].Value, MetricRelativity.LowerIsBetter));
                }

                // 90.000%
                IEnumerable<string> percentiles90 = Regex.Split(matches[2].Value, @"\s+");

                if (percentiles90?.Count() == 2)
                {
                    valueUnit = percentiles90.ElementAt(1);
                    Match valueUnitMatch = Regex.Match(valueUnit, @"(\d+([.,]\d+)?) *(us|ms|s|m|h)");
                    latencyDistributionResult.Add(new Metric("90% Network Latency", DeathStarBenchMetricsParser.ParseNumericValue(valueUnitMatch.Groups[1].Value), valueUnitMatch.Groups[3].Value, MetricRelativity.LowerIsBetter));
                }

                // 99.000%
                IEnumerable<string> percentiles99 = Regex.Split(matches[3].Value, @"\s+");

                if (percentiles99?.Count() == 2)
                {
                    valueUnit = percentiles99.ElementAt(1);
                    Match valueUnitMatch = Regex.Match(valueUnit, @"(\d+([.,]\d+)?) *(us|ms|s|m|h)");
                    latencyDistributionResult.Add(new Metric("99% Network Latency", DeathStarBenchMetricsParser.ParseNumericValue(valueUnitMatch.Groups[1].Value), valueUnitMatch.Groups[3].Value, MetricRelativity.LowerIsBetter));
                }

                // 99.990%
                IEnumerable<string> percentiles99_99 = Regex.Split(matches[4].Value, @"\s+");

                if (percentiles99_99?.Count() == 2)
                {
                    valueUnit = percentiles99_99.ElementAt(1);
                    Match valueUnitMatch = Regex.Match(valueUnit, @"(\d+([.,]\d+)?) *(us|ms|s|m|h)");
                    latencyDistributionResult.Add(new Metric("99.99% Network Latency", DeathStarBenchMetricsParser.ParseNumericValue(valueUnitMatch.Groups[1].Value), valueUnitMatch.Groups[3].Value, MetricRelativity.LowerIsBetter));
                }

                // 100.000%
                IEnumerable<string> percentiles100 = Regex.Split(matches[5].Value, @"\s+");

                if (percentiles100?.Count() == 2)
                {
                    valueUnit = percentiles100.ElementAt(1);
                    Match valueUnitMatch = Regex.Match(valueUnit, @"(\d+([.,]\d+)?) *(us|ms|s|m|h)");
                    latencyDistributionResult.Add(new Metric("100% Network Latency", DeathStarBenchMetricsParser.ParseNumericValue(valueUnitMatch.Groups[1].Value), valueUnitMatch.Groups[3].Value, MetricRelativity.LowerIsBetter));
                }
            }

            return latencyDistributionResult;
        }

        private IList<Metric> GetTransferPerSecResult(string resultsText)
        {
            IList<Metric> transferPerSecResult = new List<Metric>();
            /*
             Transfer/sec
                Transfer/sec  6.23KB
             */
            MatchCollection matches = Regex.Matches(
               resultsText,
               @"Transfer/sec[0-9\a-z\.\%\x20]+",
               RegexOptions.IgnoreCase);
            if (matches.Any())
            {
                // Transfer/sec
                IEnumerable<string> transferPerSec = Regex.Split(matches[0].Value, @"\s+");

                if (transferPerSec?.Count() == 2)
                {
                    Match valueUnitMatch = Regex.Match(transferPerSec.ElementAt(1), @"(\d+([.,]\d+)?) *(B|KB|MB|GB)");
                    transferPerSecResult.Add(new Metric("Transfer/sec", DeathStarBenchMetricsParser.ParseNumericValue(valueUnitMatch.Groups[1].Value), valueUnitMatch.Groups[3].Value, MetricRelativity.HigherIsBetter));
                }
            }

            return transferPerSecResult;
        }

        private static double ParseNumericValue(string text)
        {
            double value = 0.0;
            Match match = DeathStarBenchMetricsParser.NumericExpression.Match(text);
            if (match.Success)
            {
                value = double.Parse(match.Value);
            }

            return value;
        }

        private void ThrowIfInvalidOutputFormat()
        {
            if (this.Sections.Count <= 1 || !this.Sections.ContainsKey("Thread Stats")
                || !this.Sections.ContainsKey("Latency Distribution") || !this.Sections.ContainsKey("Transfer/sec"))
            {
                throw new SchemaException("The DeathStarBench output file has incorrect format for parsing");
            }
        }
    }
}
