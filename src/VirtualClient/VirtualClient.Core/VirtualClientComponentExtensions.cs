// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Extension methods for common operations in <see cref="VirtualClientComponent"/> derived
    /// classes.
    /// </summary>
    public static class VirtualClientComponentExtensions
    {
        private static readonly IAsyncPolicy FileSystemAccessRetryPolicy = Policy.Handle<IOException>()
            .WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));

        /// <summary>
        /// Executes a command within an isolated process.
        /// </summary>
        /// <param name="component">The component that is executing the process/command.</param>
        /// <param name="command">The command to execute within the process.</param>
        /// <param name="workingDirectory">The working directory from which the command should be executed.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the process execution.</param>
        /// <param name="runElevated">True to run the process with elevated privileges. Default = false</param>
        /// <returns>The process that executed the command.</returns>
        public static Task<IProcessProxy> ExecuteCommandAsync(
            this VirtualClientComponent component,
            string command,
            string workingDirectory,
            EventContext telemetryContext,
            CancellationToken cancellationToken,
            bool runElevated = false)
        {
            return component.ExecuteCommandAsync(command, null, workingDirectory, telemetryContext, cancellationToken, runElevated);
        }

        /// <summary>
        /// Executes a command within an isolated process.
        /// </summary>
        /// <param name="component">The component that is executing the process/command.</param>
        /// <param name="command">The command to execute within the process.</param>
        /// <param name="commandArguments">The arguments to supply to the command.</param>
        /// <param name="workingDirectory">The working directory from which the command should be executed.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the process execution.</param>
        /// <param name="runElevated">True to run the process with elevated privileges. Default = false</param>
        /// <returns>The process that executed the command.</returns>
        public static async Task<IProcessProxy> ExecuteCommandAsync(
            this VirtualClientComponent component,
            string command,
            string commandArguments,
            string workingDirectory,
            EventContext telemetryContext,
            CancellationToken cancellationToken,
            bool runElevated = false)
        {
            component.ThrowIfNull(nameof(component));
            command.ThrowIfNullOrWhiteSpace(nameof(command));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext(nameof(command), command)
                .AddContext(nameof(commandArguments), commandArguments)
                .AddContext(nameof(workingDirectory), workingDirectory)
                .AddContext(nameof(runElevated), runElevated);

            IProcessProxy process = null;
            if (!cancellationToken.IsCancellationRequested)
            {
                ProcessManager processManager = component.Dependencies.GetService<ProcessManager>();

                if (!runElevated)
                {
                    process = processManager.CreateProcess(command, commandArguments, workingDirectory);
                }
                else
                {
                    process = processManager.CreateElevatedProcess(component.Platform, command, commandArguments, workingDirectory);
                }

                component.CleanupTasks.Add(() => process.SafeKill());
                component.Logger.LogTraceMessage($"Executing: {command} {SensitiveData.ObscureSecrets(commandArguments)}".Trim(), relatedContext);

                await process.StartAndWaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            return process;
        }

        /// <summary>
        /// Returns the package/dependency path information if it is registered.
        /// </summary>
        public static Task<DependencyPath> GetPlatformSpecificPackageAsync(this VirtualClientComponent component, string packageName, CancellationToken cancellationToken)
        {
            component.ThrowIfNull(nameof(component));
            packageName.ThrowIfNullOrWhiteSpace(nameof(packageName));

            IPackageManager packageManager = component.Dependencies.GetService<IPackageManager>();
            return packageManager.GetPlatformSpecificPackageAsync(packageName, component.Platform, component.CpuArchitecture, cancellationToken);
        }

        /// <summary>
        /// Loads results from the file system for the file provided.
        /// </summary>
        /// <param name="component">The component that is loading the results.</param>
        /// <param name="filePath">A paths to the results file to load.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        /// <returns>The contents of the results file.</returns>
        public static async Task<string> LoadResultsAsync(this VirtualClientComponent component, string filePath, CancellationToken cancellationToken)
        {
            component.ThrowIfNull(nameof(component));
            filePath.ThrowIfNullOrWhiteSpace(nameof(filePath));

            string results = null;
            if (!cancellationToken.IsCancellationRequested)
            {
                if (!component.Dependencies.TryGetService<IFileSystem>(out IFileSystem fileSystem))
                {
                    throw new DependencyException(
                        $"Missing file operations dependency. To load results requires a dependency of type '{typeof(IFileSystem).FullName}' to be provided to the component instances.",
                        ErrorReason.DependencyNotFound);
                }

                if (!fileSystem.File.Exists(filePath))
                {
                    throw new WorkloadResultsException($"Expected results file '{filePath}' not found.", ErrorReason.WorkloadResultsNotFound);
                }

                results = await fileSystem.File.ReadAllTextAsync(filePath);
            }

            return results;
        }

        /// <summary>
        /// Loads results from the file system for the files provided.
        /// </summary>
        /// <param name="component">The component that is loading the results.</param>
        /// <param name="filePaths">A set of one or more paths to results files to load.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        /// <returns>The contents of the results files.</returns>
        public static async Task<IEnumerable<string>> LoadResultsAsync(this VirtualClientComponent component, IEnumerable<string> filePaths, CancellationToken cancellationToken)
        {
            component.ThrowIfNull(nameof(component));
            filePaths.ThrowIfNullOrEmpty(nameof(filePaths));

            List<string> results = null;
            if (!cancellationToken.IsCancellationRequested)
            {
                if (!component.Dependencies.TryGetService<IFileSystem>(out IFileSystem fileSystem))
                {
                    throw new DependencyException(
                        $"Missing file operations dependency. To load results requires a dependency of type '{typeof(IFileSystem).FullName}' to be provided to the component instances.",
                        ErrorReason.DependencyNotFound);
                }

                results = new List<string>();
                foreach (string filePath in filePaths)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        if (!fileSystem.File.Exists(filePath))
                        {
                            throw new WorkloadResultsException($"Expected results file '{filePath}' not found.", ErrorReason.WorkloadResultsNotFound);
                        }

                        results.Add(await fileSystem.File.ReadAllTextAsync(filePath));
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Upload a single file with defined BlobDescriptor.
        /// </summary>
        /// <param name="component">The Virtual Client component that is uploading the blob/file content.</param>
        /// <param name="blobManager">Handles the upload of the blob/file content to the store.</param>
        /// <param name="fileSystem">IFileSystem interface, required to distinguish paths between linux and windows. Provides access to the file system for reading the contents of the files.</param>
        /// <param name="blobDescriptor">The defined blob descriptor</param>
        /// <param name="filePath">Path to the file.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <param name="deleteFile">Whether to delete file after upload.</param>
        /// <param name="retryPolicy">Retry policy</param>
        /// <returns></returns>
        public static async Task UploadFileAsync(
            this VirtualClientComponent component,
            IBlobManager blobManager,
            IFileSystem fileSystem,
            BlobDescriptor blobDescriptor,
            string filePath,
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
                bool uploaded = false;

                await (retryPolicy ?? VirtualClientComponentExtensions.FileSystemAccessRetryPolicy).ExecuteAsync(async () =>
                {
                    try
                    {
                        IFileInfo fileInfo = fileSystem.FileInfo.FromFileName(filePath);

                        // Some processes creat the files up front before writing content to them. These files will
                        // be 0 bytes in size.
                        if (fileInfo.Length > 0)
                        {
                            using (FileStream uploadStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                if (uploadStream.Length > 0)
                                {
                                    EventContext telemetryContext = EventContext.Persisted()
                                        .AddContext("file", fileInfo.Name)
                                        .AddContext("blobContainer", blobDescriptor.ContainerName)
                                        .AddContext("blobName", blobDescriptor.Name);

                                    await component.Logger.LogMessageAsync($"{component.TypeName}.UploadFile", telemetryContext, async () =>
                                    {
                                        await blobManager.UploadBlobAsync(blobDescriptor, uploadStream, cancellationToken);
                                        uploaded = true;
                                    });
                                }
                            }
                        }
                    }
                    catch (IOException exc) when (exc.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase))
                    {
                        // The blob upload could fail often. We skip it and we will pick it up on next iteration.
                    }
                });

                // Delete ONLY if uploaded successfully. We DO use the cancellation token supplied to the method
                // here to ensure we cycle around quickly to uploading files while Virtual Client is trying to shut
                // down to have the best chance of getting them off the system.
                if (deleteFile && uploaded)
                {
                    await fileSystem.File.DeleteAsync(filePath);
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
        /// <param name="filePathBlobDescriptors">A set of file path and descriptor pairs that each define a blob/file to upload and the target location in the store.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <param name="deleteFile">Whether to delete file after upload.</param>
        /// <param name="retryPolicy">Retry policy</param>
        /// <returns></returns>
        public static async Task UploadFilesAsync(
            this VirtualClientComponent component,
            IBlobManager blobManager,
            IFileSystem fileSystem,
            IEnumerable<KeyValuePair<string, BlobDescriptor>> filePathBlobDescriptors,
            CancellationToken cancellationToken,
            bool deleteFile = false,
            IAsyncPolicy retryPolicy = null)
        {
            foreach (KeyValuePair<string, BlobDescriptor> pair in filePathBlobDescriptors)
            {
                await component.UploadFileAsync(blobManager, fileSystem, pair.Value, pair.Key, cancellationToken, deleteFile, retryPolicy).ConfigureAwait(false);
            }
        }
    }
}
