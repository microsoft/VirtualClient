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
        private List<Metric> metrics;

        /// <summary>
        /// Constructor for <see cref="CtsTrafficMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public CtsTrafficMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            try
            {
                this.Preprocess();
                this.metrics = new List<Metric>();
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
                            // bytes/sec that were sent within the TimeSlice period
                            this.AddMetric(new Metric($"SendBps(TimeSlice-{values[0].Trim()})", Convert.ToDouble(values[1]), "B/s", MetricRelativity.Undefined));

                            // bytes/sec that were received within the TimeSlice period
                            this.AddMetric(new Metric($"RecvBps(TimeSlice-{values[0].Trim()})", Convert.ToDouble(values[2]), "B/s", MetricRelativity.Undefined));

                            // count of established connections transmitting IO pattern data
                            this.AddMetric(new Metric($"InFlight(TimeSlice-{values[0].Trim()})", Convert.ToDouble(values[3]), MetricRelativity.Undefined));

                            // cumulative count of successfully completed IO patterns
                            this.AddMetric(new Metric($"Completed(TimeSlice-{values[0].Trim()})", Convert.ToDouble(values[4]), MetricRelativity.HigherIsBetter));

                            // cumulative count of failed IO patterns due to Winsock errors
                            this.AddMetric(new Metric($"NetworkError(TimeSlice-{values[0].Trim()})", Convert.ToDouble(values[5]), MetricRelativity.LowerIsBetter));

                            // cumulative count of failed IO patterns due to data errors
                            this.AddMetric(new Metric($"DataError(TimeSlice-{values[0].Trim()})", Convert.ToDouble(values[6]), MetricRelativity.LowerIsBetter));
                        }
                    }
                }

                return this.metrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse CtsTraffic metrics from results.", exc, ErrorReason.InvalidResults);
            }
        }

        private void AddMetric(Metric metric)
        {
            try
            {
                this.metrics.Add(metric);
            }
            catch
            {
                // do nothing as this result file has non-double values.
            }
        }
    }
}