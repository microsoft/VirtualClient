// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extensions for handling <see cref="Task"/> configure await calls.
    /// </summary>
    public static class ConfigureAwaitExtensions
    {
        /// <summary>
        /// Shortcut to configure the default await behavior for a <see cref="Task"/> (i.e. ConfigureAwait(false)).
        /// </summary>
        /// <param name="task">The task to configure.</param>
        /// <returns>The <see cref="Task"/> configured for default behaviors.</returns>
        public static ConfiguredTaskAwaitable ConfigureAwait(this Task task)
        {
            task.ThrowIfNull(nameof(task));
            return task.ConfigureAwait(false);
        }

        /// <summary>
        /// Shortcut to configure the default await behavior for a <see cref="Task"/> (i.e. ConfigureAwait(false)).
        /// </summary>
        /// <typeparam name="T">The data type of the task return object.</typeparam>
        /// <param name="task">The task to configure.</param>
        /// <returns>The <see cref="Task"/> configured for default behaviors.</returns>
        public static ConfiguredTaskAwaitable<T> ConfigureAwait<T>(this Task<T> task)
        {
            task.ThrowIfNull(nameof(task));
            return task.ConfigureAwait(false);
        }
    }
}
