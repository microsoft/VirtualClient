// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using Polly;

    /// <summary>
    /// Provides local state management features to Virtual Client components.
    /// </summary>
    public interface IStateManager
    {
        /// <summary>
        /// Deletes the state information from the persisted store.
        /// </summary>
        /// <param name="stateId">The ID of the state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply to transient failure scenarios.</param>
        Task DeleteStateAsync(string stateId, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null);

        /// <summary>
        /// Saves the state information to a persisted store.
        /// </summary>
        /// <param name="stateId">The ID of the state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply to transient failure scenarios.</param>
        Task<JObject> GetStateAsync(string stateId, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null);

        /// <summary>
        /// Saves the state information to a persisted store.
        /// </summary>
        /// <param name="stateId">The ID of the state object.</param>
        /// <param name="state">The state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply to transient failure scenarios.</param>
        Task SaveStateAsync(string stateId, JObject state, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null);
    }
}
