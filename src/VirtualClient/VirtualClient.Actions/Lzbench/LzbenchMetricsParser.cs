// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Lzbench output document
    /// </summary>
    public class LzbenchMetricsParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="LzbenchMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public LzbenchMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            try
            {
                this.Preprocess();
                this.Metrics = new List<Metric>();
                MemoryStream mStrm = new MemoryStream(Encoding.UTF8.GetBytes(this.PreprocessedText));
                using (var reader = new StreamReader(mStrm))
                {
                    var header = true;

                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                        var values = line.Split(',');
                        if (header)
                        {
                            header = false;
                        }
                        else
                        {
                            try
                            {
                                // Compression speed
                                this.Metrics.Add(new Metric($"Compression Speed({values[0].Trim()})", Convert.ToDouble(values[1]), "MB/s", MetricRelativity.HigherIsBetter));
                            }
                            catch
                            {
                                // do nothing as this result file has non-double values.
                            }

                            try
                            {
                                // Decompression speed
                                this.Metrics.Add(new Metric($"Decompression Speed({values[0].Trim()})", Convert.ToDouble(values[2]), "MB/s", MetricRelativity.HigherIsBetter));
                            }
                            catch
                            {
                                // do nothing as this result file has non-double values.
                            }

                            try
                            {
                                // Compressed size/Original size
                                this.Metrics.Add(new Metric($"Compressed size and Original size ratio({values[0].Trim()})", Convert.ToDouble(values[5]), null, MetricRelativity.LowerIsBetter));
                            }
                            catch
                            {
                                // do nothing as this result file has non-double values.
                            }
                        }
                    }
                }

                return this.Metrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse LZbench metrics from results.", exc, ErrorReason.InvalidResults);
            }
        }
    }
}
