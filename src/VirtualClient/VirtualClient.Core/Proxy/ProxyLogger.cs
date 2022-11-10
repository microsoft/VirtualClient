// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Proxy
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts.Proxy;

    /// <summary>
    /// A logger that uploads telemetry messages/events to a proxy API endpoint.
    /// </summary>
    internal class ProxyLogger : ILogger, IEnumerable<ProxyTelemetryMessage>, IFlushableChannel, IDisposable
    {
        /// <summary>
        /// The default interval at which buffered events will be transmitted.
        /// </summary>
        public static readonly TimeSpan DefaultAutoFlushSendInterval = TimeSpan.FromSeconds(30);

        private const int DefaultMaxCapacity = 100000;
        private const int DefaultMessageBatchSize = 30;

        private readonly object bufferLock = new object();
        private readonly object messageTransmissionLock = new object();
        private static AssemblyName sdkAssembly = Assembly.GetAssembly(typeof(EventContext)).GetName();

        private static Dictionary<LogType, string> eventTypeMappings = new Dictionary<LogType, string>
        {
            { LogType.Undefined, "Traces" },
            { LogType.Error, "Errors" },
            { LogType.Trace, "Traces" },
            { LogType.Metrics, "Metrics" },
            { LogType.SystemEvent, "Events" }
        };

        private AutoResetEvent autoFlushWaitHandle;
        private CancellationTokenSource cancellationTokenSource;
        private Task messageTransmissionTask;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyLogger"/> class.
        /// </summary>
        /// <param name="apiClient">The API client for interacting with the proxy endpoint.</param>
        public ProxyLogger(IProxyApiClient apiClient)
        {
            apiClient.ThrowIfNull(nameof(apiClient));

            this.ApiClient = apiClient;
            this.MaxCapacity = ProxyLogger.DefaultMaxCapacity;
            this.MessageBatchSize = ProxyLogger.DefaultMessageBatchSize;
            this.Buffer = new Queue<ProxyTelemetryMessage>();
            this.TransmissionFailureWaitTime = TimeSpan.FromSeconds(5);
            this.cancellationTokenSource = new CancellationTokenSource();
            this.autoFlushWaitHandle = new AutoResetEvent(false);
        }

        /// <summary>
        /// Event is invoked whenever a set of messages is dropped due to capacity
        /// settings being exceeded.
        /// </summary>
        public event EventHandler<ProxyChannelEventArgs> MessageDropped;

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
        /// Not implemented.
        /// </summary>
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        /// <summary>
        /// Returns true always.
        /// </summary>
        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        /// <summary>
        /// Uploads the telemetry message/event details to the proxy API endpoint.
        /// </summary>
        /// <typeparam name="TState">The data type of the event state/context object (e.g. <see cref="EventContext"/>).</typeparam>
        /// <param name="logLevel">The severity level for the message/event.</param>
        /// <param name="eventId">Identifying information for the message/event.</param>
        /// <param name="state">The event state/context object.</param>
        /// <param name="exception">An error to upload.</param>
        /// <param name="formatter">Not used.</param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!string.IsNullOrWhiteSpace(eventId.Name))
            {
                if (!ProxyLogger.eventTypeMappings.TryGetValue((LogType)eventId.Id, out string eventType))
                {
                    eventType = ProxyLogger.eventTypeMappings[LogType.Undefined];
                }

                ProxyTelemetryMessage message = new ProxyTelemetryMessage
                {
                    Source = "VirtualClient",
                    EventType = eventType,
                    Message = eventId.Name,
                    ItemType = "trace",
                    SeverityLevel = logLevel,
                    AppHost = Environment.MachineName,
                    AppName = "VirtualClient",
                    SdkVersion = ProxyLogger.sdkAssembly.Version.ToString(),
                };

                EventContext context = state as EventContext;

                if (context != null)
                {
                    message.OperationId = context.ActivityId.ToString();
                    message.OperationParentId = context.ParentActivityId.ToString();
                    message.CustomDimensions = new Dictionary<string, object>(context.Properties, StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    message.OperationId = Guid.Empty.ToString();
                    message.OperationParentId = Guid.Empty.ToString();
                }

                this.Add(message);
            }
        }

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
                        this.OnMessageDropped(message);
                    }
                }
            }
        }

        /// <summary>
        /// Begins watching for new messages to transmit to the proxy API.
        /// </summary>
        public void BeginMessageTransmission()
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
            DateTime flushTimeout = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(60));

            // Allow time for the flush to finish.
            while (this.Buffer.Any())
            {
                if (DateTime.UtcNow >= flushTimeout)
                {
                    break;
                }

                Thread.Sleep(10);
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
        /// Invokes the 'MessageDropped' event.
        /// </summary>
        protected void OnMessageDropped(ProxyTelemetryMessage message)
        {
            this.MessageDropped?.Invoke(this, new ProxyChannelEventArgs(message));
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
                                    this.RequeueMessages(currentBatch);
                                    Task.Delay(this.TransmissionFailureWaitTime).GetAwaiter().GetResult();
                                }
                            }
                        }
                        catch
                        {
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