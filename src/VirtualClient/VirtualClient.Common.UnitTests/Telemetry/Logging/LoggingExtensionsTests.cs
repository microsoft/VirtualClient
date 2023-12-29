// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class LoggingExtensionsTests
    {
        private Mock<ILogger> mockLogger;
        private string exampleEventName;
        private EventId exampleEventId;
        private LogLevel exampleLogLevel;
        private EventContext exampleEventContext;

        [SetUp]
        public void SetupTest()
        {
            this.mockLogger = new Mock<ILogger>();
            this.exampleEventName = "Any.Event";
            this.exampleEventId = new EventId(123, this.exampleEventName);
            this.exampleLogLevel = LogLevel.Information;
            this.exampleEventContext = new EventContext(Guid.NewGuid());
        }

        [Test]
        public void LogTelemetryExtensionCapturesTheExpectedTelemetryEvent()
        {
            this.mockLogger.Object.LogTelemetry(this.exampleEventName, this.exampleLogLevel, this.exampleEventContext);

            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(id => id.Name == this.exampleEventName),
                this.exampleEventContext,
                null,
                null));
        }

        [Test]
        public void LogTelemetryExtensionCapturesTheExpectedTelemetryEvent2()
        {
            this.mockLogger.Object.LogTelemetry(this.exampleEventId, this.exampleLogLevel, this.exampleEventContext);

            this.mockLogger.Verify(logger => logger.Log(
                LogLevel.Information,
                It.Is<EventId>(id => id == this.exampleEventId),
                this.exampleEventContext,
                null,
                null));
        }

        [Test]
        public void LogTelemetryExtensionCapturesTheExpectedTelemetryEvents3()
        {
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

            this.mockLogger.Object.LogTelemetry(this.exampleEventName, this.exampleEventContext, () => { });

            Assert.IsTrue(eventsLogged.Count == 2);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{this.exampleEventName}Start"
                && evt.Item2 == LogLevel.Information
                && object.ReferenceEquals(this.exampleEventContext, evt.Item3)) == 1);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{this.exampleEventName}Stop"
                && evt.Item2 == LogLevel.Information
                && object.ReferenceEquals(this.exampleEventContext, evt.Item3)) == 1);
        }

        [Test]
        public void LogTelemetryExtensionCapturesTheExpectedTelemetryEvents4()
        {
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

            this.mockLogger.Object.LogTelemetry(this.exampleEventId, this.exampleEventContext, () => { });

            Assert.IsTrue(eventsLogged.Count == 2);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{this.exampleEventId.Name}Start"
                && evt.Item2 == LogLevel.Information
                && object.ReferenceEquals(this.exampleEventContext, evt.Item3)) == 1);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{this.exampleEventId.Name}Stop"
                && evt.Item2 == LogLevel.Information
                && object.ReferenceEquals(this.exampleEventContext, evt.Item3)) == 1);
        }

        [Test]
        public async Task LogTelemetryExtensionCapturesTheExpectedTelemetryEvents5()
        {
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

            Task task = Task.CompletedTask;

            await this.mockLogger.Object.LogTelemetryAsync(this.exampleEventName, this.exampleEventContext, () => { return task; })
                .ConfigureAwait(false);

            Assert.IsTrue(eventsLogged.Count == 2);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{this.exampleEventName}Start"
                && evt.Item2 == LogLevel.Information
                && object.ReferenceEquals(this.exampleEventContext, evt.Item3)) == 1);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{this.exampleEventName}Stop"
                && evt.Item2 == LogLevel.Information
                && object.ReferenceEquals(this.exampleEventContext, evt.Item3)) == 1);
        }

        [Test]
        public async Task LogTelemetryExtensionCapturesTheExpectedTelemetryEvents6()
        {
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

            Task task = Task.CompletedTask;

            await this.mockLogger.Object.LogTelemetryAsync(this.exampleEventId, this.exampleEventContext, () => { return task; })
                .ConfigureAwait(false);

            Assert.IsTrue(eventsLogged.Count == 2);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{this.exampleEventId.Name}Start"
                && evt.Item2 == LogLevel.Information
                && object.ReferenceEquals(this.exampleEventContext, evt.Item3)) == 1);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{this.exampleEventId.Name}Stop"
                && evt.Item2 == LogLevel.Information
                && object.ReferenceEquals(this.exampleEventContext, evt.Item3)) == 1);
        }

        [Test]
        public async Task LogTelemetryExtensionCapturesTheExpectedTelemetryEvents7()
        {
            // When a name is not defined, the ID will be used.
            this.exampleEventId = new EventId(123456);
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
                    // The name will be set to {ID}Start/{ID}Stop
                    eventsLogged.Add(new Tuple<string, LogLevel, EventContext>(eventId.Name, level, context));
                });

            Task task = Task.CompletedTask;

            await this.mockLogger.Object.LogTelemetryAsync(this.exampleEventId, this.exampleEventContext, () => { return task; })
                .ConfigureAwait(false);

            Assert.IsTrue(eventsLogged.Count == 2);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{this.exampleEventId.Id}Start"
                && evt.Item2 == LogLevel.Information
                && object.ReferenceEquals(this.exampleEventContext, evt.Item3)) == 1);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{this.exampleEventId.Id}Stop"
                && evt.Item2 == LogLevel.Information
                && object.ReferenceEquals(this.exampleEventContext, evt.Item3)) == 1);
        }

        [Test]
        public void LogTelemetryExtensionCapturesTheExpectedTelemetryEventsWhenErrorsOccurDuringTheExecutionOfTheBodyLogic()
        {
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
                this.mockLogger.Object.LogTelemetry(this.exampleEventName, this.exampleEventContext, () => { throw new Exception(); });
            }
            catch
            {
            }

            Assert.IsTrue(eventsLogged.Count == 3);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{this.exampleEventName}Start"
                && evt.Item2 == LogLevel.Information
                && object.ReferenceEquals(this.exampleEventContext, evt.Item3)) == 1);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{this.exampleEventName}Error"
                && evt.Item2 == LogLevel.Error
                && object.ReferenceEquals(this.exampleEventContext, evt.Item3)) == 1);

            Assert.IsTrue(eventsLogged.Count(evt =>
                evt.Item1 == $"{this.exampleEventName}Stop"
                && evt.Item2 == LogLevel.Information
                && object.ReferenceEquals(this.exampleEventContext, evt.Item3)) == 1);
        }

        [Test]
        public void LogTelemetryExtensionAlwaysLogsTheVeryFirstEventWhenSampling1()
        {
            SamplingOptions samplingOptions = new SamplingOptions
            {
                SamplingRate = 2
            };

            this.mockLogger.Object.LogTelemetry(this.exampleEventName, this.exampleEventContext, () => { }, samplingOptions);

            this.mockLogger.Verify(logger => logger.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<EventContext>(),
                null,
                null));
        }

        [Test]
        public void LogTelemetryExtensionAlwaysLogsTheVeryFirstEventWhenSampling2()
        {
            Task task = Task.CompletedTask;
            SamplingOptions samplingOptions = new SamplingOptions
            {
                SamplingRate = 2
            };

            this.mockLogger.Object.LogTelemetryAsync(this.exampleEventName, this.exampleEventContext, () => { return task; }, samplingOptions);

            this.mockLogger.Verify(logger => logger.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<EventContext>(),
                null,
                null));
        }

        [Test]
        public void LogTelemetryExtensionLogsEventsAsExpectedWhenSamplingIsUsed1()
        {
            SamplingOptions samplingOptions = new SamplingOptions
            {
                SamplingRate = 2
            };

            int eventLogCount = 0;

            this.mockLogger
                .Setup(logger => logger.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<EventContext>(),
                    null,
                    null))
                .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, context, exc, formatter) =>
                {
                    eventLogCount++;
                });

            for (int i = 0; i < 10; i++)
            {
                this.mockLogger.Object.LogTelemetry(this.exampleEventName, this.exampleEventContext, () => { }, samplingOptions);
            }

            // 2 events are written on each sample (e.g. the *Start and *Stop events)
            Assert.IsTrue(eventLogCount == samplingOptions.EventCount / samplingOptions.SamplingRate * 2);
        }

        [Test]
        public void LogTelemetryExtensionLogsEventsAsExpectedWhenSamplingIsUsed2()
        {
            SamplingOptions samplingOptions = new SamplingOptions
            {
                SamplingRate = 2
            };

            int eventLogCount = 0;

            this.mockLogger
                .Setup(logger => logger.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<EventContext>(),
                    null,
                    null))
                .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, context, exc, formatter) =>
                {
                    eventLogCount++;
                });

            for (int i = 0; i < 10; i++)
            {
                this.mockLogger.Object.LogTelemetry(this.exampleEventName, this.exampleEventContext, () => { return 0; }, samplingOptions);
            }

            // 2 events are written on each sample (e.g. the *Start and *Stop events)
            Assert.IsTrue(eventLogCount == samplingOptions.EventCount / samplingOptions.SamplingRate * 2);
        }

        [Test]
        public async Task LogTelemetryAsyncExtensionLogsEventsAsExpectedWhenSamplingIsUsed1()
        {
            SamplingOptions samplingOptions = new SamplingOptions
            {
                SamplingRate = 2
            };

            int eventLogCount = 0;

            this.mockLogger
                .Setup(logger => logger.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<EventContext>(),
                    null,
                    null))
                .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, context, exc, formatter) =>
                {
                    eventLogCount++;
                });

            Task task = Task.CompletedTask;

            for (int i = 0; i < 10; i++)
            {
                await this.mockLogger.Object.LogTelemetryAsync(this.exampleEventName, this.exampleEventContext, () => { return task; }, samplingOptions)
                    .ConfigureAwait(false);
            }

            // 2 events are written on each sample (e.g. the *Start and *Stop events)
            Assert.IsTrue(eventLogCount == samplingOptions.EventCount / samplingOptions.SamplingRate * 2);
        }

        [Test]
        public async Task LogTelemetryAsyncExtensionLogsEventsAsExpectedWhenSamplingIsUsed2()
        {
            SamplingOptions samplingOptions = new SamplingOptions
            {
                SamplingRate = 2
            };

            int eventLogCount = 0;

            this.mockLogger
                .Setup(logger => logger.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<EventContext>(),
                    null,
                    null))
                .Callback<LogLevel, EventId, EventContext, Exception, Func<EventContext, Exception, string>>((level, eventId, context, exc, formatter) =>
                {
                    eventLogCount++;
                });

            Task<int> task = Task.FromResult(1);

            for (int i = 0; i < 10; i++)
            {
                await this.mockLogger.Object.LogTelemetryAsync(this.exampleEventName, this.exampleEventContext, () => { return task; }, samplingOptions)
                    .ConfigureAwait(false);
            }

            // 2 events are written on each sample (e.g. the *Start and *Stop events)
            Assert.IsTrue(eventLogCount == samplingOptions.EventCount / samplingOptions.SamplingRate * 2);
        }
    }
}
