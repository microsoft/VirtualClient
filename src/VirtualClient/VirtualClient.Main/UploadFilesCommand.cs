// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
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
        /// <param name="cancellationTokenSource">Provides a token that can be used to cancel the command operations.</param>
        /// <returns>The exit code for the command operations.</returns>
        public override async Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            int exitCode = 0;

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

            exitCode = await base.ExecuteAsync(args, cancellationTokenSource);

            return exitCode;
        }
    }
}
