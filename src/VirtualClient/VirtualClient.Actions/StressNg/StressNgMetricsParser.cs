// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using VirtualClient.Contracts;
    using YamlDotNet.Serialization;
    using YamlDotNet.Serialization.NamingConventions;

    /// <summary>
    /// Parser for StressNg output document
    /// </summary>
    public class StressNgMetricsParser : MetricsParser
    {
        private const string BogusOperationsPerSecond = "BogoOps/s";
        private List<Metric> metrics;

        /// <summary>
        /// Constructor for <see cref="StressNgMetricsParser"/>
        /// </summary>
        /// <param name="rawText">Raw text to parse.</param>
        public StressNgMetricsParser(string rawText)
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

                IDeserializer deserializer = new DeserializerBuilder()
                    .WithNamingConvention(PascalCaseNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .Build();
                StressNgResult parsedResult = deserializer.Deserialize<StressNgResult>(this.PreprocessedText);

                foreach (StressNgStressorResult stressor in parsedResult.Metrics)
                {
                    this.metrics.Add(new Metric($"{stressor.Stressor}-bogo-ops", stressor.BogoOps, "BogoOps"));
                    this.metrics.Add(new Metric($"{stressor.Stressor}-bogo-ops-per-second-usr-sys-time", stressor.BogoOpsPerSecondUsrSysTime, BogusOperationsPerSecond));
                    this.metrics.Add(new Metric($"{stressor.Stressor}-bogo-ops-per-second-real-time", stressor.BogoOpsPerSecondRealTime, BogusOperationsPerSecond));
                    this.metrics.Add(new Metric($"{stressor.Stressor}-wall-clock-time", stressor.WallClockTime, "second"));
                    this.metrics.Add(new Metric($"{stressor.Stressor}-user-time", stressor.UserTime, "second"));
                    this.metrics.Add(new Metric($"{stressor.Stressor}-system-time", stressor.SystemTime, "second"));
                }

                return this.metrics;
            }
            catch (Exception exc)
            {
                throw new WorkloadResultsException("Failed to parse Stress-ng metrics from results.", exc, ErrorReason.InvalidResults);
            }
        }

        /// <summary>
        /// Yaml class contract for StressNg result
        /// </summary>
        public class StressNgResult
        {
            /// <summary>
            /// System info, not captured at this moment.
            /// </summary>
            [YamlMember(Alias = "system-info", ApplyNamingConventions = false)]
            public Dictionary<string, string> SystemInfo { get; set; }

            /// <summary>
            /// Metric from stress ng test.
            /// </summary>
            [YamlMember(Alias = "metrics", ApplyNamingConventions = false)]
            public List<StressNgStressorResult> Metrics { get; set; }
        }

        /// <summary>
        /// Yaml class contract for StressNg result
        /// </summary>
        public class StressNgStressorResult
        {
            /// <summary>
            /// Stressor name
            /// </summary>
            [YamlMember(Alias = "stressor", ApplyNamingConventions = false)]
            public string Stressor { get; set; }

            /// <summary>
            /// BogoOps
            /// </summary>
            [YamlMember(Alias = "bogo-ops", ApplyNamingConventions = false)]
            public double BogoOps { get; set; }

            /// <summary>
            /// BogoOpsPerSecondUsrSysTime
            /// </summary>
            [YamlMember(Alias = "bogo-ops-per-second-usr-sys-time", ApplyNamingConventions = false)]
            public double BogoOpsPerSecondUsrSysTime { get; set; }

            /// <summary>
            /// BogoOpsPerSecondRealTime
            /// </summary>
            [YamlMember(Alias = "bogo-ops-per-second-real-time", ApplyNamingConventions = false)]
            public double BogoOpsPerSecondRealTime { get; set; }

            /// <summary>
            /// WallClockTime
            /// </summary>
            [YamlMember(Alias = "wall-clock-time", ApplyNamingConventions = false)]
            public double WallClockTime { get; set; }

            /// <summary>
            /// UserTime
            /// </summary>
            [YamlMember(Alias = "user-time", ApplyNamingConventions = false)]
            public double UserTime { get; set; }

            /// <summary>
            /// SystemTime
            /// </summary>
            [YamlMember(Alias = "system-time", ApplyNamingConventions = false)]
            public double SystemTime { get; set; }
        }
    }
}