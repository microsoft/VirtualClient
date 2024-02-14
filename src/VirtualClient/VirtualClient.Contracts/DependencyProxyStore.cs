// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Represents a store that is behind a proxy API service endpoint.
    /// </summary>
    public class DependencyProxyStore : DependencyStore
    {
        /// <summary>
        /// Initializes an instance of the <see cref="DependencyBlobStore"/> class.
        /// </summary>
        /// <param name="storeName">The name of the content store (e.g. Content, Packages).</param>
        /// <param name="proxyApiUri">The URI for the proxy API/service including its port (e.g. http://any.uri:5000).</param>
        public DependencyProxyStore(string storeName, Uri proxyApiUri)
            : base(storeName, DependencyStore.StoreTypeProxyApi)
        {
            proxyApiUri.ThrowIfNull(nameof(proxyApiUri));
            this.ProxyApiUri = proxyApiUri;
        }

        /// <summary>
        /// The URI for the proxy API/service including its port (e.g. http://any.uri:5000).
        /// </summary>
        public Uri ProxyApiUri { get; }
    }
}
