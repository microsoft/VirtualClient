// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
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

    /// <summary>
    /// This monitor processes content/file uploads requested by Virtual Client
    /// workload, monitoring and dependency components.
    /// </summary>
    [WindowsCompatible]
    [UnixCompatible]
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
            this.ProcessingIntervalWaitTime = TimeSpan.FromSeconds(10);
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
                        try
                        {
                            EventContext relatedContext = telemetryContext.Clone()
                                .AddContext("contentUploadsDirectory", this.RequestsDirectory);

                            await this.Logger.LogMessageAsync($"{this.TypeName}.ProcessUploads", telemetryContext, async () =>
                            {
                                // We do not honor the cancellation token until ALL files have been processed.
                                while (await this.ProcessUploadsAsync(blobManager, telemetryContext))
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
                            });

                            await Task.Delay(this.ProcessingIntervalWaitTime, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected whenever ctrl-C is used. Do a check once more, without cancellationToken and break;
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
            });
        }

        private async Task<bool> ProcessUploadsAsync(IBlobManager blobManager, EventContext telemetryContext)
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
                                string uploadDescriptorContent = await this.fileSystem.File.ReadAllTextAsync(uploadDescriptor, CancellationToken.None);
                                FileUploadDescriptor descriptor = uploadDescriptorContent.FromJson<FileUploadDescriptor>();

                                using (FileStream uploadStream = new FileStream(descriptor.FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                                {
                                    await this.UploadFileAsync(blobManager, this.fileSystem, descriptor, CancellationToken.None);
                                    await this.fileSystem.File.DeleteAsync(uploadDescriptor);
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
                this.Logger.LogErrorMessage(exc, telemetryContext, LogLevel.Warning);
            }

            return filesFound;
        }
    }
}