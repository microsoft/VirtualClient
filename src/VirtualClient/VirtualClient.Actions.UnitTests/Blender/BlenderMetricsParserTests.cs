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
    public class BlenderMetricsParserTests
    {
        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "Blender");
            }
        }

        private IList<Metric> actualMetrics { get; set; }

        [OneTimeSetUp]
        public void Setup()
        {
            string outputPath = Path.Combine(ExamplePath, "results_example.txt");
            string rawText = File.ReadAllText(outputPath);

            BlenderMetricsParser testParser = new BlenderMetricsParser(rawText);
            this.actualMetrics = testParser.Parse();
        }


        [Test]
        public void BlenderMetricsParserTestsCorrectly_Monster()
        {
            MetricAssert.Exists(this.actualMetrics, "device_peak_memory", 1009.97, "mb");
            MetricAssert.Exists(this.actualMetrics, "number_of_samples", 584, "sample");
            MetricAssert.Exists(this.actualMetrics, "time_for_samples", 30.0464, "second");
            MetricAssert.Exists(this.actualMetrics, "samples_per_minute", 1166.1962830821662, "samples_per_minute");
            MetricAssert.Exists(this.actualMetrics, "total_render_time", 31.8678, "second");
            MetricAssert.Exists(this.actualMetrics, "render_time_no_sync", 30.0468, "second");
        }

        [Test]
        public void BlenderMetricsParserTestsCorrectly_JunkShop()
        {
            MetricAssert.Exists(this.actualMetrics, "device_peak_memory", 4881.31, "mb");
            MetricAssert.Exists(this.actualMetrics, "number_of_samples", 323, "sample");
            MetricAssert.Exists(this.actualMetrics, "time_for_samples", 30.142288, "second");
            MetricAssert.Exists(this.actualMetrics, "samples_per_minute", 642.9505285066615, "samples_per_minute");
            MetricAssert.Exists(this.actualMetrics, "total_render_time", 38.0937, "second");
            MetricAssert.Exists(this.actualMetrics, "render_time_no_sync", 30.143, "second");
        }

        [Test]
        public void BlenderMetricsParserTestsCorrectly_Classroom()
        {
            MetricAssert.Exists(this.actualMetrics, "device_peak_memory", 1031.38, "mb");
            MetricAssert.Exists(this.actualMetrics, "number_of_samples", 289, "sample");
            MetricAssert.Exists(this.actualMetrics, "time_for_samples", 30.893534, "second");
            MetricAssert.Exists(this.actualMetrics, "samples_per_minute", 561.2825000856167, "samples_per_minute");
            MetricAssert.Exists(this.actualMetrics, "total_render_time", 31.7936, "second");
            MetricAssert.Exists(this.actualMetrics, "render_time_no_sync", 30.8942, "second");
        }

        [Test]
        public void BlenderMetricsParserTestsCorrectly_Metadata_Scene()
        {
            foreach (Metric metric in this.actualMetrics)
            {
                // Asserts that the scene name is one of "monster", "junkshop", "classroom"
                Assert.Contains(metric.Metadata["scene"], new List<string> { "monster", "junkshop", "classroom" });
            }
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
            foreach (Metric metric in this.actualMetrics)
            {
                actualMetadataCopy = new Dictionary<string, IConvertible>(metric.Metadata);

                // Remove scenes as it is not constant and is tested elsewhere.
                actualMetadataCopy.Remove("scene");
                Assert.AreEqual(expectedMetadata, actualMetadataCopy);
            }
        }
    }
}