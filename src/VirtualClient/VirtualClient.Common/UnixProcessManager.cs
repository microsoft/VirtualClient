// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System.Diagnostics;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides methods for creating and managing processes on a Linux/Unix system.
    /// </summary>
    public class UnixProcessManager : ProcessManager
    {
        /// <inheritdoc />
        public override IProcessProxy CreateProcess(string command, string arguments = null, string workingDir = null)
        {
            command.ThrowIfNullOrWhiteSpace(nameof(command));

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            };

            return new ProcessProxy(process);
        }
    }
}
