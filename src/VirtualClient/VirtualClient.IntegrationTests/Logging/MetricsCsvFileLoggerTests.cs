// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Integration")]
    internal class MetricsCsvFileLoggerTests
    {
        private static List<string> exampleScenarios = new List<string>
        {
            "Scenario1",
            "Scenario2",
            "Scenario3",
            "Scenario4"
        };

        private static List<Metric> exampleMetrics = new List<Metric>
        {
            new Metric("requests/sec", 123.56, MetricRelativity.HigherIsBetter),
            new Metric("requests/sec (min)", 120.01, MetricRelativity.HigherIsBetter),
            new Metric("requests/sec (max)", 126.12, MetricRelativity.HigherIsBetter),
            new Metric("requests/sec (stdev)", 2.3, MetricRelativity.LowerIsBetter),
            new Metric("avg. latency", 23.1654, MetricUnit.Milliseconds, MetricRelativity.LowerIsBetter),
            new Metric("max latency", 45.648, MetricRelativity.LowerIsBetter),
            new Metric("min latency", 19.443, MetricRelativity.LowerIsBetter),
            new Metric("latency stdev", 13.45, MetricRelativity.LowerIsBetter)
        };

        private static Random randomGen = new Random();
        private static Guid activityId = Guid.NewGuid();
        private static Guid experimentId = Guid.NewGuid();

        [Test]
        public void WriteMetricsToCsvFile()
        {
            string csvFilePath = Path.Combine(MockFixture.TestAssemblyDirectory, "logs", "metrics.csv");
            ILogger logger = DependencyFactory.CreateCsvFileLoggerProvider(csvFilePath)
                .CreateLogger("Testing");

            List<Metric> metrics = new List<Metric>();
            for (int i = 0; i < 100; i++)
            {
                metrics.Add(this.GetRandomMetric());
            }

            // Global VC information is passed into the LogMetrics method via a telemetry
            // EventContext object. The CSV logger is looking for certain properties within this
            // object in order to form the CSV output.
            EventContext context = this.GetEventContext();

            int randomIndex = MetricsCsvFileLoggerTests.randomGen.Next(0, MetricsCsvFileLoggerTests.exampleScenarios.Count);
            string scenario = MetricsCsvFileLoggerTests.exampleScenarios.ElementAt(randomIndex);

            logger.LogMetrics(
                "Tool.exe",
                scenario,
                DateTime.UtcNow.AddSeconds(-30),
                DateTime.UtcNow,
                metrics,
                "Throughput",
                "--duration=30 --output-json",
                new List<string> { "Test", "Requests", "Throughput" },
                context,
                null,
                "1.2.3");

            (logger as IFlushableChannel).Flush();
        }

        private EventContext GetEventContext()
        {
            var properties = new Dictionary<string, object>
            {
                { "experimentId", experimentId },
                { "agentId", Environment.MachineName },
                { "executionProfile", "PERF-WORKLOAD (win-x64)" },
                { "executionProfileName", "PERF-WORKLOAD.json" },
                { "executionSystem", "Test" },
                { "operatingSystemPlatform", Environment.OSVersion.Platform.ToString() },
                { "metricMetadata", new { metadata1 = "value1", metadata2 = 12345 } }
            };

            EventContext telemetryContext = new EventContext(MetricsCsvFileLoggerTests.activityId, properties);

            return telemetryContext;
        }

        private Metric GetRandomMetric()
        {
            return MetricsCsvFileLoggerTests.exampleMetrics.ElementAt(
                MetricsCsvFileLoggerTests.randomGen.Next(0, MetricsCsvFileLoggerTests.exampleMetrics.Count));
        }
    }
}
