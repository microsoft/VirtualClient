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
    using Newtonsoft.Json;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.PostgreSQLExecutor;

    /// <summary>
    /// MemcachedMemtier workload executor
    /// </summary>
    [UnixCompatible]
    public class MemcachedExecutor : VirtualClientComponent
    {
        /// <summary>
        /// The property name used by the server-side executor to define the number
        /// of Memcached server copies to run.
        /// </summary>
        internal const string ServerCopiesCount = nameof(ServerCopiesCount);

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
        /// The user who has the ssh identity registered for.
        /// </summary>
        public string Username
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.Username), out IConvertible username);
                return username?.ToString();
            }
        }

        /// <summary>
        /// Port on which server runs.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port));
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
        /// Server IpAddress on which Redis Server runs.
        /// </summary>
        protected string ServerIpAddress { get; set; }

        /// <summary>
        /// Client used to communicate with the hosted instance of the
        /// Virtual Client API at server side.
        /// </summary>
        protected IApiClient ServerApiClient { get; set; }

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
            await this.ValidatePlatformSupportAsync(cancellationToken);

            if (this.IsMultiRoleLayout())
            {
                ClientInstance clientInstance = this.GetLayoutClientInstance();
                string layoutIPAddress = clientInstance.IPAddress;

                this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);
                this.ThrowIfRoleNotSupported(clientInstance.Role);
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
        protected Task KillProcessesAsync(string processName, CancellationToken cancellationToken)
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

        private async Task ValidatePlatformSupportAsync(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                switch (this.Platform)
                {
                    case PlatformID.Unix:
                        LinuxDistributionInfo distroInfo = await this.SystemManagement.GetLinuxDistributionAsync(cancellationToken);

                        switch (distroInfo.LinuxDistribution)
                        {
                            case LinuxDistribution.Ubuntu:
                            case LinuxDistribution.Debian:
                            case LinuxDistribution.CentOS8:
                            case LinuxDistribution.RHEL8:
                            case LinuxDistribution.Mariner:
                                break;
                            default:
                                throw new WorkloadException(
                                    $"The Memcached Memtier benchmark workload is not supported on the current Linux distro " +
                                    $"'{distroInfo.LinuxDistribution}'.  Supported distros include: " +
                                    $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Ubuntu)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Debian)}" +
                                    $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.CentOS8)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.RHEL8)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Mariner)}",
                                    ErrorReason.LinuxDistributionNotSupported);
                        }

                        break;

                    default:
                        throw new WorkloadException(
                            $"The Memcached Memtier benchmark workload is currently not supported on the current platform/architecture " +
                            $"'{this.PlatformArchitectureName}'. Supported platform/architectures include: " +
                            $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64)}, " +
                            $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64)}",
                            ErrorReason.PlatformNotSupported);
                }
            }
        }

        internal class ServerState : State
        {
            public ServerState()
                : base()
            {
                this.ServerCopies = 1;
            }

            [JsonConstructor]
            public ServerState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            /// <summary>
            /// The number of copies/instances of the Memcached server to run simultaneously.
            /// </summary>
            public int ServerCopies
            {
                get
                {
                    return this.Properties.GetValue<int>(nameof(this.ServerCopies));
                }

                set
                {
                    this[nameof(this.ServerCopies)] = value;
                }
            }
        }
    }
}
