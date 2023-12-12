// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// The Hadoop Terasort workload executor
    /// </summary>
    [UnixCompatible]
    public class HadoopTerasortExecutor : VirtualClientComponent
    {
        private bool disposed;
        private IFileSystem fileSystem;
        private IPackageManager packageManager;
        private ISystemManagement systemManagement;
        private IStateManager stateManager;
        private HadoopExecutorState state;

        /// <summary>
        /// Constructor for <see cref="HadoopTerasortExecutor"/>
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the component.</param>
        /// <param name="parameters">Parameters defined in the profile or supplied on the command line.</param>
        public HadoopTerasortExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
             : base(dependencies, parameters)
        {
            this.RetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(5, (retries) => TimeSpan.FromSeconds(retries + 1));
            this.systemManagement = this.Dependencies.GetService<ISystemManagement>();
            this.stateManager = this.systemManagement.StateManager;
            this.packageManager = this.systemManagement.PackageManager;
            this.fileSystem = this.systemManagement.FileSystem;
        }

        /// <summary>
        /// Java Development Kit package name.
        /// </summary>
        public string JdkPackageName
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(HadoopTerasortExecutor.JdkPackageName));
            }
        }
        
        /// <summary>
         /// A policy that defines how the component will retry when
         /// it experiences transient issues.
         /// </summary>
        public IAsyncPolicy RetryPolicy { get; set; }

        /// <summary>
        /// The Row Number count defined in the profile.
        /// </summary>
        public int RowCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.RowCount), 100000);
            }

            set
            {
                this.Parameters[nameof(this.RowCount)] = value;
            }
        }

        /// <summary>
        /// The path to the Hadoop executable file.
        /// </summary>
        private string ExecutablePath { get; set; }

        /// <summary>
        /// Path to the java executable file. 
        /// </summary>
        private string JavaExecutablePath { get; set; }

        /// <summary>
        /// Path to the Java Package Directory.
        /// </summary>
        private string JavaPackageDirectory { get; set; }

        /// <summary>
        /// The path to the Hadoop package.
        /// </summary>
        private string PackageDirectory { get; set; }

        /// <summary>
        /// Disposes of resources used by the class instance.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && !this.disposed)
            {
                EventContext telemetryContext = EventContext.Persisted()
                        .AddContext("DisposeServices", "HadoopExecutor");

                Task.Run(async () =>
                {
                    await this.Logger.LogMessageAsync($"{nameof(HadoopTerasortExecutor)}.DisposeServices", telemetryContext, async () =>
                    {
                        this.state.HadoopExecutorServicesStarted = false;

                        await Task.WhenAll(
                            this.ExecuteCommandAsync("bash", $"-c sbin/stop-dfs.sh", this.PackageDirectory, telemetryContext, CancellationToken.None),
                            this.ExecuteCommandAsync("bash", $"-c sbin/stop-yarn.sh", this.PackageDirectory, telemetryContext, CancellationToken.None),
                            this.stateManager.SaveStateAsync<HadoopExecutorState>($"{nameof(HadoopExecutorState)}", this.state, CancellationToken.None));
                    });
                }).GetAwaiter().GetResult(); // Ensure Task.Run completes before continuing

                this.disposed = true;
            }
        }

        /// <summary>
        /// Executes the Hadoop Terasort workload.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string timestamp = DateTime.Now.ToString("ddMMyyHHmmss");
            string teragenCommmand = $"bin/hadoop jar share/hadoop/mapreduce/hadoop-mapreduce-examples-3.3.5.jar teragen {this.RowCount} /inp-{timestamp}";
            string terasortCommand = $"bin/hadoop jar share/hadoop/mapreduce/hadoop-mapreduce-examples-3.3.5.jar terasort /inp-{timestamp} /out-{timestamp}";

            await this.ExecutedDataCommands(teragenCommmand, "Hadoop Teragen", telemetryContext, cancellationToken);
            await this.ExecutedDataCommands(terasortCommand, "Hadoop Terasort", telemetryContext, cancellationToken);
        }

        /// <summary>
        /// Initializes the environment for execution of the Hadoop Terasort workload.
        /// </summary>
        protected override async Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            this.state = await this.stateManager.GetStateAsync<HadoopExecutorState>($"{nameof(HadoopExecutorState)}", cancellationToken)
                ?? new HadoopExecutorState();

            DependencyPath workloadPackage = await this.packageManager.GetPlatformSpecificPackageAsync(
                this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken);

            DependencyPath javaPackage = await this.GetPackageAsync(
                this.JdkPackageName, cancellationToken);

            this.PackageDirectory = workloadPackage.Path;
            this.JavaPackageDirectory = javaPackage.Path;

            this.JavaExecutablePath = this.PlatformSpecifics.Combine(this.JavaPackageDirectory, "bin", "java");

            switch (this.Platform)
            {
                case PlatformID.Unix:
                    this.ExecutablePath = this.PlatformSpecifics.Combine(this.PackageDirectory, "bin", "hadoop");
                    break;

                default:
                    throw new WorkloadException(
                        $"The Hadoop workload is not supported on the current platform/architecture " +
                        $"{PlatformSpecifics.GetPlatformArchitectureName(this.Platform, this.CpuArchitecture)}.",
                        ErrorReason.PlatformNotSupported);
            }

            if (!this.state.HadoopExecutorStateInitialized)
            {
                this.SetEnvironmentVariable(EnvironmentVariable.JAVA_HOME, this.JavaPackageDirectory, EnvironmentVariableTarget.Process);

                this.ConfigurationFilesAsync(telemetryContext, cancellationToken);
                await this.ExecutableFilesAsync(telemetryContext, cancellationToken);

                await this.ExecuteCommandAsync("bash", $"-c \"echo y | ssh-keygen -t rsa -P '' -f ~/.ssh/id_rsa\"", this.PackageDirectory, telemetryContext, cancellationToken);
                await this.ExecuteCommandAsync("bash", $"-c \"cat ~/.ssh/id_rsa.pub >> ~/.ssh/authorized_keys\"", this.PackageDirectory, telemetryContext, cancellationToken);
                await this.ExecuteCommandAsync("bash", $"-c \"chmod 0600 ~/.ssh/authorized_keys\"", this.PackageDirectory, telemetryContext, cancellationToken);
                await this.ExecuteCommandAsync("bash", $"-c \"echo y | bin/hdfs namenode -format\"", this.PackageDirectory, telemetryContext, cancellationToken);
                await this.ExecuteCommandAsync("bash", $"-c \"bin/hdfs dfs -mkdir /user\"", this.PackageDirectory, telemetryContext, cancellationToken);
                await this.ExecuteCommandAsync("bash", $"-c \"bin/hdfs dfs -mkdir /user/azureuser\"", this.PackageDirectory, telemetryContext, cancellationToken);
                this.state.HadoopExecutorStateInitialized = true;
            }

            if (!this.state.HadoopExecutorServicesStarted)
            {
                await this.ExecuteCommandAsync("bash", $"-c sbin/start-dfs.sh", this.PackageDirectory, telemetryContext, cancellationToken);
                await this.ExecuteCommandAsync("bash", $"-c sbin/start-yarn.sh", this.PackageDirectory, telemetryContext, cancellationToken);
                this.state.HadoopExecutorServicesStarted = true;
                await Task.Delay(30000); // Delay during the safe mode of the name node to verify the status of replicated nodes.
            }

            await this.stateManager.SaveStateAsync<HadoopExecutorState>($"{nameof(HadoopExecutorState)}", this.state, cancellationToken);
        }

        /// <summary>
        /// Returns true/false whether the component is supported on the current
        /// OS platform and CPU architecture.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = base.IsSupported()
                && (this.Platform == PlatformID.Unix)
                && (this.CpuArchitecture == Architecture.X64 || this.CpuArchitecture == Architecture.Arm64);

            if (!isSupported)
            {
                this.Logger.LogNotSupported("Hadoop", this.Platform, this.CpuArchitecture, EventContext.Persisted());
            }

            return isSupported;
        }

        /// <summary>
        /// Logs the Hadoop Terasort workload metrics.
        /// </summary>
        private void CaptureMetrics(IProcessProxy process, string toolName, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                this.MetadataContract.AddForScenario(
                    toolName,
                    process.FullCommand(),
                    toolVersion: "3.3.5");

                this.MetadataContract.Apply(telemetryContext);

                HadoopMetricsParser parser = new HadoopMetricsParser(process.StandardError.ToString());
                IList<Metric> workloadMetrics = parser.Parse();

                this.Logger.LogMetrics(
                    toolName,
                    this.Scenario,
                    process.StartTime,
                    process.ExitTime,
                    workloadMetrics,
                    null,
                    process.FullCommand(),
                    this.Tags,
                    telemetryContext);
            }
        }

        private void ConfigurationFilesAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            ConsoleLogger.Default.LogInformation($"ConfigurationFilesAsync");

            IDictionary<string, string> coreSite = new Dictionary<string, string>
            {
                { "fs.defaultFS", "hdfs://localhost:9000" }
            };
            this.CreateHTMLValue("core-site.xml", coreSite, cancellationToken);

            IDictionary<string, string> hdfsSite = new Dictionary<string, string>
            {
                { "dfs.replication", "1" }
            };
            this.CreateHTMLValue("hdfs-site.xml", hdfsSite, cancellationToken);

            IDictionary<string, string> mapredSite = new Dictionary<string, string>
            {
                { "mapreduce.framework.name", "yarn" },
                { "mapreduce.application.classpath", "$HADOOP_MAPRED_HOME/share/hadoop/mapreduce/*:$HADOOP_MAPRED_HOME/share/hadoop/mapreduce/lib/*" }
            };
            this.CreateHTMLValue("mapred-site.xml", mapredSite, cancellationToken);

            IDictionary<string, string> yarnSite = new Dictionary<string, string>
            {
                { "yarn.nodemanager.aux-services", "mapreduce_shuffle" },
                { "yarn.nodemanager.env-whitelist", "JAVA_HOME,HADOOP_COMMON_HOME,HADOOP_HDFS_HOME,HADOOP_CONF_DIR,CLASSPATH_PREPEND_DISTCACHE,HADOOP_YARN_HOME,HADOOP_HOME,PATH,LANG,TZ,HADOOP_MAPRED_HOME" }
            };
            this.CreateHTMLValue("yarn-site.xml", yarnSite, cancellationToken);

            string makeFilePath = this.PlatformSpecifics.Combine(this.PackageDirectory, "etc", "hadoop");
            string hadoopEnvFilePath = this.PlatformSpecifics.Combine(makeFilePath, "hadoop-env.sh");

            this.fileSystem.File.ReplaceInFileAsync(
                        hadoopEnvFilePath, @"# export JAVA_HOME=", $"export JAVA_HOME={this.JavaPackageDirectory}", cancellationToken);

            telemetryContext.AddContext(nameof(this.ConfigurationFilesAsync), "Configuration process successful required to run terasort.");
        }

        private void CreateHTMLValue(string fileName, IDictionary<string, string> value, CancellationToken cancellationToken)
        {
            string makeFilePath = this.PlatformSpecifics.Combine(this.PackageDirectory, "etc", "hadoop");
            ConsoleLogger.Default.LogInformation($"makeFilePath: {makeFilePath}");

            string filePath = this.PlatformSpecifics.Combine(makeFilePath, fileName);
            ConsoleLogger.Default.LogInformation($"filePath: {filePath}");

            string replaceStatement = @"<configuration>([\s\S]*?)<\/configuration>";
            string replacedStatement = "<configuration>";

            for (int i = 0; i < value.Count; i++)
            {
                string property = $"<property><name>{value.ElementAt(i).Key}</name><value>{value.ElementAt(i).Value}</value></property>";
                replacedStatement += property;
            }

            replacedStatement += "</configuration>";
            ConsoleLogger.Default.LogInformation($"replacedStatement: {replacedStatement}");

            this.fileSystem.File.ReplaceInFileAsync(
                    filePath, replaceStatement, replacedStatement, cancellationToken);
        }

        private async Task ExecutableFilesAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            await this.systemManagement.MakeFileExecutableAsync(this.ExecutablePath, this.Platform, cancellationToken);
            await this.systemManagement.MakeFileExecutableAsync(this.JavaExecutablePath, this.Platform, cancellationToken);

            string[] paths = 
            {
                "bin/hdfs",
                "sbin/start-dfs.sh",
                "sbin/stop-dfs.sh",
                "bin/yarn",
                "sbin/start-yarn.sh",
                "sbin/stop-yarn.sh"
            };

            foreach (var path in paths)
            {
                string fullPath = this.PlatformSpecifics.Combine(this.PackageDirectory, path); 
                await this.systemManagement.MakeFileExecutableAsync(fullPath, this.Platform, cancellationToken);
            }

            telemetryContext.AddContext(nameof(this.ExecutableFilesAsync), "Execution permission successful to use the files.");
        }

        private async Task ExecutedDataCommands(string command, string operation, EventContext telemetryContext, CancellationToken cancellationToken)
        {
            using (IProcessProxy process = await this.ExecuteCommandAsync("bash", $"-c \"{command}\"", this.PackageDirectory, telemetryContext, cancellationToken))
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    await this.LogProcessDetailsAsync(process, telemetryContext, operation, logToFile: true);

                    process.ThrowIfWorkloadFailed();

                    if (process.StandardError == null)
                    {
                        throw new WorkloadResultsException($"{operation} results data not found in the process details", ErrorReason.WorkloadResultsNotFound);
                    }

                    this.CaptureMetrics(process, operation, telemetryContext, cancellationToken);
                }
            }
        }

        internal class HadoopExecutorState : State
        {
            public HadoopExecutorState(IDictionary<string, IConvertible> properties = null)
                : base(properties)
            {
            }

            public bool HadoopExecutorStateInitialized
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(HadoopExecutorState.HadoopExecutorStateInitialized), false);
                }

                set
                {
                    this.Properties[nameof(HadoopExecutorState.HadoopExecutorStateInitialized)] = value;
                }
            }

            public bool HadoopExecutorServicesStarted
            {
                get
                {
                    return this.Properties.GetValue<bool>(nameof(HadoopExecutorState.HadoopExecutorServicesStarted), false);
                }

                set
                {
                    this.Properties[nameof(HadoopExecutorState.HadoopExecutorServicesStarted)] = value;
                }
            }
        }
    }
}
