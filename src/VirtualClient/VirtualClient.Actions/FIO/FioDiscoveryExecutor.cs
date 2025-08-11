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
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Manages the execution runtime of the FIO workload for Perf Engineering Discovery Scenario.
    /// </summary>
    [SupportedPlatforms("linux-arm64,linux-x64")]
    public class FioDiscoveryExecutor : FioExecutor
    {
        private static int variationNumber = 0;
        private static IAsyncPolicy fioDiscoveryRetryPolicy = Policy.Handle<Exception>().WaitAndRetryAsync(3, _ => RetryWaitTime);

        /// <summary>
        /// Initializes a new instance of the <see cref="FioDiscoveryExecutor"/> class.
        /// </summary>
        /// <param name="dependencies">Provides required dependencies to the workload.</param>
        /// <param name="parameters">The set of parameters defined for the action in the profile definition.</param>
        public FioDiscoveryExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            // Since in this case we are testing on raw disks, we are not cleaning up test files
            this.DeleteTestFilesOnFinish = false;

            // Convert to desired data types.
            this.DirectIO = parameters.GetValue<bool>(nameof(this.DirectIO), true) ? 1 : 0;
        }

        /// <summary>
        /// The Blocksizes list for discovery parameters.
        /// </summary>
        public string BlockSize
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.BlockSize));
            }
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
        /// The IO type whether it is randomRead,randomWrite, read (sequential read), write (sequential write).
        /// </summary>
        public string IOType
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.IOType));
            }
        }

        /// <summary>
        /// The maximum number of threads.
        /// </summary>
        public List<int> MaxThreads
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.MaxThreads)).Split(VirtualClientComponent.CommonDelimiters).Select(int.Parse).ToList();
            }
        }

        /// <summary>
        /// The Queue depths for discovery parameters.
        /// </summary>
        public List<int> QueueDepths
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.QueueDepths)).Split(VirtualClientComponent.CommonDelimiters).Select(int.Parse).ToList();
            }
        }

        /// <summary>
        /// Duration in Seconds.
        /// </summary>
        public int DurationSec
        {
            get
            {
                return this.Parameters.GetValue<int>(nameof(this.DurationSec));
            }
        }

        /// <summary>
        /// Group Id.
        /// </summary>
        public string GroupId
        {
            get
            {
                IConvertible value = string.Empty;
                if (this.Metadata != null)
                {
                    (this.Metadata as Dictionary<string, IConvertible>).TryGetValue(nameof(this.GroupId), out value);
                }

                return value == null ? string.Empty : value.ToString();
            }
        }

        /// <summary>
        /// Retry Wait Time for FIO executors.
        /// </summary>
        protected static TimeSpan RetryWaitTime { get; } = TimeSpan.FromSeconds(30);

        /// <summary>
        /// Executes the FIO workload, captures performance results and logs them to telemetry.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IEnumerable<Disk> disks = await this.SystemManagement.DiskManager.GetDisksAsync(cancellationToken);

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

            telemetryContext.AddContext("executable", this.ExecutablePath);
            telemetryContext.AddContext(nameof(disks), disks);
            telemetryContext.AddContext(nameof(disksToTest), disksToTest);

            this.WorkloadProcesses.Clear();
            List<Task> fioProcessTasks = new List<Task>();

            if (this.DiskFill)
            {
                Interlocked.Exchange(ref variationNumber, 0);

                if (await this.IsDiskFillCompleteAsync(cancellationToken) == false)
                {
                    string commandLine = this.ApplyParameter(this.CommandLine, nameof(this.Scenario), this.Scenario);
                    commandLine = this.ApplyParameter(commandLine, nameof(this.DiskFillSize), this.DiskFillSize);

                    this.Logger.LogTraceMessage($"{this.Scenario}.ExecutionStarted", telemetryContext);
                    this.WorkloadProcesses.AddRange(this.CreateWorkloadProcesses(this.ExecutablePath, commandLine, disksToTest, this.ProcessModel, telemetryContext));

                    foreach (DiskWorkloadProcess process in this.WorkloadProcesses)
                    {
                        fioProcessTasks.Add(this.ExecuteWorkloadAsync(process, this.Scenario, telemetryContext, cancellationToken));
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.WhenAll(fioProcessTasks);
                    }

                    this.Logger.LogTraceMessage($"{this.Scenario}.ExecutionEnded", telemetryContext);

                    await this.RegisterDiskFillCompleteAsync(cancellationToken);
                }
            }
            else
            {
                List<VirtualClientException> exceptions = new List<VirtualClientException>();
                foreach (int maxThreads in this.MaxThreads)
                {
                    foreach (int queueDepth in this.QueueDepths)
                    {
                        int variation = Interlocked.Increment(ref variationNumber);
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            int numJobs = (queueDepth < maxThreads) ? queueDepth : maxThreads;
                            int queueDepthPerThread = (queueDepth + numJobs - 1) / numJobs;

                            // e.g. fio_discovery_randread_134G_4K_d8_th8
                            string testName = $"fio_discovery_{this.IOType.ToLowerInvariant()}_{this.FileSize}_{this.BlockSize}_d{queueDepthPerThread}_th{numJobs}";

                            EventContext variationContext = telemetryContext.Clone().AddContext(nameof(testName), testName);

                            try
                            {
                                await this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteVariation", variationContext, async () =>
                                {
                                    // Converting Byte to Gigabytes
                                    double fileSizeGiB = Convert.ToDouble(TextParsingExtensions.TranslateStorageByUnit(this.FileSize, MetricUnit.Gigabytes));

                                    // Converting Bytes to Kilobytes
                                    double blockSizeKiB = Convert.ToDouble(TextParsingExtensions.TranslateStorageByUnit(this.BlockSize, MetricUnit.Kilobytes));

                                    string commandLine = this.ApplyParameter(this.CommandLine, nameof(this.FileSize), this.FileSize);

                                    commandLine = this.ApplyParameter(commandLine, nameof(this.IOType), this.IOType);
                                    commandLine = this.ApplyParameter(commandLine, nameof(this.BlockSize), this.BlockSize);
                                    commandLine = this.ApplyParameter(commandLine, nameof(this.DurationSec), this.DurationSec.ToString());

                                    int direct = this.DirectIO;
                                    commandLine = this.ApplyParameter(commandLine, nameof(this.DirectIO), direct);
                                    commandLine = $"--name={testName} --numjobs={numJobs} --iodepth={queueDepthPerThread} --bs={this.BlockSize} --rw={this.IOType} {commandLine}";

                                    string filePath = string.Join(',', disksToTest.Select(disk => disk.DevicePath).ToArray());

                                    Dictionary<string, IConvertible> metricsMetadata = new Dictionary<string, IConvertible>
                                    {
                                        [nameof(this.GroupId).CamelCased()] = this.GroupId,
                                        [nameof(this.DurationSec).CamelCased()] = this.DurationSec,
                                        [nameof(this.ProfileIteration).CamelCased()] = this.ProfileIteration,
                                        [nameof(this.ProfileIterationStartTime).CamelCased()] = this.ProfileIterationStartTime,
                                        [nameof(blockSizeKiB).CamelCased()] = blockSizeKiB,
                                        [nameof(queueDepth).CamelCased()] = queueDepth,
                                        [nameof(this.IOType).CamelCased()] = this.IOType,
                                        [nameof(testName).CamelCased()] = testName,
                                        [nameof(commandLine).CamelCased()] = commandLine,
                                        [nameof(variation).CamelCased()] = variation,
                                        [nameof(maxThreads).CamelCased()] = maxThreads,
                                        [nameof(numJobs).CamelCased()] = numJobs,
                                        [nameof(fileSizeGiB).CamelCased()] = fileSizeGiB,
                                        [nameof(filePath).CamelCased()] = filePath
                                    };

                                    await fioDiscoveryRetryPolicy.ExecuteAsync(async () =>
                                    {
                                        using (BackgroundOperations profiling = BackgroundOperations.BeginProfiling(this, cancellationToken))
                                        {
                                            this.WorkloadProcesses.Clear();
                                            this.WorkloadProcesses.AddRange(this.CreateWorkloadProcesses(this.ExecutablePath, commandLine, disksToTest, this.ProcessModel, telemetryContext));

                                            foreach (DiskWorkloadProcess process in this.WorkloadProcesses)
                                            {
                                                fioProcessTasks.Add(this.ExecuteWorkloadAsync(process, testName, variationContext, cancellationToken, metricsMetadata));
                                            }

                                            if (!cancellationToken.IsCancellationRequested)
                                            {
                                                await Task.WhenAll(fioProcessTasks);
                                            }
                                        }
                                    });

                                });

                                await this.CleanUpWorkloadTestFilesAsync();
                            }
                            catch (VirtualClientException exc)
                            {
                                exceptions.Add(exc);
                            }
                            finally
                            {
                                await this.CleanUpWorkloadTestFilesAsync();
                            }
                        }
                    }
                }

                if (exceptions.Any())
                {
                    AggregateException aggregateException = new AggregateException(exceptions);

                    WorkloadException workloadException = new WorkloadException(
                        $"{nameof(FioDiscoveryExecutor)} failed with following exceptions",
                        aggregateException,
                        exceptions.Max(ex => ex.Reason));

                    throw workloadException;
                }
            }
        }

        /// <summary>
        /// Returns the target device/file test path. Note that this may be either a file
        /// or a direct path to the physical device (e.g. /dev/sda, /mnt_dev_sda1/fio-test.dat).
        /// </summary>
        protected override string GetTestDevicePath(Disk disk)
        {
            return disk.DevicePath;
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            // Parameters themselves throw on being null.
        }

        private async Task CleanUpWorkloadTestFilesAsync()
        {
            foreach (DiskWorkloadProcess workload in this.WorkloadProcesses)
            {
                await this.DeleteTestVerificationFilesAsync(workload.TestFiles);

                if (this.DeleteTestFilesOnFinish)
                {
                    await this.DeleteTestFilesAsync(workload.TestFiles);
                }
            }
        }
    }
}