// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.TestExtensions;

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

            // When there is a content manager, the application will also write a file
            // upload notification file (e.g. upload.json). We validate this separately.
            this.mockFixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();
        }

        [Test]
        [TestCase(0, null, null, null, null, null)]
        [TestCase(0, "", "", "", "", "")]
        [TestCase(0, "C:\\any\\workload.exe", null, null, null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", null, null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", "output from the command", null)]
        [TestCase(123, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", "output from the command", "errors in output")]
        public void AddProcessContextExtensionAddsTheExpectedProcessInformationToTheEventContext(
           int expectedExitCode, string expectedCommand, string expectedArguments, string expectedWorkingDir, string expectedStandardOutput, string expectedStandardError)
        {
            InMemoryProcess process = new InMemoryProcess
            {
                ExitCode = expectedExitCode,
                StartInfo = new ProcessStartInfo
                {
                    FileName = expectedCommand,
                    Arguments = expectedArguments,
                    WorkingDirectory = expectedWorkingDir
                },
                StandardOutput = expectedStandardOutput != null ? new Common.ConcurrentBuffer(new StringBuilder(expectedStandardOutput)) : null,
                StandardError = expectedStandardError != null ? new Common.ConcurrentBuffer(new StringBuilder(expectedStandardError)) : null
            };

            EventContext telemetryContext = new EventContext(Guid.NewGuid()).AddProcessContext(process);

            Assert.IsTrue(telemetryContext.Properties.TryGetValue("process", out object processContext));

            string expectedProcessInfo = new
            {
                id = process.Id,
                command = $"{expectedCommand} {expectedArguments}".Trim(),
                workingDir = expectedWorkingDir ?? string.Empty,
                exitCode = expectedExitCode,
                standardOutput = expectedStandardOutput ?? string.Empty,
                standardError = expectedStandardError ?? string.Empty
            }.ToJson();

            string actualProcessInfo = processContext.ToJson();
            SerializationAssert.JsonEquals(expectedProcessInfo, actualProcessInfo);
        }

        [Test]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", "Any results 1")]
        [TestCase(123, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", "Any results 2")]
        public void AddProcessContextExtensionAddsTheExpectedProcessInformationToTheEventContextWhenResultsAreProvided(
           int expectedExitCode, string expectedCommand, string expectedArguments, string expectedWorkingDir, string expectedResults)
        {
            InMemoryProcess process = new InMemoryProcess
            {
                ExitCode = expectedExitCode,
                StartInfo = new ProcessStartInfo
                {
                    FileName = expectedCommand,
                    Arguments = expectedArguments,
                    WorkingDirectory = expectedWorkingDir
                }
            };

            EventContext telemetryContext = new EventContext(Guid.NewGuid()).AddProcessContext(process, results: expectedResults);

            Assert.IsTrue(telemetryContext.Properties.TryGetValue("processResults", out object processContext));

            string expectedProcessInfo = new
            {
                id = process.Id,
                command = $"{expectedCommand} {expectedArguments}".Trim(),
                workingDir = expectedWorkingDir ?? string.Empty,
                exitCode = expectedExitCode,
                results = expectedResults,
            }.ToJson();
            
            string actualProcessInfo = processContext.ToJson();
            SerializationAssert.JsonEquals(expectedProcessInfo, actualProcessInfo);
        }

        [Test]
        public void AddProcessContextExtensionHandlesOutputThatExceedsTheMaximumCharacterThreshold_StandardOutputOnlyScenario()
        {
            InMemoryProcess process = new InMemoryProcess
            {
                ExitCode = 0,
                StartInfo = new ProcessStartInfo
                {
                    FileName = "AnyCommand.exe",
                    Arguments = "--any=arguments",
                    WorkingDirectory = "C:\\Users\\Any"
                },
                StandardOutput = new Common.ConcurrentBuffer(new StringBuilder("Standard output from the process."))
            };

            EventContext telemetryContext = new EventContext(Guid.NewGuid()).AddProcessContext(process, maxChars:15);
            Assert.IsTrue(telemetryContext.Properties.TryGetValue("process", out object processContext));

            JObject processDetails = JObject.FromObject(processContext);
            string standardOutput = processDetails.SelectToken("standardOutput").ToString();
            string standardError = processDetails.SelectToken("standardError").ToString();

            Assert.IsTrue(standardOutput.Length == 15);
            Assert.IsTrue(standardOutput == "Standard output");
            Assert.IsTrue(standardOutput.Length + standardError.Length == 15);
        }

        [Test]
        public void AddProcessContextExtensionHandlesOutputThatExceedsTheMaximumCharacterThreshold_StandardErrorOnlyScenario()
        {
            InMemoryProcess process = new InMemoryProcess
            {
                ExitCode = 0,
                StartInfo = new ProcessStartInfo
                {
                    FileName = "AnyCommand.exe",
                    Arguments = "--any=arguments",
                    WorkingDirectory = "C:\\Users\\Any"
                },
                StandardError = new Common.ConcurrentBuffer(new StringBuilder("Standard error from the process."))
            };

            EventContext telemetryContext = new EventContext(Guid.NewGuid()).AddProcessContext(process, maxChars: 14);
            Assert.IsTrue(telemetryContext.Properties.TryGetValue("process", out object processContext));

            JObject processDetails = JObject.FromObject(processContext);
            string standardOutput = processDetails.SelectToken("standardOutput").ToString();
            string standardError = processDetails.SelectToken("standardError").ToString();

            Assert.IsTrue(standardError.Length == 14);
            Assert.IsTrue(standardError == "Standard error");
            Assert.IsTrue(standardOutput.Length + standardError.Length == 14);
        }

        [Test]
        public void AddProcessContextExtensionHandlesOutputThatExceedsTheMaximumCharacterThreshold_StandardOutputPlusErrorScenario()
        {
            InMemoryProcess process = new InMemoryProcess
            {
                ExitCode = 0,
                StartInfo = new ProcessStartInfo
                {
                    FileName = "AnyCommand.exe",
                    Arguments = "--any=arguments",
                    WorkingDirectory = "C:\\Users\\Any"
                },
                StandardOutput = new Common.ConcurrentBuffer(new StringBuilder("Standard output from the process.")),

                // 32 total chars (max 47 - 32 means that there can only be 15 in standard output)
                StandardError = new Common.ConcurrentBuffer(new StringBuilder("Standard error from the process."))
            };

            EventContext telemetryContext = new EventContext(Guid.NewGuid()).AddProcessContext(process, maxChars: 47);
            Assert.IsTrue(telemetryContext.Properties.TryGetValue("process", out object processContext));

            JObject processDetails = JObject.FromObject(processContext);
            string standardOutput = processDetails.SelectToken("standardOutput").ToString();
            string standardError = processDetails.SelectToken("standardError").ToString();

            // When the combination of the standard output and error exceed the limitation, the standard
            // output is trimmed down in attempts to preserve the standard error.
            Assert.IsTrue(standardOutput.Length == 15);
            Assert.IsTrue(standardOutput == "Standard output");
            Assert.IsTrue(standardError.Length == 32);
            Assert.IsTrue(standardError == "Standard error from the process.");
        }

        [Test]
        public void AddProcessContextExtensionHandlesOutputThatExceedsTheMaximumCharacterThresholdWhichIsLargerThanTheEntiretyOfStandardOutput()
        {
            InMemoryProcess process = new InMemoryProcess
            {
                ExitCode = 0,
                StartInfo = new ProcessStartInfo
                {
                    FileName = "AnyCommand.exe",
                    Arguments = "--any=arguments",
                    WorkingDirectory = "C:\\Users\\Any"
                },

                // 32 total chars (max 14 - 32 means that there can only be none left in standard output)
                StandardOutput = new Common.ConcurrentBuffer(new StringBuilder("Standard output from the process.")),
                StandardError = new Common.ConcurrentBuffer(new StringBuilder("Standard error from the process."))
            };

            EventContext telemetryContext = new EventContext(Guid.NewGuid()).AddProcessContext(process, maxChars: 14);
            Assert.IsTrue(telemetryContext.Properties.TryGetValue("process", out object processContext));

            JObject processDetails = JObject.FromObject(processContext);
            string standardOutput = processDetails.SelectToken("standardOutput").ToString();
            string standardError = processDetails.SelectToken("standardError").ToString();

            // When the combination of the standard output and error exceed the limitation, the standard
            // output is trimmed down in attempts to preserve the standard error.
            Assert.IsTrue(standardOutput.Length == 0);
            Assert.IsTrue(standardError.Length == 14);
            Assert.IsTrue(standardError == "Standard error");
        }

        [Test]
        public void AddProcessContextExtensionHandlesOutputThatExceedsTheMaximumCharacterThreshold_WorseCaseScenario()
        {
            InMemoryProcess process = new InMemoryProcess
            {
                ExitCode = 0,
                StartInfo = new ProcessStartInfo
                {
                    FileName = "AnyCommand.exe",
                    Arguments = "--any=arguments",
                    WorkingDirectory = "C:\\Users\\Any"
                },

                // 32 total chars (max 14 - 32 means that there can only be none left in standard output)
                StandardOutput = new Common.ConcurrentBuffer(new StringBuilder("Standard output from the process.")),
                StandardError = new Common.ConcurrentBuffer(new StringBuilder("Standard error from the process."))
            };

            EventContext telemetryContext = new EventContext(Guid.NewGuid()).AddProcessContext(process, maxChars: 0);
            Assert.IsTrue(telemetryContext.Properties.TryGetValue("process", out object processContext));

            JObject processDetails = JObject.FromObject(processContext);
            string standardOutput = processDetails.SelectToken("standardOutput").ToString();
            string standardError = processDetails.SelectToken("standardError").ToString();

            // When the combination of the standard output and error exceed the limitation, the standard
            // output is trimmed down in attempts to preserve the standard error.
            Assert.IsTrue(standardOutput.Length == 0);
            Assert.IsTrue(standardError.Length == 0);
        }

        [Test]
        public void AddProcessContextExtensionValidatesTheMaxCharacterCount()
        {
            InMemoryProcess process = new InMemoryProcess
            {
                ExitCode = 0,
                StartInfo = new ProcessStartInfo
                {
                    FileName = "AnyCommand.exe",
                    Arguments = "--any=arguments",
                    WorkingDirectory = "C:\\Users\\Any"
                }
            };

            Assert.Throws<ArgumentException>(() => new EventContext(Guid.NewGuid()).AddProcessContext(process, maxChars: -1));
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
        [TestCase(LogLevel.Trace)]
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
        [TestCase(LogType.Metric)]
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
        [TestCase(LogLevel.Trace)]
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
        public void LogMetricsExtensionLogsTheExpectedEvents_Scenario1()
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

            this.mockLogger.Object.LogMetric(
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
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metric && eventId.Name.EndsWith("ScenarioResult")),
                It.Is<EventContext>(context => context.Properties.Count == 16
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
                    && context.Properties["metricDescription"].ToString() == string.Empty
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.Undefined.ToString()
                    && context.Properties["metricVerbosity"].ToString() == "1"
                    && context.Properties["toolName"].ToString() == expectedToolName
                    && context.Properties["toolVersion"].ToString() == string.Empty
                    && context.Properties["toolResults"].ToString() == string.Empty
                    && context.Properties["tags"].ToString() == string.Join(",", expectedTags)
                    && context.Properties["metadata_metrics"].ToString() == string.Empty),
                null,
                null));
        }

        [Test]
        public void LogMetricsExtensionLogsTheExpectedEvents_Scenario2()
        {
            string expectedScenarioName = "AnyTestName";
            string expectedMetricName = "AnyMetric";
            string expectedToolName = "ToolA";
            string expectedToolVersion = "1.2.3.4";
            string expectedToolResults = "Tool A: metric1=value 1 | metric 2=value 2";
            double expectedMetricValue = 123.456;
            string expectedUnits = "seconds";
            int expectedVerbosity = 1;
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

            this.mockLogger.Object.LogMetric(
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
                expectedVerbosity,
                expectedDescription,
                expectedToolResults,
                expectedToolVersion,
                expectedMetadata);

            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metric && eventId.Name.EndsWith("ScenarioResult")),
                It.Is<EventContext>(context => context.Properties.Count == 16
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
                    && context.Properties["metricVerbosity"].ToString() == "1"
                    && context.Properties["toolName"].ToString() == expectedToolName
                    && context.Properties["tags"].ToString() == string.Join(",", expectedTags)
                    && context.Properties["metadata_metrics"] == expectedMetadata as Object
                    && context.Properties["toolVersion"].ToString() == expectedToolVersion
                    && context.Properties["toolResults"].ToString() == expectedToolResults
                    && context.Properties["tags"].ToString() == string.Join(",", expectedTags)),
                null,
                null));
        }

        [Test]
        public void LogMetricsExtensionHandlesOptionalArgumentsNotProvided()
        {
            string expectedScenarioName = "AnyTestName";
            string expectedMetricName = "AnyMetric";
            string expectedToolName = "ToolA";
            double expectedMetricValue = 123.456;
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-100);
            DateTime expectedEndTime = DateTime.UtcNow;

            this.mockLogger.Object.LogMetric(
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
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metric && eventId.Name.EndsWith("ScenarioResult")),
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
        public void LogMetricsExtensionHandlesOptionalArgumentsNotProvided_2()
        {
            string expectedScenarioName = "AnyTestName";
            string expectedMetricName = "AnyMetric";
            string expectedToolName = "ToolA";
            double expectedMetricValue = 123.456;
            DateTime expectedStartTime = DateTime.UtcNow.AddSeconds(-100);
            DateTime expectedEndTime = DateTime.UtcNow;

            this.mockLogger.Object.LogMetric(
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
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metric && eventId.Name.EndsWith("ScenarioResult")),
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
        public void LogMetricsExtensionDoesNotSideEffectOrChangeAnEventContextProvided()
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

            this.mockLogger.Object.LogMetric(
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
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metric && eventId.Name == ("PerformanceCounter")),
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
                    && context.Properties.ContainsKey("metadata_metrics")
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
                   && context.Properties["metadata_metrics"] == expectedCounters[0].Metadata as object),
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
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.Metric && eventId.Name == ("PerformanceCounter")),
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
        [TestCase(0, null, null, null, null, null)]
        [TestCase(0, "", "", "", "", "")]
        [TestCase(0, "C:\\any\\workload.exe", null, null, null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", null, null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", "output from the command", null)]
        [TestCase(123, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", "output from the command", "errors in output")]
        public async Task LogProcessDetailsAsyncExtensionEmitsTheExpectedProcessInformationAsTelemetry(
            int expectedExitCode, string expectedCommand, string expectedArguments, string expectedWorkingDir, string expectedStandardOutput, string expectedStandardError)
        {
            InMemoryProcess process = new InMemoryProcess
            {
                ExitCode = expectedExitCode,
                StartInfo = new ProcessStartInfo
                {
                    FileName = expectedCommand,
                    Arguments = expectedArguments,
                    WorkingDirectory = expectedWorkingDir
                },
                StandardOutput = expectedStandardOutput != null ? new Common.ConcurrentBuffer(new StringBuilder(expectedStandardOutput)) : null,
                StandardError = expectedStandardError != null ? new Common.ConcurrentBuffer(new StringBuilder(expectedStandardError)) : null
            };
            
            string expectedResults = "Any results output by the process.";
            bool expectedProcessDetailsCaptured = false;
            bool expectedProcessResultsCaptured = false;

            this.mockFixture.Logger.OnLog = (level, eventInfo, state, error) =>
            {
                Assert.AreEqual(LogLevel.Information, level, $"Log level not matched");
                Assert.IsInstanceOf<EventContext>(state);

                if (eventInfo.Name == $"{nameof(TestExecutor)}.ProcessDetails")
                {
                    Assert.IsTrue((state as EventContext).Properties.TryGetValue("process", out object processContext));
                    string expectedProcessInfo = new
                    {
                        id = process.Id,
                        command = $"{expectedCommand} {expectedArguments}".Trim(),
                        workingDir = expectedWorkingDir ?? string.Empty,
                        exitCode = expectedExitCode,
                        standardOutput = expectedStandardOutput ?? string.Empty,
                        standardError = expectedStandardError ?? string.Empty
                    }.ToJson();

                    string actualProcessInfo = processContext.ToJson();

                    SerializationAssert.JsonEquals(expectedProcessInfo, actualProcessInfo);
                    expectedProcessDetailsCaptured = true;
                }
                else if (eventInfo.Name == $"{nameof(TestExecutor)}.ProcessResults")
                {
                    Assert.IsTrue((state as EventContext).Properties.TryGetValue("processResults", out object processResultsContext));
                    string expectedProcessInfo = new
                    {
                        id = process.Id,
                        command = $"{expectedCommand} {expectedArguments}".Trim(),
                        workingDir = expectedWorkingDir ?? string.Empty,
                        exitCode = expectedExitCode,
                        results = expectedResults
                    }.ToJson();

                    string actualProcessInfo = processResultsContext.ToJson();

                    SerializationAssert.JsonEquals(expectedProcessInfo, actualProcessInfo);
                    expectedProcessResultsCaptured = true;
                }
            };

            TestExecutor component = new TestExecutor(this.mockFixture);
            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), results: new List<string> { expectedResults }, logToTelemetry: true)
                .ConfigureAwait(false);

            Assert.IsTrue(expectedProcessDetailsCaptured);
            Assert.IsTrue(expectedProcessResultsCaptured);
        }

        [Test]
        [TestCase(0, "run password=secret123", null)]
        [TestCase(1, "run password=secret123", "run password=secret123")]
        public async Task LogProcessDetailsAsyncObscuresSecrets(int exitCode, string standardOutput, string standardError)
        {
            InMemoryProcess process = new InMemoryProcess
            {
                ExitCode = exitCode,
                StartInfo = new ProcessStartInfo
                {
                    FileName = "run",
                    Arguments = "password=secret123"
                },
                StandardOutput = new ConcurrentBuffer(new StringBuilder(standardOutput)),
                StandardError = new ConcurrentBuffer(new StringBuilder(standardError))
            };

            this.mockFixture.Logger.OnLog = (level, eventInfo, state, error) =>
            {
                if (eventInfo.Name == $"{nameof(TestExecutor)}.ProcessDetails")
                {
                    (state as EventContext).Properties.TryGetValue("process", out object processContext);
                    string actualProcessInfo = processContext.ToJson();

                    Assert.IsFalse(actualProcessInfo.ToString().Contains("secret123"));
                }
            };

            TestExecutor component = new TestExecutor(this.mockFixture);
            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), results: new List<string> { }, logToTelemetry: true)
                .ConfigureAwait(false);
        }

        [Test]
        public void LogErrorMessageObscuresSecrets()
        {
            Exception expectedError = null;
            try
            {
                // To ensure a call stack is included.
                throw new Exception("An error occurred, password=secret123");
            }
            catch (Exception exc)
            {
                expectedError = exc;
            }

            this.mockLogger.Object.LogErrorMessage(expectedError, this.mockEventContext);

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

                    Assert.IsFalse(JsonConvert.SerializeObject(errorEntries.First()).Contains("secret123"));
                });

            this.mockLogger.Object.LogErrorMessage(expectedError, this.mockEventContext);
        }

        [Test]
        [TestCase(0, null, null, null, null, null)]
        [TestCase(0, "", "", "", "", "")]
        [TestCase(0, "C:\\any\\workload.exe", null, null, null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", null, null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", "output from the command", null)]
        [TestCase(123, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", "output from the command", "errors in output")]
        public async Task LogProcessDetailsExtensionEmitsTheExpectedProcessInformationAsTelemetryWhenTheToolsetNameIsProvided(
           int expectedExitCode, string expectedCommand, string expectedArguments, string expectedWorkingDir, string expectedStandardOutput, string expectedStandardError)
        {
            InMemoryProcess process = new InMemoryProcess
            {
                ExitCode = expectedExitCode,
                StartInfo = new ProcessStartInfo
                {
                    FileName = expectedCommand,
                    Arguments = expectedArguments,
                    WorkingDirectory = expectedWorkingDir
                },
                StandardOutput = expectedStandardOutput != null ? new Common.ConcurrentBuffer(new StringBuilder(expectedStandardOutput)) : null,
                StandardError = expectedStandardError != null ? new Common.ConcurrentBuffer(new StringBuilder(expectedStandardError)) : null
            };

            string expectedToolset = "AnyWorkloadToolset";
            string expectedResults = "Any results output by the process.";
            bool expectedProcessDetailsCaptured = false;
            bool expectedProcessResultsCaptured = false;

            this.mockFixture.Logger.OnLog = (level, eventInfo, state, error) =>
            {
                Assert.AreEqual(LogLevel.Information, level, $"Log level not matched");
                Assert.IsInstanceOf<EventContext>(state);
                

                if (eventInfo.Name == $"{nameof(TestExecutor)}.{expectedToolset}.ProcessDetails")
                {
                    Assert.IsTrue((state as EventContext).Properties.TryGetValue("process", out object processContext));
                    string expectedProcessInfo = new
                    {
                        id = process.Id,
                        command = $"{expectedCommand} {expectedArguments}".Trim(),
                        workingDir = expectedWorkingDir ?? string.Empty,
                        exitCode = expectedExitCode,
                        standardOutput = expectedStandardOutput ?? string.Empty,
                        standardError = expectedStandardError ?? string.Empty
                    }.ToJson();

                    string actualProcessInfo = processContext.ToJson();

                    SerializationAssert.JsonEquals(expectedProcessInfo, actualProcessInfo);
                    expectedProcessDetailsCaptured = true;
                }
                else if (eventInfo.Name == $"{nameof(TestExecutor)}.{expectedToolset}.ProcessResults")
                {
                    Assert.IsTrue((state as EventContext).Properties.TryGetValue("processResults", out object processResultsContext));
                    string expectedProcessInfo = new
                    {
                        id = process.Id,
                        command = $"{expectedCommand} {expectedArguments}".Trim(),
                        workingDir = expectedWorkingDir ?? string.Empty,
                        exitCode = expectedExitCode,
                        results = expectedResults
                    }.ToJson();

                    string actualProcessInfo = processResultsContext.ToJson();

                    SerializationAssert.JsonEquals(expectedProcessInfo, actualProcessInfo);
                    expectedProcessResultsCaptured = true;
                }
            };

            TestExecutor component = new TestExecutor(this.mockFixture);
            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), toolName: expectedToolset, results: new List<string> { expectedResults }, logToTelemetry: true)
               .ConfigureAwait(false);

            Assert.IsTrue(expectedProcessDetailsCaptured);
            Assert.IsTrue(expectedProcessResultsCaptured);
        }

        [Test]
        [TestCase("<AnyToolset<")]
        [TestCase(">AnyToolset>")]
        [TestCase(":AnyToolset:")]
        [TestCase("\"AnyToolset\"")]
        [TestCase("/AnyToolset/")]
        [TestCase("\\AnyToolset\\")]
        [TestCase("|AnyToolset|")]
        [TestCase("?AnyToolset?")]
        [TestCase("*AnyToolset*")]
        public async Task LogProcessDetailsExtensionHandlesReservedCharactersInTheToolsetNamesWhenEmittingTelemetry(string toolsetName)
        {
            InMemoryProcess process = new InMemoryProcess();
            TestExecutor component = new TestExecutor(this.mockFixture);

            bool processDetailsHandled = false;
            bool processResultsHandled = false;

            this.mockFixture.Logger.OnLog = (level, eventInfo, state, error) =>
            {
                if (eventInfo.Name == $"{nameof(TestExecutor)}.AnyToolset.ProcessDetails")
                {
                    processDetailsHandled = true;
                }
                else if (eventInfo.Name == $"{nameof(TestExecutor)}.AnyToolset.ProcessResults")
                {
                    processResultsHandled = true;
                }
            };

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), toolName: toolsetName, results: new List<string> { "Any results" }, logToTelemetry: true)
               .ConfigureAwait(false);

            Assert.IsTrue(processDetailsHandled);
            Assert.IsTrue(processResultsHandled);
        }

        [Test]
        [TestCase("AccessKey", "AnyKey123")]
        [TestCase("AccessToken", "AnyToken123")]
        [TestCase("Password", "AnyPass123")]
        [TestCase("Pwd", "AnyPass123")]
        public async Task LogProcessDetailsExtensionRemovesSensitiveDataFromTheProcessCommandDetailsInTelemetry(string sensitiveDataReference, string sensitiveData)
        {
            InMemoryProcess process = new InMemoryProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "C:\\any\\anyworkload.exe",
                    Arguments = $"--sensitive-data=\"{sensitiveDataReference}={sensitiveData}\""
                }
            };

            TestExecutor component = new TestExecutor(this.mockFixture);

            bool confirmed = false;
            this.mockFixture.Logger.OnLog = (level, eventInfo, state, error) =>
            {
                if (eventInfo.Name == $"{nameof(TestExecutor)}.ProcessDetails")
                {
                    Assert.IsInstanceOf<EventContext>(state);
                    Assert.IsTrue((state as EventContext).Properties.TryGetValue("process", out object processContext));

                    string actualProcessInfo = processContext.ToJson();
                    Assert.IsFalse(actualProcessInfo.Contains(sensitiveData, StringComparison.OrdinalIgnoreCase));
                    confirmed = true;
                }
                else if (eventInfo.Name == $"{nameof(TestExecutor)}.ProcessResults")
                {
                    Assert.IsInstanceOf<EventContext>(state);
                    Assert.IsTrue((state as EventContext).Properties.TryGetValue("processResults", out object processContext));

                    string actualProcessInfo = processContext.ToJson();
                    Assert.IsFalse(actualProcessInfo.Contains(sensitiveData, StringComparison.OrdinalIgnoreCase));
                    confirmed = true;
                }
            };

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), results: new List<string> { "Any results" }, logToTelemetry: true)
               .ConfigureAwait(false);

            Assert.IsTrue(confirmed);
        }

        [Test]
        public async Task LogProcessDetailsExtensionWritesLogsToTheExpectedLogFiles_Default()
        {
            // The default scenario is where the component has no 'Scenario' defined and the tool name
            // is not provided either.
            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters.Clear();

            InMemoryProcess process = new InMemoryProcess();

            bool expectedLogFileWritten = false;
            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    string expectedLogDirectory = this.mockFixture.GetLogsPath(nameof(TestExecutor).ToLowerInvariant());

                    Assert.IsTrue(path.StartsWith(expectedLogDirectory), "Log directory not matched");
                    Assert.IsTrue(path.EndsWith($"{nameof(TestExecutor)}.log".ToLowerInvariant()), "Log file name not matched");

                    expectedLogFileWritten = true;
                });

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), logToTelemetry: false, logToFile: true)
               .ConfigureAwait(false);

            Assert.IsTrue(expectedLogFileWritten);
        }

        [Test]
        public async Task LogProcessDetailsExtensionWritesLogsToTheExpectedLogFiles_ToolNameSupplied()
        {
            // The default scenario is where the component has no 'Scenario' defined and the tool name
            // is not provided either.
            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters.Clear();

            InMemoryProcess process = new InMemoryProcess();

            bool expectedLogFileWritten = false;
            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    string expectedLogDirectory = this.mockFixture.GetLogsPath("AnyTool".ToLowerInvariant());

                    Assert.IsTrue(path.StartsWith(expectedLogDirectory), "Log directory not matched");
                    Assert.IsTrue(path.EndsWith("AnyTool.log".ToLowerInvariant()), "Log file name not matched");

                    expectedLogFileWritten = true;
                });

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), toolName: "AnyTool", logToTelemetry: false, logToFile: true)
               .ConfigureAwait(false);

            Assert.IsTrue(expectedLogFileWritten);
        }

        [Test]
        public async Task LogProcessDetailsExtensionWritesLogsToTheExpectedLogFiles_ScenarioDefined()
        {
            // The default scenario is where the component has no 'Scenario' defined and the tool name
            // is not provided either.
            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters["Scenario"] = "AnyScenario";

            InMemoryProcess process = new InMemoryProcess();

            bool expectedLogFileWritten = false;
            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    string expectedLogDirectory = this.mockFixture.GetLogsPath(nameof(TestExecutor).ToLowerInvariant());

                    Assert.IsTrue(path.StartsWith(expectedLogDirectory), "Log directory not matched");
                    Assert.IsTrue(path.EndsWith($"AnyScenario.log".ToLowerInvariant()), "Log file name not matched");

                    expectedLogFileWritten = true;
                });

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), logToTelemetry: false, logToFile: true)
               .ConfigureAwait(false);

            Assert.IsTrue(expectedLogFileWritten);
        }

        [Test]
        public async Task LogProcessDetailsExtensionWritesLogsToTheExpectedLogFiles_ToolNameSupplied_ScenarioDefined()
        {
            // The default scenario is where the component has no 'Scenario' defined and the tool name
            // is not provided either.
            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters["Scenario"] = "AnyScenario";

            InMemoryProcess process = new InMemoryProcess();

            bool expectedLogFileWritten = false;
            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    string expectedLogDirectory = this.mockFixture.GetLogsPath("AnyTool".ToLowerInvariant());

                    Assert.IsTrue(path.StartsWith(expectedLogDirectory), "Log directory not matched");
                    Assert.IsTrue(path.EndsWith($"AnyScenario.log".ToLowerInvariant()), "Log file name not matched");

                    expectedLogFileWritten = true;
                });

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), toolName: "AnyTool", logToTelemetry: false, logToFile: true)
               .ConfigureAwait(false);

            Assert.IsTrue(expectedLogFileWritten);
        }

        [Test]
        [TestCase(0, null, null, null, null, null)]
        [TestCase(0, "", "", "", "", "")]
        [TestCase(0, "C:\\any\\workload.exe", null, null, null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", null, null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", "output from the command", null)]
        [TestCase(123, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", "output from the command", "errors in output")]
        public async Task LogProcessDetailsExtensionWritesTheExpectedProcessInformationToLogFilesOnTheSystem(
            int expectedExitCode, string expectedCommand, string expectedArguments, string expectedWorkingDir, string expectedStandardOutput, string expectedStandardError)
        {
            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters.Clear();

            InMemoryProcess process = new InMemoryProcess
            {
                ExitCode = expectedExitCode,
                StartInfo = new ProcessStartInfo
                {
                    FileName = expectedCommand,
                    Arguments = expectedArguments,
                    WorkingDirectory = expectedWorkingDir
                },
                StandardOutput = expectedStandardOutput != null ? new Common.ConcurrentBuffer(new StringBuilder(expectedStandardOutput)) : null,
                StandardError = expectedStandardError != null ? new Common.ConcurrentBuffer(new StringBuilder(expectedStandardError)) : null
            };

            bool expectedLogFileWritten = false;
            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    string expectedLogDirectory = this.mockFixture.GetLogsPath(nameof(TestExecutor).ToLowerInvariant());

                    Assert.IsTrue(path.StartsWith(expectedLogDirectory), "Log directory not matched");
                    Assert.IsTrue(path.EndsWith($"{nameof(TestExecutor)}.log".ToLowerInvariant()), "Log file name not matched");
                    Assert.IsTrue(content.Contains($"Command           : {expectedCommand} {expectedArguments}".TrimEnd(), StringComparison.Ordinal), "Command missing");
                    Assert.IsTrue(content.Contains($"Working Directory : {expectedWorkingDir}", StringComparison.Ordinal), "Working directory missing");
                    Assert.IsTrue(content.Contains($"Exit Code         : {expectedExitCode}", StringComparison.Ordinal), "Exit code missing");
                    Assert.IsFalse(content.Contains($"##GeneratedResults##", StringComparison.Ordinal), "Results delimiter unexpected");

                    if (!string.IsNullOrWhiteSpace(expectedStandardOutput))
                    {
                        Assert.IsTrue(content.Contains($"##StandardOutput##", StringComparison.Ordinal), "Output delimiter missing");
                        Assert.IsTrue(content.Contains(expectedStandardOutput, StringComparison.Ordinal), "Standard output missing");
                    }

                    if (!string.IsNullOrWhiteSpace(expectedStandardError))
                    {
                        Assert.IsTrue(content.Contains(expectedStandardError, StringComparison.Ordinal), "Standard error missing");
                    }

                    expectedLogFileWritten = true;
                });

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), logToTelemetry: false, logToFile: true)
               .ConfigureAwait(false);

            Assert.IsTrue(expectedLogFileWritten);
        }

        [Test]
        [TestCase(0, null, null, null, null, null)]
        [TestCase(0, "", "", "", "", "")]
        [TestCase(0, "C:\\any\\workload.exe", null, null, null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", null, null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", "output from the command", null)]
        [TestCase(123, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", "output from the command", "errors in output")]
        public async Task LogProcessDetailsExtensionWritesTheExpectedProcessInformationToLogFilesOnTheSystemWhenTheToolsetNameIsProvided(
            int expectedExitCode, string expectedCommand, string expectedArguments, string expectedWorkingDir, string expectedStandardOutput, string expectedStandardError)
        {
            InMemoryProcess process = new InMemoryProcess
            {
                ExitCode = expectedExitCode,
                StartInfo = new ProcessStartInfo
                {
                    FileName = expectedCommand,
                    Arguments = expectedArguments,
                    WorkingDirectory = expectedWorkingDir
                },
                StandardOutput = expectedStandardOutput != null ? new Common.ConcurrentBuffer(new StringBuilder(expectedStandardOutput)) : null,
                StandardError = expectedStandardError != null ? new Common.ConcurrentBuffer(new StringBuilder(expectedStandardError)) : null
            };

            string expectedToolset = "AnyWorkloadToolset";
            bool expectedLogFileWritten = false;

            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    string expectedLogDirectory = this.mockFixture.GetLogsPath(expectedToolset.ToLowerInvariant());

                    Assert.IsTrue(path.StartsWith(expectedLogDirectory), "Log directory not matched");
                    Assert.IsTrue(path.EndsWith($"{expectedToolset}.log".ToLowerInvariant()), "Log file name not matched");
                    Assert.IsTrue(content.Contains($"Command           : {expectedCommand} {expectedArguments}".TrimEnd(), StringComparison.Ordinal), "Command missing");
                    Assert.IsTrue(content.Contains($"Working Directory : {expectedWorkingDir}", StringComparison.Ordinal), "Working directory missing");
                    Assert.IsTrue(content.Contains($"Exit Code         : {expectedExitCode}", StringComparison.Ordinal), "Exit code missing");
                    Assert.IsFalse(content.Contains($"##GeneratedResults##", StringComparison.Ordinal), "Results delimiter unexpected");

                    if (!string.IsNullOrWhiteSpace(expectedStandardOutput))
                    {
                        Assert.IsTrue(content.Contains($"##StandardOutput##", StringComparison.Ordinal), "Output delimiter missing");
                        Assert.IsTrue(content.Contains(expectedStandardOutput, StringComparison.Ordinal), "Standard output missing");
                    }

                    if (!string.IsNullOrWhiteSpace(expectedStandardError))
                    {
                        Assert.IsTrue(content.Contains(expectedStandardError, StringComparison.Ordinal), "Standard error missing");
                    }

                    expectedLogFileWritten = true;
                });

            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters.Clear();
            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), expectedToolset, logToTelemetry: false, logToFile: true)
               .ConfigureAwait(false);

            Assert.IsTrue(expectedLogFileWritten);
        }

        [Test]
        [TestCase(0, null, null, null, null, null)]
        [TestCase(0, "", "", "", "", "")]
        [TestCase(0, "C:\\any\\workload.exe", null, null, null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", null, null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", null, null)]
        [TestCase(0, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", "output from the command", null)]
        [TestCase(123, "C:\\any\\workload.exe", "--any-argument=value", "C:\\any", "output from the command", "errors in output")]
        public async Task LogProcessDetailsToFileSystemAsyncExtensionWritesTheExpectedProcessInformationToLogFilesOnTheSystemWhenResultsAreProvided(
            int expectedExitCode, string expectedCommand, string expectedArguments, string expectedWorkingDir, string expectedStandardOutput, string expectedStandardError)
        {
            InMemoryProcess process = new InMemoryProcess
            {
                ExitCode = expectedExitCode,
                StartInfo = new ProcessStartInfo
                {
                    FileName = expectedCommand,
                    Arguments = expectedArguments,
                    WorkingDirectory = expectedWorkingDir
                },
                StandardOutput = expectedStandardOutput != null ? new Common.ConcurrentBuffer(new StringBuilder(expectedStandardOutput)) : null,
                StandardError = expectedStandardError != null ? new Common.ConcurrentBuffer(new StringBuilder(expectedStandardError)) : null
            };

            bool expectedLogFileWritten = false;
            string expectedResults = "Any results from the execution of the process.";

            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    string expectedLogDirectory = this.mockFixture.GetLogsPath(nameof(TestExecutor).ToLowerInvariant());

                    Assert.IsTrue(path.StartsWith(expectedLogDirectory), "Log directory not matched");
                    Assert.IsTrue(path.EndsWith($"{nameof(TestExecutor)}.log".ToLowerInvariant()), "Log file name not matched");
                    Assert.IsTrue(content.Contains($"Command           : {expectedCommand} {expectedArguments}".TrimEnd(), StringComparison.Ordinal), "Command missing");
                    Assert.IsTrue(content.Contains($"Working Directory : {expectedWorkingDir}", StringComparison.Ordinal), "Working directory missing");
                    Assert.IsTrue(content.Contains($"Exit Code         : {expectedExitCode}", StringComparison.Ordinal), "Exit code missing");
                    Assert.IsTrue(content.Contains($"##GeneratedResults##", StringComparison.Ordinal), "Results delimiter missing");

                    if (!string.IsNullOrWhiteSpace(expectedStandardOutput))
                    {
                        Assert.IsTrue(content.Contains($"##StandardOutput##", StringComparison.Ordinal), "Output delimiter missing");
                        Assert.IsTrue(content.Contains(expectedStandardOutput, StringComparison.Ordinal), "Standard output missing");
                    }

                    if (!string.IsNullOrWhiteSpace(expectedStandardError))
                    {
                        Assert.IsTrue(content.Contains(expectedStandardError, StringComparison.Ordinal), "Standard error missing");
                    }

                    Assert.IsTrue(content.Contains(expectedResults, StringComparison.Ordinal));

                    expectedLogFileWritten = true;
                });

            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters.Clear();
            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), results: new List<string> { expectedResults }, logToTelemetry: false, logToFile: true)
               .ConfigureAwait(false);

            Assert.IsTrue(expectedLogFileWritten);
        }

        [Test]
        public async Task LogProcessDetailsExtensionCreatesTheLogDirectoryIfItDoesNotExist()
        {
            InMemoryProcess process = new InMemoryProcess();
            TestExecutor component = new TestExecutor(this.mockFixture);
 
            string expectedLogPath = this.mockFixture.GetLogsPath(nameof(TestExecutor).ToLowerInvariant());

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), logToTelemetry: false, logToFile: true)
               .ConfigureAwait(false);

            this.mockFixture.Directory.Verify(dir => dir.CreateDirectory(expectedLogPath), Times.Once);
        }

        [Test]
        public async Task LogProcessDetailsExtensionCreatesTheLogDirectoryIfItDoesNotExistWhenTheToolsetNameIsProvided()
        {
            InMemoryProcess process = new InMemoryProcess();
            TestExecutor component = new TestExecutor(this.mockFixture);

            string expectedToolset = "AnyWorkloadToolset";
            string expectedLogPath = this.mockFixture.GetLogsPath(expectedToolset.ToLowerInvariant());

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), expectedToolset, logToTelemetry: false, logToFile: true)
               .ConfigureAwait(false);

            this.mockFixture.Directory.Verify(dir => dir.CreateDirectory(expectedLogPath), Times.Once);
        }

        [Test]
        [TestCase("AccessKey", "AnyKey123")]
        [TestCase("AccessToken", "AnyToken123")]
        [TestCase("Password", "AnyPass123")]
        [TestCase("Pwd", "AnyPass123")]
        public async Task LogProcessDetailsExtensionRemovesSensitiveDataFromTheProcessCommandDetailsWhenLoggingToFile(string sensitiveDataReference, string sensitiveData)
        {
            InMemoryProcess process = new InMemoryProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "C:\\any\\anyworkload.exe",
                    Arguments = $"--sensitive-data=\"{sensitiveDataReference}={sensitiveData}\""
                }
            };

            TestExecutor component = new TestExecutor(this.mockFixture);

            bool confirmed = false;
            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    Assert.IsFalse(content.Contains(sensitiveData, StringComparison.OrdinalIgnoreCase));
                    confirmed = true;
                });

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), logToTelemetry: false, logToFile: true)
               .ConfigureAwait(false);

            Assert.IsTrue(confirmed);
        }

        [Test]
        public async Task LogProcessDetailsExtensionRemovesWhitespaceFromToolsetNamesWhenLoggingToFile()
        {
            InMemoryProcess process = new InMemoryProcess();
            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters.Clear();

            bool confirmed = false;
            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    string expectedLogPath = this.mockFixture.GetLogsPath("anytoolset");

                    Assert.IsTrue(path.StartsWith(expectedLogPath));
                    Assert.IsTrue(path.EndsWith("anytoolset.log"));
                    confirmed = true;
                });

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), "AnyToolset", logToTelemetry: false, logToFile: true)
               .ConfigureAwait(false);

            Assert.IsTrue(confirmed);
        }

        [Test]
        [TestCase("<anytoolset<")]
        [TestCase(">anytoolset>")]
        [TestCase(":anytoolset:")]
        [TestCase("\"anytoolset\"")]
        [TestCase("/anytoolset/")]
        [TestCase("\\anytoolset\\")]
        [TestCase("|anytoolset|")]
        [TestCase("?anytoolset?")]
        [TestCase("*anytoolset*")]
        public async Task LogProcessDetailsExtensionHandlesReservedCharactersInTheToolsetNamesWhenLoggingToFile(string toolsetName)
        {
            InMemoryProcess process = new InMemoryProcess();
            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters.Clear();

            bool confirmed = false;
            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    string expectedLogPath = this.mockFixture.GetLogsPath("anytoolset");

                    Assert.IsTrue(path.StartsWith(expectedLogPath));
                    Assert.IsTrue(path.EndsWith("anytoolset.log"));
                    confirmed = true;
                });

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()), toolsetName, logToTelemetry: false, logToFile: true)
               .ConfigureAwait(false);

            Assert.IsTrue(confirmed);
        }

        [Test]
        public async Task LogProcessDetailsExtensionWritesAFileUploadNotificationFileWhenAContentStoreIsDefinedOnTheCommandLine()
        {
            // Ensure there is a content store defined. This indicates to the application that the user
            // supplied an intention on the command line (e.g. --contentStore) to have files uploaded to a
            // storage account.
            this.mockFixture.Dependencies.AddSingleton<IEnumerable<IBlobManager>>(
                new List<IBlobManager>
                {
                    this.mockFixture.ContentBlobManager.Object,
                    this.mockFixture.PackagesBlobManager.Object
                });

            InMemoryProcess process = new InMemoryProcess();
            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters.Clear();

            bool confirmed = false;
            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    if (path.EndsWith("upload.json"))
                    {
                        string expectedLogPath = this.mockFixture.PlatformSpecifics.ContentUploadsDirectory;
                        Assert.IsTrue(path.StartsWith(expectedLogPath));
                        confirmed = true;
                    }
                });

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()));
            Assert.IsTrue(confirmed);
        }

        [Test]
        public async Task LogProcessDetailsExtensionFileUploadDescriptionContentContainsExpectedContextInformationToTheRelatedLogFile()
        {
            // Ensure there is a content store defined. This indicates to the application that the user
            // supplied an intention on the command line (e.g. --contentStore) to have files uploaded to a
            // storage account.
            this.mockFixture.Dependencies.AddSingleton<IEnumerable<IBlobManager>>(
                new List<IBlobManager>
                {
                    this.mockFixture.ContentBlobManager.Object,
                    this.mockFixture.PackagesBlobManager.Object
                });

            InMemoryProcess process = new InMemoryProcess();
            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters.Clear();

            bool confirmed = false;
            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    if (path.EndsWith("upload.json"))
                    {
                        FileUploadDescriptor descriptor = content.FromJson<FileUploadDescriptor>();
                        Assert.IsNotNull(descriptor);
                        Assert.IsNotNull(descriptor.BlobName);
                        Assert.IsNotNull(descriptor.ContainerName);
                        Assert.IsNotNull(descriptor.ContentEncoding);
                        Assert.IsNotNull(descriptor.ContentType);
                        Assert.IsNotNull(descriptor.FilePath);
                        confirmed = true;
                    }
                });

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()));
            Assert.IsTrue(confirmed);
        }

        [Test]
        public async Task LogProcessDetailsExtensionFileUploadDescriptionContentContainsAManifest()
        {
            // Ensure there is a content store defined. This indicates to the application that the user
            // supplied an intention on the command line (e.g. --contentStore) to have files uploaded to a
            // storage account.
            this.mockFixture.Dependencies.AddSingleton<IEnumerable<IBlobManager>>(
                new List<IBlobManager>
                {
                    this.mockFixture.ContentBlobManager.Object,
                    this.mockFixture.PackagesBlobManager.Object
                });

            InMemoryProcess process = new InMemoryProcess();
            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters.Clear();

            bool confirmed = false;
            this.mockFixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    if (path.EndsWith("upload.json"))
                    {
                        FileUploadDescriptor descriptor = content.FromJson<FileUploadDescriptor>();
                        Assert.IsNotNull(descriptor);
                        Assert.IsNotNull(descriptor.Manifest);
                        Assert.IsNotEmpty(descriptor.Manifest);
                        confirmed = true;
                    }
                });

            await component.LogProcessDetailsAsync(process, new EventContext(Guid.NewGuid()));
            Assert.IsTrue(confirmed);
        }

        [Test]
        public void LogFailedMetricExtensionOnComponentLogsTheExpectedInformation_scenario1()
        {
            // Scenario:
            // Minimum info provided. Additionally, the parameters for the component do not contain a 'Scenario'
            // definition.

            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters.Clear();
            component.LogSuccessOrFailedMetric(false, telemetryContext: new EventContext(Guid.NewGuid()));

            var loggedMetric = this.mockFixture.Logger.FirstOrDefault();

            Assert.IsNotNull(loggedMetric);
            Assert.IsInstanceOf<EventContext>(loggedMetric.Item3);
            EventContext context = loggedMetric.Item3 as EventContext;

            Assert.IsTrue(context.Properties.Count == 14
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
                && context.Properties.ContainsKey("tags")
                && context.Properties.ContainsKey("metadata_metrics")
                && context.Properties["scenarioName"].ToString() == "Outcome"
                && context.Properties["scenarioArguments"].ToString() == string.Empty
                && context.Properties["metricName"].ToString() == "Failed"
                && context.Properties["metricValue"].ToString() == "1"
                && context.Properties["metricCategorization"].ToString() == string.Empty
                && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution failed for the scenario defined."
                && context.Properties["metricRelativity"].ToString() == MetricRelativity.LowerIsBetter.ToString()
                && context.Properties["metricVerbosity"].ToString() == "0"
                && context.Properties["toolName"].ToString() == component.TypeName
                && context.Properties["tags"].ToString() == string.Empty
                && context.Properties["metadata_metrics"].ToString() == string.Empty);
        }

        [Test]
        public void LogFailedMetricExtensionOnComponentLogsTheExpectedInformation_scenario2()
        {
            // Scenario:
            // Minimum info provided. Additionally, the parameters for the component contain a 'Scenario'
            // definition.

            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters[nameof(component.Scenario)] = "Any_Outcome";
            component.LogSuccessOrFailedMetric(false, telemetryContext: new EventContext(Guid.NewGuid()));

            var loggedMetric = this.mockFixture.Logger.FirstOrDefault();

            Assert.IsNotNull(loggedMetric);
            Assert.IsInstanceOf<EventContext>(loggedMetric.Item3);
            EventContext context = loggedMetric.Item3 as EventContext;

            Assert.IsTrue(context.Properties.Count == 14
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
                    && context.Properties.ContainsKey("tags")
                    && context.Properties.ContainsKey("metadata_metrics")
                    && context.Properties["scenarioName"].ToString() == "Any_Outcome"
                    && context.Properties["scenarioArguments"].ToString() == string.Empty
                    && context.Properties["metricName"].ToString() == "Failed"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == string.Empty
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution failed for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.LowerIsBetter.ToString()
                    && context.Properties["metricVerbosity"].ToString() == "0"
                    && context.Properties["toolName"].ToString() == component.TypeName
                    && context.Properties["tags"].ToString() == string.Empty
                    && context.Properties["metadata_metrics"].ToString() == string.Empty);
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
            component.LogSuccessOrFailedMetric(
                false, expectedToolName, expectedToolVersion, expectedScenarioName, expectedScenarioArguments, expectedMetricCategorization, expectedStartTime, expectedEndTime, telemetryContext: new EventContext(Guid.NewGuid()));

            var loggedMetric = this.mockFixture.Logger.FirstOrDefault();

            Assert.IsNotNull(loggedMetric);
            Assert.IsInstanceOf<EventContext>(loggedMetric.Item3);
            EventContext context = loggedMetric.Item3 as EventContext;

            Assert.IsTrue(context.Properties.Count == 14
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
                    && context.Properties.ContainsKey("tags")
                    && context.Properties.ContainsKey("metadata_metrics")
                    && context.Properties["scenarioName"].ToString() == expectedScenarioName
                    && context.Properties["scenarioArguments"].ToString() == expectedScenarioArguments
                    && context.Properties["metricName"].ToString() == "Failed"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == expectedMetricCategorization
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution failed for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.LowerIsBetter.ToString()
                    && context.Properties["metricVerbosity"].ToString() == "0"
                    && context.Properties["toolName"].ToString() == expectedToolName
                    && context.Properties["tags"].ToString() == string.Empty
                    && context.Properties["metadata_metrics"].ToString() == string.Empty);
        }

        [Test]
        public void LogSuccessMetricExtensionOnComponentLogsTheExpectedInformation_scenario1()
        {
            // Scenario:
            // Minimum info provided. Additionally, the parameters for the component do not contain a 'Scenario'
            // definition.

            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters.Clear();
            component.LogSuccessOrFailedMetric(true, telemetryContext: new EventContext(Guid.NewGuid()));

            var loggedMetric = this.mockFixture.Logger.FirstOrDefault();

            Assert.IsNotNull(loggedMetric);
            Assert.IsInstanceOf<EventContext>(loggedMetric.Item3);
            EventContext context = loggedMetric.Item3 as EventContext;

            Assert.IsTrue(context.Properties.Count == 14
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
                    && context.Properties.ContainsKey("tags")
                    && context.Properties.ContainsKey("metadata_metrics")
                    && context.Properties["scenarioName"].ToString() == "Outcome"
                    && context.Properties["scenarioArguments"].ToString() == string.Empty
                    && context.Properties["metricName"].ToString() == "Succeeded"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == string.Empty
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution succeeded for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.HigherIsBetter.ToString()
                    && context.Properties["metricVerbosity"].ToString() == "2"
                    && context.Properties["toolName"].ToString() == component.TypeName
                    && context.Properties["tags"].ToString() == string.Empty
                    && context.Properties["metadata_metrics"].ToString() == string.Empty);
        }

        [Test]
        public void LogSuccessMetricExtensionOnComponentLogsTheExpectedInformation_scenario2()
        {
            // Scenario:
            // Minimum info provided. Additionally, the parameters for the component contain a 'Scenario'
            // definition.

            TestExecutor component = new TestExecutor(this.mockFixture);
            component.Parameters[nameof(component.Scenario)] = "Any_Outcome";
            component.LogSuccessOrFailedMetric(true, telemetryContext: new EventContext(Guid.NewGuid()));

            var loggedMetric = this.mockFixture.Logger.FirstOrDefault();

            Assert.IsNotNull(loggedMetric);
            Assert.IsInstanceOf<EventContext>(loggedMetric.Item3);
            EventContext context = loggedMetric.Item3 as EventContext;

            Assert.IsTrue(context.Properties.Count == 14
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
                    && context.Properties.ContainsKey("tags")
                    && context.Properties.ContainsKey("metadata_metrics")
                    && context.Properties["scenarioName"].ToString() == "Any_Outcome"
                    && context.Properties["scenarioArguments"].ToString() == string.Empty
                    && context.Properties["metricName"].ToString() == "Succeeded"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == string.Empty
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution succeeded for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.HigherIsBetter.ToString()
                    && context.Properties["metricVerbosity"].ToString() == "2"
                    && context.Properties["toolName"].ToString() == component.TypeName
                    && context.Properties["tags"].ToString() == string.Empty
                    && context.Properties["metadata_metrics"].ToString() == string.Empty);
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
            component.LogSuccessOrFailedMetric(
                true, expectedToolName, expectedToolVersion, expectedScenarioName, expectedScenarioArguments, expectedMetricCategorization, expectedStartTime, expectedEndTime, telemetryContext: new EventContext(Guid.NewGuid()));

            var loggedMetric = this.mockFixture.Logger.FirstOrDefault();

            Assert.IsNotNull(loggedMetric);
            Assert.IsInstanceOf<EventContext>(loggedMetric.Item3);
            EventContext context = loggedMetric.Item3 as EventContext;

            Assert.IsTrue(context.Properties.Count == 14
                    && context.Properties.ContainsKey("scenarioName")
                    && context.Properties.ContainsKey("scenarioStartTime")
                    && context.Properties.ContainsKey("scenarioEndTime")
                    && context.Properties.ContainsKey("scenarioArguments")
                    && context.Properties.ContainsKey("metricName")
                    && context.Properties.ContainsKey("metricValue")
                    && context.Properties.ContainsKey("metricCategorization")
                    && context.Properties.ContainsKey("metricDescription")
                    && context.Properties.ContainsKey("metricRelativity")
                    && context.Properties.ContainsKey("metricVerbosity")
                    && context.Properties.ContainsKey("toolName")
                    && context.Properties.ContainsKey("tags")
                    && context.Properties.ContainsKey("metadata_metrics")
                    && context.Properties["scenarioName"].ToString() == expectedScenarioName
                    && context.Properties["scenarioArguments"].ToString() == expectedScenarioArguments
                    && context.Properties["metricName"].ToString() == "Succeeded"
                    && context.Properties["metricValue"].ToString() == "1"
                    && context.Properties["metricCategorization"].ToString() == expectedMetricCategorization
                    && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution succeeded for the scenario defined."
                    && context.Properties["metricRelativity"].ToString() == MetricRelativity.HigherIsBetter.ToString()
                    && context.Properties["metricVerbosity"].ToString() == "2"
                    && context.Properties["toolName"].ToString() == expectedToolName
                    && context.Properties["tags"].ToString() == string.Empty
                    && context.Properties["metadata_metrics"].ToString() == string.Empty);
        }

        [Test]
        [TestCase(LogLevel.Critical)]
        [TestCase(LogLevel.Debug)]
        [TestCase(LogLevel.Error)]
        [TestCase(LogLevel.Information)]
        [TestCase(LogLevel.None)]
        [TestCase(LogLevel.Trace)]
        [TestCase(LogLevel.Warning)]
        public void LogSystemEventExtensionLogsTheExpectedEvents(LogLevel expectedEventLevel)
        {
            string expectedEventType = "EventResult";
            string expectedEventSource = "AnySource";
            string expectedEventDescription = "Test of the system event telemetry system.";
            long expectedEventId = 100;

            IDictionary<string, object> expectedEventInfo = new Dictionary<string, object>
            {
                ["property1"] = "Process shutdown unexpectedly.",
                ["property2"] = 1234,
            };

            this.mockLogger.Object.LogSystemEvent(
                expectedEventType, 
                expectedEventSource,
                expectedEventId.ToString(),
                expectedEventLevel,
                this.mockEventContext,
                null,
                expectedEventDescription,
                expectedEventInfo);

            this.mockLogger.Verify(logger => logger.Log(
                expectedEventLevel,
                It.Is<EventId>(eventId => eventId.Id == (int)LogType.SystemEvent && eventId.Name == expectedEventType),
                It.Is<EventContext>(context => context.ActivityId == this.mockEventContext.ActivityId
                    && context.ParentActivityId == this.mockEventContext.ParentActivityId
                    && context.Properties.ContainsKey("eventType")
                    && context.Properties.ContainsKey("eventInfo")),
                null,
                null),
                Times.Exactly(1));
        }

        [Test]
        public void LogSystemEventExtensionIncludesTheSystemEventInformationInTheEventContext_1()
        {
            string expectedEventType = "EventResult";
            string expectedEventSource = "AnySource";
            string expectedEventDescription = "Test of the system event telemetry system.";
            long expectedEventId = 100;

            this.mockLogger
                .Setup(logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<EventContext>(), null, null))
                .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, state, exc, formatter) =>
                {
                    Assert.IsNotNull(state);
                    Assert.IsTrue(state.Properties.ContainsKey("eventType"));
                    Assert.IsTrue(state.Properties.ContainsKey("eventInfo"));
                    Assert.AreEqual(expectedEventType, state.Properties["eventType"]);

                    IDictionary<string, object> actualEventInfo = state.Properties["eventInfo"] as IDictionary<string, object>;
                    Assert.IsNotNull(actualEventInfo);
                    Assert.AreEqual(expectedEventDescription, actualEventInfo["eventDescription"]);
                    Assert.AreEqual(expectedEventSource, actualEventInfo["eventSource"]);
                    Assert.AreEqual(expectedEventId.ToString(), actualEventInfo["eventId"].ToString());
                });

            this.mockLogger.Object.LogSystemEvent(
                expectedEventType,
                expectedEventSource,
                expectedEventId.ToString(),
                LogLevel.Information,
                this.mockEventContext,
                null,
                expectedEventDescription);
        }

        [Test]
        public void LogSystemEventExtensionIncludesTheSystemEventInformationInTheEventContext_2()
        {
            string expectedEventType = "EventResult";
            string expectedEventSource = "AnySource";
            string expectedEventDescription = "Test of the system event telemetry system.";
            long expectedEventId = 100;

            IDictionary<string, object> expectedEventInfo = new Dictionary<string, object>
            {
                ["property1"] = "Process shutdown unexpectedly.",
                ["property2"] = 1234,
            };

            this.mockLogger
                .Setup(logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<EventContext>(), null, null))
                .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, state, exc, formatter) =>
                {
                    Assert.IsNotNull(state);
                    Assert.IsTrue(state.Properties.ContainsKey("eventType"));
                    Assert.IsTrue(state.Properties.ContainsKey("eventInfo"));
                    Assert.AreEqual(expectedEventType, state.Properties["eventType"]);

                    IDictionary<string, object> actualEventInfo = state.Properties["eventInfo"] as IDictionary<string, object>;
                    Assert.IsNotNull(actualEventInfo);
                    Assert.AreEqual(expectedEventInfo["property1"], actualEventInfo["property1"]);
                    Assert.AreEqual(expectedEventInfo["property2"], actualEventInfo["property2"]);
                    Assert.AreEqual(expectedEventDescription, actualEventInfo["eventDescription"]);
                    Assert.AreEqual(expectedEventSource, actualEventInfo["eventSource"]);
                    Assert.AreEqual(expectedEventId.ToString(), actualEventInfo["eventId"].ToString());
                });

            this.mockLogger.Object.LogSystemEvent(
                expectedEventType,
                expectedEventSource,
                expectedEventId.ToString(),
                LogLevel.Information,
                this.mockEventContext,
                null,
                expectedEventDescription,
                expectedEventInfo);
        }

        [Test]
        public void LogSystemEventExtensionDoesNotSideEffectOrChangeAnEventContextProvided()
        {
            string expectedEventType = "EventResult";
            string expectedEventSource = "AnySource";
            string expectedEventDescription = "Test of the system event telemetry system.";
            long expectedEventId = 100;

            EventContext originalContext = new EventContext(Guid.NewGuid(), new Dictionary<string, object>
            {
                ["property1"] = "Any Value",
                ["property2"] = 1234
            });

            EventContext cloneOfOriginal = originalContext.Clone();

            IDictionary<string, object> expectedEventInfo = new Dictionary<string, object>
            {
                ["property1"] = "Process shutdown unexpectedly.",
                ["property2"] = 1234,
            };

            this.mockLogger
                .Setup(logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<EventContext>(), null, null))
                .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, state, exc, formatter) =>
                {
                    // A clone is used to avoid side effecting the original
                    EventContext actualContext = state;
                    Assert.IsFalse(object.ReferenceEquals(originalContext, actualContext));
                });

            this.mockLogger.Object.LogSystemEvent(
                expectedEventType,
                expectedEventSource,
                expectedEventId.ToString(),
                LogLevel.Information,
                this.mockEventContext,
                null,
                expectedEventDescription,
                expectedEventInfo);

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