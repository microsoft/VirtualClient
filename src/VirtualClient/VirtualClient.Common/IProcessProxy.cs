// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Acts as a limited proxy to provide information about a process running
    /// on the local system.
    /// </summary>
    public interface IProcessProxy : IDisposable
    {
        /// <summary>
        /// Gets the ID of the underlying process.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// Gets the name of the underlying process.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the set of environment variables for the process/session.
        /// </summary>
        StringDictionary EnvironmentVariables { get; }

        /// <summary>
        /// Gets the exit code for the underlying process.
        /// </summary>
        int ExitCode { get; }

        /// <summary>
        /// Gets the exit time for the underlying process.
        /// </summary>
        DateTime ExitTime { get; set; }

        /// <summary>
        /// The underlying process details.
        /// </summary>
        ProcessDetails ProcessDetails { get; }

        /// <summary>
        /// A pointer to the process module control handle.
        /// </summary>
        IntPtr? Handle { get; }

        /// <summary>
        /// Gets true/false whether the underlying process has exited.
        /// </summary>
        bool HasExited { get; }

        /// <summary>
        /// True/false whether standard error from the underlying proces should
        /// be redirected. Note that this MUST be set before the process is started.
        /// </summary>
        bool RedirectStandardError { get; set; }

        /// <summary>
        /// True/false whether standard output from the underlying proces should
        /// be redirected. Note that this MUST be set before the process is started.
        /// </summary>
        bool RedirectStandardInput { get; set; }

        /// <summary>
        /// True/false whether standard output from the underlying proces should
        /// be redirected. Note that this MUST be set before the process is started.
        /// </summary>
        bool RedirectStandardOutput { get; set; }

        /// <summary>
        /// Standard error stream for the process.
        /// </summary>
        ConcurrentBuffer StandardError { get; }

        /// <summary>
        /// Standard error stream for the process.
        /// </summary>
        ConcurrentBuffer StandardOutput { get; }

        /// <summary>
        /// Standard input for the process.
        /// </summary>
        StreamWriter StandardInput { get; }

        /// <summary>
        /// Gets the process start information.
        /// </summary>
        ProcessStartInfo StartInfo { get; }

        /// <summary>
        ///  Gets the start time for the underlying process.
        /// </summary>
        DateTime StartTime { get; set; }

        /// <summary>
        /// Gracefully closes the process and main window.
        /// </summary>
        void Close();

        /// <summary>
        /// Promptly terminates/kills the underlying process without waiting for a
        /// graceful exit.
        /// </summary>
        void Kill();

        /// <summary>
        /// Promptly terminates/kills the underlying process along with entire 
        /// processtree without waiting for a graceful exit.
        /// </summary>
        void Kill(bool entireProcessTree);

        /// <summary>
        /// Starts the underlying process.
        /// </summary>
        bool Start();

        /// <summary>
        /// Wait for process to exit.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="timeout">
        /// An absolute timeout to apply for the case that the process does not finish in the amount of time expected. If the
        /// timeout is reached a <see cref="TimeoutException"/> exception will be thrown.
        /// </param>
        Task WaitForExitAsync(CancellationToken cancellationToken, TimeSpan? timeout = null);

        /// <summary>
        /// Writes input to the standard input stream.
        /// </summary>
        /// <param name="input">The input to write.</param>
        IProcessProxy WriteInput(string input);
    }
}