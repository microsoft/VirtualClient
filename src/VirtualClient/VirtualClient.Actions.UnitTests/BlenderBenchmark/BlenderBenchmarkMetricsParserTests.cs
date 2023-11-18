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
            string outputPath = Path.Combine(ExamplePath, "MonsterCPU.json");
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
        public void BlenderMetricsParserTestsCorrectlyJunkshopHIP()
        {
            string outputPath = Path.Combine(ExamplePath, "JunkshopHIP.json");
            string rawText = File.ReadAllText(outputPath);
            BlenderBenchmarkMetricsParser testParser = new BlenderBenchmarkMetricsParser(rawText);
            IList<Metric> actualMetrics = testParser.Parse();
            MetricAssert.Exists(actualMetrics, "device_peak_memory", 4881.3, "mb");
            MetricAssert.Exists(actualMetrics, "number_of_samples", 67, "sample");
            MetricAssert.Exists(actualMetrics, "time_for_samples", 30.440895, "second");
            MetricAssert.Exists(actualMetrics, "samples_per_minute", 132.0591920835442, "samples_per_minute");
            MetricAssert.Exists(actualMetrics, "total_render_time", 42.7055, "second");
            MetricAssert.Exists(actualMetrics, "render_time_no_sync", 30.4413, "second");
        }

        [Test]
        public void BlenderMetricsParserTestsCorrectlyMonsterCPUMetadata()
        {
            BlenderMetricsParserTestsCorrectlyMetadata("AMD EPYC 7763 64-Core Processor", "CPU", "MonsterCPU.json");
        }

        [Test]
        public void BlenderMetricsParserTestsCorrectlyJunkshopHIPMetadata()
        {
            BlenderMetricsParserTestsCorrectlyMetadata("AMD Radeon Pro V620 MxGPU", "HIP", "JunkshopHIP.json");
        }

        private void BlenderMetricsParserTestsCorrectlyMetadata(string deviceName, string deviceType, string exampleResultsFilePath)
        {
            var expectedMetadata = new Dictionary<string, IConvertible>
                {
                    {"blenderVersion", "3.6.0"},
                    {"benchmarkLauncher", "3.1.0"},
                    {"deviceName", deviceName},
                    {"deviceType", deviceType},
                    {"timeLimit", 30}
            };

            string outputPath = Path.Combine(ExamplePath, exampleResultsFilePath);
            string rawText = File.ReadAllText(outputPath);
            BlenderBenchmarkMetricsParser testParser = new BlenderBenchmarkMetricsParser(rawText);
            IList<Metric> actualMetrics = testParser.Parse();
            foreach (Metric actualMetric in actualMetrics)
            {
                Assert.AreEqual(expectedMetadata, actualMetric.Metadata);
            }
        }
    }
}