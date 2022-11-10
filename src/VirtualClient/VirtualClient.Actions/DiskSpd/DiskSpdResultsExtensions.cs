// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for working with DiskSpd.exe results.
    /// </summary>
    /// <remarks>
    /// DiskSpd Overview
    /// https://docs.microsoft.com/en-us/azure-stack/hci/manage/diskspd-overview
    /// </remarks>
    public static class DiskSpdResultsExtensions
    {
        private const string LineBreak = "[\r\n]+";
        private static readonly Regex NumericExpression = new Regex(@"[0-9\.]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex WhitespaceExpression = new Regex("\x20", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Extension adds the CPU utilization measurements to the set of test results.
        /// </summary>
        /// <param name="results">A set of metrics that represents the test results from the DiskSpd.exe execution.</param>
        /// <param name="resultsText">The DiskSpd.exe raw test results output in basic text format.</param>
        public static IList<Metric> AddCpuUtilizationMetrics(this IList<Metric> results, string resultsText)
        {
            /* Example Format:
            CPU |  Usage |  User  |  Kernel |  Idle
            -------------------------------------------
               0|   3.02%|   0.36%|    2.65%|  96.98%
               1|   0.05%|   0.05%|    0.00%|  99.95%
               2|   0.05%|   0.00%|    0.05%|  99.95%
               3|   0.16%|   0.00%|    0.16%|  99.84%
            -------------------------------------------
            avg.|   0.82%|   0.10%|    0.72%|  99.18%  <-- We want to parse this line
            */

            Match match = Regex.Match(
                DiskSpdResultsExtensions.WithoutSpaces(resultsText),
                @"avg\.[0-9\.%\|\x20]+",
                RegexOptions.IgnoreCase);

            if (match.Success)
            {
                // Match Example:
                // avg.|   0.82%|   0.10%|    0.72%|  99.18%
                IEnumerable<string> averages = match.Value.Split('|', StringSplitOptions.RemoveEmptyEntries).Select(entry => entry.Trim());

                if (averages != null && averages.Count() == 5)
                {
                    // Averages across all CPU cores.
                    results.Add(new Metric("avg % cpu usage", DiskSpdResultsExtensions.ParseNumericValue(averages.ElementAt(1))));
                    results.Add(new Metric("avg % cpu usage(user mode)", DiskSpdResultsExtensions.ParseNumericValue(averages.ElementAt(2))));
                    results.Add(new Metric("avg % cpu usage(kernel mode)", DiskSpdResultsExtensions.ParseNumericValue(averages.ElementAt(3))));
                    results.Add(new Metric("avg % cpu idle", DiskSpdResultsExtensions.ParseNumericValue(averages.ElementAt(4))));
                }
            }

            return results;
        }

        /// <summary>
        /// Extension adds the disk I/O latency measurements to the set of test results.
        /// </summary>
        /// <param name="results">A set of metrics that represents the test results from the DiskSpd.exe execution.</param>
        /// <param name="resultsText">The DiskSpd.exe raw test results output in basic text format.</param>
        public static IList<Metric> AddDiskIOMetrics(this IList<Metric> results, string resultsText)
        {
            /* Example Format:
             Total IO
             thread |       bytes     |     I/Os     |    MiB/s   |  I/O per s |  AvgLat  | IopsStdDev | LatStdDev |  file
             ------------------------------------------------------------------------------------------------------------------
                  0 |       146169856 |        35686 |       4.64 |    1189.03 |    0.840 |     129.18 |     1.051 | diskspd.dat (1024MiB)
             ------------------------------------------------------------------------------------------------------------------
             total:         146169856 |        35686 |       4.64 |    1189.03 |    0.840 |     129.18 |     1.051

             Read IO
             thread |       bytes     |     I/Os     |    MiB/s   |  I/O per s |  AvgLat  | IopsStdDev | LatStdDev |  file
             ------------------------------------------------------------------------------------------------------------------
                  0 |       102645760 |        25060 |       3.26 |     834.98 |    0.272 |      94.24 |     0.185 | diskspd.dat (1024MiB)
             ------------------------------------------------------------------------------------------------------------------
             total:         102645760 |        25060 |       3.26 |     834.98 |    0.272 |      94.24 |     0.185

             Write IO
             thread |       bytes     |     I/Os     |    MiB/s   |  I/O per s |  AvgLat  | IopsStdDev | LatStdDev |  file
             ------------------------------------------------------------------------------------------------------------------
                  0 |        43524096 |        10626 |       1.38 |     354.05 |    2.179 |      38.48 |     1.037 | diskspd.dat (1024MiB)
             ------------------------------------------------------------------------------------------------------------------
             total:          43524096 |        10626 |       1.38 |     354.05 |    2.179 |      38.48 |     1.037
            */

            MatchCollection matches = Regex.Matches(
                DiskSpdResultsExtensions.WithoutSpaces(resultsText),
                @"total\:[0-9\.\|\x20]+",
                RegexOptions.IgnoreCase);

            if (matches.Any())
            {
                // Match 1 = Total IO
                //        bytes     | I/Os  | MiB/s | I/O per s | AvgLat | IopsStdDev | LatStdDev
                // total: 146169856 | 35686 | 4.64  | 1189.03   | 0.840  | 129.18     | 1.051
                IEnumerable<string> totalIO = matches[0].Value.Replace("total:", string.Empty)
                    .Split('|', StringSplitOptions.RemoveEmptyEntries).Select(entry => entry.Trim());

                if (totalIO != null && totalIO.Count() == 7)
                {
                    // Averages across all CPU cores.
                    results.Add(new Metric("total bytes", DiskSpdResultsExtensions.ParseNumericValue(totalIO.ElementAt(0)), MetricUnit.Bytes, MetricRelativity.HigherIsBetter));
                    results.Add(new Metric("total IO operations", DiskSpdResultsExtensions.ParseNumericValue(totalIO.ElementAt(1)), MetricUnit.Operations, MetricRelativity.HigherIsBetter));
                    results.Add(new Metric("total throughput", DiskSpdResultsExtensions.ParseNumericValue(totalIO.ElementAt(2)), MetricUnit.MebibytesPerSecond, MetricRelativity.HigherIsBetter));
                    results.Add(new Metric("total iops", DiskSpdResultsExtensions.ParseNumericValue(totalIO.ElementAt(3)), MetricUnit.OperationsPerSec, MetricRelativity.HigherIsBetter));
                    results.Add(new Metric("avg. latency", DiskSpdResultsExtensions.ParseNumericValue(totalIO.ElementAt(4)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("iops stdev", DiskSpdResultsExtensions.ParseNumericValue(totalIO.ElementAt(5)), MetricUnit.OperationsPerSec, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("latency stdev", DiskSpdResultsExtensions.ParseNumericValue(totalIO.ElementAt(6)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                }

                // Match 2 = Read IO
                //        bytes     | I/Os  | MiB/s | I/O per s | AvgLat | IopsStdDev | LatStdDev
                // total: 102645760 | 25060 | 3.26  | 834.98    | 0.272  | 94.24      | 0.185
                IEnumerable<string> readIO = matches[1].Value.Replace("total:", string.Empty)
                    .Split('|', StringSplitOptions.RemoveEmptyEntries).Select(entry => entry.Trim());

                if (readIO != null && readIO.Count() == 7)
                {
                    // Averages across all CPU cores.
                    results.Add(new Metric("read total bytes", DiskSpdResultsExtensions.ParseNumericValue(readIO.ElementAt(0)), MetricUnit.Bytes, MetricRelativity.HigherIsBetter));
                    results.Add(new Metric("read IO operations", DiskSpdResultsExtensions.ParseNumericValue(readIO.ElementAt(1)), MetricUnit.Operations, MetricRelativity.HigherIsBetter));
                    results.Add(new Metric("read throughput", DiskSpdResultsExtensions.ParseNumericValue(readIO.ElementAt(2)), MetricUnit.MebibytesPerSecond, MetricRelativity.HigherIsBetter));
                    results.Add(new Metric("read iops", DiskSpdResultsExtensions.ParseNumericValue(readIO.ElementAt(3)), MetricUnit.OperationsPerSec, MetricRelativity.HigherIsBetter));
                    results.Add(new Metric("read avg. latency", DiskSpdResultsExtensions.ParseNumericValue(readIO.ElementAt(4)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("read iops stdev", DiskSpdResultsExtensions.ParseNumericValue(readIO.ElementAt(5)), MetricUnit.OperationsPerSec, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("read latency stdev", DiskSpdResultsExtensions.ParseNumericValue(readIO.ElementAt(6)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                }

                // Match 3 = Write IO
                //        bytes    | I/Os  | MiB/s | I/O per s | AvgLat | IopsStdDev | LatStdDev
                // total: 43524096 | 10626 | 1.38 | 354.05     | 2.179  | 38.48      | 1.037
                IEnumerable<string> writeIO = matches[2].Value.Replace("total:", string.Empty)
                   .Split('|', StringSplitOptions.RemoveEmptyEntries).Select(entry => entry.Trim());

                if (writeIO != null && writeIO.Count() == 7)
                {
                    // Averages across all CPU cores.
                    results.Add(new Metric("write total bytes", DiskSpdResultsExtensions.ParseNumericValue(writeIO.ElementAt(0)), MetricUnit.Bytes, MetricRelativity.HigherIsBetter));
                    results.Add(new Metric("write IO operations", DiskSpdResultsExtensions.ParseNumericValue(writeIO.ElementAt(1)), MetricUnit.Operations, MetricRelativity.HigherIsBetter));
                    results.Add(new Metric("write throughput", DiskSpdResultsExtensions.ParseNumericValue(writeIO.ElementAt(2)), MetricUnit.MebibytesPerSecond, MetricRelativity.HigherIsBetter));
                    results.Add(new Metric("write iops", DiskSpdResultsExtensions.ParseNumericValue(writeIO.ElementAt(3)), MetricUnit.OperationsPerSec, MetricRelativity.HigherIsBetter));
                    results.Add(new Metric("write avg. latency", DiskSpdResultsExtensions.ParseNumericValue(writeIO.ElementAt(4)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("write iops stdev", DiskSpdResultsExtensions.ParseNumericValue(writeIO.ElementAt(5)), MetricUnit.OperationsPerSec, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("write latency stdev", DiskSpdResultsExtensions.ParseNumericValue(writeIO.ElementAt(6)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                }
            }

            return results;
        }

        /// <summary>
        /// Extension adds the disk I/O latency measurements to the set of test results.
        /// </summary>
        /// <param name="results">A set of metrics that represents the test results from the DiskSpd.exe execution.</param>
        /// <param name="resultsText">The DiskSpd.exe raw test results output in basic text format.</param>
        public static IList<Metric> AddDiskIOPercentileMetrics(this IList<Metric> results, string resultsText)
        {
            /*
             total:
              %-ile |  Read (ms) | Write (ms) | Total (ms)
            ----------------------------------------------
                min |      0.036 |      1.209 |      0.036
               25th |      0.175 |      1.607 |      0.198
               50th |      0.232 |      1.833 |      0.297
               75th |      0.314 |      2.308 |      1.538
               90th |      0.458 |      3.378 |      2.065
               95th |      0.588 |      4.200 |      2.752
               99th |      0.877 |      6.107 |      4.713
            3-nines |      1.608 |      8.378 |      7.432
            4-nines |      4.753 |     11.561 |     10.489
            5-nines |      7.990 |     35.257 |     35.257
            6-nines |      7.990 |     35.257 |     35.257
            7-nines |      7.990 |     35.257 |     35.257
            8-nines |      7.990 |     35.257 |     35.257
            9-nines |      7.990 |     35.257 |     35.257
                max |      7.990 |     35.257 |     35.257
             */

            // (50th|75th|90th|95th|99th)[0-9\.\|\x20]+
            MatchCollection matches = Regex.Matches(
               DiskSpdResultsExtensions.WithoutSpaces(resultsText),
               @"(50th|75th|90th|95th|99th)[0-9\.\|\x20]+",
               RegexOptions.IgnoreCase);

            if (matches.Any())
            {
                // 50th Percentile
                IEnumerable<string> percentiles50 = matches[0].Value.Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(entry => entry.Trim());

                if (percentiles50?.Count() == 4)
                {
                    results.Add(new Metric("read latency/operation(P50)", DiskSpdResultsExtensions.ParseNumericValue(percentiles50.ElementAt(1)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("write latency/operation(P50)", DiskSpdResultsExtensions.ParseNumericValue(percentiles50.ElementAt(2)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("total latency/operation(P50)", DiskSpdResultsExtensions.ParseNumericValue(percentiles50.ElementAt(3)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                }

                // 75th Percentile
                IEnumerable<string> percentiles75 = matches[1].Value.Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(entry => entry.Trim());

                if (percentiles75?.Count() == 4)
                {
                    results.Add(new Metric("read latency/operation(P75)", DiskSpdResultsExtensions.ParseNumericValue(percentiles75.ElementAt(1)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("write latency/operation(P75)", DiskSpdResultsExtensions.ParseNumericValue(percentiles75.ElementAt(2)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("total latency/operation(P75)", DiskSpdResultsExtensions.ParseNumericValue(percentiles75.ElementAt(3)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                }

                // 90th Percentile
                IEnumerable<string> percentiles90 = matches[2].Value.Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(entry => entry.Trim());

                if (percentiles90?.Count() == 4)
                {
                    results.Add(new Metric("read latency/operation(P90)", DiskSpdResultsExtensions.ParseNumericValue(percentiles90.ElementAt(1)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("write latency/operation(P90)", DiskSpdResultsExtensions.ParseNumericValue(percentiles90.ElementAt(2)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("total latency/operation(P90)", DiskSpdResultsExtensions.ParseNumericValue(percentiles90.ElementAt(3)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                }

                // 95th Percentile
                IEnumerable<string> percentiles95 = matches[3].Value.Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(entry => entry.Trim());

                if (percentiles95?.Count() == 4)
                {
                    results.Add(new Metric("read latency/operation(P95)", DiskSpdResultsExtensions.ParseNumericValue(percentiles95.ElementAt(1)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("write latency/operation(P95)", DiskSpdResultsExtensions.ParseNumericValue(percentiles95.ElementAt(2)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("total latency/operation(P95)", DiskSpdResultsExtensions.ParseNumericValue(percentiles95.ElementAt(3)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                }

                // 99th Percentile
                IEnumerable<string> percentiles99 = matches[4].Value.Split('|', StringSplitOptions.RemoveEmptyEntries)
                    .Select(entry => entry.Trim());

                if (percentiles99?.Count() == 4)
                {
                    results.Add(new Metric("read latency/operation(P99)", DiskSpdResultsExtensions.ParseNumericValue(percentiles99.ElementAt(1)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("write latency/operation(P99)", DiskSpdResultsExtensions.ParseNumericValue(percentiles99.ElementAt(2)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                    results.Add(new Metric("total latency/operation(P99)", DiskSpdResultsExtensions.ParseNumericValue(percentiles99.ElementAt(3)), MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter));
                }
            }

            return results;
        }

        private static double ParseNumericValue(string text)
        {
            double value = 0.0;
            Match match = DiskSpdResultsExtensions.NumericExpression.Match(text);
            if (match.Success)
            {
                value = double.Parse(match.Value);
            }

            return value;
        }

        private static IEnumerable<string> SplitOnLineBreaks(string text)
        {
            return text.Split(Environment.NewLine);
        }

        private static string WithoutSpaces(string text)
        {
            return DiskSpdResultsExtensions.WhitespaceExpression.Replace(text, string.Empty);
        }
    }
}