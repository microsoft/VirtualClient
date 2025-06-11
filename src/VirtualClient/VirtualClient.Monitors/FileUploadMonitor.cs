// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient.Common.Contracts;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Platform;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Polly;

    /// <summary>
    /// This monitor processes content/file uploads requested by Virtual Client
    /// workload, monitoring and dependency components.
    /// </summary>
    public class FileUploadMonitor : VirtualClientComponent
    {
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileUploadMonitor"/> class.
        /// </summary>
        public FileUploadMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = this.Dependencies.GetService<IFileSystem>();
            this.ProcessingIntervalWaitTime = TimeSpan.FromSeconds(3);
        }

        /// <summary>
        /// The source directory to watch for content upload requests/notifications.
        /// </summary>
        public string RequestsDirectory
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.RequestsDirectory), this.PlatformSpecifics.ContentUploadsDirectory);
            }
        }

        /// <summary>
        /// The time to wait in between each processing interval. Default = 10 seconds.
        /// </summary>
        protected TimeSpan ProcessingIntervalWaitTime { get; set; }

        /// <inheritdoc/>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                // All background monitor ExecuteAsync methods should be either 'async' or should use a Task.Run() if running a 'while' loop or the
                // logic will block without returning. Monitors are typically expected to be fire-and-forget.

                if (this.TryGetContentStoreManager(out IBlobManager blobManager))
                {
                    await this.ProcessFileUploadsAsync(blobManager, telemetryContext, cancellationToken);
                    await this.ProcessSummaryFileUploadsAsync(blobManager, telemetryContext);
                }
            });
        }

        private async Task ProcessFileUploadsAsync(IBlobManager blobManager, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // We do not immediately honor a cancellation. We attempt to handle any remaining
            // file uploads to "clear the docket" before we cancel and exit. VC will allow monitors
            // some amount of time to exit gracefully before allowing the application itself to fully
            // exit. This is designed to enable the highest reliability possible for getting buffered telemetry
            // off the system as well as any log files. The user can even define an explicit wait time on the command
            // line using the --exit-wait option to further control this.
            //
            // As such, we will continue to process the files below so long as any of the following is true:
            // 1) Cancellation has NOT been requested.
            // 2) Cancellation is requested but there remain files to process.
            while (!cancellationToken.IsCancellationRequested)
            {
                EventContext relatedContext = telemetryContext.Clone().AddContext("directoryPath", this.RequestsDirectory);

                try
                {
                    await Task.Delay(this.ProcessingIntervalWaitTime, cancellationToken);

                    // We do not honor the cancellation token until ALL files have been processed.
                    while (await this.UploadFilesAsync(blobManager, relatedContext))
                    {
                        try
                        {
                            await Task.Delay(this.ProcessingIntervalWaitTime, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            // If the cancellation is requested, we want to short-circuit the Task.Delay
                            // so that we can quickly loop around to the processing. If there are no files
                            // to process, we will exit.
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever ctrl-C is used. Do a check once more, without cancellationToken and break;
                    await this.UploadFilesAsync(blobManager, telemetryContext);
                }
                catch (Exception exc)
                {
                    // The logic within the processing method should be handling all exceptions. However,
                    // this logic is here to ensure that the monitor NEVER crashes. It must attempt to
                    // exit gracefully at often as possible.
                    this.Logger.LogErrorMessage(exc, relatedContext, LogLevel.Error);
                }
            }
        }

        private async Task ProcessSummaryFileUploadsAsync(IBlobManager blobManager, EventContext telemetryContext)
        {
            EventContext relatedContext = telemetryContext.Clone().AddContext("directoryPath", this.PlatformSpecifics.LogsDirectory);

            while (true)
            {
                try
                {
                    await this.Logger.LogMessageAsync($"{this.TypeName}.ProcessSummaryFileUploads", relatedContext, async () =>
                    {
                        // Upload the workload summary logs (e.g. metrics.csv) before exiting. We do this at the very end. Same as before, we do not
                        // honor the cancellation token until ALL files have been successfully processed.
                        await this.UploadCsvSummaryFilesAsync(blobManager, relatedContext);

                        // Upload specific summary.txt at root level of logs directory.
                        await this.UploadSummaryFileAsync(blobManager, relatedContext);
                    });

                    break;
                }
                catch (OperationCanceledException)
                {
                    // Expected whenever ctrl-C is used.
                }
                catch (Exception exc)
                {
                    // The logic within the processing method should be handling all exceptions. However,
                    // this logic is here to ensure that the monitor NEVER crashes. It must attempt to
                    // exit gracefully at often as possible.
                    this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Error);
                }
            }
        }

        private async Task<bool> UploadFilesAsync(IBlobManager blobManager, EventContext telemetryContext)
        {
            bool filesFound = false;

            try
            {
                if (this.fileSystem.Directory.Exists(this.RequestsDirectory))
                {
                    IEnumerable<string> uploadDescriptorFiles = this.fileSystem.Directory.GetFiles(this.RequestsDirectory, "*.json");
                    filesFound = uploadDescriptorFiles?.Any() == true;

                    if (filesFound)
                    {
                        foreach (var uploadDescriptor in uploadDescriptorFiles)
                        {
                            try
                            {
                                bool deleteFile = false;
                                string uploadDescriptorContent = await this.fileSystem.File.ReadAllTextAsync(uploadDescriptor, CancellationToken.None);
                                FileUploadDescriptor descriptor = uploadDescriptorContent.FromJson<FileUploadDescriptor>();

                                // Do not assume the file still exists. Check to see if the file remains on the
                                // system so that we do not end up in an endless retry loop trying to upload a file that
                                // does not exist.
                                if (!this.fileSystem.File.Exists(descriptor.FilePath))
                                {
                                    await this.fileSystem.File.DeleteAsync(uploadDescriptor);
                                    continue;
                                }

                                try
                                {
                                    await this.UploadFileAsync(blobManager, this.fileSystem, descriptor, CancellationToken.None);
                                    deleteFile = descriptor.DeleteOnUpload;

                                    await this.fileSystem.File.DeleteAsync(uploadDescriptor);
                                }
                                catch (Exception exc)
                                {
                                    this.Logger.LogMessage(
                                        $"{this.TypeName}.UploadFileError",
                                        LogLevel.Warning,
                                        telemetryContext.Clone().AddError(exc).AddContext("fileDescriptor", descriptor));

                                    throw;
                                }
                                finally
                                {
                                    if (deleteFile)
                                    {
                                        await this.fileSystem.File.DeleteAsync(descriptor.FilePath);
                                    }
                                }
                            }
                            catch (JsonSerializationException)
                            {
                                // Invalid file in the directory.
                                await this.fileSystem.File.DeleteAsync(uploadDescriptor);
                            }
                            catch (IOException exc) when (exc.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase))
                            {
                                // It is common that there will be read/write access errors at certain times while
                                // upload request files are being created at the same time as attempts to read. 
                            }
                            catch
                            {
                                // We are logging within the upload logic block above. We do not want to allow the
                                // file upload monitor logic to crash. We move forward to the next upload request.
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
            }

            return filesFound;
        }

        private async Task UploadSummaryFileAsync(IBlobManager blobManager, EventContext telemetryContext)
        {
            try
            {
                string summaryTxtFileLocation = Path.Combine(this.PlatformSpecifics.LogsDirectory, "summary.txt");
                bool summaryTxtFileExists = this.fileSystem.File.Exists(summaryTxtFileLocation);
                telemetryContext
                    .AddContext(nameof(summaryTxtFileLocation), summaryTxtFileLocation)
                    .AddContext(nameof(summaryTxtFileExists), summaryTxtFileExists);

                if (summaryTxtFileExists)
                {
                    try
                    {
                        FileUploadDescriptor descriptor = this.CreateFileUploadDescriptor(
                            new FileContext(
                                this.fileSystem.FileInfo.New(summaryTxtFileLocation),
                                "text/plain",
                                Encoding.UTF8.WebName,
                                this.ExperimentId,
                                this.AgentId));

                        await this.UploadFileAsync(blobManager, this.fileSystem, descriptor, CancellationToken.None);
                    }
                    catch (IOException exc) when (exc.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase))
                    {
                        // It is common that there will be read/write access errors at certain times while
                        // upload request files are being created at the same time as attempts to read. 
                    }
                    catch (Exception exc)
                    {
                        this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                    }
                }
            }
            catch (Exception exc)
            {
                this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Error);
            }
        }

        private async Task UploadCsvSummaryFilesAsync(IBlobManager blobManager, EventContext telemetryContext)
        {
            try
            {
                if (this.fileSystem.Directory.Exists(this.PlatformSpecifics.LogsDirectory))
                {
                    IEnumerable<string> csvFiles = this.fileSystem.Directory.GetFiles(this.PlatformSpecifics.LogsDirectory, "*.csv", SearchOption.TopDirectoryOnly);
                    if (csvFiles?.Any() == true)
                    {
                        foreach (var filePath in csvFiles)
                        {
                            try
                            {
                                FileUploadDescriptor descriptor = this.CreateFileUploadDescriptor(
                                    new FileContext(
                                        this.fileSystem.FileInfo.New(filePath),
                                        "text/csv",
                                        Encoding.UTF8.WebName,
                                        this.ExperimentId,
                                        this.AgentId));

                                await this.UploadFileAsync(blobManager, this.fileSystem, descriptor, CancellationToken.None);
                            }
                            catch (IOException exc) when (exc.Message.Contains("being used by another process", StringComparison.OrdinalIgnoreCase))
                            {
                                // It is common that there will be read/write access errors at certain times while
                                // upload request files are being created at the same time as attempts to read. 
                            }
                            catch (Exception exc)
                            {
                                this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Error);
            }
        }
    }
}