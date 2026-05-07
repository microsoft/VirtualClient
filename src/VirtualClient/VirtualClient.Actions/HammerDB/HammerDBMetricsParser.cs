// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using MathNet.Numerics.Statistics;
    using VirtualClient;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for PostgreSQL result document.
    /// </summary>
    public class HammerDBMetricsParser : MetricsParser
    {
        /// <summary>
        /// Sectionize by one or more empty lines.
        /// </summary>
        private static readonly Regex HammerDBSectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by 2 or more spaces.
        /// </summary>
        private static readonly Regex HammerDBDataTableDelimiter = new Regex(@"(\s){2,}", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Regex to detect the benchmark type configured in HammerDB.
        /// </summary>
        private static readonly Regex BenchmarkTypeExpression = new Regex(@"Benchmark set to (?<benchmark>[\w\-]+)\s+for\s+(?<database>.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Regex to capture per-Vuser geometric mean timings.
        /// </summary>
        private static readonly Regex VuserGeometricMeanExpression = new Regex(@"Vuser\s+(?<id>\d+):Geometric mean of query times returning rows\s+\([\d]+\)\s+is\s+(?<mean>[\d.]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Regex to capture per-Vuser query set completions.
        /// </summary>
        private static readonly Regex VuserQuerySetExpression = new Regex(@"Vuser\s+(?<id>\d+):Completed\s+(?<count>[\d.]+)\s+query set\(s\)\s+in\s+(?<duration>[\d.]+)\s+seconds", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Regex to capture per-Vuser individual query durations.
        /// </summary>
        private static readonly Regex VuserQueryDurationExpression = new Regex(@"Vuser\s+(?<id>\d+):query\s+(?<query>\d+)\s+completed\s+in\s+(?<duration>[\d.]+)\s+seconds", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private HammerDBBenchmark benchmarkType;

        /// <summary>
        /// Constructor for <see cref="HammerDBMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public HammerDBMetricsParser(string rawText)
            : base(rawText)
        {
        }

        private enum HammerDBBenchmark
        {
            Unknown,
            TPCC,
            TPCH
        }

        /// <summary>
        /// Results for PostgreSQL.
        /// </summary>
        protected DataTable HammerDBResult { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            List<Metric> metrics = new List<Metric>();

            try
            {
                this.benchmarkType = this.DetermineBenchmarkType();
                this.Metadata["Benchmark"] = this.benchmarkType.ToString();

                this.Preprocess();

                if (this.benchmarkType == HammerDBBenchmark.TPCC)
                {
                    this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, HammerDBSectionDelimiter);
                    this.ThrowIfInvalidOutputFormat();
                    this.CalculateThroughputResult();

                    List<Metric> defaultMetrics = new List<Metric>(this.HammerDBResult.GetMetrics(nameIndex: 1, valueIndex: 0, metricRelativity: MetricRelativity.HigherIsBetter));

                    Metric operationsPerMinute = defaultMetrics.FirstOrDefault(m => string.Equals(m.Name, "Operations/min", StringComparison.OrdinalIgnoreCase));
                    if (operationsPerMinute != null)
                    {
                        metrics.Add(new Metric(
                            "Operations/sec",
                            operationsPerMinute.Value / 60,
                            MetricUnit.OperationsPerSec,
                            relativity: MetricRelativity.HigherIsBetter,
                            verbosity: 0));
                    }

                    Metric transactionsPerMinute = defaultMetrics.FirstOrDefault(m => string.Equals(m.Name, "Transactions/min", StringComparison.OrdinalIgnoreCase));
                    if (transactionsPerMinute != null)
                    {
                        metrics.Add(new Metric(
                            "Transactions/sec",
                            transactionsPerMinute.Value / 60,
                            MetricUnit.TransactionsPerSec,
                            relativity: MetricRelativity.HigherIsBetter,
                            verbosity: 0));
                    }
                }

                if (this.benchmarkType == HammerDBBenchmark.TPCH)
                {
                    metrics.AddRange(this.GetTPCHMetrics());
                }

                return metrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse HammerDB metrics from results.", exc, ErrorReason.InvalidResults);
            }
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            this.PreprocessedText = Regex.Replace(this.RawText, $"{Environment.NewLine}", " ");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, $"(\n\r)|(\r\n)", " ");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"TEST RESULT : System achieved ", $"{Environment.NewLine}{Environment.NewLine}TEST RESULT{Environment.NewLine}");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"NOPM from ", $" Operations/min{Environment.NewLine}");
            this.PreprocessedText = Regex.Replace(this.PreprocessedText, @"(PostgreSQL|MySQL) TPM", $" Transactions/min{Environment.NewLine}{Environment.NewLine}");
        }

        private void CalculateThroughputResult()
        {
            string sectionName = "TEST RESULT";
            IList<string> columnNames = new List<string> { "Value", "Name" };
            this.HammerDBResult = DataTableExtensions.ConvertToDataTable(
                this.Sections[sectionName], HammerDBDataTableDelimiter, sectionName, columnNames);
        }

        /// <inheritdoc/>
        private void ThrowIfInvalidOutputFormat()
        {
            if (this.Sections.Count <= 0 || !this.Sections.ContainsKey("TEST RESULT"))
            {
                throw new SchemaException("The HammerDB output file has incorrect format for parsing");
            }
        }

        private HammerDBBenchmark DetermineBenchmarkType()
        {
            HammerDBBenchmark benchmark = HammerDBBenchmark.Unknown;
            Match match = BenchmarkTypeExpression.Match(this.RawText);
            if (match.Success)
            {
                string normalizedBenchmark = match.Groups["benchmark"].Value.Replace("-", string.Empty).ToUpperInvariant();

                if (normalizedBenchmark.Contains("TPCH") || normalizedBenchmark.Contains("TPROCH"))
                {
                    benchmark = HammerDBBenchmark.TPCH;
                }
                else if (normalizedBenchmark.Contains("TPCC") || normalizedBenchmark.Contains("TPROCC"))
                {
                    benchmark = HammerDBBenchmark.TPCC;
                }
            }

            return benchmark;
        }

        private IEnumerable<Metric> GetTPCHMetrics()
        {
            List<Metric> vuserMetrics = new List<Metric>();
            IDictionary<int, HammerDBVuserSummary> vuserSummaries = new Dictionary<int, HammerDBVuserSummary>();

            foreach (Match match in VuserQuerySetExpression.Matches(this.RawText))
            {
                int vuserId = int.Parse(match.Groups["id"].Value, CultureInfo.InvariantCulture);
                HammerDBVuserSummary summary = this.GetOrCreateVuserSummary(vuserSummaries, vuserId);

                int querySetNumber = (int)Math.Round(double.Parse(match.Groups["count"].Value, CultureInfo.InvariantCulture));
                double durationSeconds = double.Parse(match.Groups["duration"].Value, CultureInfo.InvariantCulture);

                summary.QuerySetNumbers.Add(querySetNumber);
                summary.QuerySetDurationsByIndex[querySetNumber] = durationSeconds;
            }

            foreach (Match match in VuserGeometricMeanExpression.Matches(this.RawText))
            {
                int vuserId = int.Parse(match.Groups["id"].Value, CultureInfo.InvariantCulture);
                HammerDBVuserSummary summary = this.GetOrCreateVuserSummary(vuserSummaries, vuserId);

                double meanSeconds = double.Parse(match.Groups["mean"].Value, CultureInfo.InvariantCulture);
                summary.GeometricMeans.Add(meanSeconds);

                int meanIndex = summary.QuerySetGeometricMeansByIndex.Count;
                int querySetNumber = summary.QuerySetNumbers.Count > meanIndex
                    ? summary.QuerySetNumbers[meanIndex]
                    : meanIndex + 1;

                summary.QuerySetGeometricMeansByIndex[querySetNumber] = meanSeconds;
            }

            foreach (Match match in VuserQueryDurationExpression.Matches(this.RawText))
            {
                int vuserId = int.Parse(match.Groups["id"].Value, CultureInfo.InvariantCulture);
                HammerDBVuserSummary summary = this.GetOrCreateVuserSummary(vuserSummaries, vuserId);

                double durationSeconds = double.Parse(match.Groups["duration"].Value, CultureInfo.InvariantCulture);
                summary.QueryDurationsSeconds.Add(durationSeconds);
            }

            foreach (KeyValuePair<int, HammerDBVuserSummary> entry in vuserSummaries.OrderBy(v => v.Key))
            {
                int vuserId = entry.Key;
                HammerDBVuserSummary summary = entry.Value;

                IDictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>
                {
                    { "Benchmark", this.benchmarkType.ToString() },
                    { "Vuser", vuserId }
                };

                string metricPrefix = $"Vuser {vuserId}";

                foreach (KeyValuePair<int, double> entryDuration in summary.QuerySetDurationsByIndex.OrderBy(kvp => kvp.Key))
                {
                    IDictionary<string, IConvertible> setMetadata = new Dictionary<string, IConvertible>(metadata)
                    {
                        { "QuerySet", entryDuration.Key }
                    };

                    vuserMetrics.Add(new Metric(
                        $"{metricPrefix} QuerySet{entryDuration.Key} Duration",
                        entryDuration.Value,
                        MetricUnit.Seconds,
                        MetricRelativity.LowerIsBetter,
                        verbosity: 2,
                        metadata: setMetadata));

                    if (summary.QuerySetGeometricMeansByIndex.TryGetValue(entryDuration.Key, out double setGeometricMean))
                    {
                        vuserMetrics.Add(new Metric(
                            $"{metricPrefix} QuerySet{entryDuration.Key} GeometricMean",
                            setGeometricMean,
                            MetricUnit.Seconds,
                            MetricRelativity.LowerIsBetter,
                            metadata: setMetadata));
                    }
                }
            }

            List<double> allQueryDurations = vuserSummaries.Values.SelectMany(summary => summary.QueryDurationsSeconds).ToList();
            if (allQueryDurations.Count > 0)
            {
                IDictionary<string, IConvertible> aggregateMetadata = new Dictionary<string, IConvertible>
                {
                    { "Benchmark", this.benchmarkType.ToString() },
                    { "Scope", "AllVusers" }
                };

                vuserMetrics.Add(new Metric(
                    "AllVusers QueryDurationMin",
                    allQueryDurations.Min(),
                    MetricUnit.Seconds,
                    MetricRelativity.LowerIsBetter,
                    metadata: aggregateMetadata));

                vuserMetrics.Add(new Metric(
                    "AllVusers QueryDurationMax",
                    allQueryDurations.Max(),
                    MetricUnit.Seconds,
                    MetricRelativity.LowerIsBetter,
                    metadata: aggregateMetadata));

                vuserMetrics.Add(new Metric(
                    "AllVusers QueryDurationAvg",
                    allQueryDurations.Average(),
                    MetricUnit.Seconds,
                    MetricRelativity.LowerIsBetter,
                    metadata: aggregateMetadata));

                vuserMetrics.Add(new Metric(
                    "AllVusers QueryDurationStdev",
                    Statistics.StandardDeviation(allQueryDurations),
                    MetricUnit.Seconds,
                    MetricRelativity.LowerIsBetter,
                    metadata: aggregateMetadata));

                vuserMetrics.Add(new Metric(
                    "AllVusers QueryDurationP90",
                    Statistics.Percentile(allQueryDurations, 90),
                    MetricUnit.Seconds,
                    MetricRelativity.LowerIsBetter,
                    metadata: aggregateMetadata));

                vuserMetrics.Add(new Metric(
                    "AllVusers QueryDurationP99",
                    Statistics.Percentile(allQueryDurations, 99),
                    MetricUnit.Seconds,
                    MetricRelativity.LowerIsBetter,
                    metadata: aggregateMetadata));
            }

            return vuserMetrics;
        }

        private HammerDBVuserSummary GetOrCreateVuserSummary(IDictionary<int, HammerDBVuserSummary> summaries, int vuserId)
        {
            if (!summaries.TryGetValue(vuserId, out HammerDBVuserSummary summary))
            {
                summary = new HammerDBVuserSummary();
                summaries[vuserId] = summary;
            }

            return summary;
        }

        private class HammerDBVuserSummary
        {
            public List<int> QuerySetNumbers { get; } = new List<int>();

            public Dictionary<int, double> QuerySetDurationsByIndex { get; } = new Dictionary<int, double>();

            public List<double> GeometricMeans { get; } = new List<double>();

            public Dictionary<int, double> QuerySetGeometricMeansByIndex { get; } = new Dictionary<int, double>();

            public List<double> QueryDurationsSeconds { get; } = new List<double>();
        }
    }
}