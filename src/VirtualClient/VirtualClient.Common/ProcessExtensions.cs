// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Extension methods for process execution objects and results.
    /// </summary>
    public static class ProcessExtensions
    {
        /// <summary>
        /// Sets the process for interactive mode (e.g. standard output and input redirected).
        /// </summary>
        /// <param name="process">Represents a process on the system.</param>
        public static IProcessProxy Interactive(this IProcessProxy process)
        {
            process.ThrowIfNull(nameof(process));
            process.RedirectStandardInput = true;
            process.RedirectStandardOutput = true;
            return process;
        }

        /// <summary>
        /// Kills the process if it is still running and handles any errors that
        /// can occurs if the process has gone out of scope.
        /// </summary>
        /// <param name="process">The process to kill.</param>
        public static void SafeKill(this IProcessProxy process)
        {
            if (process != null)
            {
                try
                {
                    process.Kill(true);
                }
                catch (Exception exc)
                {
                    int processId = -1;
                    string processName = "Indeterminate";

                    try
                    {
                        processId = process.Id;
                    }
                    catch
                    {
                        // Best effort here.
                    }

                    try
                    {
                        processName = process.Name;
                    }
                    catch
                    {
                        // Best effort here.
                    }

                    // Best effort here.
                    Console.WriteLine($"Process Cleanup Error: ID={processId}, Name={processName}, Error={exc.Message}");
                }
            }
        }

        /// <summary>
        /// Starts the underlying process and monitors it for completion.
        /// </summary>
        /// <param name="process">Represents a process on the system.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="timeout">
        /// An absolute timeout to apply for the case that the process does not finish in the amount of time expected. If the
        /// timeout is reached a <see cref="TimeoutException"/> exception will be thrown.
        /// </param>
        public static async Task StartAndWaitAsync(this IProcessProxy process, CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            process.ThrowIfNull(nameof(process));

            if (process.Start())
            {
                await process.WaitForExitAsync(cancellationToken, timeout);
            }
        }

        /// <summary>
        /// Waits for process to produce a response.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="responseExpression">A regular expression used to match the response.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="timeout">A period of time to wait for the response before throwing a <see cref="TimeoutException"/>.</param>
        public static async Task<IProcessProxy> WaitForResponseAsync(
            this IProcessProxy process, Regex responseExpression, CancellationToken cancellationToken, TimeSpan? timeout = null)
        {
            process.ThrowIfNull(nameof(process));
            responseExpression.ThrowIfNull(nameof(responseExpression));

            if (!process.RedirectStandardOutput)
            {
                throw new InvalidOperationException(
                    $"The process is not redirecting standard output and thus cannot capture responses " +
                    $"(timeout={timeout.ToString()}, command={process.StartInfo.FileName} {process.StartInfo.Arguments}).");
            }

            DateTime endTime = DateTime.MaxValue;
            if (timeout != null)
            {
                endTime = DateTime.Now.Add(timeout.Value);
            }

            bool responseReceived = false;
            while (!responseReceived)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (responseExpression.IsMatch(process.StandardOutput.ToString()))
                {
                    break;
                }

                if (timeout != null && DateTime.Now > endTime)
                {
                    throw new TimeoutException(
                        $"The process response was not received within the specified timeout " +
                        $"(timeout={timeout.ToString()}, command={process.StartInfo.FileName} {process.StartInfo.Arguments}).");
                }

                await Task.Delay(10).ConfigureAwait(false);
            }

            return process;
        }

        /// <summary>
        /// Waits for process to produce a response.
        /// </summary>
        /// <param name="process">The process.</param>
        /// <param name="response">The response to wait for. Note that this can be a regular expression.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="comparisonOptions">The regular expression comparison options.</param>
        /// <param name="timeout">A period of time to wait for the response before throwing a <see cref="TimeoutException"/>.</param>
        public static Task<IProcessProxy> WaitForResponseAsync(
            this IProcessProxy process, string response, CancellationToken cancellationToken, RegexOptions comparisonOptions = RegexOptions.IgnoreCase, TimeSpan? timeout = null)
        {
            process.ThrowIfNull(nameof(process));
            response.ThrowIfNullOrWhiteSpace(nameof(response));

            return process.WaitForResponseAsync(new Regex(response, comparisonOptions), cancellationToken, timeout);
        }
    }
}