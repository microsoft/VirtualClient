// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Defines how to execute a process
    /// </summary>
    public class ProcessExecution : IProcessExecution
    {
        /// <summary>
        /// Executes the given process with the given arguments.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The arguments pass to the command.</param>
        /// <param name="timeout">An optional parameter indicating the timeout for the program to complete. Null indicates no timeout.</param>
        /// <param name="workingDir">Path to the working directory</param>
        /// <param name="redirectErrorAndOutput">Indicates whether the output and error should be redirected</param>
        /// <returns>The return code of the process.</returns>
        public Task<ProcessExecutionResult> ExecuteProcessAsync(string command, IEnumerable<string> arguments, TimeSpan? timeout = null, string workingDir = null, bool redirectErrorAndOutput = true)
        {
            return this.ExecuteProcessAsync(command, string.Join(" ", arguments), timeout, workingDir);
        }

        /// <summary>
        /// Executes the given process with the given arguments.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The arguments pass to the command.</param>
        /// <param name="timeout">An optional parameter indicating the timeout for the program to complete. Null indicates no timeout.</param>
        /// <param name="workingDir">Path to the working directory</param>
        /// <param name="redirectErrorAndOutput">Indicates whether the output and error should be redirected</param>
        /// <returns>The return code of the process.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "working dir change only")]
        public Task<ProcessExecutionResult> ExecuteProcessAsync(string command, string arguments, TimeSpan? timeout = null, string workingDir = null, bool redirectErrorAndOutput = true)
        {
            return Task.Factory.StartNew<ProcessExecutionResult>(
            () =>
            {
                ProcessExecutionResult result = new ProcessExecutionResult();

                // Process.WaitForExit treats -1 as an infinite timeout
                int timeoutMs = ((int?)timeout?.TotalMilliseconds) ?? -1;
                var workingDirectory = workingDir == null ? Path.GetTempPath() : workingDir;
                using (Process process = new Process())
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardError = redirectErrorAndOutput,
                        RedirectStandardOutput = redirectErrorAndOutput,
                        Verb = "runas"
                    };

                    process.StartInfo = startInfo;

                    if (redirectErrorAndOutput)
                    {
                        process.OutputDataReceived += (sender, args) =>
                        {
                            if (!string.IsNullOrWhiteSpace(args.Data))
                            {
                                result.Output.Add(args.Data);
                            }
                        };

                        process.ErrorDataReceived += (sender, args) =>
                        {
                            if (!string.IsNullOrWhiteSpace(args.Data))
                            {
                                result.Error.Add(args.Data);
                            }
                        };
                    }

                    if (process.Start())
                    {
                        if (redirectErrorAndOutput)
                        {
                            // Start asynchronous reading of output, as opposed to waiting until process closes.
                            process.BeginOutputReadLine();
                            process.BeginErrorReadLine();
                        }

                        if (process.WaitForExit(timeoutMs))
                        {
                            result.ExitCode = process.ExitCode;
                        }
                        else
                        {
                            result.TimedOut = true;
                        }

                        process.Close();
                    }
                }

                return result;
            },
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        }

        /// <inheritdoc/>
        public int CreateProcess(string command, string arguments, string workingDirectory, EventHandler customExitAction, IList<string> redirectOutput = null)
        {
            command.ThrowIfNullOrWhiteSpace(nameof(command));
            arguments.ThrowIfNullOrWhiteSpace(nameof(arguments));
            workingDirectory.ThrowIfNullOrWhiteSpace(nameof(workingDirectory));
            customExitAction.ThrowIfNull(nameof(customExitAction));

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = redirectOutput != null,
                RedirectStandardOutput = redirectOutput != null,
                Verb = "runas"
            };

            process.StartInfo = startInfo;

            if (redirectOutput != null)
            {
                process.OutputDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        redirectOutput.Add(args.Data);
                    }
                };

                process.ErrorDataReceived += (sender, args) =>
                {
                    if (!string.IsNullOrWhiteSpace(args.Data))
                    {
                        redirectOutput.Add(args.Data);
                    }
                };
            }

            if (process.Start() && redirectOutput != null)
            {
                // Start asynchronous reading of output, as opposed to waiting until process closes.
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }

            process.EnableRaisingEvents = true;
            process.Exited += customExitAction;

            return process.Id;
        }

        /// <inheritdoc/>
        public async Task<ProcessExecutionResult> ExecuteProcessAsync<TError>(string command, string arguments, string errorMessage = null, TimeSpan? timeout = null)
            where TError : Exception
        {
            ProcessExecutionResult result = await this.ExecuteProcessAsync(command, arguments, timeout).ConfigureAwait(false);
            result.ThrowIfErrored<TError>(errorMessage);
            return result;
        }

        /// <inheritdoc/>
        public async Task<ProcessExecutionResult> ExecuteProcessAsync<TError>(string command, IEnumerable<string> arguments, string errorMessage = null, TimeSpan? timeout = null)
            where TError : Exception
        {
            ProcessExecutionResult result = await this.ExecuteProcessAsync(command, arguments, timeout).ConfigureAwait(false);
            result.ThrowIfErrored<TError>(errorMessage);
            return result;
        }
    }
}