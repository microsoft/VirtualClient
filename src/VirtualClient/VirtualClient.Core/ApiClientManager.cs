// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Proxy;

    /// <summary>
    /// Provides methods for creating and using clients for communications with 
    /// Virtual Client REST API services.
    /// </summary>
    public class ApiClientManager : IApiClientManager
    {
        /// <summary>
        /// The default port used by the API service for HTTP/TCP communications.
        /// </summary>
        public const int DefaultApiPort = 4500;

        private static readonly object LockObject = new object();
        private Dictionary<string, IApiClient> apiClients;
        private Dictionary<string, IProxyApiClient> proxyApiClients;
        private Dictionary<string, int> apiPorts;

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiClientManager"/> class.
        /// </summary>
        public ApiClientManager(IDictionary<string, int> apiPorts = null)
        {
            this.apiClients = new Dictionary<string, IApiClient>(StringComparer.OrdinalIgnoreCase);
            this.proxyApiClients = new Dictionary<string, IProxyApiClient>(StringComparer.OrdinalIgnoreCase);
            this.apiPorts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                [nameof(ApiClientManager.DefaultApiPort)] = ApiClientManager.DefaultApiPort
            };

            // Override the default API port(s). This can contain an override of the default
            // API port (single port) or an override of the API port per role (e.g. 4501/Client, 4502/Server).
            if (apiPorts?.Any() == true)
            {
                foreach (var entry in apiPorts)
                {
                    this.apiPorts[entry.Key] = entry.Value;
                }
            }
        }

        /// <summary>
        /// Deletes the API client from the underlying cache of clients.
        /// </summary>
        /// <param name="id">The ID of the API client to delete.</param>
        public void DeleteApiClient(string id)
        {
            lock (ApiClientManager.LockObject)
            {
                if (this.apiClients.ContainsKey(id))
                {
                    this.apiClients.Remove(id);
                }

                if (this.proxyApiClients.ContainsKey(id))
                {
                    this.proxyApiClients.Remove(id);
                }
            }
        }

        /// <summary>
        /// Gets an existing/cached API client or creates a new one adding it to the cache.
        /// </summary>
        /// <param name="id">The ID of the API client to use for lookups.</param>
        /// <returns>
        /// An <see cref="IApiClient"/> that can be used to interface with a target
        /// Virtual Client API service.
        /// </returns>
        public IApiClient GetApiClient(string id)
        {
            IApiClient apiClient = null;

            lock (ApiClientManager.LockObject)
            {
                this.apiClients.TryGetValue(id, out apiClient);
            }

            return apiClient;
        }

        /// <summary>
        /// Gets all existing/cached API clients.
        /// </summary>
        /// <returns>
        /// A set of <see cref="IApiClient"/> that can be used to interface with target
        /// Virtual Client API services.
        /// </returns>
        public IEnumerable<IApiClient> GetApiClients()
        {
            List<IApiClient> clients = new List<IApiClient>();
            if (this.apiClients.Any())
            {
                clients.AddRange(this.apiClients.Values);
            }

            return clients;
        }

        /// <summary>
        /// Returns the effective port to use for hosting the API service. The port can be defined/overridden
        /// on the command line.
        /// </summary>
        /// <param name="instance">The client instance defining the role for the system.</param>
        /// <returns>The port on which the REST API service will be hosted.</returns>
        public int GetApiPort(ClientInstance instance = null)
        {
            int apiPort = this.apiPorts[nameof(ApiClientManager.DefaultApiPort)];
            if (!string.IsNullOrWhiteSpace(instance?.Role) && this.apiPorts.TryGetValue(instance.Role, out int userDefinedApiPort))
            {
                apiPort = userDefinedApiPort;
            }

            return apiPort;
        }

        /// <summary>
        /// Gets an existing/cached proxy API client or creates a new one adding it to the cache.
        /// </summary>
        /// <param name="id">The ID of the proxy API client to use for lookups.</param>
        /// <returns>
        /// An <see cref="IProxyApiClient"/> that can be used to interface with a target
        /// Virtual Client proxy API service.
        /// </returns>
        public IProxyApiClient GetProxyApiClient(string id)
        {
            IProxyApiClient apiClient = null;

            lock (ApiClientManager.LockObject)
            {
                this.proxyApiClients.TryGetValue(id, out apiClient);
            }

            return apiClient;
        }

        /// <summary>
        /// Gets an existing/cached API client or creates a new one adding it to the cache.
        /// </summary>
        /// <param name="id">The ID of the API client to use for lookups.</param>
        /// <param name="ipAddress">The IP address of the target Virtual Client API service.</param>
        /// <param name="port">The port to use for the API URI (e.g. 4500 -> https://10.1.0.1:4500). Default = 4500.</param>
        /// <returns>
        /// An <see cref="IApiClient"/> that can be used to interface with a target
        /// Virtual Client API service.
        /// </returns>
        public IApiClient GetOrCreateApiClient(string id, IPAddress ipAddress, int? port = null)
        {
            IApiClient apiClient = null;

            lock (ApiClientManager.LockObject)
            {
                apiClient = this.GetApiClient(id);
                if (apiClient == null)
                {
                    int apiPort = ApiClientManager.DefaultApiPort;
                    if (port != null)
                    {
                        apiPort = port.Value;
                    }

                    apiClient = DependencyFactory.CreateVirtualClientApiClient(ipAddress, apiPort);
                    this.apiClients[id] = apiClient;
                }
            }

            return apiClient;
        }

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
        public IApiClient GetOrCreateApiClient(string id, ClientInstance targetInstance)
        {
            IPAddress ipAddress = IPAddress.Parse(targetInstance.IPAddress);
            return this.GetOrCreateApiClient(id, ipAddress, this.GetApiPort(targetInstance));
        }

        /// <summary>
        /// Gets an existing/cached API client or creates a new one adding it to the cache.
        /// </summary>
        /// <param name="id">The ID of the API client to use for lookups.</param>
        /// <param name="uri">The URI of the target Virtual Client API service including the port (e.g. http://any.server.uri:4500).</param>
        /// <returns>
        /// An <see cref="VirtualClientApiClient"/> that can be used to interface with a target
        /// Virtual Client API service.
        /// </returns>
        public IApiClient GetOrCreateApiClient(string id, Uri uri)
        {
            IApiClient apiClient = null;

            lock (ApiClientManager.LockObject)
            {
                apiClient = this.GetApiClient(id);
                if (apiClient == null)
                {
                    apiClient = DependencyFactory.CreateVirtualClientApiClient(uri);
                    this.apiClients[id] = apiClient;
                }
            }

            return apiClient;
        }

        /// <summary>
        /// Gets an existing/cached proxy API client or creates a new one adding it to the cache.
        /// </summary>
        /// <param name="id">The ID of the proxy API client to use for lookups.</param>
        /// <param name="uri">The URI of the target Virtual Client API service including the port (e.g. http://any.server.uri:4500).</param>
        /// <returns>
        /// An <see cref="IProxyApiClient"/> that can be used to interface with a target
        /// Virtual Client API service.
        /// </returns>
        public IProxyApiClient GetOrCreateProxyApiClient(string id, Uri uri)
        {
            IProxyApiClient apiClient = null;

            lock (ApiClientManager.LockObject)
            {
                apiClient = this.GetProxyApiClient(id);
                if (apiClient == null)
                {
                    apiClient = DependencyFactory.CreateVirtualClientProxyApiClient(uri);
                    this.proxyApiClients[id] = apiClient;
                }
            }

            return apiClient;
        }

        /// <summary>
        /// Creates new API clients from the ones that are already cached.
        /// </summary>
        public void RecycleApiClients()
        {
            lock (ApiClientManager.LockObject)
            {
                if (this.apiClients.Any())
                {
                    Dictionary<string, IApiClient> newClients = new Dictionary<string, IApiClient>(StringComparer.OrdinalIgnoreCase);
                    foreach (var entry in this.apiClients)
                    {
                        newClients.Add(entry.Key, DependencyFactory.CreateVirtualClientApiClient(entry.Value.BaseUri));
                    }

                    this.apiClients.Clear();
                    this.apiClients.AddRange(newClients);
                }

                if (this.proxyApiClients.Any())
                {
                    Dictionary<string, IProxyApiClient> newClients = new Dictionary<string, IProxyApiClient>(StringComparer.OrdinalIgnoreCase);
                    foreach (var entry in this.proxyApiClients)
                    {
                        newClients.Add(entry.Key, DependencyFactory.CreateVirtualClientProxyApiClient(entry.Value.BaseUri));
                    }

                    this.proxyApiClients.Clear();
                    this.proxyApiClients.AddRange(newClients);
                }
            }
        }
    }
}
