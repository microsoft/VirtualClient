// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Proxy;

    /// <summary>
    /// An in-memory api client manager
    /// </summary>
    public class InMemoryApiClientManager : Dictionary<string, IApiClient>, IApiClientManager
    {
        /// <summary>
        /// Delegate enables custom logic to be executed on a call to delete api client.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> Id - The ID of api client to delete.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Action<string> OnDeleteApiClient { get; set; }

        /// <summary>
        /// Delegate enables custom logic to be executed on a call to retrieve api client.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> Id - The ID of api client to retrieve.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Func<string, IApiClient> OnGetApiClient { get; set; }

        /// <summary>
        /// Delegate enables custom logic to be executed on a call to retrieve or create api client.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> Id - The ID of api client to retrieve or create.</item>
        /// <item><see cref="IPAddress"/> ipaddress - ipaddress of the api client. </item>
        /// </list>
        /// </list>
        /// </summary>
        public Func<string, IPAddress, Uri, IApiClient> OnGetOrCreateApiClient { get; set; }

        /// <summary>
        /// Delegate enables custom logic to be executed on a call to recycle api clients.
        /// </summary>
        public Action OnRecycleApiCLients { get; set; }

        /// <summary>
        /// Deletes the API client from the underlying cache of clients.
        /// </summary>
        /// <param name="id">The ID of the API client to delete.</param>
        public void DeleteApiClient(string id)
        {
            if (this.OnDeleteApiClient != null)
            {
                this.OnDeleteApiClient.Invoke(id);
            }
            else
            {
                if (this.ContainsKey(id))
                {
                    this.Remove(id);
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

            if (this.OnGetApiClient != null)
            {
                apiClient = this.OnGetApiClient.Invoke(id);
            }
            else
            {
                if (this.ContainsKey(id))
                {
                    apiClient = this[id];
                }
            }
            
            return apiClient;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public IProxyApiClient GetProxyApiClient(string id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets an existing/cached API client or creates a new one adding it to the cache.
        /// </summary>
        /// <param name="id">The ID of the API client to use for lookups.</param>
        /// <param name="ipAddress">The IP address of the target Virtual Client API service.</param>
        /// <param name="port">The port to use for the API URI (e.g. 4500 -> https://10.1.0.1:4500). Default = 4500.</param>
        /// <returns>
        /// A <see cref="VirtualClientApiClient"/> that can be used to interface with a target
        /// Virtual Client API service.
        /// </returns>
        public IApiClient GetOrCreateApiClient(string id, IPAddress ipAddress, int? port = null)
        {
            IApiClient apiClient;
            if (this.OnGetOrCreateApiClient != null)
            {
                apiClient = this.OnGetOrCreateApiClient.Invoke(id, ipAddress, null);
            }
            else
            {
                if (this.ContainsKey(id))
                {
                    apiClient = this[id];
                }
                else
                {
                    apiClient = new InMemoryApiClient(ipAddress);
                    this[id] = apiClient;
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
        /// A <see cref="VirtualClientApiClient"/> that can be used to interface with a target
        /// Virtual Client API service.
        /// </returns>
        public IApiClient GetOrCreateApiClient(string id, ClientInstance targetInstance)
        {
            IPAddress ipAddress = IPAddress.Parse(targetInstance.PrivateIPAddress);
            return this.GetOrCreateApiClient(id, ipAddress, targetInstance.Port);
        }

        /// <summary>
        /// Gets an existing/cached API client or creates a new one adding it to the cache.
        /// </summary>
        /// <param name="id">The ID of the API client to use for lookups.</param>
        /// <param name="uri">The URI of the target Virtual Client API service.</param>
        /// <returns>
        /// A <see cref="VirtualClientApiClient"/> that can be used to interface with a target
        /// Virtual Client API service.
        /// </returns>
        public IApiClient GetOrCreateApiClient(string id, Uri uri)
        {
            IApiClient apiClient;
            if (this.OnGetOrCreateApiClient != null)
            {
                apiClient = this.OnGetOrCreateApiClient.Invoke(id, null, uri);
            }
            else
            {
                if (this.ContainsKey(id))
                {
                    apiClient = this[id];
                }
                else
                {
                    apiClient = new InMemoryApiClient(uri);
                    this[id] = apiClient;
                }
            }

            return apiClient;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public IProxyApiClient GetOrCreateProxyApiClient(string id, Uri uri)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates API clients from the ones that are already cached.
        /// </summary>
        /// <returns>
        /// A <see cref="VirtualClientApiClient"/> that can be used to interface with a target
        /// Virtual Client API service.
        /// </returns>
        public void RecycleApiClients()
        {
        }
    }
}
