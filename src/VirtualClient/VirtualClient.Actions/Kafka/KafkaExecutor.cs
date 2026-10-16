// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// The Kafka client action
    /// </summary>
    [SupportedPlatforms("win-x64,win-arm64,linux-arm64,linux-x64")]
    public abstract class KafkaExecutor : VirtualClientComponent
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public KafkaExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.ApiClientManager = dependencies.GetService<IApiClientManager>();
            this.SystemManagement = this.Dependencies.GetService<ISystemManagement>();
        }

        /// <summary>
        /// Parameter defines the number of server instances/copies to run.
        /// </summary>
        public int ServerInstanceCount
        {
            get
            {
                return this.Parameters.GetValue<int>(new string[] { nameof(this.ServerInstanceCount), "ServerInstances" }, 3);
            }
        }

        /// <summary>
        /// Parameter defines the number of client instances/copies to run.
        /// </summary>
        public int MaxClientInstanceCount
        {
            get
            {
                return this.Parameters.GetValue<int>(new string[] { nameof(this.MaxClientInstanceCount), "MaxClientInstances" }, 3);
            }
        }

        /// <summary>
        /// Parameter defines the number of client instances/copies to run.
        /// </summary>
        public int Partitions
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Partitions), 6);
            }
        }

        /// <summary>
        /// Java Development Kit package name.
        /// </summary>
        public string JdkPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.JdkPackageName), "javadevelopmentkit");
            }
        }

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
        /// Server IpAddress on which Kafka Server runs.
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
        /// Path to Kafka server package.
        /// </summary>
        protected string KafkaPackagePath { get; set; }

        /// <summary>
        /// Path to the Java Development Kit package.
        /// </summary>
        protected string JDKPackagePath { get; set; }

        /// <summary>
        /// Command type to be used based on platform.
        /// </summary>
        protected string PlatformSpecificCommandType { get; set; }

        /// <summary>
        /// Validates the component definition for requirements.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            if (this.MaxClientInstanceCount > this.Partitions)
            {
                throw new ArgumentException(
                    $"Parameter {nameof(this.MaxClientInstanceCount)} {this.MaxClientInstanceCount} should be <= parameter {nameof(this.Partitions)} {this.Partitions}");
            }
        }

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
        /// Initializes the environment and dependencies for running the Kafka workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.EvaluateParametersAsync(cancellationToken);

            if (this.IsMultiRoleLayout())
            {
                ClientInstance clientInstance = this.GetLayoutClientInstance();
                string layoutIPAddress = clientInstance.IPAddress;

                this.ThrowIfLayoutClientIPAddressNotFound(layoutIPAddress);
                this.ThrowIfRoleNotSupported(clientInstance.Role);
            }

            this.InitializeApiClients();

            DependencyPath kafkaPackage = await this.GetPackageAsync(this.PackageName, cancellationToken);
            this.KafkaPackagePath = kafkaPackage.Path;

            DependencyPath jdkPackage = await this.GetPackageAsync(this.JdkPackageName, cancellationToken);
            this.JDKPackagePath = jdkPackage.Path;
            this.SetEnvironmentVariable(EnvironmentVariable.JAVA_HOME, this.JDKPackagePath, EnvironmentVariableTarget.Process);

            switch (this.Platform)
            {
                case PlatformID.Win32NT:
                    this.PlatformSpecificCommandType = "cmd";
                    break;

                case PlatformID.Unix:
                    this.PlatformSpecificCommandType = "bash";
                    break;

                default:
                    throw new WorkloadException(
                        $"The kafka workload is not supported on the current platform/architecture " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture)}." +
                        ErrorReason.PlatformNotSupported);
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
                this.ServerIpAddress = serverIPAddress.ToString();

                this.ServerApiClient = clientManager.GetOrCreateApiClient(serverIPAddress.ToString(), serverIPAddress);
                this.RegisterToSendExitNotifications($"{this.TypeName}.ExitNotification", this.ServerApiClient);
            }
        }

        /// <summary>
        /// Returns platform specific formatted command string
        /// </summary>
        protected string GetPlatformFormattedCommandArguement(string scriptPath, string scriptArgs)
        {
            string scriptCommand = $"{scriptPath} {scriptArgs}";
            string commandArgs = this.Platform == PlatformID.Win32NT ? $"/c {scriptCommand}" : scriptCommand;

            return commandArgs;
        }

        /// <summary>
        /// Executes a command within an isolated process.
        /// </summary>
        /// <param name="command">The command to execute within the process.</param>
        /// <param name="commandArguments">The arguments to supply to the command.</param>
        /// <param name="workingDirectory">The working directory from which the command should be executed.</param>
        /// <param name="telemetryContext">Provides context information to include with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the process execution.</param>
        /// <param name="runElevated">True to run the process with elevated privileges. Default = false</param>
        /// <param name="username">The username to use for executing the command. Note that this is applied ONLY for Unix/Linux scenarios.</param>
        /// <param name="beforeExecution">Optional delegate/action allows the user to configure the process after creation but before execution.</param>
        /// <returns>The process that executed the command.</returns>
        protected async Task<IProcessProxy> RunProcessAsync(
            string command,
            string commandArguments,
            string workingDirectory,
            EventContext telemetryContext,
            CancellationToken cancellationToken,
            bool runElevated = false,
            string username = null,
            Action<IProcessProxy> beforeExecution = null)
        {
            command.ThrowIfNullOrWhiteSpace(nameof(command));
            telemetryContext.ThrowIfNull(nameof(telemetryContext));

            if (!string.IsNullOrWhiteSpace(username))
            {
                if (this.Platform != PlatformID.Unix)
                {
                    throw new NotSupportedException($"The application of a username is not supported on '{this.Platform}' platform/architecture systems.");
                }

                if (!runElevated)
                {
                    throw new NotSupportedException($"The application of a username is not supported unless running elevated. Use the '{nameof(runElevated)}' parameter.");
                }
            }

            EventContext relatedContext = telemetryContext.Clone()
                .AddContext(nameof(command), command)
                .AddContext(nameof(commandArguments), commandArguments)
                .AddContext(nameof(workingDirectory), workingDirectory)
                .AddContext(nameof(runElevated), runElevated);

            IProcessProxy process = null;
            if (!cancellationToken.IsCancellationRequested)
            {
                ProcessManager processManager = this.Dependencies.GetService<ProcessManager>();

                if (!runElevated)
                {
                    process = processManager.CreateProcess(command, commandArguments, workingDirectory);
                }
                else if (this.Platform == PlatformID.Unix && !string.IsNullOrEmpty(this.JDKPackagePath))
                {
                    // sudo strips environment variables by default on Linux (env_reset policy).
                    // Using 'env JAVA_HOME=...' as the elevated command ensures the variable is
                    // set in the process where Kafka scripts execute.
                    process = processManager.CreateElevatedProcess(this.Platform, "env", $"JAVA_HOME={this.JDKPackagePath} {command} {commandArguments}", workingDirectory, username);
                }
                else
                {
                    process = processManager.CreateElevatedProcess(this.Platform, command, commandArguments, workingDirectory, username);
                }

                this.Logger.LogTraceMessage($"Executing: {command} {SensitiveData.ObscureSecrets(commandArguments)}".Trim(), relatedContext);

                beforeExecution?.Invoke(process);
                await process.StartAndWaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }

            return process;
        }
    }
}