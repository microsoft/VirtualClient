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
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class LatMemRdMetricsParserTests
    {
        private static string Examples = MockFixture.GetDirectory(typeof(LMbenchExecutorTests), "Examples", "LMbench");
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
        }

        [Test]
        public void LMbenchMetricsParserCapturesTheExpectedMetrics_1()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = this.mockFixture.Combine(LatMemRdMetricsParserTests.Examples, "latmemrd_example_results.txt");
            string results = File.ReadAllText(outputPath);

            LatMemRdMetricsParser parser = new LatMemRdMetricsParser(results);
            IList<Metric> metrics = parser.Parse();

            Assert.IsTrue(metrics.Count == 54);
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "LatencyArraySizeInMB_0.00098" && m.Value == 1.438));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "LatencyArraySizeInMB_0.01172" && m.Value == 1.438));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "LatencyArraySizeInMB_64.00000" && m.Value == 4.545));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "LatencyArraySizeInMB_128.00000" && m.Value == 2.537));
            Assert.IsNotNull(metrics.FirstOrDefault(m => m.Name == "LatencyArraySizeInMB_48.00000" && m.Value == 1.831));
        }
    }
}