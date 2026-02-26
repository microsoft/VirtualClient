// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// A mock/test process manager.
    /// </summary>
    public class InMemoryProcessManager : ProcessManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryProcessManager"/> class.
        /// </summary>
        public InMemoryProcessManager(PlatformID platform)
        {
            this.Platform = platform;
            this.Processes = new List<IProcessProxy>();
        }

        /// <summary>
        /// The list of all commands associated with processes executed.
        /// </summary>
        public IEnumerable<string> Commands
        {
            get
            {
                return this.Processes.Select(proc => $"{proc.StartInfo.FileName} {proc.StartInfo.Arguments}".Trim());
            }
        }

        /// <summary>
        /// The set of processes created by the test process manager.
        /// </summary>
        public IEnumerable<IProcessProxy> Processes { get; }

        /// <summary>
        /// Delegate allows user to control the <see cref="IProcessProxy"/> that is provided
        /// to the test.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="string"/> command - The command to execute.</item>
        /// <item><see cref="string"/> arguments - The arguments to pass to the command on the command line.</item>
        /// <item><see cref="string"/> workingDir - The working directory for the command execution.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Func<string, string, string, IProcessProxy> OnCreateProcess { get; set; }

        /// <summary>
        /// Delegate allows user to control the <see cref="IProcessProxy"/> that is provided
        /// to the test.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="int"/> process ID.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Func<int, IProcessProxy> OnGetProcess { get; set; }

        /// <summary>
        /// Delegate allows user to make any changes to the <see cref="IProcessProxy"/> that is provided
        /// to the test.
        /// <list>
        /// <item>Parameters:</item>
        /// <list type="bullet">
        /// <item><see cref="IProcessProxy"/> process - The process that was created.</item>
        /// </list>
        /// </list>
        /// </summary>
        public Action<IProcessProxy> OnProcessCreated { get; set; }

        /// <inheritdoc />
        public override PlatformID Platform { get; }

        /// <summary>
        /// Gets the process tracking instance. Only populated after <see cref="TrackProcesses"/> is called.
        /// </summary>
        public FixtureTracking Tracking { get; private set; }

        /// <summary>
        /// Enables automatic tracking of all process executions. Wraps <see cref="OnCreateProcess"/> to
        /// record each execution in a <see cref="FixtureTracking"/> instance.
        /// </summary>
        /// <param name="reset">If true, clears any previously tracked commands.</param>
        public InMemoryProcessManager TrackProcesses(bool reset = true)
        {
            if (this.Tracking == null || reset)
            {
                this.Tracking = new FixtureTracking();
            }

            Func<string, string, string, IProcessProxy> existingHandler = this.OnCreateProcess;

            this.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                IProcessProxy process = existingHandler != null
                    ? existingHandler(command, arguments, workingDirectory)
                    : this.CreateDefaultProcess(command, arguments, workingDirectory);

                this.Tracking.AddCommand(new CommandExecutionInfo(
                    command,
                    arguments,
                    workingDirectory,
                    process,
                    DateTime.UtcNow));

                return process;
            };

            return this;
        }

        /// <summary>
        /// Sets up automatic output for processes whose full command line matches
        /// <paramref name="commandPattern"/>. Wraps any existing <see cref="OnCreateProcess"/> handler.
        /// </summary>
        /// <param name="commandPattern">Regex pattern (or plain substring) matched against the full command.</param>
        /// <param name="standardOutput">Standard output to inject into matching processes.</param>
        /// <param name="standardError">Standard error to inject into matching processes (optional).</param>
        /// <param name="exitCode">Exit code for matching processes (default: 0).</param>
        public InMemoryProcessManager SetupProcessOutput(
            string commandPattern,
            string standardOutput,
            string standardError = null,
            int exitCode = 0)
        {
            commandPattern.ThrowIfNullOrWhiteSpace(nameof(commandPattern));

            Func<string, string, string, IProcessProxy> existingHandler = this.OnCreateProcess;

            this.OnCreateProcess = (command, arguments, workingDirectory) =>
            {
                IProcessProxy process = existingHandler != null
                    ? existingHandler(command, arguments, workingDirectory)
                    : this.CreateDefaultProcess(command, arguments, workingDirectory);

                string fullCommand = string.IsNullOrEmpty(arguments)
                    ? command
                    : $"{command} {arguments}";

                bool matches;
                try
                {
                    matches = Regex.IsMatch(fullCommand, commandPattern, RegexOptions.IgnoreCase);
                }
                catch
                {
                    matches = fullCommand.Contains(commandPattern, StringComparison.OrdinalIgnoreCase);
                }

                if (matches && process is InMemoryProcess inMemoryProcess)
                {
                    // Inject output/error into the existing process so that any tracking
                    // wrapper already holding a reference to it sees the populated buffers.
                    inMemoryProcess.ExitCode = exitCode;

                    if (!string.IsNullOrEmpty(standardOutput))
                    {
                        inMemoryProcess.StandardOutput.Append(standardOutput);
                    }

                    if (!string.IsNullOrEmpty(standardError))
                    {
                        inMemoryProcess.StandardError.Append(standardError);
                    }
                }

                return process;
            };

            return this;
        }

        /// <inheritdoc />
        public override IProcessProxy CreateProcess(string command, string arguments = null, string workingDir = null)
        {
            IProcessProxy process = this.OnCreateProcess != null
                ? this.OnCreateProcess.Invoke(command, arguments, workingDir)
                : this.CreateDefaultProcess(command, arguments, workingDir);

            (this.Processes as List<IProcessProxy>).Add(process);
            this.OnProcessCreated?.Invoke(process);

            return process;
        }

        /// <inheritdoc />
        public override IProcessProxy GetProcess(int processId)
        {
            IProcessProxy process = null;
            if (this.OnGetProcess != null)
            {
                process = this.OnGetProcess.Invoke(processId);
            }
            else
            {
                process = this.Processes?.FirstOrDefault(p => p.Id == processId);
            }

            return process;
        }

        private IProcessProxy CreateDefaultProcess(string command, string arguments, string workingDirectory)
        {
            InMemoryProcess process = new InMemoryProcess
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory ?? (this.Platform == PlatformID.Unix
                        ? Path.GetDirectoryName(command).Replace('\\', '/')
                        : Path.GetDirectoryName(command))
                },
                OnHasExited = () => true,
                OnStart = () => true
            };

            return process;
        }
    }
}
