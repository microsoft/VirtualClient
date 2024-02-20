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
        public void ThreeDMarkMetricsParserTestsCorrectly_ScenarioTSGT1()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "3DMark", "result_tsgt1.xml");
            string rawText = File.ReadAllText(outputPath);

            ThreeDMarkMetricsParser testParser = new ThreeDMarkMetricsParser(rawText, "custom_TSGT1.3dmdef");
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "timespy.graphics.1 [fps]", 59.65, "fps");
        }

        [Test]
        public void ThreeDMarkMetricsParserTestsCorrectly_ScenarioTSGT2()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "3DMark", "result_tsgt2.xml");
            string rawText = File.ReadAllText(outputPath);

            ThreeDMarkMetricsParser testParser = new ThreeDMarkMetricsParser(rawText, "custom_TSGT2.3dmdef");
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "timespy.graphics.2 [fps]", 58.10, "fps");
        }

        [Test]
        public void ThreeDMarkMetricsParserTestsCorrectly_ScenarioTSCT()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "3DMark", "result_tsct.xml");
            string rawText = File.ReadAllText(outputPath);

            ThreeDMarkMetricsParser testParser = new ThreeDMarkMetricsParser(rawText, "custom_TSCT.3dmdef");
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(1, metrics.Count);
            MetricAssert.Exists(metrics, "timespy.cpu [fps]", 28.50, "fps");
        }

    }
}