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
    using VirtualClient.Contracts;

    /// <summary>
    /// Command executes the operations to bootstrap/install dependencies on the system
    /// prior to running a Virtual Client profile.
    /// </summary>
    public class RunBootstrapCommand : CommandBase
    {
        /// <summary>
        /// The name of the package to bootstrap/install.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The logical name of the package to bootstrap/install.
        /// </summary>
        public string Package { get; set; }

        /// <summary>
        /// Executes the dependency bootstrap/installation operations.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public override Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            RunProfileCommand profileExecutionCommand = new RunProfileCommand
            {
                AgentId = this.AgentId,
                ContentStore = this.ContentStore,
                Debug = this.Debug,
                Timeout = ProfileTiming.OneIteration(),
                EventHubStore = this.EventHubStore,
                ExecutionSystem = this.ExecutionSystem,
                ExperimentId = this.ExperimentId,
                InstallDependencies = true,
                Metadata = this.Metadata,
                PackageStore = this.PackageStore,
                Parameters = new Dictionary<string, IConvertible>
                {
                    { "Package", this.Package },
                    { "RegisterAsName", this.Name }
                },
                Profiles = new List<DependencyProfileReference>
                {
                    new DependencyProfileReference("BOOTSTRAP-DEPENDENCIES.json")
                },
                ProxyApiUri = this.ProxyApiUri
            };

            return profileExecutionCommand.ExecuteAsync(args, cancellationTokenSource);
        }
    }
}
