// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common.Extensions
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Extension methods for <see cref="Task"/> instances.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Configures the default behaviors for the <see cref="Task"/>. This includes applying
        /// ConfigureAwait(false) on the <see cref="Task"/>.
        /// </summary>
        /// <param name="task">The task to configure.</param>
        /// <returns>The <see cref="Task"/> configured for default behaviors.</returns>
        public static ConfiguredTaskAwaitable ConfigureDefaults(this Task task)
        {
            task.ThrowIfNull(nameof(task));
            return task.ConfigureAwait(false);
        }

        /// <summary>
        /// Configures the default behaviors for the <see cref="Task"/>. This includes applying
        /// ConfigureAwait(false) on the <see cref="Task"/>.
        /// </summary>
        /// <typeparam name="T">The data type of the task return object.</typeparam>
        /// <param name="task">The task to configure.</param>
        /// <returns>The <see cref="Task"/> configured for default behaviors.</returns>
        public static ConfiguredTaskAwaitable<T> ConfigureDefaults<T>(this Task<T> task)
        {
            task.ThrowIfNull(nameof(task));
            return task.ConfigureAwait(false);
        }

        /// <summary>
        /// Instructs the task to run a delegate only on the success of the task.
        /// </summary>
        /// <typeparam name="TResult">The type of result the task returns</typeparam>
        /// <param name="task">The task whom the delegate should be assigned to.</param>
        /// <param name="continueWith">The delegate that is assigned.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the current thread of execution.</param>
        /// <returns>The task supplied, with the continuation function assigned.</returns>
        public static Task<TResult> OnSuccessAsync<TResult>(this Task<TResult> task, Func<TResult, Task<TResult>> continueWith, CancellationToken cancellationToken)
        {
            task.ThrowIfNull(nameof(task));
            continueWith.ThrowIfNull(nameof(continueWith));

            return task.ContinueWith(
                (Task<TResult> t) =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        return continueWith(t.Result);
                    }

                    return t;
                },
                cancellationToken,
                TaskContinuationOptions.None,
                TaskScheduler.Default).Unwrap();
        }

        /// <summary>
        /// Instructs the task to run a delegate only on the failure of the task.
        /// </summary>
        /// <typeparam name="TResult">The type of result the task returns</typeparam>
        /// <param name="task">The task whom the delegate should be assigned to.</param>
        /// <param name="continueWith">The delegate that is assigned.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the current thread of execution.</param>
        /// <returns>The task supplied, with the continuation function assigned.</returns>
        public static Task<TResult> OnFailureAsync<TResult>(this Task<TResult> task, Func<Exception, Task<TResult>> continueWith, CancellationToken cancellationToken)
        {
            task.ThrowIfNull(nameof(task));
            continueWith.ThrowIfNull(nameof(continueWith));

            return task.ContinueWith<Task<TResult>>(
                (Task<TResult> t) =>
                {
                    if (t.IsFaulted)
                    {
                        return continueWith(t.Exception);
                    }

                    return t;
                },
                cancellationToken,
                TaskContinuationOptions.None,
                TaskScheduler.Default).Unwrap();
        }
    }
}