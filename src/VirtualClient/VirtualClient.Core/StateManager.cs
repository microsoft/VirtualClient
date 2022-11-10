// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO;
    using System.IO.Abstractions;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides local state management features to Virtual Client
    /// components.
    /// </summary>
    public class StateManager : IStateManager
    {
        private static readonly SemaphoreSlim LockSemaphore = new SemaphoreSlim(1);

        private static readonly IAsyncPolicy DefaultRetryPolicy = Policy
            .Handle<IOException>()
            .WaitAndRetryAsync(10, (retries) => TimeSpan.FromSeconds(retries + 1));

        /// <summary>
        /// Initializes a new instance of the <see cref="StateManager"/> class.
        /// </summary>
        /// <param name="fileSystem">Provides features for interacting with the file system.</param>
        /// <param name="platformSpecifics">Provides platform-specific path information.</param>
        public StateManager(IFileSystem fileSystem, PlatformSpecifics platformSpecifics)
        {
            fileSystem.ThrowIfNull(nameof(fileSystem));
            platformSpecifics.ThrowIfNull(nameof(fileSystem));

            this.FileSystem = fileSystem;
            this.PlatformSpecifics = platformSpecifics;
        }

        /// <summary>
        /// Provides features for interacting with the file system.
        /// </summary>
        public IFileSystem FileSystem { get; }

        /// <summary>
        /// Provides platform-specific information.
        /// </summary>
        public PlatformSpecifics PlatformSpecifics { get; }

        /// <summary>
        /// Deletes the state information from the persisted store.
        /// </summary>
        /// <param name="stateId">The ID of the state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply to transient failure scenarios.</param>
        public virtual async Task DeleteStateAsync(string stateId, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            await StateManager.LockSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                string stateFilePath = this.GetStateFilePath(stateId);
                if (this.FileSystem.File.Exists(stateFilePath))
                {
                    await (retryPolicy ?? StateManager.DefaultRetryPolicy).ExecuteAsync(async () =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.FileSystem.File.DeleteAsync(stateFilePath)
                                .ConfigureAwait(false);
                        }

                    }).ConfigureAwait(false);
                }
            }
            finally
            {
                StateManager.LockSemaphore.Release();
            }
        }

        /// <summary>
        /// Saves the state information to a persisted store.
        /// </summary>
        /// <param name="stateId">The ID of the state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply to transient failure scenarios.</param>
        public virtual async Task<JObject> GetStateAsync(string stateId, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            await StateManager.LockSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                JObject state = default(JObject);
                string stateFilePath = this.GetStateFilePath(stateId);
                if (this.FileSystem.File.Exists(stateFilePath))
                {
                    state = await (retryPolicy ?? StateManager.DefaultRetryPolicy).ExecuteAsync(async () =>
                    {
                        string fileContent = await this.FileSystem.File.ReadAllTextAsync(stateFilePath, cancellationToken)
                            .ConfigureAwait(false);

                        return JObject.Parse(fileContent);

                    }).ConfigureAwait(false);
                }

                return state;
            }
            finally
            {
                StateManager.LockSemaphore.Release();
            }
        }

        /// <summary>
        /// Saves the state information to a persisted store.
        /// </summary>
        /// <param name="stateId">The ID of the state object.</param>
        /// <param name="state">The state object.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="retryPolicy">A retry policy to apply to transient failure scenarios.</param>
        public virtual async Task SaveStateAsync(string stateId, JObject state, CancellationToken cancellationToken, IAsyncPolicy retryPolicy = null)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));
            state.ThrowIfNull(nameof(state));

            await StateManager.LockSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                string stateFilePath = this.GetStateFilePath(stateId);
                string stateFileDirectory = Path.GetDirectoryName(stateFilePath);

                await (retryPolicy ?? StateManager.DefaultRetryPolicy).ExecuteAsync(async () =>
                {
                    if (!this.FileSystem.Directory.Exists(stateFileDirectory))
                    {
                        this.FileSystem.Directory.CreateDirectory(stateFileDirectory);
                    }

                    string fileContent = state.ToString(Formatting.Indented);
                    await this.FileSystem.File.WriteAllTextAsync(stateFilePath, fileContent, cancellationToken)
                        .ConfigureAwait(false);

                }).ConfigureAwait(false);
            }
            finally
            {
                StateManager.LockSemaphore.Release();
            }
        }

        /// <summary>
        /// Returns the full path and file name to the state file given the ID provided.
        /// </summary>
        /// <param name="stateId">The ID of the state file/object.</param>
        protected virtual string GetStateFilePath(string stateId)
        {
            // Example:
            // C:\any\directory\VirtualClient.1.2.3.4\content\VirtualClient.exe
            // C:\any\directory\VirtualClient.1.2.3.4\content\state\examplestate.json
            return this.PlatformSpecifics.GetStatePath($"{stateId.ToLowerInvariant()}.json");
        }
    }
}
