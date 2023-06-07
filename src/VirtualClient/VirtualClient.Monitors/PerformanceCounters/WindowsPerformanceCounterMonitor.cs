// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Monitors
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    /// <summary>
    /// Monitor captures performance counters from Windows systems.
    /// </summary>
    public class WindowsPerformanceCounterMonitor : VirtualClientIntervalBasedMonitor
    {
        private static readonly TimeSpan DefaultCaptureInterval = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan DefaultDiscoveryInterval = TimeSpan.FromMinutes(2);

        private ConcurrentBag<PerformanceCounterCategory> categories;
        private SemaphoreSlim semaphore;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsPerformanceCounterMonitor"/> class.
        /// </summary>
        public WindowsPerformanceCounterMonitor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
            : base(dependencies, parameters)
        {
            this.semaphore = new SemaphoreSlim(1, 1);
            this.categories = new ConcurrentBag<PerformanceCounterCategory>();
            this.Counters = new ConcurrentDictionary<string, WindowsPerformanceCounter>();
            this.Descriptors = new List<CounterDescriptor>();
        }

        /// <summary>
        /// The interval at which the monitor will check the counter categories for
        /// counters. This allows the monitor to pickup new counters that show up after the
        /// application begins running.
        /// </summary>
        public TimeSpan CounterCaptureInterval
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(
                    nameof(this.CounterCaptureInterval), WindowsPerformanceCounterMonitor.DefaultCaptureInterval);
            }
        }

        /// <summary>
        /// The interval at which the monitor will check the counter categories for
        /// counters. This allows the monitor to pickup new counters that show up after the
        /// application begins running.
        /// </summary>
        public TimeSpan CounterDiscoveryInterval
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(
                    nameof(this.CounterDiscoveryInterval), WindowsPerformanceCounterMonitor.DefaultDiscoveryInterval);
            }
        }

        /// <summary>
        /// The list of performance counter categories to capture.
        /// </summary>
        protected IEnumerable<string> Categories
        {
            get
            {
                return this.Descriptors.Select(desc => desc.CategoryName.Trim()).Distinct();
            }
        }

        /// <summary>
        /// The set of counters to capture.
        /// </summary>
        protected IDictionary<string, WindowsPerformanceCounter> Counters { get; }

        /// <summary>
        /// The list of filters used to identify the performance counters to capture.
        /// </summary>
        protected List<CounterDescriptor> Descriptors { get; private set; }

        /// <summary>
        /// Executes the monitor to capture performance counters on specified intervals.
        /// </summary>
        protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            try
            {
                Task counterDiscoveryTask = this.DiscoverCountersAsync(telemetryContext, cancellationToken);
                Task counterCaptureTask = this.CaptureCountersAsync(telemetryContext, cancellationToken);
                Task snapshotTask = this.SnapshotCountersAsync(telemetryContext, cancellationToken);

                await Task.WhenAll(counterDiscoveryTask, counterCaptureTask, snapshotTask);
            }
            catch (OperationCanceledException)
            {
                // Expected when a cancellation request occurs.
            }
        }

        /// <summary>
        /// Background task captures performance counters on an interval.
        /// </summary>
        protected Task CaptureCountersAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                DateTime startTime = DateTime.UtcNow;
                while (!cancellationToken.IsCancellationRequested)
                {
                    await this.semaphore.WaitAsync();

                    try
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            foreach (WindowsPerformanceCounter counter in this.Counters.Values)
                            {
                                try
                                {
                                    counter.Capture();
                                }
                                catch (Exception exc)
                                {
                                    this.Logger.LogMessage($"{this.TypeName}.MetricCaptureError", LogLevel.Warning, telemetryContext.Clone().AddError(exc));
                                }
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // This type of exception will happen if the cancellation token is cancelled
                        // in the Task.Delay() call above. We don't want to surface this as an exception
                        // because it is entirely expected.
                    }
                    catch (Exception exc)
                    {
                        this.Logger.LogMessage($"{this.TypeName}.MetricCaptureError", LogLevel.Warning, telemetryContext.Clone().AddError(exc));
                    }
                    finally
                    {
                        this.semaphore.Release();
                        await Task.Delay(this.CounterCaptureInterval, cancellationToken);
                    }
                }
            });
        }

        /// <summary>
        /// Background task discovers counters on an interval to allow for counters to
        /// become available at different times.
        /// </summary>
        protected Task DiscoverCountersAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await this.semaphore.WaitAsync();

                    try
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            this.LoadCounters(telemetryContext, cancellationToken);
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // This type of exception will happen if the cancellation token is cancelled
                        // in the Task.Delay() call above. We don't want to surface this as an exception
                        // because it is entirely expected.
                    }
                    catch (Exception exc)
                    {
                        this.Logger.LogMessage($"{this.TypeName}.MetricDiscoveryError", LogLevel.Warning, telemetryContext.Clone().AddError(exc));
                    }
                    finally
                    {
                        this.semaphore.Release();
                        await Task.Delay(this.CounterDiscoveryInterval, cancellationToken);
                    }
                }
            });
        }

        /// <summary>
        /// Initializes the monitor before capturing counters.
        /// </summary>
        protected override Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            IEnumerable<CounterDescriptor> descriptors = this.GetCounterDescriptors();
            if (descriptors?.Any() != true)
            {
                throw new MonitorException(
                    $"Required counter descriptions missing. The component does not have any counter descriptions defined. Descriptions are expected to be defined " +
                    $"within the parameters using the format 'Counters1:<category>=<counter_match_expression>, Counters2:<category>=<counter_match_expression>' etc..",
                    ErrorReason.InvalidProfileDefinition);
            }

            this.Descriptors.AddRange(descriptors);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Return true/false whether the monitor is supported on the platform/system. Supported on
        /// Windows platforms only.
        /// </summary>
        protected override bool IsSupported()
        {
            bool isSupported = this.Platform == PlatformID.Win32NT;
            if (isSupported)
            {
                isSupported = base.IsSupported();
            }

            return isSupported;
        }

        /// <summary>
        /// Returns true/false whether the counter specified is supported (and will thus be captured).
        /// </summary>
        /// <param name="categoryName">The performance counter category.</param>
        /// <param name="counterName">The name of the performance counter (e.g. \Processor(_Total)\% Idle Time).</param>
        /// <returns></returns>
        protected bool IsSupportedCounter(string categoryName, string counterName)
        {
            bool isSupported = false;
            foreach (CounterDescriptor descriptor in this.Descriptors.Where(desc => string.Equals(desc.CategoryName, categoryName, StringComparison.OrdinalIgnoreCase)))
            {
                if (descriptor.CounterExpression.IsMatch(counterName))
                {
                    isSupported = true;
                    break;
                }
            }

            return isSupported;
        }

        /// <summary>
        /// Loads the performance counters for the categories provided to the monitor.
        /// </summary>
        /// <exception cref="DependencyException">No performance counter categories were found on the system.</exception>
        protected virtual void LoadCounters(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.categories?.Any() != true)
            {
                IEnumerable<PerformanceCounterCategory> existingCategories = PerformanceCounterCategory.GetCategories()
                    ?.Where(cat => this.Categories.Distinct().Contains(cat.CategoryName, StringComparer.OrdinalIgnoreCase));

                if (existingCategories?.Any() != true)
                {
                    throw new DependencyException(
                        $"Unexpected scenarios. No performance counter categories were found on the system.",
                        ErrorReason.MonitorUnexpectedAnomaly);
                }

                existingCategories.ToList().ForEach(cat => this.categories.Add(cat));
            }

            foreach (PerformanceCounterCategory category in this.categories)
            {
                try
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }

                    switch (category.CategoryType)
                    {
                        case PerformanceCounterCategoryType.MultiInstance:
                            string[] counterInstances = category.GetInstanceNames();
                            if (counterInstances?.Any() == true)
                            {
                                foreach (string instance in counterInstances)
                                {
                                    PerformanceCounter[] instanceCounters = category.GetCounters(instance);
                                    this.AddSupportedCounters(instanceCounters);
                                }
                            }

                            break;

                        default:
                            PerformanceCounter[] counters = category.GetCounters();
                            this.AddSupportedCounters(counters);
                            break;
                    }
                }
                catch (Exception exc)
                {
                    this.Logger.LogMessage($"{this.TypeName}.MetricDiscoveryError", LogLevel.Warning, telemetryContext.Clone().AddError(exc));
                }
            }
        }

        /// <summary>
        /// Background task snapshots the counters on a specified interval to produce
        /// a set of performance counter metrics.
        /// </summary>
        protected Task SnapshotCountersAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(this.MonitorFrequency);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            List<Metric> metrics = new List<Metric>();
                            foreach (WindowsPerformanceCounter counter in this.Counters.Values)
                            {
                                try
                                {
                                    Metric performanceSnapshot = counter.Snapshot();
                                    if (performanceSnapshot != null && performanceSnapshot != Metric.None)
                                    {
                                        metrics.Add(performanceSnapshot);
                                    }
                                }
                                catch (Exception exc)
                                {
                                    this.Logger.LogMessage($"{this.TypeName}.MetricSnapshotError", LogLevel.Warning, telemetryContext.Clone().AddError(exc));
                                }
                            }

                            if (metrics.Any())
                            {
                                this.Logger.LogPerformanceCounters(".NET SDK", metrics, metrics.Min(m => m.StartTime), DateTime.UtcNow, telemetryContext);
                            }
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // This type of exception will happen if the cancellation token is cancelled
                        // in the Task.Delay() call above. We don't want to surface this as an exception
                        // becuase it is entirely expected.
                    }
                    catch (Exception exc)
                    {
                        this.Logger.LogMessage($"{this.TypeName}.MetricSnapshotError", LogLevel.Warning, telemetryContext.Clone().AddError(exc));
                    }
                }
            });
        }

        /// <inheritdoc/>
        protected override void Validate()
        {
            base.Validate();
            if (this.MonitorFrequency <= TimeSpan.Zero)
            {
                throw new MonitorException(
                    $"The monitor frequency defined/provided for the '{this.TypeName}' component '{this.MonitorFrequency}' is not valid. " +
                    $"The frequency must be greater than zero.",
                    ErrorReason.InvalidProfileDefinition);
            }
        }

        private void AddSupportedCounters(IEnumerable<PerformanceCounter> counters)
        {
            if (counters?.Any() == true)
            {
                foreach (PerformanceCounter counter in counters)
                {
                    string counterName = WindowsPerformanceCounter.GetCounterName(counter.CategoryName, counter.CounterName, counter.InstanceName);

                    if (!this.Counters.ContainsKey(counterName) && this.IsSupportedCounter(counter.CategoryName, counterName))
                    {
                        this.Counters[counterName] = new WindowsPerformanceCounter(counter, CaptureStrategy.Average);
                    }
                }
            }
        }

        private IEnumerable<CounterDescriptor> GetCounterDescriptors()
        {
            var descriptors = new List<CounterDescriptor>();
            foreach (var entry in this.Parameters.Where(p => p.Key.StartsWith("Counters")))
            {
                string[] parts = entry.Value?.ToString().Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                if (parts?.Any() == true && parts.Count() == 2)
                {
                    descriptors.Add(new CounterDescriptor(
                        parts[0],
                        new Regex(parts[1], RegexOptions.Compiled | RegexOptions.IgnoreCase)));
                }
            }

            return descriptors;
        }

        /// <summary>
        /// Represents a counter category and match description/expression.
        /// </summary>
        [DebuggerDisplay("Category={CategoryName}, Counter Expression={CounterExpression.ToString()}")]
        public class CounterDescriptor
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CounterDescriptor"/> class.
            /// </summary>
            /// <param name="categoryName"></param>
            /// <param name="counterExpression"></param>
            public CounterDescriptor(string categoryName, Regex counterExpression)
            {
                categoryName.ThrowIfNullOrWhiteSpace(nameof(categoryName));
                counterExpression.ThrowIfNull(nameof(counterExpression));

                this.CategoryName = categoryName;
                this.CounterExpression = counterExpression;
            }

            /// <summary>
            /// The counter category name.
            /// </summary>
            public string CategoryName { get; }

            /// <summary>
            /// The counter match expression.
            /// </summary>
            public Regex CounterExpression { get; }
        }
    }
}
