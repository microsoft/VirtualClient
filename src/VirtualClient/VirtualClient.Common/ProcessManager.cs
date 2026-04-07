// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// Provides methods for creating and managing processes on the system.
    /// </summary>
    public abstract class ProcessManager
    {
        /// <summary>
        /// The OS platform for the current runtime.
        /// </summary>
        public abstract PlatformID Platform { get; }

        /// <summary>
        /// Creates a process manager for the OS/system.
        /// </summary>
        /// <param name="platform">The OS/system platform (e.g. Windows, Linux).</param>
        public static ProcessManager Create(PlatformID platform)
        {
            ProcessManager manager = null;
            switch (platform)
            {
                case PlatformID.Win32NT:
                    manager = new WindowsProcessManager();
                    break;

                case PlatformID.Unix:
                    manager = new UnixProcessManager();
                    break;

                default:
                    throw new NotSupportedException($"Process management is not supported for '{platform}' platforms.");
            }

            return manager;
        }

        /// <summary>
        /// Creates a process on the system.
        /// </summary>
        /// <param name="command">The command to execute.</param>
        /// <param name="arguments">The arguments pass to the command.</param>
        /// <param name="workingDir">Path to the working directory</param>
        public abstract IProcessProxy CreateProcess(string command, string arguments = null, string workingDir = null);

        /// <summary>
        /// Returns the process with a matching ID.
        /// </summary>
        /// <param name="processId">The ID of the process on the system.</param>
        public virtual IProcessProxy GetProcess(int processId)
        {
            IProcessProxy process = null;

            try
            {
                process = new ProcessProxy(Process.GetProcessById(processId));
            }
            catch (ArgumentException)
            {
                // Process is not running and does not exist.
            }

            return process;
        }

        /// <summary>
        /// Returns a set of 1 or more processes with a matching name.
        /// </summary>
        /// <param name="processName">The name of the process(es) on the system.</param>
        public virtual IEnumerable<IProcessProxy> GetProcesses(string processName)
        {
            IEnumerable<IProcessProxy> processes = null;
            Process[] runningProcesses = Process.GetProcessesByName(processName);

            if (runningProcesses?.Any() == true)
            {
                processes = runningProcesses.Select(process => new ProcessProxy(process));
            }

            return processes;
        }
    }
}
