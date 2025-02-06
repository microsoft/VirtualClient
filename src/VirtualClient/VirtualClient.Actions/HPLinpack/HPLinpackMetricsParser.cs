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
        /// To check if the system is AMD.
        /// </summary>
        private bool isAMD = false;

        private List<Metric> metrics;

        /// <summary>
        /// constructor for <see cref="HPLinpackMetricsParser"/>.
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        /// <param name="isAMD">To check if the system is AMD.</param>
        public HPLinpackMetricsParser(string rawText, bool isAMD = false)
            : base(rawText)
        {
            this.isAMD = isAMD;
        }

        /// <summary>
        /// Results for Linpack results .
        /// </summary>
        public DataTable LinpackResult { get; set; }

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
                this.metrics = new List<Metric>();
                for (int i = 0; i < matches.Count; i++)
                {
                    var st = Regex.Split(matches.ElementAt(i).Value, SplitAtSpace);

                    if (this.isAMD)
                    {
                        Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>()
                         {
                             { $"N_WR{st[0]}", st[2] },
                             { $"NB_WR{st[0]}", st[3] },
                             { $"P_WR{st[0]}", st[4] },
                             { $"Q_WR{st[0]}", st[5] },
                         };

                        this.metrics.Add(new Metric($"Time", Convert.ToDouble(st[6]), "secs", metadata: metadata));
                        this.metrics.Add(new Metric($"GFlops", Convert.ToDouble(st[7]), "Gflops", metadata: metadata));
                    }
                    else
                    {
                        Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>()
                         {
                             { $"N_WR{st[0]}", st[1] },
                             { $"NB_WR{st[0]}", st[2] },
                             { $"P_WR{st[0]}", st[3] },
                             { $"Q_WR{st[0]}", st[4] },
                         };

                        this.metrics.Add(new Metric($"Time", Convert.ToDouble(st[5]), "secs", metadata: metadata));
                        this.metrics.Add(new Metric($"GFlops", Convert.ToDouble(st[6]), "Gflops", metadata: metadata));
                    }
                }
            }

            return this.metrics;
        }
    }
}
