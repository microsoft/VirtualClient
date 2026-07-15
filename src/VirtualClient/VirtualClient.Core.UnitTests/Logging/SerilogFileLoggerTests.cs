// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System.Collections.Generic;
    using global::Serilog;
    using global::Serilog.Core;
    using global::Serilog.Events;
    using Microsoft.Extensions.Logging;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class SerilogFileLoggerTests : MockFixture
    {
        [Test]
        public void SerilogFileLoggerSupportsStandardLoggerMessages()
        {
            InMemoryLogEventSink sink = new InMemoryLogEventSink();
            using (Logger serilogLogger = new LoggerConfiguration().WriteTo.Sink(sink).CreateLogger())
            {
                SerilogFileLogger logger = new SerilogFileLogger(serilogLogger, LogLevel.Information);

                Assert.DoesNotThrow(() => logger.LogInformation("Status Code: 200, GET /api/heartbeat"));
                Assert.AreEqual(1, sink.Events.Count);
                Assert.AreEqual("Status Code: 200, GET /api/heartbeat", sink.Events[0].Properties["message"].ToString().Trim('"'));
            }
        }

        private class InMemoryLogEventSink : ILogEventSink
        {
            public IList<LogEvent> Events { get; } = new List<LogEvent>();

            public void Emit(LogEvent logEvent)
            {
                this.Events.Add(logEvent);
            }
        }
    }
}
