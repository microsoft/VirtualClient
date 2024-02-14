// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using VirtualClient.Common;

    /// <summary>
    /// A mock/test process manager.
    /// </summary>
    public class InMemoryProcessManager : ProcessManager
    {
        private PlatformID platform;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryProcessManager"/> class.
        /// </summary>
        public InMemoryProcessManager(PlatformID platform)
        {
            this.platform = platform;
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
        public override IProcessProxy CreateProcess(string command, string arguments = null, string workingDir = null)
        {
            IProcessProxy process = null;
            if (this.OnCreateProcess != null)
            {
                process = this.OnCreateProcess?.Invoke(command, arguments, workingDir);
            }
            else
            {
                process = new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = arguments,
                        WorkingDirectory = workingDir ?? (this.platform == PlatformID.Unix 
                            ? Path.GetDirectoryName(command).Replace('\\', '/')
                            : Path.GetDirectoryName(command))
                    }
                };

                (process as InMemoryProcess).OnHasExited = () => true;
                (process as InMemoryProcess).OnStart = () => true;
            }

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
    }
}
