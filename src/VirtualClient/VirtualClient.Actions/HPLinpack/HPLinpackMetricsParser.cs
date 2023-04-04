// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Linpack test results file.
    /// </summary>
    public class HPLinpackMetricsParser : MetricsParser
    {
        /// <summary>
        /// To match metrics line of the result.
        /// </summary>
        private const string GetMetricsLine = @"(?<=WR).*(?=\n)";

        /// <summary>
        /// Split string at one or more spaces.
        /// </summary>
        private const string SplitAtSpace = @"\s{1,}";

        /// <summary>
        /// constructor for <see cref="HPLinpackMetricsParser"/>.
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public HPLinpackMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <summary>
        /// Results for Linpack results .
        /// </summary>
        public DataTable LinpackResult { get; set; }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            var matches = Regex.Matches(this.RawText, GetMetricsLine);
            if (matches.Count == 0)
            {
                throw new SchemaException("The HPLinpack output file has incorrect format for parsing");
            }
            else
            {
                this.Metrics = new List<Metric>();
                for (int i = 0; i < matches.Count; i++)
                {
                    var st = Regex.Split(matches.ElementAt(i).Value, SplitAtSpace);

                    Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>()
                         {
                             { $"N_WR{st[0]}", st[1] },
                             { $"NB_WR{st[0]}", st[2] },
                             { $"P_WR{st[0]}", st[3] },
                             { $"Q_WR{st[0]}", st[4] },
                         };

                    this.Metrics.Add(new Metric($"Time", Convert.ToDouble(st[5]), "secs", metadata: metadata));
                    this.Metrics.Add(new Metric($"GFlops", Convert.ToDouble(st[6]), "Gflops", metadata: metadata));
                }
            }

            return this.Metrics;
        }
    }
}
