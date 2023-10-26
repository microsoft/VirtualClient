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

        private IList<Metric> metrics;

        [OneTimeSetUp]
        public void Setup()
        {
            string outputPath = Path.Combine(ExamplePath, "resultCSV.csv");
            string rawText = File.ReadAllText(outputPath);

            SpecViewMetricsParser testParser = new SpecViewMetricsParser(rawText);
            metrics = testParser.Parse();
        }


        [Test]
        public void SpecViewMetricsParserTestsCorrectly_Metrics()
        {
            MetricAssert.Exists(this.metrics, "3dsmax-07", 50.87, "fps");
            MetricAssert.Exists(this.metrics, "3dsmax-07", 42.35, "fps");
            MetricAssert.Exists(this.metrics, "3dsmax-07", 147.76, "fps");
            MetricAssert.Exists(this.metrics, "3dsmax-07", 110.5, "fps");
            MetricAssert.Exists(this.metrics, "3dsmax-07", 7.91, "fps");
            MetricAssert.Exists(this.metrics, "3dsmax-07", 9.82, "fps");
            MetricAssert.Exists(this.metrics, "3dsmax-07", 11.39, "fps");
            MetricAssert.Exists(this.metrics, "3dsmax-07", 11.54, "fps");
            MetricAssert.Exists(this.metrics, "3dsmax-07", 72.91, "fps");
            MetricAssert.Exists(this.metrics, "3dsmax-07", 106.39, "fps");
            MetricAssert.Exists(this.metrics, "3dsmax-07", 45.2, "fps");
            MetricAssert.Exists(this.metrics, "catia-06", 34.01, "fps");
            MetricAssert.Exists(this.metrics, "catia-06", 44.3, "fps");
            MetricAssert.Exists(this.metrics, "catia-06", 424.59, "fps");
            MetricAssert.Exists(this.metrics, "catia-06", 158.01, "fps");
            MetricAssert.Exists(this.metrics, "catia-06", 15.86, "fps");
            MetricAssert.Exists(this.metrics, "catia-06", 26.73, "fps");
            MetricAssert.Exists(this.metrics, "catia-06", 46.68, "fps");
            MetricAssert.Exists(this.metrics, "catia-06", 60.68, "fps");
            MetricAssert.Exists(this.metrics, "creo-03", 60.44, "fps");
            MetricAssert.Exists(this.metrics, "creo-03", 25.21, "fps");
            MetricAssert.Exists(this.metrics, "creo-03", 33.31, "fps");
            MetricAssert.Exists(this.metrics, "creo-03", 172.31, "fps");
            MetricAssert.Exists(this.metrics, "creo-03", 244.89, "fps");
            MetricAssert.Exists(this.metrics, "creo-03", 72.06, "fps");
            MetricAssert.Exists(this.metrics, "creo-03", 40.56, "fps");
            MetricAssert.Exists(this.metrics, "creo-03", 50.14, "fps");
            MetricAssert.Exists(this.metrics, "creo-03", 12.34, "fps");
            MetricAssert.Exists(this.metrics, "creo-03", 92.99, "fps");
            MetricAssert.Exists(this.metrics, "creo-03", 56.1, "fps");
            MetricAssert.Exists(this.metrics, "creo-03", 49.47, "fps");
            MetricAssert.Exists(this.metrics, "creo-03", 85.21, "fps");
            MetricAssert.Exists(this.metrics, "energy-03", 38.02, "fps");
            MetricAssert.Exists(this.metrics, "energy-03", 20.46, "fps");
            MetricAssert.Exists(this.metrics, "energy-03", 18.04, "fps");
            MetricAssert.Exists(this.metrics, "energy-03", 49.49, "fps");
            MetricAssert.Exists(this.metrics, "energy-03", 25.79, "fps");
            MetricAssert.Exists(this.metrics, "energy-03", 23.92, "fps");
            MetricAssert.Exists(this.metrics, "maya-06", 120.22, "fps");
            MetricAssert.Exists(this.metrics, "maya-06", 664.73, "fps");
            MetricAssert.Exists(this.metrics, "maya-06", 106.89, "fps");
            MetricAssert.Exists(this.metrics, "maya-06", 459.27, "fps");
            MetricAssert.Exists(this.metrics, "maya-06", 245.47, "fps");
            MetricAssert.Exists(this.metrics, "maya-06", 86.95, "fps");
            MetricAssert.Exists(this.metrics, "maya-06", 198.37, "fps");
            MetricAssert.Exists(this.metrics, "maya-06", 388.78, "fps");
            MetricAssert.Exists(this.metrics, "maya-06", 506.82, "fps");
            MetricAssert.Exists(this.metrics, "maya-06", 87.18, "fps");
            MetricAssert.Exists(this.metrics, "medical-03", 258.34, "fps");
            MetricAssert.Exists(this.metrics, "medical-03", 310.64, "fps");
            MetricAssert.Exists(this.metrics, "medical-03", 63.19, "fps");
            MetricAssert.Exists(this.metrics, "medical-03", 26.93, "fps");
            MetricAssert.Exists(this.metrics, "medical-03", 32.06, "fps");
            MetricAssert.Exists(this.metrics, "medical-03", 82.55, "fps");
            MetricAssert.Exists(this.metrics, "medical-03", 31.91, "fps");
            MetricAssert.Exists(this.metrics, "medical-03", 53.17, "fps");
            MetricAssert.Exists(this.metrics, "medical-03", 1.26, "fps");
            MetricAssert.Exists(this.metrics, "medical-03", 3.74, "fps");
            MetricAssert.Exists(this.metrics, "snx-04", 123.54, "fps");
            MetricAssert.Exists(this.metrics, "snx-04", 164.72, "fps");
            MetricAssert.Exists(this.metrics, "snx-04", 85.33, "fps");
            MetricAssert.Exists(this.metrics, "snx-04", 114.15, "fps");
            MetricAssert.Exists(this.metrics, "snx-04", 158.74, "fps");
            MetricAssert.Exists(this.metrics, "snx-04", 238.02, "fps");
            MetricAssert.Exists(this.metrics, "snx-04", 216.75, "fps");
            MetricAssert.Exists(this.metrics, "snx-04", 118.66, "fps");
            MetricAssert.Exists(this.metrics, "snx-04", 134.47, "fps");
            MetricAssert.Exists(this.metrics, "snx-04", 232.72, "fps");
            MetricAssert.Exists(this.metrics, "solidworks-07", 139.26, "fps");
            MetricAssert.Exists(this.metrics, "solidworks-07", 191.75, "fps");
            MetricAssert.Exists(this.metrics, "solidworks-07", 256.76, "fps");
            MetricAssert.Exists(this.metrics, "solidworks-07", 268.97, "fps");
            MetricAssert.Exists(this.metrics, "solidworks-07", 146.99, "fps");
            MetricAssert.Exists(this.metrics, "solidworks-07", 105.26, "fps");
            MetricAssert.Exists(this.metrics, "solidworks-07", 167.8, "fps");
            MetricAssert.Exists(this.metrics, "solidworks-07", 92.75, "fps");
            MetricAssert.Exists(this.metrics, "solidworks-07", 9.3, "fps");
            MetricAssert.Exists(this.metrics, "solidworks-07", 4.99, "fps");
        }

        [Test]
        public void SpecViewMetricsParserTestsCorrectly_Metadata()
        {
            var expectedMetadataList = new List<Dictionary<string, IConvertible>>
            {
                // composites
                new Dictionary<string, IConvertible> { { "weight", 100 }, { "index", -1 }, { "isCompositeScore", true } },
                new Dictionary<string, IConvertible> { { "weight", 100 }, { "index", -1 }, { "isCompositeScore", true } },
                new Dictionary<string, IConvertible> { { "weight", 100 }, { "index", -1 }, { "isCompositeScore", true } },
                new Dictionary<string, IConvertible> { { "weight", 100 }, { "index", -1 }, { "isCompositeScore", true } },
                new Dictionary<string, IConvertible> { { "weight", 100 }, { "index", -1 }, { "isCompositeScore", true } },
                new Dictionary<string, IConvertible> { { "weight", 100 }, { "index", -1 }, { "isCompositeScore", true } },
                new Dictionary<string, IConvertible> { { "weight", 100 }, { "index", -1 }, { "isCompositeScore", true } },
                new Dictionary<string, IConvertible> { { "weight", 100 }, { "index", -1 }, { "isCompositeScore", true } },

                // 3dsmax-07
                new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 1 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 2 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 3 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 4 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 5 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 6 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 7 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 8 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 9 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 9.52 }, { "index", 10 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 4.8 }, { "index", 11 }, { "isCompositeScore", false } },

                // catia-06
                new Dictionary<string, IConvertible> { { "weight", 14.28 }, { "index", 1 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 14.28 }, { "index", 2 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 0 }, { "index", 3 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 14.28 }, { "index", 4 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 14.29 }, { "index", 5 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 14.29 }, { "index", 6 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 14.29 }, { "index", 7 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 14.29 }, { "index", 8 }, { "isCompositeScore", false } },

                // creo-03
                new Dictionary<string, IConvertible> { { "weight", 8.33 }, { "index", 1 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 8.33 }, { "index", 2 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 8.34 }, { "index", 3 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 4 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 5 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 6 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 7 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 8 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 9 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 3.75 }, { "index", 10 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 3.75 }, { "index", 11 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 3.75 }, { "index", 12 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 3.75 }, { "index", 13 }, { "isCompositeScore", false } },

                // energy-03
                new Dictionary<string, IConvertible> { { "weight", 16.67 }, { "index", 1 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 16.67 }, { "index", 2 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 16.67 }, { "index", 3 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 16.67 }, { "index", 4 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 16.66 }, { "index", 5 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 16.66 }, { "index", 6 }, { "isCompositeScore", false } },

                // maya-06
                new Dictionary<string, IConvertible> { { "weight", 8.33 }, { "index", 1 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 8.33 }, { "index", 2 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 12.5 }, { "index", 3 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 12.5 }, { "index", 4 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 12.5 }, { "index", 5 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 8.33 }, { "index", 6 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 8.33 }, { "index", 7 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 12.5 }, { "index", 8 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 8.34 }, { "index", 9 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 8.34 }, { "index", 10 }, { "isCompositeScore", false } },

                // medical-03
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 1 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 2 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 3 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 4 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 5 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 6 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 7 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 8 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 9 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 10 }, { "isCompositeScore", false } },

                // snx-04
                new Dictionary<string, IConvertible> { { "weight", 7.5 }, { "index", 1 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 2 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 20 }, { "index", 3 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 5 }, { "index", 4 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 7.5 }, { "index", 5 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 7.5 }, { "index", 6 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 7 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 20 }, { "index", 8 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 5 }, { "index", 9 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 7.5 }, { "index", 10 }, { "isCompositeScore", false } },

                // solidworks-07
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 1 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 2 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 15 }, { "index", 3 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 5 }, { "index", 4 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 15 }, { "index", 5 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 6 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 7 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 8 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 5 }, { "index", 9 }, { "isCompositeScore", false } },
                new Dictionary<string, IConvertible> { { "weight", 10 }, { "index", 10 }, { "isCompositeScore", false } }
            };

            for (int i = 0; i < metrics.Count; i++)
            {
                Assert.AreEqual(metrics[i].Metadata, expectedMetadataList[i]);
            }
        }
    }
}