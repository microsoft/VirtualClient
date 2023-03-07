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
    using Newtonsoft.Json;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class VirtualClientLoggingExtensionsTests
    {
        private MockFixture mockFixture;
        private Mock<ILogger> mockLogger;
        private EventContext mockEventContext;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockLogger = new Mock<ILogger>();
            this.mockEventContext = new EventContext(Guid.NewGuid());
        }

        [Test]
        [TestCase("PropertyName", "propertyName")]
        [TestCase("propertyName", "propertyName")]
        public void CamelCasedExtensionCreatesTheExpectedPropertyName(string propertyName, string expectedValue)
        {
            string actualValue = propertyName.CamelCased();
            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        [TestCase(LogLevel.Critical)]
        [TestCase(LogLevel.Debug)]
        [TestCase(LogLevel.Error)]
        [TestCase(LogLevel.Information)]
        [TestCase(LogLevel.None)]
        [TestCase(LogLevel.Trace)]
        [TestCase(LogLevel.Warning)]
        public void LogMessageExtensionLogsEventsWithTheExpectedLogLevel(LogLevel expectedLogLevel)
        {
            string expectedEventName = "AnyMessage";
            this.mockLogger.Object.LogMessage(expectedEventName, expectedLogLevel, LogType.Trace, this.mockEventContext);
            this.mockLogger.Verify(logger => logger.Log(
                expectedLogLevel,
                It.IsAny<EventId>(),
                It.Is<EventContext>(context => context.ActivityId == this.mockEventContext.ActivityId
                    && context.ParentActivityId == this.mockEventContext.ParentActivityId
                    && context.TransactionId == this.mockEventContext.TransactionId),
                null,
                null));
        }

        [Test]
        [TestCase(LogType.Error)]
        [TestCase(LogType.SystemEvent)]
        [TestCase(LogType.Metrics)]
        [TestCase(LogType.Trace)]
        [TestCase(LogType.Undefined)]
        public void LogMessageExtensionLogsTheExpectedEventsForTheVariousLogTypeDesignations(LogType expectedLogType)
        {
            string expectedEventName = "AnyMessage";
            this.mockLogger.Object.LogMessage(expectedEventName, LogLevel.Information, expectedLogType, this.mockEventContext);
            this.mockLogger.Verify(logger => logger.Log(
                It.IsAny<LogLevel>(),
                It.Is<EventId>(eventId => eventId.Id == (int) expectedLogType && eventId.Name == expectedEventName),
                It.Is<EventContext>(context => context.ActivityId == this.mockEventContext.ActivityId
                    && context.ParentActivityId == this.mockEventContext.ParentActivityId
                    && context.TransactionId == this.mockEventContext.TransactionId),
                null,
                null));
        }

        [Test]
        public void LogMessageExtensionDoesNotSideEffectOrChangeAnEventContextProvided()
        {
            EventContext originalContext = new EventContext(Guid.NewGuid(), new Dictionary<string, object>
            {
                ["property1"] = "Any Value",
                ["property2"] = 1234
            });

            EventContext cloneOfOriginal = originalContext.Clone();

            this.mockLogger
               .Setup(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<EventContext>(), null, null))
               .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, state, exc, formatter) =>
               {
                   // A clone is used to avoid side effecting the original
                   EventContext actualContext = state;
                   Assert.IsFalse(object.ReferenceEquals(originalContext, actualContext));
               });

            this.mockLogger.Object.LogMessage("AnyMessage", LogLevel.Information, LogType.Trace, originalContext);

            // The original should not have been changed.
            Assert.AreEqual(originalContext.ActivityId, cloneOfOriginal.ActivityId);
            Assert.AreEqual(originalContext.ParentActivityId, cloneOfOriginal.ParentActivityId);
            Assert.AreEqual(originalContext.TransactionId, cloneOfOriginal.TransactionId);
            Assert.AreEqual(originalContext.Properties.Count, cloneOfOriginal.Properties.Count);
            CollectionAssert.AreEquivalent(originalContext.Properties.Keys, cloneOfOriginal.Properties.Keys);
            CollectionAssert.AreEquivalent(originalContext.Properties.Values, cloneOfOriginal.Properties.Values);
        }

        [Test]
        public void LogMessageExtensionLogsTheExpectedEventsForMethodBodyLogic()
        {
            string exampleEvent = "AnyEvent";
            EventContext exampleEventContext = new EventContext(Guid.NewGuid());
            List <Tuple<string, LogLevel, EventContext>> eventsLogged = new List<Tuple<string, LogLevel, EventContext>>();

            this.mockLogger
                .Setup(logger => logger.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<EventContext>(),
                    null,
                    null))
                .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, context, exc, formatter) =>
                {
                    eventsLogged.Add(new Tuple<string, LogLevel, EventContext>(eventId.Name, level, context));
                });

            this.mockLogger.Object.LogMessage(exampleEvent, exampleEventContext, () => { });

            Assert.IsTrue(eventsLogged.Count == 2);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{exampleEvent}Start"
                && evt.Item2 == LogLevel.Information
                && evt.Item3.ActivityId == exampleEventContext.ActivityId) == 1);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{exampleEvent}Stop"
                && evt.Item2 == LogLevel.Information
                && evt.Item3.ActivityId == exampleEventContext.ActivityId) == 1);
        }

        [Test]
        public void LogMessageExtensionLogsTheExpectedEventsWhenErrorsOccurDuringTheExecutionOfMethodBodyLogic()
        {
            string exampleEvent = "AnyEvent";
            EventContext exampleEventContext = new EventContext(Guid.NewGuid());
            List<Tuple<string, LogLevel, EventContext>> eventsLogged = new List<Tuple<string, LogLevel, EventContext>>();

            this.mockLogger
                .Setup(logger => logger.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<EventContext>(),
                    null,
                    null))
                .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, context, exc, formatter) =>
                {
                    eventsLogged.Add(new Tuple<string, LogLevel, EventContext>(eventId.Name, level, context));
                });

            try
            {
                this.mockLogger.Object.LogMessage(exampleEvent, exampleEventContext, () => { throw new Exception(); });
            }
            catch
            {
            }

            Assert.IsTrue(eventsLogged.Count == 3);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{exampleEvent}Start"
                && evt.Item2 == LogLevel.Information
                && evt.Item3.ActivityId == exampleEventContext.ActivityId) == 1);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{exampleEvent}Error"
                && evt.Item2 == LogLevel.Error
                && evt.Item3.ActivityId == exampleEventContext.ActivityId) == 1);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{exampleEvent}Stop"
                && evt.Item2 == LogLevel.Information
                && evt.Item3.ActivityId == exampleEventContext.ActivityId) == 1);
        }

        [Test]
        public void LogMessageExtensionProtectsTheUserFromMisusingItBySupplyingAnAsynchronousTaskAsTheMethodBodyLogic()
        {
            Assert.ThrowsAsync<ArgumentException>(
                () => this.mockLogger.Object.LogMessage("AnyMessage", this.mockEventContext, () => { return Task.Run(() => { }); }));
        }

        [Test]
        public async Task LogMessageAsyncExtensionHandlesAsynchronousOperationsCorrectly()
        {
            bool there = false;
            await this.mockLogger.Object.LogMessageAsync("AnyMessage", this.mockEventContext, async () =>
            {
                await Task.Delay(1000).ConfigureAwait(false);
                there = true;
            });

            Assert.IsTrue(there);
        }

        [Test]
        [TestCase(LogLevel.Critical)]
        [TestCase(LogLevel.Debug)]
        [TestCase(LogLevel.Error)]
        [TestCase(LogLevel.Information)]
        [TestCase(LogLevel.None)]
        [TestCase(LogLevel.Trace)]
        [TestCase(LogLevel.Warning)]
        public void LogErrorMessageExtensionLogsTheExpectedExceptionInformation(LogLevel expectedLevel)
        {
            Exception expectedError = null;
            try
            {
                // To ensure a call stack is included.
                throw new Exception("An error occurred");
            }
            catch (Exception exc)
            {
                expectedError = exc;
            }

            this.mockLogger.Object.LogErrorMessage(expectedError, this.mockEventContext);
            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Error,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Error && eventId.Name == expectedError.Message),
                It.Is<EventContext>(context => context.ActivityId == this.mockEventContext.ActivityId
                    && context.ParentActivityId == this.mockEventContext.ParentActivityId
                    && context.TransactionId == this.mockEventContext.TransactionId),
                null,
                null));
        }

        [Test]
        public void LogErrorMessageExtensionIncludesTheErrorMessageAndCallstackInTheEventContext()
        {
            Exception expectedError = null;
            try
            {
                // To ensure a call stack is included.
                throw new Exception("An error occurred");
            }
            catch (Exception exc)
            {
                expectedError = exc;
            }

            this.mockLogger
                .Setup(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<EventContext>(), null, null))
                .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, state, exc, formatter) =>
                {
                    Assert.IsNotNull(state);
                    Assert.IsTrue(state.Properties.ContainsKey("error"));
                    Assert.IsTrue(state.Properties.ContainsKey("errorCallstack"));

                    List<object> errorEntries = state.Properties["error"] as List<object>;
                    Assert.IsNotNull(errorEntries);
                    Assert.IsTrue(errorEntries.Count == 1);

                    // The error objects are saved to the EventContext properties as an array of
                    // objects each representing a distinct exception. Exceptions can have inner
                    // exceptions and so on. We keep each of those separate for clarity and telemetry
                    // categorization purposes.
                    // e.g.
                    // { errorType = "System.Exception", errorMessage = "An error occurred." }
                    object expectedError = new
                    {
                        errorType = typeof(Exception).FullName,
                        errorMessage = "An error occurred"
                    };

                    Assert.AreEqual(JsonConvert.SerializeObject(expectedError), JsonConvert.SerializeObject(errorEntries.First()));
                });


            this.mockLogger.Object.LogErrorMessage(expectedError, this.mockEventContext);
        }

        [Test]
        public void LogErrorMessageExtensionDoesNotSideEffectOrChangeAnEventContextProvided()
        {
            EventContext originalContext = new EventContext(Guid.NewGuid(), new Dictionary<string, object>
            {
                ["property1"] = "Any Value",
                ["property2"] = 1234
            });

            EventContext cloneOfOriginal = originalContext.Clone();

            this.mockLogger
               .Setup(logger => logger.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<EventContext>(), null, null))
               .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, state, exc, formatter) =>
               {
                   // A clone is used to avoid side effecting the original
                   EventContext actualContext = state;
                   Assert.IsFalse(object.ReferenceEquals(originalContext, actualContext));
               });

            this.mockLogger.Object.LogErrorMessage(new Exception("AnyMessage"), originalContext);

            // The original should not have been changed.
            Assert.AreEqual(originalContext.ActivityId, cloneOfOriginal.ActivityId);
            Assert.AreEqual(originalContext.ParentActivityId, cloneOfOriginal.ParentActivityId);
            Assert.AreEqual(originalContext.TransactionId, cloneOfOriginal.TransactionId);
            Assert.AreEqual(originalContext.Properties.Count, cloneOfOriginal.Properties.Count);
            CollectionAssert.AreEquivalent(originalContext.Properties.Keys, cloneOfOriginal.Properties.Keys);
            CollectionAssert.AreEquivalent(originalContext.Properties.Values, cloneOfOriginal.Properties.Values);
        }

        [Test]
        public void LogPerformanceCountersExtensionLogsTheExpectedEvents_Scenario1()
        {
            string expectedToolName = "AnyTool";
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-100);
            DateTime expectedEndTime = DateTime.UtcNow;
            List<string> expectedTags = new List<string> { "Tag1", "Tag2" };
            var expectedMetadata = new Dictionary<string, IConvertible>(){
               { "Key1","Value1" },
               { "Key2","Value2" }
            };

            List<Metric> expectedCounters = new List<Metric>
            {
                new Metric("Metric1", 12345, MetricRelativity.HigherIsBetter, tags: expectedTags, description: "Metric 1 description", metadata: expectedMetadata)
            };

            this.mockLogger.Object.LogPerformanceCounters(
                expectedToolName,
                expectedCounters,
                expectedStartTime,
                expectedEndTime,
                this.mockEventContext);

            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metrics && eventId.Name == ("PerformanceCounter")),
                It.Is<EventContext>(context => context.Properties.Count == 12
                    && context.ActivityId == this.mockEventContext.ActivityId
                    && context.ParentActivityId == this.mockEventContext.ParentActivityId
                    && context.Properties.ContainsKey("toolName")
                    && context.Properties.ContainsKey("toolVersion")
                    && context.Properties.ContainsKey("metricName")
                    && context.Properties.ContainsKey("metricValue")
                    && context.Properties.ContainsKey("metricUnit")
                    && context.Properties.ContainsKey("metricDescription")
                    && context.Properties.ContainsKey("metricRelativity")
                    && context.Properties.ContainsKey("scenarioName")
                    && context.Properties.ContainsKey("scenarioStartTime")
                    && context.Properties.ContainsKey("scenarioEndTime")
                    && context.Properties.ContainsKey("tags")
                    && context.Properties.ContainsKey("metricMetadata")
                    && context.Properties["toolName"].ToString() == expectedToolName
                    && context.Properties["scenarioName"].ToString() == "PerformanceCounter"
                    && context.Properties["scenarioStartTime"].ToString() == expectedStartTime.ToString()
                    && context.Properties["scenarioEndTime"].ToString() == expectedEndTime.ToString()
                    && context.Properties["metricName"].ToString() == expectedCounters[0].Name
                    && context.Properties["metricValue"].ToString() == expectedCounters[0].Value.ToString()
                    && context.Properties["metricUnit"].ToString() == string.Empty
                    && context.Properties["metricDescription"].ToString() == "Metric 1 description"
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.HigherIsBetter.ToString()
                    && context.Properties["tags"].ToString() == string.Join(",", expectedTags)
                   && context.Properties["metricMetadata"] == expectedCounters[0].Metadata as object),
                null,
                null));
        }

        [Test]
        public void LogPerformanceCountersExtensionLogsTheExpectedEvents_Scenario2()
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
                expectedToolVersion);

            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metrics && eventId.Name == ("PerformanceCounter")),
                It.Is<EventContext>(context => context.Properties.Count == 12
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
                    && context.Properties.ContainsKey("metricMetadata")
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
                    && context.Properties["metricMetadata"] == expectedCounters[0].Metadata as object),
                null,
                null));
        }

        [Test]
        public void LogFailedMetricExtensionLogsTheExpectedInformation()
        {
            string expectedScenarioName = "AnyTestName";
            string expectedToolName = "ToolA";
            string expectedToolVersion = "1.2.3";
            string expectedMetricCategorization = "instanceA";
            string expectedScenarioArguments = "--name=AnyTestName --runtime=100";
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-100);
            DateTime expectedEndTime = DateTime.UtcNow;
            List<string> expectedTags = new List<string> { "Tag1", "Tag2" };

            this.mockLogger.Object.LogFailedMetric(
                expectedToolName,
                expectedScenarioName,
                expectedStartTime,
                expectedEndTime,
                this.mockEventContext,
                expectedScenarioArguments,
                expectedMetricCategorization,
                expectedToolVersion,
                expectedTags);

            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metrics && eventId.Name.EndsWith("ScenarioResult")),
                It.Is<EventContext>(context => context.Properties.Count == 15
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
                    && context.Properties.ContainsKey("metricMetadata")
                    && context.Properties["scenarioName"].ToString() == expectedScenarioName
                    && context.Properties["scenarioStartTime"].ToString() == expectedStartTime.ToString()
                    && context.Properties["scenarioEndTime"].ToString() == expectedEndTime.ToString()
                    && context.Properties["scenarioArguments"].ToString() == expectedScenarioArguments
                    && context.Properties["metricName"].ToString() == "Failed"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == expectedMetricCategorization
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution failed for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.LowerIsBetter.ToString()
                    && context.Properties["toolName"].ToString() == expectedToolName
                    && context.Properties["toolVersion"].ToString() == expectedToolVersion
                    && context.Properties["tags"].ToString() == string.Join(",", expectedTags)
                    && context.Properties["metricMetadata"].ToString() == string.Empty),
                null,
                null));
        }

        [Test]
        public void LogFailedMetricExtensionLogsTheExpectedInformation_WhenMinimumInfoIsProvided()
        {
            string expectedScenarioName = "AnyTestName";
            string expectedToolName = "ToolA";
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-100);
            DateTime expectedEndTime = DateTime.UtcNow;
            List<string> expectedTags = new List<string> { "Tag1", "Tag2" };

            this.mockLogger.Object.LogFailedMetric(expectedToolName, expectedScenarioName, expectedStartTime, expectedEndTime, this.mockEventContext);

            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metrics && eventId.Name.EndsWith("ScenarioResult")),
                It.Is<EventContext>(context => context.Properties.Count == 15
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
                    && context.Properties.ContainsKey("metricMetadata")
                    && context.Properties["scenarioName"].ToString() == expectedScenarioName
                    && context.Properties["scenarioStartTime"].ToString() == expectedStartTime.ToString()
                    && context.Properties["scenarioEndTime"].ToString() == expectedEndTime.ToString()
                    && context.Properties["scenarioArguments"].ToString() == string.Empty
                    && context.Properties["metricName"].ToString() == "Failed"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == string.Empty
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution failed for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.LowerIsBetter.ToString()
                    && context.Properties["toolName"].ToString() == expectedToolName
                    && context.Properties["toolVersion"].ToString() == string.Empty
                    && context.Properties["tags"].ToString() == string.Empty
                    && context.Properties["metricMetadata"].ToString() == string.Empty),
                null,
                null));
        }

        [Test]
        public void LogSuccessMetricExtensionLogsTheExpectedInformation()
        {
            string expectedScenarioName = "AnyTestName";
            string expectedToolName = "ToolA";
            string expectedToolVersion = "1.2.3";
            string expectedMetricCategorization = "instanceA";
            string expectedScenarioArguments = "--name=AnyTestName --runtime=100";
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-100);
            DateTime expectedEndTime = DateTime.UtcNow;
            List<string> expectedTags = new List<string> { "Tag1", "Tag2" };

            this.mockLogger.Object.LogSuccessMetric(
                expectedToolName,
                expectedScenarioName,
                expectedStartTime,
                expectedEndTime,
                this.mockEventContext,
                expectedScenarioArguments,
                expectedMetricCategorization,
                expectedToolVersion,
                expectedTags);

            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metrics && eventId.Name.EndsWith("ScenarioResult")),
                It.Is<EventContext>(context => context.Properties.Count == 15
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
                    && context.Properties.ContainsKey("metricMetadata")
                    && context.Properties["scenarioName"].ToString() == expectedScenarioName
                    && context.Properties["scenarioStartTime"].ToString() == expectedStartTime.ToString()
                    && context.Properties["scenarioEndTime"].ToString() == expectedEndTime.ToString()
                    && context.Properties["scenarioArguments"].ToString() == expectedScenarioArguments
                    && context.Properties["metricName"].ToString() == "Succeeded"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == expectedMetricCategorization
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution succeeded for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.HigherIsBetter.ToString()
                    && context.Properties["toolName"].ToString() == expectedToolName
                    && context.Properties["toolVersion"].ToString() == expectedToolVersion
                    && context.Properties["tags"].ToString() == string.Join(",", expectedTags)
                    && context.Properties["metricMetadata"].ToString() == string.Empty),
                null,
                null));
        }

        [Test]
        public void LogSuccessMetricExtensionLogsTheExpectedInformation_WhenMinimumInfoIsProvided()
        {
            string expectedScenarioName = "AnyTestName";
            string expectedToolName = "ToolA";
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-100);
            DateTime expectedEndTime = DateTime.UtcNow;
            List<string> expectedTags = new List<string> { "Tag1", "Tag2" };

            this.mockLogger.Object.LogSuccessMetric(expectedToolName, expectedScenarioName, expectedStartTime, expectedEndTime, this.mockEventContext);

            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metrics && eventId.Name.EndsWith("ScenarioResult")),
                It.Is<EventContext>(context => context.Properties.Count == 15
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
                    && context.Properties.ContainsKey("metricMetadata")
                    && context.Properties["scenarioName"].ToString() == expectedScenarioName
                    && context.Properties["scenarioStartTime"].ToString() == expectedStartTime.ToString()
                    && context.Properties["scenarioEndTime"].ToString() == expectedEndTime.ToString()
                    && context.Properties["scenarioArguments"].ToString() == string.Empty
                    && context.Properties["metricName"].ToString() == "Succeeded"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == string.Empty
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution succeeded for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.HigherIsBetter.ToString()
                    && context.Properties["toolName"].ToString() == expectedToolName
                    && context.Properties["toolVersion"].ToString() == string.Empty
                    && context.Properties["tags"].ToString() == string.Empty
                    && context.Properties["metricMetadata"].ToString() == string.Empty),
                null,
                null));
        }

        [Test]
        public void LogFailedMetricExtensionOnComponentLogsTheExpectedInformation_scenario1()
        {
            // Scenario:
            // Minimum info provided. Additionally, the parameters for the component do not contain a 'Scenario'
            // definition.

            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters.Clear();
            component.LogFailedMetric();

            var loggedMetric = this.mockFixture.Logger.FirstOrDefault();

            Assert.IsNotNull(loggedMetric);
            Assert.IsInstanceOf<EventContext>(loggedMetric.Item3);
            EventContext context = loggedMetric.Item3 as EventContext;

            Assert.IsTrue(context.Properties.Count == 15
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
                    && context.Properties.ContainsKey("metricMetadata")
                    && context.Properties["scenarioName"].ToString() == "Outcome"
                    && context.Properties["scenarioArguments"].ToString() == string.Empty
                    && context.Properties["metricName"].ToString() == "Failed"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == string.Empty
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution failed for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.LowerIsBetter.ToString()
                    && context.Properties["toolName"].ToString() == component.TypeName
                    && context.Properties["toolVersion"].ToString() == string.Empty
                    && context.Properties["tags"].ToString() == string.Empty
                    && context.Properties["metricMetadata"].ToString() == string.Empty);
        }

        [Test]
        public void LogFailedMetricExtensionOnComponentLogsTheExpectedInformation_scenario2()
        {
            // Scenario:
            // Minimum info provided. Additionally, the parameters for the component contain a 'Scenario'
            // definition.

            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters[nameof(component.Scenario)] = "Any_Outcome";
            component.LogFailedMetric();

            var loggedMetric = this.mockFixture.Logger.FirstOrDefault();

            Assert.IsNotNull(loggedMetric);
            Assert.IsInstanceOf<EventContext>(loggedMetric.Item3);
            EventContext context = loggedMetric.Item3 as EventContext;

            Assert.IsTrue(context.Properties.Count == 15
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
                    && context.Properties.ContainsKey("metricMetadata")
                    && context.Properties["scenarioName"].ToString() == "Any_Outcome"
                    && context.Properties["scenarioArguments"].ToString() == string.Empty
                    && context.Properties["metricName"].ToString() == "Failed"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == string.Empty
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution failed for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.LowerIsBetter.ToString()
                    && context.Properties["toolName"].ToString() == component.TypeName
                    && context.Properties["toolVersion"].ToString() == string.Empty
                    && context.Properties["tags"].ToString() == string.Empty
                    && context.Properties["metricMetadata"].ToString() == string.Empty);
        }

        [Test]
        public void LogFailedMetricExtensionOnComponentLogsTheExpectedInformation_scenario3()
        {
            // Scenario:
            // Complete information provided including scenario name, tool name, and timestamps.

            string expectedScenarioName = "AnyTestName";
            string expectedToolName = "ToolA";
            string expectedToolVersion = "1.2.3";
            string expectedMetricCategorization = "instanceA";
            string expectedScenarioArguments = "--name=AnyTestName --runtime=100";
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-100);
            DateTime expectedEndTime = DateTime.UtcNow;

            TestExecutor component = new TestExecutor(this.mockFixture);
            component.LogFailedMetric(expectedToolName, expectedToolVersion, expectedScenarioName, expectedScenarioArguments, expectedMetricCategorization, expectedStartTime, expectedEndTime);

            var loggedMetric = this.mockFixture.Logger.FirstOrDefault();

            Assert.IsNotNull(loggedMetric);
            Assert.IsInstanceOf<EventContext>(loggedMetric.Item3);
            EventContext context = loggedMetric.Item3 as EventContext;

            Assert.IsTrue(context.Properties.Count == 15
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
                    && context.Properties.ContainsKey("metricMetadata")
                    && context.Properties["scenarioName"].ToString() == expectedScenarioName
                    && context.Properties["scenarioArguments"].ToString() == expectedScenarioArguments
                    && context.Properties["metricName"].ToString() == "Failed"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == expectedMetricCategorization
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution failed for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.LowerIsBetter.ToString()
                    && context.Properties["toolName"].ToString() == expectedToolName
                    && context.Properties["toolVersion"].ToString() == expectedToolVersion
                    && context.Properties["tags"].ToString() == string.Empty
                    && context.Properties["metricMetadata"].ToString() == string.Empty);
        }

        [Test]
        public void LogSuccessMetricExtensionOnComponentLogsTheExpectedInformation_scenario1()
        {
            // Scenario:
            // Minimum info provided. Additionally, the parameters for the component do not contain a 'Scenario'
            // definition.

            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters.Clear();
            component.LogSuccessMetric();

            var loggedMetric = this.mockFixture.Logger.FirstOrDefault();

            Assert.IsNotNull(loggedMetric);
            Assert.IsInstanceOf<EventContext>(loggedMetric.Item3);
            EventContext context = loggedMetric.Item3 as EventContext;

            Assert.IsTrue(context.Properties.Count == 15
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
                    && context.Properties.ContainsKey("metricMetadata")
                    && context.Properties["scenarioName"].ToString() == "Outcome"
                    && context.Properties["scenarioArguments"].ToString() == string.Empty
                    && context.Properties["metricName"].ToString() == "Succeeded"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == string.Empty
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution succeeded for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.HigherIsBetter.ToString()
                    && context.Properties["toolName"].ToString() == component.TypeName
                    && context.Properties["toolVersion"].ToString() == string.Empty
                    && context.Properties["tags"].ToString() == string.Empty
                    && context.Properties["metricMetadata"].ToString() == string.Empty);
        }

        [Test]
        public void LogSuccessMetricExtensionOnComponentLogsTheExpectedInformation_scenario2()
        {
            // Scenario:
            // Minimum info provided. Additionally, the parameters for the component contain a 'Scenario'
            // definition.

            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters[nameof(component.Scenario)] = "Any_Outcome";
            component.LogSuccessMetric();

            var loggedMetric = this.mockFixture.Logger.FirstOrDefault();

            Assert.IsNotNull(loggedMetric);
            Assert.IsInstanceOf<EventContext>(loggedMetric.Item3);
            EventContext context = loggedMetric.Item3 as EventContext;

            Assert.IsTrue(context.Properties.Count == 15
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
                    && context.Properties.ContainsKey("metricMetadata")
                    && context.Properties["scenarioName"].ToString() == "Any_Outcome"
                    && context.Properties["scenarioArguments"].ToString() == string.Empty
                    && context.Properties["metricName"].ToString() == "Succeeded"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == string.Empty
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution succeeded for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.HigherIsBetter.ToString()
                    && context.Properties["toolName"].ToString() == component.TypeName
                    && context.Properties["toolVersion"].ToString() == string.Empty
                    && context.Properties["tags"].ToString() == string.Empty
                    && context.Properties["metricMetadata"].ToString() == string.Empty);
        }

        [Test]
        public void LogSuccessMetricExtensionOnComponentLogsTheExpectedInformation_scenario3()
        {
            // Scenario:
            // Complete information provided including scenario name, tool name, and timestamps.

            string expectedScenarioName = "AnyTestName";
            string expectedToolName = "ToolA";
            string expectedToolVersion = "1.2.3";
            string expectedMetricCategorization = "instanceA";
            string expectedScenarioArguments = "--name=AnyTestName --runtime=100";
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-100);
            DateTime expectedEndTime = DateTime.UtcNow;

            TestExecutor component = new TestExecutor(this.mockFixture);
            component.LogSuccessMetric(expectedToolName, expectedToolVersion, expectedScenarioName, expectedScenarioArguments, expectedMetricCategorization, expectedStartTime, expectedEndTime);

            var loggedMetric = this.mockFixture.Logger.FirstOrDefault();

            Assert.IsNotNull(loggedMetric);
            Assert.IsInstanceOf<EventContext>(loggedMetric.Item3);
            EventContext context = loggedMetric.Item3 as EventContext;

            Assert.IsTrue(context.Properties.Count == 15
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
                    && context.Properties.ContainsKey("metricMetadata")
                    && context.Properties["scenarioName"].ToString() == expectedScenarioName
                    && context.Properties["scenarioArguments"].ToString() == expectedScenarioArguments
                    && context.Properties["metricName"].ToString() == "Succeeded"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == expectedMetricCategorization
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution succeeded for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.HigherIsBetter.ToString()
                    && context.Properties["toolName"].ToString() == expectedToolName
                    && context.Properties["toolVersion"].ToString() == expectedToolVersion
                    && context.Properties["tags"].ToString() == string.Empty
                    && context.Properties["metricMetadata"].ToString() == string.Empty);
        }

        [Test]
        public void LogSystemEventsExtensionLogsTheExpectedEvents()
        {
            string expectedMessage = "RealtimeDataMonitorCounters";
            IDictionary<string, object> expectedSystemEvents = new Dictionary<string, object>
            {
                ["SystemEventLog"] = "Process shutdown unexpectedly.",
                ["SystemFileChange"] = "ntdll.dll version 1.2.3 replaced."
            };

            this.mockLogger.Object.LogSystemEvents(expectedMessage, expectedSystemEvents, this.mockEventContext);

            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.SystemEvent && eventId.Name == expectedMessage),
                It.Is<EventContext>(context => context.ActivityId == this.mockEventContext.ActivityId
                    && context.ParentActivityId == this.mockEventContext.ParentActivityId
                    && context.Properties.ContainsKey("eventType")
                    && context.Properties.ContainsKey("eventInfo")),
                null,
                null),
                Times.Exactly(2)); // Each performance counter is logged as an individual message
        }

        [Test]
        public void LogSystemEventsExtensionIncludesTheSystemEventInformationInTheEventContext()
        {
            string expectedMessage = "RealtimeDataMonitorCounters";
            IDictionary<string, object> expectedSystemEvents = new Dictionary<string, object>
            {
                ["SystemEventLog"] = "Process shutdown unexpectedly.",
                ["SystemFileChange"] = "ntdll.dll version 1.2.3 replaced.",
                ["MicrocodeVersion"] = 1234
            };

            int counterIndex = 0;

            this.mockLogger
                .Setup(logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<EventContext>(), null, null))
                .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, state, exc, formatter) =>
                {
                    Assert.IsNotNull(state);
                    Assert.IsTrue(state.Properties.ContainsKey("eventType"));
                    Assert.IsTrue(state.Properties.ContainsKey("eventInfo"));
                    Assert.AreEqual(state.Properties["eventType"].ToString(), expectedSystemEvents.ElementAt(counterIndex).Key);
                    Assert.AreEqual(state.Properties["eventInfo"], expectedSystemEvents.ElementAt(counterIndex).Value);
                    counterIndex++;
                });

            this.mockLogger.Object.LogSystemEvents(expectedMessage, expectedSystemEvents, this.mockEventContext);
        }


        [Test]
        public void LogSystemEventsExtensionDoesNotSideEffectOrChangeAnEventContextProvided()
        {
            EventContext originalContext = new EventContext(Guid.NewGuid(), new Dictionary<string, object>
            {
                ["property1"] = "Any Value",
                ["property2"] = 1234
            });

            EventContext cloneOfOriginal = originalContext.Clone();

            IDictionary<string, object> systemEvents = new Dictionary<string, object>
            {
                ["SystemEventLog"] = "Process shutdown unexpectedly."
            };

            this.mockLogger
                .Setup(logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<EventContext>(), null, null))
                .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, state, exc, formatter) =>
                {
                    // A clone is used to avoid side effecting the original
                    EventContext actualContext = state;
                    Assert.IsFalse(object.ReferenceEquals(originalContext, actualContext));
                });

            this.mockLogger.Object.LogSystemEvents("AnyMessage", systemEvents, this.mockEventContext);

            // The original should not have been changed.
            Assert.AreEqual(originalContext.ActivityId, cloneOfOriginal.ActivityId);
            Assert.AreEqual(originalContext.ParentActivityId, cloneOfOriginal.ParentActivityId);
            Assert.AreEqual(originalContext.TransactionId, cloneOfOriginal.TransactionId);
            Assert.AreEqual(originalContext.Properties.Count, cloneOfOriginal.Properties.Count);
            CollectionAssert.AreEquivalent(originalContext.Properties.Keys, cloneOfOriginal.Properties.Keys);
            CollectionAssert.AreEquivalent(originalContext.Properties.Values, cloneOfOriginal.Properties.Values);
        }

        [Test]
        public void LogTestMetricsExtensionLogsTheExpectedEvents_Scenario1()
        {
            string expectedScenarioName = "AnyTestName";
            string expectedMetricName = "AnyMetric";
            string expectedToolName = "ToolA";
            double expectedMetricValue = 123.456;
            string expectedUnits = "seconds";
            string expectedMetricCategorization = "instanceA";
            string expectedTestArguments = "--name=AnyTestName --runtime=100";
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-100);
            DateTime expectedEndTime = DateTime.UtcNow;
            List<string> expectedTags = new List<string> { "Tag1", "Tag2" };

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
                this.mockEventContext);

            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metrics && eventId.Name.EndsWith("ScenarioResult")),
                It.Is<EventContext>(context => context.Properties.Count == 15
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
                    && context.Properties.ContainsKey("metricMetadata")
                    && context.Properties["scenarioName"].ToString() == expectedScenarioName
                    && context.Properties["scenarioStartTime"].ToString() == expectedStartTime.ToString()
                    && context.Properties["scenarioEndTime"].ToString() == expectedEndTime.ToString()
                    && context.Properties["scenarioArguments"].ToString() == expectedTestArguments
                    && context.Properties["metricName"].ToString() == expectedMetricName
                    && context.Properties["metricValue"].ToString() == expectedMetricValue.ToString()
                    && context.Properties["metricCategorization"].ToString() == expectedMetricCategorization
                    && context.Properties["metricDescription"].ToString() == string.Empty
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.Undefined.ToString()
                    && context.Properties["toolName"].ToString() == expectedToolName
                    && context.Properties["tags"].ToString() == string.Join(",", expectedTags)
                    && context.Properties["metricMetadata"].ToString() == string.Empty),
                null,
                null));
        }

        [Test]
        public void LogTestMetricsExtensionLogsTheExpectedEvents_Scenario2()
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
                expectedMetadata);

            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metrics && eventId.Name.EndsWith("ScenarioResult")),
                It.Is<EventContext>(context => context.Properties.Count == 15
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
                    && context.Properties.ContainsKey("metricMetadata")
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
                    && context.Properties["metricMetadata"] == expectedMetadata as Object
                    && context.Properties["toolVersion"].ToString() == expectedToolVersion
                    && context.Properties["toolResults"].ToString() == expectedToolResults
                    && context.Properties["tags"].ToString() == string.Join(",", expectedTags)),
                null,
                null));
        }

        [Test]
        public void LogTestMetricsExtensionHandlesOptionalArgumentsNotProvided()
        {
            string expectedScenarioName = "AnyTestName";
            string expectedMetricName = "AnyMetric";
            string expectedToolName = "ToolA";
            double expectedMetricValue = 123.456;
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-100);
            DateTime expectedEndTime = DateTime.UtcNow;

            this.mockLogger.Object.LogMetrics(
                expectedToolName,
                expectedScenarioName,
                expectedStartTime,
                expectedEndTime,
                expectedMetricName,
                expectedMetricValue,
                null, // metric units are optional
                null, // metric categorization is optional
                null, // command line arguments are optional
                null, // tags are optional
                this.mockEventContext);

            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metrics && eventId.Name.EndsWith("ScenarioResult")),
                It.Is<EventContext>(context => context.ActivityId == this.mockEventContext.ActivityId
                    && context.ParentActivityId == this.mockEventContext.ParentActivityId
                    && context.Properties.ContainsKey("scenarioName")
                    && context.Properties.ContainsKey("scenarioArguments")
                    && context.Properties.ContainsKey("scenarioStartTime")
                    && context.Properties.ContainsKey("scenarioEndTime")
                    && context.Properties.ContainsKey("metricName")
                    && context.Properties.ContainsKey("metricUnit")
                    && context.Properties.ContainsKey("metricCategorization")
                    && context.Properties.ContainsKey("metricRelativity")
                    && context.Properties.ContainsKey("toolName")
                    && context.Properties.ContainsKey("toolVersion")
                    && context.Properties.ContainsKey("toolResults")
                    && context.Properties.ContainsKey("tags")),
                null,
                null));
        }

        [Test]
        public void LogTestMetricsExtensionHandlesOptionalArgumentsNotProvided_2()
        {
            string expectedScenarioName = "AnyTestName";
            string expectedMetricName = "AnyMetric";
            string expectedToolName = "ToolA";
            double expectedMetricValue = 123.456;
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-100);
            DateTime expectedEndTime = DateTime.UtcNow;

            this.mockLogger.Object.LogMetrics(
                expectedToolName,
                expectedScenarioName,
                expectedStartTime,
                expectedEndTime,
                expectedMetricName,
                expectedMetricValue,
                null, // units are optional
                null, // tested instance is optional
                null, // command line arguments are optional
                new List<string>(), // empty tags
                this.mockEventContext);

            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metrics && eventId.Name.EndsWith("ScenarioResult")),
                It.Is<EventContext>(context => context.ActivityId == this.mockEventContext.ActivityId
                    && context.ParentActivityId == this.mockEventContext.ParentActivityId
                    && context.Properties.ContainsKey("scenarioName")
                    && context.Properties.ContainsKey("scenarioArguments")
                    && context.Properties.ContainsKey("scenarioStartTime")
                    && context.Properties.ContainsKey("scenarioEndTime")
                    && context.Properties.ContainsKey("metricName")
                    && context.Properties.ContainsKey("metricUnit")
                    && context.Properties.ContainsKey("metricCategorization")
                    && context.Properties.ContainsKey("metricRelativity")
                    && context.Properties.ContainsKey("toolName")
                    && context.Properties.ContainsKey("toolVersion")
                    && context.Properties.ContainsKey("toolResults")
                    && context.Properties.ContainsKey("tags")),
                null,
                null));
        }

        [Test]
        public void LogTestMetricsExtensionDoesNotSideEffectOrChangeAnEventContextProvided()
        {
            EventContext originalContext = new EventContext(Guid.NewGuid(), new Dictionary<string, object>
            {
                ["property1"] = "Any Value",
                ["property2"] = 1234
            });

            EventContext cloneOfOriginal = originalContext.Clone();

            this.mockLogger
                .Setup(logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<EventContext>(), null, null))
                .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, state, exc, formatter) =>
                {
                    // A clone is used to avoid side effecting the original
                    EventContext actualContext = state;
                    Assert.IsFalse(object.ReferenceEquals(originalContext, actualContext));
                });

            this.mockLogger.Object.LogMetrics(
                "AnyTool",
                "AnyScenario",
                DateTime.UtcNow,
                DateTime.UtcNow,
                "AnyMetric",
                1234,
                "AnyUnits",
                "AnyMetricCategorization",
                "AnyCommandLine",
                new List<string>(),
                this.mockEventContext);

            // The original should not have been changed.
            Assert.AreEqual(originalContext.ActivityId, cloneOfOriginal.ActivityId);
            Assert.AreEqual(originalContext.ParentActivityId, cloneOfOriginal.ParentActivityId);
            Assert.AreEqual(originalContext.TransactionId, cloneOfOriginal.TransactionId);
            Assert.AreEqual(originalContext.Properties.Count, cloneOfOriginal.Properties.Count);
            CollectionAssert.AreEquivalent(originalContext.Properties.Keys, cloneOfOriginal.Properties.Keys);
            CollectionAssert.AreEquivalent(originalContext.Properties.Values, cloneOfOriginal.Properties.Values);
        }

        [Test]
        public void ObscureSecretsExtensionChangesSecretsToExpectedValues_Scenario1()
        {
            Tuple<string, string> sensitiveData = VirtualClientLoggingExtensionsTests.GetAccessTokenPair();
            IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
            {
                ["AccessToken"] = sensitiveData.Item1,
                ["PersonalAccessToken"] = sensitiveData.Item1,
                ["CommandLine"] = $"--profile=ANY-PROFILE.json --platform=Any --timeout=1440 --parameters=PersonalAccessToken={sensitiveData.Item1}"
            };

            IDictionary<string, IConvertible> obscuredParameters = parameters.ObscureSecrets();

            foreach (var entry in obscuredParameters)
            {
                Assert.IsFalse(entry.Value.ToString().Contains(sensitiveData.Item1));
                Assert.IsTrue(entry.Value.ToString().Contains(sensitiveData.Item2));
            }
        }

        [Test]
        public void ObscureSecretsExtensionChangesSecretsToExpectedValues_Scenario2()
        {
            Tuple<string, string> sensitiveData = VirtualClientLoggingExtensionsTests.GetAccessTokenPair();
            IDictionary<string, object> parameters = new Dictionary<string, object>
            {
                ["AccessToken"] = sensitiveData.Item1,
                ["PersonalAccessToken"] = sensitiveData.Item1,
                ["CommandLine"] = $"--profile=ANY-PROFILE.json --platform=Any --timeout=1440 --parameters=PersonalAccessToken={sensitiveData.Item1} --metadata=key1=value1,,,key2=value2"
            };

            IDictionary<string, object> obscuredParameters = parameters.ObscureSecrets();

            foreach (var entry in obscuredParameters)
            {
                Assert.IsFalse(entry.Value.ToString().Contains(sensitiveData.Item1));
                Assert.IsTrue(entry.Value.ToString().Contains(sensitiveData.Item2));
            }
        }

        [Test]
        public void ObscureSecretsExtensionDoesNotModifyDataInTheOriginalParameters()
        {
            Tuple<string, string> sensitiveData = VirtualClientLoggingExtensionsTests.GetAccessTokenPair();
            IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
            {
                ["AccessToken"] = sensitiveData.Item1,
                ["PersonalAccessToken"] = sensitiveData.Item1,
                ["CommandLine"] = $"--profile=ANY-PROFILE.json --platform=Any --timeout=1440 --parameters=PersonalAccessToken={sensitiveData.Item1}"
            };

            // Preserve the original values
            IDictionary<string, string> originalValues = new Dictionary<string, string>();
            parameters.ToList().ForEach(p => originalValues.Add(p.Key, p.Value.ToString()));

            parameters.ObscureSecrets();

            foreach (var entry in parameters)
            {
                Assert.AreEqual(originalValues[entry.Key], entry.Value.ToString());
            }
        }

        private static Tuple<string, string> GetAccessTokenPair()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            var originalBytes = new List<byte>
            {
                115, 114, 113, 102, 119, 114, 101, 52, 53, 102, 49, 101, 106, 112, 107, 109, 51, 100, 103, 113, 114, 56,
                121, 53, 100, 119, 99, 113, 110, 113, 106, 114, 108, 120, 109, 100, 53, 120, 101, 104, 97, 112, 100, 107,
                110, 113, 111, 109, 116, 113, 116, 97
            };

            // 80, 101, 114, 115, 111, 110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 61,

            var obscuredBytes = new List<byte>
            {
                46, 46, 46
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }
    }
}