// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Producer;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class EventHubTelemetryLoggerTests
    {
        private static AssemblyName loggingAssembly = Assembly.GetAssembly(typeof(EventHubTelemetryLogger)).GetName();
        private static AssemblyName executingAssembly = Assembly.GetEntryAssembly().GetName();

        private EventHubTelemetryChannel mockChannel;

        [SetUp]
        public void SetupTest()
        {
            EventHubProducerClient mockClient = new EventHubProducerClient(
                "Endpoint=sb://anynamespace.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=AnYacCEssKey=",
                "any-hub");

            this.mockChannel = new EventHubTelemetryChannel(mockClient);
        }

        [Test]
        public void EventHubTelemetryLoggerCreatesTheExpectedEventDataObjectForUpload_1()
        {
            TestEventHubTelemetryLogger logger = new TestEventHubTelemetryLogger(this.mockChannel, LogLevel.Information);

            DateTime now = DateTime.UtcNow;
            EventContext telemetryContext = new EventContext(Guid.NewGuid(), Guid.NewGuid());
            EventData eventData = logger.CreateEventObject("TestMessage", LogLevel.Information, now, telemetryContext);
            Assert.IsNotNull(eventData);

            JObject eventBody = JObject.Parse(eventData.EventBody.ToString());
            Assert.IsNotEmpty(eventBody);
            Assert.AreEqual("TestMessage", eventBody["message"].ToString());
            Assert.AreEqual((int)LogLevel.Information, int.Parse(eventBody["severityLevel"].ToString()));
            Assert.AreEqual(telemetryContext.ActivityId.ToString(), eventBody["operation_Id"].ToString());
            Assert.AreEqual(telemetryContext.ParentActivityId.ToString(), eventBody["operation_ParentId"].ToString());
            Assert.AreEqual(loggingAssembly.Version.ToString(), eventBody["sdkVersion"].ToString());
            Assert.AreEqual(executingAssembly.Name, eventBody["appName"].ToString());
            Assert.AreEqual(Environment.MachineName, eventBody["appHost"].ToString());
            Assert.AreEqual(now, eventBody["timestamp"].Value<DateTime>());

            Assert.IsTrue(eventBody.ContainsKey("customDimensions"));
            Assert.AreEqual(telemetryContext.TransactionId.ToString(), eventBody["customDimensions"]["transactionId"].ToString());
            Assert.AreEqual(telemetryContext.DurationMs, long.Parse(eventBody["customDimensions"]["durationMs"].ToString()));
        }

        [Test]
        public void EventHubTelemetryLoggerCreatesTheExpectedEventDataObjectForUpload_2()
        {
            TestEventHubTelemetryLogger logger = new TestEventHubTelemetryLogger(this.mockChannel, LogLevel.Information);

            EventContext telemetryContext = new EventContext(Guid.NewGuid(), Guid.NewGuid(), new Dictionary<string, object>
            {
                ["property1"] = "Value",
                ["property2"] = 1234
            });

            DateTime now = DateTime.UtcNow;
            EventData eventData = logger.CreateEventObject("TestMessage", LogLevel.Information, now, telemetryContext);
            Assert.IsNotNull(eventData);

            JObject eventBody = JObject.Parse(eventData.EventBody.ToString());
            Assert.IsNotEmpty(eventBody);
            Assert.AreEqual("TestMessage", eventBody["message"].ToString());
            Assert.AreEqual((int)LogLevel.Information, int.Parse(eventBody["severityLevel"].ToString()));
            Assert.AreEqual(telemetryContext.ActivityId.ToString(), eventBody["operation_Id"].ToString());
            Assert.AreEqual(telemetryContext.ParentActivityId.ToString(), eventBody["operation_ParentId"].ToString());
            Assert.AreEqual(loggingAssembly.Version.ToString(), eventBody["sdkVersion"].ToString());
            Assert.AreEqual(executingAssembly.Name, eventBody["appName"].ToString());
            Assert.AreEqual(Environment.MachineName, eventBody["appHost"].ToString());
            Assert.AreEqual(now, eventBody["timestamp"].Value<DateTime>());

            Assert.IsTrue(eventBody.ContainsKey("customDimensions"));
            Assert.AreEqual(telemetryContext.TransactionId.ToString(), eventBody["customDimensions"]["transactionId"].ToString());
            Assert.AreEqual(telemetryContext.DurationMs, long.Parse(eventBody["customDimensions"]["durationMs"].ToString()));
            Assert.AreEqual("Value", eventBody["customDimensions"]["property1"].ToString());
            Assert.AreEqual(1234, int.Parse(eventBody["customDimensions"]["property2"].ToString()));
        }

        [Test]
        public void EventHubTelemetryLoggerSupportsOverridesToCertainContextProperties()
        {
            TestEventHubTelemetryLogger logger = new TestEventHubTelemetryLogger(this.mockChannel, LogLevel.Information);
            EventContext telemetryContext = new EventContext(Guid.NewGuid(), Guid.NewGuid());

            // Overridable properties
            DateTime expectedTimestamp = DateTime.UtcNow.AddMinutes(-30);
            telemetryContext.Properties["appHost"] = "Host01";
            telemetryContext.Properties["appName"] = "Metis";
            telemetryContext.Properties["appVersion"] = "1.2.3.4";
            telemetryContext.Properties["timestamp"] = expectedTimestamp.ToString("o");

            EventData eventData = logger.CreateEventObject("TestMessage", LogLevel.Information, expectedTimestamp, telemetryContext);
            Assert.IsNotNull(eventData);

            JObject eventBody = JObject.Parse(eventData.EventBody.ToString());
            Assert.IsNotEmpty(eventBody);
            Assert.AreEqual("1.2.3.4", eventBody["sdkVersion"].ToString());
            Assert.AreEqual("Metis", eventBody["appName"].ToString());
            Assert.AreEqual("Host01", eventBody["appHost"].ToString());
            Assert.AreEqual(expectedTimestamp, eventBody["timestamp"].Value<DateTime>());
        }

        [Test]
        public void EventHubTelemetryLoggerEnsuresTimestampsAreInUtcFormat()
        {
            TestEventHubTelemetryLogger logger = new TestEventHubTelemetryLogger(this.mockChannel, LogLevel.Information);
            EventContext telemetryContext = new EventContext(Guid.NewGuid(), Guid.NewGuid());

            // Overridable properties
            DateTime localTimestamp = DateTime.Now.AddMinutes(-30);
            DateTime expectedTimestamp = localTimestamp.ToUniversalTime();
            telemetryContext.Properties["appHost"] = "Host01";
            telemetryContext.Properties["appName"] = "Metis";
            telemetryContext.Properties["appVersion"] = "1.2.3.4";
            telemetryContext.Properties["timestamp"] = localTimestamp.ToString("o");

            EventData eventData = logger.CreateEventObject("TestMessage", LogLevel.Information, DateTime.UtcNow, telemetryContext);
            Assert.IsNotNull(eventData);

            JObject eventBody = JObject.Parse(eventData.EventBody.ToString());
            Assert.IsNotEmpty(eventBody);
            Assert.AreEqual("1.2.3.4", eventBody["sdkVersion"].ToString());
            Assert.AreEqual("Metis", eventBody["appName"].ToString());
            Assert.AreEqual("Host01", eventBody["appHost"].ToString());
            Assert.AreEqual(expectedTimestamp, eventBody["timestamp"].Value<DateTime>());
        }

        [Test]
        public void EventHubTelemetryLoggerLogsFormattedInformationWithoutAnEventContext()
        {
            TestEventHubTelemetryLogger logger = new TestEventHubTelemetryLogger(this.mockChannel, LogLevel.Information);

            logger.LogInformation("Processed {Count} items", 42);

            Assert.AreEqual(1, logger.Events.Count);
            JObject eventBody = JObject.Parse(logger.Events[0].EventBody.ToString());
            Assert.AreEqual("Processed 42 items", eventBody["message"].ToString());
            Assert.AreEqual((int)LogLevel.Information, eventBody["severityLevel"].Value<int>());
        }

        [TestCase("Plain message")]
        [TestCase(null)]
        public void EventHubTelemetryLoggerLogsPlainOrNullStateWithoutAnEventContext(string state)
        {
            TestEventHubTelemetryLogger logger = new TestEventHubTelemetryLogger(this.mockChannel, LogLevel.Information);

            logger.Log<string>(LogLevel.Information, default, state, null, null);

            Assert.AreEqual(1, logger.Events.Count);
            JObject eventBody = JObject.Parse(logger.Events[0].EventBody.ToString());
            Assert.AreEqual(state, eventBody["message"]?.Value<string>());
            Assert.AreEqual((int)LogLevel.Information, eventBody["severityLevel"].Value<int>());
            Assert.IsNotNull(eventBody["customDimensions"]);
        }

        [TestCase("Plain message")]
        [TestCase(null)]
        public void EventHubTelemetryLoggerPreservesAnExplicitEventNameWithoutAnEventContext(string state)
        {
            TestEventHubTelemetryLogger logger = new TestEventHubTelemetryLogger(this.mockChannel, LogLevel.Information);

            logger.Log<string>(LogLevel.Warning, new EventId(1, "NamedEvent"), state, null, (value, error) => "Formatted message");

            Assert.AreEqual(1, logger.Events.Count);
            JObject eventBody = JObject.Parse(logger.Events[0].EventBody.ToString());
            Assert.AreEqual("NamedEvent", eventBody["message"].ToString());
            Assert.AreEqual((int)LogLevel.Warning, eventBody["severityLevel"].Value<int>());
        }

        [TestCase(true)]
        [TestCase(false)]
        public void EventHubTelemetryLoggerLogsExceptionsWithoutAnEventContext(bool withState)
        {
            TestEventHubTelemetryLogger logger = new TestEventHubTelemetryLogger(this.mockChannel, LogLevel.Information);
            Exception exception = new ArgumentException("Test exception");

            logger.Log<string>(
                LogLevel.Error,
                default,
                withState ? "Test state" : null,
                exception,
                (value, error) => $"{value}: {error.Message}");

            Assert.AreEqual(1, logger.Events.Count);
            JObject eventBody = JObject.Parse(logger.Events[0].EventBody.ToString());
            if (withState)
            {
                Assert.AreEqual("Test state: Test exception", eventBody["message"].ToString());
            }
            else
            {
                StringAssert.Contains("Test exception", eventBody["message"].ToString());
                StringAssert.Contains(nameof(ArgumentException), eventBody["message"].ToString());
            }

            Assert.AreEqual((int)LogLevel.Error, eventBody["severityLevel"].Value<int>());
        }

        [Test]
        [NonParallelizable]
        public void EventHubTelemetryLoggerUsesPersistedContextForOrdinaryLogs()
        {
            TestEventHubTelemetryLogger logger = new TestEventHubTelemetryLogger(this.mockChannel, LogLevel.Information);
            EventContext originalContext = EventContext.Persisted();
            Guid activityId = Guid.NewGuid();
            Guid parentActivityId = Guid.NewGuid();
            string propertyName = Guid.NewGuid().ToString();

            try
            {
                EventContext.Persist(activityId, parentActivityId);
                EventContext.PersistentProperties[propertyName] = "Persistent value";

                logger.LogInformation("Ordinary message");

                Assert.AreEqual(1, logger.Events.Count);
                JObject eventBody = JObject.Parse(logger.Events[0].EventBody.ToString());
                Assert.AreEqual("Ordinary message", eventBody["message"].ToString());
                Assert.AreEqual(activityId.ToString(), eventBody["operation_Id"].ToString());
                Assert.AreEqual(parentActivityId.ToString(), eventBody["operation_ParentId"].ToString());
                Assert.AreEqual("Persistent value", eventBody["customDimensions"][propertyName].ToString());
            }
            finally
            {
                EventContext.PersistentProperties.Remove(propertyName);
                EventContext.Persist(originalContext.ActivityId, originalContext.ParentActivityId);
            }
        }

        [Test]
        public void EventHubTelemetryLoggerPreservesStructuredContextWhenLogging()
        {
            TestEventHubTelemetryLogger logger = new TestEventHubTelemetryLogger(this.mockChannel, LogLevel.Information);
            DateTime timestamp = DateTime.UtcNow.AddMinutes(-30);
            EventContext context = new EventContext(Guid.NewGuid(), Guid.NewGuid())
                .AddContext("appHost", "Host01")
                .AddContext("timestamp", timestamp.ToString("o"))
                .AddContext("property1", "Value");

            logger.Log<EventContext>(LogLevel.Information, new EventId(1, "StructuredEvent"), context, null, null);

            Assert.AreEqual(1, logger.Events.Count);
            JObject eventBody = JObject.Parse(logger.Events[0].EventBody.ToString());
            Assert.AreEqual("StructuredEvent", eventBody["message"].ToString());
            Assert.AreEqual(context.ActivityId.ToString(), eventBody["operation_Id"].ToString());
            Assert.AreEqual(context.ParentActivityId.ToString(), eventBody["operation_ParentId"].ToString());
            Assert.AreEqual(context.TransactionId.ToString(), eventBody["customDimensions"]["transactionId"].ToString());
            Assert.AreEqual("Host01", eventBody["appHost"].ToString());
            Assert.AreEqual(timestamp, eventBody["timestamp"].Value<DateTime>());
            Assert.AreEqual("Value", eventBody["customDimensions"]["property1"].ToString());
        }

        [Test]
        public void EventHubTelemetryLoggerPreservesOversizedContextHandling()
        {
            TestEventHubTelemetryLogger logger = new TestEventHubTelemetryLogger(this.mockChannel, LogLevel.Information);
            EventContext context = new EventContext(Guid.NewGuid(), Guid.NewGuid())
                .AddContext("largeProperty", new string('a', EventHubTelemetryChannel.MaxEventDataBytes));

            logger.Log<EventContext>(LogLevel.Information, new EventId(1, "LargeEvent"), context, null, null);

            Assert.AreEqual(1, logger.Events.Count);
            JObject eventBody = JObject.Parse(logger.Events[0].EventBody.ToString());
            Assert.AreEqual("LargeEvent", eventBody["message"].ToString());
            Assert.AreEqual(context.ActivityId.ToString(), eventBody["operation_Id"].ToString());
            Assert.AreEqual(context.ParentActivityId.ToString(), eventBody["operation_ParentId"].ToString());
            Assert.AreEqual(bool.TrueString, eventBody["customDimensions"]["exceededSizeLimits"].ToString());
            Assert.IsNull(eventBody["customDimensions"]["largeProperty"]);
            Assert.Less(logger.Events[0].Body.Length, EventHubTelemetryChannel.MaxEventDataBytes);
        }

        [Test]
        public void EventHubTelemetryLoggerDoesNotEmitDisabledLogs()
        {
            TestEventHubTelemetryLogger logger = new TestEventHubTelemetryLogger(this.mockChannel, LogLevel.Warning);

            logger.LogInformation("Disabled message");

            Assert.IsEmpty(logger.Events);
        }

        private class TestEventHubTelemetryLogger : EventHubTelemetryLogger
        {
            public TestEventHubTelemetryLogger(EventHubTelemetryChannel channel, LogLevel level)
                : base(channel, level)
            {
            }

            public IList<EventData> Events { get; } = new List<EventData>();

            public new EventData CreateEventObject(string eventMessage, LogLevel logLevel, DateTime eventTimestamp, EventContext eventContext, object bufferInfo = null)
            {
                return base.CreateEventObject(eventMessage, logLevel, eventTimestamp, eventContext, bufferInfo);
            }

            protected override void AddEventDataToChannel(EventData eventData)
            {
                this.Events.Add(eventData);
            }
        }
    }
}
