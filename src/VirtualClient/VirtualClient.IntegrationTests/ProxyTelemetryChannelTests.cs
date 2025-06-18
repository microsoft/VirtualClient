namespace VirtualClient.Proxy
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts.Proxy;

    [TestFixture]
    [Category("Integration")]
    internal class ProxyTelemetryChannelTests
    {
        private MockFixture mockFixture;
        private Mock<IProxyApiClient> proxyApiClient;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.proxyApiClient = new Mock<IProxyApiClient>();

            this.proxyApiClient
                .Setup(client => client.UploadTelemetryAsync(
                    It.IsAny<IEnumerable<ProxyTelemetryMessage>>(),
                    It.IsAny<CancellationToken>(),
                    It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));
        }

        [Test]
        public void ProxyTelemetryChannelMessagesTransmitted()
        {
            ILogger logger = DependencyFactory.CreateFileLoggerProvider(this.mockFixture.Combine(MockFixture.TestAssemblyDirectory, "logs", "traces-proxy.log"), TimeSpan.FromSeconds(2), LogLevel.Trace)
                .CreateLogger("Proxy");

            using (ProxyTelemetryChannel channel = DependencyFactory.CreateProxyTelemetryChannel(this.proxyApiClient.Object, logger))
            {
                channel.BeginMessageTransmission();
                this.LogTelemetry(channel, 0, 10);

                while (channel.Any())
                {
                    Task.Delay(200).GetAwaiter().GetResult();
                }

                Task.Delay(5000).GetAwaiter().GetResult();
            }
        }

        [Test]
        public void ProxyTelemetryChannelFlushMessages()
        {
            ILogger logger = DependencyFactory.CreateFileLoggerProvider(this.mockFixture.Combine(MockFixture.TestAssemblyDirectory, "logs", "traces-proxy.log"), TimeSpan.FromSeconds(2), LogLevel.Trace)
                .CreateLogger("Proxy");

            using (ProxyTelemetryChannel channel = DependencyFactory.CreateProxyTelemetryChannel(this.proxyApiClient.Object, logger))
            {
                this.LogTelemetry(channel, 0, 200);
                Task flushing = Task.Run(() => channel.Flush(TimeSpan.FromSeconds(15)));
                Task.Delay(1000).GetAwaiter().GetResult();
                channel.BeginMessageTransmission();

                flushing.GetAwaiter().GetResult();
            }
        }

        [Test]
        public void ProxyTelemetryChannelMessageTransmissionError()
        {
            bool isFirstCall = true;
            this.proxyApiClient
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
                        statusCode = System.Net.HttpStatusCode.InternalServerError;
                    }

                    return this.mockFixture.CreateHttpResponse(statusCode);
                });

            ILogger logger = DependencyFactory.CreateFileLoggerProvider(this.mockFixture.Combine(MockFixture.TestAssemblyDirectory, "logs", "traces-proxy.log"), TimeSpan.FromSeconds(2), LogLevel.Trace)
                .CreateLogger("Proxy");

            using (ProxyTelemetryChannel channel = DependencyFactory.CreateProxyTelemetryChannel(this.proxyApiClient.Object, logger))
            {
                channel.BeginMessageTransmission();
                this.LogTelemetry(channel, 0, 10);

                while (channel.Any())
                {
                    Task.Delay(200).GetAwaiter().GetResult();
                }

                Task.Delay(5000).GetAwaiter().GetResult();
            }
        }

        [Test]
        public void ProxyTelemetryChannelMessagesDropped()
        {
            ILogger logger = DependencyFactory.CreateFileLoggerProvider(this.mockFixture.Combine(MockFixture.TestAssemblyDirectory, "logs", "traces-proxy.log"), TimeSpan.FromSeconds(2), LogLevel.Trace)
                .CreateLogger("Proxy");

            using (ProxyTelemetryChannel channel = DependencyFactory.CreateProxyTelemetryChannel(this.proxyApiClient.Object, logger))
            {
                channel.MaxCapacity = 0;
                channel.BeginMessageTransmission();
                this.LogTelemetry(channel, 0, 10);

                while (channel.Any())
                {
                    Task.Delay(200).GetAwaiter().GetResult();
                }

                Task.Delay(5000).GetAwaiter().GetResult();
            }
        }

        [Test]
        public void ProxyTelemetryChannelMessagesDroppedOnFlushing()
        {
            ILogger logger = DependencyFactory.CreateFileLoggerProvider(this.mockFixture.Combine(MockFixture.TestAssemblyDirectory, "logs", "traces-proxy.log"), TimeSpan.FromSeconds(2), LogLevel.Trace)
                .CreateLogger("Proxy");

            using (ProxyTelemetryChannel channel = DependencyFactory.CreateProxyTelemetryChannel(this.proxyApiClient.Object, logger))
            {
                this.LogTelemetry(channel, 0, 100);
                channel.Flush(TimeSpan.FromSeconds(1));
            }
        }

        private void LogTelemetry(ProxyTelemetryChannel channel, int countFrom, int countTo)
        {
            for (int i = countFrom; i < countTo; i++)
            {
                EventContext context = new EventContext(Guid.NewGuid()).AddContext("property", "value");

                ProxyTelemetryMessage message = new ProxyTelemetryMessage
                {
                    Source = "VirtualClient",
                    Message = $"{nameof(ProxyTelemetryChannel)}.Message{i}",
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

                channel.Add(message);
            }
        }
    }
}
