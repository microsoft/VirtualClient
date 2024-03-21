namespace VirtualClient.Actions
{
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
    using VirtualClient.Actions;

    [TestFixture]
    [Category("Unit")]
    public class HammerDBMetricsParserTests
    {
        private static string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "HammerDB");

        [Test]
        public void HammerDBParserVerifyMetrics()
        {
            string rawText = File.ReadAllText(Path.Combine(examplesDirectory, "Results_HammerDB.txt"));
            HammerDBMetricsParser testParser = new HammerDBMetricsParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual(4, metrics.Count);
            MetricAssert.Exists(metrics, "Transactions/min", 26163);
            MetricAssert.Exists(metrics, "Transactions/sec", 436.05);
            MetricAssert.Exists(metrics, "Operations/min", 11400);
            MetricAssert.Exists(metrics, "Operations/sec", 190);
        }

        [Test]
        public void PostgreSQLParserThrowIfInvalidOutputFormat()
        {
            string rawText = File.ReadAllText(Path.Combine(examplesDirectory, "HammerDBIncorrectResultsExample.txt"));
            HammerDBMetricsParser testParser = new HammerDBMetricsParser(rawText);

            WorkloadResultsException exception = Assert.Throws<WorkloadResultsException>(() => testParser.Parse());
            Assert.AreEqual(ErrorReason.InvalidResults, exception.Reason);
        }
    }
}