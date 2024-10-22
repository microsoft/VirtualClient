// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class TelemetryRoutingTests
    {
        [Test]
        public void VirtualClientTelemetryRoutingSendsUnexpectedLogTypeEventsToTheExpectedLogger_1()
        {
            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>();
            Mock<ILogger> loggerExpected = new Mock<ILogger>();

            // Setup this logger/logger provider to handle events that are the expected LogType.
            loggerProviders.Add(new TestLoggerProvider(loggerExpected.Object).HandleTraces());

            ILogger logger = new LoggerFactory(loggerProviders).CreateLogger("AnyCategory");
            logger.Log(LogLevel.Information, new EventId((int)LogType.Undefined, "AnyName"), "AnyState", null, null);

            // The expected logger should have handled the event.
            loggerExpected.Verify(
                logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()));
        }

        [Test]
        public void VirtualClientTelemetryRoutingSendsUnexpectedLogTypeEventsToTheExpectedLogger_2()
        {
            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>();
            Mock<ILogger> loggerExpected = new Mock<ILogger>();

            // Setup this logger/logger provider to handle events that are the expected LogType.
            loggerProviders.Add(new TestLoggerProvider(loggerExpected.Object).HandleTraces());

            ILogger logger = new LoggerFactory(loggerProviders).CreateLogger("AnyCategory");
            logger.Log(LogLevel.Information, new EventId((int)LogType.Trace, "AnyName"), "AnyState", null, null);

            // The expected logger should have handled the event.
            loggerExpected.Verify(
                logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()));
        }

        [Test]
        public void VirtualClientTelemetryRoutingSendsTraceLogTypeEventsToTheExpectedLogger()
        {
            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>();
            Mock<ILogger> loggerExpected = new Mock<ILogger>();

            // Setup this logger/logger provider to handle events that are the expected LogType.
            loggerProviders.Add(new TestLoggerProvider(loggerExpected.Object).HandleTraces());

            ILogger logger = new LoggerFactory(loggerProviders).CreateLogger("AnyCategory");
            logger.Log(LogLevel.Information, new EventId((int)LogType.Trace, "AnyName"), "AnyState", null, null);

            // The expected logger should have handled the event.
            loggerExpected.Verify(
                logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()));
        }

        [Test]
        public void VirtualClientTelemetryRoutingSendsErrorLogTypeEventsToTheExpectedLogger()
        {
            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>();
            Mock<ILogger> loggerExpected = new Mock<ILogger>();

            // Setup this logger/logger provider to handle events that are the expected LogType.
            loggerProviders.Add(new TestLoggerProvider(loggerExpected.Object).HandleTraces());

            ILogger logger = new LoggerFactory(loggerProviders).CreateLogger("AnyCategory");
            logger.Log(LogLevel.Error, new EventId((int)LogType.Error, "AnyName"), "AnyState", null, null);

            // The expected logger should have handled the event.
            loggerExpected.Verify(
                logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()));
        }

        [Test]
        public void VirtualClientTelemetryRoutingSendsPerformanceCounterLogTypeEventsToTheExpectedLogger()
        {
            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>();
            Mock<ILogger> loggerExpected = new Mock<ILogger>();

            // Setup this logger/logger provider to handle events that are the expected LogType.
            loggerProviders.Add(new TestLoggerProvider(loggerExpected.Object).HandlePerformanceCounters());

            ILogger logger = new LoggerFactory(loggerProviders).CreateLogger("AnyCategory");
            logger.Log(LogLevel.Information, new EventId((int)LogType.Metrics, "PerformanceCounter"), "AnyState", null, null);

            // The expected logger should have handled the event.
            loggerExpected.Verify(
                logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()));
        }

        [Test]
        public void VirtualClientTelemetryRoutingSendsSystemEventLogTypeEventsToTheExpectedLogger()
        {
            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>();
            Mock<ILogger> loggerExpected = new Mock<ILogger>();

            // Setup this logger/logger provider to handle events that are the expected LogType.
            loggerProviders.Add(new TestLoggerProvider(loggerExpected.Object).HandleSystemEvents());

            ILogger logger = new LoggerFactory(loggerProviders).CreateLogger("AnyCategory");
            logger.Log(LogLevel.Information, new EventId((int)LogType.SystemEvent, "AnyName"), "AnyState", null, null);

            // The expected logger should have handled the event.
            loggerExpected.Verify(
                logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()));
        }

        [Test]
        public void VirtualClientTelemetryRoutingSendsTestMetricLogTypeEventsToTheExpectedLogger()
        {
            List<ILoggerProvider> loggerProviders = new List<ILoggerProvider>();
            Mock<ILogger> loggerExpected = new Mock<ILogger>();

            // Setup this logger/logger provider to handle events that are the expected LogType.
            loggerProviders.Add(new TestLoggerProvider(loggerExpected.Object).HandleMetrics());

            ILogger logger = new LoggerFactory(loggerProviders).CreateLogger("AnyCategory");
            logger.Log(LogLevel.Information, new EventId((int)LogType.Metrics, "AnyName"), "AnyState", null, null);

            // The expected logger should have handled the event.
            loggerExpected.Verify(
                logger => logger.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<object>(), It.IsAny<Exception>(), It.IsAny<Func<object, Exception, string>>()));
        }

        private class TestLoggerProvider : ILoggerProvider
        {
            private ILogger logger;

            public TestLoggerProvider(ILogger testLogger)
            {
                this.logger = testLogger;
            }

            public ILogger CreateLogger(string categoryName)
            {
                return this.logger;
            }

            public void Dispose()
            {
            }
        }
    }
}
