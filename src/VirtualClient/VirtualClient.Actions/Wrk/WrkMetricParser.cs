// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using MathNet.Numerics.Distributions;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// A metrics parser for WRK workload results.
    /// </summary>
    public class WrkMetricParser : MetricsParser
    {
        /// <summary>
        /// Sectionize the text by one or more empty lines.
        /// </summary>
        private static readonly Regex WRK2SectionDelimiter = new Regex(@"(\n)(\s)*(\n)", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Separate the column values by ' '
        /// </summary>
        private static readonly Regex WRK2DataTableDelimiter = new Regex(@"(\s)+", RegexOptions.ExplicitCapture);

        /// <summary>
        /// Initializes a new instance of the <see cref="WrkMetricParser"/> class.
        /// </summary>
        /// <param name="resultsText">The raw results from the OpenSSL speed command.</param>
        public WrkMetricParser(string resultsText)
            : base(resultsText)
        {
        }

        /// <summary>
        /// Returns the set of metrics parsed from WRK tool.
        /// </summary>
        /// <returns></returns>
        public override IList<Metric> Parse()
        {
            return this.Parse();
        }

        /// <summary>
        /// Returns the set of metrics parsed from WRK tool.
        /// </summary>
        /// <param name="emitLatencySpectrum"> Enable option to emit percentile spectrum.</param>
        /// <returns></returns>
        public IList<Metric> Parse(bool emitLatencySpectrum = true)
        {
            List<Metric> metrics = new List<Metric>();
            this.Preprocess();

            this.Sections = TextParsingExtensions.Sectionize(this.PreprocessedText, WrkMetricParser.WRK2SectionDelimiter);

            metrics.AddRange(this.ParseErrorDetails());
            metrics.AddRange(this.ParseLatencyDistribution());
            metrics.AddRange(this.ParseRequestAndTransferStats());

            if (emitLatencySpectrum)
            {
                metrics.AddRange(this.ParseLatencySpectrum());
            }

            return metrics;
        }

        /// <summary>
        /// Get Test Configuration
        /// </summary>
        public string GetTestConfig()
        {
            List<string> lines = this.RawText.Split(Environment.NewLine).ToList();
            return $"{lines[0].Trim()} with {lines[1].Trim()}";
        }

        /// <inheritdoc/>
        protected override void Preprocess()
        {
            string latencyDistribution = "Latency Distribution";
            string percentileSpectrum = "Percentile spectrum";
            string requestPerSecond = "Requests/sec:";
            string spectrumMinMaxDetails = "#[Mean";

            /*
            string latencyDistribution_hdr = "Latency Distribution (HdrHistogram - Recorded Latency)";
            string uncorrectedLatencyDistribution_hdr = "Latency Distribution (HdrHistogram - Uncorrected Latency (measured without taking delayed starts into account))";
            string uncorrectedLatencyDistribution = "Uncorrected Latency Distribution";
            ////
            string percentileSpectrum = "Detailed Percentile spectrum:";
            string uncorrectedPercentileSpectrum = "Uncorrected Percentile spectrum:";
            string spectrumMinMaxDetails = "#[Mean";
            */

            List<string> processedLines = new List<string>();
            string uncorrected = string.Empty;
            foreach (string lineText in this.RawText.Split(Environment.NewLine))
            {
                if (string.IsNullOrWhiteSpace(lineText))
                {
                    continue;
                }

                string line = lineText.Trim();

                string newLine = string.Empty;
                if (line.Trim().EndsWith("read", StringComparison.OrdinalIgnoreCase))
                {
                    // Adding additional lines between latency distribution and execution summary
                    newLine = $"{Environment.NewLine}{Environment.NewLine}{line}";
                }
                else if (line.Contains(latencyDistribution, StringComparison.OrdinalIgnoreCase))
                {
                    // Types of Latency Distribution that can show up:
                    // Latency Distribution
                    // Latency Distribution (HdrHistogram - Recorded Latency)
                    // Latency Distribution (HdrHistogram - Uncorrected Latency (measured without taking delayed starts into account))
                    // Uncorrected Latency Distribution

                    if (line.Contains("Uncorrected", StringComparison.OrdinalIgnoreCase))
                    {
                        uncorrected = "Uncorrected";
                    }

                    newLine = $"{Environment.NewLine}{Environment.NewLine}{uncorrected} {latencyDistribution}{Environment.NewLine}{uncorrected} {latencyDistribution}";
                }
                else if (line.Contains(percentileSpectrum, StringComparison.OrdinalIgnoreCase))
                {
                    // We need to distinguish between Detailed Percentile spectrum and Detailed Uncorrected Percentile spectrum
                    // Uncorrected Percentile Spectrum shows up after Uncorrected Latency Distribution (example2)
                    string columns = "Value   metricName   TotalCount 1/(1-Percentile)";
                    newLine = $"{Environment.NewLine}{uncorrected} {percentileSpectrum} {columns}{Environment.NewLine}{uncorrected} {percentileSpectrum} {columns}";
                }
                else if (line.Contains(requestPerSecond, StringComparison.OrdinalIgnoreCase))
                {
                    newLine = $"{Environment.NewLine}{requestPerSecond}{Environment.NewLine}{line}";
                }
                else if (line.Contains(spectrumMinMaxDetails, StringComparison.OrdinalIgnoreCase))
                {
                    newLine = $"{Environment.NewLine}{spectrumMinMaxDetails}{uncorrected}";
                }
                else
                {
                    newLine = line;
                }

                processedLines.Add(newLine);
            }

            this.PreprocessedText = string.Join(Environment.NewLine, processedLines);
        }

        private IList<Metric> ParseErrorDetails()
        {
            List<string> lines = this.RawText.Split(Environment.NewLine).ToList();

            List<Metric> metrics = new List<Metric>();
            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i].Trim();
                if (line.StartsWith("Socket errors", StringComparison.OrdinalIgnoreCase))
                {
                    metrics.Add(new Metric("Socket Error", 1, tags: null, description: line));
                    throw new WorkloadException(line);
                }
                else if (line.StartsWith("Non-2xx", StringComparison.OrdinalIgnoreCase))
                {
                    string[] arr = line.Split(":");
                    if (arr.Count() > 1 && double.TryParse(arr[1].Trim(), out double value))
                    {
                        metrics.Add(new Metric(arr[0].Trim(), value, tags: null, description: line));
                    }
                }
            }

            return metrics;
        }

        private IList<Metric> ParseLatencyDistribution()
        {
            List<Metric> metrics = new List<Metric>();
            string sectionName = "Latency Distribution";

            /*
                Latency Distribution (HdrHistogram - Recorded Latency)
                50.000%   29.56s 
                75.000%   39.75s 
                90.000%   45.97s 
                99.000%   49.74s 
                99.900%   50.17s 
                99.990%   50.23s 
                99.999%   50.23s 
                100.000%   50.23s 

                Latency Distribution (HdrHistogram - Uncorrected Latency (measured without taking delayed starts into account))
                 50.000 % 364.00us
                 75.000 % 484.00us
                 90.000 % 658.00us
                 99.000 % 1.33ms
                 99.900 % 1.33ms
                 99.990 % 1.33ms
                 99.999 % 1.33ms
                100.000 % 1.33ms
            */

            foreach (KeyValuePair<string, string> kvp in this.Sections)
            {
                if (kvp.Key.Contains(sectionName))
                {
                    DataTable table = DataTableExtensions.ConvertToDataTable(this.Sections[kvp.Key], WrkMetricParser.WRK2DataTableDelimiter, sectionName, columnNames: null);

                    bool isUncorrected = kvp.Key.Contains("Uncorrected", StringComparison.OrdinalIgnoreCase);
                    string metricNamePrefix = isUncorrected ? "uncorrected_latency_p" : "latency_p";
                    string description = isUncorrected ? "Latency Distribution(HdrHistogram -Uncorrected Latency(measured without taking delayed starts into account))" : "Latency Distribution (HdrHistogram - Recorded Latency)";

                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        DataRow dataRow = table.Rows[i];

                        string[] metricNameArr = Convert.ToString(dataRow[0]).Trim().TrimEnd('%').TrimStart('0').Split('.');
                        string percentilePrecision = metricNameArr.Count() > 1 ? metricNameArr[1].TrimEnd('0') : string.Empty;
                        string latencyMetricName =
                            metricNamePrefix +
                            metricNameArr[0] +
                            (string.IsNullOrWhiteSpace(percentilePrecision) ? string.Empty : $"_{percentilePrecision}");

                        string latencyMetricValueStr = Convert.ToString(dataRow[1]).Trim();
                        string latencyMetricValueStrV2 = latencyMetricValueStr.Trim('s').Trim(new char[] { 'm', 'h', 'u', 'n' });

                        if (!double.TryParse(latencyMetricValueStrV2, out double latencyMetricValue))
                        {
                            throw new SchemaException(
                                $"Unable to parse the results from {this.GetType().Name}. The results has incorrect metric value format for parsing. " +
                                $"Results table contains {latencyMetricName} = {latencyMetricValueStr}. SectionName: {sectionName}");
                        }

                        if (latencyMetricValueStr.EndsWith('h'))
                        {
                            // hour
                            latencyMetricValue = (double)latencyMetricValue * 3600000;
                        }
                        else if (latencyMetricValueStr.EndsWith('m'))
                        {
                            // minutes
                            latencyMetricValue = (double)latencyMetricValue * 60000;
                        }
                        else if (latencyMetricValueStr.EndsWith("ms"))
                        {
                            // milliseconds
                            latencyMetricValue = (double)latencyMetricValue * 1;
                        }
                        else if (latencyMetricValueStr.EndsWith("us"))
                        {
                            // microseconds
                            latencyMetricValue = (double)latencyMetricValue * 0.001;
                        }
                        else if (latencyMetricValueStr.EndsWith("ns"))
                        {
                            // nano seconds. likely won't come to this.
                            latencyMetricValue = (double)latencyMetricValue * 0.000001;
                        }
                        else if (latencyMetricValueStr.EndsWith("s"))
                        {
                            // seconds
                            latencyMetricValue = (double)latencyMetricValue * 1000;
                        }

                        metrics.Add(new Metric(name: latencyMetricName, value: latencyMetricValue, unit: MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter, tags: null, description: description));
                    }
                }
            }

            return metrics;
        }

        private IList<Metric> ParseRequestAndTransferStats()
        {
            List<Metric> metrics = new List<Metric>();
            string sectionName = "Requests/sec:";

            DataTable table = DataTableExtensions.ConvertToDataTable(this.Sections[sectionName], new Regex(@":", RegexOptions.ExplicitCapture), sectionName, columnNames: new List<string>() { "title", "value" });

            /*
             * Requests/sec: 16305.17
             * Transfer/sec: 20.01MB
             */

            foreach (DataRow row in table.Rows)
            {
                string metricName = Convert.ToString(row[0]).Trim();
                string metricValueStr = Convert.ToString(row[1]).Trim();
                string units = string.Empty;

                if (metricName.Contains("transfer", StringComparison.OrdinalIgnoreCase))
                {
                    metricValueStr = metricValueStr.TrimEnd('B'); // 45.63B fails to translate.
                    metricValueStr = TextParsingExtensions.TranslateStorageByUnit(metricValueStr, MetricUnit.Megabytes);
                    units = MetricUnit.Megabytes;
                    metricName = "transfers/sec";
                }
                else if (metricName.Contains("Requests", StringComparison.OrdinalIgnoreCase))
                {
                    metricName = metricName.ToLower();
                }

                if (!double.TryParse(metricValueStr, out double metricValue))
                {
                    throw new SchemaException(
                        $"Unable to parse the results from {this.GetType().Name}. The results has incorrect metric value format for parsing. " +
                        $"Results table contains {metricName} = {metricValueStr}. SectionName: {sectionName}");
                }

                metrics.Add(new Metric(metricName, metricValue, unit: units, MetricRelativity.HigherIsBetter));
            }

            return metrics;
        }

        private IList<Metric> ParseLatencySpectrum()
        {
            // Latency Spectrum is useful to view HdrHistogram. https://hdrhistogram.github.io/HdrHistogram/plotFiles.html
            // LatencyDistribution is good enough for most cases.

            List<Metric> metrics = new List<Metric>();

            /*
              Detailed Percentile Spectrum:
                   Value   Percentile   TotalCount 1/(1-Percentile)

                8372.223     0.000000            2         1.00
               12525.567     0.100000        81614         1.11
               16605.183     0.200000       163195         1.25
               20660.223     0.300000       244674         1.43
               24805.375     0.400000       326214         1.67
               29556.735     0.500000       407688         2.00
            */

            string sectionName = "Percentile spectrum";

            foreach (KeyValuePair<string, string> kvp in this.Sections)
            {
                if (kvp.Key.Contains(sectionName))
                {
                    DataTable table = DataTableExtensions.ConvertToDataTable(this.Sections[kvp.Key], WrkMetricParser.WRK2DataTableDelimiter, sectionName, columnNames: null);

                    bool isUncorrected = kvp.Key.Contains("Uncorrected", StringComparison.OrdinalIgnoreCase);
                    string metricNamePrefix = isUncorrected ? "uncorrected_latency_spectrum_p" : "latency_spectrum_p";

                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        DataRow dataRow = table.Rows[i];
                        string percentileValue = Convert.ToString(dataRow[0]).Trim();
                        string percentileName = Convert.ToString(dataRow[1]).Trim().Replace("0.", "0_").Replace("1.", "1_");
                        string details = $"TotalCount:{Convert.ToString(dataRow[2]).Trim()}";

                        if (percentileName.Contains("Percentile", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }
                        else if (double.TryParse(percentileValue, out double value))
                        {
                            metrics.Add(new Metric(name: $"{metricNamePrefix}{percentileName}", value: value, MetricRelativity.LowerIsBetter, tags: null, description: details));
                        }
                        else
                        {
                            throw new SchemaException(
                                $"Unable to parse results from {this.GetType().Name}. The results has incorrect metric value format for parsing. " +
                                $"Results table contains {percentileName} = {percentileValue}. SectionName: {sectionName}");
                        }
                    }
                }
            }

            return metrics;
        }
    }
}