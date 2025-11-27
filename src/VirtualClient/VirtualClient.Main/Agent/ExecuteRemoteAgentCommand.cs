// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Agent
{
    using System;
    using System.Collections.Generic;
    using System.CommandLine;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command runs the full SDK Agent command line on a target system.
    /// </summary>
    internal class ExecuteRemoteAgentCommand : ExecuteProfileCommand
    {
        /// <summary>
        /// A command line to execute independently of a profile.
        /// </summary>
        public string Command { get; set; }

        /// <summary>
        /// Executes the operations to reset the environment.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="dependencies">Dependencies/services created for the application.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        protected override Task<int> ExecuteAsync(string[] args, IServiceCollection dependencies, CancellationTokenSource cancellationTokenSource)
        {
            return base.ExecuteAsync(args, dependencies, cancellationTokenSource);
        }

        /// <summary>
        /// Initializes the command state before execution.
        /// </summary>
        protected override void Initialize(string[] args, PlatformSpecifics platformSpecifics)
        {
            if (this.Profiles?.Any() != true && string.IsNullOrWhiteSpace(this.Command))
            {
                throw new NotSupportedException("Command line usage is not supported. The intended command or profile execution intentions are unclear.");
            }

            this.Profiles = new List<DependencyProfileReference>
            {
                new DependencyProfileReference("EXECUTE-ON-REMOTE.json")
            };

            // To avoid confusing situations, remote command execution DOES NOT support
            // the following features on the controller/local system. The options (if defined) however
            // will be passed to the target system:
            // - Dependency installation on the controller.
            // - Targeting specific scenarios (e.g. --scenarios=Scenario01).
            this.InstallDependencies = false;
            this.Scenarios = null;

            this.Iterations = ProfileTiming.OneIteration();
            this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase)
            {
                { nameof(this.Command), this.GetTargetCommandArguments(args) }
            };
        }

        /// <summary>
        /// Returns the command line arguments to execute on the target/remote system.
        /// </summary>
        /// <param name="commandArguments">The original/full command line arguments.</param>
        protected string GetTargetCommandArguments(string[] commandArguments)
        {
            List<string> targetCommandArguments = new List<string>();
            Option targetAgentOption = OptionFactory.CreateTargetAgentOption();

            foreach (string argument in commandArguments.Skip(1))
            {
                string effectiveArgument = argument.Trim();
                if (effectiveArgument.StartsWith("@"))
                {
                    // Do not add response files. They are not supported for the controller during
                    // remote execution as this would require the same response file to exist on the
                    // target system. Additionally, the options within the response file could be confusing
                    // (e.g. --target={ssh_target}).
                    continue;
                }

                if (!OptionFactory.ContainsOption(targetAgentOption, argument))
                {
                    // Remove the '--target' options.
                    targetCommandArguments.Add(argument);
                }
            }

            return string.Join(" ", targetCommandArguments);
        }
    }
}