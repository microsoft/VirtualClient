// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// An example Virtual Client component responsible for executing a workload or a test on
    /// the system.
    /// </summary>
    public class ExampleWorkloadClientExecutor : VirtualClientMultiRoleComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ISystemManagement systemManagement;
        private ProcessManager processManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleWorkloadExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ExampleWorkloadClientExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.fileSystem = this.systemManagement.FileSystem;
            this.packageManager = this.systemManagement.PackageManager;
            this.processManager = this.systemManagement.ProcessManager;

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
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // Overview:
            // ----------------------------------------------------------------------------------------------------------------------
            // The ExecuteAsync method is where the core workload/test logic is executed. By the time we are here, we should
            // be confident that all required dependencies have been verified in the InitializeAsync() method below and
            // that we are ready to rock!!

            try
            {
                // We use the ILogger to emit telemetry. There are a number of extension/helper methods available
                // that help with keeping telemetry logic simple, clean and consistent.
                this.Logger.LogTraceMessage($"{nameof(ExampleWorkloadExecutor)}.Starting", telemetryContext);

                // Dates should ALWAYS be represented in UTC.
                DateTime startTime = DateTime.UtcNow;

            }
            catch (OperationCanceledException)
            {
                // Expected when a Task.Delay is cancelled.
            }

            return Task.CompletedTask;
        }
    }
}