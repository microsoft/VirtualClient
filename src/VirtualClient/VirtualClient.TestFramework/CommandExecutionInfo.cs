// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using VirtualClient.Common;

    /// <summary>
    /// Represents information about a command/process execution captured during testing.
    /// </summary>
    public class CommandExecutionInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommandExecutionInfo"/> class.
        /// </summary>
        /// <param name="command">The command/executable that was run.</param>
        /// <param name="arguments">The command-line arguments.</param>
        /// <param name="workingDirectory">The working directory for the command execution.</param>
        /// <param name="process">The process proxy associated with the execution.</param>
        /// <param name="executionTime">The time at which the command was executed.</param>
        public CommandExecutionInfo(
            string command,
            string arguments,
            string workingDirectory,
            IProcessProxy process,
            DateTime executionTime)
        {
            this.Command = command;
            this.Arguments = arguments;
            this.WorkingDirectory = workingDirectory;
            this.Process = process;
            this.ExecutionTime = executionTime;
        }

        /// <summary>
        /// The command/executable that was run.
        /// </summary>
        public string Command { get; }

        /// <summary>
        /// The command-line arguments.
        /// </summary>
        public string Arguments { get; }

        /// <summary>
        /// The working directory for the command execution.
        /// </summary>
        public string WorkingDirectory { get; }

        /// <summary>
        /// The full command string (command + arguments).
        /// </summary>
        public string FullCommand => string.IsNullOrEmpty(this.Arguments)
            ? this.Command
            : $"{this.Command} {this.Arguments}";

        /// <summary>
        /// The process proxy associated with the execution.
        /// </summary>
        public IProcessProxy Process { get; }

        /// <summary>
        /// The time at which the command was executed.
        /// </summary>
        public DateTime ExecutionTime { get; }

        /// <summary>
        /// Exit code of the process.
        /// </summary>
        public int ExitCode => this.Process?.ExitCode ?? 0;

        /// <summary>
        /// Standard output from the process.
        /// </summary>
        public string StandardOutput => this.Process?.StandardOutput?.ToString();

        /// <summary>
        /// Standard error from the process.
        /// </summary>
        public string StandardError => this.Process?.StandardError?.ToString();
    }
}
