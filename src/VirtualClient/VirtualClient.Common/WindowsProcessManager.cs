// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Common
{
    using System.Diagnostics;
    using System.IO;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Provides methods for creating and managing processes on a Windows system.
    /// </summary>
    public class WindowsProcessManager : ProcessManager
    {
        /// <inheritdoc />
        public override IProcessProxy CreateProcess(string command, string arguments = null, string workingDir = null)
        {
            command.ThrowIfNullOrWhiteSpace(nameof(command));

            return new ProcessProxy(new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    WorkingDirectory = workingDir ?? Path.GetDirectoryName(command),
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                }
            });
        }
    }
}
