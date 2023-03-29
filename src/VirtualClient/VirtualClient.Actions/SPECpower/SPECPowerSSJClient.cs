// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common.Extensions;
    using global::VirtualClient.Common.Platform;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Manages the SPEC Power "SSJ Client" (Server-Side Java) program.
    /// Note that "Server-Side Java" is misleading, this application is a TCP client and does not host any server.
    /// Instead, it means "this program runs on the server machine", not that the program itself is a server.
    /// </summary>
    public class SPECPowerSSJClient : SPECPowerProcess
    {
        private const string SSJFolder = "ssj";
        private const string WindowsClassPath = "ssj.jar;check.jar;lib/jcommon-1.0.16.jar;lib/jfreechart-1.0.13.jar";
        private const string UnixClassPath = "ssj.jar:check.jar:lib/jcommon-1.0.16.jar:lib/jfreechart-1.0.13.jar";
        private const string ConfigName = "SPECpower_ssj.props";
        private const string LoadPercentageSequenceConfigPlaceholder = "{VIRTUALCLIENT_LOAD_PERCENTAGE_SEQUENCE}";
        private const string LoadLevelLengthSecondsConfigPlaceholder = "{VIRTUALCLIENT_LOAD_LEVEL_LENGTH_SECONDS}";
        private const string LogicalProcessorsConfigPlaceholder = "{VIRTUALCLIENT_LOGICAL_PROCESSORS}";
        private const int CoresPerJvm = 4;

        private ISystemManagement systemManagement;

        /// <summary>
        /// Initializes a new instance of the <see cref="SPECPowerSSJClient"/> class.
        /// </summary>
        public SPECPowerSSJClient(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.systemManagement = dependencies.GetService<ISystemManagement>();
        }

        /// <inheritdoc/>
        public override void Cleanup()
        {
            this.SPECProcesses?.ForEach(process => process.Kill());
            this.SPECProcesses.Clear();
        }

        /// <summary>
        /// Start a calculated number of SSJ Client processes.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.SetupConfiguration();

            MemoryInfo memoryInfo = await this.systemManagement.GetMemoryInfoAsync(CancellationToken.None);

            long totalMemoryKiloBytes = memoryInfo.TotalMemory;
            int coreCount = Environment.ProcessorCount;

            telemetryContext.AddContext(nameof(totalMemoryKiloBytes), totalMemoryKiloBytes);
            telemetryContext.AddContext(nameof(coreCount), coreCount);

            int instanceCount = this.ActionOwner.OverrideInstanceCount;
            if (instanceCount < 0)
            {
                // This could be wrong, but this is what it seems like the documentation says to do.
                instanceCount = Math.Max(1, (coreCount + 1) / CoresPerJvm);
            }

            int megaBytesPerInstance = (int)(((totalMemoryKiloBytes * .9D) / instanceCount) / (1024));
            this.Logger.LogTraceMessage($"Total Memory {totalMemoryKiloBytes}KB. Running {instanceCount} SSJ instance(s) with {megaBytesPerInstance} megabytes of memory each.");

            using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
            {
                for (int i = 0; i < instanceCount; i++)
                {
                    this.StartClientSSJInstanceAsync(megaBytesPerInstance, instanceCount, telemetryContext, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Updates the configuration files with arguments from the profile.
        /// </summary>
        private void SetupConfiguration()
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>()
            {
                { LoadPercentageSequenceConfigPlaceholder, this.ActionOwner.LoadPercentageSequence },
                { LoadLevelLengthSecondsConfigPlaceholder, this.ActionOwner.LoadLevelLengthSeconds },
                { LogicalProcessorsConfigPlaceholder, this.ActionOwner.ProcessorCount.ToString() }
            };

            string configurationFilePath = this.PlatformSpecifics.Combine(this.ActionOwner.WorkloadPackagePath, SSJFolder, ConfigName);

            ConfigurationHelpers.ReplacePlaceholders(configurationFilePath, replacements);
        }

        /// <summary>
        /// Starts an SSJ Client process.
        /// </summary>
        private void StartClientSSJInstanceAsync(int megabytesOfRam, int totalInstanceCount, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            int priorCreatedProcesses = this.SPECProcesses.Count;
            int jvmId = priorCreatedProcesses + 1;

            string ssjOptions = this.ActionOwner.Parameters.GetValue<string>("SSJArguments")
                .Replace("{RAM_MB}", megabytesOfRam.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{JVM_ID}", jvmId.ToString(), StringComparison.OrdinalIgnoreCase)
                .Replace("{INSTANCE_COUNT}", totalInstanceCount.ToString(), StringComparison.OrdinalIgnoreCase);
            string processName = $"SPEC Power SSJ-Client #{jvmId}";

            string workloadDirectoryPath = this.PlatformSpecifics.Combine(this.ActionOwner.WorkloadPackagePath, SSJFolder);
            string classPath = this.Platform == PlatformID.Win32NT ? WindowsClassPath : UnixClassPath;

            this.StartSPECProcessAsync(processName, workloadDirectoryPath, classPath, ssjOptions, cancellationToken);
            this.Logger.LogTraceMessage("Started an instance of SPEC Power Client (SSJ).");

            const int MaxProcessors = 64;
            const int MaxProcessesSet = (MaxProcessors / CoresPerJvm);

            // This feature only works if we actually have this many processors. It also only works with up to 64 processors.
            // It is also totally safe to continue if an exception is thrown. Therefore, we will only log the exception, and not stop execution.
            if (this.ActionOwner.ShouldSetProcessAffinity && this.Platform == PlatformID.Win32NT && priorCreatedProcesses < MaxProcessesSet)
            {
                // Apologies for the unreadable code, unfortunately this is just how the value we're setting works.
                // This creates a bit mask for the number of processors it's going to use. If we're using 4 processors per JVM, then the mask we'll get is 0b1111.
                // Then, it shifts the mask the number so it will take claim over the proper processors.
                // For example, to use processors 0-4, it will do 0b1111 << 0, and for processors 5-8 it will do 0b1111 << 4.
                // An explanation of this can be found here: https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.processthread.processoraffinity?view=net-5.0
                // Because this is stored in a long (with 1 bit per processor), the maximum number of processors which can be specified is 64.
                long newProcessAffinity = ((1L << CoresPerJvm) - 1) << (priorCreatedProcesses * CoresPerJvm);

                try
                {
                    // To Do: Expose Affinity in VC Common package..
                    // process.Process.SetProcessAffinity(newProcessAffinity);
                    // this.LogMessage($"Successfully set {process.ProcessName}'s process affinity to 0x{newProcessAffinity:X8}.");
                }
                catch (Exception exc)
                {
                    // It is acceptable to let the process continue, even though there was an error here.
                    this.Logger.LogMessage(
                        $"{this.TypeName}.SetProcessAffinityError",
                        LogLevel.Warning,
                        telemetryContext.Clone().AddError(exc));
                }
            }
        }
    }
}