namespace VirtualClient.Actions.ApacheBench
{
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;

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
            this.testParser.Parse();
            string metricsInput = this.testParser.Sections["Metrics"];
            Assert.IsNotNull(metricsInput);

            Assert.IsTrue(metricsInput.Contains("Total requests"));
            Assert.IsTrue(metricsInput.Contains("Total time (seconds)"));
            Assert.IsTrue(metricsInput.Contains("Total failed requests"));
            Assert.IsTrue(metricsInput.Contains("Total requests (per second)"));
            Assert.IsTrue(metricsInput.Contains("Total time (milliseconds) per request"));
            Assert.IsTrue(metricsInput.Contains("Total data transferred (bytes)"));
            Assert.IsTrue(metricsInput.Contains("Data transfer rate (kilo bytes per second)"));
        }

        [Test]
        public void ApacheBenchResultsParserThrowsWhenInvalidResultsAreProvided()
        {
            string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string outputPath = Path.Combine(workingDirectory, @"Examples\ApacheBench\ApacheBenchResultsInvalidExample.txt");
            this.rawText = File.ReadAllText(outputPath);
            this.testParser = new ApacheBenchMetricsParser(this.rawText);
            Assert.Throws<WorkloadException>(() => this.testParser.Parse());
        }
    }
}
