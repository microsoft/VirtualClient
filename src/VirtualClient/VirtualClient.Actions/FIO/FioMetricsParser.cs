// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides functionality for parsing metrics from FIO results.
    /// </summary>
    public class FioMetricsParser : MetricsParser
    {
        private const double NanosecondsToMilliseconds = 0.000001;
        private IList<Metric> resultingMetrics;

        /// <summary>
        /// Initializes a new instance of the <see cref="FioMetricsParser"/> class.
        /// </summary>
        /// <param name="results">Raw text to parse.</param>
        /// <param name="parseDataIntegrityErrors">True/false whether to parse data integrity verification errors.</param>
        public FioMetricsParser(string results, bool parseDataIntegrityErrors)
            : base(results)
        {
            this.resultingMetrics = new List<Metric>();
            this.ParseDataIntegrityMetrics = parseDataIntegrityErrors;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FioMetricsParser"/> class.
        /// </summary>
        /// <param name="results">Raw text to parse.</param>
        /// <param name="parseReadMetrics">True/false whether to parse read metrics.</param>
        /// <param name="parseWriteMetrics">True/false whether to parse write metrics.</param>
        /// <param name="conversionUnits">The conversion factor to apply to FIO results which are typically represented in nanoseconds.</param>
        public FioMetricsParser(string results, bool parseReadMetrics, bool parseWriteMetrics, string conversionUnits = MetricUnit.Milliseconds)
            : base(results)
        {
            this.resultingMetrics = new List<Metric>();
            this.ParseReadMetrics = parseReadMetrics;
            this.ParseWriteMetrics = parseWriteMetrics;

            switch (conversionUnits)
            {
                case MetricUnit.Milliseconds:
                    this.ConversionFactor = FioMetricsParser.NanosecondsToMilliseconds;
                    this.ConversionUnits = conversionUnits;
                    break;

                default:
                    // The default conversion factor is no conversion at all.
                    this.ConversionFactor = 1.0;
                    this.ConversionUnits = MetricUnit.Nanoseconds;
                    break;
            }
        }

        /// <summary>
        /// The conversion factor to apply to FIO results which are typically represented in nanoseconds. 
        /// Note that this is for backwards compatibility only. In the future, the metrics will all be
        /// converted to milliseconds.
        /// </summary>
        public double ConversionFactor { get; }

        /// <summary>
        /// The metric units to use for conversions (e.g. nanoseconds, milliseconds). Note that this is for 
        /// backwards compatibility only. In the future, the metrics will all be converted to milliseconds.
        /// </summary>
        public string ConversionUnits { get; }

        /// <summary>
        /// True/false whether results for read operations should be parsed.
        /// </summary>
        public bool ParseReadMetrics { get; }

        /// <summary>
        /// True/false whether results for write operations should be parsed.
        /// </summary>
        public bool ParseWriteMetrics { get; }

        /// <summary>
        /// True/false whether results for disk integrity/verification operations
        /// should be parsed.
        /// </summary>
        public bool ParseDataIntegrityMetrics { get; }

        /// <summary>
        /// Reads the set of metrics from the FIO results.
        /// </summary>
        public override IList<Metric> Parse()
        {
            List<Metric> metrics = new List<Metric>();

            if (this.ParseDataIntegrityMetrics)
            {
                this.ParseDataIntegrityVerificationMetrics(metrics);
            }
            else
            {
                this.ParseReadWriteMetrics(metrics);
                this.ParseFioVersion(metrics);
            }

            return metrics;
        }

        private void AddLatencyHistogramMeasurements(IList<Metric> metrics, JToken resultsJson)
        {
            // The logic will add the metric ONLY if it is found. In order to support different versions of
            // FIO, it is ok to add in duplicates below differing only by the JSON property name. Only the one
            // found will be added. The others will be ignored.
            //
            // The logic will step through all objects in the 'jobs' array. The paths noted below are relevant to
            // the individual job object.
            JArray jobs = resultsJson.SelectToken("jobs") as JArray;
            if (jobs != null)
            {
                foreach (JToken job in jobs)
                {
                    this.AddMeasurement(metrics, job, $"latency_us.2", "h000000_002", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_us.4", "h000000_004", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_us.10", "h000000_010", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_us.20", "h000000_020", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_us.50", "h000000_050", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_us.100", "h000000_100", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_us.250", "h000000_250", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_us.500", "h000000_500", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_us.750", "h000000_750", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_us.1000", "h000001_000", verbosity: 2);

                    this.AddMeasurement(metrics, job, $"latency_ms.2", "h000002_000", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_ms.4", "h000004_000", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_ms.10", "h000010_000", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_ms.20", "h000020_000", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_ms.50", "h000050_000", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_ms.100", "h000100_000", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_ms.250", "h000250_000", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_ms.500", "h000500_000", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_ms.750", "h000750_000", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_ms.1000", "h001000_000", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_ms.2000", "h002000_000", verbosity: 2);
                    this.AddMeasurement(metrics, job, $"latency_ms.['>=2000']", "hgt002000_000", verbosity: 2);
                }
            }
        }

        private void AddReadMeasurements(IList<Metric> metrics, JToken resultsJson)
        {
            // The logic will add the metric ONLY if it is found. In order to support different versions of
            // FIO, it is ok to add in duplicates below differing only by the JSON property name. Only the one
            // found will be added. The others will be ignored.
            //
            // The logic will step through all objects in the 'jobs' array. The paths noted below are relevant to
            // the individual job object.
            JArray jobs = resultsJson.SelectToken("jobs") as JArray;
            if (jobs != null)
            {
                foreach (JToken job in jobs)
                {
                    this.AddMeasurement(metrics, job, $"read.io_bytes", "read_bytes", null, MetricRelativity.HigherIsBetter, verbosity: 2);
                    this.AddMeasurement(metrics, job, $"read.total_ios", "read_ios", null, MetricRelativity.HigherIsBetter, verbosity: 2);
                    this.AddMeasurement(metrics, job, $"read.short_ios", "read_ios_short", null, MetricRelativity.LowerIsBetter, verbosity: 2);
                    this.AddMeasurement(metrics, job, $"read.drop_ios", "read_ios_dropped", null, MetricRelativity.LowerIsBetter, verbosity: 2);

                    this.AddMeasurement(metrics, job, $"read.bw", "read_bandwidth", MetricUnit.KilobytesPerSecond, MetricRelativity.HigherIsBetter);
                    this.AddMeasurement(metrics, job, $"read.bw_min", "read_bandwidth_min", MetricUnit.KilobytesPerSecond, MetricRelativity.HigherIsBetter);
                    this.AddMeasurement(metrics, job, $"read.bw_max", "read_bandwidth_max", MetricUnit.KilobytesPerSecond, MetricRelativity.HigherIsBetter);
                    this.AddMeasurement(metrics, job, $"read.bw_mean", "read_bandwidth_mean", MetricUnit.KilobytesPerSecond, MetricRelativity.HigherIsBetter, verbosity: 0);
                    this.AddMeasurement(metrics, job, $"read.bw_dev", "read_bandwidth_stdev", MetricUnit.KilobytesPerSecond, MetricRelativity.LowerIsBetter, verbosity: 2);

                    this.AddMeasurement(metrics, job, $"read.iops", "read_iops", null, MetricRelativity.HigherIsBetter);
                    this.AddMeasurement(metrics, job, $"read.iops_min", "read_iops_min", null, MetricRelativity.HigherIsBetter);
                    this.AddMeasurement(metrics, job, $"read.iops_max", "read_iops_max", null, MetricRelativity.HigherIsBetter);
                    this.AddMeasurement(metrics, job, $"read.iops_mean", "read_iops_mean", null, MetricRelativity.HigherIsBetter, verbosity: 0);
                    this.AddMeasurement(metrics, job, $"read.iops_stddev", "read_iops_stdev", null, MetricRelativity.LowerIsBetter, verbosity: 2);

                    this.AddMeasurement(metrics, job, $"read.lat_ns.min", "read_latency_min", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"read.lat_ns.max", "read_latency_max", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"read.lat_ns.mean", "read_latency_mean", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"read.lat_ns.stddev", "read_latency_stdev", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor, verbosity: 2);

                    this.AddMeasurement(metrics, job, $"read.clat_ns.min", "read_completionlatency_min", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"read.clat_ns.max", "read_completionlatency_max", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"read.clat_ns.mean", "read_completionlatency_mean", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"read.clat_ns.stddev", "read_completionlatency_stdev", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor, verbosity: 2);

                    this.AddMeasurement(metrics, job, $"read.clat_ns.percentile.['50.000000']", "read_completionlatency_p50", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor, verbosity: 0);
                    this.AddMeasurement(metrics, job, $"read.clat_ns.percentile.['70.000000']", "read_completionlatency_p70", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"read.clat_ns.percentile.['90.000000']", "read_completionlatency_p90", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"read.clat_ns.percentile.['99.000000']", "read_completionlatency_p99", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor, verbosity: 0);
                    this.AddMeasurement(metrics, job, $"read.clat_ns.percentile.['99.990000']", "read_completionlatency_p99_99", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);

                    this.AddMeasurement(metrics, job, $"read.slat_ns.min", "read_submissionlatency_min", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"read.slat_ns.max", "read_submissionlatency_max", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"read.slat_ns.mean", "read_submissionlatency_mean", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"read.slat_ns.stddev", "read_submissionlatency_stdev", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor, verbosity: 2);

                }
            }
        }

        private void AddWriteMeasurements(IList<Metric> metrics, JToken resultsJson)
        {
            // The logic will add the metric ONLY if it is found. In order to support different versions of
            // FIO, it is ok to add in duplicates below differing only by the JSON property name. Only the one
            // found will be added. The others will be ignored.
            //
            // The logic will step through all objects in the 'jobs' array. The paths noted below are relevant to
            // the individual job object.
            JArray jobs = resultsJson.SelectToken("jobs") as JArray;
            if (jobs != null)
            {
                foreach (JToken job in jobs)
                {
                    this.AddMeasurement(metrics, job, $"write.io_bytes", "write_bytes", null, MetricRelativity.HigherIsBetter, verbosity: 2);
                    this.AddMeasurement(metrics, job, $"write.total_ios", "write_ios", null, MetricRelativity.HigherIsBetter, verbosity: 2);
                    this.AddMeasurement(metrics, job, $"write.short_ios", "write_ios_short", null, MetricRelativity.LowerIsBetter, verbosity: 2);
                    this.AddMeasurement(metrics, job, $"write.drop_ios", "write_ios_dropped", null, MetricRelativity.LowerIsBetter, verbosity: 2);

                    this.AddMeasurement(metrics, job, $"write.bw", "write_bandwidth", MetricUnit.KilobytesPerSecond, MetricRelativity.HigherIsBetter);
                    this.AddMeasurement(metrics, job, $"write.bw_min", "write_bandwidth_min", MetricUnit.KilobytesPerSecond, MetricRelativity.HigherIsBetter);
                    this.AddMeasurement(metrics, job, $"write.bw_max", "write_bandwidth_max", MetricUnit.KilobytesPerSecond, MetricRelativity.HigherIsBetter);
                    this.AddMeasurement(metrics, job, $"write.bw_mean", "write_bandwidth_mean", MetricUnit.KilobytesPerSecond, MetricRelativity.HigherIsBetter, verbosity: 0);
                    this.AddMeasurement(metrics, job, $"write.bw_dev", "write_bandwidth_stdev", MetricUnit.KilobytesPerSecond, MetricRelativity.LowerIsBetter, verbosity: 2);

                    this.AddMeasurement(metrics, job, $"write.iops", "write_iops", null, MetricRelativity.HigherIsBetter);
                    this.AddMeasurement(metrics, job, $"write.iops_min", "write_iops_min", null, MetricRelativity.HigherIsBetter);
                    this.AddMeasurement(metrics, job, $"write.iops_max", "write_iops_max", null, MetricRelativity.HigherIsBetter);
                    this.AddMeasurement(metrics, job, $"write.iops_mean", "write_iops_mean", null, MetricRelativity.HigherIsBetter, verbosity: 0);
                    this.AddMeasurement(metrics, job, $"write.iops_stddev", "write_iops_stdev", null, MetricRelativity.LowerIsBetter, verbosity: 2);

                    this.AddMeasurement(metrics, job, $"write.lat_ns.min", "write_latency_min", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"write.lat_ns.max", "write_latency_max", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"write.lat_ns.mean", "write_latency_mean", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"write.lat_ns.stddev", "write_latency_stdev", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor, verbosity: 2);

                    this.AddMeasurement(metrics, job, $"write.clat_ns.min", "write_completionlatency_min", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"write.clat_ns.max", "write_completionlatency_max", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"write.clat_ns.mean", "write_completionlatency_mean", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"write.clat_ns.stddev", "write_completionlatency_stdev", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor, verbosity: 2);

                    this.AddMeasurement(metrics, job, $"write.clat_ns.percentile.['50.000000']", "write_completionlatency_p50", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor, verbosity: 0);
                    this.AddMeasurement(metrics, job, $"write.clat_ns.percentile.['70.000000']", "write_completionlatency_p70", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"write.clat_ns.percentile.['90.000000']", "write_completionlatency_p90", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"write.clat_ns.percentile.['99.000000']", "write_completionlatency_p99", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor, verbosity: 0);
                    this.AddMeasurement(metrics, job, $"write.clat_ns.percentile.['99.990000']", "write_completionlatency_p99_99", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);

                    this.AddMeasurement(metrics, job, $"write.slat_ns.min", "write_submissionlatency_min", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"write.slat_ns.max", "write_submissionlatency_max", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"write.slat_ns.mean", "write_submissionlatency_mean", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor);
                    this.AddMeasurement(metrics, job, $"write.slat_ns.stddev", "write_submissionlatency_stdev", this.ConversionUnits, MetricRelativity.LowerIsBetter, this.ConversionFactor, verbosity: 2);

                }
            }
        }

        private void AddMeasurement(IList<Metric> metrics, JToken job, string path, string metricName, string metricUnit = null, MetricRelativity metricRelativity = MetricRelativity.Undefined, double? conversionFactor = null, int verbosity = 1)
        {
            var jobOptions = job.SelectToken("['job options']") as JObject;

            var metricMetaData = new Dictionary<string, IConvertible>();
            foreach (var prop in jobOptions.Properties())
            {
                metricMetaData[prop.Name] = prop.Value.ToString();
            }

            JToken jobName = job.SelectToken("jobname") as JToken;
            metricMetaData[nameof(jobName)] = jobName.ToString();

            JToken matchingToken = job.SelectToken(path);
            if (matchingToken != null)
            {
                if (double.TryParse(matchingToken.Value<string>(), out double measurementValue))
                {
                    if (conversionFactor != null)
                    {
                        measurementValue = measurementValue * conversionFactor.Value;
                    }

                    metrics.Add(new Metric(metricName, measurementValue, metricUnit, metricRelativity, verbosity: verbosity, metadata: metricMetaData));
                }
            }
        }

        private void ParseFioVersion(List<Metric> metrics)
        {
            JToken resultsJson = JObject.Parse(this.RawText);

            JToken fioVersionToken = resultsJson["fio version"];
            if (fioVersionToken != null)
            {
                var metricMetaData = new Dictionary<string, IConvertible>();
                metricMetaData["fio version"] = fioVersionToken.Value<string>();
                metrics.Add(new Metric("IsFioVersionCaptured", 1, null, MetricRelativity.Undefined, verbosity: 1, metadata: metricMetaData));
            }
        }

        private void ParseDataIntegrityVerificationMetrics(List<Metric> metrics)
        {
            int dataIntegrityErrors = 0;
            if (!string.IsNullOrWhiteSpace(this.RawText))
            {
                Regex verificationFailureExpression = new Regex(@"(verify\s+failed)|(verify:\s+bad\s+header)", RegexOptions.IgnoreCase);
                MatchCollection matches = verificationFailureExpression.Matches(this.RawText);

                dataIntegrityErrors = matches.Count();
            }

            metrics.Add(new Metric("data_integrity_errors", dataIntegrityErrors));
        }

        private void ParseReadWriteMetrics(List<Metric> metrics)
        {
            try
            {
                metrics.Clear();
                JToken resultsJson = JObject.Parse(this.RawText);

                if (this.ParseReadMetrics)
                {
                    this.AddReadMeasurements(metrics, resultsJson);
                }

                if (this.ParseWriteMetrics)
                {
                    this.AddWriteMeasurements(metrics, resultsJson);
                }

                this.AddLatencyHistogramMeasurements(metrics, resultsJson);
            }
            catch (JsonException exc)
            {
                throw new WorkloadResultsException(
                    "Unsupported results format. The FIO results provided are not JSON-formatted. This parser supports JSON-formatted results only.",
                    exc,
                    ErrorReason.InvalidResults);
            }
        }
    }
}
