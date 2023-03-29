// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SysbenchMetricsParserTests
    {
        private static string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "SOCStress");

        [Test]
        public void SysbenchMetricsParserParsesCorrectly()
        {
            string rawText = File.ReadAllText(Path.Combine(examplesDirectory, "SysbenchOutputExample.txt"));
            SysbenchMetricsParser parser = new SysbenchMetricsParser(rawText);

            IList<Metric> metrics = parser.Parse();
            Assert.AreEqual(8, metrics.Count);
            MetricAssert.Exists(metrics, "Total number of events", 160, "");
            MetricAssert.Exists(metrics, "Total Time", 10.2258, "Seconds");
            MetricAssert.Exists(metrics, "Latency Min", 510.98, "milliSeconds");
            MetricAssert.Exists(metrics, "Latency Avg", 511.1, "milliSeconds");
            MetricAssert.Exists(metrics, "Latency Max", 511.53, "milliSeconds");
            MetricAssert.Exists(metrics, "Latency 95th Percentile", 95, "milliSeconds");
            MetricAssert.Exists(metrics, "Thread Fairness Avg Events", 20, "");
            MetricAssert.Exists(metrics, "Thread Fairness Avg Execution Time", 10.222, "");
        }
    }
}