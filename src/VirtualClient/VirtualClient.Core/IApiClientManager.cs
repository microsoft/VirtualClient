// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Proxy;

    /// <summary>
    /// Provides methods for creating and using clients for communications with 
    /// Virtual Client REST API services. 
    /// </summary>
    public interface IApiClientManager
    {
        /// <summary>
        /// Deletes the API client from the underlying cache of clients.
        /// </summary>
        /// <param name="id">The ID of the API client to delete.</param>
        void DeleteApiClient(string id);

        /// <summary>
        /// Gets an existing/cached API client.
        /// </summary>
        /// <param name="id">The ID of the API client to use for lookups.</param>
        /// <returns>
        /// An <see cref="IApiClient"/> that can be used to interface with a target
        /// Virtual Client API service.
        /// </returns>
        IApiClient GetApiClient(string id);

        /// <summary>
        /// Returns the effective port to use for hosting the API service. The port can be defined/overridden
        /// on the command line.
        /// </summary>
        /// <param name="instance">The client instance defining the role for the system.</param>
        /// <returns>The port on which the REST API service will be hosted.</returns>
        int GetApiPort(ClientInstance instance = null);

        /// <summary>
        /// Gets an existing/cached proxy API client or creates a new one adding it to the cache.
        /// </summary>
        /// <param name="id">The ID of the proxy API client to use for lookups.</param>
        /// <returns>
        /// An <see cref="IProxyApiClient"/> that can be used to interface with a target
        /// Virtual Client proxy API service.
        /// </returns>
        IProxyApiClient GetProxyApiClient(string id);

        /// <summary>
        /// Gets an existing/cached API client or creates a new one adding it to the cache. Note that the port for which
        /// the client will target will use the default port (e.g. 4500) unless the port is defined. The defaults can be overridden 
        /// by setting environment variables on the system or for the process that follow the format 
        /// {id}_Port (e.g. client-vm-01_Port=4500, server-vm-01_Port=4501).
        /// </summary>
        /// <param name="id">The ID of the API client to use for lookups.</param>
        /// <param name="ipAddress">The IP address of the target Virtual Client API service.</param>
        /// <param name="port">The port to use for the API URI (e.g. 4500 -> https://10.1.0.1:4500). Default = 4500.</param>
        /// <returns>
        /// An <see cref="IApiClient"/> that can be used to interface with a target
        /// Virtual Client API service.
        /// </returns>
        IApiClient GetOrCreateApiClient(string id, IPAddress ipAddress, int? port = null);

        /// <summary>
        /// Gets an existing/cached API client or creates a new one adding it to the cache. Note that the port for which
        /// the client will target will use the default port (e.g. 4500) unless the port is defined. The defaults can be overridden 
        /// by setting environment variables on the system or for the process that follow the format 
        /// {id}_Port (e.g. client-vm-01_Port=4500, server-vm-01_Port=4501).
        /// </summary>
        /// <param name="id">The ID of the API client to use for lookups.</param>
        /// <param name="targetInstance">The target Virtual Client server/instance for which to create the client.</param>
        /// <returns>
        /// An <see cref="IApiClient"/> that can be used to interface with a target
        /// Virtual Client API service.
        /// </returns>
        IApiClient GetOrCreateApiClient(string id, ClientInstance targetInstance);

        /// <summary>
        /// Gets an existing/cached API client or creates a new one adding it to the cache.
        /// </summary>
        /// <param name="id">The ID of the API client to use for lookups.</param>
        /// <param name="uri">The URI of the target Virtual Client API service including the port (e.g. http://any.server.uri:4500).</param>
        /// <returns>
        /// An <see cref="IApiClient"/> that can be used to interface with a target
        /// Virtual Client API service.
        /// </returns>
        IApiClient GetOrCreateApiClient(string id, Uri uri);

        /// <summary>
        /// Gets an existing/cached proxy API client or creates a new one adding it to the cache.
        /// </summary>
        /// <param name="id">The ID of the proxy API client to use for lookups.</param>
        /// <param name="uri">The URI of the target Virtual Client API service including the port (e.g. http://any.server.uri:4500).</param>
        /// <returns>
        /// An <see cref="IProxyApiClient"/> that can be used to interface with a target
        /// Virtual Client proxy API service.
        /// </returns>
        IProxyApiClient GetOrCreateProxyApiClient(string id, Uri uri);

        /// <summary>
        /// Creates new API clients from the ones that are already cached.
        /// </summary>
        void RecycleApiClients();
    }
}
