// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// Describes a class which can execute a process.
    /// </summary>
    public interface IProcessExecution
    {
        /// <summary>
        /// Executes the given process with the given arguments.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The arguments pass to the command.</param>
        /// <param name="timeout">An optional parameter indicating the timeout for the program to complete. Null indicates no timeout.</param>
        /// <param name="workingDir">Path to the working directory</param>
        /// <param name="redirectErrorAndOutput">Indicates whether the output should be redirected</param>
        /// <returns>The return code of the process.</returns>
        Task<ProcessExecutionResult> ExecuteProcessAsync(string command, string arguments, TimeSpan? timeout = null, string workingDir = null, bool redirectErrorAndOutput = true);

        /// <summary>
        /// Creates and starts a Process with the given parameters.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The arguments pass to the command.</param>
        /// <param name="workingDir">Path to the working directory</param>
        /// <param name="redirectErrorAndOutput">Indicates whether the output should be redirected</param>
        /// <param name="customExitAction">Custom action to take when process exits.</param>
        /// <returns>Process Id of the created process.</returns>
        int CreateProcess(string command, string arguments, string workingDir, EventHandler customExitAction, IList<string> redirectErrorAndOutput = null);

        /// <summary>
        /// Executes the given process with the given arguments.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The arguments pass to the command.</param>
        /// <param name="timeout">An optional parameter indicating the timeout for the program to complete. Null indicates no timeout.</param>
        /// <param name="workingDir">Path to the working directory</param>
        /// <param name="redirectErrorAndutput">Indicates whether the output should be redirected</param>
        /// <returns>Process execution results which includes the return code.</returns>
        Task<ProcessExecutionResult> ExecuteProcessAsync(string command, IEnumerable<string> arguments, TimeSpan? timeout = null, string workingDir = null, bool redirectErrorAndutput = true);

        /// <summary>
        /// Executes the given process with the given arguments and throw exception upon failure.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The arguments pass to the command.</param>
        /// <param name="errorMessage">Error message if the result indicates failure.</param>
        /// <param name="timeout">An optional parameter indicating the timeout for the program to complete. Null indicates no timeout.</param>
        /// <returns>Process execution results which includes the return code.</returns>
        Task<ProcessExecutionResult> ExecuteProcessAsync<TError>(string command, string arguments, string errorMessage = null, TimeSpan? timeout = null)
            where TError : Exception;

        /// <summary>
        /// Executes the given process with the given arguments and throw exception upon failure.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The arguments pass to the command.</param>
        /// <param name="errorMessage">Error message if the result indicates failure.</param>
        /// <param name="timeout">An optional parameter indicating the timeout for the program to complete. Null indicates no timeout.</param>
        /// <returns>Process execution results which includes the return code.</returns>
        Task<ProcessExecutionResult> ExecuteProcessAsync<TError>(string command, IEnumerable<string> arguments, string errorMessage = null, TimeSpan? timeout = null)
            where TError : Exception;
    }
}