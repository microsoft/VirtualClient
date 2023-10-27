// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The Sysbench Server workload executor.
    /// </summary>
    public class SysbenchOLTPServerExecutor : SysbenchOLTPExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SysbenchOLTPServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>
        public SysbenchOLTPServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.StateManager = this.Dependencies.GetService<IStateManager>();
        }

        /// <summary>
        /// Disk filter specified
        /// </summary>
        public string DiskFilter
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(SysbenchOLTPServerExecutor.DiskFilter), string.Empty);
            }
        }

        /// <summary>
        /// Provides access to the local state management facilities.
        /// </summary>
        protected IStateManager StateManager { get; }

        /// <summary>
        /// Initializes the environment and dependencies for server of sysbench workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await base.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);

            await this.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);

            SysbenchOLTPState state = await this.StateManager.GetStateAsync<SysbenchOLTPState>(nameof(SysbenchOLTPState), cancellationToken)
                ?? new SysbenchOLTPState();

            // prepare the server for a specific scenario

            if (!state.DatabaseScenarioInitialized)
            {
                // only prepare if the scenario has not been initialized

                switch (this.DatabaseScenario)
                {
                    case SysbenchOLTPScenario.InMemory:
                        await this.PrepareInMemoryScenarioAsync(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);

                        state.DatabaseScenarioInitialized = true;
                        break;
                    case SysbenchOLTPScenario.Balanced:
                        string diskPaths = await this.PrepareBalancedScenarioAsync(telemetryContext, cancellationToken)
                            .ConfigureAwait(false);

                        state.DatabaseScenarioInitialized = true;
                        state.DiskPathsArgument = diskPaths;
                        break;
                    case SysbenchOLTPScenario.Default:
                        break;
                }

                Item<SysbenchOLTPState> stateUpdate = new Item<SysbenchOLTPState>(nameof(SysbenchOLTPState), state);

                HttpResponseMessage response = await this.ServerApiClient.UpdateStateAsync<SysbenchOLTPState>(nameof(SysbenchOLTPState), stateUpdate, cancellationToken)
                    .ConfigureAwait(false);

                response.ThrowOnError<WorkloadException>();

                await this.StateManager.SaveStateAsync<SysbenchOLTPState>(nameof(SysbenchOLTPState), state, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes server side of workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(SysbenchOLTPServerExecutor)}.ExecuteServer", telemetryContext, async () =>
            {
                using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    this.SetServerOnline(true);

                    if (this.IsMultiRoleLayout())
                    {
                        await this.WaitAsync(cancellationToken)
                            .ConfigureAwait(false);
                    }
                }
            });
        }

        private async Task PrepareInMemoryScenarioAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                // server's job is to configure buffer size, in memory script updates the mysql config file

                string inMemoryScript = "in-memory.sh";
                string scriptsDirectory = this.PlatformSpecifics.GetScriptPath("sysbencholtp");

                MemoryInfo memoryInfo = await this.SystemManager.GetMemoryInfoAsync(cancellationToken);
                long totalMemoryKiloBytes = memoryInfo.TotalMemory;
                int bufferSizeInMegaBytes = Convert.ToInt32(totalMemoryKiloBytes / 1024);

                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    this.PlatformSpecifics.Combine(scriptsDirectory, inMemoryScript),
                    $"{bufferSizeInMegaBytes}",
                    scriptsDirectory,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext);
                        process.ThrowIfWorkloadFailed();
                    }
                }
            }
        }

        private async Task<string> PrepareBalancedScenarioAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // from the server side, to prep for the balanced scenario, the script runs commands to configure
            // permisions for the server vm to use the disk space as mysql database storage
            // this is because sysbench workload prepares the tables from the client side.
            // once the tables have been prepared, the client will re-copy the tables from OS disk to data disk

            // in the meantime, the server needs to pass on information about what disks are going to be used
            // to distribute the database on; this method prepares that information

            string diskPaths = string.Empty;

            if (!cancellationToken.IsCancellationRequested)
            {
                string diskFilter = "osdisk:false";

                if (!string.IsNullOrEmpty(this.DiskFilter))
                {
                    diskFilter += string.Concat("&", this.DiskFilter);
                }

                IEnumerable<Disk> disks = await this.SystemManager.DiskManager.GetDisksAsync(cancellationToken)
                        .ConfigureAwait(false);

                if (disks?.Any() != true)
                {
                    throw new WorkloadException(
                        "Unexpected scenario. The disks defined for the system could not be properly enumerated.",
                        ErrorReason.WorkloadUnexpectedAnomaly);
                }

                IEnumerable<Disk> disksToTest = DiskFilters.FilterDisks(disks, diskFilter, this.Platform).ToList();

                if (disksToTest?.Any() != true)
                {
                    throw new WorkloadException(
                        "Expected disks to test not found. Given the parameters defined for the profile action/step or those passed " +
                        "in on the command line, the requisite disks do not exist on the system or could not be identified based on the properties " +
                        "of the existing disks.",
                        ErrorReason.DependencyNotFound);
                }

                if (await this.CreateMountPointsAsync(disksToTest, cancellationToken).ConfigureAwait(false))
                {
                    // Refresh the disks to pickup the mount point changes.
                    await Task.Delay(1000).ConfigureAwait(false);
                    IEnumerable<Disk> updatedDisks = await this.SystemManager.DiskManager.GetDisksAsync(cancellationToken)
                        .ConfigureAwait(false);

                    disksToTest = DiskFilters.FilterDisks(updatedDisks, diskFilter, this.Platform).ToList();
                }

                foreach (Disk disk in disksToTest)
                {
                    if (disk.GetPreferredAccessPath(this.Platform) != "/mnt")
                    {
                        diskPaths += $"{disk.GetPreferredAccessPath(this.Platform)} ";
                    }
                }

                string balancedScript = "balanced-server.sh";
                string scriptsDirectory = this.PlatformSpecifics.GetScriptPath("sysbencholtp");

                using (IProcessProxy process = await this.ExecuteCommandAsync(
                    this.PlatformSpecifics.Combine(scriptsDirectory, balancedScript),
                    diskPaths,
                    scriptsDirectory,
                    telemetryContext,
                    cancellationToken,
                    runElevated: true))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await this.LogProcessDetailsAsync(process, telemetryContext, "Sysbench", logToFile: true);
                        process.ThrowIfWorkloadFailed();
                    }
                }      
            }

            return diskPaths;
        }

        /// <summary>
        /// Creates mount points for any disks that do not have them already.
        /// </summary>
        /// <param name="disks">This disks on which to create the mount points.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        private async Task<bool> CreateMountPointsAsync(IEnumerable<Disk> disks, CancellationToken cancellationToken)
        {
            bool mountPointsCreated = false;

            // Don't mount any partition in OS drive.
            foreach (Disk disk in disks.Where(d => !d.IsOperatingSystem()))
            {
                // mount every volume that doesn't have an accessPath.
                foreach (DiskVolume volume in disk.Volumes.Where(v => v.AccessPaths?.Any() != true))
                {
                    string newMountPoint = volume.GetDefaultMountPoint();
                    this.Logger.LogTraceMessage($"Create Mount Point: {newMountPoint}");

                    EventContext relatedContext = EventContext.Persisted().Clone()
                        .AddContext(nameof(volume), volume)
                        .AddContext("mountPoint", newMountPoint);

                    await this.Logger.LogMessageAsync($"{this.TypeName}.CreateMountPoint", relatedContext, async () =>
                    {
                        string newMountPoint = volume.GetDefaultMountPoint();
                        if (!this.SystemManager.FileSystem.Directory.Exists(newMountPoint))
                        {
                            this.SystemManager.FileSystem.Directory.CreateDirectory(newMountPoint).Create();
                        }

                        await this.SystemManager.DiskManager.CreateMountPointAsync(volume, newMountPoint, cancellationToken)
                            .ConfigureAwait(false);

                        mountPointsCreated = true;

                    }).ConfigureAwait(false);
                }
            }

            return mountPointsCreated;
        }
    }
}
