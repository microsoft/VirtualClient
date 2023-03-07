// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;
    using static System.Collections.Specialized.BitVector32;
    using DataTableExtensions = VirtualClient.Contracts.DataTableExtensions;

    /// <summary>
    /// Parser for Linpack test results file.
    /// </summary>
    public class HPLMetricsParser : MetricsParser
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
        /// constructor for <see cref="HPLMetricsParser"/>.
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public HPLMetricsParser(string rawText)
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

                    this.Metrics.Add(new Metric($"N_WR{st[0]}", Convert.ToDouble(st[1])));
                    this.Metrics.Add(new Metric($"NB_WR{st[0]}", Convert.ToDouble(st[2])));
                    this.Metrics.Add(new Metric($"P_WR{st[0]}", Convert.ToDouble(st[3])));
                    this.Metrics.Add(new Metric($"Q_WR{st[0]}", Convert.ToDouble(st[4])));
                    this.Metrics.Add(new Metric($"Time_WR{st[0]}", Convert.ToDouble(st[5])));
                    this.Metrics.Add(new Metric($"GFlops_WR{st[0]}", Convert.ToDouble(st[6])));
                }
            }

            return this.Metrics;
        }
    }
}
