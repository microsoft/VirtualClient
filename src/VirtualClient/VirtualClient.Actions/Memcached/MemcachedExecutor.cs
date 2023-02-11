// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Dependencies;

    /// <summary>
    /// MemcachedMemtier workload executor
    /// </summary>
    [UnixCompatible]
    public class MemcachedExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Name of memtier benchmark tool.
        /// </summary>
        protected const string Memtier = "Memtier";

        /// <summary>
        /// Name of MemcachedBenchmark tool.
        /// </summary>
        protected const string MemcachedBenchmark = "MemcachedBenchmark";

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleClientServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public MemcachedExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.SystemManagement = dependencies.GetService<ISystemManagement>();
            this.ApiClientManager = dependencies.GetService<IApiClientManager>();
            this.FileSystem = this.SystemManagement.FileSystem;
            this.PackageManager = this.SystemManagement.PackageManager;
            this.ProcessManager = this.SystemManagement.ProcessManager;

            // Supported roles for this client/server workload.
            this.SupportedRoles = new List<string>
            {
                ClientRole.Client,
                ClientRole.Server
            };
        }

        /// <summary>
        /// State representing server Instances.
        /// </summary>
        public State ServerCopiesCount { get; set; }

        /// <summary>
        /// Number of copies of Redis server instances to be created.
        /// </summary>
        public string Copies { get; set; }

        /// <summary>
        /// Client used to communicate with the hosted instance of the
        /// Virtual Client API at server side.
        /// </summary>
        public IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// The user who has the ssh identity registered for.
        /// </summary>
        public string Username
        {
            get
            {
                this.Parameters.TryGetValue(nameof(MemcachedExecutor.Username), out IConvertible username);
                return username?.ToString();
            }
        }

        /// <summary>
        /// Port on which server runs.
        /// </summary>
        public string Port
        {
            get
            {
                this.Parameters.TryGetValue(nameof(MemcachedExecutor.Port), out IConvertible port);
                return port?.ToString();
            }
        }

        /// <summary>
        /// Protocol to use at client side.
        /// </summary>
        public string Protocol
        {
            get
            {
                this.Parameters.TryGetValue(nameof(MemcachedExecutor.Protocol), out IConvertible protocol);
                return protocol?.ToString();
            }
        }

        /// <summary>
        /// Path to MemcachedMemtier Benchmark Package.
        /// </summary>
        public string PackagePath { get; set; }

        /// <summary>
        /// Server IpAddress on which Redis Server runs.
        /// </summary>
        protected string ServerIpAddress { get; set; }

        /// <summary>
        /// Path to Memcached Package.
        /// </summary>
        protected string MemcachedPackagePath { get; set; }

        /// <summary>
        /// Path to Memcached Package.
        /// </summary>
        protected string MemtierPackagePath { get; set; }

        /// <summary>
        /// Decides whether to Bind redis process to cores.
        /// </summary>
        protected string Bind
        {
            get
            {
                this.Parameters.TryGetValue(nameof(MemcachedExecutor.Bind), out IConvertible bind);
                return bind?.ToString();
            }
        }

        /// <summary>
        /// Provides the ability to create API clients for interacting with local as well as remote instances
        /// of the Virtual Client API service.
        /// </summary>
        protected IApiClientManager ApiClientManager { get; }

        /// <summary>
        /// Enables access to file system operations.
        /// </summary>
        protected IFileSystem FileSystem { get; }

        /// <summary>
        /// Provides access to the dependency packages on the system.
        /// </summary>
        protected IPackageManager PackageManager { get; }

        /// <summary>
        /// Provides the ability to create isolated operating system processes for running
        /// applications (e.g. workloads) on the system separate from the runtime.
        /// </summary>
        protected ProcessManager ProcessManager { get; }

        /// <summary>
        /// Provides access to dependencies required for interacting with the system, environment
        /// and runtime platform.
        /// </summary>
        protected ISystemManagement SystemManagement { get; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // The derived classes are expected to implement this method.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Initializes the environment and dependencies for running the Memcached Memtier workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.CheckPlatformSupport();
            await this.CheckDistroSupportAsync(telemetryContext, cancellationToken)
                .ConfigureAwait(false);

            if (this.IsMultiRoleLayout())
            {
                ClientInstance clientInstance = this.GetLayoutClientInstance();
                string layoutIPAddress = clientInstance.IPAddress;

                this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);
                this.ThrowIfRoleNotSupported(clientInstance.Role);
            }

            DependencyPath memtierPackage = await this.PackageManager.GetPackageAsync(MemtierInstallation.MemtierPackage, CancellationToken.None)
                    .ConfigureAwait(false);
            if (memtierPackage != null)
            {
                this.MemtierPackagePath = memtierPackage.Path;
            }
            else
            {
                throw new DependencyException(
                    $"Memtier package was not found on the system.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            DependencyPath memcachedPackage = await this.PackageManager.GetPackageAsync(MemcachedInstallation.MemcachedPackage, CancellationToken.None)
                .ConfigureAwait(false);
            if (memcachedPackage != null)
            {
                this.MemcachedPackagePath = memcachedPackage.Path;

            }
            else
            {
                throw new DependencyException(
                    $"Memcached package was not found on the system.",
                    ErrorReason.WorkloadDependencyMissing);
            }
        }

        /// <summary>
        /// Initializes API client.
        /// </summary>
        protected void InitializeApiClients()
        {
            IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();
            bool isSingleVM = !this.IsMultiRoleLayout();

            if (isSingleVM)
            {
                this.ServerApiClient = clientManager.GetOrCreateApiClient(IPAddress.Loopback.ToString(), IPAddress.Loopback);
            }
            else
            {
                ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                this.ServerApiClient = clientManager.GetOrCreateApiClient(serverIPAddress.ToString(), serverIPAddress);
            }
        }

        /// <summary>
        /// Executes the commands.
        /// </summary>
        /// <param name="command">The command to run.</param>
        /// <param name="arguments">The command line arguments to supply to the command.</param>
        /// <param name="workingDir">The working directory for the command.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected async Task ExecuteCommandAsync(string command, string arguments, string workingDir, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{command}' '{arguments}' at directory '{workingDir}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("packageName", this.PackageName)
                    .AddContext("packagePath", workingDir)
                    .AddContext("command", command)
                    .AddContext("commandArguments", arguments);

                await this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.ProcessManager.CreateElevatedProcess(this.Platform, command, arguments, workingDir))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken).ConfigureAwait();

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            await this.LogProcessDetailsAsync(process, telemetryContext)
                                .ConfigureAwait(false);

                            process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Gets all the processes running with the given name.
        /// </summary>
        /// <param name="processName">Name of the process.</param>
        /// <returns>List of processes with the given name.</returns>
        protected static IEnumerable<IProcessProxy> GetRunningProcessByName(string processName)
        {
            IEnumerable<IProcessProxy> processProxyList = null;
            var processlist = Process.GetProcesses();
            foreach (Process process in processlist)
            {
                if (process.ProcessName.Contains(processName))
                {
                    Process[] processesByName = Process.GetProcessesByName(process.ProcessName);
                    if (processesByName?.Any() ?? false)
                    {
                        if (processProxyList == null)
                        {
                            processProxyList = processesByName.Select((Process process) => new ProcessProxy(process));
                        }
                        else
                        {
                            foreach (Process proxy in processesByName)
                            {
                                _ = processProxyList.Append(new ProcessProxy(proxy));
                            }
                        }
                    }
                }
            }

            return processProxyList;
        }

        /// <summary>
        /// Kills the processes running with the given name.
        /// </summary>
        /// <param name="processName">The name of the process required to be killed.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns></returns>
        protected Task KillProcessesWithNameAsync(string processName, CancellationToken cancellationToken)
        {
            IEnumerable<IProcessProxy> processProxyList = GetRunningProcessByName(processName);
            if (processProxyList != null)
            {
                foreach (IProcessProxy process in processProxyList)
                {
                    Console.WriteLine("Killing process {0} with id {1}", process.Name, process.Id);
                    process.SafeKill();
                }
            }

            return this.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken);
        }

        private void CheckPlatformSupport()
        {
            switch (this.Platform)
            {
                case PlatformID.Unix:
                    break;
                default:
                    throw new WorkloadException(
                        $"The Memcached Memtier benchmark workload is currently not supported on the current platform/architecture " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture)}." +
                        $" Supported platform/architectures include: " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64)}, " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64)}",
                        ErrorReason.PlatformNotSupported);
            }
        }

        private async Task CheckDistroSupportAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.Platform == PlatformID.Unix)
            {
                var linuxDistributionInfo = await this.SystemManagement.GetLinuxDistributionAsync(cancellationToken)
                .ConfigureAwait(false);

                if (!cancellationToken.IsCancellationRequested)
                {
                    switch (linuxDistributionInfo.LinuxDistribution)
                    {
                        case LinuxDistribution.Ubuntu:
                        case LinuxDistribution.Debian:
                        case LinuxDistribution.CentOS8:
                        case LinuxDistribution.RHEL8:
                        case LinuxDistribution.Mariner:
                            break;
                        default:
                            throw new WorkloadException(
                                $"The Memcached Memtier benchmark workload is not supported on the current Linux distro - " +
                                $"{linuxDistributionInfo.LinuxDistribution}.  Supported distros include:" +
                                $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Ubuntu)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Debian)}" + 
                                $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.CentOS8)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.RHEL8)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Mariner)}",
                                ErrorReason.LinuxDistributionNotSupported);
                    }
                }
            }
        }
    }
}
