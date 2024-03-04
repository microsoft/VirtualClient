// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class ThreeDMarkMetricsParserTests
    {
        [Test]
        public void ThreeDMarkMetricsParserTestsCorrectly_Timespy()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "3DMark", "exampleTimespyResult.xml");
            string rawText = File.ReadAllText(outputPath);

            ThreeDMarkMetricsParser testParser = new ThreeDMarkMetricsParser(rawText, "timespy");
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(6, metrics.Count);
            MetricAssert.Exists(metrics, "graphics1", 44.16, "fps");
            MetricAssert.Exists(metrics, "graphics2", 35.35, "fps");
            MetricAssert.Exists(metrics, "cpu2", 31.33, "fps");

            // Aggregates
            MetricAssert.Exists(metrics, "graphicsScore", 6436, "score");
            MetricAssert.Exists(metrics, "cpuScore", 9325, "score");
            MetricAssert.Exists(metrics, "3dMarkScore", 1/(0.15/9325 + 0.85/6436), "score");
        }

        public void ThreeDMarkMetricsParserTestsCorrectly_TimespyExtreme()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "3DMark", "exampleTimespyExtremeResult.xml");
            string rawText = File.ReadAllText(outputPath);

            ThreeDMarkMetricsParser testParser = new ThreeDMarkMetricsParser(rawText, "timespy_extreme");
            IList<Metric> metrics = testParser.Parse();
            MetricAssert.Exists(metrics, "graphicsScore", 3213, "score");
            MetricAssert.Exists(metrics, "cpuScore", 6660, "score");
            MetricAssert.Exists(metrics, "3dMarkScore", 1 / (0.15 / 6660 + 0.85 / 3213), "score");
        }

    }
}