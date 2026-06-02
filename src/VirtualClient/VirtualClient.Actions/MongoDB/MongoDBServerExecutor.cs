// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// MongoDB Server Executor - Handles MongoDB server installation and configuration.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class MongoDBServerExecutor : MongoDBExecutor
    {
        private IFileSystem fileSystem;
        private ISystemManagement systemManagement;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MongoDBServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public MongoDBServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            
            this.ServerRetryPolicy = Policy.Handle<Exception>(exc => !(exc is OperationCanceledException))
                .WaitAndRetryAsync(10, (retries) => TimeSpan.FromSeconds(retries));
        }

        /// <summary>
        /// Disk filter string to filter disks for MongoDB data path.
        /// </summary>
        public string DiskFilter
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(MongoDBServerExecutor.DiskFilter), string.Empty);
            }
        }

        /// <summary>
        /// A retry policy to apply to the server when starting to handle transient issues.
        /// </summary>
        protected IAsyncPolicy ServerRetryPolicy { get; set; }

        /// <summary>
        /// Initializes the environment for running MongoDB server.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            this.InitializeApiClients();

            // Initialize disk if DiskFilter is specified
            if (!string.IsNullOrWhiteSpace(this.DiskFilter))
            {
                await this.InitializeDiskPathAsync(cancellationToken).ConfigureAwait(false);
                await this.ConfigureDiskForMongoDBAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
            }

            // Ensure MongoDB is configured to listen on all interfaces
            await this.ConfigureMongoDBBindAddressAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            // Start MongoDB server
            await this.StartMongoDBServerAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the MongoDB server workload.
        /// </summary>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteServer", telemetryContext, async () =>
            {
                try
                {
                    this.SetServerOnline(false);

                    await this.ServerApiClient.PollForHeartbeatAsync(TimeSpan.FromMinutes(5), cancellationToken)
                        .ConfigureAwait(false);

                    // Server is now online and ready to accept connections
                    this.SetServerOnline(true);

                    // Keep the server running until cancelled
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                }
            });
        }

        /// <summary>
        /// Disposes of resources used by the executor.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            
            if (disposing && !this.disposed)
            {
                this.disposed = true;
            }
        }

        /// <summary>
        /// Configures MongoDB to bind to all network interfaces.
        /// </summary>
        private async Task ConfigureMongoDBBindAddressAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                string configFile = "/etc/mongod.conf";
                string setBindIpCmd = $@"sudo sed -i 's/bindIp:.*/bindIp: 0.0.0.0/g' {configFile}";
                
                await this.ExecuteMongoDBCommandAsync(
                    "bash",
                    $"-c \"{setBindIpCmd}\"",
                    "ConfigureBindAddress",
                    telemetryContext,
                    cancellationToken).ConfigureAwait(false);

                this.Logger.LogMessage(
                    $"{nameof(MongoDBServerExecutor)}.BindAddressConfigured",
                    LogLevel.Information,
                    EventContext.Persisted().AddContext("bindIp", "0.0.0.0"));
            }
            catch (Exception ex)
            {
                EventContext relatedContext = telemetryContext.Clone().AddError(ex);
                this.Logger.LogMessage($"{nameof(MongoDBServerExecutor)}.ConfigureBindAddressFailed", LogLevel.Warning, relatedContext);
            }
        }

        /// <summary>
        /// Starts the MongoDB server.
        /// </summary>
        private async Task StartMongoDBServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                // Restart MongoDB to apply all configurations
                await this.ExecuteMongoDBServiceCommandAsync("restart", telemetryContext, cancellationToken).ConfigureAwait(false);

                // Wait a bit for MongoDB to fully start
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);

                // Verify MongoDB is running
                await this.VerifyMongoDBIsRunningAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

                this.Logger.LogMessage(
                    $"{nameof(MongoDBServerExecutor)}.MongoDBServerStarted",
                    LogLevel.Information,
                    EventContext.Persisted());
            }
            catch (Exception ex)
            {
                EventContext relatedContext = telemetryContext.Clone().AddError(ex);
                this.Logger.LogMessage($"{nameof(MongoDBServerExecutor)}.StartMongoDBServerFailed", LogLevel.Error, relatedContext);
                throw;
            }
        }

        /// <summary>
        /// Verifies that MongoDB is running.
        /// </summary>
        private async Task VerifyMongoDBIsRunningAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(
                this.Platform, "mongosh", "--eval \"db.runCommand({ping: 1})\""))
            {
                await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);
                
                if (process.ExitCode != 0)
                {
                    string output = process.StandardOutput.ToString();
                    string error = process.StandardError.ToString();
                    this.Logger.LogMessage(
                        $"{nameof(MongoDBServerExecutor)}.MongoDBVerificationDebug",
                        LogLevel.Warning,
                        telemetryContext.Clone()
                            .AddContext("exitCode", process.ExitCode)
                            .AddContext("output", output)
                            .AddContext("error", error));
                    
                    throw new WorkloadException(
                        $"MongoDB server verification failed with exit code {process.ExitCode}. Output: {output}. Error: {error}",
                        ErrorReason.WorkloadFailed);
                }

                this.Logger.LogMessage(
                    $"{nameof(MongoDBServerExecutor)}.MongoDBVerified",
                    LogLevel.Information,
                    telemetryContext);
            }
        }

        /// <summary>
        /// Configures the disk for MongoDB data storage.
        /// </summary>
        private async Task ConfigureDiskForMongoDBAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested || string.IsNullOrWhiteSpace(this.DiskFilter))
            {
                return;
            }

            try
            {
                string diskDevicePath = this.Parameters["DiskDevicePath"].ToString();
                string mongoDataPath = "/mnt/mongodb-data";

                // Stop MongoDB service before mounting
                await this.ExecuteMongoDBServiceCommandAsync("stop", telemetryContext, cancellationToken).ConfigureAwait(false);

                // Create filesystem on disk
                await this.ExecuteMongoDBCommandAsync(
                    "bash",
                    $"-c \"sudo mkfs.ext4 -F {diskDevicePath}\"",
                    "CreateFilesystem",
                    telemetryContext,
                    cancellationToken).ConfigureAwait(false);

                // Create mount point
                await this.ExecuteMongoDBCommandAsync(
                    "bash",
                    $"-c \"sudo mkdir -p {mongoDataPath}\"",
                    "CreateMountPoint",
                    telemetryContext,
                    cancellationToken).ConfigureAwait(false);

                // Mount the disk
                await this.ExecuteMongoDBCommandAsync(
                    "bash",
                    $"-c \"sudo mount -t ext4 {diskDevicePath} {mongoDataPath}\"",
                    "MountDisk",
                    telemetryContext,
                    cancellationToken).ConfigureAwait(false);

                // Set permissions
                await this.ExecuteMongoDBCommandAsync(
                    "bash",
                    $"-c \"sudo chown -R mongodb:mongodb {mongoDataPath}\"",
                    "SetPermissions",
                    telemetryContext,
                    cancellationToken).ConfigureAwait(false);

                // Update mongod.conf to use the new dbPath
                string configFile = "/etc/mongod.conf";
                string setDbPathCmd = $@"sudo sed -i 's|^\s*dbPath:.*|  dbPath: {mongoDataPath}|g' {configFile}";
                await this.ExecuteMongoDBCommandAsync(
                    "bash",
                    $"-c \"{setDbPathCmd}\"",
                    "UpdateMongoConf",
                    telemetryContext,
                    cancellationToken).ConfigureAwait(false);

                this.Logger.LogMessage(
                    $"{nameof(MongoDBServerExecutor)}.DiskConfigurationComplete",
                    LogLevel.Information,
                    EventContext.Persisted().AddContext("diskDevicePath", diskDevicePath).AddContext("mongoDataPath", mongoDataPath));
            }
            catch (Exception ex)
            {
                EventContext relatedContext = telemetryContext.Clone().AddError(ex);
                this.Logger.LogMessage($"{nameof(MongoDBServerExecutor)}.DiskConfigurationFailed", LogLevel.Warning, relatedContext);
                // Continue - disk configuration is not critical if already configured
            }
        }

        /// <summary>
        /// Executes a MongoDB-related command.
        /// </summary>
        private async Task ExecuteMongoDBCommandAsync(
            string command,
            string commandArguments,
            string scenario,
            EventContext telemetryContext,
            CancellationToken cancellationToken)
        {
            try
            {
                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(
                    this.Platform, command, commandArguments))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());
                    this.LogProcessTrace(process);

                    await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, $"MongoDBServer-{scenario}", logToFile: true)
                            .ConfigureAwait(false);

                        if (process.ExitCode != 0)
                        {
                            this.Logger.LogMessage(
                                $"{nameof(MongoDBServerExecutor)}.{scenario}Warning",
                                LogLevel.Warning,
                                telemetryContext.Clone()
                                    .AddContext("command", command)
                                    .AddContext("commandArguments", commandArguments)
                                    .AddContext("exitCode", process.ExitCode));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogMessage(
                    $"{nameof(MongoDBServerExecutor)}.{scenario}Failed",
                    LogLevel.Warning,
                    telemetryContext.Clone().AddError(ex).AddContext("command", command));
            }
        }

        /// <summary>
        /// Executes MongoDB service command.
        /// </summary>
        private async Task ExecuteMongoDBServiceCommandAsync(string action, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                using (IProcessProxy process = this.systemManagement.ProcessManager.CreateElevatedProcess(
                    this.Platform, "sudo", $"systemctl {action} mongod"))
                {
                    this.CleanupTasks.Add(() => process.SafeKill());
                    this.LogProcessTrace(process);

                    await process.StartAndWaitAsync(cancellationToken).ConfigureAwait(false);

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, $"MongoDBServer-Service-{action}", logToFile: true)
                            .ConfigureAwait(false);

                        if (process.ExitCode != 0)
                        {
                            this.Logger.LogMessage(
                                $"{nameof(MongoDBServerExecutor)}.ServiceCommandWarning",
                                LogLevel.Warning,
                                telemetryContext.Clone()
                                    .AddContext("action", action)
                                    .AddContext("exitCode", process.ExitCode));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.Logger.LogMessage(
                    $"{nameof(MongoDBServerExecutor)}.MongoServiceCommandFailed",
                    LogLevel.Warning,
                    telemetryContext.Clone().AddError(ex).AddContext("action", action));
            }
        }

        /// <summary>
        /// Initializes the disk path for MongoDB based on DiskFilter parameter.
        /// </summary>
        private async Task InitializeDiskPathAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested || string.IsNullOrWhiteSpace(this.DiskFilter))
            {
                return;
            }

            IEnumerable<Disk> disks = await this.systemManagement.DiskManager.GetDisksAsync(cancellationToken)
                .ConfigureAwait(false);

            if (disks == null || !disks.Any())
            {
                throw new DependencyException(
                    $"No disks are available on the system to match the filter criteria '{this.DiskFilter}'.",
                    ErrorReason.DiskInformationNotAvailable);
            }

            IEnumerable<Disk> disksToTest = DiskFilters.FilterDisks(disks, this.DiskFilter, this.Platform);

            if (disksToTest == null || !disksToTest.Any())
            {
                throw new DependencyException(
                    $"No disks matched the filter criteria '{this.DiskFilter}'.",
                    ErrorReason.DiskInformationNotAvailable);
            }

            Disk selectedDisk = disksToTest.First();

            // Store the device path for use in configuration
            this.Parameters["DiskDevicePath"] = selectedDisk.DevicePath;

            EventContext telemetryContext = EventContext.Persisted()
                .AddContext("diskFilter", this.DiskFilter)
                .AddContext("selectedDisk", selectedDisk.DevicePath)
                .AddContext("totalDisks", disks.Count())
                .AddContext("filteredDisks", disksToTest.Count());

            this.Logger.LogMessage($"{nameof(MongoDBServerExecutor)}.DiskSelected", LogLevel.Information, telemetryContext);
        }
    }
}
