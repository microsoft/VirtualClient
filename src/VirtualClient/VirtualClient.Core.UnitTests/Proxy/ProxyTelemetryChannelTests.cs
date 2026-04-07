// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Proxy
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Microsoft.Extensions.Logging;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts.Proxy;

    [TestFixture]
    [Category("Unit")]
    internal class ProxyTelemetryChannelTests
    {
        private MockFixture mockFixture;
        private Mock<IProxyApiClient> mockProxyApiClient;
        private TestProxyTelemetryChannel testChannel;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockProxyApiClient = new Mock<IProxyApiClient>();

            this.mockProxyApiClient
                .Setup(client => client.UploadTelemetryAsync(
                    It.IsAny<IEnumerable<ProxyTelemetryMessage>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.testChannel = new TestProxyTelemetryChannel(this.mockProxyApiClient.Object);
            this.testChannel.TransmissionFailureWaitTime = TimeSpan.Zero;
        }

        [TearDown]
        public void CleanupTest()
        {
            this.testChannel?.Dispose();
        }

        [Test]
        public void ProxyTelemetryChannelSetsPropertiesToExpectedDefaultValues()
        {
            Assert.AreEqual(100000, this.testChannel.MaxCapacity);
            Assert.AreEqual(30, this.testChannel.MessageBatchSize);
        }

        [Test]
        public void ProxyTelemetryChannelAddsMessagesToTheBufferInExpectedOrder()
        {
            List<ProxyTelemetryMessage> expectedEvents = new List<ProxyTelemetryMessage>
            {
                this.mockFixture.Create<ProxyTelemetryMessage>(),
                this.mockFixture.Create<ProxyTelemetryMessage>(),
                this.mockFixture.Create<ProxyTelemetryMessage>(),
                this.mockFixture.Create<ProxyTelemetryMessage>(),
                this.mockFixture.Create<ProxyTelemetryMessage>()
            };

            expectedEvents.ForEach(message => this.testChannel.Add(message));

            for (int i = 0; i < expectedEvents.Count; i++)
            {
                ProxyTelemetryMessage nextMessage = this.testChannel.Buffer.Dequeue();
                Assert.IsTrue(object.ReferenceEquals(expectedEvents[i], nextMessage));
            }
        }

        [Test]
        public async Task ProxyTelemetryChannelSendsExpectedBatchesOfMessages()
        {
            List<ProxyTelemetryMessage> expectedMessages = new List<ProxyTelemetryMessage>();
            int totalTransmissions = 0;

            for (int i = 0; i < this.testChannel.MessageBatchSize * 3; i++)
            {
                expectedMessages.Add(this.mockFixture.Create<ProxyTelemetryMessage>());
            }

            expectedMessages.ForEach(msg => this.testChannel.Add(msg));

            this.mockProxyApiClient
                .Setup(client => client.UploadTelemetryAsync(
                    It.IsAny<IEnumerable<ProxyTelemetryMessage>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<IEnumerable<ProxyTelemetryMessage>, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((messages, token, retryPolicy) =>
                {
                    totalTransmissions++;

                    Assert.IsNotEmpty(messages);
                    Assert.AreEqual(this.testChannel.MessageBatchSize, messages.Count());

                    foreach (ProxyTelemetryMessage message in messages)
                    {
                        expectedMessages.Remove(message);
                    }
                })
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            await this.testChannel.TransmitEventsAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsEmpty(expectedMessages);
            Assert.IsTrue(totalTransmissions == 3);
        }

        [Test]
        public async Task ProxyTelemetryChannelProcessesBufferedMessageTransmissionWithoutMessageLoss_SingleThread()
        {
            ConcurrentBag<ProxyTelemetryMessage> messagesTransmitted = new ConcurrentBag<ProxyTelemetryMessage>();

            this.mockProxyApiClient
                .Setup(client => client.UploadTelemetryAsync(
                    It.IsAny<IEnumerable<ProxyTelemetryMessage>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<IEnumerable<ProxyTelemetryMessage>, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((messages, token, retryPolicy) =>
                {
                    messages.ToList().ForEach(msg => messagesTransmitted.Add(msg));
                })
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.testChannel.BeginMessageTransmission();

            // Log messages for transmission, single logging thread/caller.
            await Task.Run(() => this.LogMessages(0, 1000)).ConfigureAwait(false);

            // Exit the wait if the buffer is not cleared within the timeout.
            bool timedOut = false;
            DateTime maxExitTime = DateTime.UtcNow.AddSeconds(10);
            while (this.testChannel.Buffer.Count > 0)
            {
                await Task.Delay(10).ConfigureAwait(false);

                if (DateTime.UtcNow > maxExitTime)
                {
                    timedOut = true;
                    break;
                }
            }

            Assert.IsFalse(timedOut);
            Assert.IsNotEmpty(messagesTransmitted);
            Assert.IsTrue(messagesTransmitted.Count == 1000);
            CollectionAssert.AllItemsAreUnique(messagesTransmitted.Select(msg => msg.Message));
        }

        [Test]
        public async Task ProxyTelemetryChannelProcessesBufferedMessageTransmissionWithoutMessageLoss_MultipleThreads()
        {
            ConcurrentBag<ProxyTelemetryMessage> messagesTransmitted = new ConcurrentBag<ProxyTelemetryMessage>();

            this.mockProxyApiClient
                .Setup(client => client.UploadTelemetryAsync(
                    It.IsAny<IEnumerable<ProxyTelemetryMessage>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<IEnumerable<ProxyTelemetryMessage>, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((messages, token, retryPolicy) =>
                {
                    messages.ToList().ForEach(msg => messagesTransmitted.Add(msg));
                })
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.testChannel.BeginMessageTransmission();

            // Log messages for transmission, multiple logging threads/callers at the same time.
            Task clientThread1 = Task.Run(() => this.LogMessages(0, 1000));
            Task clientThread2 = Task.Run(() => this.LogMessages(1000, 2000));

            await Task.WhenAll(clientThread1, clientThread2).ConfigureAwait(false);

            // Exit the wait if the buffer is not cleared within the timeout.
            bool timedOut = false;
            DateTime maxExitTime = DateTime.UtcNow.AddSeconds(10);
            while (this.testChannel.Buffer.Count > 0)
            {
                await Task.Delay(10).ConfigureAwait(false);

                if (DateTime.UtcNow > maxExitTime)
                {
                    timedOut = true;
                    break;
                }
            }

            Assert.IsFalse(timedOut);
            Assert.IsNotEmpty(messagesTransmitted);
            Assert.IsTrue(messagesTransmitted.Count == 2000);
            CollectionAssert.AllItemsAreUnique(messagesTransmitted.Select(msg => msg.Message));
        }

        [Test]
        public async Task ProxyTelemetryChannelDoesNotLoseMessagesDuringTransmissionFailures_SingleThread()
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                ConcurrentBag<ProxyTelemetryMessage> messagesTransmitted = new ConcurrentBag<ProxyTelemetryMessage>();

                int numTransmissionFailures = 0;

                this.mockProxyApiClient
                    .Setup(client => client.UploadTelemetryAsync(
                        It.IsAny<IEnumerable<ProxyTelemetryMessage>>(),
                        It.IsAny<CancellationToken>(),
                        It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .Callback<IEnumerable<ProxyTelemetryMessage>, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((messages, token, retryPolicy) =>
                    {
                        // Track the messages successfully submitted. After some number of failed transmission attempts, we want to
                        // mimic recovering from the transmission failures where messages are sent successfully.
                        numTransmissionFailures++;
                        if (numTransmissionFailures >= 3)
                        {
                            messages.ToList().ForEach(msg => messagesTransmitted.Add(msg));
                        }
                    })
                    .ReturnsAsync(() =>
                    {
                        if (numTransmissionFailures >= 3)
                        {
                            // Mimic finally recovering from the transmission failures.
                            return this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK);
                        }

                        // Mimic a REST API response failure
                        return this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.Forbidden);
                    });

                // Log messages for transmission, single logging thread/caller.
                await Task.Run(() => this.LogMessages(0, 1000)).ConfigureAwait(false);

                // Do not wait in-between attempts to retransmit messages on failures.
                this.testChannel.TransmissionFailureWaitTime = TimeSpan.Zero;

                int originalBufferCount = this.testChannel.Buffer.Count;

                // Ensure we do not wait forever in the case a bug causes the message transmits to run in
                // an endless loop.
                Task timeoutTask = Task.Delay(20000);
                Task transmitTask = this.testChannel.TransmitEventsAsync(CancellationToken.None);
                await Task.WhenAny(transmitTask, timeoutTask).ConfigureAwait(false);

                Assert.IsEmpty(this.testChannel.Buffer);
                Assert.AreEqual(originalBufferCount, messagesTransmitted.Count);
                Assert.AreEqual(1000, messagesTransmitted.Count);

                CollectionAssert.AllItemsAreUnique(messagesTransmitted.Select(msg => msg.Message));
            }
        }

        [Test]
        public async Task ProxyTelemetryChannelDoesNotLoseMessagesDuringTransmissionFailures_MultipleThreads()
        {
            using (CancellationTokenSource cancellationSource = new CancellationTokenSource())
            {
                ConcurrentBag<ProxyTelemetryMessage> messagesTransmitted = new ConcurrentBag<ProxyTelemetryMessage>();

                int numTransmissionFailures = 0;

                this.mockProxyApiClient
                    .Setup(client => client.UploadTelemetryAsync(
                        It.IsAny<IEnumerable<ProxyTelemetryMessage>>(),
                        It.IsAny<CancellationToken>(),
                        It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                    .Callback<IEnumerable<ProxyTelemetryMessage>, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((messages, token, retryPolicy) =>
                    {
                        // Track the messages successfully submitted. After some number of failed transmission attempts, we want to
                        // mimic recovering from the transmission failures where messages are sent successfully.
                        numTransmissionFailures++;
                        if (numTransmissionFailures >= 10)
                        {
                            messages.ToList().ForEach(msg => messagesTransmitted.Add(msg));
                        }
                    })
                    .ReturnsAsync(() =>
                    {
                        if (numTransmissionFailures >= 10)
                        {
                            // Mimic finally recovering from the transmission failures.
                            return this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK);
                        }

                        // Mimic a REST API response failure
                        return this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.Forbidden);
                    });

                // Log messages for transmission, multiple logging threads/callers at the same time.
                Task clientThread1 = Task.Run(() => this.LogMessages(0, 1000));
                Task clientThread2 = Task.Run(() => this.LogMessages(1000, 2000));

                await Task.WhenAll(clientThread1, clientThread2).ConfigureAwait(false);

                // Do not wait in-between attempts to retransmit messages on failures.
                this.testChannel.TransmissionFailureWaitTime = TimeSpan.Zero;

                int originalBufferCount = this.testChannel.Buffer.Count;

                // Ensure we do not wait forever in the case a bug causes the message transmits to run in
                // an endless loop.
                Task timeoutTask = Task.Delay(20000);
                Task transmitTask = this.testChannel.TransmitEventsAsync(CancellationToken.None);
                await Task.WhenAny(transmitTask, timeoutTask).ConfigureAwait(false);

                Assert.IsEmpty(this.testChannel.Buffer);
                Assert.AreEqual(originalBufferCount, messagesTransmitted.Count);
                Assert.AreEqual(2000, messagesTransmitted.Count);

                CollectionAssert.AllItemsAreUnique(messagesTransmitted.Select(msg => msg.Message));
            }
        }

        [Test]
        public async Task ProxyTelemetryChannelProcessesBufferedMessagesDuringFlushUntilATimeoutIsReached()
        {
            int messageCount = 1000;
            int totalApiCallAttempts = 0;

            System.Net.HttpStatusCode currentStatusCode = System.Net.HttpStatusCode.ServiceUnavailable;
            ConcurrentBag<ProxyTelemetryMessage> messagesTransmitted = new ConcurrentBag<ProxyTelemetryMessage>();

            this.mockProxyApiClient
                .Setup(client => client.UploadTelemetryAsync(
                    It.IsAny<IEnumerable<ProxyTelemetryMessage>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<IEnumerable<ProxyTelemetryMessage>, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((messages, token, retryPolicy) =>
                {
                    // On the first time seeing the message, we stage a submission failure, to enable validating
                    // that the original message is not lost and that we continue attempting to clear the buffer
                    // during a flush operation.
                    totalApiCallAttempts++;
                    if (totalApiCallAttempts > messageCount)
                    {
                        // After we have failed to send ALL messages at least once, we start succeeding.
                        currentStatusCode = System.Net.HttpStatusCode.OK;
                        messages.ToList().ForEach(msg => messagesTransmitted.Add(msg));
                    }
                })
                .ReturnsAsync(() => this.mockFixture.CreateHttpResponse(currentStatusCode));

            // Log messages for transmission, single logging thread/caller.
            this.LogMessages(0, messageCount);

            // We will wait to flush for up to 15 seconds here. In practice this should not take that amount of
            // time. Nonetheless by the time we exit the Flush method call, the buffer should be cleared.
            Task flushThread = Task.Run(() => this.testChannel.Flush(TimeSpan.FromSeconds(15)));

            this.testChannel.BeginMessageTransmission();
            await flushThread.ConfigureAwait(false);

            Assert.IsTrue(totalApiCallAttempts >= messageCount);
            Assert.IsNotEmpty(messagesTransmitted);
            Assert.IsTrue(messagesTransmitted.Count == messageCount);
            CollectionAssert.AllItemsAreUnique(messagesTransmitted.Select(msg => msg.Message));
        }

        [Test]
        public void ProxyTelemetryChannelInvokesTheExpectedEventWhenMessagesAreDroppedBeforeBeingAddedToTheBuffer()
        {
            bool eventInvoked = false;
            this.testChannel.MaxCapacity = 0;
            List<ProxyTelemetryMessage> messagesDropped = new List<ProxyTelemetryMessage>();

            this.testChannel.MessagesDropped += (sender, args) =>
            {
                Assert.IsTrue(sender is ProxyTelemetryChannel);
                Assert.IsTrue(object.ReferenceEquals(this.testChannel, sender));
                Assert.IsNotNull(args);
                Assert.IsNotEmpty(args.Messages);
                Assert.IsNotNull(args.Context);

                eventInvoked = true;
                messagesDropped.AddRange(args.Messages);
            };

            List<ProxyTelemetryMessage> messagesBuffered = this.LogMessages(0, 5);

            Assert.IsTrue(eventInvoked);
            CollectionAssert.AreEquivalent(
                messagesBuffered.Select(msg => msg.Message),
                messagesDropped.Select(msg => msg.Message));
        }

        [Test]
        public void ProxyTelemetryChannelInvokesTheExpectedEventWhenMessagesAreDroppedAfterFlushOperationsTimeOut()
        {
            bool eventInvoked = false;
            List<ProxyTelemetryMessage> messagesDropped = new List<ProxyTelemetryMessage>();

            this.testChannel.MessagesDropped += (sender, args) =>
            {
                eventInvoked = true;
                Assert.IsTrue(sender is ProxyTelemetryChannel);
                Assert.IsTrue(object.ReferenceEquals(this.testChannel, sender));
                Assert.IsNotNull(args);
                Assert.IsNotEmpty(args.Messages);
                Assert.IsNotNull(args.Context);

                eventInvoked = true;
                messagesDropped.AddRange(args.Messages);
            };

            List<ProxyTelemetryMessage> messagesBuffered = this.LogMessages(0, 5);
            this.testChannel.Flush(TimeSpan.Zero);

            Assert.IsTrue(eventInvoked);
            CollectionAssert.AreEquivalent(messagesBuffered.Select(msg => msg.Message), messagesDropped.Select(msg => msg.Message));
        }

        [Test]
        public async Task ProxyTelemetryChannelInvokesTheExpectedEventWhenMessagesAreSuccessfullyTransmitted()
        {
            bool eventInvoked = false;
            List<ProxyTelemetryMessage> messagesTransmitted = new List<ProxyTelemetryMessage>();

            this.testChannel.MessagesTransmitted += (sender, args) =>
            {
                Assert.IsTrue(sender is ProxyTelemetryChannel);
                Assert.IsTrue(object.ReferenceEquals(this.testChannel, sender));
                Assert.IsNotNull(args);
                Assert.IsNotEmpty(args.Messages);
                Assert.IsNotNull(args.Context);

                eventInvoked = true;
                messagesTransmitted.AddRange(args.Messages);
            };

            List<ProxyTelemetryMessage> messagesBuffered = this.LogMessages(0, 5);
            this.testChannel.BeginMessageTransmission();

            DateTime maxExitTime = DateTime.UtcNow.AddSeconds(10);
            while (!eventInvoked)
            {
                await Task.Delay(10).ConfigureAwait(false);

                if (DateTime.UtcNow > maxExitTime)
                {
                    break;
                }
            }

            Assert.IsTrue(eventInvoked);
            CollectionAssert.AreEquivalent(messagesBuffered.Select(msg => msg.Message), messagesTransmitted.Select(msg => msg.Message));
        }

        [Test]
        public async Task ProxyTelemetryChannelInvokesTheExpectedEventWhenMessagesFailToBeTransmitted()
        {
            bool eventInvoked = false;
            List<ProxyTelemetryMessage> messagesInTransmission = new List<ProxyTelemetryMessage>();

            this.testChannel.MessageTransmissionError += (sender, args) =>
            {
                Assert.IsTrue(sender is ProxyTelemetryChannel);
                Assert.IsTrue(object.ReferenceEquals(this.testChannel, sender));
                Assert.IsNotNull(args);
                Assert.IsNotEmpty(args.Messages);
                Assert.IsNotNull(args.Context);

                eventInvoked = true;
                messagesInTransmission.AddRange(args.Messages);
            };

            bool isFirstCall = true;
            this.mockProxyApiClient
               .Setup(client => client.UploadTelemetryAsync(
                   It.IsAny<IEnumerable<ProxyTelemetryMessage>>(),
                   It.IsAny<CancellationToken>(),
                   It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
               .ReturnsAsync(() =>
               {
                   System.Net.HttpStatusCode statusCode = System.Net.HttpStatusCode.OK;
                   if (isFirstCall)
                   {
                       isFirstCall = false;
                       statusCode = System.Net.HttpStatusCode.ServiceUnavailable;
                   }

                   return this.mockFixture.CreateHttpResponse(statusCode);
               });

            List<ProxyTelemetryMessage> messagesBuffered = this.LogMessages(0, 1);
            this.testChannel.BeginMessageTransmission();

            DateTime maxExitTime = DateTime.UtcNow.AddSeconds(10);
            while (!eventInvoked)
            {
                await Task.Delay(10).ConfigureAwait(false);

                if (DateTime.UtcNow > maxExitTime)
                {
                    break;
                }
            }

            Assert.IsTrue(eventInvoked);
            CollectionAssert.AreEquivalent(messagesBuffered.Select(msg => msg.Message), messagesInTransmission.Select(msg => msg.Message));
        }

        private List<ProxyTelemetryMessage> LogMessages(int countFrom, int countTo)
        {
            List<ProxyTelemetryMessage> messagesBuffered = new List<ProxyTelemetryMessage>();

            for (int i = countFrom; i < countTo; i++)
            {
                EventContext context = new EventContext(Guid.NewGuid()).AddContext("property", "value");

                ProxyTelemetryMessage message = new ProxyTelemetryMessage
                {
                    Source = "VirtualClient",
                    Message = i.ToString(),
                    SeverityLevel = LogLevel.Information,
                    EventType = LogType.Trace.ToString(),
                    ItemType = "trace",
                    CustomDimensions = context.Properties,
                    OperationId = context.ActivityId.ToString(),
                    OperationParentId = context.ParentActivityId.ToString(),
                    SdkVersion = "1.2.3.4",
                    AppHost = Environment.MachineName,
                    AppName = "VirtualClient"
                };

                messagesBuffered.Add(message);
                this.testChannel.Add(message);
            }

            return messagesBuffered;
        }


        private class TestProxyTelemetryChannel : ProxyTelemetryChannel
        {
            public TestProxyTelemetryChannel(IProxyApiClient apiClient)
                : base(apiClient)
            {
            }

            public new Queue<ProxyTelemetryMessage> Buffer
            {
                get
                {
                    return base.Buffer;
                }
            }

            public new Task TransmitEventsAsync(CancellationToken cancellationToken)
            {
                return base.TransmitEventsAsync(cancellationToken);
            }
        }
    }
}
