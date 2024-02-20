using VirtualClient.Common.Contracts;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    [TestFixture]
    [Category("Unit")]
    class DeathStarBenchMetricsParserUnitTests
    {
        private string rawText;
        private DeathStarBenchMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, "Examples", "DeathStarBench", "DeathStarBenchOutputExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new DeathStarBenchMetricsParser(this.rawText);
        }

        [Test]
        public void DeathStarBenchMetricsParserVerifyMetrics()
        {
            IList<Metric> metrics = this.testParser.Parse();

            MetricAssert.Exists(metrics, "Avg network Latency", 540.95, "us");
            MetricAssert.Exists(metrics, "Stdev network Latency", 66.96, "us");

            MetricAssert.Exists(metrics, "Avg Req/sec", 1070);
            MetricAssert.Exists(metrics, "Stdev Req/sec", 10.83);
            MetricAssert.Exists(metrics, "99% Req/sec", 0.00);

            MetricAssert.Exists(metrics, "50% Network Latency", 524.00, "us");
            MetricAssert.Exists(metrics, "75% Network Latency", 573.00, "us");
            MetricAssert.Exists(metrics, "90% Network Latency", 620.00, "us");
            MetricAssert.Exists(metrics, "99% Network Latency", 729.00, "us");
            MetricAssert.Exists(metrics, "99.99% Network Latency", 729.00, "us");
            MetricAssert.Exists(metrics, "100% Network Latency", 729.00, "us");

            MetricAssert.Exists(metrics, "Transfer/sec", 6.23, "KB");
        }

        [Test]
        public void DeathStarBenchMetricsParserThrowIfInvalidOutputFormat()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string incorrectDeathBenchoutputPath = Path.Combine(workingDirectory, "Examples", "DeathStarBench", "DeathStarBenchIncorrectOutputExample.txt");

            this.rawText = File.ReadAllText(incorrectDeathBenchoutputPath);
            this.testParser = new DeathStarBenchMetricsParser(this.rawText);

            WorkloadResultsException exception = Assert.Throws<WorkloadResultsException>(() => this.testParser.Parse());
            StringAssert.Contains("Failed to parse DeathStarBench metrics from results.", exception.Message);
        }
    }
}
