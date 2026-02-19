// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using VirtualClient.Contracts;

    /// <summary>
    /// Parser for Bombardier output document
    /// Choco install command: powershell -Command "Set-ExecutionPolicy Bypass -Scope Process -Force; [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.ServicePointManager]::SecurityProtocol -bor 3072; iex ((New-Object System.Net.WebClient).DownloadString('https://community.chocolatey.org/install.ps1'))"
    /// </summary>
    public class BombardierMetricsParser : MetricsParser
    {
        private List<Metric> metrics;

        /// <summary>
        /// Constructor for <see cref="BombardierMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public BombardierMetricsParser(string rawText)
            : base(rawText)
        {
        }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.metrics = new List<Metric>();

            Root root = JsonSerializer.Deserialize<Root>(this.PreprocessedText);
            this.metrics.Add(new Metric("Latency Max", root.Result.Latency.Max, MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 5));
            this.metrics.Add(new Metric("Latency Average", root.Result.Latency.Mean, MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 1));
            this.metrics.Add(new Metric("Latency Stddev", root.Result.Latency.Stddev, MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 5));
            this.metrics.Add(new Metric("Latency P50", root.Result.Latency.Percentiles.P50, MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 1));
            this.metrics.Add(new Metric("Latency P75", root.Result.Latency.Percentiles.P75, MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 3));
            this.metrics.Add(new Metric("Latency P90", root.Result.Latency.Percentiles.P90, MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 3));
            this.metrics.Add(new Metric("Latency P95", root.Result.Latency.Percentiles.P95, MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 3));
            this.metrics.Add(new Metric("Latency P99", root.Result.Latency.Percentiles.P99, MetricUnit.Microseconds, MetricRelativity.LowerIsBetter, verbosity: 1));

            this.metrics.Add(new Metric("RequestPerSecond Max", root.Result.Rps.Max, MetricUnit.RequestsPerSec, MetricRelativity.HigherIsBetter, verbosity: 5));
            this.metrics.Add(new Metric("RequestPerSecond Average", root.Result.Rps.Mean, MetricUnit.RequestsPerSec, MetricRelativity.HigherIsBetter, verbosity: 1));
            this.metrics.Add(new Metric("RequestPerSecond Stddev", root.Result.Rps.Stddev, MetricUnit.RequestsPerSec, MetricRelativity.HigherIsBetter, verbosity: 5));
            this.metrics.Add(new Metric("RequestPerSecond P50", root.Result.Rps.Percentiles.P50, MetricUnit.RequestsPerSec, MetricRelativity.HigherIsBetter));
            this.metrics.Add(new Metric("RequestPerSecond P75", root.Result.Rps.Percentiles.P75, MetricUnit.RequestsPerSec, MetricRelativity.HigherIsBetter));
            this.metrics.Add(new Metric("RequestPerSecond P90", root.Result.Rps.Percentiles.P90, MetricUnit.RequestsPerSec, MetricRelativity.HigherIsBetter));
            this.metrics.Add(new Metric("RequestPerSecond P95", root.Result.Rps.Percentiles.P95, MetricUnit.RequestsPerSec, MetricRelativity.HigherIsBetter));
            this.metrics.Add(new Metric("RequestPerSecond P99", root.Result.Rps.Percentiles.P99, MetricUnit.RequestsPerSec, MetricRelativity.HigherIsBetter));

            return this.metrics;
        }

        private class Latency
        {
            [JsonPropertyName("mean")]
            public double Mean { get; set; }

            [JsonPropertyName("stddev")]
            public double Stddev { get; set; }

            [JsonPropertyName("max")]
            public double Max { get; set; }

            [JsonPropertyName("percentiles")]
            public Percentiles Percentiles { get; set; }
        }

        private class Percentiles
        {
            [JsonPropertyName("50")]
            public double P50 { get; set; }

            [JsonPropertyName("75")]
            public double P75 { get; set; }

            [JsonPropertyName("90")]
            public double P90 { get; set; }

            [JsonPropertyName("95")]
            public double P95 { get; set; }

            [JsonPropertyName("99")]
            public double P99 { get; set; }
        }

        private class Result
        {
            [JsonPropertyName("bytesRead")]
            public long BytesRead { get; set; }

            [JsonPropertyName("bytesWritten")]
            public long BytesWritten { get; set; }

            [JsonPropertyName("timeTakenSeconds")]
            public double TimeTakenSeconds { get; set; }

            [JsonPropertyName("req1xx")]
            public long Req1xx { get; set; }

            [JsonPropertyName("req2xx")]
            public long Req2xx { get; set; }

            [JsonPropertyName("req3xx")]
            public long Req3xx { get; set; }

            [JsonPropertyName("req4xx")]
            public long Req4xx { get; set; }

            [JsonPropertyName("req5xx")]
            public long Req5xx { get; set; }

            [JsonPropertyName("others")]
            public long Others { get; set; }

            [JsonPropertyName("latency")]
            public Latency Latency { get; set; }

            [JsonPropertyName("rps")]
            public Rps Rps { get; set; }
        }

        private class Root
        {
            [JsonPropertyName("spec")]
            public Spec Spec { get; set; }

            [JsonPropertyName("result")]
            public Result Result { get; set; }
        }

        private class Rps
        {
            [JsonPropertyName("mean")]
            public double Mean { get; set; }

            [JsonPropertyName("stddev")]
            public double Stddev { get; set; }

            [JsonPropertyName("max")]
            public double Max { get; set; }

            [JsonPropertyName("percentiles")]
            public Percentiles Percentiles { get; set; }
        }

        private class Spec
        {
            [JsonPropertyName("ConnectionCount")]
            public long ConnectionCount { get; set; }

            [JsonPropertyName("testType")]
            public string TestType { get; set; }

            [JsonPropertyName("testDurationSeconds")]
            public long TestDurationSeconds { get; set; }

            [JsonPropertyName("method")]
            public string Method { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }

            [JsonPropertyName("body")]
            public string Body { get; set; }

            [JsonPropertyName("stream")]
            public bool Stream { get; set; }

            [JsonPropertyName("timeoutSeconds")]
            public long TimeoutSeconds { get; set; }

            [JsonPropertyName("client")]
            public string Client { get; set; }
        }
    }
}
