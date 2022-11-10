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
        private const string RequestPerSecond = "Reqs/sec";
        private const string Microsecond = "microsecond";

        /// <summary>
        /// Constructor for <see cref="BombardierMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public BombardierMetricsParser(string rawText)
            : base(rawText)
        {
        }

        private List<Metric> Metrics { get; set; }

        /// <inheritdoc/>
        public override IList<Metric> Parse()
        {
            this.Preprocess();
            this.Metrics = new List<Metric>();

            Root root = JsonSerializer.Deserialize<Root>(this.PreprocessedText);
            this.Metrics.Add(new Metric("Latency Max", root.Result.Latency.Max, BombardierMetricsParser.Microsecond, MetricRelativity.LowerIsBetter));
            this.Metrics.Add(new Metric("Latency Average", root.Result.Latency.Mean, BombardierMetricsParser.Microsecond, MetricRelativity.LowerIsBetter));
            this.Metrics.Add(new Metric("Latency Stddev", root.Result.Latency.Stddev, BombardierMetricsParser.Microsecond, MetricRelativity.LowerIsBetter));
            this.Metrics.Add(new Metric("Latency P50", root.Result.Latency.Percentiles.P50, BombardierMetricsParser.Microsecond, MetricRelativity.LowerIsBetter));
            this.Metrics.Add(new Metric("Latency P75", root.Result.Latency.Percentiles.P75, BombardierMetricsParser.Microsecond, MetricRelativity.LowerIsBetter));
            this.Metrics.Add(new Metric("Latency P90", root.Result.Latency.Percentiles.P90, BombardierMetricsParser.Microsecond, MetricRelativity.LowerIsBetter));
            this.Metrics.Add(new Metric("Latency P95", root.Result.Latency.Percentiles.P95, BombardierMetricsParser.Microsecond, MetricRelativity.LowerIsBetter));
            this.Metrics.Add(new Metric("Latency P99", root.Result.Latency.Percentiles.P99, BombardierMetricsParser.Microsecond, MetricRelativity.LowerIsBetter));

            this.Metrics.Add(new Metric("RequestPerSecond Max", root.Result.Rps.Max, BombardierMetricsParser.RequestPerSecond, MetricRelativity.HigherIsBetter));
            this.Metrics.Add(new Metric("RequestPerSecond Average", root.Result.Rps.Mean, BombardierMetricsParser.RequestPerSecond, MetricRelativity.HigherIsBetter));
            this.Metrics.Add(new Metric("RequestPerSecond Stddev", root.Result.Rps.Stddev, BombardierMetricsParser.RequestPerSecond, MetricRelativity.HigherIsBetter));
            this.Metrics.Add(new Metric("RequestPerSecond P50", root.Result.Rps.Percentiles.P50, BombardierMetricsParser.RequestPerSecond, MetricRelativity.HigherIsBetter));
            this.Metrics.Add(new Metric("RequestPerSecond P75", root.Result.Rps.Percentiles.P75, BombardierMetricsParser.RequestPerSecond, MetricRelativity.HigherIsBetter));
            this.Metrics.Add(new Metric("RequestPerSecond P90", root.Result.Rps.Percentiles.P90, BombardierMetricsParser.RequestPerSecond, MetricRelativity.HigherIsBetter));
            this.Metrics.Add(new Metric("RequestPerSecond P95", root.Result.Rps.Percentiles.P95, BombardierMetricsParser.RequestPerSecond, MetricRelativity.HigherIsBetter));
            this.Metrics.Add(new Metric("RequestPerSecond P99", root.Result.Rps.Percentiles.P99, BombardierMetricsParser.RequestPerSecond, MetricRelativity.HigherIsBetter));

            return this.Metrics;
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
            public int BytesRead { get; set; }

            [JsonPropertyName("bytesWritten")]
            public int BytesWritten { get; set; }

            [JsonPropertyName("timeTakenSeconds")]
            public double TimeTakenSeconds { get; set; }

            [JsonPropertyName("req1xx")]
            public int Req1xx { get; set; }

            [JsonPropertyName("req2xx")]
            public int Req2xx { get; set; }

            [JsonPropertyName("req3xx")]
            public int Req3xx { get; set; }

            [JsonPropertyName("req4xx")]
            public int Req4xx { get; set; }

            [JsonPropertyName("req5xx")]
            public int Req5xx { get; set; }

            [JsonPropertyName("others")]
            public int Others { get; set; }

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
            [JsonPropertyName("numberOfConnections")]
            public int NumberOfConnections { get; set; }

            [JsonPropertyName("testType")]
            public string TestType { get; set; }

            [JsonPropertyName("testDurationSeconds")]
            public int TestDurationSeconds { get; set; }

            [JsonPropertyName("method")]
            public string Method { get; set; }

            [JsonPropertyName("url")]
            public string Url { get; set; }

            [JsonPropertyName("body")]
            public string Body { get; set; }

            [JsonPropertyName("stream")]
            public bool Stream { get; set; }

            [JsonPropertyName("timeoutSeconds")]
            public int TimeoutSeconds { get; set; }

            [JsonPropertyName("client")]
            public string Client { get; set; }
        }
    }
}
