// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Agent
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command runs the full SDK Agent command line on a target system.
    /// </summary>
    internal class ExecuteAgentCommand : ExecuteProfileCommand
    {
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
            string subcommand = args[0].Trim();
            List<DependencyProfileReference> profiles = new List<DependencyProfileReference>();

            // We use a convention here to map the name of the supported subcommand to
            // the name of the profile. By convention, the profile should NOT take in any parameters.
            //
            // e.g.
            // copy-logs        -> COPY-LOGS.json
            // install-packages -> INSTALL-PACKAGES.json
            // install-agent    -> INSTALL-AGENT.json
            this.Profiles = new List<DependencyProfileReference>
            {
                new DependencyProfileReference($"{subcommand.ToUpperInvariant()}.json")
            };

            // To avoid confusing situations, remote command execution DOES NOT support
            // the following features on the controller/local system:
            // - Dependency installation on the controller.
            // - Targeting specific scenarios (e.g. --scenarios=Scenario01).
            this.InstallDependencies = false;
            this.Scenarios = null;

            this.FailFast = true;
            this.Iterations = ProfileTiming.OneIteration();
            this.LogToFile = true;
        }
    }
}