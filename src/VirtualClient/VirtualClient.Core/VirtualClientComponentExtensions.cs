// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for common operations in <see cref="VirtualClientComponent"/> derived
    /// classes.
    /// </summary>
    public static class VirtualClientComponentExtensions
    {
        private static readonly IAsyncPolicy FileSystemAccessRetryPolicy = Policy.Handle<IOException>()
            .WaitAndRetryAsync(10, (retries) => TimeSpan.FromSeconds(1 * (retries + 1)));

        /// <summary>
        /// Upload a single file with defined BlobDescriptor.
        /// </summary>
        /// <param name="component">The Virtual Client component that is uploading the blob/file content.</param>
        /// <param name="blobManager">Handles the upload of the blob/file content to the store.</param>
        /// <param name="fileSystem">IFileSystem interface, required to distinguish paths between linux and windows. Provides access to the file system for reading the contents of the files.</param>
        /// <param name="descriptor">The defined blob descriptor</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <param name="deleteFile">Whether to delete file after upload.</param>
        /// <param name="retryPolicy">Retry policy</param>
        /// <returns></returns>
        public static async Task UploadFileAsync(
            this VirtualClientComponent component,
            IBlobManager blobManager,
            IFileSystem fileSystem,
            FileBlobDescriptor descriptor,
            CancellationToken cancellationToken,
            bool deleteFile = true,
            IAsyncPolicy retryPolicy = null)
        {
            /*
             * Azure Storage blob naming limit
             * https://docs.microsoft.com/en-us/rest/api/storageservices/naming-and-referencing-shares--directories--files--and-metadata
             * 
             * The following characters are not allowed: " \ / : | < > * ?
             * Directory and file names are case-preserving and case-insensitive.
             * A path name may be no more than 2,048 characters in length. Individual components in the path can be a maximum of 255 characters in length.
             * The depth of subdirectories in the path cannot exceed 250.
             * The same name cannot be used for a file and a directory that share the same parent directory.
             */

            /* VC upload naming convention
             * 
             * SingleClient: /experimentid/agentid/toolname/uploadTimestamp/{fileDirectories}/fileName
             * 
             * Blob Name/Path Examples:
             * --------------------------------------------------------
             * [Non-Client/Server Workloads]
             * /7dfae74c-06c0-49fc-ade6-987534bb5169/anyagentid/azureprofiler/2022-04-30T20:13:23.3768938Z-2c5cfa4031e34c8a8002745f3a9daee4.bin
             *
             * [Client/Server Workloads]
             * /7dfae74c-06c0-49fc-ade6-987534bb5169/anyagentid-client/azureprofiler/2022-04-30T20:13:23.3768938Z-client-2c5cfa4031e34c8a8002745f3a9daee4.bin
             * /7dfae74c-06c0-49fc-ade6-987534bb5169/anyotheragentid-server/azureprofiler/2022-04-30T20:13:18.4857827Z-server-3b6beb4142d23d7b7103634e2b8cbff3.bin
             */

            try
            {
                IAsyncPolicy asyncPolicy = retryPolicy ?? VirtualClientComponentExtensions.FileSystemAccessRetryPolicy;

                bool uploaded = false;
                await asyncPolicy.ExecuteAsync(async () =>
                {
                    try
                    {
                        // Some processes creat the files up front before writing content to them. These files will
                        // be 0 bytes in size.
                        if (descriptor.File.Length > 0)
                        {
                            using (FileStream uploadStream = new FileStream(descriptor.File.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                if (uploadStream.Length > 0)
                                {
                                    EventContext telemetryContext = EventContext.Persisted()
                                        .AddContext("file", descriptor.File.FullName)
                                        .AddContext("blobContainer", descriptor.ContainerName)
                                        .AddContext("blobName", descriptor.Name);

                                    await component.Logger.LogMessageAsync($"{component.TypeName}.UploadFile", telemetryContext, async () =>
                                    {
                                        await blobManager.UploadBlobAsync(descriptor, uploadStream, cancellationToken);
                                        uploaded = true;
                                    });
                                }
                            }
                        }
                    }
                    catch (IOException exc) when (exc.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase))
                    {
                        // The Azure blob upload could fail often. We skip it and we will pick it up on next iteration.
                    }
                });

                // Delete ONLY if uploaded successfully. We DO use the cancellation token supplied to the method
                // here to ensure we cycle around quickly to uploading files while Virtual Client is trying to shut
                // down to have the best chance of getting them off the system.
                if (deleteFile && uploaded)
                {
                    await fileSystem.File.DeleteAsync(descriptor.File.FullName);
                }
            }
            catch (Exception exc)
            {
                // Do not crash the file upload thread if we hit issues trying to upload to the blob store or
                // in accessing/deleting files on the file system. The logging logic will catch the details of
                // the failures and they may be transient.
                component.Logger.LogMessage($"{component.TypeName}.UploadFileFailure", LogLevel.Error, EventContext.Persisted().AddError(exc));
            }
        }

        /// <summary>
        /// Upload a list of files with the matching Blob descriptors.
        /// </summary>
        /// <param name="component">The Virtual Client component that is uploading the blob/file content.</param>
        /// <param name="blobManager">Handles the upload of the blob/file content to the store.</param>
        /// <param name="fileSystem">IFileSystem interface, required to distinguish paths between linux and windows. Provides access to the file system for reading the contents of the files.</param>
        /// <param name="descriptors">A set of file path and descriptor pairs that each define a blob/file to upload and the target location in the store.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <param name="deleteFile">Whether to delete file after upload.</param>
        /// <param name="retryPolicy">Retry policy</param>
        /// <returns></returns>
        public static async Task UploadFilesAsync(
            this VirtualClientComponent component,
            IBlobManager blobManager,
            IFileSystem fileSystem,
            IEnumerable<FileBlobDescriptor> descriptors,
            CancellationToken cancellationToken,
            bool deleteFile = false,
            IAsyncPolicy retryPolicy = null)
        {
            foreach (FileBlobDescriptor descriptor in descriptors)
            {
                await component.UploadFileAsync(blobManager, fileSystem, descriptor, cancellationToken, deleteFile, retryPolicy);
            }
        }
    }
}
