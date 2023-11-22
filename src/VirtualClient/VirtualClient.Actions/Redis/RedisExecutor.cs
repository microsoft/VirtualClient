// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json;
    using VirtualClient.Actions.NetworkPerformance;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Redis workload executor
    /// </summary>
    [UnixCompatible]
    public class RedisExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">An enumeration of key-value pairs that can control the execution of the component.</param>/param>
        protected RedisExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.ApiClientManager = dependencies.GetService<IApiClientManager>();
            this.SystemManagement = this.Dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// The Memtier benchmark will return an exit code of 130 when it is interupted while
        /// trying to write to standard output. This happens when Ctrl-C is used for example.
        /// We handle this error for this reason.
        /// </summary>
        protected static IEnumerable<int> SuccessExitCodes { get; } = new List<int>(ProcessProxy.DefaultSuccessCodes) { 130 };

        /// <summary>
        /// Provides the ability to create API clients for interacting with local as well as remote instances
        /// of the Virtual Client API service.
        /// </summary>
        protected IApiClientManager ApiClientManager { get; }

        /// <summary>
        /// Client used to communicate with the hosted instance of the
        /// Virtual Client API at server side.
        /// </summary>
        protected IApiClient ServerApiClient { get; set; }

        /// <summary>
        /// Server IpAddress on which Redis Server runs.
        /// </summary>
        protected string ServerIpAddress { get; set; }

        /// <summary>
        /// Cancellation Token Source for Server.
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// An interface that can be used to communicate with the underlying system.
        /// </summary>
        protected ISystemManagement SystemManagement { get; set; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes the commands.
        /// </summary>
        /// <param name="command">Command that needs to be executed</param>
        /// <param name="workingDir">The directory where we want to execute the command</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="successCodes">Alternative exit codes to use to represent successful process exit.</param>
        /// <returns>String output of the command.</returns>
        protected async Task ExecuteCommandAsync(string command, string workingDir, CancellationToken cancellationToken, IEnumerable<int> successCodes = null)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.Logger.LogTraceMessage($"Executing process '{command}'  at directory '{workingDir}'.");

                EventContext telemetryContext = EventContext.Persisted()
                    .AddContext("packageName", this.PackageName)
                    .AddContext("packagePath", workingDir)
                    .AddContext("command", "sudo")
                    .AddContext("commandArguments", command);

                await this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteProcess", telemetryContext, async () =>
                {
                    using (IProcessProxy process = this.SystemManagement.ProcessManager.CreateElevatedProcess(this.Platform, command, null, workingDir))
                    {
                        this.CleanupTasks.Add(() => process.SafeKill());
                        await process.StartAndWaitAsync(cancellationToken);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.LogProcessDetailsAsync(process, telemetryContext);

                            process.ThrowIfErrored<WorkloadException>(
                                successCodes ?? ProcessProxy.DefaultSuccessCodes,
                                errorReason: ErrorReason.WorkloadFailed);
                        }
                    }
                }).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Initializes the environment and dependencies for running the Redis Memtier workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.ValidatePlatformSupportAsync(cancellationToken);
            await this.EvaluateParametersAsync(cancellationToken);

            if (this.IsMultiRoleLayout())
            {
                ClientInstance clientInstance = this.GetLayoutClientInstance();
                string layoutIPAddress = clientInstance.IPAddress;

                this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);
                this.ThrowIfRoleNotSupported(clientInstance.Role);
            }
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            if (base.IsSupported())
            {
                bool isSupported = (this.Platform == PlatformID.Unix)
                && (this.CpuArchitecture == Architecture.X64 || this.CpuArchitecture == Architecture.Arm64);

                if (!isSupported)
                {
                    this.Logger.LogNotSupported("Redis", this.Platform, this.CpuArchitecture, EventContext.Persisted());
                }

                return isSupported;
            }
            else
            {
                return false;
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
                this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", this.ServerApiClient);
            }
        }

        private async Task ValidatePlatformSupportAsync(CancellationToken cancellationToken)
        {
            switch (this.Platform)
            {
                case PlatformID.Unix:
                    if (this.Platform == PlatformID.Unix)
                    {
                        LinuxDistributionInfo distroInfo = await this.SystemManagement.GetLinuxDistributionAsync(cancellationToken);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            switch (distroInfo.LinuxDistribution)
                            {
                                case LinuxDistribution.Ubuntu:
                                case LinuxDistribution.Debian:
                                case LinuxDistribution.CentOS8:
                                case LinuxDistribution.RHEL8:
                                    break;
                                default:
                                    throw new WorkloadException(
                                        $"The Redis Memtier benchmark workload is not supported on the current Linux distro " +
                                        $"'{distroInfo.LinuxDistribution}'.  Supported distros include: " +
                                        $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Ubuntu)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.Debian)}" +
                                        $"{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.CentOS8)},{Enum.GetName(typeof(LinuxDistribution), LinuxDistribution.RHEL8)}",
                                        ErrorReason.LinuxDistributionNotSupported);
                            }
                        }
                    }

                    break;

                default:
                    throw new WorkloadException(
                        $"The Redis Memtier benchmark workload is currently not supported on the current platform/architecture " +
                        $"'{this.PlatformArchitectureName}'." +
                        $" Supported platform/architectures include: " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.X64)}, " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(PlatformID.Unix, Architecture.Arm64)}",
                        ErrorReason.PlatformNotSupported);
            }
        }

        internal class ServerState : State
        {
            [JsonConstructor]
            public ServerState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            internal ServerState(IEnumerable<int> ports)
               : base()
            {
                if (ports?.Any() == true)
                {
                    this[nameof(this.Ports)] = string.Join(",", ports);
                }
            }

            /// <summary>
            /// The set of ports on which the Redis servers are running.
            /// </summary>
            public IEnumerable<int> Ports
            {
                get
                {
                    this.Properties.TryGetValue(nameof(this.Ports), out IConvertible ports);
                    return ports?.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries).Select(i => int.Parse(i.Trim()));
                }
            }
        }
    }
}
