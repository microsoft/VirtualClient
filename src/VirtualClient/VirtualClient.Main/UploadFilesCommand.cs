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
