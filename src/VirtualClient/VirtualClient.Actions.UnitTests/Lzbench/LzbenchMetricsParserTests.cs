using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    public class LzbenchResultsParserUnitTests
    {
        private string rawText;
        private LzbenchMetricsParser testParser;

        private string ExamplePath
        {
            get
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                return Path.Combine(workingDirectory, "Examples", "Lzbench");
            }
        }

        [Test]
        public void LzbenchMetricsParserHandlesMetricsThatAreNotFormattedAsNumericValues()
        {
            string outputPath = Path.Combine(this.ExamplePath, "LzbenchResultsExample.csv");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new LzbenchMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            // The example itself contains non-numeric values.
            // If the test doesn't throw, it means the parser handles non-numeric values.
        }

        [Test]
        public void LzbenchMetricsParserVerifyMetrics()
        {
            string outputPath = Path.Combine(this.ExamplePath, "LzbenchResultsExample.csv");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new LzbenchMetricsParser(this.rawText);
            IList<Metric> metrics = this.testParser.Parse();

            Assert.AreEqual(618, metrics.Count);
            MetricAssert.Exists(metrics, "Compression Speed(memcpy)", 5716.47, "MB/s");
            MetricAssert.Exists(metrics, "Decompression Speed(memcpy)", 5683.63, "MB/s");
            MetricAssert.Exists(metrics, "Compressed size and Original size ratio(memcpy)", 100.00);
            MetricAssert.Exists(metrics, "Compression Speed(blosclz 2.0.0 -1)", 13066.07, "MB/s");
            MetricAssert.Exists(metrics, "Decompression Speed(blosclz 2.0.0 -1)", 13635.57, "MB/s");
            MetricAssert.Exists(metrics, "Compressed size and Original size ratio(blosclz 2.0.0 -1)", 100.00);
            MetricAssert.Exists(metrics, "Compression Speed(brieflz 1.3.0 -3)", 90.87, "MB/s");
            MetricAssert.Exists(metrics, "Decompression Speed(brieflz 1.3.0 -3)", 195.01, "MB/s");
            MetricAssert.Exists(metrics, "Compressed size and Original size ratio(brieflz 1.3.0 -3)", 33.58);
            MetricAssert.Exists(metrics, "Compression Speed(brotli 2019-10-01 -11)", 0.53, "MB/s");
            MetricAssert.Exists(metrics, "Decompression Speed(brotli 2019-10-01 -11)", 544.77, "MB/s");
            MetricAssert.Exists(metrics, "Compressed size and Original size ratio(brotli 2019-10-01 -11)", 20.15);
            MetricAssert.Exists(metrics, "Compression Speed(bzip2 1.0.8 -5)", 14.29, "MB/s");
            MetricAssert.Exists(metrics, "Decompression Speed(bzip2 1.0.8 -5)", 32.91, "MB/s");
            MetricAssert.Exists(metrics, "Compressed size and Original size ratio(bzip2 1.0.8 -5)", 19.50);
            MetricAssert.Exists(metrics, "Compression Speed(csc 2016-10-13 -1)", 22.40, "MB/s");
            MetricAssert.Exists(metrics, "Decompression Speed(csc 2016-10-13 -1)", 60.50, "MB/s");
            MetricAssert.Exists(metrics, "Compressed size and Original size ratio(csc 2016-10-13 -1)", 28.25);
            MetricAssert.Exists(metrics, "Compression Speed(fastlz 0.1 -1)", 255.41, "MB/s");
            MetricAssert.Exists(metrics, "Decompression Speed(fastlz 0.1 -1)", 368.54, "MB/s");
            MetricAssert.Exists(metrics, "Compressed size and Original size ratio(fastlz 0.1 -1)", 50.33);
            MetricAssert.Exists(metrics, "Compression Speed(gipfeli 2016-07-13)", 262.16, "MB/s");
            MetricAssert.Exists(metrics, "Decompression Speed(gipfeli 2016-07-13)", 0.00, "MB/s");
            MetricAssert.Exists(metrics, "Compressed size and Original size ratio(gipfeli 2016-07-13)", 39.91);
            MetricAssert.Exists(metrics, "Compression Speed(lizard 1.0 -10)", 297.66, "MB/s");
            MetricAssert.Exists(metrics, "Decompression Speed(lizard 1.0 -10)", 1505.41, "MB/s");
            MetricAssert.Exists(metrics, "Compressed size and Original size ratio(lizard 1.0 -10)", 48.06);
            MetricAssert.Exists(metrics, "Compression Speed(lz4fast 1.9.2 -3)", 360.12, "MB/s");
            MetricAssert.Exists(metrics, "Decompression Speed(lz4fast 1.9.2 -3)", 2783.45, "MB/s");
            MetricAssert.Exists(metrics, "Compressed size and Original size ratio(lz4fast 1.9.2 -3)", 50.13);
            MetricAssert.Exists(metrics, "Compression Speed(lzham 1.0 -d26 -0)", 8.10, "MB/s");
            MetricAssert.Exists(metrics, "Decompression Speed(lzham 1.0 -d26 -0)", 153.48, "MB/s");
            MetricAssert.Exists(metrics, "Compressed size and Original size ratio(lzham 1.0 -d26 -0)", 31.86);
            MetricAssert.Exists(metrics, "Compression Speed(lzo1b 2.10 -1)", 207.81, "MB/s");
            MetricAssert.Exists(metrics, "Decompression Speed(lzo1b 2.10 -1)", 411.57, "MB/s");
            MetricAssert.Exists(metrics, "Compressed size and Original size ratio(lzo1b 2.10 -1)", 45.43);
            MetricAssert.Exists(metrics, "Compression Speed(lzsse2 2019-04-18 -1)", 17.24, "MB/s");
            MetricAssert.Exists(metrics, "Decompression Speed(lzsse2 2019-04-18 -1)", 3055.94, "MB/s");
            MetricAssert.Exists(metrics, "Compressed size and Original size ratio(lzsse2 2019-04-18 -1)", 40.10);
            MetricAssert.Exists(metrics, "Compression Speed(tornado 0.6a -6)", 40.81, "MB/s");
            MetricAssert.Exists(metrics, "Decompression Speed(tornado 0.6a -6)", 172.91, "MB/s");
            MetricAssert.Exists(metrics, "Compressed size and Original size ratio(tornado 0.6a -6)", 28.20);
        }
    }
}