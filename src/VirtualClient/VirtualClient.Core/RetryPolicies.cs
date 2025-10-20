// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO;
    using Polly;
    using Polly.Retry;

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
        /// Common retry policy for file access and operations.
        /// </summary>
        public static IAsyncPolicy FileOperations { get; } = Policy.Handle<IOException>().WaitAndRetryAsync(5, retries => TimeSpan.FromSeconds(retries));

        /// <summary>
        /// Common retry policy for state updates via the <see cref="StateManager"/>.
        /// </summary>
        public static IAsyncPolicy StateUpdate { get; } = Policy.Handle<Exception>(exc =>
        {
            return true;
        }).WaitAndRetryAsync(10, retries => TimeSpan.FromSeconds(retries + 2));

        /// <summary>
        /// Synchronous retry policies.
        /// </summary>
        public static class Synchronous
        {
            /// <summary>
            /// Common retry policy for deleting files.
            /// </summary>
            public static RetryPolicy FileDelete { get; } = Policy.Handle<IOException>().WaitAndRetry(5, retries => TimeSpan.FromSeconds(retries));

            /// <summary>
            /// Common retry policy for file access and operations.
            /// </summary>
            public static RetryPolicy FileOperations { get; } = Policy.Handle<IOException>().WaitAndRetry(5, retries => TimeSpan.FromSeconds(retries));
        }
    }
}
