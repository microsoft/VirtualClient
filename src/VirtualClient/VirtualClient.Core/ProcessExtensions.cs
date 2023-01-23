// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for process-related components.
    /// </summary>
    public static class ProcessExtensions
    {
        /// <summary>
        /// Creates process that can be run with elevated privileges.
        /// </summary>
        /// <param name="processManager">The process manager used to create the process.</param>
        /// <param name="platform">The OS platform.</param>
        /// <param name="command">The command to run.</param>
        /// <param name="arguments">The command line arguments to supply to the command.</param>
        /// <param name="workingDir">The working directory for the command.</param>
        public static IProcessProxy CreateElevatedProcess(this ProcessManager processManager, PlatformID platform, string command, string arguments = null, string workingDir = null)
        {
            IProcessProxy process = null;
            switch (platform)
            {
                case PlatformID.Unix:
                    string effectiveCommandArguments = arguments;
                    string effectiveCommand = command;
                    if (!string.Equals(command, "sudo") && !PlatformSpecifics.IsRunningInContainer())
                    {
                        effectiveCommandArguments = $"{command} {arguments}";
                        effectiveCommand = "sudo";
                    }

                    process = processManager.CreateProcess(effectiveCommand, effectiveCommandArguments?.Trim(), workingDir);
                    break;

                default:
                    process = processManager.CreateProcess(command, arguments?.Trim(), workingDir);
                    break;
            }

            return process;
        }

        /// <summary>
        /// Returns the full command including arguments executed within the process.
        /// </summary>
        public static string FullCommand(this IProcessProxy process)
        {
            return $"{process.StartInfo.FileName} {process.StartInfo.Arguments}".Trim();
        }

        /// <summary>
        /// Kills the process if it is still running and handles any errors that
        /// can occurs if the process has gone out of scope.
        /// </summary>
        /// <param name="process">The process to kill.</param>
        /// <param name="logger">The logger to use to write trace information.</param>
        public static void SafeKill(this IProcessProxy process, ILogger logger = null)
        {
            if (process != null)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception exc)
                {
                    // Best effort here.
                    logger?.LogTraceMessage($"Kill Process Failure. Error = {exc.Message}");
                }
            }
        }

        /// <summary>
        /// Kills the associated process and it's child/dependent processes if it is still running and 
        /// handles any errors that can occurs if the process has gone out of scope.
        /// </summary>
        /// <param name="process">The process to kill.</param>
        /// <param name="entireProcessTree">true to kill asociated process and it's descendents, false to only kill the process.</param>
        /// <param name="logger">The logger to use to write trace information.</param>
        public static void SafeKill(this IProcessProxy process, bool entireProcessTree, ILogger logger = null)
        {
            if (process != null)
            {
                try
                {
                    process.Kill(entireProcessTree);
                }
                catch (Exception exc)
                {
                    // Best effort here.
                    logger?.LogTraceMessage($"Kill Process Failure. Error = {exc.Message}");
                }
            }
        }

        /// <summary>
        /// Kills any processes that are defined and handles any errors that
        /// can occurs if the process has gone out of scope.
        /// </summary>
        /// <param name="processManager">The process manager used to find the processes.</param>
        /// <param name="processNames">The names/paths of the processes to kill.</param>
        /// <param name="logger">The logger to use to write trace information.</param>
        public static void SafeKill(this ProcessManager processManager, IEnumerable<string> processNames, ILogger logger = null)
        {
            processManager.ThrowIfNull(nameof(processManager));

            if (processNames?.Any() == true)
            {
                foreach (string process in processNames)
                {
                    try
                    {
                        // Processes are named without the extensions (e.g. VirtualClient not VirtualClient.exe).
                        string processName = Path.GetFileNameWithoutExtension(process);
                        IEnumerable<IProcessProxy> processes = processManager.GetProcesses(processName);

                        if (processes?.Any() == true)
                        {
                            foreach (IProcessProxy sideProcess in processes)
                            {
                                sideProcess.SafeKill(logger);
                            }
                        }
                    }
                    catch
                    {
                        // best effort
                    }
                }
            }
        }

        /// <summary>
        /// Throws an exception if the process has exited and the exit code does not match
        /// one of the success exit codes provided.
        /// </summary>
        /// <param name="process">Represents a process running on the system.</param>
        /// <param name="successCodes">The set of exit codes that indicate success.</param>
        /// <param name="errorMessage">An optional error message to use instead of the default.</param>
        /// <param name="errorReason">The reason/category of the error.</param>
        public static void ThrowIfErrored<TError>(this IProcessProxy process, IEnumerable<int> successCodes, string errorMessage = null, ErrorReason errorReason = ErrorReason.Undefined)
            where TError : VirtualClientException
        {
            process.ThrowIfNull(nameof(process));
            successCodes.ThrowIfNullOrEmpty(nameof(successCodes));

            if (process.HasExited && !successCodes.Contains(process.ExitCode))
            {
                TError exception = default(TError);
                string error = null;
                string command = !string.IsNullOrWhiteSpace(process.StartInfo.Arguments)
                    ? $"{process.StartInfo.FileName} {process.StartInfo.Arguments}"
                    : $"{process.StartInfo.FileName}";

                if (errorMessage != null)
                {
                    error = $"{errorMessage.Trim().TrimEnd('.')} (error/exit code={process.ExitCode}, command={command}).";
                }
                else
                {
                    error = $"Process execution failed (error/exit code={process.ExitCode}, command={command}).";
                }

                if (process.StandardOutput?.Length > 0)
                {
                    error = $"{error}{Environment.NewLine}{Environment.NewLine}{"StandardOutput : "}{process.StandardOutput}";
                }

                if (process.StandardError?.Length > 0)
                {
                    error = $"{error}{Environment.NewLine}{Environment.NewLine}{"StandardError : "}{process.StandardError}";
                }

                try
                {
                    exception = (TError)Activator.CreateInstance(typeof(TError), error, errorReason);
                    throw exception;
                }
                catch (MissingMethodException)
                {
                    throw new MissingMethodException(
                        $"The exception type provided '{typeof(TError).FullName}' does not have a constructor that takes in the parameters supplied.");
                }
            }
        }
    }
}
