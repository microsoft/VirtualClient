// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using VirtualClient.Common.Contracts;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;
    using VirtualClient.Actions;

    [TestFixture]
    [Category("Unit")]
    public class CtsTrafficMetricsParserTests
    {
        private string rawText;
        private CtsTrafficMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\CtsTraffic\CtsTrafficResultsExample.csv");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new CtsTrafficMetricsParser(this.rawText);
        }

        [Test]
        public void CtsTrafficParserVerifyMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(30, metrics.Count);
            MetricAssert.Exists(metrics, "SendBps(TimeSlice-0.006)", 8, "B/s");
            MetricAssert.Exists(metrics, "RecvBps(TimeSlice-0.006)", 1, "B/s");
            MetricAssert.Exists(metrics, "InFlight(TimeSlice-0.006)", 1);
            MetricAssert.Exists(metrics, "Completed(TimeSlice-0.006)", 0);
            MetricAssert.Exists(metrics, "NetworkError(TimeSlice-0.006)",0);
            MetricAssert.Exists(metrics, "DataError(TimeSlice-0.006)", 0);
            MetricAssert.Exists(metrics, "SendBps(TimeSlice-20.007)", 27375465, "B/s");
            MetricAssert.Exists(metrics, "RecvBps(TimeSlice-20.007)", 119906898, "B/s");
            MetricAssert.Exists(metrics, "InFlight(TimeSlice-20.007)", 1);
            MetricAssert.Exists(metrics, "Completed(TimeSlice-20.007)", 0);
            MetricAssert.Exists(metrics, "NetworkError(TimeSlice-20.007)", 0);
            MetricAssert.Exists(metrics, "DataError(TimeSlice-20.007)", 0);
        }
    }
}
