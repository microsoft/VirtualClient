// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// An example Virtual Client component responsible for executing a client-side role responsibilities 
    /// in the client/server workload.
    /// </summary>
    /// <remarks>
    /// This is on implementation pattern for client/server workloads. In this model, the client and server
    /// roles inherit from the <see cref="ExampleClientServerExecutor"/> and override behavior as
    /// is required by the particular role. For this particular example, the client role is responsible for
    /// confirming the server-side is online followed by running a workload against it.
    /// </remarks>
    internal class ExampleServerExecutor : ExampleClientServerExecutor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleClientExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        public ExampleServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// 'Port' parameter defined in the profile action.
        /// </summary>
        public int Port
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.Port));
            }
        }

        /// <summary>
        /// The path to the web server host executable/scripts.
        /// </summary>
        protected string WebServerExecutablePath { get; set; }

        /// <summary>
        /// The path to the package that contains the web server host executable/scripts
        /// </summary>
        protected DependencyPath WebServerPackage { get; set; }

        /// <summary>
        /// Executes the workload.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                // Run the web server
                string commandArguments = $"Api --port={this.Port}";
                using (IProcessProxy webHostProcess = this.ProcessManager.CreateElevatedProcess(this.Platform, this.WebServerExecutablePath, commandArguments))
                {
                    if (!webHostProcess.Start())
                    {
                        throw new WorkloadException($"The API server workload did not start as expected.", ErrorReason.WorkloadFailed);
                    }

                    this.CleanupTasks.Add(() => webHostProcess.SafeKill(this.Logger));
                    this.Logger.LogTraceMessage($"API server workload online awaiting client requests...");

                    // Signal to clients that the server-side is online and ready to receive requests. The server-side is offline
                    // by default every time the application starts. This handshake helps to ensure the client does not start sending
                    // requests before the server has confirmed it is ready. That the server-side is offline by default always when the
                    // application starts ensures that either the client or server-side can go down and recover creating some amount of
                    // resiliency in the face of transient errors.
                    this.SetServerOnline(true);

                    await webHostProcess.WaitForExitAsync(cancellationToken)
                        .ConfigureAwait(false);
                }

                // If you do not have a process that you are running for the web server, you can still perform a simple sleep/waite.
                // It is important to keep the Virtual Client itself up and running in client/server workload scenarios because it
                // allows for transient issues on either side of the equation that cause VC itself to crash...handshake mechanics!
                // await this.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected whenever certain operations (e.g. Task.Delay) are cancelled.
            }
            finally
            {
                // Always signal to clients that the server is offline before exiting. This helps to ensure that the client
                // and server have consistency in handshakes even if one side goes down and returns at some other point.
                this.SetServerOnline(false);
            }
        }

        /// <summary>
        /// Performs initialization operations for the executor.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            // Imagine there is a package that contains binaries/exes used to run a web server (e.g. NGINX).
            this.WebServerPackage = await this.GetPlatformSpecificPackageAsync(this.PackageName, cancellationToken);

            if (this.Platform == PlatformID.Win32NT)
            {
                // On Windows the binary has a .exe in the name.
                this.WebServerExecutablePath = this.Combine(this.WebServerPackage.Path, "ExampleWorkload.exe");
            }
            else
            {
                // PlatformID.Unix
                // On Unix/Linux the binary does NOT have a .exe in the name. 
                this.WebServerExecutablePath = this.Combine(this.WebServerPackage.Path, "ExampleWorkload");

                // Binaries on Unix/Linux must be attributed with an attribute that makes them "executable".
                await this.SystemManagement.MakeFileExecutableAsync(this.WebServerExecutablePath, this.Platform, cancellationToken);
            }
        }
    }
}