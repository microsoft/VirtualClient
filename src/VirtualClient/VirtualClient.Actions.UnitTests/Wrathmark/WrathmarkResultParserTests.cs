namespace VirtualClient.Actions
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    public class WrathmarkResultParserTests : WrathmarkTestBase
    {
        [Test]
        public void Parse_Benchmark_SingleResult_ReturnsMetrics()
        {
            // Arrange
            string results = File.ReadAllText(GetExampleFileForTests(Constants.BenchmarkResults));
            WrathmarkMetricsParser sut = new WrathmarkMetricsParser(results);

            // Act
            var metrics = sut.Parse();

            // Assert
            Assert.IsNotNull(metrics);
            Assert.AreNotEqual(0, metrics.Count);
            Assert.AreEqual(1, metrics.Count);
            Assert.That(metrics.All(m => m.Name != null));
            Assert.That(metrics.All(m => m.Value >= 0));

            Metric expected = new Metric("BoardsPerSecond", 7_760_880, MetricRelativity.HigherIsBetter);
            Metric actual = metrics[0];

            MetricAssert.Exists(metrics, expected);

            Assert.AreEqual(expected.Relativity, actual.Relativity);
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void Parse_ValidInput_ReturnsMetrics()
        {
            // Arrange
            string results = File.ReadAllText(Path.Combine(ProfilesDirectory, "TestWrathmark.txt"));
            WrathmarkMetricsParser sut = new WrathmarkMetricsParser(results);

            // Act
            var metrics = sut.Parse();

            // Assert
            Assert.IsNotNull(metrics);
            Assert.AreNotEqual(0, metrics.Count);
            Assert.That(metrics.All(m => m.Name != null));
            Assert.That(metrics.All(m => m.Value >= 0));
            Assert.AreEqual(new Metric("BoardsPerSecond", 5_029_887, MetricRelativity.HigherIsBetter), metrics[0]);
        }

        [Test]
        public void Parse_InvalidInput_ReturnsEmptyList()
        {
            // Arrange
            const string results = "invalid input";
            WrathmarkMetricsParser sut = new WrathmarkMetricsParser(results);

            // Act
            IList<Metric> metrics = sut.Parse();

            // Assert
            Assert.IsNotNull(metrics);
            Assert.AreEqual(0, metrics.Count);
        }
    }
}
