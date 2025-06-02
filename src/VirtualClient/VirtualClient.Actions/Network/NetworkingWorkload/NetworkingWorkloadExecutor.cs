// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions.NetworkPerformance
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Newtonsoft.Json.Linq;
    using Polly;
    using VirtualClient;
    using VirtualClient.Api;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Azure Networking Workload Executor tests networking between 2 VMs (client and server).
    /// </summary>
    public class NetworkingWorkloadExecutor : VirtualClientComponent
    {
        private static readonly object LockObject = new object();
        private static Task heartbeatTask;

        // Contains the background running process for the server-side
        // networking workload tool.
        private BackgroundWorkloadServer backgroundWorkloadServer;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkingWorkloadExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public NetworkingWorkloadExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.ServerOnlinePollingTimeout = TimeSpan.FromHours(1);
            this.StateConfirmationPollingTimeout = TimeSpan.FromMinutes(5);
            this.ClientExecutionRetryPolicy = Policy.Handle<Exception>().RetryAsync(3);
        }

        /// <summary>
        ///  Client used to communicate with the locally self-hosted instance of the
        /// Virtual Client API.
        /// </summary>
        public static IApiClient LocalApiClient { get; private set; }

        /// <summary>
        /// Client used to communicate with the target self-hosted instance of the
        /// Virtual Client API (i.e. the server-side instance).
        /// </summary>
        public static IApiClient ServerApiClient { get; private set; }

        /// <summary>
        /// Parameter defines the network buffer size for client to use in the workload toolset 
        /// tests.
        /// </summary>
        public string BufferSizeClient
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.BufferSizeClient), out IConvertible bufferSizeClient);
                return bufferSizeClient?.ToString();
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.BufferSizeClient)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the network buffer size for server to use in the workload toolset 
        /// tests.
        /// </summary>
        public string BufferSizeServer
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.BufferSizeServer), out IConvertible bufferSizeServer);
                return bufferSizeServer?.ToString();
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.BufferSizeServer)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the number of concurrent threads to use in the execution of the
        /// networking workload toolset tests.
        /// </summary>
        public int ThreadCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(NetworkingWorkloadExecutor.ThreadCount), 1);
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.ThreadCount)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the timeout to use when polling the server-side API for state changes.
        /// </summary>
        public TimeSpan StateConfirmationPollingTimeout { get; set; }

        /// <summary>
        /// Parameter defines the timeout to use when confirming the server is online.
        /// </summary>
        public TimeSpan ServerOnlinePollingTimeout { get; set; }

        /// <summary>
        /// Parameter defines the communication protocol (UDP, TCP) to use in the workload toolset 
        /// tests.
        /// </summary>
        public string Protocol
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.Protocol), out IConvertible protocol);
                return protocol?.ToString();
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.Protocol)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the test mode to use in the SockPerf workload toolset tests.
        /// </summary>
        public string TestMode
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.TestMode), out IConvertible testMode);
                return testMode?.ToString();
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.TestMode)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the test duration to use in the execution of the networking workload
        /// toolset tests.
        /// </summary>
        public int TestDuration
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(NetworkingWorkloadExecutor.TestDuration), 60);
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.TestDuration)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the name of the workload tool to run (e.g. NTttcp, SockPerf).
        /// </summary>
        public string ToolName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(NetworkingWorkloadExecutor.ToolName));
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.ToolName)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the message size to use in the workload toolset tests.
        /// </summary>
        public int MessageSize
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(NetworkingWorkloadExecutor.MessageSize), 512);
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.MessageSize)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the number of connections to use in the workload toolset tests.
        /// </summary>
        public int Connections
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(NetworkingWorkloadExecutor.Connections), 8);
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.Connections)] = value;
            }
        }
        
        /// <summary>
        /// Parameter defines the warmup time to use in the workload toolset tests.
        /// </summary>
        public int WarmupTime
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(NetworkingWorkloadExecutor.WarmupTime), 8);
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.WarmupTime)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the delay time to use in the workload toolset tests.
        /// </summary>
        public int DelayTime
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(NetworkingWorkloadExecutor.DelayTime), 0);
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.DelayTime)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the port used by first thread of the tool.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(NetworkingWorkloadExecutor.Port), 5001);
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.Port)] = value;
            }
        }

        /// <summary>
        /// Parameter indicates that the server will work in multi-client mode in the workload toolset tests.
        /// </summary>
        public bool? ReceiverMultiClientMode
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.ReceiverMultiClientMode), out IConvertible receiverMultiClientMode);
                return receiverMultiClientMode?.ToBoolean(CultureInfo.InvariantCulture);
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.ReceiverMultiClientMode)] = value;
            }
        }

        /// <summary>
        /// Parameter indicates that this is the last client when test is with multi-client mode in the workload toolset tests.
        /// </summary>
        public bool? SenderLastClient
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.SenderLastClient), out IConvertible senderLastClient);
                return senderLastClient?.ToBoolean(CultureInfo.InvariantCulture);
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.SenderLastClient)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the number of threads per each server port in the workload toolset tests.
        /// </summary>
        public int? ThreadsPerServerPort
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.ThreadsPerServerPort), out IConvertible threadsPerServerPort);
                return threadsPerServerPort?.ToInt32(CultureInfo.InvariantCulture);
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.ThreadsPerServerPort)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the number of connections in each sender thread in the workload toolset tests.
        /// </summary>
        public int? ConnectionsPerThread
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.ConnectionsPerThread), out IConvertible connectionsPerThread);
                return connectionsPerThread?.ToInt32(CultureInfo.InvariantCulture);
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.ConnectionsPerThread)] = value;
            }
        }

        /// <summary>
        /// The Cool down period for Virtual Client Component.
        /// </summary>
        public TimeSpan CoolDownPeriod
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.CoolDownPeriod), TimeSpan.FromSeconds(0));
            }
        }

        /// <summary>
        /// Parameter defines the differentiator for which to convey the number of interrupts in the workload toolset tests.
        /// </summary>
        public string DevInterruptsDifferentiator
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.DevInterruptsDifferentiator), out IConvertible devInterruptsDifferentiator);
                return devInterruptsDifferentiator?.ToString();
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.DevInterruptsDifferentiator)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the number of messages-per-second for SockPerf workload toolset tests.
        /// </summary>
        public string MessagesPerSecond
        {
            get
            {
                this.Parameters.TryGetValue(nameof(NetworkingWorkloadExecutor.MessagesPerSecond), out IConvertible messagesPerSecond);
                return messagesPerSecond?.ToString();
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.MessagesPerSecond)] = value;
            }
        }

        /// <summary>
        /// Parameter defines the confidence level used for calculating the confidence intervals.
        /// </summary>
        public double? ConfidenceLevel
        {
            get
            {
                return this.Parameters.GetValue<double>(nameof(NetworkingWorkloadExecutor.ConfidenceLevel), 99);
            }

            set
            {
                this.Parameters[nameof(NetworkingWorkloadExecutor.ConfidenceLevel)] = value;
            }
        }

        /// <summary>
        /// Cancellation Token Source for Server.
        /// </summary>
        protected CancellationTokenSource ServerCancellationSource { get; set; }

        /// <summary>
        /// The retry policy to apply to the client-side execution workflow.
        /// </summary>
        protected IAsyncPolicy ClientExecutionRetryPolicy { get; set; }

        /// <summary>
        /// Returns true if the local instance is in the Client role.
        /// </summary>
        protected bool IsInClientRole { get; set; }

        /// <summary>
        /// Returns true if the local instance is in the Server role.
        /// </summary>
        protected bool IsInServerRole { get; set; }

        /// <summary>
        /// The role of the current Virtual Client instance. Supported roles = Client or Server
        /// </summary>
        protected string Role { get; private set; }

        /// <summary>
        /// Poll the server until the state is deleted/not found.
        /// </summary>
        /// <param name="client">The API client instance.</param>
        /// <param name="stateId">The unique ID of the state object.</param>
        /// <param name="timeout">The period of time for which the client tries to get the state before timing out. </param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <returns>
        /// An <see cref="HttpResponseMessage"/> containing the state object/definition at last poll call.
        /// </returns>
        internal static async Task<HttpResponseMessage> PollUntilStateDeletedAsync(IApiClient client, string stateId, TimeSpan timeout, CancellationToken cancellationToken)
        {
            stateId.ThrowIfNullOrWhiteSpace(nameof(stateId));

            DateTime pollingTimeout = DateTime.UtcNow.Add(timeout);
            HttpResponseMessage response = null;
            bool stateStillExists = true;

            do
            {
                try
                {
                    response = await client.GetStateAsync(stateId, cancellationToken).ConfigureAwait(false);
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        stateStillExists = false;
                    }
                }
                catch
                {
                    // State not available on server yet.
                }
                finally
                {
                    if (stateStillExists)
                    {
                        if (DateTime.UtcNow >= pollingTimeout)
                        {
                            throw new WorkloadException(
                                $"Polling for deletion of state '{stateId}' timed out (timeout={timeout}).",
                                ErrorReason.ApiStatePollingTimeout);
                        }

                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            while (stateStillExists && !cancellationToken.IsCancellationRequested);

            return response;
        }

        /// <summary>
        /// Makes an API call to save the state on the target system hosting the API client.
        /// </summary>
        internal static async Task SaveStateAsync(IApiClient apiClient, string stateId, State state, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            Item<State> stateUpdate = new Item<State>(stateId, state);
            HttpResponseMessage response = await apiClient.UpdateStateAsync(stateId, JObject.FromObject(stateUpdate), cancellationToken)
                .ConfigureAwait(false);

            telemetryContext.AddResponseContext(response, "saveStateResponse");
            response.ThrowOnError<WorkloadException>();
        }

        /// <summary>
        /// Get networking workload state Item.
        /// </summary>
        /// <returns>Networking workload state item stored in the state id.</returns>
        internal static async Task SendInstructionsAsync<TState>(IApiClient apiClient, Item<TState> instructions, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                HttpResponseMessage response = await NetworkingWorkloadExecutor.ServerApiClient.SendInstructionsAsync(JObject.FromObject(instructions), cancellationToken)
                    .ConfigureAwait(false);

                telemetryContext.AddResponseContext(response, "sendInstructionsResponse");
                response.ThrowOnError<WorkloadException>();
            }
            catch (Exception exc)
            {
                Console.WriteLine($"Send instructions error: {exc.Message}");
            }
        }

        /// <summary>
        /// Get networking workload state Item.
        /// </summary>
        /// <returns>Networking workload state item stored in the state id.</returns>
        internal static async Task UpdateStateAsync(IApiClient apiClient, Item<NetworkingWorkloadState> state, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await NetworkingWorkloadExecutor.LocalApiClient.UpdateStateAsync(state.Id, JObject.FromObject(state), cancellationToken)
                .ConfigureAwait(false);

            telemetryContext.AddResponseContext(response, "updateStateResponse");
            response.ThrowOnError<WorkloadException>();
        }

        /// <summary>
        /// Disposes of resources used by the class instance.
        /// </summary>
        /// <param name="disposing">If dispose the server cancellation source.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (!this.disposed)
                {
                    if (this.IsInServerRole)
                    {
                        VirtualClientRuntime.ReceiveInstructions -= this.OnInstructionsReceived;
                        this.ServerCancellationSource?.Dispose();
                    }
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Intialize Networking workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // Emit heartbeats on the system so that we can determine if the workload
            // is online and running.
            if (NetworkingWorkloadExecutor.heartbeatTask == null)
            {
                NetworkingWorkloadExecutor.heartbeatTask = this.StartHeartbeatTask(TimeSpan.FromMinutes(2), telemetryContext, cancellationToken);
            }

            ClientInstance clientInstance = this.GetLayoutClientInstance(this.AgentId);
            string layoutIPAddress = clientInstance.IPAddress;

            this.Logger.LogTraceMessage($"Layout-Defined IP Address: {layoutIPAddress}");
            this.Logger.LogTraceMessage($"Layout-Defined Role: {clientInstance.Role}");

            this.ThrowIfLayoutNotDefined();
            this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);

            DependencyPath workloadPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);
            telemetryContext.AddContext("package", workloadPackage);

            this.Role = clientInstance.Role;
            this.IsInClientRole = this.IsInRole(ClientRole.Client);
            this.IsInServerRole = !this.IsInClientRole;

            if (this.IsInServerRole)
            {
                this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            }

            EventContext.PersistentProperties["role"] = this.Role;

            this.InitializeApiClients();
        }

        /// <summary>
        /// Returns true/false whether the component should execute on the system/platform.
        /// </summary>
        /// <returns>Boolean if the workload is supported on current platform.</returns>
        protected override bool IsSupported()
        {
            bool isSupported = false;
            switch (this.ToolName.ToLowerInvariant())
            {
                case "cps":
                    isSupported = this.Platform == PlatformID.Win32NT || this.Platform == PlatformID.Unix;
                    break;

                case "latte":
                    isSupported = this.Platform == PlatformID.Win32NT;
                    break;

                case "ntttcp":
                    isSupported = this.Platform == PlatformID.Win32NT || this.Platform == PlatformID.Unix;
                    break;

                case "sockperf":
                    isSupported = this.Platform == PlatformID.Unix;
                    break;
            }

            return isSupported;
        }

        /// <summary>
        /// Executes Networking workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.IsInClientRole)
            {
                await this.ExecuteClientAsync(telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (this.IsInServerRole)
            {
                await this.ExecuteServerAsync(telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                throw new NotSupportedException($"The role: {this.Role} is not supported.");
            }

            // TO DO: Remove once we have Loop Executor.
            await this.WaitAsync(this.CoolDownPeriod, cancellationToken);

        }

        /// <summary>
        /// Creates the workload executor for the type of tool.
        /// </summary>
        protected virtual NetworkingWorkloadToolExecutor CreateWorkloadExecutor(NetworkingWorkloadTool tool)
        {
            NetworkingWorkloadToolExecutor action = null;

            if (this.IsInClientRole)
            {
                switch (tool)
                {
                    case NetworkingWorkloadTool.CPS:
                        action = new CPSClientExecutor(this);
                        break;

                    case NetworkingWorkloadTool.NTttcp:
                        action = new NTttcpClientExecutor(this);
                        break;

                    case NetworkingWorkloadTool.Latte:
                        action = new LatteClientExecutor(this);
                        break;

                    case NetworkingWorkloadTool.SockPerf:
                        action = new SockPerfClientExecutor(this);
                        break;

                    default:
                        throw new NotSupportedException($"The '{tool}' workload is not supported for the '{this.Role}' role on platform '{this.Platform}'.");
                }
            }
            else
            {
                switch (tool)
                {
                    case NetworkingWorkloadTool.CPS:
                        action = new CPSServerExecutor(this);
                        break;

                    case NetworkingWorkloadTool.NTttcp:
                        action = new NTttcpServerExecutor(this);
                        break;

                    case NetworkingWorkloadTool.Latte:
                        action = new LatteServerExecutor(this);
                        break;

                    case NetworkingWorkloadTool.SockPerf:
                        action = new SockPerfServerExecutor(this);
                        break;

                    default:
                        throw new NotSupportedException($"The '{tool}' workload is not supported for the '{this.Role}' role on platform '{this.Platform}'.");
                }
            }

            return action;
        }

        /// <summary>
        /// Initializes the API clients for synchronizing between the different instances of
        /// the Virtual Client.
        /// </summary>
        protected void InitializeApiClients()
        {
            IApiClientManager clientManager = this.Dependencies.GetService<IApiClientManager>();
            ClientInstance localInstance = this.GetLayoutClientInstance(this.AgentId);

            NetworkingWorkloadExecutor.LocalApiClient = clientManager.GetOrCreateApiClient(localInstance.Name, localInstance);

            if (this.IsInClientRole)
            {
                ClientInstance serverInstance = this.GetLayoutClientInstances(ClientRole.Server).First();
                IPAddress.TryParse(serverInstance.IPAddress, out IPAddress serverIPAddress);

                // It is important that we reuse the API client. The HttpClient created underneath will need to use a
                // new connection from the connection pool typically for each instance created. Especially for the case with
                // this workload that is testing network resources, we need to be very cognizant of our usage of TCP connections.
                NetworkingWorkloadExecutor.ServerApiClient = clientManager.GetOrCreateApiClient(serverInstance.Name, serverInstance);
                this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", NetworkingWorkloadExecutor.ServerApiClient);
            }
        }

        /// <summary>
        /// On executes receiving instructions from client. 
        /// </summary>
        protected void OnInstructionsReceived(object sender, JObject instructions)
        {
            lock (NetworkingWorkloadExecutor.LockObject)
            {
                try
                {
                    EventContext telemetryContext = EventContext.Persisted()
                        .AddContext("instructions", instructions);

                    if (VirtualClientRuntime.IsApiOnline)
                    {
                        this.Logger.LogMessage($"{nameof(NetworkingWorkloadExecutor)}.InstructionsReceived", telemetryContext, () =>
                        {
                            CancellationToken cancellationToken = this.ServerCancellationSource.Token;

                            this.Logger.LogTraceMessage($"{nameof(NetworkingWorkloadExecutor)}.Notification = {instructions}");

                            Item<State> notification = instructions.ToObject<Item<State>>();

                            if (notification.Definition.Properties.ContainsKey(nameof(NetworkingWorkloadState.Tool)))
                            {
                                NetworkingWorkloadState serverInstructions = new NetworkingWorkloadState(notification.Definition.Properties);
                                telemetryContext.AddClientRequestId(serverInstructions.ClientRequestId);

                                if (serverInstructions.ToolState == NetworkingWorkloadToolState.Stop)
                                {
                                    this.Logger.LogTraceMessage($"Synchronization: Stopping all workloads...");
                                    this.StopServerTool(telemetryContext);
                                    this.DeleteWorkloadStateAsync(telemetryContext, cancellationToken).GetAwaiter().GetResult();
                                }
                                else if (serverInstructions.ToolState == NetworkingWorkloadToolState.Start)
                                {
                                    this.Logger.LogTraceMessage($"Synchronization: Starting {serverInstructions.Tool} workload...");
                                    this.StopServerTool(telemetryContext);
                                    this.DeleteWorkloadStateAsync(telemetryContext, cancellationToken).GetAwaiter().GetResult();

                                    // The client will pass the settings to the server side. The server side will need to be updated
                                    // to use those settings specified below. (e.g. communications protocol, concurrent threads, network buffer size).
                                    this.PackageName = serverInstructions.PackageName;
                                    this.ToolName = serverInstructions.Tool.ToString();
                                    this.Scenario = serverInstructions.Scenario;
                                    this.Protocol = serverInstructions.Protocol;
                                    this.ThreadCount = serverInstructions.ThreadCount;
                                    this.BufferSizeClient = serverInstructions.BufferSizeClient;
                                    this.BufferSizeServer = serverInstructions.BufferSizeServer;
                                    this.TestMode = serverInstructions.TestMode;
                                    this.TestDuration = serverInstructions.TestDuration;
                                    this.MessageSize = serverInstructions.MessageSize;
                                    this.Connections = serverInstructions.Connections;
                                    this.WarmupTime = serverInstructions.WarmupTime;
                                    this.DelayTime = serverInstructions.DelayTime;
                                    this.Port = serverInstructions.Port;
                                    this.ReceiverMultiClientMode = serverInstructions.ReceiverMultiClientMode;
                                    this.SenderLastClient = serverInstructions.SenderLastClient;
                                    this.ThreadsPerServerPort = serverInstructions.ThreadsPerServerPort;
                                    this.ConnectionsPerThread = serverInstructions.ConnectionsPerThread;
                                    this.DevInterruptsDifferentiator = serverInstructions.DevInterruptsDifferentiator;
                                    this.MessagesPerSecond = serverInstructions.MessagesPerSecond;
                                    this.ConfidenceLevel = serverInstructions.ConfidenceLevel;
                                    this.ProfilingEnabled = serverInstructions.ProfilingEnabled;
                                    this.ProfilingScenario = serverInstructions.ProfilingScenario;
                                    this.ProfilingPeriod = serverInstructions.ProfilingPeriod;
                                    this.ProfilingWarmUpPeriod = serverInstructions.ProfilingWarmUpPeriod;

                                    if (serverInstructions.Metadata?.Any() == true)
                                    {
                                        this.Metadata.AddRange(serverInstructions.Metadata, withReplace: true);
                                    }

                                    if (serverInstructions.Extensions?.Any() == true)
                                    {
                                        this.Extensions.AddRange(serverInstructions.Extensions, withReplace: true);
                                    }

                                    NetworkingWorkloadExecutor.SaveStateAsync(
                                        NetworkingWorkloadExecutor.LocalApiClient,
                                        nameof(NetworkingWorkloadState),
                                        serverInstructions,
                                        telemetryContext,
                                        cancellationToken).GetAwaiter().GetResult();

                                    this.StartServerTool(serverInstructions.Tool, serverInstructions.ClientRequestId, telemetryContext, cancellationToken);
                                }
                            }
                        });
                    }
                }
                catch
                {
                    // We should not surface exceptions that cause the eventing system
                    // issues.
                }
            }
        }

        /// <summary>
        /// Makes an API call to save the state on the target server API to stop all workloads.
        /// </summary>
        protected async Task RequestStopAllWorkloadsAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await NetworkingWorkloadExecutor.SendInstructionsAsync(
                NetworkingWorkloadExecutor.ServerApiClient,
                new Item<State>(nameof(NetworkingWorkloadState), new NetworkingWorkloadState(
                    packageName: this.PackageName,
                    scenario: this.Scenario,
                    tool: NetworkingWorkloadTool.Undefined,
                    toolState: NetworkingWorkloadToolState.Stop)),
                telemetryContext,
                cancellationToken).ConfigureAwait(false);

            // Confirm the server has stopped all workloads
            await NetworkingWorkloadExecutor.PollUntilStateDeletedAsync(
                NetworkingWorkloadExecutor.ServerApiClient,
                nameof(NetworkingWorkloadState),
                this.StateConfirmationPollingTimeout,
                cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the client side of the networking workload.
        /// </summary>
        private async Task ExecuteClientAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            StringComparison ignoreCase = StringComparison.OrdinalIgnoreCase;
            if (string.Equals(this.ToolName, NetworkingWorkloadTool.CPS.ToString(), ignoreCase))
            {
                await this.ExecuteClientToolAsync(NetworkingWorkloadTool.CPS, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (string.Equals(this.ToolName, NetworkingWorkloadTool.Latte.ToString(), ignoreCase))
            {
                if (this.Platform != PlatformID.Win32NT)
                {
                    throw new NotSupportedException($"The '{this.ToolName}' workload is not supported on platforms other than Windows.");
                }

                await this.ExecuteClientToolAsync(NetworkingWorkloadTool.Latte, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (string.Equals(this.ToolName, NetworkingWorkloadTool.NTttcp.ToString(), ignoreCase))
            {
                await this.ExecuteClientToolAsync(NetworkingWorkloadTool.NTttcp, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
            else if (string.Equals(this.ToolName, NetworkingWorkloadTool.SockPerf.ToString(), ignoreCase))
            {
                if (this.Platform != PlatformID.Unix)
                {
                    throw new NotSupportedException($"The '{this.ToolName}' workload is not supported on platforms other than Unix/Linux.");
                }

                await this.ExecuteClientToolAsync(NetworkingWorkloadTool.SockPerf, telemetryContext, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                throw new NotSupportedException($"A workload with the name '{this.ToolName}' is not supported.");
            }
        }

        /// <summary>
        /// Execute the tool on client side,Following the steps below:
        /// 1. Poll for the server to be online.
        /// 2. Send Notification to server to start instance of the specificed tool.
        /// 3. Execute client side of the tool.
        /// </summary>
        private Task ExecuteClientToolAsync(NetworkingWorkloadTool toolName, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone()
                .AddContext(nameof(toolName), toolName.ToString());

            return this.Logger.LogMessageAsync($"{nameof(NetworkingWorkloadExecutor)}.ExecuteClient", relatedContext, async () =>
            {
                await this.ClientExecutionRetryPolicy.ExecuteAsync(async () =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        // The request ID enables correlation between client/server and server operations.
                        Guid requestId = Guid.NewGuid();
                        relatedContext.AddClientRequestId(requestId);

                        this.Logger.LogTraceMessage("Synchronization: Wait for server online...");

                        // 1) Confirm server is online.
                        // ===========================================================================
                        await NetworkingWorkloadExecutor.ServerApiClient.PollForHeartbeatAsync(this.ServerOnlinePollingTimeout, cancellationToken, logger: this.Logger);

                        // 2) Wait for the server to signal the eventing API is online.
                        // ===========================================================================
                        await NetworkingWorkloadExecutor.ServerApiClient.PollForServerOnlineAsync(this.ServerOnlinePollingTimeout, cancellationToken, logger: this.Logger);

                        // 3) Request the server to stop ALL workload processes
                        // ===========================================================================
                        this.Logger.LogTraceMessage("Synchronization: Request server to stop all workloads...");

                        await this.RequestStopAllWorkloadsAsync(relatedContext, cancellationToken);

                        // 4) Request the server start the next workload.
                        // ===========================================================================
                        NetworkingWorkloadState workloadInstructions = new NetworkingWorkloadState(
                            this.PackageName,
                            this.Scenario,
                            toolName,
                            NetworkingWorkloadToolState.Start,
                            this.Protocol,
                            this.ThreadCount,
                            this.BufferSizeClient,
                            this.BufferSizeServer,
                            this.Connections,
                            this.TestDuration,
                            this.WarmupTime,
                            this.DelayTime,
                            this.TestMode,
                            this.MessageSize,
                            this.Port,
                            this.ReceiverMultiClientMode,
                            this.SenderLastClient,
                            this.ThreadsPerServerPort,
                            this.ConnectionsPerThread,
                            this.DevInterruptsDifferentiator,
                            this.MessagesPerSecond,
                            this.ConfidenceLevel,
                            this.ProfilingEnabled,
                            this.ProfilingScenario,
                            this.ProfilingPeriod.ToString(),
                            this.ProfilingWarmUpPeriod.ToString(),
                            requestId);

                        Item<State> instructions = new Item<State>(nameof(NetworkingWorkloadState), workloadInstructions);
                        relatedContext.AddContext("instructions", instructions);
                        this.Logger.LogTraceMessage($"Synchronization: Request server to start {toolName} workload...");

                        await NetworkingWorkloadExecutor.SendInstructionsAsync(
                            NetworkingWorkloadExecutor.ServerApiClient,
                            instructions,
                            relatedContext,
                            cancellationToken);

                        // 5) Confirm the server has started the requested workload.
                        // ===========================================================================
                        await NetworkingWorkloadExecutor.ServerApiClient.PollForExpectedStateAsync<NetworkingWorkloadState>(
                            nameof(NetworkingWorkloadState),
                            (serverState) =>
                            {
                                return serverState.Scenario == workloadInstructions.Scenario
                                    && serverState.Tool == workloadInstructions.Tool
                                    && serverState.ToolState == NetworkingWorkloadToolState.Running;
                            },
                            this.StateConfirmationPollingTimeout,
                            cancellationToken);

                        this.Logger.LogTraceMessage("Synchronization: Server workload startup confirmed...");
                        this.Logger.LogTraceMessage("Synchronization: Start client workload...");

                        // 6) Execute the client workload.
                        // ===========================================================================
                        NetworkingWorkloadToolExecutor executor = this.CreateWorkloadExecutor(toolName);
                        executor.ClientRequestId = requestId;

                        try
                        {
                            await executor.ExecuteAsync(cancellationToken)
                                .ConfigureAwait(false);
                        }
                        finally
                        {
                            this.Logger.LogTraceMessage("Synchronization: Wait for server to stop workload...");

                            await this.RequestStopAllWorkloadsAsync(telemetryContext, cancellationToken)
                                .ConfigureAwait(false);
                        }
                    }

                }).ConfigureAwait(false);
            });
        }

        private Task ExecuteServerAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return this.Logger.LogMessageAsync($"{nameof(NetworkingWorkloadExecutor)}.ExecuteServer", telemetryContext, async () =>
            {
                // The current model uses an event handler to subscribe to events that are processed by the 
                // Events API. Event handlers have a signature that is may be too strict to 
                using (this.ServerCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                {
                    try
                    {
                        // Subscribe to notifications from the Events API. The client passes instructions
                        // to the server via this API.
                        VirtualClientRuntime.ReceiveInstructions += this.OnInstructionsReceived;
                        VirtualClientRuntime.SetEventingApiOnline(true);

                        await this.WaitAsync(this.ServerCancellationSource.Token)
                            .ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
                    }
                    finally
                    {
                        // Cleanup the event subscription to avoid any issues with memory leaks.
                        VirtualClientRuntime.ReceiveInstructions -= this.OnInstructionsReceived;
                        VirtualClientRuntime.SetEventingApiOnline(false);
                    }
                }
            });
        }

        private Task DeleteWorkloadStateAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            EventContext relatedContext = telemetryContext.Clone();

            return this.Logger.LogMessageAsync($"{nameof(NetworkingWorkloadExecutor)}.ResetState", relatedContext, async () =>
            {
                HttpResponseMessage response = await NetworkingWorkloadExecutor.LocalApiClient.DeleteStateAsync(
                    nameof(NetworkingWorkloadState),
                    cancellationToken).ConfigureAwait(false);

                if (response.StatusCode != HttpStatusCode.NoContent)
                {
                    response.ThrowOnError<WorkloadException>(ErrorReason.HttpNonSuccessResponse);
                }
            });
        }

        private void StartServerTool(NetworkingWorkloadTool toolName, Guid? clientRequestId, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                EventContext relatedContext = telemetryContext.Clone()
                    .AddContext(nameof(toolName), toolName.ToString());

                this.Logger.LogMessage($"{this.TypeName}.StartServerWorkload", relatedContext, () =>
                {
                    CancellationTokenSource cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    NetworkingWorkloadToolExecutor action = this.CreateWorkloadExecutor(toolName);
                    action.ClientRequestId = clientRequestId;

                    this.backgroundWorkloadServer = new BackgroundWorkloadServer
                    {
                        Name = toolName,
                        CancellationSource = cancellationSource,
                        BackgroundTask = action.ExecuteAsync(cancellationSource.Token)
                    };
                });
            }
            catch
            {
                // Do not crash the application
            }
        }

        private void StopServerTool(EventContext telemetryContext)
        {
            try
            {
                if (this.backgroundWorkloadServer != null)
                {
                    EventContext relatedContext = telemetryContext.Clone();

                    this.Logger.LogMessage($"{this.TypeName}.StopServerWorkload", relatedContext, () =>
                    {
                        this.backgroundWorkloadServer.CancellationSource?.Cancel();
                        this.backgroundWorkloadServer.BackgroundTask.GetAwaiter().GetResult();
                        this.backgroundWorkloadServer.BackgroundTask.Dispose();
                        this.backgroundWorkloadServer = null;
                    });
                }
            }
            catch
            {
                // Do not crash the application
            }
        }

        /// <summary>
        /// Used to track and managed the execution of the background server-side workload
        /// process over a long running period of time.
        /// </summary>
        private class BackgroundWorkloadServer
        {
            public NetworkingWorkloadTool Name { get; set; }

            public Task BackgroundTask { get; set; }

            public CancellationTokenSource CancellationSource { get; set; }
        }
    }
}
