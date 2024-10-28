// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    internal class VirtualClientLoggingExtensionsTests_BackwardsCompatibility
    {
        private Mock<ILogger> mockLogger;
        private EventContext mockEventContext;

        [SetUp]
        public void SetupTest()
        {
            this.mockLogger = new Mock<ILogger>();
            this.mockEventContext = new EventContext(Guid.NewGuid());
        }

        [Test]
        public void LogPerformanceCountersExtensionLogsTheExpectedEventsWhenSupportForOriginalSchemaIsRequested()
        {
            string expectedToolName = "AnyTool";
            string expectedToolVersion = "1.2.3.4";
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-100);
            DateTime expectedEndTime = DateTime.UtcNow;
            List<string> expectedTags = new List<string> { "Tag1", "Tag2" };
            var expectedMetadata = new Dictionary<string, IConvertible>(){
               { "Key1","Value1" },
               { "Key2","Value2" }
            };

            List<Metric> expectedCounters = new List<Metric>
            {
                new Metric("Metric1", 12345, "KB/sec", MetricRelativity.HigherIsBetter, tags: expectedTags, description: "Metric 1 description", metadata: expectedMetadata)
            };

            this.mockLogger.Object.LogPerformanceCounters(
                expectedToolName,
                expectedCounters,
                expectedStartTime,
                expectedEndTime,
                this.mockEventContext,
                expectedToolVersion,
                supportOriginalSchema: true);

            // Original Schema
            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metrics && eventId.Name == ("PerformanceCounter")),
                It.Is<EventContext>(context => context.Properties.Count == 18
                    && context.ActivityId == this.mockEventContext.ActivityId
                    && context.ParentActivityId == this.mockEventContext.ParentActivityId
                    && context.Properties.ContainsKey("counterName")
                    && context.Properties.ContainsKey("counterValue")
                    && context.Properties.ContainsKey("testName")
                    && context.Properties.ContainsKey("testStartTime")
                    && context.Properties.ContainsKey("testEndTime")
                    && context.Properties.ContainsKey("units")
                    && context.Properties["testName"].ToString() == "PerformanceCounter"
                    && context.Properties["testStartTime"].ToString() == expectedStartTime.ToString()
                    && context.Properties["testEndTime"].ToString() == expectedEndTime.ToString()
                    && context.Properties["counterName"].ToString() == expectedCounters[0].Name
                    && context.Properties["counterValue"].ToString() == expectedCounters[0].Value.ToString()
                    && context.Properties["units"].ToString() == "KB/sec"),
                null,
                null));

            // New Schema
            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metrics && eventId.Name == ("PerformanceCounter")),
                It.Is<EventContext>(context => context.Properties.Count == 18
                    && context.ActivityId == this.mockEventContext.ActivityId
                    && context.ParentActivityId == this.mockEventContext.ParentActivityId
                    && context.Properties.ContainsKey("toolName")
                    && context.Properties.ContainsKey("toolVersion")
                    && context.Properties.ContainsKey("metricName")
                    && context.Properties.ContainsKey("metricValue")
                    && context.Properties.ContainsKey("metricUnit")
                    && context.Properties.ContainsKey("scenarioName")
                    && context.Properties.ContainsKey("scenarioStartTime")
                    && context.Properties.ContainsKey("scenarioEndTime")
                    && context.Properties.ContainsKey("tags")
                    && context.Properties.ContainsKey("metadata_metrics")
                    && context.Properties["toolName"].ToString() == expectedToolName
                    && context.Properties["toolVersion"].ToString() == expectedToolVersion
                    && context.Properties["scenarioName"].ToString() == "PerformanceCounter"
                    && context.Properties["scenarioStartTime"].ToString() == expectedStartTime.ToString()
                    && context.Properties["scenarioEndTime"].ToString() == expectedEndTime.ToString()
                    && context.Properties["metricName"].ToString() == expectedCounters[0].Name
                    && context.Properties["metricValue"].ToString() == expectedCounters[0].Value.ToString()
                    && context.Properties["metricUnit"].ToString() == "KB/sec"
                    && context.Properties["metricDescription"].ToString() == "Metric 1 description"
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.HigherIsBetter.ToString()
                    && context.Properties["tags"].ToString() == string.Join(",", expectedTags)
                    && context.Properties["metadata_metrics"] == expectedCounters[0].Metadata as object),
                null,
                null));
        }

        [Test]
        public void LogMetricsExtensionLogsTheExpectedEventsWhenSupportForOriginalSchemaIsRequested()
        {
            string expectedScenarioName = "AnyTestName";
            string expectedMetricName = "AnyMetric";
            string expectedToolName = "ToolA";
            string expectedToolVersion = "1.2.3.4";
            string expectedToolResults = "Tool A: metric1=value 1 | metric 2=value 2";
            double expectedMetricValue = 123.456;
            string expectedUnits = "seconds";
            string expectedMetricCategorization = "instanceA";
            string expectedDescription = "Metric description";
            MetricRelativity expectedRelativity = MetricRelativity.LowerIsBetter;
            string expectedTestArguments = "--name=AnyTestName --runtime=100";
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-100);
            DateTime expectedEndTime = DateTime.UtcNow;
            List<string> expectedTags = new List<string> { "Tag1", "Tag2" };
            var expectedMetadata = new Dictionary<string, IConvertible>(){
               { "Key1","Value1" },
               { "Key2","Value2" }
            };

            this.mockLogger.Object.LogMetrics(
                expectedToolName,
                expectedScenarioName,
                expectedStartTime,
                expectedEndTime,
                expectedMetricName,
                expectedMetricValue,
                expectedUnits,
                expectedMetricCategorization,
                expectedTestArguments,
                expectedTags,
                this.mockEventContext,
                expectedRelativity,
                expectedDescription,
                expectedToolResults,
                expectedToolVersion,
                expectedMetadata,
                supportOriginalSchema: true);

            // Original Schema
            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metrics && eventId.Name.EndsWith("ScenarioResult")),
                It.Is<EventContext>(context => context.Properties.Count == 22
                    && context.ActivityId == this.mockEventContext.ActivityId
                    && context.ParentActivityId == this.mockEventContext.ParentActivityId
                    && context.Properties.ContainsKey("testName")
                    && context.Properties.ContainsKey("testStartTime")
                    && context.Properties.ContainsKey("testEndTime")
                    && context.Properties.ContainsKey("testArguments")
                    && context.Properties.ContainsKey("testResult")
                    && context.Properties.ContainsKey("units")
                    && context.Properties.ContainsKey("testedInstance")
                    && context.Properties["testName"].ToString() == expectedScenarioName
                    && context.Properties["testStartTime"].ToString() == expectedStartTime.ToString()
                    && context.Properties["testEndTime"].ToString() == expectedEndTime.ToString()
                    && context.Properties["testArguments"].ToString() == expectedTestArguments
                    && context.Properties["metricName"].ToString() == expectedMetricName
                    && context.Properties["testResult"].ToString() == expectedMetricValue.ToString()
                    && context.Properties["testedInstance"].ToString() == expectedMetricCategorization),
                null,
                null));

            // New Schema
            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metrics && eventId.Name.EndsWith("ScenarioResult")),
                It.Is<EventContext>(context => context.Properties.Count == 22
                    && context.ActivityId == this.mockEventContext.ActivityId
                    && context.ParentActivityId == this.mockEventContext.ParentActivityId
                    && context.Properties.ContainsKey("scenarioName")
                    && context.Properties.ContainsKey("scenarioStartTime")
                    && context.Properties.ContainsKey("scenarioEndTime")
                    && context.Properties.ContainsKey("scenarioArguments")
                    && context.Properties.ContainsKey("metricName")
                    && context.Properties.ContainsKey("metricValue")
                    && context.Properties.ContainsKey("metricCategorization")
                    && context.Properties.ContainsKey("metricDescription")
                    && context.Properties.ContainsKey("metricRelativity")
                    && context.Properties.ContainsKey("toolName")
                    && context.Properties.ContainsKey("toolVersion")
                    && context.Properties.ContainsKey("toolResults")
                    && context.Properties.ContainsKey("tags")
                    && context.Properties.ContainsKey("metadata_metrics")
                    && context.Properties["scenarioName"].ToString() == expectedScenarioName
                    && context.Properties["scenarioStartTime"].ToString() == expectedStartTime.ToString()
                    && context.Properties["scenarioEndTime"].ToString() == expectedEndTime.ToString()
                    && context.Properties["scenarioArguments"].ToString() == expectedTestArguments
                    && context.Properties["metricName"].ToString() == expectedMetricName
                    && context.Properties["metricValue"].ToString() == expectedMetricValue.ToString()
                    && context.Properties["metricCategorization"].ToString() == expectedMetricCategorization
                    && context.Properties["metricDescription"].ToString() == expectedDescription
                    && context.Properties["metricRelativity"].ToString() == expectedRelativity.ToString()
                    && context.Properties["toolName"].ToString() == expectedToolName
                    && context.Properties["tags"].ToString() == string.Join(",", expectedTags)
                    && context.Properties["metadata_metrics"] == expectedMetadata as Object
                    && context.Properties["toolVersion"].ToString() == expectedToolVersion
                    && context.Properties["toolResults"].ToString() == expectedToolResults
                    && context.Properties["tags"].ToString() == string.Join(",", expectedTags)),
                null,
                null));
        }
    }
}
