// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// A VirtualClientComponent used by the SPEC Power action to manage processes.
    /// These have been split into subcomponents mainly for readability.
    /// There is coupling between them and the parent Action, but it's a worthwhile trade-off.
    /// </summary>
    public abstract class SPECPowerProcess : VirtualClientComponent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SPECPowerProcess"/> class.
        /// </summary>
        public SPECPowerProcess(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.SPECProcesses = new List<IProcessProxy>();
        }

        /// <summary>
        /// The SPECPowerExecutor which owns this
        /// </summary>
        public SPECPowerExecutor ActionOwner { get; internal set; }

        /// <summary>
        /// IProcessProxy used by Spec Power.
        /// </summary>
        public List<IProcessProxy> SPECProcesses { get; set; }

        /// <summary>
        /// Cleans up the resources used by this component.
        /// </summary>
        public abstract void Cleanup();

        /// <summary>
        /// Creates a SPECProgramProcess with the given information.
        /// </summary>
        /// <param name="processName">The name of the process being started.</param>
        /// <param name="workingDirectory">The working directory to execute in.</param>
        /// <param name="classPath">The java classpath to execute under.</param>
        /// <param name="commandLineArguments">The command-line arguments to add to the </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>specProgramProcess</returns>
        protected async Task StartSPECProcessAsync(string processName, string workingDirectory, string classPath, string commandLineArguments, CancellationToken cancellationToken)
        {
            if (!workingDirectory.EndsWith(Path.DirectorySeparatorChar))
            {
                workingDirectory += Path.DirectorySeparatorChar;
            }

            ISystemManagement systemManagement = this.Dependencies.GetService<ISystemManagement>();

            string javaExePath = SPECPowerExecutor.JavaExecutablePath;

            if (!systemManagement.FileSystem.File.Exists(javaExePath))
            {
                throw new DependencyException(
                    $"The Java Development Kit is required but is not installed at the path expected '{javaExePath}'.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            IProcessProxy process = systemManagement.ProcessManager.CreateElevatedProcess(this.Platform, javaExePath, $"-cp {classPath} {commandLineArguments}", workingDirectory);
            this.SPECProcesses.Add(process);

            await process.StartAndWaitAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!cancellationToken.IsCancellationRequested)
            {
                await this.LogProcessDetailsAsync(process, EventContext.Persisted(), "SPECpower", logToFile: true);
                process.ThrowIfErrored<WorkloadException>(errorReason: ErrorReason.WorkloadFailed);
            }
        }
    }
}