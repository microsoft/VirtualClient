// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Reads files in a given directory and requests a file upload.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64,win-arm64,win-x64")]
    public class UploadFiles : VirtualClientComponent
    {
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="UploadFiles"/> class.
        /// </summary>
        public UploadFiles(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = this.Dependencies.GetService<IFileSystem>();
        }

        /// <summary>
        /// Set to true to request the files be deleted after they are
        /// uploaded (i.e. self-cleaning).
        /// </summary>
        public bool DeleteOnUpload
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.DeleteOnUpload), false);
            }
        }

        /// <summary>
        /// The directory for which to request log uploads. 
        /// </summary>
        public string TargetDirectory
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.TargetDirectory));
            }
        }

        /// <summary>
        /// True/false whether the files should have timestamps added to the
        /// file names for upload or not.
        /// </summary>
        public bool Timestamped
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.Timestamped), false);
            }
        }

        /// <summary>
        /// The name of the tool in which to associate the file content.
        /// </summary>
        public string Toolname
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.Toolname), out IConvertible toolName);
                return toolName?.ToString();
            }
        }

        /// <summary>
        /// Executes the logic to process the files in the logs directory.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!this.fileSystem.Directory.Exists(this.TargetDirectory))
            {
                this.Logger.LogMessage($"Target directory '{this.TargetDirectory}' does not exist.", LogLevel.Warning, telemetryContext);
                return;
            }

            if (!this.TryGetContentStoreManager(out IBlobManager contentStore))
            {
                this.Logger.LogMessage($"Target content store not supplied.", LogLevel.Warning, telemetryContext);
                return;
            }

            IEnumerable<FileUploadDescriptor> uploadDescriptors = this.CreateFileUploadDescriptors(
                this.TargetDirectory,
                this.Toolname,
                this.Parameters,
                this.Metadata,
                timestamped: this.Timestamped);

            if (uploadDescriptors?.Any() != true)
            {
                this.Logger.LogWarning($"No files found in target directory '{this.TargetDirectory}'.", telemetryContext);
                return;
            }

            this.Logger.LogMessage(
                $"{this.TypeName}.FilesToUpload", 
                LogLevel.Information, 
                telemetryContext.Clone().AddContext("files", uploadDescriptors.Select(desc => desc.FilePath.OrderBy(path => path))));

            foreach (FileUploadDescriptor descriptor in uploadDescriptors)
            {
                descriptor.DeleteOnUpload = this.DeleteOnUpload;
                await this.RequestFileUploadAsync(descriptor);
            }
        }
    }
}
