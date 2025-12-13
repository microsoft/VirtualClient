// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
        /// Starts the underlying process and monitors it for completion.
        /// </summary>
        /// <param name="process">Represents a process on the system.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="timeout">
        /// An absolute timeout to apply for the case that the process does not finish in the amount of time expected. If the
        /// timeout is reached a <see cref="TimeoutException"/> exception will be thrown.
        /// </param>
        /// <param name="withExitConfirmation">True to confirm an exit code before returning. Default = false.</param>
        public static async Task StartAndWaitAsync(this IProcessProxy process, CancellationToken cancellationToken, TimeSpan? timeout = null, bool withExitConfirmation = false)
        {
            process.ThrowIfNull(nameof(process));

            if (process.Start())
            {
                await process.WaitForExitAsync(cancellationToken, timeout);

                if (withExitConfirmation)
                {
                    // There is a race condition-style flaw in the .NET implementation of the
                    // WaitForExit() method. The race condition allows for the process to exit after
                    // completion but for a period of time to pass before the kernel completes all finalization
                    // and cleanup steps (e.g. setting an exit code). To help prevent downstream issues that
                    // happen when attempting to access properties on the process during this race condition period
                    // of time, we are adding in an extra check on the process HasExited.
                    //
                    // Example of error hit during race condition period of time:
                    // Process must exit before requested information can be determined.
                    DateTime exitTime = DateTime.UtcNow.AddMinutes(2);
                    int exitCode = -1;
                    bool confirmed = false;

                    while (DateTime.UtcNow < exitTime)
                    {
                        try
                        {
                            // If the exit code is not available, this line will throw an exception.
                            exitCode = process.ExitCode;
                            confirmed = true;
                            break;
                        }
                        catch
                        {
                            // Wait, but don't throttle the CPU.
                            await Task.Delay(1000);
                        }
                    }

                    if (!confirmed)
                    {
                        try
                        {
                            string processName = null;
                            ProcessExtensions.TryGetValue<string>(
                                () =>
                                {
                                    return $"{Path.GetFileName(process?.StartInfo.FileName)} {process?.StartInfo.Arguments}"?.Trim();
                                }, 
                                out processName);

                            int processId = -1;
                            ProcessExtensions.TryGetValue<int>(() => process.Id, out processId);

                            Console.Error.WriteLine($"Process exit confirmation failed for process '{processName} (id={processId})'.");
                        }
                        catch
                        {
                            // Do not allow any exceptions to surface from here.
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="ProcessDetails"/> instance for the current process.
        /// </summary>
        /// <param name="process">The process from which to get the details.</param>
        /// <param name="toolName">The name of the tool/toolset associated with the process.</param>
        /// <param name="results">
        /// One or more sets of results generated by the process. Each key represents an identifier for the file (e.g. the file path or name) 
        /// and the value should be the content.
        /// </param>
        public static ProcessDetails ToProcessDetails(this IProcessProxy process, string toolName, params KeyValuePair<string, string>[] results)
        {
            process.ThrowIfNull(nameof(process));

            int processId = -1;
            int exitCode = -1;

            try
            {
                if (ProcessExtensions.TryGetValue<int>(() => process.Id, out int id))
                {
                    processId = id;
                }

                if (ProcessExtensions.TryGetValue<int>(() => process.ExitCode, out int code))
                {
                    exitCode = code;
                }
            }
            catch
            {
                // Avoid exceptions caused by kernel-layer race conditions on process finalization.
                // e.g.
                // Process must exit before requested information can be determined.
            }

            return new ProcessDetails
            {
                Id = processId,
                CommandLine = SensitiveData.ObscureSecrets($"{process.StartInfo?.FileName} {process.StartInfo?.Arguments}".Trim()),
                ExitCode = exitCode,
                Results = results,
                StandardError = process.StandardError?.Length > 0 ? process.StandardError.ToString() : string.Empty,
                StandardOutput = process.StandardOutput?.Length > 0 ? process.StandardOutput.ToString() : string.Empty,
                StartTime = process.StartTime,
                ExitTime = process.ExitTime,
                ToolName = toolName,
                WorkingDirectory = process.StartInfo?.WorkingDirectory
            };
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

        /// <summary>
        /// Returns true/false whether the value can be derived.
        /// </summary>
        /// <typeparam name="T">The data type of the value.</typeparam>
        /// <param name="reader">A function/delegate used to retrieve the value.</param>
        /// <param name="value">The value if existing.</param>
        /// <returns>True if the value can be confirmed to exist and is non-null.</returns>
        private static bool TryGetValue<T>(Func<T> reader, out T value)
            where T : IConvertible
        {
            bool confirmed = false;
            value = default(T);

            try
            {
                value = reader.Invoke();
                confirmed = true;
            }
            catch
            {
            }

            return confirmed;
        }
    }
}