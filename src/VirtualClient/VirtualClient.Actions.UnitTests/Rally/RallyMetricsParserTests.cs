// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class RallyMetricsParserTests
    {
        private static string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "Rally");

        [Test]
        public void RallyParserParsesCorrectly()
        {
            string rawText = File.ReadAllText(Path.Combine(examplesDirectory, "rally-output.json"));
            RallyMetricsParser parser = new RallyMetricsParser(rawText);

            IList<Metric> metrics = parser.Parse();
            Assert.AreEqual(77, metrics.Count);
            MetricAssert.Exists(metrics, "index-append_throughput_median", 191.7316520183353, "docs/s");
            MetricAssert.Exists(metrics, "index-append_service_time_50_0", 3151.838972000405, "ms");
            MetricAssert.Exists(metrics, "index-append_latency_90_0", 16505.79694721382, "ms");
            MetricAssert.Exists(metrics, "index-append_processing_time_99_0", 27598.408258756404, "ms");
            MetricAssert.Exists(metrics, "term_throughput_median", 19.884066533827863, "ops/s");
            MetricAssert.Exists(metrics, "term_service_time_99_0", 19.383756511961057, "ms");
            MetricAssert.Exists(metrics, "term_latency_50_0", 13.66237350157462, "ms");
            MetricAssert.Exists(metrics, "term_processing_time_90_0", 16.551671968773007, "ms");
            MetricAssert.Exists(metrics, "scroll_throughput_median", 12.540198885688246, "pages/s");
            MetricAssert.Exists(metrics, "scroll_service_time_90_0", 950.8418231911492, "ms");
            MetricAssert.Exists(metrics, "scroll_latency_99_0", 1117.69719580072, "ms");
            MetricAssert.Exists(metrics, "scroll_processing_time_50_0", 925.6542755174451, "ms");
        }
    }
}
