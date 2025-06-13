namespace VirtualClient.Proxy
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
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
        }

        [Test]
        public void ProxyLoggerUsesTheExpectedSourceWhenAnExplicitSourceIsProvided()
        {
            string expectedSource = "AnySource";
            using (ProxyTelemetryChannel channel = new ProxyTelemetryChannel(this.mockProxyApiClient.Object))
            {
                ProxyLogger logger = new ProxyLogger(channel, expectedSource);
                logger.Log(LogLevel.Information, new EventId(123, "AnyName"), EventContext.None, null, null);
                ProxyTelemetryMessage messageLogged = channel.FirstOrDefault();

                Assert.IsNotNull(messageLogged);
                Assert.AreEqual(expectedSource, messageLogged.Source);
            }
        }

        [Test]
        public void ProxyLoggerDefaultsToVirtualClientWhenAnExplicitSourceIsNotProvided()
        {
            using (ProxyTelemetryChannel channel = new ProxyTelemetryChannel(this.mockProxyApiClient.Object))
            {
                ProxyLogger logger = new ProxyLogger(channel);
                logger.Log(LogLevel.Information, new EventId(123, "AnyName"), EventContext.None, null, null);
                ProxyTelemetryMessage messageLogged = channel.FirstOrDefault();

                Assert.IsNotNull(messageLogged);
                Assert.AreEqual("VirtualClient", messageLogged.Source);
            }
        }
    }
}
