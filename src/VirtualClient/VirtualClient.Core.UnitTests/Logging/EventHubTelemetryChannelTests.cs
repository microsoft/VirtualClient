// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Messaging.EventHubs;
    using Azure.Messaging.EventHubs.Producer;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class EventHubTelemetryChannelTests
    {
        [Test]
        public void EventHubTelemetryChannelLimitsTheBufferByBytes()
        {
            using (TestAmqpEventHubTelemetryChannel channel = new TestAmqpEventHubTelemetryChannel())
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
        public void EventHubTelemetryChannelRequeuesFailedTransmissions()
        {
            using (TestAmqpEventHubTelemetryChannel channel = new TestAmqpEventHubTelemetryChannel())
            {
                int transmissionAttempts = 0;
                channel.AutoFlushInterval = TimeSpan.FromHours(1);
                channel.TransmissionBehavior = events =>
                {
                    if (Interlocked.Increment(ref transmissionAttempts) == 1)
                    {
                        throw new InvalidOperationException("Expected test failure.");
                    }

                    return Task.CompletedTask;
                };

                channel.Add(new EventData(new byte[10]));
                channel.Flush(TimeSpan.FromSeconds(1));

                Assert.AreEqual(0, channel.BufferCount);
                Assert.AreEqual(0, channel.BufferSizeBytes);
                Assert.AreEqual(1, channel.Diagnostics.EventsTransmissionFailed());
                Assert.AreEqual(1, channel.Diagnostics.EventsTransmitted());
            }
        }

        [Test]
        public void EventHubTelemetryChannelCountsEventsAsTransmittedAfterTheSendCompletes()
        {
            using (TestAmqpEventHubTelemetryChannel channel = new TestAmqpEventHubTelemetryChannel())
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
        public void EventHubTelemetryChannelDoesNotAddAnEventThatExceedsTheBatchByteLimit()
        {
            using (TestAmqpEventHubTelemetryChannel channel = new TestAmqpEventHubTelemetryChannel())
            {
                List<int> transmittedBatchSizes = new List<int>();
                channel.AutoFlushInterval = TimeSpan.FromHours(1);
                channel.TransmissionBehavior = events =>
                {
                    transmittedBatchSizes.Add(events.Sum(eventData => eventData.Body.Length));
                    return Task.CompletedTask;
                };

                channel.Add(new EventData(new byte[400000]));
                channel.Add(new EventData(new byte[400000]));
                channel.Flush(TimeSpan.FromSeconds(1));

                CollectionAssert.AreEqual(new[] { 400000, 400000 }, transmittedBatchSizes);
                Assert.AreEqual(2, channel.Diagnostics.EventsTransmitted());
                Assert.AreEqual(0, channel.Diagnostics.EventsTransmissionFailed());
            }
        }

        [Test]
        public async Task EventHubTelemetryChannelMaintainsTheByteLimitWhileATransmissionIsInProgress()
        {
            using (TestAmqpEventHubTelemetryChannel channel = new TestAmqpEventHubTelemetryChannel())
            {
                TaskCompletionSource transmissionStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                TaskCompletionSource releaseTransmission = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                channel.AutoFlushInterval = TimeSpan.FromHours(1);
                channel.MaxBufferSizeBytes = 20;
                channel.TransmissionBehavior = async events =>
                {
                    transmissionStarted.TrySetResult();
                    await releaseTransmission.Task;
                };

                channel.Add(new EventData(new byte[10]));
                Task flushTask = Task.Run(() => channel.Flush(TimeSpan.FromSeconds(1)));

                await transmissionStarted.Task.WaitAsync(TimeSpan.FromSeconds(1));
                channel.Add(new EventData(new byte[10]));
                channel.Add(new EventData(new byte[10]));
                channel.Add(new EventData(new byte[10]));
                releaseTransmission.SetResult();
                await flushTask;

                Assert.AreEqual(0, channel.BufferCount);
                Assert.AreEqual(0, channel.BufferSizeBytes);
                Assert.AreEqual(1, channel.Diagnostics.EventsDropped());
                Assert.AreEqual(3, channel.Diagnostics.EventsTransmitted());
            }
        }

        [Test]
        public void EventHubTelemetryChannelSendsRestEventsIndividually()
        {
            using (TestRestEventHubTelemetryChannel channel = new TestRestEventHubTelemetryChannel())
            {
                List<int> transmittedBatchCounts = new List<int>();
                channel.AutoFlushInterval = TimeSpan.FromHours(1);
                channel.TransmissionBehavior = events =>
                {
                    transmittedBatchCounts.Add(events.Count());
                    return Task.CompletedTask;
                };

                channel.Add(new EventData(new byte[10]));
                channel.Add(new EventData(new byte[10]));
                channel.Flush(TimeSpan.FromSeconds(1));

                CollectionAssert.AreEqual(new[] { 1, 1 }, transmittedBatchCounts);
            }
        }

        private class TestAmqpEventHubTelemetryChannel : EventHubTelemetryChannel
        {
            public TestAmqpEventHubTelemetryChannel()
                : base(new EventHubProducerClient(
                    "Endpoint=sb://anynamespace.servicebus.windows.net/;SharedAccessKeyName=AnyAccessPolicy;SharedAccessKey=AnYacCEssKey=",
                    "any-hub"),
                    enableDiagnostics: true)
            {
            }

            public Func<IEnumerable<EventData>, Task> TransmissionBehavior { get; set; } =
                events => Task.CompletedTask;

            protected override Task TransmitBatchAsync(IEnumerable<EventData> eventDataBatch)
            {
                return this.TransmissionBehavior.Invoke(eventDataBatch);
            }
        }

        private class TestRestEventHubTelemetryChannel : EventHubTelemetryChannel
        {
            public TestRestEventHubTelemetryChannel()
                : base(new HttpClient
                {
                    BaseAddress = new Uri("https://localhost")
                }, enableDiagnostics: true)
            {
            }

            public Func<IEnumerable<EventData>, Task> TransmissionBehavior { get; set; } =
                events => Task.CompletedTask;

            protected override Task TransmitBatchAsync(IEnumerable<EventData> eventDataBatch)
            {
                return this.TransmissionBehavior.Invoke(eventDataBatch);
            }
        }
    }
}
