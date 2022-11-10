// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Implements a test/mock state manager that tracks state objects in-memory and can be
    /// used to setup and confirm state behaviors in test scenarios.
    /// </summary>
    public class InMemoryStateManager : Dictionary<string, object>, IStateManager
    {
        /// <summary>
        /// Delegate enables custom logic to be executed on a call to delete state.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> stateId - The ID of the state object to delete.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Action<string> OnDeleteState { get; set; }

        /// <summary>
        /// Delegate enables custom logic to be executed on a call to get state.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> stateId - The ID of the state object to retrieve.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Action<string> OnGetState { get; set; }

        /// <summary>
        /// Delegate enables custom logic to be executed on a call to save state.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> stateId - The ID of the state object to save.</item>
        /// <item><see cref="JObject"/> state - The state object to save (note that it must be JSON-serializable of course).</item>
        /// </list>
        /// </list>
        /// </summary>
        public Action<string, JObject> OnSaveState { get; set; }

        /// <summary>
        /// Deletes the state saved in-memory.
        /// </summary>
        public Task DeleteStateAsync(string stateId, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            this.OnDeleteState?.Invoke(stateId);
            if (this.ContainsKey(stateId))
            {
                this.Remove(stateId);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns the state saved in-memory.
        /// </summary>
        public Task<JObject> GetStateAsync(string stateId, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            this.OnGetState?.Invoke(stateId);
            JObject state = null;
            if (this.ContainsKey(stateId))
            {
                object stateValue = this[stateId];
                if (stateValue is JObject)
                {
                    state = stateValue as JObject;
                }
                else
                {
                    state = JObject.FromObject(stateValue);
                }
            }

            return Task.FromResult(state);
        }

        /// <summary>
        /// Saves the state in-memory.
        /// </summary>
        public Task SaveStateAsync(string stateId, JObject state, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));
            this.OnSaveState?.Invoke(stateId, state);
            this[stateId] = state;

            return Task.CompletedTask;
        }
    }
}
