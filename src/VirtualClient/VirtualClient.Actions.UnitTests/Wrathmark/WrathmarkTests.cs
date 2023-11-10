namespace VirtualClient.Actions.Wrathmark.UnitTests
{
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using VirtualClient.Actions.Wrathmark;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class WrathmarkResultParserTests
    {
        private static readonly string ProfilesDirectory = Path.Combine(
            Path.GetDirectoryName(Assembly.GetAssembly(typeof(WrathmarkResultParserTests)).Location),
            "Examples",
            "Wrathmark");

        [Test]
        public void Parse_Benchmark_SingleResult_ReturnsMetrics()
        {
            // Arrange
            string results = File.ReadAllText(Path.Combine(ProfilesDirectory, "BenchWrathmark.txt"));
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

            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.Value, actual.Value);
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
