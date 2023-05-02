// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace CRC.VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Platform;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;

    /// <summary>
    /// The FileUpload Service of Virtual Client to upload workload logs to blob
    /// </summary>
    [WindowsCompatible]
    [UnixCompatible]
    public class FileUploadMonitor : VirtualClientIntervalBasedMonitor
    {
        private ISystemManagement systemManagement;
        private IFileSystem fileSystem;
        private string contentsUploadDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadMonitor"/> class.
        /// </summary>
        public FileUploadMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <inheritdoc/>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // All background monitor ExecuteAsync methods should be either 'async' or should use a Task.Run() if running a 'while' loop or the
            // logic will block without returning. Monitors are typically expected to be fire-and-forget.

            await this.UploadManifestFileAsync("crclabslogcontainer", cancellationToken);

            await this.UploadFilesToBlobAsync(telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Initializes the executor dependencies, package locations, etc...
        /// </summary>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;

            this.contentsUploadDirectory = this.PlatformSpecifics.Combine(this.PlatformSpecifics.LogsDirectory, "contentuploads");

            if (!this.fileSystem.Directory.Exists(this.contentsUploadDirectory))
            {
                this.fileSystem.Directory.CreateDirectory(this.contentsUploadDirectory);
            }

            return Task.CompletedTask;
        }

        private async Task UploadFilesToBlobAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // while (!cancellationToken.IsCancellationRequested || (this.fileSystem.Directory.GetFiles(this.contentsUploadDirectory).Length != 0))
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await this.UploadLogsAsPerMarkerFiles(cancellationToken);

                    await Task.Delay(this.MonitorFrequency, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever ctrl-C is used. Do a check once more, without cancellationToken and break;
                    await this.UploadLogsAsPerMarkerFiles(CancellationToken.None);
                    break;
                }
                catch (IOException)
                {
                    // Retry in case of IO Exceptions to ensure no file is left to be uploaded on account of transient IO issues.
                    await Task.Delay(500, cancellationToken);
                }
                catch (Exception exc)
                {
                    this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                    await Task.Delay(500, cancellationToken);
                }
            }
        }

        private async Task UploadManifestFileAsync(string containerName, CancellationToken cancellationToken, string userEmail = null, string source = null)
        {
            // GET ALL DATA FROM METADATA AS A DICTIONARY
            if (string.IsNullOrEmpty(userEmail))
            {
                userEmail = "adityaa@microsoft.com";
            }

            if (string.IsNullOrEmpty(source))
            {
                source = "vc-workload";
            }

            string[] folderNames = { this.ExperimentId, this.AgentId, this.Scenario, "crc_logs_manifest.json" };
            string manifestBlobPath = string.Join("/", folderNames.Where(str => !string.IsNullOrWhiteSpace(str)));

            string manifestData = $"{{\r\n    " +
                $"\"user\": \"{userEmail}\",\r\n    " +
                $"\"source\": \"{source}\",\r\n    " +
                $"\"resultCollectionType\": \"private\",\r\n    " +
                $"\"localMachineName\": \"{this.AgentId}\",\r\n    " +
                $"\"experimentDefinitionId\": \"{this.ExperimentId}\",\r\n    " +
                $"\"experimentName\": \"\",\r\n    " +
                $"\"experimentInstanceId\": \"{this.ExperimentId}\",\r\n    " +
                $"\"experimentStepName\": \"{this.Scenario}\"\r\n}}";
            
            string manifestFilePath = this.PlatformSpecifics.Combine(this.PlatformSpecifics.LogsDirectory, "crc_logs_manifest.json");

            await this.fileSystem.File.WriteAllTextAsync(manifestFilePath, manifestData)
                .ConfigureAwait(false);

            if (this.TryGetContentStoreManager(out IBlobManager blobManager))
            {
                using FileStream uploadStream = new (manifestFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                await this.UploadFileStream(blobManager, containerName, manifestBlobPath, Encoding.UTF8, "text/plain", uploadStream, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task UploadFileStream(IBlobManager blobManager, string containerName, string blobName, Encoding contentEncoding, string contentType, Stream uploadStream, CancellationToken cancellationToken)
        {
            BlobDescriptor resultsBlob = new ()
            {
                Name = blobName,
                ContainerName = containerName,
                ContentEncoding = contentEncoding, // Encoding.UTF8
                ContentType = contentType
            };

            await blobManager.UploadBlobAsync(resultsBlob, uploadStream, cancellationToken)
                .ConfigureAwait(false);

            return;
        }

        private async Task UploadLogsAsPerMarkerFiles(CancellationToken cancellationToken)
        {
            var filesToBeUploaded = this.fileSystem.Directory.GetFiles(this.contentsUploadDirectory);

            foreach (var markerFile in filesToBeUploaded)
            {
                // Assuming a following format of the JSON Marker file that points to filePath to upload.
                // {
                //     containerName: "csitoolkitlogcontainer",
                //     blobName: "/anyvm-01/geekbench5/scoresystem/2023-04-29T01_00_05_1284676z-geekbench5.log",
                //     contentEncoding: "utf-8",
                //     contentType: "text/plain",
                //     filePath: "C:\\VirtualClient\\content\\win-x64\\logs\\geekbench5\\scoresystem\\2023-04-29T01_00_05_1284676z-geekbench5.log"
                // }

                string markerFileContent = await this.fileSystem.File.ReadAllTextAsync(markerFile, cancellationToken);

                ContentUploadMarker contentUploadMarker = JsonConvert.DeserializeObject<ContentUploadMarker>(markerFileContent);

                if (this.TryGetContentStoreManager(out IBlobManager blobManager))
                {
                    using FileStream uploadStream = new (contentUploadMarker.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    await this.UploadFileStream(blobManager, contentUploadMarker.ContainerName, contentUploadMarker.BlobName, Encoding.UTF8, contentUploadMarker.ContentType, uploadStream, cancellationToken)
                        .ConfigureAwait(false);
                }

                await this.fileSystem.File.DeleteAsync(markerFile);
            }
        }
    }
}