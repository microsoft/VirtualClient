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
    using Microsoft.Azure.Amqp.Framing;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SpecViewMetricsParserTests
    {
        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "SPECview");
            }
        }


        [Test]
        public void SpecViewMetricsParserTestsCorrectly_MultipleViewSets()
        {
            string outputPath = Path.Combine(ExamplePath, "resultCSV_multipleViewSets.csv");
            string rawText = File.ReadAllText(outputPath);

            SpecViewMetricsParser testParser = new SpecViewMetricsParser(rawText);
            IList<Metric> metrics = testParser.Parse();
            IList<Metric> expected = new List<Metric>();

            expected.Add(new Metric("3dsmax-07", 35.46, "fps"));
            expected.Add(new Metric("catia-06", 27.46, "fps"));
            expected.Add(new Metric("energy-03", 227.8, "fps"));
            expected.Add(new Metric("medical-03", 35.16, "fps"));
            expected.Add(new Metric("snx-04", 139.11, "fps"));
            expected.Add(new Metric("solidworks-07", 97.8, "fps"));

            MetricAssert.Equals(metrics, expected);
        }

        [Test]
        public void SpecViewMetricsParserTestsCorrectly_OneViewSet()
        {
            string outputPath = Path.Combine(ExamplePath, "resultCSV_oneViewSet.csv");
            string rawText = File.ReadAllText(outputPath);

            SpecViewMetricsParser testParser = new SpecViewMetricsParser(rawText);
            IList<Metric> metrics = testParser.Parse();
            IList<Metric> expected = new List<Metric>();

            expected.Add(new Metric("3dsmax-07", 29.33, "fps"));

            MetricAssert.Equals(metrics, expected);
        }

    }
}