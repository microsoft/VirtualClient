// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Microsoft.Extensions.Logging;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class MetricsCsvFileLoggerTests
    {
        private static AssemblyName loggingAssembly = Assembly.GetAssembly(typeof(EventHubTelemetryLogger)).GetName();
        private static AssemblyName executingAssembly = Assembly.GetEntryAssembly().GetName();

        [Test]
        public void MetricsCsvFileLoggerUsesTheExpectedColumnNamesForTheCsvFiles()
        {
            List<string> expectedColumnHeaders = new List<string>
            {
                "Timestamp",
                "ExperimentId",
                "ExecutionSystem",
                "ProfileName",
                "ClientId",
                "ToolName",
                "ToolVersion",
                "ScenarioName",
                "ScenarioStartTime",
                "ScenarioEndTime",
                "MetricName",
                "MetricValue",
                "MetricUnit",
                "MetricCategorization",
                "MetricDescription",
                "MetricRelativity",
                "MetricVerbosity",
                "AppHost",
                "AppName",
                "AppVersion",
                "OperatingSystemPlatform",
                "PlatformArchitecture",
                "SeverityLevel",
                "OperationId",
                "OperationParentId",
                "Metadata",
                "Metadata_Host",
                "ToolResults",
                "Tags"
            };

            // Order matters, so we are doing an explicit equality check here.
            CollectionAssert.AreEqual(expectedColumnHeaders, MetricsCsvFileLogger.CsvFields.Select(field => field.ColumnName));
        }

        [Test]
        public void MetricsCsvFileLoggerWritesTheExpectedCsvToFiles_1()
        {
            Guid expectedExperimentId = Guid.NewGuid();
            Guid expectedActivityId = Guid.NewGuid();
            DateTime timestamp = DateTime.UtcNow;
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-30);
            DateTime expectedEndTime = DateTime.UtcNow;

            var properties = new Dictionary<string, object>
            {
                { "timestamp", timestamp },
                { "experimentId", expectedExperimentId },
                { "executionSystem", "Test" },
                { "executionProfileName", "PERF-WORKLOAD.json" },
                { "clientId", Environment.MachineName },
                { "toolName", "ToolA" },
                { "toolVersion", "1.2.3" },
                { "scenarioName", "Scenario01" },
                { "scenarioStartTime", expectedStartTime },
                { "scenarioEndTime",  expectedEndTime },
                { "metricName", "avg. latency" },
                { "metricValue", 123.45 },
                { "metricUnit", "milliseconds" },
                { "metricCategorization", "Latency" },
                { "metricDescription", "The average \"latency\" (in milliseconds)." },
                { "metricRelativity", MetricRelativity.LowerIsBetter },
                { "metricVerbosity", 3 },
                { "operatingSystemPlatform", PlatformID.Unix.ToString() },
                { "platformArchitecture", "linux-x64" },
                { "severityLevel", "1" },
                { "metadata", new Dictionary<string, object> { ["Metadata1"] = "1234", ["Metadata2"] = true } },
                { "metadata_host", new Dictionary<string, object> { ["HostMetadata1"] = "One", ["HostMetadata2"] = "Two" } },
                { "toolResults", "ToolA version 1.2.3 output" },
                { "tags", "Tag1,Tag2,Tag3" }
            };

            EventContext context = new EventContext(expectedActivityId, properties);
            string actualCsvMessage = MetricsCsvFileLogger.CreateMessage(context);

            Assert.IsTrue(actualCsvMessage.StartsWith(Environment.NewLine));

            Assert.AreEqual(
                $"\"{timestamp.ToString("o")}\"" +
                $",\"{expectedExperimentId}\"" +
                $",\"Test\"" +
                $",\"PERF-WORKLOAD.json\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"ToolA\"" +
                $",\"1.2.3\"" +
                $",\"Scenario01\"" +
                $",\"{expectedStartTime.ToString("o")}\"" +
                $",\"{expectedEndTime.ToString("o")}\"" +
                $",\"avg. latency\"" +
                $",\"123.45\"" +
                $",\"milliseconds\"" +
                $",\"Latency\"" +
                $",\"The average \"\"latency\"\" (in milliseconds).\"" +
                $",\"LowerIsBetter\"" +
                $",\"3\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"{executingAssembly.Name}\"" +
                $",\"{executingAssembly.Version}\"" +
                $",\"Unix\"" +
                $",\"linux-x64\"" +
                $",\"1\"" +
                $",\"{expectedActivityId}\"" +
                $",\"{Guid.Empty}\"" +
                $",\"Metadata1=1234;Metadata2=True\"" +
                $",\"HostMetadata1=One;HostMetadata2=Two\"" +
                $",\"ToolA version 1.2.3 output\"" +
                $",\"Tag1,Tag2,Tag3\"",
                //
                // We are removing the line break and timestamp here because the timestamp
                // is variable depending upon when the test is ran. We are confirming the line
                // break above anyhow. The round-trip timestamp and quotes are 31 chars in length.
                actualCsvMessage.Substring(Environment.NewLine.Length));
        }

        [Test]
        public void MetricsCsvFileLoggerWritesTheExpectedCsvToFiles_2()
        {
            Guid expectedExperimentId = Guid.NewGuid();
            Guid expectedActivityId = Guid.NewGuid();
            DateTime timestamp = DateTime.UtcNow;
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-30);
            DateTime expectedEndTime = DateTime.UtcNow;

            var properties = new Dictionary<string, object>
            {
                { "timestamp", timestamp },
                { "experimentId", expectedExperimentId },
                { "executionSystem", "Test" },
                { "executionProfileName", "PERF-WORKLOAD.json" },
                { "clientId", Environment.MachineName },
                { "toolName", "ToolA" },
                { "toolVersion", "1.2.3" },
                { "scenarioName", "Scenario01" },
                { "scenarioStartTime", expectedStartTime },
                { "scenarioEndTime",  expectedEndTime },
                { "metricName", "avg. latency" },
                { "metricValue", 123.45 },
                { "metricUnit", "milliseconds" },
                { "metricCategorization", "Latency" },
                { "metricDescription", "The average latency (in milliseconds)." },
                { "metricRelativity", MetricRelativity.LowerIsBetter },
                { "metricVerbosity", 3 },
                { "operatingSystemPlatform", PlatformID.Win32NT.ToString() },
                { "platformArchitecture", "win-arm64" },
                { "severityLevel", "1" },
                { "metadata", new Dictionary<string, object> { ["Metadata1"] = "1234", ["Metadata2"] = true } },
                { "metadata_host", new Dictionary<string, object> { ["HostMetadata1"] = "One", ["HostMetadata2"] = "Two" } },
                { "toolResults", "ToolA version 1.2.3 output" },
                { "tags", "Tag1,Tag2,Tag3" }
            };

            EventContext context = new EventContext(expectedActivityId, properties);
            string actualCsvMessage = MetricsCsvFileLogger.CreateMessage(context);

            Assert.IsTrue(actualCsvMessage.StartsWith(Environment.NewLine));

            Assert.AreEqual(
                $"\"{timestamp.ToString("o")}\"" +
                $",\"{expectedExperimentId}\"" +
                $",\"Test\"" +
                $",\"PERF-WORKLOAD.json\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"ToolA\"" +
                $",\"1.2.3\"" +
                $",\"Scenario01\"" +
                $",\"{expectedStartTime.ToString("o")}\"" +
                $",\"{expectedEndTime.ToString("o")}\"" +
                $",\"avg. latency\"" +
                $",\"123.45\"" +
                $",\"milliseconds\"" +
                $",\"Latency\"" +
                $",\"The average latency (in milliseconds).\"" +
                $",\"LowerIsBetter\"" +
                $",\"3\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"{executingAssembly.Name}\"" +
                $",\"{executingAssembly.Version}\"" +
                $",\"Win32NT\"" +
                $",\"win-arm64\"" +
                $",\"1\"" +
                $",\"{expectedActivityId}\"" +
                $",\"{Guid.Empty}\"" +
                $",\"Metadata1=1234;Metadata2=True\"" +
                $",\"HostMetadata1=One;HostMetadata2=Two\"" +
                $",\"ToolA version 1.2.3 output\"" +
                $",\"Tag1,Tag2,Tag3\"",
                //
                // We are removing the line break and timestamp here because the timestamp
                // is variable depending upon when the test is ran. We are confirming the line
                // break above anyhow. The round-trip timestamp and quotes are 31 chars in length.
                actualCsvMessage.Substring(Environment.NewLine.Length));
        }

        [Test]
        [TestCase(3, 3)]
        [TestCase(LogLevel.Trace, 0)]
        [TestCase(LogLevel.Debug, 1)]
        [TestCase(LogLevel.Information, 2)]
        [TestCase(LogLevel.Warning, 3)]
        [TestCase(LogLevel.Error, 4)]
        [TestCase(LogLevel.Critical, 5)]
        public void MetricsCsvFileLoggerHandlesSeverityLevelsAsEitherIntegerOrEnumerationValues(object severityLevel, int expectedSeverityLevel)
        {
            Guid expectedExperimentId = Guid.NewGuid();
            Guid expectedActivityId = Guid.NewGuid();
            DateTime timestamp = DateTime.UtcNow;
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-30);
            DateTime expectedEndTime = DateTime.UtcNow;

            var properties = new Dictionary<string, object>
            {
                { "timestamp", timestamp },
                { "experimentId", expectedExperimentId },
                { "executionSystem", "Test" },
                { "executionProfileName", "PERF-WORKLOAD.json" },
                { "clientId", Environment.MachineName },
                { "toolName", "ToolA" },
                { "toolVersion", "1.2.3" },
                { "scenarioName", "Scenario01" },
                { "scenarioStartTime", expectedStartTime },
                { "scenarioEndTime",  expectedEndTime },
                { "metricName", "avg. latency" },
                { "metricValue", 123.45 },
                { "metricUnit", "milliseconds" },
                { "metricCategorization", "Latency" },
                { "metricDescription", "The average latency (in milliseconds)." },
                { "metricRelativity", MetricRelativity.LowerIsBetter },
                { "metricVerbosity", 3 },
                { "operatingSystemPlatform", PlatformID.Win32NT.ToString() },
                { "platformArchitecture", "win-arm64" },
                { "severityLevel", severityLevel },
                { "metadata", new Dictionary<string, object> { ["Metadata1"] = "1234", ["Metadata2"] = true } },
                { "metadata_host", new Dictionary<string, object> { ["HostMetadata1"] = "One", ["HostMetadata2"] = "Two" } },
                { "toolResults", "ToolA version 1.2.3 output" },
                { "tags", "Tag1,Tag2,Tag3" }
            };

            EventContext context = new EventContext(expectedActivityId, properties);
            string actualCsvMessage = MetricsCsvFileLogger.CreateMessage(context);

            Assert.IsTrue(actualCsvMessage.StartsWith(Environment.NewLine));

            Assert.AreEqual(
                $"\"{timestamp.ToString("o")}\"" +
                $",\"{expectedExperimentId}\"" +
                $",\"Test\"" +
                $",\"PERF-WORKLOAD.json\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"ToolA\"" +
                $",\"1.2.3\"" +
                $",\"Scenario01\"" +
                $",\"{expectedStartTime.ToString("o")}\"" +
                $",\"{expectedEndTime.ToString("o")}\"" +
                $",\"avg. latency\"" +
                $",\"123.45\"" +
                $",\"milliseconds\"" +
                $",\"Latency\"" +
                $",\"The average latency (in milliseconds).\"" +
                $",\"LowerIsBetter\"" +
                $",\"3\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"{executingAssembly.Name}\"" +
                $",\"{executingAssembly.Version}\"" +
                $",\"Win32NT\"" +
                $",\"win-arm64\"" +
                $",\"{expectedSeverityLevel}\"" +
                $",\"{expectedActivityId}\"" +
                $",\"{Guid.Empty}\"" +
                $",\"Metadata1=1234;Metadata2=True\"" +
                $",\"HostMetadata1=One;HostMetadata2=Two\"" +
                $",\"ToolA version 1.2.3 output\"" +
                $",\"Tag1,Tag2,Tag3\"",
                //
                // We are removing the line break and timestamp here because the timestamp
                // is variable depending upon when the test is ran. We are confirming the line
                // break above anyhow. The round-trip timestamp and quotes are 31 chars in length.
                actualCsvMessage.Substring(Environment.NewLine.Length));
        }

        [Test]
        public void MetricsCsvFileLoggerHandlesScenariosWhereTheMetricInformationIsMissingInTheContext()
        {
            Guid expectedExperimentId = Guid.NewGuid();
            Guid expectedActivityId = Guid.NewGuid();

            var properties = new Dictionary<string, object>
            {
                { "experimentId", expectedExperimentId },
                { "clientId", "linux-client-01" },
                { "executionProfileName", "PERF-WORKLOAD.json" },
                { "executionSystem", "Test" },
                { "operatingSystemPlatform", PlatformID.Unix.ToString() },
            };

            EventContext context = new EventContext(expectedActivityId, properties);
            string actualCsvMessage = MetricsCsvFileLogger.CreateMessage(context);

            Assert.IsTrue(actualCsvMessage.StartsWith(Environment.NewLine));

            Assert.AreEqual(
                $",\"{expectedExperimentId}\"" +
                $",\"Test\"" +
                $",\"PERF-WORKLOAD.json\"" +
                $",\"linux-client-01\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"{executingAssembly.Name}\"" +
                $",\"{executingAssembly.Version}\"" +
                $",\"Unix\"" +
                $",\"\"" +
                $",\"2\"" +
                $",\"{expectedActivityId}\"" +
                $",\"{Guid.Empty}\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"",
                //
                // We are removing the line break and timestamp here because the timestamp
                // is variable depending upon when the test is ran. We are confirming the line
                // break above anyhow. The round-trip timestamp and quotes are 31 chars in length.
                actualCsvMessage.Substring(Environment.NewLine.Length + 30));
        }

        [Test]
        public void MetricsCsvFileLoggerHandlesScenariosWhereAllExpectedInformationIsMissingInTheContext()
        {
            Guid expectedActivityId = Guid.NewGuid();

            // NONE of the expected properties exist in the EventContext. Whereas this is
            // never expected to happen, the logic should handle this gracefully.
            var properties = new Dictionary<string, object>();

            EventContext context = new EventContext(expectedActivityId, properties);
            string actualCsvMessage = MetricsCsvFileLogger.CreateMessage(context);

            Assert.IsTrue(actualCsvMessage.StartsWith(Environment.NewLine));

            Assert.AreEqual(
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"{executingAssembly.Name}\"" +
                $",\"{executingAssembly.Version}\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"2\"" +
                $",\"{expectedActivityId}\"" +
                $",\"{Guid.Empty}\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"",
                //
                // We are removing the line break and timestamp here because the timestamp
                // is variable depending upon when the test is ran. We are confirming the line
                // break above anyhow. The round-trip timestamp and quotes are 31 chars in length.
                actualCsvMessage.Substring(Environment.NewLine.Length + 30));
        }

        [Test]
        public void MetricsCsvFileLoggerHandlesNullValuesInTheContextProperties()
        {
            Guid expectedActivityId = Guid.NewGuid();
            Guid expectedParentActivityId = Guid.NewGuid();

            var properties = new Dictionary<string, object>
            {
                { "timestamp", null },
                { "experimentId", null },
                { "executionSystem", null },
                { "executionProfileName", null },
                { "clientId", null },
                { "toolName", null },
                { "toolVersion", null },
                { "scenarioName", null },
                { "scenarioStartTime", null },
                { "scenarioEndTime",  null },
                { "metricName", null },
                { "metricValue", null },
                { "metricUnit", null },
                { "metricCategorization", null },
                { "metricDescription", null },
                { "metricRelativity", null },
                { "metricVerbosity", null },
                { "operatingSystemPlatform", null },
                { "platformArchitecture", null },
                { "severityLevel", null },
                { "metadata", null },
                { "metadata_host", null },
                { "toolResults", null },
                { "tags", null }
            };

            EventContext context = new EventContext(expectedActivityId, properties);
            string actualCsvMessage = MetricsCsvFileLogger.CreateMessage(context);

            Assert.IsTrue(actualCsvMessage.StartsWith(Environment.NewLine));

            Assert.AreEqual(
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"{executingAssembly.Name}\"" +
                $",\"{executingAssembly.Version}\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"2\"" +
                $",\"{expectedActivityId}\"" +
                $",\"{Guid.Empty}\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"" +
                $",\"\"",
                //
                // We are removing the line break and timestamp here because the timestamp
                // is variable depending upon when the test is ran. We are confirming the line
                // break above anyhow. The round-trip timestamp and quotes are 31 chars in length.
                actualCsvMessage.Substring(Environment.NewLine.Length + 30));
        }
    }
}