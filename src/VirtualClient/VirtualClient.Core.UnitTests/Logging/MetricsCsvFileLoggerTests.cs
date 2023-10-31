// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

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
                "ClientId",
                "Profile",
                "ProfileName",
                "ToolName",
                "ScenarioName",
                "ScenarioStartTime",
                "ScenarioEndTime",
                "MetricCategorization",
                "MetricName",
                "MetricValue",
                "MetricUnit",
                "MetricDescription",
                "MetricRelativity",
                "ExecutionSystem",
                "OperatingSystemPlatform",
                "OperationId",
                "OperationParentId",
                "AppHost",
                "AppName",
                "AppVersion",
                "AppTelemetryVersion",
                "Tags"
            };

            // Order matters, so we are doing an explicit equality check here.
            string expectedHeaderString = string.Join(",", expectedColumnHeaders.Select(h => $"\"{h}\""));
            Assert.AreEqual(expectedHeaderString, EventContextLoggingExtensions.GetCsvHeaders());
        }

        [Test]
        public void MetricsCsvFileLoggerWritesTheExpectedCsvToFiles_1()
        {
            Guid expectedExperimentId = Guid.NewGuid();
            Guid expectedActivityId = Guid.NewGuid();
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-30);
            DateTime expectedEndTime = DateTime.UtcNow;

            var properties = new Dictionary<string, object>
            {
                { "experimentId", expectedExperimentId },
                { "agentId", Environment.MachineName },
                { "executionProfile", "PERF-WORKLOAD (win-x64)" },
                { "executionProfileName", "PERF-WORKLOAD.json" },
                { "executionSystem", "Test" },
                { "operatingSystemPlatform", PlatformID.Unix.ToString() },
                { "scenarioName", "Scenario01" },
                { "scenarioStartTime", expectedStartTime },
                { "scenarioEndTime",  expectedEndTime },
                { "scenarioArguments", string.Empty },
                { "metricName", "avg. latency" },
                { "metricValue", 123.45 },
                { "metricUnit", "milliseconds" },
                { "metricCategorization", "Latency" },
                { "metricDescription", "The average latency (in milliseconds)." },
                { "metricRelativity", MetricRelativity.LowerIsBetter },
                { "toolName", "ToolA" },
                { "tags", "Tag1,Tag2,Tag3" }
            };

            EventContext context = new EventContext(expectedActivityId, properties);
            string actualCsvMessage = MetricsCsvFileLogger.CreateMessage(context);

            Assert.IsTrue(actualCsvMessage.StartsWith(Environment.NewLine));
            // Assert.AreEqual(468, actualCsvMessage.Length,);

            Assert.AreEqual(
                $",\"{expectedExperimentId}\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"PERF-WORKLOAD (win-x64)\"" +
                $",\"PERF-WORKLOAD.json\"" +
                $",\"ToolA\"" +
                $",\"Scenario01\"" +
                $",\"{expectedStartTime.ToString("o")}\"" +
                $",\"{expectedEndTime.ToString("o")}\"" +
                $",\"Latency\"" +
                $",\"avg. latency\"" +
                $",\"123.45\"" +
                $",\"milliseconds\"" +
                $",\"The average latency (in milliseconds).\"" +
                $",\"LowerIsBetter\"" +
                $",\"Test\"" +
                $",\"Unix\"" +
                $",\"{expectedActivityId}\"" +
                $",\"{Guid.Empty}\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"{executingAssembly.Name}\"" +
                $",\"{executingAssembly.Version}\"" +
                $",\"{loggingAssembly.Version}\"" +
                $",\"Tag1,Tag2,Tag3\"",
                //
                // We are removing the line break and timestamp here because the timestamp
                // is variable depending upon when the test is ran. We are confirming the line
                // break above anyhow. The round-trip timestamp and quotes are 31 chars in length.
                actualCsvMessage.Substring(Environment.NewLine.Length + 30));
        }

        [Test]
        public void MetricsCsvFileLoggerWritesTheExpectedCsvToFiles_2()
        {
            Guid expectedExperimentId = Guid.NewGuid();
            Guid expectedActivityId = Guid.NewGuid();
            Guid expectedParentActivityId = Guid.NewGuid();
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-30);
            DateTime expectedEndTime = DateTime.UtcNow;

            var properties = new Dictionary<string, object>
            {
                { "experimentId", expectedExperimentId },
                { "agentId", Environment.MachineName },
                { "executionProfile", "PERF-WORKLOAD (win-x64)" },
                { "executionProfileName", "PERF-WORKLOAD.json" },
                { "executionSystem", "Test" },
                { "operatingSystemPlatform", PlatformID.Win32NT.ToString() },
                { "scenarioName", "Scenario01" },
                { "scenarioStartTime", expectedStartTime },
                { "scenarioEndTime",  expectedEndTime },
                { "scenarioArguments", string.Empty },
                { "metricName", "avg. latency" },
                { "metricValue", 123.45 },
                { "metricUnit", "milliseconds" },
                { "metricCategorization", "Latency" },
                { "metricDescription", "The average latency (in milliseconds)." },
                { "metricRelativity", MetricRelativity.LowerIsBetter },
                { "toolName", "ToolA" },
                { "tags", "Tag1,Tag2,Tag3" }
            };

            EventContext context = new EventContext(expectedActivityId, expectedParentActivityId, properties);
            string actualCsvMessage = MetricsCsvFileLogger.CreateMessage(context);

            Assert.IsTrue(actualCsvMessage.StartsWith(Environment.NewLine));
            // Assert.AreEqual(471, actualCsvMessage.Length);

            Assert.AreEqual(
                $",\"{expectedExperimentId}\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"PERF-WORKLOAD (win-x64)\"" +
                $",\"PERF-WORKLOAD.json\"" +
                $",\"ToolA\"" +
                $",\"Scenario01\"" +
                $",\"{expectedStartTime.ToString("o")}\"" +
                $",\"{expectedEndTime.ToString("o")}\"" +
                $",\"Latency\"" +
                $",\"avg. latency\"" +
                $",\"123.45\"" +
                $",\"milliseconds\"" +
                $",\"The average latency (in milliseconds).\"" +
                $",\"LowerIsBetter\"" +
                $",\"Test\"" +
                $",\"Win32NT\"" +
                $",\"{expectedActivityId}\"" +
                $",\"{expectedParentActivityId}\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"{executingAssembly.Name}\"" +
                $",\"{executingAssembly.Version}\"" +
                $",\"{loggingAssembly.Version}\"" +
                $",\"Tag1,Tag2,Tag3\"",
                //
                // We are removing the line break and timestamp here because the timestamp
                // is variable depending upon when the test is ran. We are confirming the line
                // break above anyhow. The round-trip timestamp and quotes are 31 chars in length.
                actualCsvMessage.Substring(Environment.NewLine.Length + 30));
        }

        [Test]
        public void MetricsCsvFileLoggerHandlesScenariosWhereTheMetricInformationIsMissingInTheContext()
        {
            Guid expectedExperimentId = Guid.NewGuid();
            Guid expectedActivityId = Guid.NewGuid();

            var properties = new Dictionary<string, object>
            {
                { "experimentId", expectedExperimentId },
                { "agentId", Environment.MachineName },
                { "executionProfile", "PERF-WORKLOAD (win-x64)" },
                { "executionProfileName", "PERF-WORKLOAD.json" },
                { "executionSystem", "Test" },
                { "operatingSystemPlatform", PlatformID.Unix.ToString() },
            };

            EventContext context = new EventContext(expectedActivityId, properties);
            string actualCsvMessage = MetricsCsvFileLogger.CreateMessage(context);

            Assert.IsTrue(actualCsvMessage.StartsWith(Environment.NewLine));
            // Assert.AreEqual(295, actualCsvMessage.Length);

            Assert.AreEqual(
                $",\"{expectedExperimentId}\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"PERF-WORKLOAD (win-x64)\"" +
                $",\"PERF-WORKLOAD.json\"" +
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
                $",\"Test\"" +
                $",\"Unix\"" +
                $",\"{expectedActivityId}\"" +
                $",\"{Guid.Empty}\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"{executingAssembly.Name}\"" +
                $",\"{executingAssembly.Version}\"" +
                $",\"{loggingAssembly.Version}\"" +
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
            // Assert.AreEqual(203, actualCsvMessage.Length);

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
                $",\"{expectedActivityId}\"" +
                $",\"{Guid.Empty}\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"{executingAssembly.Name}\"" +
                $",\"{executingAssembly.Version}\"" +
                $",\"{loggingAssembly.Version}\"" +
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
                { "experimentId", null },
                { "agentId", null },
                { "executionProfile", null },
                { "executionProfileName", null },
                { "executionSystem", null },
                { "operatingSystemPlatform", null },
                { "scenarioName", null },
                { "scenarioStartTime", null },
                { "scenarioEndTime",  null },
                { "scenarioArguments", null },
                { "metricName", null },
                { "metricValue", null },
                { "metricUnit", null },
                { "metricCategorization", null },
                { "metricDescription", null },
                { "metricRelativity", null },
                { "toolName", null },
                { "tags", null }
            };

            EventContext context = new EventContext(expectedActivityId, properties);
            string actualCsvMessage = MetricsCsvFileLogger.CreateMessage(context);

            Assert.IsTrue(actualCsvMessage.StartsWith(Environment.NewLine));
            // Assert.AreEqual(203, actualCsvMessage.Length);

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
                $",\"{expectedActivityId}\"" +
                $",\"{Guid.Empty}\"" +
                $",\"{Environment.MachineName}\"" +
                $",\"{executingAssembly.Name}\"" +
                $",\"{executingAssembly.Version}\"" +
                $",\"{loggingAssembly.Version}\"" +
                $",\"\"",
                //
                // We are removing the line break and timestamp here because the timestamp
                // is variable depending upon when the test is ran. We are confirming the line
                // break above anyhow. The round-trip timestamp and quotes are 31 chars in length.
                actualCsvMessage.Substring(Environment.NewLine.Length + 30));
        }
    }
}