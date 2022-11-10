// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
    using static System.Collections.Specialized.BitVector32;

    /// <summary>
    /// Manages the execution runtime of the FIO Multi Throughput workload.
    /// It represents an OLTP-C scenario.
    /// Random IO represents Database transactions.
    /// Sequential IO represents Logger transactions.
    /// </summary>
    [UnixCompatible]
    public class FioMultiThroughputExecutor : FioExecutor
    {
        private static readonly object VariationLock = new object();
        private static int variationNumber = 0;
        private string randomIOFilePath;
        private string sequentialIOFilePath;
        private int totalIOPS;
        private int randomReadIOPS;
        private int randomWriteIOPS;
        private int sequentialReadIOPS;
        private int sequentialWriteIOPS;

        private static IAsyncPolicy fioMultiThroughputRetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(3, _ => RetryWaitTime);

        /// <summary>
        /// Initializes a new instance of the <see cref="FioMultiThroughputExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the workload.</param>
        /// <param name="parameters">The set of parameters defined for the action in the profile definition.</param>
        public FioMultiThroughputExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
        }

        /// <summary>
        /// Direct IO parameter for FIO.
        /// </summary>
        public string DirectIO
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.DirectIO));
            }
        }

        /// <summary>
        /// Group reporting parameter for FIO.
        /// </summary>
        public int GroupReporting
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.GroupReporting));
            }
        }

        /// <summary>
        /// Random IO File Size.
        /// </summary>
        public string RandomIOFileSize
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.RandomIOFileSize));
            }
        }

        /// <summary>
        /// Block size for Random Read.
        /// </summary>
        public string RandomReadBlockSize
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.RandomReadBlockSize));
            }
        }

        /// <summary>
        /// Number Jobs for Random Read.
        /// </summary>
        public int RandomReadNumJobs
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.RandomReadNumJobs));
            }
        }

        /// <summary>
        /// Queue Depth for Random Read.
        /// </summary>
        public int RandomReadQueueDepth
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.RandomReadQueueDepth));
            }
        }

        /// <summary>
        /// Block size for Random Write.
        /// </summary>
        public string RandomWriteBlockSize
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.RandomWriteBlockSize));
            }
        }

        /// <summary>
        /// Number of Jobs for Random Write.
        /// </summary>
        public int RandomWriteNumJobs
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.RandomWriteNumJobs));
            }
        }

        /// <summary>
        /// QueueDepth for Random Write.
        /// </summary>
        public int RandomWriteQueueDepth
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.RandomWriteQueueDepth));
            }
        }

        /// <summary>
        /// Run Time for running the FIO in Seconds.
        /// </summary>
        public int DurationSec
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.DurationSec));
            }
        }

        /// <summary>
        /// File size of Sequential IO File.
        /// </summary>
        public string SequentialIOFileSize
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.SequentialIOFileSize));
            }
        }

        /// <summary>
        /// Block Size for Sequential Read.
        /// </summary>
        public string SequentialReadBlockSize
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.SequentialReadBlockSize));
            }
        }

        /// <summary>
        /// Number of Jobs for Sequential Read.
        /// </summary>
        public int SequentialReadNumJobs
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.SequentialReadNumJobs));
            }
        }

        /// <summary>
        /// QueueDepth for Sequential Read.
        /// </summary>
        public int SequentialReadQueueDepth
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.SequentialReadQueueDepth));
            }
        }

        /// <summary>
        /// Block Size for Sequential Write.
        /// </summary>
        public string SequentialWriteBlockSize
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.SequentialWriteBlockSize));
            }
        }

        /// <summary>
        /// Number of Jobs for Sequential Write.
        /// </summary>
        public int SequentialWriteNumJobs
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.SequentialWriteNumJobs));
            }
        }

        /// <summary>
        /// Queue Depth for Sequential Write.
        /// </summary>
        public int SequentialWriteQueueDepth
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.SequentialWriteQueueDepth));
            }
        }

        /// <summary>
        /// Template for Job file.
        /// </summary>
        public string TemplateJobFile
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.TemplateJobFile));
            }
        }

        /// <summary>
        /// Target percents list.
        /// </summary>
        public List<int> TargetPercents
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.TargetPercents)).Split(VirtualClientComponent.CommonDelimiters).Select(int.Parse).ToList();
            }
        }

        /// <summary>
        /// Weight for Random Read.
        /// </summary>
        public int RandomReadWeight
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.RandomReadWeight));
            }
        }

        /// <summary>
        /// Weight for Random Write.
        /// </summary>
        public int RandomWriteWeight
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.RandomWriteWeight));
            }
        }

        /// <summary>
        /// Weight for Sequential Read.
        /// </summary>
        public int SequentialReadWeight
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.SequentialReadWeight));
            }
        }

        /// <summary>
        /// Weight for Sequential Write.
        /// </summary>
        public int SequentialWriteWeight
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.SequentialWriteWeight));
            }
        }

        /// <summary>
        /// Target IOPS.
        /// </summary>
        public int TargetIOPS
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.TargetIOPS));
            }
        }

        /// <summary>
        /// Retry Wait Time for FIO executors.
        /// </summary>
        protected static TimeSpan RetryWaitTime { get; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// Executes the FIO workload, captures performance results and logs them to telemetry.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            string ioEngine = FioExecutor.GetIOEngine(Environment.OSVersion.Platform);

            IEnumerable<Disk> disks = await this.SystemManagement.DiskManager.GetDisksAsync(cancellationToken)
                        .ConfigureAwait(false);

            if (disks?.Any() != true)
            {
                throw new WorkloadException(
                    "Unexpected scenario. The disks defined for the system could not be properly enumerated.",
                    ErrorReason.WorkloadUnexpectedAnomaly);
            }

            IEnumerable<Disk> disksToTest = this.GetDisksToTest(disks);

            if (disksToTest?.Any() != true)
            {
                throw new WorkloadException(
                    "Expected disks to test not found. Given the parameters defined for the profile action/step or those passed " +
                    "in on the command line, the requisite disks do not exist on the system or could not be identified based on the properties " +
                    "of the existing disks.",
                    ErrorReason.DependencyNotFound);
            }

            disksToTest.ToList().ForEach(disk => this.Logger.LogTraceMessage($"Disk Target: '{disk}'"));

            this.UpdateTestFilePaths(disksToTest);

            telemetryContext.AddContext("executable", this.ExecutablePath);
            telemetryContext.AddContext(nameof(ioEngine), ioEngine);
            telemetryContext.AddContext(nameof(disks), disks);
            telemetryContext.AddContext(nameof(disksToTest), disksToTest);
            telemetryContext.AddContext(nameof(this.sequentialIOFilePath), this.sequentialIOFilePath);
            telemetryContext.AddContext(nameof(this.randomIOFilePath), this.randomIOFilePath);

            if (this.DiskFill)
            {
                Interlocked.Exchange(ref variationNumber, 0);

                if (await this.IsDiskFillCompleteAsync(cancellationToken).ConfigureAwait(false) == false)
                {
                    string testName = this.Scenario;
                    Dictionary<string, IConvertible> metricsMetadata = this.GetMetricsMetadata();
                    
                    this.Logger.LogMessage($"{this.Scenario}.ExecutionStarted", telemetryContext);

                    await this.ExecuteWorkloadAsync(telemetryContext, cancellationToken, metricsMetadata)
                                        .ConfigureAwait(false);

                    this.Logger.LogMessage($"{this.Scenario}.ExecutionStarted", telemetryContext);

                    await this.RegisterDiskFillCompleteAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
            }
            else
            {
                List<VirtualClientException> exceptions = new List<VirtualClientException>();

                foreach (int targetPercent in this.TargetPercents)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        int variation = Interlocked.Increment(ref variationNumber);

                        string testName = $"{this.Scenario}_targetPercent{targetPercent}";
                        try
                        {
                            this.SetRuntimeParameters(targetPercent);

                            EventContext variationContext = telemetryContext.Clone();
                            variationContext.AddContext(nameof(testName), testName);

                            Dictionary<string, IConvertible> metricsMetadata = this.GetMetricsMetadata();
                            
                            metricsMetadata[nameof(targetPercent).CamelCased()] = targetPercent;
                            metricsMetadata[nameof(variation).CamelCased()] = variation;
                            metricsMetadata[nameof(testName).CamelCased()] = testName;

                            this.Logger.LogMessage($"{testName}.ExecutionStarted", variationContext);

                            await fioMultiThroughputRetryPolicy.ExecuteAsync(async () =>
                            {
                                await this.ExecuteWorkloadAsync(variationContext, cancellationToken, metricsMetadata)
                                    .ConfigureAwait(false);
                            }).ConfigureAwait(false);

                            this.Logger.LogMessage($"{testName}.ExecutionCompleted", variationContext);
                        }
                        catch (VirtualClientException exc)
                        {
                            // Adding exception instead of throwing it.
                            // Furhter runs might not fail. We should try running it.
                            this.Logger.LogError($"{testName}.ExecutionFailed", exc);
                            exceptions.Add(exc);
                        }
                    }
                }

                if (exceptions.Any())
                {
                    AggregateException aggregateException = new AggregateException(exceptions);

                    WorkloadException workloadException = new WorkloadException(
                        $"{nameof(FioMultiThroughputExecutor)} failed with following exceptions",
                        aggregateException,
                        exceptions.Max(ex => ex.Reason));

                    throw workloadException;
                }
            }
        }

        /// <inheritdoc/>
        protected override void ValidateParameters()
        {
            // Not required as parameters themselves can throw error if they are null.
        }

        /// <summary>
        /// Gets the logging setting. Checks workload profile, then command line arguments, then defaults to READ
        /// </summary>
        /// <returns>Logging setting that controls which results are reported</returns>
        protected override void GetMetricsParsingDirectives(out bool parseReadMetrics, out bool parseWriteMetrics, string commandLine)
        {
            parseReadMetrics = false;
            parseWriteMetrics = false; 

            if (commandLine.Contains("--section init", StringComparison.OrdinalIgnoreCase) || commandLine.Contains("--section randomwrite", StringComparison.OrdinalIgnoreCase) || commandLine.Contains("--section sequentialwrite", StringComparison.OrdinalIgnoreCase))
            {
                parseWriteMetrics = true;
            }

            if (commandLine.Contains("--section randomread", StringComparison.OrdinalIgnoreCase) || commandLine.Contains("--section sequentialread", StringComparison.OrdinalIgnoreCase) || parseWriteMetrics == false)
            {
                parseReadMetrics = true;
            }
        }

        /// <summary>
        /// Executes the FIO workload, captures performance results and logs them to telemetry.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        /// <param name="metricsMetadata">Metric's Metadata.</param>
        private async Task ExecuteWorkloadAsync(EventContext telemetryContext, CancellationToken cancellationToken, Dictionary<string, IConvertible> metricsMetadata)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                string ioEngine = FioExecutor.GetIOEngine(Environment.OSVersion.Platform);

                telemetryContext.AddContext("executable", this.ExecutablePath);
                telemetryContext.AddContext(nameof(ioEngine), ioEngine);
                telemetryContext.AddContext(nameof(this.TemplateJobFile), this.TemplateJobFile);

                IPackageManager packageManager = this.Dependencies.GetService<IPackageManager>();
                DependencyPath workloadPackage = await packageManager.GetPlatformSpecificPackageAsync(this.PackageName, this.Platform, this.CpuArchitecture, cancellationToken)
                    .ConfigureAwait(false);

                string jobFileFolder = this.PlatformSpecifics.GetScriptPath("fio");

                string templateJobFilePath = this.PlatformSpecifics.Combine(jobFileFolder, this.TemplateJobFile);
                string jobFilePath = this.PlatformSpecifics.Combine(workloadPackage.Path, nameof(FioMultiThroughputExecutor) + this.TemplateJobFile);

                this.CreateOrUpdateJobFile(templateJobFilePath, jobFilePath);

                DiskPerformanceWorkloadProcess process = this.CreateWorkloadProcess(this.ExecutablePath, jobFilePath);

                metricsMetadata[nameof(this.CommandLine).CamelCased()] = process.CommandArguments;
                await this.ExecuteWorkloadAsync(process, this.Scenario, telemetryContext, cancellationToken, metricsMetadata)
                    .ConfigureAwait(false);

            }
        }

        /// <summary>
        /// Creates a process to run FIO targeting the file names specified.
        /// </summary>
        private DiskPerformanceWorkloadProcess CreateWorkloadProcess(string executable, string jobFile)
        {
            string fioArguments = $"{jobFile.Trim()} {this.GetSections().Trim()} --output-format=json --fallocate=none".Trim();

            IProcessProxy process = this.SystemManagement.ProcessManager.CreateElevatedProcess(this.Platform, executable, fioArguments);

            string testedInstances = $"{this.randomIOFilePath},{this.sequentialIOFilePath}";
            List<string> testFiles = new List<string>() { this.randomIOFilePath, this.sequentialIOFilePath };

            return new DiskPerformanceWorkloadProcess(process, testedInstances, testFiles.ToArray());
        }

        private void CreateOrUpdateJobFile(string sourcePath, string destinationPath)
        {
            string text = this.SystemManagement.FileSystem.File.ReadAllText(sourcePath);

            if (this.DiskFill)
            { 
                text = text.Replace("${ioengine}", FioExecutor.GetIOEngine(this.Platform));
                text = text.Replace($"${{{nameof(this.DirectIO).ToLower()}}}", this.DirectIO);
                text = text.Replace($"${{{nameof(this.DurationSec).ToLower()}}}", this.DurationSec.ToString());
                text = text.Replace($"${{{nameof(this.GroupReporting).ToLower()}}}", this.GroupReporting.ToString());

                text = text.Replace($"${{{nameof(this.RandomIOFileSize).ToLower()}}}", this.RandomIOFileSize.ToString());
                text = text.Replace($"${{{nameof(this.randomIOFilePath).ToLower()}}}", this.randomIOFilePath.ToString());
                text = text.Replace($"${{{nameof(this.SequentialIOFileSize).ToLower()}}}", this.SequentialIOFileSize.ToString());
                text = text.Replace($"${{{nameof(this.sequentialIOFilePath).ToLower()}}}", this.sequentialIOFilePath.ToString());
            }
            else
            {
                int randomReadIOdepth = this.RandomReadQueueDepth / this.RandomReadNumJobs;
                int randomWriteIOdepth = this.RandomWriteQueueDepth / this.RandomWriteNumJobs;
                int sequentialReadIOdepth = this.SequentialReadQueueDepth / this.SequentialReadNumJobs;
                int sequentialWriteIOdepth = this.SequentialWriteQueueDepth / this.SequentialWriteNumJobs;

                text = text.Replace("${ioengine}", FioExecutor.GetIOEngine(this.Platform));
                text = text.Replace($"${{{nameof(this.DirectIO).ToLower()}}}", this.DirectIO);
                text = text.Replace($"${{{nameof(this.DurationSec).ToLower()}}}", this.DurationSec.ToString());
                text = text.Replace($"${{{nameof(this.GroupReporting).ToLower()}}}", this.GroupReporting.ToString());

                text = text.Replace($"${{{nameof(this.RandomIOFileSize).ToLower()}}}", this.RandomIOFileSize.ToString());
                text = text.Replace($"${{{nameof(this.randomIOFilePath).ToLower()}}}", this.randomIOFilePath.ToString());
                text = text.Replace($"${{{nameof(this.SequentialIOFileSize).ToLower()}}}", this.SequentialIOFileSize.ToString());
                text = text.Replace($"${{{nameof(this.sequentialIOFilePath).ToLower()}}}", this.sequentialIOFilePath.ToString());

                text = text.Replace($"${{{nameof(this.RandomReadBlockSize).ToLower()}}}", this.RandomReadBlockSize);
                text = text.Replace($"${{{nameof(randomReadIOdepth).ToLower()}}}", randomReadIOdepth.ToString());
                text = text.Replace($"${{{nameof(this.randomReadIOPS).ToLower()}}}", this.randomReadIOPS.ToString());
                text = text.Replace($"${{{nameof(this.RandomReadNumJobs).ToLower()}}}", this.RandomReadNumJobs.ToString());

                text = text.Replace($"${{{nameof(this.RandomWriteBlockSize).ToLower()}}}", this.RandomWriteBlockSize);
                text = text.Replace($"${{{nameof(randomWriteIOdepth).ToLower()}}}", randomWriteIOdepth.ToString());
                text = text.Replace($"${{{nameof(this.randomWriteIOPS).ToLower()}}}", this.randomWriteIOPS.ToString());
                text = text.Replace($"${{{nameof(this.RandomWriteNumJobs).ToLower()}}}", this.RandomWriteNumJobs.ToString());

                text = text.Replace($"${{{nameof(this.SequentialReadBlockSize).ToLower()}}}", this.SequentialReadBlockSize);
                text = text.Replace($"${{{nameof(sequentialReadIOdepth).ToLower()}}}", sequentialReadIOdepth.ToString());
                text = text.Replace($"${{{nameof(this.sequentialReadIOPS).ToLower()}}}", this.sequentialReadIOPS.ToString());
                text = text.Replace($"${{{nameof(this.SequentialReadNumJobs).ToLower()}}}", this.SequentialReadNumJobs.ToString());

                text = text.Replace($"${{{nameof(this.SequentialWriteBlockSize).ToLower()}}}", this.SequentialWriteBlockSize);
                text = text.Replace($"${{{nameof(sequentialWriteIOdepth).ToLower()}}}", sequentialWriteIOdepth.ToString());
                text = text.Replace($"${{{nameof(this.sequentialWriteIOPS).ToLower()}}}", this.sequentialWriteIOPS.ToString());
                text = text.Replace($"${{{nameof(this.SequentialWriteNumJobs).ToLower()}}}", this.SequentialWriteNumJobs.ToString());
            }

            this.SystemManagement.FileSystem.File.WriteAllText(@destinationPath, text);
        }

        private Dictionary<string, IConvertible> GetMetricsMetadata()
        {
            var metricsMetadata = new Dictionary<string, IConvertible>();
            
            metricsMetadata[nameof(this.ProfileIteration).CamelCased()] = this.ProfileIteration;
            metricsMetadata[nameof(this.ProfileIterationStartTime).CamelCased()] = this.ProfileIterationStartTime;

            if (!this.DiskFill)
            {
                double randomReadBlockSizeKiB = Convert.ToDouble(TextParsingExtensions.TranslateStorageByUnit(this.RandomReadBlockSize, MetricUnit.Kilobytes));
                double randomWriteBlockSizeKiB = Convert.ToDouble(TextParsingExtensions.TranslateStorageByUnit(this.RandomWriteBlockSize, MetricUnit.Kilobytes));

                double sequentialReadBlockSizeKiB = Convert.ToDouble(TextParsingExtensions.TranslateStorageByUnit(this.SequentialReadBlockSize, MetricUnit.Kilobytes));
                double sequentialWriteBlockSizeKiB = Convert.ToDouble(TextParsingExtensions.TranslateStorageByUnit(this.SequentialWriteBlockSize, MetricUnit.Kilobytes));

                double randomIOFileSizeGiB = Convert.ToDouble(TextParsingExtensions.TranslateStorageByUnit(this.RandomIOFileSize, MetricUnit.Gigabytes));
                double sequentialIOFileSizeGiB = Convert.ToDouble(TextParsingExtensions.TranslateStorageByUnit(this.SequentialIOFileSize, MetricUnit.Gigabytes));

                metricsMetadata[nameof(this.TargetIOPS).CamelCased()] = this.TargetIOPS;

                metricsMetadata[nameof(this.DurationSec).CamelCased()] = this.DurationSec;
                metricsMetadata[nameof(this.DirectIO).CamelCased()] = this.DirectIO;
                metricsMetadata[nameof(this.GroupReporting).CamelCased()] = this.GroupReporting;

                metricsMetadata[nameof(randomIOFileSizeGiB).CamelCased()] = randomIOFileSizeGiB;
                metricsMetadata[nameof(sequentialIOFileSizeGiB).CamelCased()] = sequentialIOFileSizeGiB;
                metricsMetadata[nameof(this.randomIOFilePath).CamelCased()] = this.randomIOFilePath;
                metricsMetadata[nameof(this.sequentialIOFilePath).CamelCased()] = this.sequentialIOFilePath;

                metricsMetadata[nameof(sequentialReadBlockSizeKiB).CamelCased()] = sequentialReadBlockSizeKiB;
                metricsMetadata[nameof(this.SequentialReadQueueDepth).CamelCased()] = this.SequentialReadQueueDepth;
                metricsMetadata[nameof(this.SequentialReadNumJobs).CamelCased()] = this.SequentialReadNumJobs;
                metricsMetadata[nameof(this.SequentialReadWeight).CamelCased()] = this.SequentialReadWeight;

                metricsMetadata[nameof(sequentialWriteBlockSizeKiB).CamelCased()] = sequentialWriteBlockSizeKiB;
                metricsMetadata[nameof(this.SequentialWriteQueueDepth).CamelCased()] = this.SequentialWriteQueueDepth;
                metricsMetadata[nameof(this.SequentialWriteNumJobs).CamelCased()] = this.SequentialWriteNumJobs;
                metricsMetadata[nameof(this.SequentialWriteWeight).CamelCased()] = this.SequentialWriteWeight;

                metricsMetadata[nameof(randomReadBlockSizeKiB).CamelCased()] = randomReadBlockSizeKiB;
                metricsMetadata[nameof(this.RandomReadQueueDepth).CamelCased()] = this.RandomReadQueueDepth;
                metricsMetadata[nameof(this.RandomReadNumJobs).CamelCased()] = this.RandomReadNumJobs;
                metricsMetadata[nameof(this.RandomReadWeight).CamelCased()] = this.RandomReadWeight;

                metricsMetadata[nameof(randomWriteBlockSizeKiB).CamelCased()] = randomWriteBlockSizeKiB;
                metricsMetadata[nameof(this.RandomWriteQueueDepth).CamelCased()] = this.RandomWriteQueueDepth;
                metricsMetadata[nameof(this.RandomWriteNumJobs).CamelCased()] = this.RandomWriteNumJobs;
                metricsMetadata[nameof(this.RandomWriteWeight).CamelCased()] = this.RandomWriteWeight;

            }

            return metricsMetadata;
        }

        private void SetRuntimeParameters(int targetPercent)
        {
            this.totalIOPS = (this.TargetIOPS * targetPercent) / 100;
            int totalWeights = this.RandomReadWeight + this.RandomWriteWeight + this.SequentialReadWeight + this.SequentialWriteWeight;
            totalWeights = totalWeights == 0 ? 1 : totalWeights;

            this.randomReadIOPS = ((this.totalIOPS * this.RandomReadWeight) / totalWeights) / this.RandomReadNumJobs;
            this.randomWriteIOPS = ((this.totalIOPS * this.RandomWriteWeight) / totalWeights) / this.RandomWriteNumJobs;
            this.sequentialReadIOPS = ((this.totalIOPS * this.SequentialReadWeight) / totalWeights) / this.SequentialReadNumJobs;
            this.sequentialWriteIOPS = ((this.totalIOPS * this.SequentialWriteWeight) / totalWeights) / this.SequentialWriteNumJobs;
        }

        private void UpdateTestFilePaths(IEnumerable<Disk> disksToTest)
        {
            disksToTest.ThrowIfNullOrEmpty(nameof(disksToTest));
            disksToTest.OrderByDescending(disk => disk.Volumes.FirstOrDefault());
            this.randomIOFilePath = disksToTest.FirstOrDefault().DevicePath;

            if (disksToTest.Count() == 1)
            {
                this.sequentialIOFilePath = disksToTest.FirstOrDefault().DevicePath;
            }
            else
            {
                this.sequentialIOFilePath = disksToTest.ElementAtOrDefault(1).DevicePath;
            }

            this.Logger.LogTraceMessage($"File Path for {nameof(this.randomIOFilePath)} : {this.randomIOFilePath} and {nameof(this.sequentialIOFilePath)} : {this.sequentialIOFilePath}");
        }

        private string GetSections()
        {
            string sections = string.Empty;

            if (this.DiskFill)
            {
                sections = "--section initrandomio --section initsequentialio";
            }
            else
            {
                if (this.randomReadIOPS > 0)
                {
                    sections = sections + " --section randomreader";
                }

                if (this.randomWriteIOPS > 0)
                {
                    sections = sections + " --section randomwriter";
                }

                if (this.sequentialReadIOPS > 0)
                {
                    sections = sections + " --section sequentialreader";
                }

                if (this.sequentialWriteIOPS > 0)
                {
                    sections = sections + " --section sequentialwriter";
                }
            }

            return sections;
        }
    }
}
