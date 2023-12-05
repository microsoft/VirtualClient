// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
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

        private static string compositeScoreMetricName = "compositeScore";
        private static string individualScoreMetricName = "individualScore";

        [Test]
        public void SpecViewMetricsParserTestsCorrectly3dsmaxMetrics()
        {
            string outputPath = Path.Combine(ExamplePath, "3dsmaxResultCSV.csv");
            string rawText = File.ReadAllText(outputPath);

            SpecViewMetricsParser testParser = new SpecViewMetricsParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            MetricAssert.Exists(metrics, compositeScoreMetricName, 35.46, "fps");

            MetricAssert.Exists(metrics, individualScoreMetricName, 50.87, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 42.35, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 147.76, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 110.5, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 7.91, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 9.82, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 11.39, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 11.54, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 72.91, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 106.39, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 45.2, "fps");
        }

        [Test]
        public void SpecViewMetricsParserTestsCorrectlyCatiaMetrics()
        {
            string outputPath = Path.Combine(ExamplePath, "CatiaResultCSV.csv");
            string rawText = File.ReadAllText(outputPath);

            SpecViewMetricsParser testParser = new SpecViewMetricsParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            MetricAssert.Exists(metrics, compositeScoreMetricName, 43.3, "fps");

            MetricAssert.Exists(metrics, individualScoreMetricName, 34.01, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 44.3, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 424.59, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 158.01, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 15.86, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 26.73, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 46.68, "fps");
            MetricAssert.Exists(metrics, individualScoreMetricName, 60.68, "fps");
        }

        [Test]
        public void SpecViewMetricsParserTestsCorrectly3dsmaxMetadata()
        {
            string outputPath = Path.Combine(ExamplePath, "3dsmaxResultCSV.csv");
            string rawText = File.ReadAllText(outputPath);

            SpecViewMetricsParser testParser = new SpecViewMetricsParser(rawText);
            IList<Metric> metrics = testParser.Parse();
            IList<Dictionary<string, IConvertible>> expectedMetadataList = new List<Dictionary<string, IConvertible>>();

            // composites
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 100 }, { "index", -1 } });

            // individuals
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 1 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 2 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 3 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 4 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 5 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 6 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 7 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 8 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 9 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 10 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 4.8 }, { "index", 11 } });

            for (int i = 0; i<metrics.Count; i++)
            {
                Assert.AreEqual(metrics[i].Metadata, expectedMetadataList[i]);
            }
        }

        [Test]
        public void SpecViewMetricsParserTestsCorrectlyCatiaMetadata()
        {
            string outputPath = Path.Combine(ExamplePath, "CatiaResultCSV.csv");
            string rawText = File.ReadAllText(outputPath);

            SpecViewMetricsParser testParser = new SpecViewMetricsParser(rawText);
            IList<Metric> metrics = testParser.Parse();
            IList<Dictionary<string, IConvertible>> expectedMetadataList = new List<Dictionary<string, IConvertible>>();

            // composites
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 100 }, { "index", -1 } });
            // inviduals
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 14.28 }, { "index", 1 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 14.28 }, { "index", 2 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 0 }, { "index", 3 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 14.28 }, { "index", 4 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 14.29 }, { "index", 5 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 14.29 }, { "index", 6 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 14.29 }, { "index", 7 } });
            expectedMetadataList.Add(new Dictionary<string, IConvertible> { { "weight", 14.29 }, { "index", 8 } });

            for (int i = 0; i<metrics.Count; i++)
            {
                Assert.AreEqual(metrics[i].Metadata, expectedMetadataList[i]);
            }
        }

    }
}