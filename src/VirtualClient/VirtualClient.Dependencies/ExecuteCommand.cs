// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine.Builder;
    using System.CommandLine.Parsing;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
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
        /// <param name="runElevated">True to signal that the command should be ran in elevated mode.</param>
        protected static bool TryGetCommandParts(string fullCommand, out string command, out string commandArguments, out bool runElevated)
        {
            fullCommand.ThrowIfNullOrWhiteSpace(nameof(fullCommand));

            command = null;
            commandArguments = null;
            runElevated = false;

            ParseResult result = new CommandLineBuilder().UseEnvironmentVariableDirective().Build().Parse(fullCommand);
            if (result.Tokens?.Any() == true)
            {
                if (result.Tokens.Count == 2 && result.Tokens.First().Value == "sudo")
                {
                    // e.g.
                    // sudo ./configure
                    runElevated = true;
                    command = result.Tokens.ElementAt(1).Value;
                }
                else if (result.Tokens.Count > 2 && result.Tokens.First().Value == "sudo")
                {
                    // e.g.
                    // sudo ./command1 --argument=value
                    runElevated = true;
                    command = result.Tokens.ElementAt(1).Value;
                    commandArguments = string.Join(' ', result.Tokens.Skip(2).Select(t => t.Value));
                }
                else if (result.Tokens.Count == 1)
                {
                    command = result.Tokens.First().Value;
                }
                else
                {
                    command = result.Tokens.First().Value;
                    commandArguments = string.Join(' ', result.Tokens.Skip(1).Select(t => t.Value));
                }

                // Normalize for white space in the command path.
                if (command.Contains(' ', StringComparison.OrdinalIgnoreCase))
                {
                    command = $"\"{command}\"";
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
                        if (!cancellationToken.IsCancellationRequested
                            && ExecuteCommand.TryGetCommandParts(fullCommand, out string command, out string commandArguments, out bool runElevated))
                        {
                            await (this.RetryPolicy ?? Policy.NoOpAsync()).ExecuteAsync(async () =>
                            {
                                using (IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, this.WorkingDirectory, telemetryContext, cancellationToken, runElevated))
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
    }
}
