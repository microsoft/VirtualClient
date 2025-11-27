// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Contracts;

    /// <summary>
    /// Command executes the operations to upload files from a directory on the system.
    /// </summary>
    internal class UploadFilesCommand : ExecuteProfileCommand
    {
        private const string DefaultContentPathTemplate = "{experimentId}/{agentId}";

        /// <summary>
        /// The directory to search for files to upload.
        /// </summary>
        public string TargetDirectory { get; set; }

        /// <summary>
        /// Executes the file upload operations on the target directory.
        /// </summary>
        /// <param name="args">The arguments provided to the application on the command line.</param>
        /// <param name="dependencies">Dependencies/services created for the application.</param>
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        protected override Task<int> ExecuteAsync(string[] args, IServiceCollection dependencies, CancellationTokenSource cancellationTokenSource)
        {
            return base.ExecuteAsync(args, dependencies, cancellationTokenSource);
        }

        protected override void Initialize(string[] args, PlatformSpecifics platformSpecifics)
        {
            this.Timeout = ProfileTiming.OneIteration();
            this.Profiles = new List<DependencyProfileReference>
            {
                new DependencyProfileReference("UPLOAD-FILES.json")
            };

            if (string.IsNullOrWhiteSpace(this.ContentPathTemplate))
            {
                this.ContentPathTemplate = UploadFilesCommand.DefaultContentPathTemplate;
            }

            if (this.Parameters == null)
            {
                this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            }

            this.Parameters["TargetDirectory"] = this.TargetDirectory;
        }
    }
}
