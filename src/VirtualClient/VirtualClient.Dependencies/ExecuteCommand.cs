// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine.Builder;
    using System.CommandLine.Parsing;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.Extensions.DependencyInjection;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Executes a command on the system with the working directory set to a 
    /// package installed.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class ExecuteCommand : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExecuteCommand"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public ExecuteCommand(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(
                this.MaxRetries,
                (retries) => TimeSpan.FromSeconds(retries + 1));
        }

        /// <summary>
        /// Parameter defines the command(s) to execute. Multiple commands should be delimited using
        /// the '&amp;&amp;' characters which works on both Windows and Unix systems (e.g. ./configure&amp;&amp;make).
        /// </summary>
        public string Command
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.Command));
            }
        }

        /// <summary>
        /// Parameter defines the maximum number of retries on failures of the
        /// command execution. Default = 0;
        /// </summary>
        public int MaxRetries
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.MaxRetries), 0);
            }
        }

        /// <summary>
        /// Parameter defines the working directory from which the command should be executed. When the
        /// 'PackageName' parameter is defined, this parameter will take precedence. Otherwise, the directory
        /// where the package is installed for the 'PackageName' parameter will be used as the working directory.
        /// </summary>
        public string WorkingDirectory
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.WorkingDirectory), out IConvertible workingDir);
                return workingDir?.ToString();
            }
        }

        /// <summary>
        /// A policy that defines how the component will retry when it experiences transient issues.
        /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// Returns true if the command parts can be determined and outputs the parts.
        /// </summary>
        /// <param name="fullCommand">The full comamnd and arguments (e.g. sudo lshw -c disk).</param>
        /// <param name="command">The command to execute.</param>
        /// <param name="commandArguments">The arguments to pass to the command.</param>
        protected static bool TryGetCommandParts(string fullCommand, out string command, out string commandArguments)
        {
            fullCommand.ThrowIfNullOrWhiteSpace(nameof(fullCommand));

            command = null;
            commandArguments = null;

            string effectiveFullCommand = fullCommand.Trim();
            Match commandMatch = null;

            // Note:
            // \x22 = quotation mark
            if (effectiveFullCommand.StartsWith('"'))
            {
                // e.g.
                // "/home/user/dir/anycommand"
                // "/home/user/dir with space/anycommand"
                //
                // ...directories having spaces in the name
                // "/home/user/dir/anycommand" --argument=value --argument2=value2
                // "/home/user/dir with space/anycommand" --argument=value --argument2=value2
                commandMatch = Regex.Match(effectiveFullCommand, @"^(\x22[\x20\x21\x23-\x7E]+\x22)", RegexOptions.IgnoreCase);
            }
            else
            {
                // e.g.
                // /home/user/dir/anycommand
                // /home/user/dir/anycommand --argument=value --argument2=value2
                commandMatch = Regex.Match(effectiveFullCommand, @"^([\x21\x23-\x7E]+)", RegexOptions.IgnoreCase);
            }

            if (commandMatch.Success)
            {
                command = commandMatch.Groups[1].Value?.Trim();
                commandArguments = fullCommand.Substring(commandMatch.Groups[1].Value.Trim().Length)?.Trim();

                if (string.IsNullOrWhiteSpace(commandArguments))
                {
                    commandArguments = null;
                }
            }

            return command != null;
        }

        /// <summary>
        /// Execute the command(s) logic on the system.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            telemetryContext.AddContext("command", SensitiveData.ObscureSecrets(this.Command));
            telemetryContext.AddContext("workingDirectory", this.WorkingDirectory);
            telemetryContext.AddContext("platforms", string.Join(VirtualClientComponent.CommonDelimiters.First(), this.SupportedPlatforms));

            if (!cancellationToken.IsCancellationRequested)
            {
                IEnumerable<string> commandsToExecute = this.GetCommandsToExecute();

                if (commandsToExecute?.Any() == true)
                {
                    foreach (string fullCommand in commandsToExecute)
                    {
                        if (!cancellationToken.IsCancellationRequested && ExecuteCommand.TryGetCommandParts(fullCommand, out string command, out string commandArguments))
                        {
                            await (this.RetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
                            {
                                using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, this.WorkingDirectory, telemetryContext, cancellationToken))
                                {
                                    if (!cancellationToken.IsCancellationRequested)
                                    {
                                        await this.LogProcessDetailsAsync(process, telemetryContext, this.LogFolderName);
                                        process.ThrowIfComponentOperationFailed(this.ComponentType);
                                    }
                                }
                            });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the component for execution.
        /// </summary>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.EvaluateParametersAsync(cancellationToken);
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the system.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = false;
            if (base.IsSupported())
            {
                // We execute only if the current platform/architecture matches those
                // defined in the parameters.
                if (!this.SupportedPlatforms.Any() || this.SupportedPlatforms.Contains(this.PlatformArchitectureName))
                {
                    isSupported = true;
                }
            }

            return isSupported;
        }

        private IEnumerable<string> GetCommandsToExecute()
        {
            List<string> commandsToExecute = new List<string>();
            bool sudo = this.Command.StartsWith("sudo", StringComparison.OrdinalIgnoreCase);

            IEnumerable<string> commands = this.Command.Split("&&", StringSplitOptions.RemoveEmptyEntries & StringSplitOptions.TrimEntries);

            foreach (string fullCommand in commands)
            {
                if (sudo && !fullCommand.Contains("sudo", StringComparison.OrdinalIgnoreCase))
                {
                    commandsToExecute.Add($"sudo {fullCommand}");
                }
                else
                {
                    commandsToExecute.Add(fullCommand);
                }
            }

            return commandsToExecute;
        }

        ////private class CommandLineParser
        ////{
        ////    public string Executable { get; private set; }
        ////    public List<string> Arguments { get; private set; }

        ////    public CommandLineParser(string input)
        ////    {
        ////        Executable = string.Empty;
        ////        Arguments = new List<string>();
        ////        Parse(input);
        ////    }

        ////    private void Parse(string input)
        ////    {
        ////        // Match quoted strings or whitespace-separated tokens
        ////        var pattern = @"(?<token>(""(?:\\.|[^""])*""|'(?:\\.|[^'])*'|\S+))";
        ////        var matches = Regex.Matches(input, pattern);

        ////        var tokens = new List<string>();

        ////        foreach (Match match in matches)
        ////        {
        ////            string token = match.Groups["token"].Value;

        ////            // Remove surrounding quotes
        ////            if ((token.StartsWith("\"") && token.EndsWith("\"")) ||
        ////                (token.StartsWith("'") && token.EndsWith("'")))
        ////            {
        ////                token = token.Substring(1, token.Length - 2);
        ////            }

        ////            tokens.Add(token);
        ////        }

        ////        if (tokens.Count > 0)
        ////        {
        ////            Executable = tokens[0];
        ////            Arguments.AddRange(tokens.GetRange(1, tokens.Count - 1));
        ////        }
        ////    }
        ////}
    }
}
