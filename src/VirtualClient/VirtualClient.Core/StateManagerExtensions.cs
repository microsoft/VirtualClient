// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extensions for <see cref="IStateManager"/> instances.
    /// </summary>
    public static class StateManagerExtensions
    {
        /// <summary>
        /// Saves the state information to a persisted store.
        /// </summary>
        /// <param name="stateManager">The state manager.</param>
        /// <param name="stateId">The ID of the state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A policy that can be applied to handle transient issues.</param>
        public static async Task<TState> GetStateAsync<TState>(this IStateManager stateManager, string stateId, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null)
            where TState : class
        {
            stateManager.ThrowIfNull(nameof(stateManager));
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            JObject state = await stateManager.GetStateAsync(stateId, cancellationToken, retryPolicy).ConfigureAwait(false);
            return state?.ToObject<TState>();
        }

        /// <summary>
        /// Saves the state information to a persisted store.
        /// </summary>
        /// <param name="stateManager">The state manager.</param>
        /// <param name="stateId">The ID of the state object.</param>
        /// <param name="state">The state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A policy that can be applied to handle transient issues.</param>
        public static Task SaveStateAsync<TState>(this IStateManager stateManager, string stateId, TState state, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null)
            where TState : class
        {
            stateManager.ThrowIfNull(nameof(stateManager));
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));
            state.ThrowIfNull(nameof(state));

            return stateManager.SaveStateAsync(stateId, JObject.FromObject(state), cancellationToken, retryPolicy);
        }
    }
}
