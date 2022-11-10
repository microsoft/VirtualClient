// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Proxy
{
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts.Proxy;

    internal class ProxyLoggerProvider : ILoggerProvider
    {
        private IProxyApiClient proxyApiClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProxyLoggerProvider"/> class.
        /// </summary>
        /// <param name="apiClient">A client to the proxy API service including its port (e.g. http://any.proxy.uri:5000).</param>
        public ProxyLoggerProvider(IProxyApiClient apiClient)
        {
            apiClient.ThrowIfNull(nameof(apiClient));
            this.proxyApiClient = apiClient;
        }

        /// <summary>
        /// Creates a new <see cref="ProxyLogger"/> that uploads telemetry through a proxy API endpoint.
        /// </summary>
        public ILogger CreateLogger(string categoryName)
        {
            ProxyLogger logger = new ProxyLogger(this.proxyApiClient);
            logger.BeginMessageTransmission();
            return logger;
        }

        /// <summary>
        /// Does nothing.
        /// </summary>
        public void Dispose()
        {
        }
    }
}