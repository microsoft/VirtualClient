// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Actions;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command runs a system command as-is.
    /// </summary>
    internal class ExecuteCommand : ExecuteProfileCommand
    {
        /// <summary>
        /// When determining the name of the command, we want to exclude certain terms
        /// that define the hosting/terminal environment (e.g. pwsh, python).
        /// </summary>
        /// <remarks>
        /// Examples:
        /// pwsh S:\any\Script.ps1 -> Script
        /// pwsh -Command S:\any\Script.ps1 -> Script
        /// </remarks>
        private static readonly Regex CommandTerminalExpression = new Regex("pwsh|pwsh.exe|powershell|powershell.exe|python|python.exe|python3|python3.exe|-[a-z-_]");

        private static readonly Regex PowerShellExpression = new Regex(
            "pwsh.exe|pwsh|powershell.exe|powershell", 
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// The full command line.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Returns true if the user command line arguments provided indicates a PowerShell (pwsh) 
        /// command (vs. a profile execution).
        /// </summary>
        protected bool IsPowerShell
        {
            get
            {
                return ExecuteCommand.PowerShellExpression.IsMatch(this.Command);
            }
        }

        /// <summary>
        /// Executes the operations to reset the environment.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public override async Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            int exitCode = 0;
            if (!String.IsNullOrWhiteSpace(this.Command))
            {
                exitCode = await this.ExecuteCommandAsync(args, cancellationTokenSource);
            }
            else if (this.Profiles?.Any() == true)
            {
                exitCode = await this.ExecuteProfilesAsync(args, cancellationTokenSource);
            }
            else
            {
                throw new NotSupportedException("Command line usage is not supported. The intended command or profile execution intentions are unclear.");
            }

            return exitCode;
        }

        /// <summary>
        /// Executes the flow for basic (1-off) commands (e.g. ipconfig /all).
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        protected virtual Task<int> ExecuteCommandAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            string[] commandArguments = this.Command?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string commandName = ExecuteCommand.GetCommandName(commandArguments);

            string fullCommand = this.Command;
            if (this.IsPowerShell)
            {
                fullCommand = ExecuteCommand.NormalizeForPowerShell(this.Command);
            }

            List<DependencyProfileReference> profiles = new List<DependencyProfileReference>
            {
                new DependencyProfileReference("EXECUTE-COMMAND.json")
            };

            if (this.Profiles?.Any() == true)
            {
                profiles.AddRange(this.Profiles);
            }

            this.Iterations = ProfileTiming.OneIteration();
            this.Profiles = profiles;
            if (this.Parameters == null)
            {
                this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            }

            this.Parameters["Command"] = fullCommand;
            this.Parameters["Scenario"] = $"Execute-{commandName}";

            return base.ExecuteAsync(args, cancellationTokenSource);
        }

        /// <summary>
        /// Executes the flow for basic (1-off) commands (e.g. ipconfig /all).
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        protected virtual Task<int> ExecuteProfilesAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            return base.ExecuteAsync(args, cancellationTokenSource);
        }

        /// <summary>
        /// Returns the name of the command being executed.
        /// </summary>
        /// <param name="commandArguments">The full command line arguments.</param>
        protected static string GetCommandName(string[] commandArguments)
        {
            string commandName = null;
            foreach (string argument in commandArguments)
            {
                if (String.Equals(commandName, "sudo"))
                {
                    continue;
                }

                // Find the first argument that is not a
                if (!ExecuteCommand.CommandTerminalExpression.IsMatch(argument))
                {
                    commandName = Path.GetFileNameWithoutExtension(argument.Trim());
                    break;
                }
            }

            return commandName;
        }

        /// <summary>
        /// Normalizes the PowerShell command for execution in a non-interactive
        /// environment.
        /// </summary>
        /// <param name="commandLine">The command line arguments provided by the user.</param>
        /// <returns>A PowerShell command ready for non-interactive execution.</returns>
        protected static string NormalizeForPowerShell(string commandLine)
        {
            string normalizedCommand = commandLine;
            if (!Regex.IsMatch(commandLine, "-NonInteractive", RegexOptions.IgnoreCase))
            {
                int indexToInsert = -1;
                List<string> commandArgs = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

                for (int i = 0; i < commandArgs.Count; i++)
                {
                    if (ExecuteCommand.PowerShellExpression.IsMatch(commandArgs[i]))
                    {
                        indexToInsert = i + 1;
                        break;
                    }
                }

                if (indexToInsert >= 0)
                {
                    commandArgs.Insert(indexToInsert, "-NonInteractive");
                }

                normalizedCommand = string.Join(' ', commandArgs);
            }

            return normalizedCommand;
        }

        /// <summary>
        /// Initializes dependencies required by Virtual Client application operations.
        /// </summary>
        protected override IServiceCollection InitializeDependencies(string[] args)
        {
            if (this.LoggingLevel == null)
            {
                // Default log level is Debug/Verbose to ensure standard output + error
                // for processes executed is output to console/screen.
                this.LoggingLevel = LogLevel.Debug;
            }

            return base.InitializeDependencies(args);
        }
    }
}