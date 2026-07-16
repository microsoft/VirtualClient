// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Logging
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using global::Azure.Messaging.EventHubs;
    using global::Azure.Messaging.EventHubs.Producer;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;

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
        private const long DefaultMaxBufferSizeBytes = 268435456;
        private const int DefaultMinCapacity = 1001;

        private readonly object transmissionLock = new object();
        private readonly object bufferLock = new object();
        private int maxCapacity;
        private long maxBufferSizeBytes;
        private long bufferSizeBytes;
        private int minCapacity;
        private AutoResetEvent autoFlushWaitHandle;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken transmissionCancellationToken;
        private Task transmissionAutoSendTask;
        private SendEventOptions sendEventOptions;

        /// <summary>
        /// Initialized a new instance of the <see cref="EventHubTelemetryChannel"/> class.
        /// </summary>
        /// <param name="client">The AMQP client to use for publishing telemetry to the Event Hub.</param>
        /// <param name="sendEventOptions">Options to use when publishing batches of telemetry events.</param>
        /// <param name="enableDiagnostics">True/false whether event transmission diagnostics/debugging is enabled.</param>
        public EventHubTelemetryChannel(EventHubProducerClient client, SendEventOptions sendEventOptions = null, bool enableDiagnostics = false)
            : this(enableDiagnostics)
        {
            client.ThrowIfNull(nameof(client));
            this.AmqpClient = client;
            this.sendEventOptions = sendEventOptions;
        }

        /// <summary>
        /// Initialized a new instance of the <see cref="EventHubTelemetryChannel"/> class.
        /// </summary>
        /// <param name="client">The basic HTTP/REST client to use for publishing telemetry to the Event Hub.</param>
        /// <param name="enableDiagnostics">True/false whether event transmission diagnostics/debugging is enabled.</param>
        public EventHubTelemetryChannel(HttpClient client, bool enableDiagnostics = false)
            : this(enableDiagnostics)
        {
            client.ThrowIfNull(nameof(client));
            this.RestClient = client;
        }

        private EventHubTelemetryChannel(bool enableDiagnostics = false)
        {
            this.Buffer = new Queue<EventData>();

            this.minCapacity = EventHubTelemetryChannel.DefaultMinCapacity;
            this.maxCapacity = EventHubTelemetryChannel.DefaultMaxCapacity;
            this.maxBufferSizeBytes = EventHubTelemetryChannel.DefaultMaxBufferSizeBytes;
            this.cancellationTokenSource = new CancellationTokenSource();
            this.autoFlushWaitHandle = new AutoResetEvent(false);
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
                lock (this.bufferLock)
                {
                    return this.Buffer.Count;
                }
            }
        }

        /// <summary>
        /// Gets the size in bytes of the events in the telemetry channel buffer.
        /// </summary>
        public long BufferSizeBytes
        {
            get
            {
                lock (this.bufferLock)
                {
                    return this.bufferSizeBytes;
                }
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
        /// Gets or sets the maximum number of bytes that can be buffered for transmission.
        /// </summary>
        public long MaxBufferSizeBytes
        {
            get
            {
                return this.maxBufferSizeBytes;
            }

            set
            {
                this.maxBufferSizeBytes = value > 0
                    ? value
                    : EventHubTelemetryChannel.DefaultMaxBufferSizeBytes;
            }
        }

        /// <summary>
        /// The client to use for publishing events to the Event Hub.
        /// </summary>
        public EventHubProducerClient AmqpClient { get; }

        /// <summary>
        /// Gets or sets the intervel for which VC autoflushes telemetry.
        /// Making the interval too short will stress eventhub and create throttle.
        /// </summary>
        public TimeSpan AutoFlushInterval { get; set; } = TimeSpan.FromMilliseconds(5000);

        /// <summary>
        /// Gets or sets the maximum amount of time allowed for a single transmission.
        /// </summary>
        public TimeSpan TransmissionTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// The client to use for publishing events to the Event Hub.
        /// </summary>
        public HttpClient RestClient { get; }

        /// <summary>
        /// Gets the channel diagnostics instance if enabled.
        /// </summary>
        internal ChannelDiagnostics Diagnostics { get; }

        /// <summary>
        /// Gets the telemetry buffer in which events/items are contained.
        /// </summary>
        protected Queue<EventData> Buffer { get; private set; }

        /// <summary>
        /// Gets the cancellation token for the current transmission.
        /// </summary>
        protected CancellationToken TransmissionCancellationToken
        {
            get
            {
                return this.transmissionCancellationToken;
            }
        }

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

            while (this.BufferCount > 0)
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

                    if (this.Buffer.Count >= this.MaxCapacity
                        || this.bufferSizeBytes + item.Body.Length > this.MaxBufferSizeBytes
                        || item.Body.Length >= EventHubTelemetryChannel.MaxEventDataBytes)
                    {
                        this.Diagnostics?.EventsDropped(1);
                        this.OnEventsDropped(new List<EventData> { item });

                        return;
                    }

                    this.Buffer.Enqueue(item);
                    this.bufferSizeBytes += item.Body.Length;
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
        protected virtual async Task TransmitBatchAsync(IEnumerable<EventData> eventDataBatch)
        {
            if (eventDataBatch?.Any() == true)
            {
                if (this.AmqpClient != null)
                {
                    await this.AmqpClient.SendAsync(eventDataBatch, this.sendEventOptions, this.TransmissionCancellationToken);
                }
                else
                {
                    foreach (EventData eventData in eventDataBatch)
                    {
                        string eventBody = Encoding.UTF8.GetString(eventData.Body.ToArray());
                        using (var request = new HttpRequestMessage(HttpMethod.Post, this.RestClient.BaseAddress))
                        {
                            using (HttpContent content = new StringContent(eventBody, Encoding.UTF8, "application/json"))
                            {
                                request.Content = content;

                                using HttpResponseMessage response = await this.RestClient.SendAsync(request, this.TransmissionCancellationToken);
                                response.EnsureSuccessStatusCode();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Transmits the events in the buffer to the target EventHub endpoint.
        /// </summary>
        private void TransmitEvents()
        {
            if (this.BufferCount > 0)
            {
                lock (this.transmissionLock)
                {
                    // AMQP transmissions can support batching, so we will allow the batch size to be determined by the number of events in the buffer up to the max event
                    // data bytes allowed by Event Hub. Event Hub transmissions with HTTP use the REST client and do not support batching, so we need to set the batch size
                    // to 1 in that case.
                    int? maxBatchSize = this.RestClient != null ? 1 : null;
                    this.TransmitEvents(maxBatchSize);
                }
            }
        }

        /// <summary>
        /// Transmits the events in the buffer to the target EventHub endpoint.
        /// </summary>
        private void TransmitEvents(int? maxBatchSize = null)
        {
            if (this.BufferCount > 0)
            {
                List<EventData> currentBatch = new List<EventData>();

                try
                {
                    while (this.BufferCount > 0)
                    {
                        lock (this.bufferLock)
                        {
                            int currentBatchSize = 0;
                            int batchSize = maxBatchSize ?? this.Buffer.Count;
                            for (int currentEventIndex = 0; currentEventIndex < batchSize; currentEventIndex++)
                            {
                                EventData nextEventItem = this.Buffer.Dequeue();
                                this.bufferSizeBytes -= nextEventItem.Body.Length;

                                if (nextEventItem != null)
                                {
                                    currentBatch.Add(nextEventItem);
                                    currentBatchSize += nextEventItem.Body.Length;

                                    if (currentBatchSize > EventHubTelemetryChannel.MaxEventDataBytes || maxBatchSize == 1)
                                    {
                                        break;
                                    }
                                }
                            }
                        }

                        using (CancellationTokenSource transmissionTimeoutSource = new CancellationTokenSource(this.TransmissionTimeout))
                        {
                            try
                            {
                                this.transmissionCancellationToken = transmissionTimeoutSource.Token;
                                this.TransmitBatchAsync(currentBatch).GetAwaiter().GetResult();
                            }
                            finally
                            {
                                this.transmissionCancellationToken = CancellationToken.None;
                            }
                        }

                        this.Diagnostics?.EventsTransmitted(currentBatch.Count);
                        this.OnEventsTransmitted(currentBatch);
                        currentBatch.Clear();
                    }
                }
                catch (Exception exc)
                {
                    if (currentBatch.Any())
                    {
                        List<EventData> droppedEvents = new List<EventData>();
                        lock (this.bufferLock)
                        {
                            // If we failed to transmit the events, we need to add them back to the
                            // buffer so that we can retry transmission on the next attempt.
                            this.Diagnostics?.EventsTransmissionFailed(currentBatch.Count);
                            currentBatch.ForEach(eventItem =>
                            {
                                if (this.Buffer.Count < this.MaxCapacity
                                    && this.bufferSizeBytes + eventItem.Body.Length <= this.MaxBufferSizeBytes)
                                {
                                    this.Buffer.Enqueue(eventItem);
                                    this.bufferSizeBytes += eventItem.Body.Length;
                                }
                                else
                                {
                                    this.Diagnostics?.EventsDropped(1);
                                    droppedEvents.Add(eventItem);
                                }
                            });
                        }

                        if (droppedEvents.Any())
                        {
                            this.OnEventsDropped(droppedEvents);
                        }
                    }

                    // Telemetry transmission is a best-effort process.  We do not want to crash
                    // the hosting process on failures to send telemetry events.  Additionally, the
                    // reliable buffer will retry on subsequent attempts.
                    this.OnEventTransmissionError(currentBatch, exc);
                }
            }
        }

        private Task StartEventTransmissionBackgroundTask()
        {
            return Task.Run(async () =>
            {
                try
                {
                    await Task.Factory.StartNew(this.TransmitEventsInTheBackground, this.cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default)
                        .ContinueWith(
                            task =>
                            {
                                string msg = $"{typeof(EventHubTelemetryChannel)}: Unhandled exception in transmission channel: {task.Exception.Message}";
                            },
                            TaskContinuationOptions.OnlyOnFaulted);
                }
                catch (OperationCanceledException)
                {
                    // Expected when a Cancellation is requested.
                }
            });
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
                    Interlocked.Add(ref this.eventsDroppedCount, count.Value);
                }

                return Interlocked.Read(ref this.eventsDroppedCount);
            }

            public long EventsExpected(long? count = null)
            {
                if (count != null)
                {
                    Interlocked.Add(ref this.eventsExpectedCount, count.Value);
                }

                return Interlocked.Read(ref this.eventsExpectedCount);
            }

            public long EventsTransmitted(long? count = null)
            {
                if (count != null)
                {
                    Interlocked.Add(ref this.eventsTransmittedCount, count.Value);
                }

                return Interlocked.Read(ref this.eventsTransmittedCount);
            }

            public long EventsTransmissionFailed(long? count = null)
            {
                if (count != null)
                {
                    Interlocked.Add(ref this.eventsTransmissionFailureCount, count.Value);
                }

                return Interlocked.Read(ref this.eventsTransmissionFailureCount);
            }
        }
    }
}