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
    /// Manages the execution runtime of the FIO workload for Perf Engineering Discovery Scenario.
    /// </summary>
    [UnixCompatible]
    public class FioDiscoveryExecutor : FioExecutor
    {
        private static readonly object VariationLock = new object();
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
        public List<string> BlockSize
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.BlockSize)).Split(VirtualClientComponent.CommonDelimiters).ToList();
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
        /// The IO types list whether it is randomRead,randomWrite,sequentialRead,sequentialWrite.
        /// </summary>
        public List<string> IOType
        {
            get
            {
                return this.Parameters.GetValue<string>(nameof(this.IOType)).Split(VirtualClientComponent.CommonDelimiters).ToList();
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
            string ioEngine = FioExecutor.GetIOEngine(this.Platform);

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

            telemetryContext.AddContext("executable", this.ExecutablePath);
            telemetryContext.AddContext(nameof(ioEngine), ioEngine);
            telemetryContext.AddContext(nameof(disks), disks);
            telemetryContext.AddContext(nameof(disksToTest), disksToTest);

            this.WorkloadProcesses.Clear();
            List<Task> fioProcessTasks = new List<Task>();

            if (this.DiskFill)
            {
                Interlocked.Exchange(ref variationNumber, 0);

                if (await this.IsDiskFillCompleteAsync(cancellationToken).ConfigureAwait(false) == false)
                {
                    string commandLine = this.ApplyParameter(this.CommandLine, nameof(this.Scenario), this.Scenario);
                    commandLine = this.ApplyParameter(commandLine, nameof(this.DiskFillSize), this.DiskFillSize);
                    commandLine = commandLine + $" --ioengine={ioEngine}";

                    this.Logger.LogTraceMessage($"{this.Scenario}.ExecutionStarted", telemetryContext);
                    this.WorkloadProcesses.AddRange(this.CreateWorkloadProcesses(this.ExecutablePath, commandLine, disksToTest, this.ProcessModel));

                    foreach (DiskWorkloadProcess process in this.WorkloadProcesses)
                    {
                        fioProcessTasks.Add(this.ExecuteWorkloadAsync(process, this.Scenario, telemetryContext, cancellationToken));
                    }

                    if (!cancellationToken.IsCancellationRequested)
                    {
                        await Task.WhenAll(fioProcessTasks).ConfigureAwait(false);
                    }

                    this.Logger.LogTraceMessage($"{this.Scenario}.ExecutionEnded", telemetryContext);

                    await this.RegisterDiskFillCompleteAsync(cancellationToken)
                            .ConfigureAwait(false);
                }
            }
            else
            {
                List<VirtualClientException> exceptions = new List<VirtualClientException>();
                foreach (int maxThreads in this.MaxThreads)
                {
                    foreach (string ioType in this.IOType)
                    {
                        foreach (string blockSize in this.BlockSize)
                        {
                            foreach (int queueDepth in this.QueueDepths)
                            {
                                int variation = Interlocked.Increment(ref variationNumber);
                                if (!cancellationToken.IsCancellationRequested)
                                {
                                    int numJobs = (queueDepth < maxThreads) ? queueDepth : maxThreads;
                                    int queueDepthPerThread = (queueDepth + numJobs - 1) / numJobs;

                                    // e.g. fio_discovery_randread_134G_4K_d8_th8
                                    string testName = $"fio_discovery_{ioType.ToLowerInvariant()}_{this.FileSize}_{blockSize}_d{queueDepthPerThread}_th{numJobs}";

                                    EventContext variationContext = telemetryContext.Clone().AddContext(nameof(testName), testName);

                                    try
                                    {
                                        await this.Logger.LogMessageAsync($"{this.TypeName}.ExecuteVariation", variationContext, async () =>
                                        {
                                            // Converting Byte to Gigabytes
                                            double fileSizeGiB = Convert.ToDouble(TextParsingExtensions.TranslateStorageByUnit(this.FileSize, MetricUnit.Gigabytes));

                                            // Converting Bytes to Kilobytes
                                            double blockSizeKiB = Convert.ToDouble(TextParsingExtensions.TranslateStorageByUnit(blockSize, MetricUnit.Kilobytes));

                                            string commandLine = this.ApplyParameter(this.CommandLine, nameof(this.FileSize), this.FileSize);

                                            commandLine = this.ApplyParameter(commandLine, nameof(this.IOType), ioType);
                                            commandLine = this.ApplyParameter(commandLine, nameof(this.BlockSize), blockSize);
                                            commandLine = this.ApplyParameter(commandLine, nameof(this.DurationSec), this.DurationSec.ToString());

                                            int direct = this.DirectIO;
                                            commandLine = this.ApplyParameter(commandLine, nameof(this.DirectIO), direct);
                                            commandLine = $"--name={testName} --numjobs={numJobs} --iodepth={queueDepthPerThread} --ioengine={ioEngine} " + commandLine;

                                            string filePath = string.Join(',', disksToTest.Select(disk => disk.DevicePath).ToArray());

                                            Dictionary<string, IConvertible> metricsMetadata = new Dictionary<string, IConvertible>
                                            {
                                                [nameof(this.GroupId).CamelCased()] = this.GroupId,
                                                [nameof(this.DurationSec).CamelCased()] = this.DurationSec,
                                                [nameof(this.ProfileIteration).CamelCased()] = this.ProfileIteration,
                                                [nameof(this.ProfileIterationStartTime).CamelCased()] = this.ProfileIterationStartTime,
                                                [nameof(blockSizeKiB).CamelCased()] = blockSizeKiB,
                                                [nameof(queueDepth).CamelCased()] = queueDepth,
                                                [nameof(ioType).CamelCased()] = ioType,
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
                                                    this.WorkloadProcesses.AddRange(this.CreateWorkloadProcesses(this.ExecutablePath, commandLine, disksToTest, this.ProcessModel));

                                                    foreach (DiskWorkloadProcess process in this.WorkloadProcesses)
                                                    {
                                                        fioProcessTasks.Add(this.ExecuteWorkloadAsync(process, testName, variationContext, cancellationToken, metricsMetadata));
                                                    }

                                                    if (!cancellationToken.IsCancellationRequested)
                                                    {
                                                        await Task.WhenAll(fioProcessTasks).ConfigureAwait(false);
                                                    }
                                                }
                                            }).ConfigureAwait(false);

                                        }).ConfigureAwait(false);

                                        await this.CleanUpWorkloadTestFilesAsync()
                                            .ConfigureAwait(false);
                                    }
                                    catch (VirtualClientException exc)
                                    {
                                        exceptions.Add(exc);
                                    }
                                    finally
                                    {
                                        await this.CleanUpWorkloadTestFilesAsync()
                                            .ConfigureAwait(false);
                                    }
                                }
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
        /// Creates a process to run FIO targeting the disks specified.
        /// </summary>
        /// <param name="executable">The full path to the FIO executable.</param>
        /// <param name="commandArguments">
        /// The command line arguments to supply to the FIO executable (e.g. --name=fio_randread_4GB_4k_d1_th1_direct --ioengine=libaio).
        /// </param>
        /// <param name="testedInstance">The disk instance under test (e.g. remote_disk, remote_disk_premium_lrs).</param>
        /// <param name="disksToTest">The disks under test.</param>
        protected override DiskWorkloadProcess CreateWorkloadProcess(string executable, string commandArguments, string testedInstance, params Disk[] disksToTest)
        {
            string[] testFiles = disksToTest.Select(disk => disk.DevicePath).ToArray();
            string fioArguments = $"{commandArguments} {string.Join(" ", testFiles.Select(file => $"--filename={file}"))}".Trim();

            IProcessProxy process = this.SystemManagement.ProcessManager.CreateElevatedProcess(this.Platform, executable, fioArguments);

            return new DiskWorkloadProcess(process, testedInstance, testFiles);
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
                await this.DeleteTestVerificationFilesAsync(workload.TestFiles)
                    .ConfigureAwait(false);

                if (this.DeleteTestFilesOnFinish)
                {
                    await this.DeleteTestFilesAsync(workload.TestFiles)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}