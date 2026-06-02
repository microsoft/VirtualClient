namespace VirtualClient.Actions.UnitTests.MongoDB
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Actions;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MongoDBMetricsParserTests
    {
        private string exampleOutputPath;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            // Build path relative to test assembly root; examples copied to output per csproj settings.
            string baseDir = TestContext.CurrentContext.TestDirectory;
            exampleOutputPath = System.IO.Path.Combine(baseDir, "Examples", "MongoDB", "YCSBMongoDBOutputExample.txt");
        }

        [Test]
        public void MongoDBMetricsParser_Constructor_SetsScenarioCorrectly()
        {
            // Arrange & Act
            var parser = new MongoDBMetricsParser("TestScenario", "dummy text");

            // Assert - We can't directly test private field, but constructor should not throw
            Assert.IsNotNull(parser);
        }

        [Test]
        public void Parse_WithValidYCSBOutput_ParsesAllMetricsCorrectly()
        {
            // Arrange
            Assert.IsTrue(System.IO.File.Exists(exampleOutputPath), $"Example output file not found at {exampleOutputPath}");
            string rawText = System.IO.File.ReadAllText(exampleOutputPath);
            var parser = new MongoDBMetricsParser("MongoDBScenario", rawText);

            // Act
            IList<Metric> metrics = parser.Parse();

            // Assert
            Assert.IsNotNull(metrics);
            Assert.AreEqual(34, metrics.Count, "Expected exactly 34 metrics from the example output");

            // Test specific metrics with exact values
            AssertContainsMetric(metrics, "OVERALL-RunTime", 177015, "ms");
            AssertContainsMetric(metrics, "OVERALL-Throughput", 28246.19382538203, "ops/sec");
            AssertContainsMetric(metrics, "READ-AverageLatency", 202.70430571259894, "us");
            AssertContainsMetric(metrics, "READ-95thPercentileLatency", 231, "us");
            AssertContainsMetric(metrics, "UPDATE-AverageLatency", 354.05011122992516, "us");
            AssertContainsMetric(metrics, "UPDATE-99thPercentileLatency", 294, "us");
            AssertContainsMetric(metrics, "TOTAL_GCs-Count", 330, "");
            AssertContainsMetric(metrics, "READ-Operations", 2498425, "");
            AssertContainsMetric(metrics, "UPDATE-Return-OK-Count", 2501575, "");
        }

        [Test]
        public void Parse_WithMinimalValidOutput_ParsesBasicMetrics()
        {
            // Arrange
            string minimalOutput = @"
                [OVERALL], RunTime(ms), 1000
                [OVERALL], Throughput(ops/sec), 100.5
                [READ], Operations, 50
                [READ], AverageLatency(us), 200.0
                [UPDATE], Operations, 50
                [UPDATE], AverageLatency(us), 300.0";

            var parser = new MongoDBMetricsParser("TestScenario", minimalOutput);

            // Act
            IList<Metric> metrics = parser.Parse();

            // Assert
            Assert.IsNotNull(metrics);
            Assert.AreEqual(6, metrics.Count);

            AssertContainsMetric(metrics, "OVERALL-RunTime", 1000, "ms");
            AssertContainsMetric(metrics, "OVERALL-Throughput", 100.5, "ops/sec");
            AssertContainsMetric(metrics, "READ-Operations", 50, "");
            AssertContainsMetric(metrics, "READ-AverageLatency", 200.0, "us");
            AssertContainsMetric(metrics, "UPDATE-Operations", 50, "");
            AssertContainsMetric(metrics, "UPDATE-AverageLatency", 300.0, "us");
        }

        [Test]
        public void Parse_SetsCorrectMetricRelativity_ForDifferentMetricTypes()
        {
            // Arrange
            string outputWithDifferentTypes = @"
                [OVERALL], Throughput(ops/sec), 1000
                [READ], AverageLatency(us), 200
                [UPDATE], 95thPercentileLatency(us), 300
                [CLEANUP], 99thPercentileLatency(us), 400";

            var parser = new MongoDBMetricsParser("TestScenario", outputWithDifferentTypes);

            // Act
            IList<Metric> metrics = parser.Parse();

            // Assert
            AssertContainsMetric(metrics, "OVERALL-Throughput", 1000, "ops/sec", MetricRelativity.HigherIsBetter);
            AssertContainsMetric(metrics, "READ-AverageLatency", 200, "us", MetricRelativity.LowerIsBetter);
            AssertContainsMetric(metrics, "UPDATE-95thPercentileLatency", 300, "us", MetricRelativity.LowerIsBetter);
            AssertContainsMetric(metrics, "CLEANUP-99thPercentileLatency", 400, "us", MetricRelativity.LowerIsBetter);
        }

        [Test]
        public void Parse_HandlesMetricsWithReturnValues()
        {
            // Arrange
            string outputWithReturns = @"
                [OVERALL], Throughput(ops/sec), 1000
                [READ], Return=OK, 500
                [READ], Return=ERROR, 5
                [UPDATE], Return=NOT_FOUND, 10
                [UPDATE], Return=OK, 490";

            var parser = new MongoDBMetricsParser("TestScenario", outputWithReturns);

            // Act
            IList<Metric> metrics = parser.Parse();

            // Assert
            Assert.AreEqual(5, metrics.Count);
            AssertContainsMetric(metrics, "OVERALL-Throughput", 1000, "ops/sec");
            AssertContainsMetric(metrics, "READ-Return-OK-Count", 500, "");
            AssertContainsMetric(metrics, "READ-Return-ERROR-Count", 5, "");
            AssertContainsMetric(metrics, "UPDATE-Return-NOT_FOUND-Count", 10, "");
            AssertContainsMetric(metrics, "UPDATE-Return-OK-Count", 490, "");
        }

        [Test]
        public void Parse_ThrowsWorkloadException_WhenOverallSectionMissing()
        {
            // Arrange
            string outputWithoutOverall = @"
                [READ], Operations, 100
                [UPDATE], Operations, 100";

            var parser = new MongoDBMetricsParser("TestScenario", outputWithoutOverall);

            // Act & Assert
            var exception = Assert.Throws<WorkloadException>(() => parser.Parse());
            Assert.AreEqual(ErrorReason.WorkloadResultsParsingFailed, exception.Reason);
            StringAssert.Contains("Benchmarking metrics are not present", exception.Message);
        }

        [Test]
        public void Parse_ThrowsWorkloadException_WhenRawTextIsEmpty()
        {
            // Arrange
            var parser = new MongoDBMetricsParser("TestScenario", "");

            // Act & Assert
            var exception = Assert.Throws<WorkloadException>(() => parser.Parse());
            Assert.AreEqual(ErrorReason.WorkloadResultsParsingFailed, exception.Reason);
        }

        [Test]
        public void Parse_ThrowsWorkloadException_WhenRawTextIsNull()
        {
            // Arrange
            var parser = new MongoDBMetricsParser("TestScenario", null);

            // Act & Assert
            Assert.Throws<WorkloadException>(() => parser.Parse());
        }

        [Test]
        public void Parse_HandlesMalformedLines_Gracefully()
        {
            // Arrange - Some lines have wrong format, but parser should still work with valid ones
            string outputWithMalformed = @"
                // Valid overall section
                [OVERALL], RunTime(ms), 1000
                [OVERALL], Throughput(ops/sec), 500
                Malformed line without proper structure
                ,,,
                // Valid metrics
                [READ], AverageLatency(us), 200
                Bad line
                [UPDATE], AverageLatency(us), 300";

            var parser = new MongoDBMetricsParser("TestScenario", outputWithMalformed);

            // Act
            IList<Metric> metrics = parser.Parse();

            // Assert - Should parse the valid metrics and ignore malformed ones
            Assert.IsNotNull(metrics);
            Assert.GreaterOrEqual(metrics.Count, 3); // At least the valid ones

            AssertContainsMetric(metrics, "OVERALL-RunTime", 1000, "ms");
            AssertContainsMetric(metrics, "OVERALL-Throughput", 500, "ops/sec");
            AssertContainsMetric(metrics, "READ-AverageLatency", 200, "us");
            AssertContainsMetric(metrics, "UPDATE-AverageLatency", 300, "us");
        }

        [Test]
        public void Parse_HandlesMetricsWithUnitsContainingParentheses()
        {
            // Arrange
            string outputWithComplexUnits = @"
                [OVERALL], RunTime(ms), 1000
                [TOTAL_GC_TIME_%_G1_Young_Generation], Time(%), 0.5
                [TOTAL_GC_TIME_G1_Concurrent_GC], Time(ms), 150";

            var parser = new MongoDBMetricsParser("TestScenario", outputWithComplexUnits);

            // Act
            IList<Metric> metrics = parser.Parse();

            // Assert
            AssertContainsMetric(metrics, "OVERALL-RunTime", 1000, "ms");
            AssertContainsMetric(metrics, "TOTAL_GC_TIME_%_G1_Young_Generation-Time", 0.5, "%");
            AssertContainsMetric(metrics, "TOTAL_GC_TIME_G1_Concurrent_GC-Time", 150, "ms");
        }

        private static void AssertContainsMetric(IList<Metric> metrics, string name, double value, string unit, MetricRelativity? expectedRelativity = null)
        {
            var metric = metrics.FirstOrDefault(m => m.Name == name);
            Assert.IsNotNull(metric, $"Metric '{name}' not found in parsed metrics");
            Assert.AreEqual(value, metric.Value, 0.0001, $"Metric '{name}' value mismatch");
            Assert.AreEqual(unit, metric.Unit, $"Metric '{name}' unit mismatch");
            if (expectedRelativity.HasValue)
            {
                Assert.AreEqual(expectedRelativity.Value, metric.Relativity, $"Metric '{name}' relativity mismatch");
            }
        }
    }
}
