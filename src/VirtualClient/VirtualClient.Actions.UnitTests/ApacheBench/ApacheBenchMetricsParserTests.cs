namespace VirtualClient.Actions.ApacheBench
{
    using NUnit.Framework;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    public class ApacheBenchMetricsParserTests
    {
        private string rawText;
        private ApacheBenchMetricsParser testParser;

        [SetUp]
        public void Setup()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\ApacheBench\ApacheBenchResultsExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new ApacheBenchMetricsParser(this.rawText);
        }

        [Test]
        public void ApacheBenchMetricsParserParsesAsExpected()
        {
            this.testParser.Parse();
            Assert.IsNotNull(this.testParser.Sections["Metrics"]);
        }

        [Test]
        public void ApacheBenchMetricsParserParsesInputAsExpected()
        {
            IList<Metric> metrics = this.testParser.Parse();
            string metricsInput = this.testParser.Sections["Metrics"];
            Assert.IsNotNull(metricsInput);

            MetricAssert.Exists(metrics, "Total requests", 100);
            MetricAssert.Exists(metrics, "Total time", 0.578);
            MetricAssert.Exists(metrics, "Total failed requests", 0);
            MetricAssert.Exists(metrics, "Requests", 172.97);
            MetricAssert.Exists(metrics, "Total time per request", 5.781);
            MetricAssert.Exists(metrics, "Total data transferred", 65006);
            MetricAssert.Exists(metrics, "Data transfer rate", 109.81);
        }

        [Test]
        public void ApacheBenchResultsParserReturnsEmptyResultWhenInvalidResultsAreProvided()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\ApacheBench\ApacheBenchResultsInvalidExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new ApacheBenchMetricsParser(this.rawText);
            Assert.Throws<WorkloadException>(() => this.testParser.Parse());
        }
    }
}
