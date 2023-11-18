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
    public class BlenderBenchmarkMetricsParserTests
    {
        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "BlenderBenchmark");
            }
        }


        [Test]
        public void BlenderMetricsParserTestsCorrectlyMonsterCPU()
        {
            string outputPath = Path.Combine(ExamplePath, "monster_cpu.json");
            string rawText = File.ReadAllText(outputPath);
            BlenderBenchmarkMetricsParser testParser = new BlenderBenchmarkMetricsParser(rawText);
            IList<Metric> actualMetrics = testParser.Parse();
            MetricAssert.Exists(actualMetrics, "device_peak_memory", 646.81, "mb");
            MetricAssert.Exists(actualMetrics, "number_of_samples", 26, "sample");
            MetricAssert.Exists(actualMetrics, "time_for_samples", 30.161805, "second");
            MetricAssert.Exists(actualMetrics, "samples_per_minute", 51.72104255696899, "samples_per_minute");
            MetricAssert.Exists(actualMetrics, "total_render_time", 31.6883, "second");
            MetricAssert.Exists(actualMetrics, "render_time_no_sync", 30.1621, "second");
        }

        [Test]
        public void BlenderMetricsParserTestsCorrectlyJunkShopHIP()
        {
            string outputPath = Path.Combine(ExamplePath, "monster_cpu.json");
            string rawText = File.ReadAllText(outputPath);
            BlenderBenchmarkMetricsParser testParser = new BlenderBenchmarkMetricsParser(rawText);
            IList<Metric> actualMetrics = testParser.Parse();
            MetricAssert.Exists(actualMetrics, "device_peak_memory", 646.81, "mb");
            MetricAssert.Exists(actualMetrics, "number_of_samples", 26, "sample");
            MetricAssert.Exists(actualMetrics, "time_for_samples", 30.161805, "second");
            MetricAssert.Exists(actualMetrics, "samples_per_minute", 51.72104255696899, "samples_per_minute");
            MetricAssert.Exists(actualMetrics, "total_render_time", 31.6883, "second");
            MetricAssert.Exists(actualMetrics, "render_time_no_sync", 30.1621, "second");
        }

        [Test]
        public void BlenderMetricsParserTestsCorrectly_Metadata_Others()
        {
            // These metadata are constants.
            var expectedMetadata = new Dictionary<string, IConvertible>
                {
                    {"blenderVersion", "3.6.0"},
                    {"benchmarkLauncher", "3.1.0"},
                    {"deviceName", "AMD Radeon Pro V620 MxGPU"},
                    {"deviceType", "HIP"},
                    {"timeLimit", 30}
                };

            IDictionary<string, IConvertible> actualMetadataCopy;
            foreach (Metric metric in actualMetrics)
            {
                actualMetadataCopy = new Dictionary<string, IConvertible>(metric.Metadata);

                // Remove scenes as it is not constant and is tested elsewhere.
                actualMetadataCopy.Remove("scene");
                Assert.AreEqual(expectedMetadata, actualMetadataCopy);
            }
        }
    }
}