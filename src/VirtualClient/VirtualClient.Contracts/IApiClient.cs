// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides methods for interfacing with a target Virtual Client REST
    /// API service.
    /// </summary>
    public interface IApiClient
    {
        /// <summary>
        /// Gets the base URI to the server hosting the API (e.g. http://localhost:4500).
        /// </summary>
        Uri BaseUri { get; }

        /// <summary>
        /// Makes an API request to create a new state object/definition.
        /// </summary>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="state">The state object/definition.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the state object/definition created.
        /// </returns>
        Task<HttpResponseMessage> CreateStateAsync(string stateId, JObject state, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null);

        /// <summary>
        /// Makes an API request to create a new state object/definition.
        /// </summary>
        /// <typeparam name="TState">The data type of the state object.</typeparam>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="state">The state object/definition.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the state object/definition created.
        /// </returns>
        Task<HttpResponseMessage> CreateStateAsync<TState>(string stateId, TState state, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
            where TState : State;

        /// <summary>
        /// Makes an API request to delete a state object/definition.
        /// </summary>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the status code of the delete operation.
        /// </returns>
        Task<HttpResponseMessage> DeleteStateAsync(string stateId, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null);

        /// <summary>
        /// Makes an API request to get a heartbeat response.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the HTTP status code of the response.
        /// </returns>
        Task<HttpResponseMessage> GetHeartbeatAsync(CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null);

        /// <summary>
        /// Makes an API request to confirm if the eventing API is online and ready to service requests. The eventing
        /// API is offline by default awaiting signal from workloads or monitors within the Virtual Client to indicate they
        /// are ready.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the online status of the API.
        /// </returns>
        Task<HttpResponseMessage> GetServerOnlineStatusAsync(CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null);

        /// <summary>
        /// Makes an API request to get a state object/definition.
        /// </summary>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the state object/definition.
        /// </returns>
        Task<HttpResponseMessage> GetStateAsync(string stateId, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null);

        /// <summary>
        /// Makes an API request to instruct the target Virtual Client application to exit.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the status code of the operation.
        /// </returns>
        Task<HttpResponseMessage> SendApplicationExitInstructionAsync(CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null);

        /// <summary>
        /// Sends instructions to the target Virtual Client instance.
        /// </summary>
        /// <param name="instructions">Instructions to send to the target instance.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the status of the request.
        /// </returns>
        Task<HttpResponseMessage> SendInstructionsAsync(JObject instructions, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null);

        /// <summary>
        /// Sends instructions to the target Virtual Client instance.
        /// </summary>
        /// <typeparam name="TInstructions">The data type of the instructions object.</typeparam>
        /// <param name="instructions">Instructions to send to the target instance.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the status of the request.
        /// </returns>
        Task<HttpResponseMessage> SendInstructionsAsync<TInstructions>(TInstructions instructions, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
            where TInstructions : Instructions;

        /// <summary>
        /// Makes an API request to update an existing state object/definition.
        /// </summary>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="state">The state object/definition.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the state object/definition updated.
        /// </returns>
        Task<HttpResponseMessage> UpdateStateAsync(string stateId, JObject state, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null);

        /// <summary>
        /// Makes an API request to update an existing state object/definition.
        /// </summary>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="state">The state object/definition.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply when the client receives transient error responses from the API.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the state object/definition updated.
        /// </returns>
        Task<HttpResponseMessage> UpdateStateAsync<TState>(string stateId, Item<TState> state, CancellationToken cancellationToken, IAsyncPolicy<HttpResponseMessage> retryPolicy = null)
            where TState : State;
    }
}
