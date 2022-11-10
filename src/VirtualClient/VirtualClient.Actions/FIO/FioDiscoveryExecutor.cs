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
    using static System.Net.WebRequestMethods;

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

                    this.Logger.LogTraceMessage($"{this.Scenario}.ExecutionStarted", telemetryContext);

                    this.WorkloadProcesses.AddRange(this.CreateWorkloadProcesses(this.ExecutablePath, commandLine, disksToTest, this.ProcessModel));

                    foreach (DiskPerformanceWorkloadProcess process in this.WorkloadProcesses)
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
                                    int threads = (queueDepth < maxThreads) ? queueDepth : maxThreads;
                                    int queueDepthPerThread = (queueDepth + threads - 1) / threads;
                                    string testName = this.ApplyParameter(this.Scenario, nameof(ioType), ioType);
                                    testName = this.ApplyParameter(testName, nameof(blockSize), blockSize);
                                    testName = $"{testName}_d{queueDepthPerThread}_th{threads}";

                                    try
                                    {
                                        // Converting Byte to Gigabytes
                                        double fileSizeGiB = Convert.ToDouble(TextParsingExtensions.TranslateStorageByUnit(this.FileSize, MetricUnit.Gigabytes));

                                        // Converting Bytes to Kilobytes
                                        double blockSizeKiB = Convert.ToDouble(TextParsingExtensions.TranslateStorageByUnit(blockSize, MetricUnit.Kilobytes));

                                        string commandLine = this.ApplyParameter(this.CommandLine, nameof(this.FileSize), this.FileSize);
                                        commandLine = this.ApplyParameter(commandLine, nameof(ioType), ioType);
                                        commandLine = this.ApplyParameter(commandLine, nameof(blockSize), blockSize);

                                        commandLine = commandLine + $" --name={testName} --numjobs={threads} --iodepth={queueDepthPerThread}";

                                        EventContext processContext = telemetryContext.Clone();
                                        processContext.AddContext(nameof(testName), testName);
                                        Dictionary<string, IConvertible> metricsMetadata = new Dictionary<string, IConvertible>();

                                        metricsMetadata[nameof(blockSizeKiB).CamelCased()] = blockSizeKiB;
                                        metricsMetadata[nameof(queueDepth).CamelCased()] = queueDepth;
                                        metricsMetadata[nameof(ioType).CamelCased()] = ioType;
                                        metricsMetadata[nameof(testName).CamelCased()] = testName;
                                        metricsMetadata[nameof(commandLine).CamelCased()] = commandLine;
                                        metricsMetadata[nameof(variation).CamelCased()] = variation;
                                        metricsMetadata[nameof(maxThreads).CamelCased()] = maxThreads;
                                        metricsMetadata[nameof(threads).CamelCased()] = threads;
                                        metricsMetadata[nameof(fileSizeGiB).CamelCased()] = fileSizeGiB;
                                        metricsMetadata[nameof(this.ProfileIteration).CamelCased()] = this.ProfileIteration;
                                        metricsMetadata[nameof(this.ProfileIterationStartTime).CamelCased()] = this.ProfileIterationStartTime;

                                        this.Logger.LogTraceMessage($"{testName}.ExecutionStarted", processContext);

                                        this.WorkloadProcesses.Clear();

                                        await fioDiscoveryRetryPolicy.ExecuteAsync(async () =>
                                        {
                                            this.WorkloadProcesses.AddRange(this.CreateWorkloadProcesses(this.ExecutablePath, commandLine, disksToTest, this.ProcessModel));

                                            foreach (DiskPerformanceWorkloadProcess process in this.WorkloadProcesses)
                                            {
                                                fioProcessTasks.Add(this.ExecuteWorkloadAsync(process, testName, processContext, cancellationToken, metricsMetadata));
                                            }

                                            if (!cancellationToken.IsCancellationRequested)
                                            {
                                                await Task.WhenAll(fioProcessTasks).ConfigureAwait(false);
                                            }
                                        }).ConfigureAwait(false);

                                        await this.CleanUpWorkloadTestFilesAsync()
                                            .ConfigureAwait(false);

                                        this.Logger.LogTraceMessage($"{testName}.ExecutionCompleted", processContext);

                                    }
                                    catch (VirtualClientException exc)
                                    {
                                        // Adding exception instead of throwing it.
                                        // Furhter runs might not fail. We should try running it.
                                        this.Logger.LogError($"{testName}.ExecutionFailed", exc);
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
        protected override DiskPerformanceWorkloadProcess CreateWorkloadProcess(string executable, string commandArguments, string testedInstance, params Disk[] disksToTest)
        {
            string ioEngine = FioExecutor.GetIOEngine(this.Platform);
            string[] testFiles = disksToTest.Select(disk => disk.DevicePath).ToArray();
            string fioArguments = $"{commandArguments} --ioengine={ioEngine} {string.Join(" ", testFiles.Select(file => $"--filename={file}"))}".Trim();

            IProcessProxy process = this.SystemManagement.ProcessManager.CreateElevatedProcess(this.Platform, executable, fioArguments);

            return new DiskPerformanceWorkloadProcess(process, testedInstance, testFiles);
        }

        /// <inheritdoc/>
        protected override void ValidateParameters()
        {
            // Parameters themselves throw on being null.
        }

        private string ApplyParameter(string commandLine, string parameterName, string value)
        {
            string updatedCommandLine = commandLine.Replace($"[{parameterName}]", value, StringComparison.OrdinalIgnoreCase);
            return updatedCommandLine;
        }

        private async Task CleanUpWorkloadTestFilesAsync()
        {
            foreach (DiskPerformanceWorkloadProcess workload in this.WorkloadProcesses)
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