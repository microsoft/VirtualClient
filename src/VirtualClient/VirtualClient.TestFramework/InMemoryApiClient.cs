using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Polly;
using VirtualClient.Common.Contracts;
using VirtualClient.Common.Extensions;
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    /// <summary>
    /// An in-memory API Client
    /// </summary>
    public class InMemoryApiClient : IApiClient
    {
        private Dictionary<string, HttpResponseInfo> responses;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryApiClient"/> class.
        /// </summary>
        public InMemoryApiClient(IPAddress ipAddress, int? port = null)
            : base()
        {
            ipAddress.ThrowIfNull(nameof(ipAddress));

            string address = ipAddress.ToString();
            if (ipAddress == IPAddress.Loopback)
            {
                address = "localhost";
            }

            this.BaseUri = new Uri($"https://{address}:{port ?? ApiClientManager.DefaultApiPort}");
            this.responses = new Dictionary<string, HttpResponseInfo>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryApiClient"/> class.
        /// </summary>
        public InMemoryApiClient(Uri uri)
            : base()
        {
            this.BaseUri = uri;
        }

        /// <summary>
        /// Base Uri of in-memory api client
        /// </summary>
        public Uri BaseUri { get; set; }

        /// <summary>
        /// Delegate enables custom logic to be executed on a call to create state.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> stateId - The ID of the state object to create.</item>
        /// <item><see cref="JObject"/> state - The state object to create (note that it must be JSON-serializable of course).</item>
        /// </list>
        /// </list>
        /// </summary>
        public Func<string, JObject, HttpResponseMessage> OnCreateState { get; set; }

        /// <summary>
        /// Delegate enables custom logic to be executed on a call to delete state.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> stateId - The ID of the state object to delete.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Func<string, HttpResponseMessage> OnDeleteState { get; set; }

        /// <summary>
        /// Delegate enables custom logic to be executed on a call to get the eventing API
        /// online status.
        /// </summary>
        public Func<HttpResponseMessage> OnGetEventingOnlineStatus { get; set; }

        /// <summary>
        /// Delegate enables custom logic to be executed on a call to get heartbeat.
        /// </summary>
        public Func<HttpResponseMessage> OnGetHeartbeat { get; set; }

        /// <summary>
        /// Delegate enables custom logic to be executed on a call to get state.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> stateId - The ID of the state object to retrieve.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Func<string, HttpResponseMessage> OnGetState { get; set; }

        /// <summary>
        /// Delegate enables custom logic to be executed on a call to send instructions.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="JObject"/> state - The state object of instructions (note that it must be JSON-serializable of course).</item>
        /// </list>
        /// </list>
        /// </summary>
        public Func<JObject, HttpResponseMessage> OnSendInstructions { get; set; }

        /// <summary>
        /// Delegate enables custom logic to be executed on a call to update state.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> stateId - The ID of the state object to update.</item>
        /// <item><see cref="JObject"/> state - The state object to update (note that it must be JSON-serializable of course).</item>
        /// </list>
        /// </list>
        /// </summary>
        public Func<string, JObject, HttpResponseMessage> OnUpdateState { get; set; }

        /// <inheritdoc />
        public Task<HttpResponseMessage> CreateStateAsync(string stateId, JObject state, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));
            state.ThrowIfNull(nameof(state));

            HttpResponseMessage response = null;

            if (this.OnCreateState != null)
            {
                response = this.OnCreateState.Invoke(stateId, state);
            }
            else
            {
                Item<JObject> stateItem = new Item<JObject>(stateId, JObject.FromObject(state));
                response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                response.Content = new StringContent(stateItem.ToJson());

                this.responses[stateId] = new HttpResponseInfo(HttpStatusCode.OK, stateItem);
            }

            return Task.FromResult(response);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> CreateStateAsync<TState>(string stateId, TState state, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
            where TState : State
        {
            return this.CreateStateAsync(stateId, JObject.FromObject(state), cancellationToken, retryPolicy);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> DeleteStateAsync(string stateId, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            HttpResponseMessage response = null;

            if (this.OnDeleteState != null)
            {
                response = this.OnDeleteState.Invoke(stateId);
            }
            else
            {
                if (this.responses.ContainsKey(stateId))
                {
                    response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                    this.responses.Remove(stateId);
                }
                else
                {
                    response = new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
                }
            }
            
            return Task.FromResult(response);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetServerOnlineStatusAsync(CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            HttpResponseMessage response = null;

            if (this.OnGetEventingOnlineStatus != null)
            {
                response = this.OnGetEventingOnlineStatus.Invoke();
            }
            else
            {
                response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            return Task.FromResult(response);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetHeartbeatAsync(CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            HttpResponseMessage response = null;

            if (this.OnGetHeartbeat != null)
            {
                response = this.OnGetHeartbeat.Invoke();
            }
            else
            {
                response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            return Task.FromResult(response);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> GetStateAsync(string stateId, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            HttpResponseMessage response = null;

            if (this.OnGetState != null)
            {
                response = this.OnGetState.Invoke(stateId);
            }
            else
            {
                if (this.responses.TryGetValue(stateId, out HttpResponseInfo responseInfo))
                {
                    response = new HttpResponseMessage(responseInfo.Status);
                    if (responseInfo.Content != null)
                    {
                        response.Content = new StringContent(responseInfo.Content.ToJson());
                    }
                }
                else
                {
                    response = new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
                }
            }
            
            return Task.FromResult(response);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> SendApplicationExitInstructionAsync(CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> SendInstructionsAsync(JObject instructions, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            HttpResponseMessage response = null;

            if (this.OnSendInstructions != null)
            {
                response = this.OnSendInstructions.Invoke(instructions);
            }
            else
            {
                response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            }

            return Task.FromResult(response);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> SendInstructionsAsync<TInstructions>(TInstructions instructions, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
            where TInstructions : Instructions
        {
            return this.SendInstructionsAsync(JObject.FromObject(instructions), cancellationToken, retryPolicy);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> UpdateStateAsync(string stateId, JObject state, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));
            state.ThrowIfNull(nameof(state));

            HttpResponseMessage response = null;
            if (this.OnUpdateState != null)
            {
                response = this.OnUpdateState.Invoke(stateId, state);
            }
            else
            {
                JObject stateItem = JObject.FromObject(state);
                response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
                response.Content = new StringContent(stateItem.ToJson());

                this.responses[stateId] = new HttpResponseInfo(HttpStatusCode.OK, state);
            }
            
            return Task.FromResult(response);
        }

        /// <inheritdoc />
        public Task<HttpResponseMessage> UpdateStateAsync<TState>(string stateId, Item<TState> state, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
            where TState : State
        {
            return this.UpdateStateAsync(stateId, JObject.FromObject(state), cancellationToken, retryPolicy);
        }

        private class HttpResponseInfo
        {
            public HttpResponseInfo(HttpStatusCode statusCode, object content = null)
            {
                this.Status = statusCode;
                this.Content = content;
            }

            public HttpStatusCode Status { get; }

            public object Content { get; }
        }
    }
}
