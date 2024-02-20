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
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// An example Virtual Client component responsible for executing a client/server workload or a test on
    /// the system.
    /// </summary>
    public class ExampleClientServerExecutor : VirtualClientComponent
    {
        /// <summary>
        /// The name of the state object used to indicate the expected server application (e.g. web server)
        /// is online and ready for the client workload.
        /// </summary>
        protected static readonly string ServerReadyState = $"{nameof(ExampleClientServerExecutor)}.ServerReady";

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleClientServerExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ExampleClientServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.SystemManagement = dependencies.GetService<ISystemManagement>();
            this.ApiClientManager = dependencies.GetService<IApiClientManager>();
            this.FileSystem = this.SystemManagement.FileSystem;
            this.PackageManager = this.SystemManagement.PackageManager;
            this.ProcessManager = this.SystemManagement.ProcessManager;
            this.StateManager = this.SystemManagement.StateManager;

            // Define the roles that are supported for this client/server workload.
            this.SupportedRoles = new List<string>
            {
                ClientRole.Client,
                ClientRole.Server,
                ClientRole.ReverseProxy
            };

            // Dependencies:
            // ==============================================================================================================
            // The core 'dependencies' for the Virtual Client are passed into each and every component whether it is an
            // action/executor, monitor or dependency installer/handler. These core dependencies can be used to interoperate 
            // with the Virtual Client core runtime platform as well as the system on which it is running. The core dependencies
            // include the following. The interfaces and implementations can be found in the VirtualClient.Core project:
            // - IApiClientManager
            //   Virtual Client API client creation and management.
            //
            // - IBlobManager
            //   Used to interface with blob stores (e.g. Azure Blob store).
            //
            // - IDiskManager
            //   Used to access information about disks on the system, to create mount points and to initialize/format disks.
            //
            // - IFileSystem (.NET out-of-box abstraction)
            //   Used to access folders and files on the file system.
            //
            // - IFirewallManager
            //   Used to add rules and ports to the local firewall on the system.
            //
            // - ILogger (.NET out-of-box abstraction)
            //   Used to log ALL telemetry emitted by the Virtual Client.
            //
            // - IPackageManager
            //   Used to access dependency packages on the system and to download/extract packages from remote stores (e.g. storage account blobs).
            //
            // - ProcessManager
            //   Used to create and manage operating system processes used to run executables for workloads, tests etc...
            //
            // - IStateManager
            //   Used to create and manage state objects on the local system.
            //
            // - ISystemInfo
            //   Used to supply information about the system on which Virtual Client is running (e.g. OS platform, CPU architecture etc...).
            //
            // - ISystemManagement
            //   Core interface provides all core dependencies for the Virtual Client platform. This interface contains all of the dependencies
            //   noted above and is used primarily as an extensibility point for each of them.

            // Parameters:
            // ==============================================================================================================
            // The 'parameters' passed into the component are those that are defined in the workload or monitoring profile for
            // that particular component. Parameters allow a given action/executor, monitor or dependency installer/handler
            // component to be flexible for supporting different scenarios. Certain parameters (defined at the top of the
            // profile) can be overridden on the command line by the user/automation.

            // General Flow:
            // ==============================================================================================================
            // All components in the Virtual Client codebase follow a consistent logical workflow. This is because all
            // Virtual Client components derive from the base class 'VirtualClientComponent'. The component methods are executed
            // in the following order:
            //
            // 1) IsSupported
            //    Whether or not the component should be executed on the system.
            //    - Does this component support the current platform/architecture?
            //    - Does this component support the current distro of the operating system.
            //    - Should this component be executed? 
            //
            // 2) Validate
            //    Was the component given the information it needs? Allows the developer to perform validations/checks on the 
            //    parameters provided to ensure they are correct and that expected parameters exist.
            // 
            // 3) InitializeAsync
            //    Does the component have the dependencies it needs to succeed? Allows the developer to perform preliminary/initialization work:
            //    - confirming required packages/dependencies
            //    - setting class member variables and properties that will be used later.
            //
            // 4) ExecuteAsync
            //    Allows the developer to do the main body of work associated with the component. This might be running a workload or test 
            //    and capturing metrics. This might be running a stress test on the system that does not itself produce any specific metrics.
            //
            // 5) CleanupAsync
            //    Allows the developer to perform any cleanup steps/operations required. Its a good idea to always cleanup any artifacts
            //    that were created during the execution of the component in case the same profile is executed again on the system
            //    (i.e. components should be idempotent).
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
        /// Provides access to the local state management facilities.
        /// </summary>
        protected IStateManager StateManager { get; }

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
        /// Determines if the executor should be included in the set of components executed.
        /// </summary>
        protected override bool IsSupported()
        {
            // Overview:
            // ----------------------------------------------------------------------------------------------------------------------
            // When the ProfileExecutor is determining which components defined in a profile to execute, it will call this method.
            // This is typically used to ensure the component does not execute on platforms or CPU architectures for which it is not
            // supported. For example, the component may only have support for Linux OS platform and x64 architecture.

            bool shouldExecute = base.IsSupported();

            if (shouldExecute)
            {
                // Example: Maybe this component only works on Windows and Unix/Linux platforms.
                shouldExecute = this.Platform == PlatformID.Win32NT || this.Platform == PlatformID.Unix;
            }

            return shouldExecute;
        }

        /// <summary>
        /// Allows the parameters passed into the component to be validated. This allows the
        /// developer to ensure the definitions in the workload profile are valid and have defined
        /// required information.
        /// </summary>
        protected override void Validate()
        {
            base.Validate();
            this.ThrowIfLayoutNotDefined();

            IEnumerable<string> roles = this.Layout.Clients.Select(client => client.Role).Distinct();
            IEnumerable<string> supportedRoles = this.SupportedRoles.Intersect(roles);
            if (supportedRoles?.Any() != true && roles.Count() != supportedRoles.Count())
            {
                throw new WorkloadException(
                    $"Role not supported. The following roles defined in the environment layout supplied '{string.Join(",", roles.Except(supportedRoles))}' are not supported.",
                    ErrorReason.InvalidProfileDefinition);
            }
        }
    }
}