// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Provides an example of a monitor that needs to upload files/information to 
    /// a content blob store (as defined on the command line).
    /// </summary>
    public class ExampleMonitorWithBlobUploadIntegration : VirtualClientIntervalBasedMonitor
    {
        private IFileSystem fileSystem;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleMonitorWithBlobUploadIntegration"/> class.
        /// </summary>
        public ExampleMonitorWithBlobUploadIntegration(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
        }

        /// <summary>
        /// Executes the monitor and uploads the results to the target content blob store.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            Task monitoringTask = Task.CompletedTask;
            if (this.Platform == PlatformID.Win32NT)
            {
                try
                {
                    // Long-running monitors should NOT block the thread. They should instead return
                    // a Task immediately (i.e. no await or GetAwaiter().GetResult()).
                    monitoringTask = this.StartMonitorAsync(telemetryContext, cancellationToken);
                }
                catch (Exception exc)
                {
                    this.Logger.LogErrorMessage(exc, telemetryContext);
                }
            }

            return monitoringTask;
        }

        /// <summary>
        /// Returns an object that contains properties that describe the host, OS and system
        /// on which the monitor is running.
        /// </summary>
        protected virtual string GetSystemInformation(DateTime snapshotTimestamp)
        {
            return string.Join(Environment.NewLine, new List<string>
            {
                $"Experiment: {this.ExperimentId}",
                $"Agent: {this.AgentId}",
                $"HostName: {Environment.MachineName}",
                $"Process: {SensitiveData.ObscureSecrets(Environment.CommandLine)}",
                $"CPU Architecture: {RuntimeInformation.ProcessArchitecture}",
                $"OS Description: {RuntimeInformation.OSDescription}",
                $"OS Platform: {Environment.OSVersion.Platform}",
                $"OS Version: {Environment.OSVersion.Version}",
                $"OS Service Pack: {Environment.OSVersion.ServicePack}",
                $"Timestamp: {snapshotTimestamp.ToString("o")}"
            });
        }

        private async Task<string> StageMonitoringResultsFileAsync(string systemInformation, CancellationToken cancellationToken)
        {
            string resultsDir = this.PlatformSpecifics.Combine(
                Path.GetDirectoryName(VirtualClientComponent.ExecutingAssembly.Location),
                "content",
                "example_monitor_results");

            string resultsFilePath = this.PlatformSpecifics.Combine(resultsDir, "system_info.txt");

            if (!this.fileSystem.Directory.Exists(resultsDir))
            {
                this.fileSystem.Directory.CreateDirectory(resultsDir);
            }

            await this.fileSystem.File.WriteAllTextAsync(resultsFilePath, systemInformation, cancellationToken)
                .ConfigureAwait(false);

            return resultsFilePath;
        }

        private Task StartMonitorAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                // If the content blob store is not defined, the monitor does nothing and exits.
                if (this.TryGetContentStoreManager(out IBlobManager blobManager))
                {
                    DateTime nextMonitoringTime = DateTime.UtcNow.Add(this.MonitorWarmupPeriod);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            if (DateTime.UtcNow >= nextMonitoringTime)
                            {
                                DateTime snapshotTime = DateTime.UtcNow;
                                string systemInformation = this.GetSystemInformation(snapshotTime);

                                // This is an example. For the sake of this example, imagine the content to
                                // upload is a file containing results produced by another toolset (e.g. Azure Profiler
                                // EMON). We are going to stage a file on the file system so that we can illustrate the blob
                                // upload scenario.
                                string exampleResultsFilePath = await this.StageMonitoringResultsFileAsync(systemInformation, cancellationToken)
                                    .ConfigureAwait(false);

                                await this.UploadResultsFileAsync(blobManager, exampleResultsFilePath, snapshotTime, cancellationToken)
                                    .ConfigureAwait(false);

                                nextMonitoringTime = DateTime.UtcNow.Add(this.MonitorFrequency);
                            }
                        }
                        catch (Exception exc)
                        {
                            // Capture the error information, but do not crash the monitoring process.
                            this.Logger.LogErrorMessage(exc, telemetryContext);
                        }
                        finally
                        {
                            await Task.Delay(500).ConfigureAwait(false);
                        }
                    }
                }
            });
        }

        private Task UploadResultsFileAsync(IBlobManager blobManager, string resultsFilePath, DateTime snapshotTime, CancellationToken cancellationToken)
        {
            // Example Blob Store Structure:
            // {containerName}/{experimentId}/{agentId}/{roundtrip-datetime-timestamp}_{file-name}.{file-extension}
            // Container: monitors
            // Blobs:     555553ed-3f63-43fe-ae7c-327bae09ee60/cluster01,cc296787-aee6-4ce4-b814-180627508d12,anyvm-01/example_monitor/2021-11-19T13:45:31.1247568Z-monitor.log
            //            555553ed-3f63-43fe-ae7c-327bae09ee60/cluster01,cc296787-aee6-4ce4-b814-180627508d12,anyvm-01/example_monitor/2021-11-19T13:46:30.6489302Z-monitor.log
            //            555553ed-3f63-43fe-ae7c-327bae09ee60/cluster01,cc296787-aee6-4ce4-b814-180627508d12,anyvm-01/example_monitor/2021-11-19T13:47:32.7295123Z-monitor.log
            FileUploadDescriptor descriptor = this.CreateFileUploadDescriptor(
                new FileContext(
                    this.fileSystem.FileInfo.New(resultsFilePath),
                    HttpContentType.PlainText,
                    Encoding.UTF8.WebName,
                    this.ExperimentId,
                    this.AgentId,
                    "examplemonitor",
                    this.Scenario,
                    null,
                    this.Roles?.FirstOrDefault()));

            return this.UploadFileAsync(blobManager, this.fileSystem, descriptor, cancellationToken, deleteFile: true);
        }
    }
}
