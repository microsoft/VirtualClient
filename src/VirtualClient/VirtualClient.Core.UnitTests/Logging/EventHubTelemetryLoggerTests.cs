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

        private class TestEventHubTelemetryLogger : EventHubTelemetryLogger
        {
            public TestEventHubTelemetryLogger(EventHubTelemetryChannel channel, LogLevel level)
                : base(channel, level)
            {
            }

            public new EventData CreateEventObject(string eventMessage, LogLevel logLevel, DateTime eventTimestamp, EventContext eventContext, object bufferInfo = null)
            {
                return base.CreateEventObject(eventMessage, logLevel, eventTimestamp, eventContext, bufferInfo);
            }
        }
    }
}
