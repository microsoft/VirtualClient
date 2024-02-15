// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The Metaseq workload executor.
    /// </summary>
    [UnixCompatible]
    public class MetaseqExecutor : VirtualClientComponent
    {
        private const string RunScript = "run_training.sh";
        private const string HostFileName = "hostfile.txt";
        private string apptainerImageName;        

        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private IStateManager stateManager;
        private ISystemManagement systemManager;

        /// <summary>
        /// Constructor for <see cref="MetaseqExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public MetaseqExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.systemManager = this.Dependencies.GetService<ISystemManagement>();
            this.packageManager = this.systemManager.PackageManager;
            this.stateManager = this.systemManager.StateManager;
            this.fileSystem = this.systemManager.FileSystem;

            this.apptainerImageName = $"metaseq_{this.ApptainerImageVersion}.sif";
        }

        /// <summary>
        /// The version of apptainer of metaseq that needs to be pulled.
        /// </summary>
        public string ApptainerImageVersion
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.ApptainerImageVersion), "f7ffa5f_2306");
            }
        }

        /// <summary>
        /// Script used for training the system.
        /// </summary>
        public string TrainingScript
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.TrainingScript), "train_7.5.sh");
            }
        }

        /// <summary>
        /// Comma separated hostnames of the nodes in a cluster on which metaseq need to be run.
        /// </summary>
        public string Hostnames
        {
            get
            {
                this.Parameters.TryGetValue(nameof(MetaseqExecutor.Hostnames), out IConvertible hostnames);
                return hostnames?.ToString();
            }
        }

        /// <summary>
        /// The directory where the Metaseq package is installed.
        /// </summary>
        public string MetaseqDirectory
        {
            get
            {
                return this.PlatformSpecifics.Combine(this.PlatformSpecifics.PackagesDirectory, "metaseq");
            }
        }

        /// <summary>
        /// The output directory of metaseq.
        /// </summary>
        public string OutputDirectory
        {
            get
            {
                return this.PlatformSpecifics.Combine(this.MetaseqDirectory, "outputs");
            }
        }

        /// <summary>
        /// Executes the Metaseq workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string apptainerImagePath = this.PlatformSpecifics.Combine(this.MetaseqDirectory, this.apptainerImageName);
                string hostFilePath = this.PlatformSpecifics.Combine(this.MetaseqDirectory, MetaseqExecutor.HostFileName);
                string runScriptPath = this.PlatformSpecifics.Combine(this.MetaseqDirectory, MetaseqExecutor.RunScript);

                string commandArguments = $"{apptainerImagePath} {hostFilePath} {this.TrainingScript}";

                using (IProcessProxy process = await this.ExecuteCommandAsync(runScriptPath, commandArguments, this.MetaseqDirectory, telemetryContext, cancellationToken, runElevated: false))
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        if (process.IsErrored())
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext, "Metaseq", logToFile: true);
                            process.ThrowIfWorkloadFailed();
                        }

                        await this.CaptureMetricsAsync(process, commandArguments, telemetryContext, cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the environment for execution of the Metaseq workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.ValidateHostnames(telemetryContext, cancellationToken);

            MetaseqState state = await this.stateManager.GetStateAsync<MetaseqState>($"{nameof(MetaseqState)}", cancellationToken)
                ?? new MetaseqState();

            if (!state.MetaseqInitialized)
            {
                // Currently we are using NVMe Disks to store logs but this is not a critical dependency.
                // Once VC will be stable to run this benchmark, we can think of removing this dependency and 
                // can use DiskFilter to decide which disks to use and accordingly update run_training.sh to take the path as input.
                await this.ConfigureNVMeDisksForLogs(telemetryContext, cancellationToken);

                await this.WrapExecuteCommandAsync(
                    "apptainer", 
                    $"pull {this.apptainerImageName} oras://aisweco.azurecr.io/metaseq_cuda:{this.ApptainerImageVersion}", 
                    this.MetaseqDirectory, 
                    telemetryContext, 
                    cancellationToken).ConfigureAwait(false);

                state.MetaseqInitialized = true;
            }

            await this.stateManager.SaveStateAsync<MetaseqState>($"{nameof(MetaseqState)}", state, cancellationToken);
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = base.IsSupported()
                && (this.Platform == PlatformID.Unix)
                && (this.CpuArchitecture == Architecture.X64);

            if (!isSupported)
            {
                this.Logger.LogNotSupported("Metaseq", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        private async Task ValidateHostnames(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string lineSeparatedHosts = this.Hostnames.Replace(',', '\n');
            string hostFilePath = this.PlatformSpecifics.Combine(this.MetaseqDirectory, MetaseqExecutor.HostFileName);
            List<string> hostList = this.Hostnames.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();

            if (hostList.Count == 0)
            {
                throw new WorkloadException($"Hostnames can not be empty. Currently it is: {this.Hostnames}");
            }            

            foreach (string host in hostList)
            {
                IProcessProxy process = await this.ExecuteCommandAsync("ping", $"-W 1000 -c 1 {host}", Environment.CurrentDirectory, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, nameof(MetaseqExecutor), logToFile: true);
                    process.ThrowIfWorkloadFailed($"Host - {host} is not reachable.");
                }
            }

            await this.fileSystem.File.WriteAllTextAsync(hostFilePath, lineSeparatedHosts);
        }

        private async Task ConfigureNVMeDisksForLogs(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // Find NVMe disks with partition number 'n1'
            string[] nvmeDisks = this.systemManager.FileSystem.Directory.GetFiles("/dev/", "nvme*n1");
            int nvmeDiskCount = nvmeDisks.Length;

            if (nvmeDiskCount == 0)
            {
                throw new WorkloadException("There are no NVMe disks present on system to store the logs.");
            }

            this.systemManager.FileSystem.Directory.CreateDirectory("/mnt/resource_nvme");

            // Build command to create RAID-0 array
            string mdadmCommand = $"--create /dev/md128 --level 0 --raid-devices {nvmeDiskCount} " + string.Join(" ", nvmeDisks);

            await this.WrapExecuteCommandAsync("mdadm", mdadmCommand, Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.WrapExecuteCommandAsync("mkfs.xfs", "/dev/md128", Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.WrapExecuteCommandAsync("mount", "/dev/md128 /mnt/resource_nvme", Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            await this.WrapExecuteCommandAsync("chmod", "777 /mnt/resource_nvme", Environment.CurrentDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task WrapExecuteCommandAsync(string command, string commandArguments, string workingDirectory, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IProcessProxy process = await this.ExecuteCommandAsync(command, commandArguments, workingDirectory, telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            if (!cancellationToken.IsCancellationRequested)
            {
                await this.LogProcessDetailsAsync(process, telemetryContext, nameof(MetaseqExecutor), logToFile: true);
                process.ThrowIfWorkloadFailed();
            }
        }

        private async Task CaptureMetricsAsync(IProcessProxy process, string commandArguments, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    "Metaseq",
                    process.FullCommand(),
                    toolVersion: null);

                this.MetadataContract.Apply(telemetryContext);
                await this.LogProcessDetailsAsync(process, telemetryContext, "Metaseq", logToFile: true);

                string[] outputFiles = this.fileSystem.Directory.GetFiles(this.OutputDirectory, "results-summary.jsonl", SearchOption.AllDirectories);

                foreach (string file in outputFiles)
                {
                    string results = this.fileSystem.File.ReadAllText(file);                    

                    /*
                    MetaseqMetricsParser parser = new MetaseqMetricsParser(results);
                    IList<Metric> metrics = parser.Parse();

                    this.Logger.LogMetrics(
                        toolName: "Metaseq",
                        scenarioName: "Metaseq",
                        process.StartTime,
                        process.ExitTime,
                        metrics,
                        metricCategorization: $"{this.TrainingScript}",
                        scenarioArguments: commandArguments,
                        this.Tags,
                        telemetryContext);

                    */
                    await this.fileSystem.File.DeleteAsync(file);
                }
            }
        }

        internal class MetaseqState : State
        {
            public MetaseqState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool MetaseqInitialized
            { 
                get
                {
                    return this.Properties.GetValue<bool>(nameof(MetaseqState.MetaseqInitialized), false);
                }

                set
                {
                    this.Properties[nameof(MetaseqState.MetaseqInitialized)] = value;
                }
            }
        }
    }
}