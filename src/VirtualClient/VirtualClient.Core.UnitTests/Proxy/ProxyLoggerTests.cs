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
    internal class ProxyLoggerTests
    {
        private MockFixture mockFixture;
        private TestProxyLogger mockProxyLogger;
        private Mock<IProxyApiClient> mockProxyApiClient;

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

            this.mockProxyLogger = new TestProxyLogger(this.mockProxyApiClient.Object);
        }

        [TearDown]
        public void CleanupTest()
        {
            this.mockProxyLogger?.Dispose();
        }

        [Test]
        public void ProxyLoggerSetsPropertiesToExpectedDefaultValues()
        {
            Assert.AreEqual(100000, this.mockProxyLogger.MaxCapacity);
            Assert.AreEqual(30, this.mockProxyLogger.MessageBatchSize);
        }

        [Test]
        public void ProxyLoggerUsesTheExpectedSourceWhenAnExplicitSourceIsProvided()
        {
            string expectedSource = "AnySource";
            this.mockProxyLogger = new TestProxyLogger(this.mockProxyApiClient.Object, expectedSource);
            this.mockProxyLogger.Log(LogLevel.Information, new EventId(123, "AnyName"), EventContext.None, null, null);
            ProxyTelemetryMessage messageLogged = this.mockProxyLogger.Buffer.Dequeue();

            Assert.AreEqual(expectedSource, messageLogged.Source);
        }

        [Test]
        public void ProxyLoggerUsesTheExpectedSourceWhenAnExplicitSourceIsNotProvided()
        {
            this.mockProxyLogger.Log(LogLevel.Information, new EventId(123, "AnyName"), EventContext.None, null, null);
            ProxyTelemetryMessage messageLogged = this.mockProxyLogger.Buffer.Dequeue();

            Assert.AreEqual(ProxyBlobDescriptor.DefaultSource, messageLogged.Source);
        }

        [Test]
        public void ProxyLoggerAddsMessagesToTheBufferInExpectedOrder()
        {
            List<ProxyTelemetryMessage> expectedEvents = new List<ProxyTelemetryMessage>
            {
                this.mockFixture.Create<ProxyTelemetryMessage>(),
                this.mockFixture.Create<ProxyTelemetryMessage>(),
                this.mockFixture.Create<ProxyTelemetryMessage>(),
                this.mockFixture.Create<ProxyTelemetryMessage>(),
                this.mockFixture.Create<ProxyTelemetryMessage>()
            };

            expectedEvents.ForEach(message => this.mockProxyLogger.Add(message));

            for (int i = 0; i < expectedEvents.Count; i++)
            {
                ProxyTelemetryMessage nextMessage = this.mockProxyLogger.Buffer.Dequeue();
                Assert.IsTrue(object.ReferenceEquals(expectedEvents[i], nextMessage));
            }
        }

        [Test]
        public async Task ProxyLoggerSendsExpectedBatchesOfMessages()
        {
            List<ProxyTelemetryMessage> expectedMessages = new List<ProxyTelemetryMessage>();
            int totalTransmissions = 0;

            for (int i = 0; i < this.mockProxyLogger.MessageBatchSize * 3; i++)
            {
                expectedMessages.Add(this.mockFixture.Create<ProxyTelemetryMessage>());
            }

            expectedMessages.ForEach(msg => this.mockProxyLogger.Add(msg));

            this.mockProxyApiClient
                .Setup(client => client.UploadTelemetryAsync(
                    It.IsAny<IEnumerable<ProxyTelemetryMessage>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .Callback<IEnumerable<ProxyTelemetryMessage>, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((messages, token, retryPolicy) =>
                {
                    totalTransmissions++;

                    Assert.IsNotEmpty(messages);
                    Assert.AreEqual(this.mockProxyLogger.MessageBatchSize, messages.Count());

                    foreach (ProxyTelemetryMessage message in messages)
                    {
                        expectedMessages.Remove(message);
                    }
                })
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            await this.mockProxyLogger.TransmitEventsAsync(CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsEmpty(expectedMessages);
            Assert.IsTrue(totalTransmissions == 3);
        }

        [Test]
        public async Task ProxyLoggerProcessesBufferedMessageTransmissionWithoutMessageLoss_SingleThread()
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

            this.mockProxyLogger.BeginMessageTransmission();

            // Log messages for transmission, single logging thread/caller.
            await Task.Run(() => this.LogMessages(0, 1000)).ConfigureAwait(false);

            // Exit the wait if the buffer is not cleared within the timeout.
            bool timedOut = false;
            DateTime maxExitTime = DateTime.UtcNow.AddSeconds(10);
            while (this.mockProxyLogger.Buffer.Count > 0)
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
        public async Task ProxyLoggerProcessesBufferedMessageTransmissionWithoutMessageLoss_MultipleThreads()
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

            this.mockProxyLogger.BeginMessageTransmission();

            // Log messages for transmission, multiple logging threads/callers at the same time.
            Task clientThread1 = Task.Run(() => this.LogMessages(0, 1000));
            Task clientThread2 = Task.Run(() => this.LogMessages(1000, 2000));

            await Task.WhenAll(clientThread1, clientThread2).ConfigureAwait(false);

            // Exit the wait if the buffer is not cleared within the timeout.
            bool timedOut = false;
            DateTime maxExitTime = DateTime.UtcNow.AddSeconds(10);
            while (this.mockProxyLogger.Buffer.Count > 0)
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
        public async Task ProxyLoggerDoesNotLoseMessagesDuringTransmissionFailures_SingleThread()
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
                this.mockProxyLogger.TransmissionFailureWaitTime = TimeSpan.Zero;

                int originalBufferCount = this.mockProxyLogger.Buffer.Count;

                // Ensure we do not wait forever in the case a bug causes the message transmits to run in
                // an endless loop.
                Task timeoutTask = Task.Delay(20000);
                Task transmitTask = this.mockProxyLogger.TransmitEventsAsync(CancellationToken.None);
                await Task.WhenAny(transmitTask, timeoutTask).ConfigureAwait(false);

                Assert.IsEmpty(this.mockProxyLogger.Buffer);
                Assert.AreEqual(originalBufferCount, messagesTransmitted.Count);
                Assert.AreEqual(1000, messagesTransmitted.Count);

                CollectionAssert.AllItemsAreUnique(messagesTransmitted.Select(msg => msg.Message));
            }
        }

        [Test]
        public async Task ProxyLoggerDoesNotLoseMessagesDuringTransmissionFailures_MultipleThreads()
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
                this.mockProxyLogger.TransmissionFailureWaitTime = TimeSpan.Zero;

                int originalBufferCount = this.mockProxyLogger.Buffer.Count;

                // Ensure we do not wait forever in the case a bug causes the message transmits to run in
                // an endless loop.
                Task timeoutTask = Task.Delay(20000);
                Task transmitTask = this.mockProxyLogger.TransmitEventsAsync(CancellationToken.None);
                await Task.WhenAny(transmitTask, timeoutTask).ConfigureAwait(false);

                Assert.IsEmpty(this.mockProxyLogger.Buffer);
                Assert.AreEqual(originalBufferCount, messagesTransmitted.Count);
                Assert.AreEqual(2000, messagesTransmitted.Count);

                CollectionAssert.AllItemsAreUnique(messagesTransmitted.Select(msg => msg.Message));
            }
        }

        private void LogMessages(int countFrom, int countTo)
        {
            for (int i = countFrom; i < countTo; i++)
            {
                this.mockProxyLogger.Log(
                    LogLevel.Information,
                    new EventId((int)LogType.Trace,
                    i.ToString()),
                    new EventContext(Guid.NewGuid()).AddContext("property", "value"),
                    null,
                    null);
            }
        }


        private class TestProxyLogger : ProxyLogger
        {
            public TestProxyLogger(IProxyApiClient apiClient, string source = null)
                : base(apiClient, source)
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
