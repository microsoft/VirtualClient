// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command executes the operations to bootstrap/install dependencies on the system
    /// prior to running a Virtual Client profile.
    /// </summary>
    internal class BootstrapPackageCommand : ExecuteProfileCommand
    {
        /// <summary>
        /// The name (logical name) to use when registering the package.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The name of the package (in storage) to bootstrap/install.
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
            string registerAsName = this.Name;
            if (String.IsNullOrWhiteSpace(registerAsName))
            {
                registerAsName = Path.GetFileNameWithoutExtension(this.Package);
            }

            this.Timeout = ProfileTiming.OneIteration();
            this.Profiles = new List<DependencyProfileReference>
            {
                new DependencyProfileReference("BOOTSTRAP-DEPENDENCIES.json")
            };

            if (this.Parameters == null)
            {
                this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            }

            this.Parameters["Package"] = this.Package;
            this.Parameters["RegisterAsName"] = registerAsName;

            return base.ExecuteAsync(args, cancellationTokenSource);
        }
    }
}
