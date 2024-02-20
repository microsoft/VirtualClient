// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO;
    using Polly;

    /// <summary>
    /// Common retry policies used by Virtual Client components to handle common
    /// transient errors to improve operational reliability.
    /// </summary>
    public static class RetryPolicies
    {
        /// <summary>
        /// Common retry policy for deleting files.
        /// </summary>
        public static IAsyncPolicy FileDelete { get; } = Policy.Handle<IOException>().WaitAndRetryAsync(5, retries => TimeSpan.FromSeconds(retries));

        /// <summary>
        /// Common retry policy for state updates via the <see cref="StateManager"/>.
        /// </summary>
        public static IAsyncPolicy StateUpdate { get; } = Policy.Handle<Exception>(exc =>
        {
            return true;
        }).WaitAndRetryAsync(10, retries => TimeSpan.FromSeconds(retries + 2));
    }
}
