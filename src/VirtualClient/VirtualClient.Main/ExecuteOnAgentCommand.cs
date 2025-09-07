// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Logging;

    /// <summary>
    /// Command runs the full Virtual Client command line on a target system.
    /// </summary>
    internal class ExecuteOnAgentCommand : ExecuteProfileCommand
    {
        /// <summary>
        /// A command line to execute independently of a profile.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Executes the operations to reset the environment.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public override Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            if (this.Profiles?.Any() != true && string.IsNullOrWhiteSpace(this.Command))
            {
                throw new ArgumentException("Invalid usage. Either a profile or a command must be defined on the command line.");
            }

            this.Profiles = new List<DependencyProfileReference>
            {
                new DependencyProfileReference("EXECUTE-COMMAND-ON-REMOTE.json")
            };

            // To avoid confusing situations, remote command execution DOES NOT support
            // the following features on the controller/local system:
            // - Dependency installation on the controller.
            // - Targeting specific scenarios (e.g. --scenarios=Scenario01).
            this.InstallDependencies = false;
            this.Scenarios = null;

            this.Iterations = ProfileTiming.OneIteration();
            this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { nameof(this.Command), this.GetTargetCommandArguments(args) }
            };

            return base.ExecuteAsync(args, cancellationTokenSource);
        }

        /// <summary>
        /// Returns the command line arguments to execute on the target/remote system.
        /// </summary>
        /// <param name="commandArguments">The original/full command line arguments.</param>
        protected string GetTargetCommandArguments(string[] commandArguments)
        {
            List<string> targetCommandArguments = new List<string>();

            Option sshOption = OptionFactory.CreateSshOption();
            Option logToFileOption = OptionFactory.CreateLogToFileFlag();
            Option experimentIdOption = OptionFactory.CreateExperimentIdOption();

            Regex logToFileExpression = new Regex(string.Join("|", logToFileOption.Aliases.Select(a => $"{a}=")));
            Regex sshExpression = new Regex(string.Join("|", sshOption.Aliases.Select(a => $"{a}=")));
            Regex subCommandExpression = new Regex("remote", RegexOptions.IgnoreCase);
            Regex responseFileReferenceExpression = new Regex(@"@[a-z0-9\/\\\.:]+", RegexOptions.IgnoreCase);

            bool logToFileOptionDefined = false;

            foreach (string argument in commandArguments)
            {
                if (!string.IsNullOrWhiteSpace(this.Command) && argument == this.Command)
                {
                    targetCommandArguments.Add($"\"{argument}\"");
                }
                else if (!sshExpression.IsMatch(argument) 
                    && !subCommandExpression.IsMatch(argument) 
                    && !responseFileReferenceExpression.IsMatch(argument))
                {
                    // Remove the remote-execute subcommand and SSH options.
                    targetCommandArguments.Add(argument);
                }

                if (!logToFileOptionDefined && logToFileExpression.IsMatch(argument))
                {
                    logToFileOptionDefined = true;
                }
            }

            if (!logToFileOptionDefined)
            {
                targetCommandArguments.Add(logToFileOption.Aliases.ElementAt(1));
            }

            return string.Join(" ", targetCommandArguments);
        }
    }
}