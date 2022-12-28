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
    public class SysbenchOLTPMetricsParserTests
    {
        private static string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "SysbenchOLTP");

        [Test]
        public void OLTPSysbenchParserParsesCorrectly()
        {
            string rawText = File.ReadAllText(Path.Combine(examplesDirectory, "SysbenchOLTPExample.txt"));
            SysbenchOLTPMetricsParser parser = new SysbenchOLTPMetricsParser(rawText);

            IList<Metric> metrics = parser.Parse();
            Assert.AreEqual(17, metrics.Count);
            MetricAssert.Exists(metrics, "# read queries", 5039772, "");
            MetricAssert.Exists(metrics, "# write queries", 259534, "");
            MetricAssert.Exists(metrics, "# other queries", 1284992, "");
            MetricAssert.Exists(metrics, "# transactions", 257521, "");
            MetricAssert.Exists(metrics, "transactions/sec", 143.01, "transactions/sec");
            MetricAssert.Exists(metrics, "# queries", 6584298, "");
            MetricAssert.Exists(metrics, "queries/sec", 3657.94, "queries/sec");
            MetricAssert.Exists(metrics, "# ignored errors", 0, "");
            MetricAssert.Exists(metrics, "ignored errors/sec", 0.00, "ignored errors/sec");
            MetricAssert.Exists(metrics, "# reconnects", 0, "");
            MetricAssert.Exists(metrics, "reconnects/sec", 0.00, "reconnects/sec");
            MetricAssert.Exists(metrics, "elapsed time", 1800.0319, "seconds");
            MetricAssert.Exists(metrics, "latency min", 7.39, "milliseconds");
            MetricAssert.Exists(metrics, "latency avg", 28.97, "milliseconds");
            MetricAssert.Exists(metrics, "latency max", 720.22, "milliseconds");
            MetricAssert.Exists(metrics, "latency p95", 68.05, "milliseconds");
            MetricAssert.Exists(metrics, "latency sum", 7458385.25, "milliseconds");
        }
    }
}