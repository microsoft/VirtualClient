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
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Platform;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

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
        private long totalIOPS;
        private long randomReadIOPS;
        private long randomWriteIOPS;
        private long sequentialReadIOPS;
        private long sequentialWriteIOPS;

        private static IAsyncPolicy fioMultiThroughputRetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(3, _ => RetryWaitTime);

        /// <summary>
        /// Initializes a new instance of the <see cref="FioMultiThroughputExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the workload.</param>
        /// <param name="parameters">The set of parameters defined for the action in the profile definition.</param>
        public FioMultiThroughputExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            // Since in this case we are testing on raw disks, we are not cleaning up test files
            this.DeleteTestFilesOnFinish = false;

            // Convert to desired data types.
            this.DirectIO = parameters.GetValue<bool>(nameof(this.DirectIO), true) ? 1 : 0;
        }

        /// <summary>
        /// Parameter. True to used direct, non-buffered I/O (default). False to use buffered I/O.
        /// </summary>
        public int DirectIO
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.DirectIO), 1);
            }

            private set
            {
                this.Parameters[nameof(this.DirectIO)] = value;
            }
        }

        /// <summary>
        /// Group Id.
        /// </summary>
        public string GroupId
        {
            get
            {
                object metadata = null;
                EventContext.PersistentProperties.TryGetValue(nameof(metadata), out metadata);

                IConvertible value = string.Empty;
                if (metadata != null)
                {
                    (metadata as Dictionary<string, IConvertible>).TryGetValue(nameof(this.GroupId).CamelCased(), out value);
                }

                return value == null ? string.Empty : value.ToString();
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
        /// Queue Depth for Sequential Read.
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
        /// Number of sequential disks.
        /// </summary>
        public int SequentialDiskCount
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.SequentialDiskCount), 1);
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
                    "of the existing disks. Verify or specify the disk filter.",
                    ErrorReason.DependencyNotFound);
            }

            if (disksToTest?.Any(disk => disk.IsOperatingSystem()) == true)
            {
                throw new WorkloadException(
                    "Expected disks to test contain the disk on which the operation system is installed. This scenario runs I/O operations against the raw disk without any file " +
                    "system layers and can overwrite important information on the disk such as disk volume partitions. As such I/O operations against the operating system disk " +
                    "are not supported.",
                    ErrorReason.NotSupported);
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
                    await this.ExecuteWorkloadAsync(this.Scenario, new Dictionary<string, IConvertible>(), telemetryContext, cancellationToken)
                        .ConfigureAwait(false);

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

                        // e.g.
                        // fio_multithroughput_read/write/randread/randwrite_20G_128G(56k/56k/8k/8k, d64/64/512/512, th1/1/1/1, w0/329/5416/4255)_10%
                        string testName = $"fio_multithroughput_read/write/randread/randwrite_{this.SequentialIOFileSize}_{this.RandomIOFileSize}(" +
                            $"{this.SequentialReadBlockSize.ToLowerInvariant()}/{this.SequentialWriteBlockSize.ToLowerInvariant()}/{this.RandomReadBlockSize.ToLowerInvariant()}/{this.RandomWriteBlockSize.ToLowerInvariant()}, " +
                            $"d{this.SequentialReadQueueDepth}/{this.SequentialWriteQueueDepth}/{this.RandomReadQueueDepth}/{this.RandomWriteQueueDepth}, " +
                            $"th{this.SequentialReadNumJobs}/{this.SequentialWriteNumJobs}/{this.RandomReadNumJobs}/{this.RandomWriteNumJobs}, " +
                            $"w{this.SequentialReadWeight}/{this.SequentialWriteWeight}/{this.RandomReadWeight}/{this.RandomWriteWeight})_{targetPercent}%";

                        EventContext variationContext = telemetryContext.Clone().AddContext(nameof(testName), testName);

                        try
                        {
                            await this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteVariation", variationContext, async () =>
                            {
                                this.SetRuntimeParameters(targetPercent);

                                Dictionary<string, IConvertible> metricsMetadata = new Dictionary<string, IConvertible>
                                {
                                    [nameof(targetPercent).CamelCased()] = targetPercent,
                                    [nameof(variation).CamelCased()] = variation,
                                    [nameof(testName).CamelCased()] = testName
                                };

                                await fioMultiThroughputRetryPolicy.ExecuteAsync(async () =>
                                {
                                    await this.ExecuteWorkloadAsync(testName, metricsMetadata, variationContext, cancellationToken)
                                        .ConfigureAwait(false);
                                }).ConfigureAwait(false);

                            }).ConfigureAwait(false);
                        }
                        catch (VirtualClientException exc)
                        {
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
        protected override void Validate()
        {
            // Override default validation.
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

        /// <inheritdoc/>
        protected override void CaptureMetrics(
            IProcessProxy workloadProcess, string testName, string metricCategorization, string commandArguments, EventContext telemetryContext, Dictionary<string, IConvertible> metricMetadata = null)
        {
            this.GetMetricsParsingDirectives(out bool parseReadMetrics, out bool parseWriteMetrics, commandArguments);
            FioMetricsParser parser = new FioMetricsParser(workloadProcess.StandardOutput.ToString(), parseReadMetrics, parseWriteMetrics);
            IList<Metric> metrics = parser.Parse();

            if (this.MetricFilters?.Any() == true)
            {
                metrics = metrics.FilterBy(this.MetricFilters).ToList();
            }

            if (metricMetadata != null)
            {
                foreach (var metric in metrics)
                {
                    foreach (var metricMetadataValue in metricMetadata)
                    {
                        metric.Metadata[metricMetadataValue.Key] = metricMetadataValue.Value;
                    }
                }
            }

            this.Logger.LogMetrics(
               "FIO",
               testName,
               workloadProcess.StartTime,
               workloadProcess.ExitTime,
               this.GetSectionizedMetrics(metrics),
               metricCategorization,
               commandArguments,
               this.Tags,
               telemetryContext);
        }

        /// <summary>
        /// Executes the FIO workload, captures performance results and logs them to telemetry.
        /// </summary>
        private async Task ExecuteWorkloadAsync(string testName, Dictionary<string, IConvertible> metricsMetadata, EventContext telemetryContext, CancellationToken cancellationToken)
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

                DiskWorkloadProcess process = this.CreateWorkloadProcess(this.ExecutablePath, jobFilePath);

                metricsMetadata[nameof(this.CommandLine)] = process.CommandArguments;
                using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                {
                    await this.ExecuteWorkloadAsync(process, testName, telemetryContext, cancellationToken, metricsMetadata)
                    .ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Creates a process to run FIO targeting the file names specified.
        /// </summary>
        private DiskWorkloadProcess CreateWorkloadProcess(string executable, string jobFile)
        {
            string fioArguments = $"{jobFile.Trim()} {this.GetSections().Trim()} --time_based --output-format=json --thread --fallocate=none".Trim();

            IProcessProxy process = this.SystemManagement.ProcessManager.CreateElevatedProcess(this.Platform, executable, fioArguments);

            string testedInstances = $"{this.randomIOFilePath},{this.sequentialIOFilePath}";
            List<string> testFiles = new List<string>() { this.randomIOFilePath, this.sequentialIOFilePath };

            return new DiskWorkloadProcess(process, testedInstances, testFiles.ToArray());
        }

        private void CreateOrUpdateJobFile(string sourcePath, string destinationPath)
        {
            string text = this.SystemManagement.FileSystem.File.ReadAllText(sourcePath);
            int direct = this.DirectIO;

            if (this.DiskFill)
            {
                text = text.Replace("${ioengine}", FioExecutor.GetIOEngine(this.Platform));
                text = text.Replace($"${{{nameof(this.DurationSec).ToLower()}}}", this.DurationSec.ToString());
                text = text.Replace($"${{{nameof(this.GroupReporting).ToLower()}}}", this.GroupReporting.ToString());
                text = text.Replace($"${{{nameof(this.DirectIO).ToLower()}}}", direct.ToString());
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
                text = text.Replace($"${{{nameof(this.DirectIO).ToLower()}}}", direct.ToString());
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

        private IList<Metric> GetSectionizedMetrics(IList<Metric> metrics)
        {
            IList<Metric> sectionizedMetrics = new List<Metric>();

            foreach (var metric in metrics)
            {
                string ioType = metric.Metadata["rw"].ToString();
                bool validMetric = true;

                switch (ioType.ToLower())
                {
                    case "read":
                    case "randread":
                        if (metric.Name.StartsWith("write", StringComparison.OrdinalIgnoreCase))
                        {
                            validMetric = false;
                        }

                        break;

                    case "write":
                    case "randwrite":
                        if (metric.Name.StartsWith("read", StringComparison.OrdinalIgnoreCase))
                        {
                            validMetric = false;
                        }

                        break;

                    default:
                        validMetric = true;
                        break;
                }

                if (validMetric)
                {
                    IDictionary<string, IConvertible> metadata = this.GetMetricsMetadata(ioType);

                    foreach (var metadataValue in metadata)
                    {
                        metric.Metadata[metadataValue.Key] = metadataValue.Value;
                    }

                    sectionizedMetrics.Add(metric);
                }
            }

            return sectionizedMetrics;
        }

        private IDictionary<string, IConvertible> GetMetricsMetadata(string ioType)
        {
            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            metadata[nameof(this.ProfileIteration).CamelCased()] = this.ProfileIteration;
            metadata[nameof(this.ProfileIterationStartTime).CamelCased()] = this.ProfileIterationStartTime;
            metadata[nameof(this.GroupId)] = this.GroupId;

            if (!this.DiskFill)
            {
                string blockSizeKiB = string.Empty;
                string fileSizeGiB = string.Empty;
                string queueDepth = string.Empty;
                string numJobs = string.Empty;
                string filePath = string.Empty;
                string weight = string.Empty;

                switch (ioType.ToLower())
                {
                    case "randread":
                        blockSizeKiB = TextParsingExtensions.TranslateStorageByUnit(this.RandomReadBlockSize, MetricUnit.Kilobytes);
                        fileSizeGiB = TextParsingExtensions.TranslateStorageByUnit(this.RandomIOFileSize, MetricUnit.Gigabytes);
                        queueDepth = this.RandomReadQueueDepth.ToString();
                        numJobs = this.RandomReadNumJobs.ToString();
                        filePath = this.randomIOFilePath.ToString();
                        weight = this.RandomReadWeight.ToString();

                        break;

                    case "read":
                        blockSizeKiB = TextParsingExtensions.TranslateStorageByUnit(this.SequentialReadBlockSize, MetricUnit.Kilobytes);
                        fileSizeGiB = TextParsingExtensions.TranslateStorageByUnit(this.SequentialIOFileSize, MetricUnit.Gigabytes);
                        queueDepth = this.SequentialReadQueueDepth.ToString();
                        numJobs = this.SequentialReadNumJobs.ToString();
                        filePath = this.sequentialIOFilePath.ToString();
                        weight = this.SequentialReadWeight.ToString();

                        break;

                    case "randwrite":
                        blockSizeKiB = TextParsingExtensions.TranslateStorageByUnit(this.RandomWriteBlockSize, MetricUnit.Kilobytes);
                        fileSizeGiB = TextParsingExtensions.TranslateStorageByUnit(this.RandomIOFileSize, MetricUnit.Gigabytes);
                        queueDepth = this.RandomWriteQueueDepth.ToString();
                        numJobs = this.RandomWriteNumJobs.ToString();
                        filePath = this.randomIOFilePath.ToString();
                        weight = this.RandomWriteWeight.ToString();

                        break;

                    case "write":
                        blockSizeKiB = TextParsingExtensions.TranslateStorageByUnit(this.SequentialWriteBlockSize, MetricUnit.Kilobytes);
                        fileSizeGiB = TextParsingExtensions.TranslateStorageByUnit(this.SequentialIOFileSize, MetricUnit.Gigabytes);
                        queueDepth = this.SequentialWriteQueueDepth.ToString();
                        numJobs = this.SequentialWriteNumJobs.ToString();
                        filePath = this.sequentialIOFilePath.ToString();
                        weight = this.SequentialWriteWeight.ToString();

                        break;
                }

                metadata[nameof(this.TargetIOPS).CamelCased()] = this.TargetIOPS;
                metadata[nameof(this.DurationSec).CamelCased()] = this.DurationSec;
                metadata[nameof(this.DirectIO).CamelCased()] = this.DirectIO;
                metadata[nameof(this.GroupReporting).CamelCased()] = this.GroupReporting;
                metadata[nameof(ioType).CamelCased()] = ioType;
                metadata[nameof(blockSizeKiB).CamelCased()] = blockSizeKiB;
                metadata[nameof(fileSizeGiB).CamelCased()] = fileSizeGiB;
                metadata[nameof(queueDepth).CamelCased()] = queueDepth;
                metadata[nameof(numJobs).CamelCased()] = numJobs;
                metadata[nameof(filePath).CamelCased()] = filePath;
                metadata[nameof(weight).CamelCased()] = weight;
            }

            return metadata;
        }

        private void SetRuntimeParameters(int targetPercent)
        {
            this.totalIOPS = (this.TargetIOPS * targetPercent) / 100;
            long totalWeights = this.RandomReadWeight + this.RandomWriteWeight + this.SequentialReadWeight + this.SequentialWriteWeight;
            totalWeights = totalWeights == 0 ? 1 : totalWeights;

            this.randomReadIOPS = ((this.totalIOPS * this.RandomReadWeight) / totalWeights) / this.RandomReadNumJobs;
            this.randomWriteIOPS = ((this.totalIOPS * this.RandomWriteWeight) / totalWeights) / this.RandomWriteNumJobs;
            this.sequentialReadIOPS = ((this.totalIOPS * this.SequentialReadWeight) / totalWeights) / this.SequentialReadNumJobs;
            this.sequentialWriteIOPS = ((this.totalIOPS * this.SequentialWriteWeight) / totalWeights) / this.SequentialWriteNumJobs;
        }

        private void UpdateTestFilePaths(IEnumerable<Disk> disksToTest)
        {
            disksToTest.ThrowIfNullOrEmpty(nameof(disksToTest));
            disksToTest.OrderByDescending(disk => disk.SizeInBytes(this.Platform));

            if (disksToTest.Count() == 1)
            {
                this.randomIOFilePath = disksToTest.FirstOrDefault().DevicePath;
                this.sequentialIOFilePath = disksToTest.FirstOrDefault().DevicePath;
            }
            else
            {
                int sequentialDiskCount = this.SequentialDiskCount;

                if (sequentialDiskCount >= disksToTest.Count())
                {
                    sequentialDiskCount = disksToTest.Count() - 1;
                    this.Logger.LogTraceMessage($"{nameof(sequentialDiskCount)} should be less than total disks to test. Setting it to {sequentialDiskCount}.");
                }

                int randomDiskCount = disksToTest.Count() - sequentialDiskCount;
                this.Logger.LogTraceMessage($"{nameof(randomDiskCount)} : {randomDiskCount} and {nameof(sequentialDiskCount)}: {sequentialDiskCount}.");

                this.randomIOFilePath = string.Join(':', disksToTest.Take(randomDiskCount).Select(disk => disk.DevicePath).ToArray());
                this.sequentialIOFilePath = string.Join(':', disksToTest.TakeLast(sequentialDiskCount).Select(disk => disk.DevicePath).ToArray());
            }

            // e.g.
            // /dev/sdc
            // /dev/sdc, /dev/sdd, /dev/sde
            this.Logger.LogTraceMessage($"Disk Targets (Random I/O): {this.randomIOFilePath.Replace(":", ", ")}");
            this.Logger.LogTraceMessage($"Disk Targets (Sequential I/O): '{this.sequentialIOFilePath.Replace(":", ", ")}'");
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