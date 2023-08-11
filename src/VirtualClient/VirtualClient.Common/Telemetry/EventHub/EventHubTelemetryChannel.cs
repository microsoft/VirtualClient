// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Telemetry
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Azure.Messaging.EventHubs;
    using global::Azure.Messaging.EventHubs.Producer;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Buffers <see cref="EventData"/> items for efficient batched transmission.
    /// </summary>
    /// <remarks>
    /// Event Hubs Programming Overview
    /// https://docs.microsoft.com/en-us/dotnet/api/overview/azure/event-hubs?view=azure-dotnet
    /// </remarks>
    public class EventHubTelemetryChannel : IEnumerable<EventData>, IFlushableChannel, IDisposable
    {
        // Each EventDataBatch has a limit of one megabyte, regardless of the number of
        // EventData objects within. An offset of 16 bytes seems to allow for the size of
        // an EventData object with its contents being the remainder of the megabyte.
        // This is set to 700KB for the context data, to allow for some significant buffer
        // for other remaining properties.
        internal const int MaxEventDataBytes = 700000;

        private const int DefaultMaxCapacity = 1000000;
        private const int DefaultMinCapacity = 1001;

        private readonly object transmissionLock = new object();
        private readonly object bufferLock = new object();
        private int maxCapacity;
        private int minCapacity;
        private AutoResetEvent autoFlushWaitHandle;
        private CancellationTokenSource cancellationTokenSource;
        private Task transmissionAutoSendTask;
        private SendEventOptions sendEventOptions;

        /// <summary>
        /// Initialized a new instance of the <see cref="EventHubTelemetryChannel"/> class.
        /// </summary>
        /// <param name="client">The client to use for publishing telemetry to the Event Hub.</param>
        /// <param name="sendEventOptions">Options to use when publishing batches of telemetry events.</param>
        /// <param name="enableDiagnostics">True/false whether event transmission diagnostics/debugging is enabled.</param>
        public EventHubTelemetryChannel(EventHubProducerClient client, SendEventOptions sendEventOptions = null, bool enableDiagnostics = false)
        {
            client.ThrowIfNull(nameof(client));

            this.Client = client;
            this.Buffer = new Queue<EventData>();

            this.minCapacity = EventHubTelemetryChannel.DefaultMinCapacity;
            this.maxCapacity = EventHubTelemetryChannel.DefaultMaxCapacity;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.autoFlushWaitHandle = new AutoResetEvent(false);
            this.sendEventOptions = sendEventOptions;
            this.DiagnosticsEnabled = enableDiagnostics;

            if (enableDiagnostics)
            {
                this.Diagnostics = new ChannelDiagnostics();
            }
        }

        /// <summary>
        /// Event is invoked whenever a set of events is dropped due to capacity
        /// settings being exceeded.
        /// </summary>
        public event EventHandler<EventHubChannelEventArgs> EventsDropped;

        /// <summary>
        /// Event is invoked whenever a set of events are successfully transmitted
        /// to EventHub.
        /// </summary>
        public event EventHandler<EventHubChannelEventArgs> EventsTransmitted;

        /// <summary>
        /// Event is invoked whenever the transmission of a set of events to Event Hub
        /// fails. Note that the events remain buffered in this scenario and will be resent on
        /// subsequent retries.
        /// </summary>
        public event EventHandler<EventHubChannelEventArgs> EventTransmissionError;

        /// <summary>
        /// Gets the count of the events in the telemetry channel buffer.
        /// </summary>
        public int BufferCount
        {
            get
            {
                return this.Buffer.Count;
            }
        }

        /// <summary>
        /// Gets true/false whether channel diagnostics is enabled.
        /// </summary>
        public bool DiagnosticsEnabled { get; }

        /// <summary>
        /// Gets or sets the maximum number of telemetry items that can be in the buffer to send. Items will be dropped
        /// once this limit is hit.
        /// </summary>
        public int MaxCapacity
        {
            get
            {
                return this.maxCapacity;
            }

            set
            {
                if (value < this.minCapacity)
                {
                    this.maxCapacity = this.minCapacity;
                    return;
                }

                this.maxCapacity = value;
            }
        }

        /// <summary>
        /// Gets or sets the intervel for which VC autoflushes telemetry.
        /// Making the interval too short will stress eventhub and create throttle.
        /// </summary>
        public TimeSpan AutoFlushInterval { get; set; } = TimeSpan.FromMilliseconds(5000);

        /// <summary>
        /// Gets the channel diagnostics instance if enabled.
        /// </summary>
        internal ChannelDiagnostics Diagnostics { get; }

        /// <summary>
        /// Gets the telemetry buffer in which events/items are contained.
        /// </summary>
        protected Queue<EventData> Buffer { get; private set; }

        /// <summary>
        /// The client to use for publishing events to the Event Hub.
        /// </summary>
        protected EventHubProducerClient Client { get; }

        /// <summary>
        /// Add a telemetry item to the buffer.
        /// </summary>
        /// <param name="item">A telemetry item.</param>
        public void Add(EventData item)
        {
            this.AddToBuffer(item);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Flushes the in-memory buffer and sends it.
        /// </summary>
        public void Flush(TimeSpan? timeout = null)
        {
            DateTime flushTimeout = DateTime.Now.Add(timeout ?? TimeSpan.FromSeconds(60));

            while (this.Buffer.Count > 0)
            {
                this.TransmitEvents();
                if (DateTime.Now >= flushTimeout)
                {
                    break;
                }

                Thread.Sleep(10);
            }
        }

        /// <summary>
        /// Get the <see cref="IEnumerator"/> for the <see cref="EventData"/>.
        /// </summary>
        /// <returns><see cref="IEnumerator"/> for the <see cref="EventData"/>.</returns>
        public IEnumerator<EventData> GetEnumerator()
        {
            return this.Buffer.GetEnumerator();
        }

        /// <summary>
        /// Get the <see cref="IEnumerator"/> for the <see cref="EventData"/>.
        /// </summary>
        /// <returns><see cref="IEnumerator"/> for the <see cref="EventData"/>.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Add a telemetry item to the buffer.
        /// </summary>
        /// <param name="item">A telemetry item.</param>
        protected virtual void AddToBuffer(EventData item)
        {
            if (item != null)
            {
                lock (this.bufferLock)
                {
                    if (this.transmissionAutoSendTask == null)
                    {
                        lock (this.transmissionLock)
                        {
                            if (this.transmissionAutoSendTask == null)
                            {
                                this.transmissionAutoSendTask = this.StartEventTransmissionBackgroundTask();
                            }
                        }
                    }

                    if (this.Buffer.Count >= this.MaxCapacity || item.Body.Length >= EventHubTelemetryChannel.MaxEventDataBytes)
                    {
                        this.Diagnostics?.EventsDropped(1);
                        this.OnEventsDropped(new List<EventData> { item });

                        return;
                    }

                    this.Buffer.Enqueue(item);
                    this.Diagnostics?.EventsExpected(1);
                }
            }
        }

        /// <summary>
        /// Invokes the 'EventsDropped' event.
        /// </summary>
        protected void OnEventsDropped(IEnumerable<EventData> events)
        {
            this.EventsDropped?.Invoke(this, new EventHubChannelEventArgs(events));
        }

        /// <summary>
        /// Invokes the 'EventsTransmitted' event.
        /// </summary>
        protected void OnEventsTransmitted(IEnumerable<EventData> events)
        {
            this.EventsTransmitted?.Invoke(this, new EventHubChannelEventArgs(events));
        }

        /// <summary>
        /// Invokes the 'EventTransmissionError' event.
        /// </summary>
        protected void OnEventTransmissionError(IEnumerable<EventData> events, Exception error)
        {
            this.EventTransmissionError?.Invoke(this, new EventHubChannelEventArgs(events, error));
        }

        /// <summary>
        /// Disposes of resources used by the channel.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Call Set to prevent waiting for the next interval in the runner.
                try
                {
                    this.cancellationTokenSource.Cancel();
                    this.autoFlushWaitHandle.Set();
                    this.transmissionAutoSendTask?.GetAwaiter().GetResult();

                    this.cancellationTokenSource.Dispose();
                    this.autoFlushWaitHandle.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // We need to try catch the Set call in case the auto-reset event wait interval occurs between setting enabled
                    // to false and the call to Set then the auto-reset event will have already been disposed by the runner thread.
                }
            }
        }

        /// <summary>
        /// Transmits the batch to the target EventHub endpoint.
        /// </summary>
        /// <param name="eventDataBatch"></param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        protected virtual Task TransmitBatchAsync(IEnumerable<EventData> eventDataBatch)
        {
            // Context on CancellationToken.None Here:
            // We purposefully DO NOT honor the channel CancellationToken here. We do not want the
            // transmission logic to exit on cancellation but to keep trying to get the telemetry through.
            // We prefer a delayed exit of the application to losing telemetry.
            return this.Client.SendAsync(eventDataBatch, this.sendEventOptions, CancellationToken.None);
        }

        /// <summary>
        /// Transmits the events in the buffer to the target EventHub endpoint.
        /// </summary>
        private void TransmitEvents()
        {
            if (this.Buffer.Count > 0)
            {
                lock (this.transmissionLock)
                {
                    if (this.Buffer.Count > 0)
                    {
                        List<EventData> currentBatch = new List<EventData>();

                        try
                        {
                            while (this.Buffer.Count > 0)
                            {
                                lock (this.bufferLock)
                                {
                                    int currentBatchSize = 0;
                                    for (int currentEventIndex = 0; currentEventIndex < this.Buffer.Count; currentEventIndex++)
                                    {
                                        EventData nextEventItem = this.Buffer.Dequeue();
                                        this.Diagnostics?.EventsTransmitted(1);

                                        if (nextEventItem != null)
                                        {
                                            currentBatch.Add(nextEventItem);
                                            currentBatchSize += nextEventItem.Body.Length;

                                            if (currentBatchSize > EventHubTelemetryChannel.MaxEventDataBytes)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }

                                this.TransmitBatchAsync(currentBatch).GetAwaiter().GetResult();
                                this.OnEventsTransmitted(currentBatch);
                                currentBatch.Clear();
                            }
                        }
                        catch (Exception exc)
                        {
                            if (currentBatch.Any())
                            {
                                lock (this.bufferLock)
                                {
                                    // If we failed to transmit the events, we need to add them back to the
                                    // buffer so that we can retry transmission on the next attempt.
                                    this.Diagnostics?.EventsTransmissionFailed(currentBatch.Count);
                                    currentBatch.ForEach(eventItem =>
                                    {
                                        this.Buffer.Enqueue(eventItem);
                                    });
                                }
                            }

                            // Telemetry transmission is a best-effort process.  We do not want to crash
                            // the hosting process on failures to send telemetry events.  Additionally, the
                            // reliable buffer will retry on subsequent attempts.
                            this.OnEventTransmissionError(currentBatch, exc);
                        }
                    }
                }
            }
        }

        private Task StartEventTransmissionBackgroundTask()
        {
            return Task.Factory.StartNew(this.TransmitEventsInTheBackground, this.cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                .ContinueWith(
                    task =>
                    {
                        string msg = $"{typeof(EventHubTelemetryChannel)}: Unhandled exception in transmission channel: {task.Exception.Message}";
                    },
                    TaskContinuationOptions.OnlyOnFaulted);
        }

        private void TransmitEventsInTheBackground()
        {
            while (!this.cancellationTokenSource.IsCancellationRequested)
            {
                // Waiting for the flush delay to elapse
                this.autoFlushWaitHandle.WaitOne(this.AutoFlushInterval);

                // Pulling all items from the buffer and sending as one transmission.
                this.TransmitEvents();
            }
        }

        internal class ChannelDiagnostics
        {
            private long eventsDroppedCount;
            private long eventsExpectedCount;
            private long eventsTransmittedCount;
            private long eventsTransmissionFailureCount;

            public long EventsDropped(long? count = null)
            {
                if (count != null)
                {
                    Interlocked.Exchange(ref this.eventsDroppedCount, this.eventsDroppedCount + count.Value);
                }

                return this.eventsDroppedCount;
            }

            public long EventsExpected(long? count = null)
            {
                if (count != null)
                {
                    Interlocked.Exchange(ref this.eventsExpectedCount, this.eventsExpectedCount + count.Value);
                }

                return this.eventsExpectedCount;
            }

            public long EventsTransmitted(long? count = null)
            {
                if (count != null)
                {
                    Interlocked.Exchange(ref this.eventsTransmittedCount, this.eventsTransmittedCount + count.Value);
                }

                return this.eventsTransmittedCount;
            }

            public long EventsTransmissionFailed(long? count = null)
            {
                if (count != null)
                {
                    Interlocked.Exchange(ref this.eventsTransmissionFailureCount, this.eventsTransmissionFailureCount + count.Value);
                }

                return this.eventsTransmissionFailureCount;
            }
        }
    }
}