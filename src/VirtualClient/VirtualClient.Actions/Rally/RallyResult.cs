using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VirtualClient.Actions
{
    /// <summary>
    /// Metrics result of a Elasticsearch-Rally run.
    /// </summary>
    public class RallyResult
    {
        /// <summary>
        /// Version of Rally.
        /// </summary>
        [JsonProperty("rally-version")]
        public string RallyVersion { get; set; }

        /// <summary>
        /// Race Id of the run
        /// </summary>
        [JsonProperty("race-id")]
        public string RaceId { get; set; }

        /// <summary>
        /// Timestamp of the race
        /// </summary>
        [JsonProperty("race-timestamp")]
        public string RaceTimestamp { get; set; }

        /// <summary>
        /// Track used on the run
        /// </summary>
        [JsonProperty("track")]
        public string Track { get; set; }

        /// <summary>
        /// Results object.
        /// </summary>
        [JsonProperty("results")]
        public Results Results { get; set; }

    }

    /// <summary>
    /// Results of the ES-Rally run.
    /// </summary>
    public class Results
    {
        /// <summary>
        /// Collection of operations run, and their metrics.
        /// </summary>
        [JsonProperty("op_metrics")]
        public List<RallyMetrics> RallyMetrics { get; set; }

    }

    /// <summary>
    /// Metrics of each operation run.
    /// </summary>
    public class RallyMetrics
    {
        /// <summary>
        /// Task Name.
        /// </summary>
        [JsonProperty("task")]
        public string Task { get; set; }

        /// <summary>
        /// Operation Name (usually same as Task Name).
        /// </summary>
        [JsonProperty("operation")]
        public string Operation { get; set; }

        /// <summary>
        /// Throughput value
        /// </summary>
        [JsonProperty("throughput")]
        public MetricType Throughput { get; set; }

        /// <summary>
        /// Latency value
        /// </summary>
        [JsonProperty("latency")]
        public StatsType Latency { get; set; }

        /// <summary>
        /// Service Time
        /// </summary>
        [JsonProperty("service_time")]
        public StatsType ServiceTime { get; set; }

        /// <summary>
        /// Processing Time
        /// </summary>
        [JsonProperty("processing_time")]
        public StatsType ProcessingTime { get; set; }

        /// <summary>
        /// Total duration
        /// </summary>
        [JsonProperty("duration")]
        public float Duration { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class MetricType
    {
        /// <summary>
        /// Median value
        /// </summary>
        [JsonProperty("median")]
        public double Median { get; set; }

        /// <summary>
        /// Unit
        /// </summary>
        [JsonProperty("unit")]
        public string Unit { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class StatsType
    {
        /// <summary>
        /// 50th percentile
        /// </summary>
        [JsonProperty("50_0")]
        public double FiftyP { get; set; }

        /// <summary>
        /// 90th percentile
        /// </summary>
        [JsonProperty("90_0")]
        public double NinetyP { get; set; }

        /// <summary>
        /// 99th percentile
        /// </summary>
        [JsonProperty("99_0")]
        public double NinetyNineP { get; set; }

        /// <summary>
        /// Unit
        /// </summary>
        [JsonProperty("unit")]
        public string Unit { get; set; }
    }
}