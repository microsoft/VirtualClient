// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Proxy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts.Proxy;

    /// <summary>
    /// Provides a channel for the upload of telemetry messages/events to a proxy API endpoint.
    /// </summary>
    public class ProxyTelemetryChannel : IEnumerable<ProxyTelemetryMessage>, IFlushableChannel, IDisposable
    {
        /// <summary>
        /// The default interval at which buffered events will be transmitted.
        /// </summary>
        public static readonly TimeSpan DefaultAutoFlushSendInterval = TimeSpan.FromSeconds(30);

        private const int DefaultMaxCapacity = 100000;
        private const int DefaultMessageBatchSize = 30;

        private readonly object bufferLock = new object();
        private readonly object messageTransmissionLock = new object();

        private AutoResetEvent autoFlushWaitHandle;
        private CancellationTokenSource cancellationTokenSource;
        private Task messageTransmissionTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyLogger"/> class.
        /// </summary>
        /// <param name="apiClient">The API client for interacting with the proxy endpoint.</param>
        public ProxyTelemetryChannel(IProxyApiClient apiClient)
        {
            apiClient.ThrowIfNull(nameof(apiClient));

            this.ApiClient = apiClient;
            this.MaxCapacity = ProxyTelemetryChannel.DefaultMaxCapacity;
            this.MessageBatchSize = ProxyTelemetryChannel.DefaultMessageBatchSize;
            this.Buffer = new Queue<ProxyTelemetryMessage>();
            this.TransmissionFailureWaitTime = TimeSpan.FromSeconds(3);
            this.cancellationTokenSource = new CancellationTokenSource();
            this.autoFlushWaitHandle = new AutoResetEvent(false);
        }

        /// <summary>
        /// Event is invoked whenever a set of messages is dropped during flush operations
        /// because of a timeout.
        /// </summary>
        public event EventHandler<ProxyChannelEventArgs> FlushMessages;

        /// <summary>
        /// Event is invoked whenever a set of messages is dropped due to capacity
        /// settings being exceeded.
        /// </summary>
        public event EventHandler<ProxyChannelEventArgs> MessagesDropped;

        /// <summary>
        /// Event is invoked whenever a set of events are successfully transmitted
        /// to EventHub.
        /// </summary>
        public event EventHandler<ProxyChannelEventArgs> MessagesTransmitted;

        /// <summary>
        /// Event is invoked whenever the transmission of a set of events to Event Hub
        /// fails. Note that the events remain buffered in this scenario and will be resent on
        /// subsequent retries.
        /// </summary>
        public event EventHandler<ProxyChannelEventArgs> MessageTransmissionError;

        /// <summary>
        /// Gets or sets the maximum number of telemetry messages that can be in the buffer to send. Messages will be dropped
        /// once this limit is hit until space in the buffer is cleared.
        /// </summary>
        public int MaxCapacity { get; set; }

        /// <summary>
        /// Defines the size of the batch (i.e. number of messages) to submit to the
        /// proxy API endpoint per call.
        /// </summary>
        public int MessageBatchSize { get; set; }

        /// <summary>
        /// A period of time to wait in-between failed transmission attempts against the target
        /// proxy REST API endpoint before retrying. Default = 5 seconds.
        /// </summary>
        public TimeSpan TransmissionFailureWaitTime { get; set; }

        /// <summary>
        /// The API client for interacting with the proxy endpoint.
        /// </summary>
        protected IProxyApiClient ApiClient { get; }

        /// <summary>
        /// Gets the telemetry buffer in which events/items are contained.
        /// </summary>
        protected Queue<ProxyTelemetryMessage> Buffer { get; private set; }

        /// <summary>
        /// Add a telemetry message/event to the buffer.
        /// </summary>
        /// <param name="message">A telemetry message/event.</param>
        public void Add(ProxyTelemetryMessage message)
        {
            if (message != null)
            {
                lock (this.bufferLock)
                {
                    if (this.Buffer.Count < this.MaxCapacity)
                    {
                        this.Buffer.Enqueue(message);
                    }
                    else
                    {
                        this.MessagesDropped?.Invoke(this, new ProxyChannelEventArgs(
                            message,
                            new
                            {
                                flushing = false,
                                messageCount = 1,
                                reason = $"Max capacity ({this.MaxCapacity}) exceeded."
                            }));
                    }
                }
            }
        }

        /// <summary>
        /// Begins watching for new messages to transmit to the proxy API.
        /// </summary>
        public void BeginMessageTransmission()
        {
            if (this.messageTransmissionTask == null)
            {
                this.messageTransmissionTask = Task.Factory.StartNew(() =>
                {
                    while (!this.cancellationTokenSource.IsCancellationRequested)
                    {
                        // Waiting for the flush delay to elapse
                        this.autoFlushWaitHandle.WaitOne(500);

                        // Pulling all items from the buffer and sending as one transmission.
                        this.TransmitEventsAsync(this.cancellationTokenSource.Token)
                            .GetAwaiter().GetResult();
                    }
                },
                this.cancellationTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            }
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
            try
            {
                TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromMinutes(2);
                DateTime flushTimeout = DateTime.UtcNow.Add(effectiveTimeout);

                this.FlushMessages?.Invoke(this, new ProxyChannelEventArgs(
                    this.Buffer,
                    new
                    {
                        flushing = true,
                        messageCount = this.Buffer.Count
                    }));

                // Allow time for the flush to finish.
                while (true)
                {
                    try
                    {
                        lock (this.bufferLock)
                        {
                            if (!this.Buffer.Any())
                            {
                                break;
                            }

                            if (DateTime.UtcNow >= flushTimeout)
                            {
                                if (this.Buffer.Any())
                                {
                                    this.MessagesDropped?.Invoke(this, new ProxyChannelEventArgs(
                                        this.Buffer,
                                        new
                                        {
                                            flushing = true,
                                            messageCount = this.Buffer.Count,
                                            reason = $"Flush timeout '{effectiveTimeout}' exceeded before buffer cleared."
                                        }));

                                    break;
                                }
                            }
                        }

                        Thread.Sleep(2000); 
                    }
                    catch
                    {
                        // Do not allow an unhandled error to crash the application. We hit the following
                        // rare occurrence bug in scale operations as an example:
                        //
                        // Unhandled exception: System.AggregateException: One or more errors occurred. (Collection was modified; enumeration operation may not execute.)
                        // ---> System.InvalidOperationException: Collection was modified; enumeration operation may not execute. at System.Collections.Generic.Queue`1.Enumerator.MoveNext()
                        // at System.Collections.Generic.List`1..ctor(IEnumerable`1 collection) at VirtualClient.Proxy.ProxyChannelEventArgs..ctor(IEnumerable`1 messages, Object context)
                        // at VirtualClient.Proxy.ProxyTelemetryChannel.Flush(Nullable`1 timeout) at VirtualClient.DependencyFactory.<>c__DisplayClass15_0.<FlushTelemetry>b__0(IFlushableChannel channel)
                        Thread.Sleep(2000);
                    }
                }
            }
            catch
            {
                // Do not allow an unhandled error to crash the application.
            }
        }

        /// <summary>
        /// Get the <see cref="IEnumerator"/> for the <see cref="ProxyTelemetryMessage"/>.
        /// </summary>
        /// <returns><see cref="IEnumerator"/> for the <see cref="ProxyTelemetryMessage"/>.</returns>
        public IEnumerator<ProxyTelemetryMessage> GetEnumerator()
        {
            return this.Buffer.GetEnumerator();
        }

        /// <summary>
        /// Get the <see cref="IEnumerator"/> for the <see cref="ProxyTelemetryMessage"/>.
        /// </summary>
        /// <returns><see cref="IEnumerator"/> for the <see cref="ProxyTelemetryMessage"/>.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
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

                    if (this.messageTransmissionTask != null)
                    {
                        Task.WhenAny(this.messageTransmissionTask, Task.Delay(10000))
                            .GetAwaiter().GetResult();
                    }

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
        /// Transmits the events in the buffer to the target EventHub endpoint.
        /// </summary>
        protected Task TransmitEventsAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                if (this.Buffer.Any())
                {
                    lock (this.messageTransmissionLock)
                    {
                        List<ProxyTelemetryMessage> currentBatch = new List<ProxyTelemetryMessage>();

                        try
                        {
                            while (this.Buffer.Any() && !cancellationToken.IsCancellationRequested)
                            {
                                currentBatch.Clear();

                                lock (this.bufferLock)
                                {
                                    while (currentBatch.Count < this.MessageBatchSize)
                                    {
                                        if (!this.Buffer.Any())
                                        {
                                            break;
                                        }

                                        ProxyTelemetryMessage nextMessage = this.Buffer.Dequeue();

                                        if (nextMessage != null)
                                        {
                                            currentBatch.Add(nextMessage);
                                        }
                                    }
                                }

                                HttpResponseMessage response = this.ApiClient.UploadTelemetryAsync(currentBatch, this.cancellationTokenSource.Token)
                                    .GetAwaiter().GetResult();

                                if (!response.IsSuccessStatusCode)
                                {
                                    this.MessageTransmissionError?.Invoke(this, new ProxyChannelEventArgs(
                                        currentBatch,
                                        new
                                        {
                                            messageCount = currentBatch.Count,
                                            httpStatus = response.StatusCode.ToString(),
                                            httpStatusCode = response.StatusCode
                                        }));

                                    this.RequeueMessages(currentBatch);
                                    Task.Delay(this.TransmissionFailureWaitTime).GetAwaiter().GetResult();
                                }
                                else
                                {
                                    this.MessagesTransmitted?.Invoke(this, new ProxyChannelEventArgs(
                                        currentBatch,
                                        new
                                        {
                                            messageCount = currentBatch.Count,
                                            httpStatus = response.StatusCode.ToString(),
                                            httpStatusCode = response.StatusCode
                                        }));
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            this.MessageTransmissionError?.Invoke(this, new ProxyChannelEventArgs(
                                currentBatch,
                                exc,
                                new { messageCount = currentBatch.Count }));

                            // Requeue the batch of messages if we fail to transmit them.
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                this.RequeueMessages(currentBatch);
                            }
                        }
                    }
                }
            });
        }

        private void RequeueMessages(List<ProxyTelemetryMessage> messages)
        {
            if (messages?.Any() == true)
            {
                lock (this.bufferLock)
                {
                    messages.ForEach(eventItem => this.Buffer.Enqueue(eventItem));
                }
            }
        }
    }
}