// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Executes a command on the system with the working directory set to a 
    /// package installed.
    /// </summary>
    public partial class ExecuteCommand : VirtualClientComponent
    {
        /// <summary>
        /// Execute the command(s) logic on the system using new logic for shell support.
        /// </summary>
        protected async Task ExecuteWithShellSupportAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                string commandToExecute = this.GetCommandToExecute();

                if (!string.IsNullOrWhiteSpace(commandToExecute))
                {
                    IDictionary<string, IConvertible> environmentVariables = this.EnvironmentVariables;
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        if (PlatformSpecifics.TryGetCommandParts(commandToExecute, out string effectiveCommand, out string effectiveCommandArguments))
                        {
                            await (this.RetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
                            {
                                // There appears to be an unfortunate implementation choice in .NET causing a Win32Exception similar to the following when
                                // referencing a binary and setting the working directory.
                                //
                                // System.ComponentModel.Win32Exception:
                                // 'An error occurred trying to start process 'Coreinfo64.exe' with working directory 'S:\microsoft\virtualclient\out\bin\Debug\AnyCPU\VirtualClient.Main\net9.0\packages\system_tools\win-x64'.
                                // The system cannot find the file specified.
                                //
                                // The .NET Process class does not reference the 'WorkingDirectory' when looking for the 'FileName' when UseShellExecute = false. The workaround
                                // for this is to add the working directory to the PATH environment variable.
                                string effectiveWorkingDirectory = this.WorkingDirectory;
                                if (!string.IsNullOrWhiteSpace(effectiveWorkingDirectory))
                                {
                                    this.PlatformSpecifics.SetEnvironmentVariable(
                                        EnvironmentVariable.PATH,
                                        effectiveWorkingDirectory,
                                        EnvironmentVariableTarget.Process,
                                        append: true);
                                }

                                using (IProcessProxy process = this.processManager.CreateProcess(effectiveCommand, effectiveCommandArguments, effectiveWorkingDirectory))
                                {
                                    this.AddEnvironmentVariables(process, environmentVariables);
                                    await process.StartAndWaitAsync(cancellationToken);

                                    if (!cancellationToken.IsCancellationRequested)
                                    {
                                        string logFolder = this.LogFolderName;
                                        if (string.IsNullOrWhiteSpace(logFolder) && PlatformSpecifics.TryGetCommandName(commandToExecute, out string commandName))
                                        {
                                            logFolder = commandName;
                                        }

                                        await this.LogProcessDetailsAsync(process, telemetryContext, toolName: logFolder, logFileName: this.LogFileName);
                                        process.ThrowIfComponentOperationFailed(this.ComponentType);
                                    }
                                }
                            });
                        }
                    }
                }
            }
        }

        private string GetCommandToExecute()
        {
            // There are a few command chaining scenarios we need to handle:
            // 1) Chaining that is not related to a shell (e.g. "execute_one.sh && execute_two.sh"). For these scenarios, we will execute these
            //    each in sequential order as separate processes.
            //
            // 2) For cases where a shell is referenced, we will execute the entire command as a single process and let the shell handle the chaining (e.g. "bash -c 'execute_one.sh && execute_two.sh'").

            string targetCommand = this.Command;
            if (this.UseShell)
            {
                // Using a shell (e.g. bash, cmd) allows support for command chaining and other shell features. The user can define the shell directly.
                // The 'UseShell' parameter is a flag for convenience and to support cross-platform scenarios in a single profile component definition.
                switch (this.Platform)
                {
                    case PlatformID.Unix:
                        targetCommand = $"bash -c \"{targetCommand.Trim(ExecuteCommand.Quotes)}\"";
                        break;

                    case PlatformID.Win32NT:
                        targetCommand = $"cmd /C \"{targetCommand.Trim(ExecuteCommand.Quotes)}\"";
                        break;
                }
            }
            else
            {
            }

            return targetCommand;
        }
    }
}
