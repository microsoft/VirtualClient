// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class EventHubTelemetryChannelTests
    {
        [Test]
        public void EventHubTelemetryChannelLimitsTheBufferByBytes()
        {
            using (TestEventHubTelemetryChannel channel = new TestEventHubTelemetryChannel())
            {
                channel.AutoFlushInterval = TimeSpan.FromHours(1);
                channel.MaxBufferSizeBytes = 10;

                channel.Add(new EventData(new byte[6]));
                channel.Add(new EventData(new byte[6]));

                Assert.AreEqual(1, channel.BufferCount);
                Assert.AreEqual(6, channel.BufferSizeBytes);
                Assert.AreEqual(1, channel.Diagnostics.EventsDropped());
            }
        }

        [Test]
        public void EventHubTelemetryChannelTimesOutAndRequeuesFailedTransmissions()
        {
            using (TestEventHubTelemetryChannel channel = new TestEventHubTelemetryChannel())
            {
                channel.AutoFlushInterval = TimeSpan.FromHours(1);
                channel.TransmissionTimeout = TimeSpan.FromMilliseconds(25);
                channel.TransmissionBehavior = (events, cancellationToken) =>
                    Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);

                channel.Add(new EventData(new byte[10]));
                channel.Flush(TimeSpan.FromMilliseconds(50));

                Assert.AreEqual(1, channel.BufferCount);
                Assert.AreEqual(10, channel.BufferSizeBytes);
                Assert.GreaterOrEqual(channel.Diagnostics.EventsTransmissionFailed(), 1);
                Assert.AreEqual(0, channel.Diagnostics.EventsTransmitted());
            }
        }

        [Test]
        public void EventHubTelemetryChannelCountsEventsAsTransmittedAfterTheSendCompletes()
        {
            using (TestEventHubTelemetryChannel channel = new TestEventHubTelemetryChannel())
            {
                channel.AutoFlushInterval = TimeSpan.FromHours(1);

                channel.Add(new EventData(new byte[10]));
                channel.Flush(TimeSpan.FromSeconds(1));

                Assert.AreEqual(0, channel.BufferCount);
                Assert.AreEqual(0, channel.BufferSizeBytes);
                Assert.AreEqual(1, channel.Diagnostics.EventsTransmitted());
            }
        }

        [Test]
        public async Task EventHubTelemetryChannelMaintainsTheByteLimitWhileATransmissionIsBlocked()
        {
            using (TestEventHubTelemetryChannel channel = new TestEventHubTelemetryChannel())
            {
                TaskCompletionSource transmissionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                channel.AutoFlushInterval = TimeSpan.FromHours(1);
                channel.MaxBufferSizeBytes = 20;
                channel.TransmissionTimeout = TimeSpan.FromMilliseconds(100);
                channel.TransmissionBehavior = (events, cancellationToken) =>
                {
                    transmissionStarted.TrySetResult();
                    return Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                };

                channel.Add(new EventData(new byte[10]));
                Task flushTask = Task.Run(() => channel.Flush(TimeSpan.FromMilliseconds(150)));

                await transmissionStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
                channel.Add(new EventData(new byte[10]));
                channel.Add(new EventData(new byte[10]));
                await flushTask;

                Assert.AreEqual(2, channel.BufferCount);
                Assert.AreEqual(20, channel.BufferSizeBytes);
                Assert.GreaterOrEqual(channel.Diagnostics.EventsTransmissionFailed(), 1);
                Assert.GreaterOrEqual(channel.Diagnostics.EventsDropped(), 1);
                Assert.AreEqual(0, channel.Diagnostics.EventsTransmitted());
            }
        }

        private class TestEventHubTelemetryChannel : EventHubTelemetryChannel
        {
            public TestEventHubTelemetryChannel()
                : base(new HttpClient
                {
                    BaseAddress = new Uri("https://localhost")
                }, enableDiagnostics: true)
            {
            }

            public Func<IEnumerable<EventData>, CancellationToken, Task> TransmissionBehavior { get; set; } =
                (events, cancellationToken) => Task.CompletedTask;

            protected override Task TransmitBatchAsync(IEnumerable<EventData> eventDataBatch)
            {
                return this.TransmissionBehavior.Invoke(eventDataBatch, this.TransmissionCancellationToken);
            }
        }
    }
}
