// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for CtsTraffic output document
    /// </summary>
    public class CtsTrafficMetricsParser : MetricsParser
    {
        /// <summary>
        /// Constructor for <see cref="CtsTrafficMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public CtsTrafficMetricsParser(string rawText)
            : base(rawText)
        {
        }

        private List<Metric> Metrics { get; set; }

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
                                // bytes/sec that were sent within the TimeSlice period
                                this.Metrics.Add(new Metric($"SendBps(TimeSlice-{values[0].Trim()})", Convert.ToDouble(values[1]), "B/s", MetricRelativity.Undefined));
                            }
                            catch
                            {
                                // do nothing as this result file has non-double values.
                            }

                            try
                            {
                                // bytes/sec that were received within the TimeSlice period
                                this.Metrics.Add(new Metric($"RecvBps(TimeSlice-{values[0].Trim()})", Convert.ToDouble(values[2]), "B/s", MetricRelativity.Undefined));
                            }
                            catch
                            {
                                // do nothing as this result file has non-double values.
                            }

                            try
                            {
                                // count of established connections transmitting IO pattern data
                                this.Metrics.Add(new Metric($"InFlight(TimeSlice-{values[0].Trim()})", Convert.ToDouble(values[3]), MetricRelativity.Undefined));
                            }
                            catch
                            {
                                // do nothing as this result file has non-double values.
                            }

                            try
                            {
                                // cumulative count of successfully completed IO patterns
                                this.Metrics.Add(new Metric($"Completed(TimeSlice-{values[0].Trim()})", Convert.ToDouble(values[4]), MetricRelativity.HigherIsBetter));
                            }
                            catch
                            {
                                // do nothing as this result file has non-double values.
                            }

                            try
                            {
                                // cumulative count of failed IO patterns due to Winsock errors
                                this.Metrics.Add(new Metric($"NetworkError(TimeSlice-{values[0].Trim()})", Convert.ToDouble(values[5]), MetricRelativity.LowerIsBetter));
                            }
                            catch
                            {
                                // do nothing as this result file has non-double values.
                            }

                            try
                            {
                                // cumulative count of failed IO patterns due to data errors
                                this.Metrics.Add(new Metric($"DataError(TimeSlice-{values[0].Trim()})", Convert.ToDouble(values[6]), MetricRelativity.LowerIsBetter));
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
                throw new WorkloadResultsException("Failed to parse CtsTraffic metrics from results.", exc, ErrorReason.InvalidResults);
            }
        }
    }
}