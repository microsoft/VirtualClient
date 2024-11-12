// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
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
        /// <param name="username">The username to use for running the command.</param>
        public static IProcessProxy CreateElevatedProcess(this ProcessManager processManager, PlatformID platform, string command, string arguments = null, string workingDir = null, string username = null)
        {
            IProcessProxy process = null;
            switch (platform)
            {
                case PlatformID.Unix:
                    string effectiveCommandArguments = arguments;
                    string effectiveCommand = command;
                    if (!string.Equals(command, "sudo") && !PlatformSpecifics.RunningInContainer)
                    {
                        if (string.IsNullOrWhiteSpace(username))
                        {
                            effectiveCommandArguments = $"{command} {arguments}";
                        }
                        else
                        {
                            effectiveCommandArguments = $"-u {username} {command} {arguments}";
                        }

                        effectiveCommand = "sudo";
                    }

                    process = processManager.CreateProcess(effectiveCommand, effectiveCommandArguments?.Trim(), workingDir);
                    break;

                default:
                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        throw new NotSupportedException($"The application of a username is not supported on '{platform}' platform/architecture systems.");
                    }

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
        /// True if the process returned a non-success exit code.
        /// </summary>
        /// <param name="process">Represents a process running on the system.</param>
        /// <param name="successCodes">The set of exit codes that indicate success.</param>
        public static bool IsErrored(this IProcessProxy process, IEnumerable<int> successCodes = null)
        {
            process.ThrowIfNull(nameof(process));
            return process.HasExited && !(successCodes ?? ProcessProxy.DefaultSuccessCodes).Contains(process.ExitCode);
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
        /// Throws an exception when a dependency installation process has exited and the exit code does not match
        /// one of the default success exit codes.
        /// </summary>
        /// <param name="process">Represents a process running on the system.</param>
        /// <param name="errorMessage">An optional error message to use instead of the default.</param>
        /// <param name="errorReason">The reason/category of the error.</param>
        public static void ThrowIfDependencyInstallationFailed(this IProcessProxy process, string errorMessage = null, ErrorReason errorReason = ErrorReason.DependencyInstallationFailed)
        {
            process.ThrowIfNull(nameof(process));
            process.ThrowIfErrored<DependencyException>(ProcessProxy.DefaultSuccessCodes, errorMessage ?? "Dependency installation process failed.", errorReason);
        }

        /// <summary>
        /// Throws an exception when a dependency installation process has exited and the exit code does not match
        /// one of the default success exit codes.
        /// </summary>
        /// <param name="process">Represents a process running on the system.</param>
        /// <param name="successCodes">The set of exit codes that indicate success.</param>
        /// <param name="errorMessage">An optional error message to use instead of the default.</param>
        /// <param name="errorReason">The reason/category of the error.</param>
        public static void ThrowIfDependencyInstallationFailed(this IProcessProxy process, IEnumerable<int> successCodes, string errorMessage = null, ErrorReason errorReason = ErrorReason.DependencyInstallationFailed)
        {
            successCodes.ThrowIfNullOrEmpty(nameof(successCodes));
            process.ThrowIfNull(nameof(process));
            process.ThrowIfErrored<DependencyException>(successCodes, errorMessage ?? "Dependency installation process failed.", errorReason);
        }

        /// <summary>
        /// Throws an exception if the process has exited and the exit code does not match
        /// one of the default success exit codes.
        /// </summary>
        /// <param name="process">Represents a process running on the system.</param>
        /// <param name="errorMessage">An optional error message to use instead of the default.</param>
        /// <param name="errorReason">The reason/category of the error.</param>
        public static void ThrowIfErrored<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TError>(this IProcessProxy process, string errorMessage = null, ErrorReason errorReason = ErrorReason.Undefined)
            where TError : VirtualClientException
        {
            process.ThrowIfNull(nameof(process));
            process.ThrowIfErrored<TError>(ProcessProxy.DefaultSuccessCodes, errorMessage, errorReason);
        }

        /// <summary>
        /// Throws an exception if the process has exited and the exit code does not match
        /// one of the success exit codes provided.
        /// </summary>
        /// <param name="process">Represents a process running on the system.</param>
        /// <param name="successCodes">The set of exit codes that indicate success.</param>
        /// <param name="errorMessage">An optional error message to use instead of the default.</param>
        /// <param name="errorReason">The reason/category of the error.</param>
        public static void ThrowIfErrored<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TError>(this IProcessProxy process, IEnumerable<int> successCodes, string errorMessage = null, ErrorReason errorReason = ErrorReason.Undefined)
            where TError : VirtualClientException
        {
            process.ThrowIfNull(nameof(process));
            successCodes.ThrowIfNullOrEmpty(nameof(successCodes));

            if (process.HasExited && !successCodes.Contains(process.ExitCode))
            {
                process.ThrowErrored<TError>(errorMessage, errorReason);        
            }
        }

        /// <summary>
        /// Throws an exception when a monitor process has exited and the exit code does not match
        /// one of the default success exit codes.
        /// </summary>
        /// <param name="process">Represents a process running on the system.</param>
        /// <param name="errorMessage">An optional error message to use instead of the default.</param>
        /// <param name="errorReason">The reason/category of the error.</param>
        public static void ThrowIfMonitorFailed(this IProcessProxy process, string errorMessage = null, ErrorReason errorReason = ErrorReason.MonitorFailed)
        {
            process.ThrowIfNull(nameof(process));
            process.ThrowIfErrored<MonitorException>(ProcessProxy.DefaultSuccessCodes, errorMessage ?? "Monitor process execution failed.", errorReason);
        }

        /// <summary>
        /// Throws an exception when a monitor process has exited and the exit code does not match
        /// one of the default success exit codes.
        /// </summary>
        /// <param name="process">Represents a process running on the system.</param>
        /// <param name="successCodes">The set of exit codes that indicate success.</param>
        /// <param name="errorMessage">An optional error message to use instead of the default.</param>
        /// <param name="errorReason">The reason/category of the error.</param>
        public static void ThrowIfMonitorFailed(this IProcessProxy process, IEnumerable<int> successCodes, string errorMessage = null, ErrorReason errorReason = ErrorReason.MonitorFailed)
        {
            successCodes.ThrowIfNullOrEmpty(nameof(successCodes));
            process.ThrowIfNull(nameof(process));
            process.ThrowIfErrored<MonitorException>(successCodes, errorMessage ?? "Monitor process execution failed.", errorReason);
        }

        /// <summary>
        /// Throws an exception when a workload process has exited and the exit code does not match
        /// one of the default success exit codes.
        /// </summary>
        /// <param name="process">Represents a process running on the system.</param>
        /// <param name="errorMessage">An optional error message to use instead of the default.</param>
        /// <param name="errorReason">The reason/category of the error.</param>
        public static void ThrowIfWorkloadFailed(this IProcessProxy process, string errorMessage = null, ErrorReason errorReason = ErrorReason.WorkloadFailed)
        {
            process.ThrowIfNull(nameof(process));
            process.ThrowIfErrored<WorkloadException>(ProcessProxy.DefaultSuccessCodes, errorMessage ?? "Workload process execution failed.", errorReason);
        }

        /// <summary>
        /// Throws an exception when a workload process has exited and the exit code does not match
        /// one of the default success exit codes.
        /// </summary>
        /// <param name="process">Represents a process running on the system.</param>
        /// <param name="successCodes">The set of exit codes that indicate success.</param>
        /// <param name="errorMessage">An optional error message to use instead of the default.</param>
        /// <param name="errorReason">The reason/category of the error.</param>
        public static void ThrowIfWorkloadFailed(this IProcessProxy process, IEnumerable<int> successCodes, string errorMessage = null, ErrorReason errorReason = ErrorReason.WorkloadFailed)
        {
            successCodes.ThrowIfNullOrEmpty(nameof(successCodes));
            process.ThrowIfNull(nameof(process));
            process.ThrowIfErrored<WorkloadException>(successCodes, errorMessage ?? "Workload process execution failed.", errorReason);
        }

        /// <summary>
        /// Throws an exception if the process has received any error information in standard error.
        /// </summary>
        /// <param name="process">Represents a process running on the system.</param>
        /// <param name="errorMessage">Represents a process running on the system.</param>
        /// <param name="errorReason">Represents a process running on the system.</param>
        public static void ThrowOnStandardError<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TError>(this IProcessProxy process, string errorMessage = null, ErrorReason errorReason = ErrorReason.Undefined)
            where TError : VirtualClientException
        {
            process.ThrowIfNull(nameof(process));
            string standardError = process.StandardError?.ToString().Trim();

            if (!string.IsNullOrWhiteSpace(standardError))
            {
                try
                {
                    process.ThrowErrored<TError>(errorMessage, errorReason);
                }
                catch (MissingMethodException)
                {
                    throw new MissingMethodException(
                        $"The exception type provided '{typeof(TError).FullName}' does not have a constructor that takes in a single 'message' parameter.");
                }
            }
        }

        /// <summary>
        /// Throws an exception if the process has received any error information in standard error.
        /// </summary>
        /// <param name="process">Represents a process running on the system.</param>
        /// <param name="errorMessage">Represents a process running on the system.</param>
        /// <param name="errorReason">Represents a process running on the system.</param>
        /// <param name="expressions">A set of expressions to use for matching the contents of standard errors that represents failure cases.</param>
        public static void ThrowOnStandardError<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TError>(this IProcessProxy process, string errorMessage = null, ErrorReason errorReason = ErrorReason.Undefined, params Regex[] expressions)
            where TError : VirtualClientException
        {
            process.ThrowIfNull(nameof(process));
            if (expressions?.Any() == true)
            {
                string standardError = process.StandardError?.ToString().Trim();

                if (!string.IsNullOrWhiteSpace(standardError))
                {
                    try
                    {
                        foreach (Regex expression in expressions)
                        {
                            if (expression.IsMatch(standardError))
                            {
                                process.ThrowErrored<TError>(errorMessage, errorReason);
                            }
                        }
                    }
                    catch (MissingMethodException)
                    {
                        throw new MissingMethodException(
                            $"The exception type provided '{typeof(TError).FullName}' does not have a constructor that takes in a single 'message' parameter.");
                    }
                }
            }
        }

        private static void ThrowErrored<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TError>(this IProcessProxy process, string errorMessage, ErrorReason errorReason)
            where TError : VirtualClientException
        {
            process.ThrowIfNull(nameof(process));

            string error = null;
            string command = string.Empty;
            if (process.StartInfo != null)
            {
                command = !string.IsNullOrWhiteSpace(process.StartInfo.Arguments)
                ? $"{process.StartInfo.FileName} {process.StartInfo.Arguments}"
                : $"{process.StartInfo.FileName}";
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                error = $"{errorMessage.Trim().TrimEnd('.')} (error/exit code={process.ExitCode}, command={command}).";
            }
            else
            {
                error = $"Process execution failed (error/exit code={process.ExitCode}, command={command}).";
            }

            if (process.StandardOutput?.Length > 0)
            {
                error = $"{error}{Environment.NewLine}{Environment.NewLine}{"StandardOutput: "}{process.StandardOutput}";
            }

            if (process.StandardError?.Length > 0)
            {
                error = $"{error}{Environment.NewLine}{Environment.NewLine}{"StandardError: "}{process.StandardError}";
            }

            try
            {
                TError exception = (TError)Activator.CreateInstance(typeof(TError), error, errorReason);
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
