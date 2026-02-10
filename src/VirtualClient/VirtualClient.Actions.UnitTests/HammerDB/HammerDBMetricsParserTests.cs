namespace VirtualClient.Actions
{
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class HammerDBMetricsParserTests
    {
        private static string examplesDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Examples", "HammerDB");

        [Test]
        public void PostgreSQLParserThrowIfInvalidOutputFormat()
        {
            string rawText = File.ReadAllText(Path.Combine(examplesDirectory, "HammerDBIncorrectResultsExample.txt"));
            HammerDBMetricsParser testParser = new HammerDBMetricsParser(rawText);

            WorkloadResultsException exception = Assert.Throws<WorkloadResultsException>(() => testParser.Parse());
            Assert.AreEqual(ErrorReason.InvalidResults, exception.Reason);
        }

        [Test]
        public void HammerDBParserDetectsTpccBenchmarkAndAggregatesOverallMetrics()
        {
            string rawText = File.ReadAllText(Path.Combine(examplesDirectory, "HammerDBMySQL.txt"));
            HammerDBMetricsParser testParser = new HammerDBMetricsParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual("TPCC", testParser.Metadata["Benchmark"]);

            MetricAssert.Exists(metrics, "Operations/sec", 0.18333333333333332d, MetricUnit.OperationsPerSec);
            MetricAssert.Exists(metrics, "Transactions/sec", 0.4166666666666667d, MetricUnit.TransactionsPerSec);

            Assert.AreEqual(2, metrics.Count);
        }

        [Test]
        public void HammerDBParserDetectsTpchBenchmarkAndAggregatesPerVuserMetrics()
        {
            string rawText = File.ReadAllText(Path.Combine(examplesDirectory, "HammerDBMySQLTPCH.txt"));
            HammerDBMetricsParser testParser = new HammerDBMetricsParser(rawText);
            IList<Metric> metrics = testParser.Parse();

            Assert.AreEqual("TPCH", testParser.Metadata["Benchmark"]);

            MetricAssert.Exists(metrics, "Vuser 1 QuerySet1 Duration", 75d, MetricUnit.Seconds);
            MetricAssert.Exists(metrics, "Vuser 1 QuerySet1 GeometricMean", 1.62046d, MetricUnit.Seconds);
            MetricAssert.Exists(metrics, "Vuser 1 QuerySet2 Duration", 76d, MetricUnit.Seconds);
            MetricAssert.Exists(metrics, "Vuser 1 QuerySet2 GeometricMean", 1.6494d, MetricUnit.Seconds);

            MetricAssert.Exists(metrics, "Vuser 2 QuerySet1 Duration", 77d, MetricUnit.Seconds);
            MetricAssert.Exists(metrics, "Vuser 2 QuerySet2 Duration", 79d, MetricUnit.Seconds);
            MetricAssert.Exists(metrics, "Vuser 2 QuerySet1 GeometricMean", 1.64223d, MetricUnit.Seconds);
            MetricAssert.Exists(metrics, "Vuser 2 QuerySet2 GeometricMean", 1.683d, MetricUnit.Seconds);

            Metric allVusersMin = metrics.First(m => m.Name == "AllVusers QueryDurationMin");
            Assert.AreEqual(MetricUnit.Seconds, allVusersMin.Unit);
            Assert.That(allVusersMin.Value, Is.GreaterThan(0d));

            Metric allVusersMax = metrics.First(m => m.Name == "AllVusers QueryDurationMax");
            Assert.AreEqual(MetricUnit.Seconds, allVusersMax.Unit);
            Assert.That(allVusersMax.Value, Is.GreaterThan(allVusersMin.Value));

            Metric allVusersAvg = metrics.First(m => m.Name == "AllVusers QueryDurationAvg");
            Assert.AreEqual(MetricUnit.Seconds, allVusersAvg.Unit);
            Assert.That(allVusersAvg.Value, Is.GreaterThan(allVusersMin.Value));

            Metric allVusersStdev = metrics.First(m => m.Name == "AllVusers QueryDurationStdev");
            Assert.AreEqual(MetricUnit.Seconds, allVusersStdev.Unit);
            Assert.That(allVusersStdev.Value, Is.GreaterThan(0d));

            Metric allVusersP90 = metrics.First(m => m.Name == "AllVusers QueryDurationP90");
            Assert.AreEqual(MetricUnit.Seconds, allVusersP90.Unit);
            Assert.That(allVusersP90.Value, Is.GreaterThan(allVusersAvg.Value));

            Assert.AreEqual(38, metrics.Count);
        }
    }
}