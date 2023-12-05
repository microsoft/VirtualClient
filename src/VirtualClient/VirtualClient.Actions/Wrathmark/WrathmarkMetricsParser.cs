// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    internal class WrathmarkMetricsParser : MetricsParser
    {
        public WrathmarkMetricsParser(string results)
            : base(results)
        {
        }

        public override IList<Metric> Parse()
        {
            const string pattern = @"Evaluated ([\d,]+) boards in [\d:.]+ ([\d,]+(\.\d+)?) boards/sec";
            Regex regex = new Regex(pattern);

            List<Metric> metrics = new List<Metric>(64);

            foreach (Match match in regex.Matches(this.RawText))
            {
                double boardsPerSec = double.Parse(match.Groups[2].Value);
                metrics.Add(new Metric("BoardsPerSecond", boardsPerSec, MetricRelativity.HigherIsBetter));
            }

            return metrics;
        }
    }
}