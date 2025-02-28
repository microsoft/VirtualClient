namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;    

    /// <summary>
    /// Installation component for the PSCP
    /// </summary>
    public class PSCPInstallation : VirtualClientComponent
    {
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ProcessManager processManager;
        private ISystemManagement systemManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSCPInstallation"/> class.
        /// </summary>
        /// <param name="dependencies">An enumeration of dependencies that can be used for dependency injection.</param>
        /// <param name="parameters">A series of key value pairs that dictate runtime execution.</param>
        public PSCPInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.fileSystem = dependencies.GetService<IFileSystem>();
            this.packageManager = dependencies.GetService<IPackageManager>();
            this.processManager = dependencies.GetService<ProcessManager>();
            this.systemManagement = dependencies.GetService<ISystemManagement>();
            this.SupportingExecutables = new List<string>();
        }

        /// <summary>
        /// The path to the pscp executable.
        /// </summary>
        public string ExecutablePath { get; set; }

        /// <summary>
        /// The path where the pscp JSON results file should be output.
        /// </summary>
        public string ResultsFilePath { get; set; }

        /// <summary>
        /// path to store pscp.exe
        /// </summary>
        public string PscpPath
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(PSCPInstallation.PscpPath));
            }
        }

        /// <summary>
        /// A set of paths for supporting executables of the main process 
        /// (e.g. pscp_x86_64, pscp_aarch64). These typically need to 
        /// be cleaned up/terminated at the end of each round of processing.
        /// </summary>
        protected IList<string> SupportingExecutables { get; }
        
        /// <summary>
        /// Executes Geek bench
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.DeleteResultsFile(telemetryContext);  

            string commandLineArguments = string.Empty;

            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                string pscpPath = Path.Combine(this.PscpPath, "pscp.exe");
                if (!Directory.Exists(this.PscpPath))
                {
                    // If it doesn't exist, create dir
                    Directory.CreateDirectory(this.PscpPath);
                }

                await Task.Run(() =>
                {
                    this.systemManagement.FileSystem.File.Copy(this.ExecutablePath, pscpPath);
                });
            }
        }

        /// <summary>
        /// Initializes the environment and dependencies for running the pscp workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            DependencyPath workloadPackage = await this.packageManager.GetPackageAsync(this.PackageName, CancellationToken.None);
            
            if (workloadPackage == null)
            {
                throw new DependencyException(
                    $"The expected package '{this.PackageName}' does not exist on the system or is not registered.",
                    ErrorReason.WorkloadDependencyMissing);
            }

            workloadPackage = this.PlatformSpecifics.ToPlatformSpecificPath(workloadPackage, this.Platform, this.CpuArchitecture);

            switch (this.PlatformArchitectureName)
            {
                case "win-x64":
                    this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "pscp.exe");
                    ConsoleLogger.Default.LogMessage($"win-x64 path {this.ExecutablePath}", telemetryContext);
                    this.SupportingExecutables.Add("pscp.exe");
                    break;

                case "win-arm64":
                    this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "pscp.exe");
                    ConsoleLogger.Default.LogMessage($"win-arm64 path {this.ExecutablePath}", telemetryContext);
                    this.SupportingExecutables.Add("pscp.exe");
                    break;

                case "win-x32":
                    this.ExecutablePath = this.PlatformSpecifics.Combine(workloadPackage.Path, "pscp.exe");
                    ConsoleLogger.Default.LogMessage($"win-x32 path {this.ExecutablePath}", telemetryContext);
                    this.SupportingExecutables.Add("pscp.exe");
                    
                    break;
            }

            this.ResultsFilePath = this.PlatformSpecifics.Combine(workloadPackage.Path, $"{this.PackageName}-output.txt");
            
            if (!this.fileSystem.File.Exists(this.ExecutablePath))
            {
                throw new DependencyException(
                    $"PSCP executable not found at path '{this.ExecutablePath}'",
                    ErrorReason.WorkloadDependencyMissing);
            }
        }
                
        private void DeleteResultsFile(EventContext telemetryContext)
        {
            try
            {
                if (this.fileSystem.File.Exists(this.ResultsFilePath))
                {
                    this.fileSystem.File.Delete(this.ResultsFilePath);
                }
            }
            catch (IOException exc)
            {
                this.Logger.LogErrorMessage(exc, telemetryContext);
            }
        }         
    }
}
