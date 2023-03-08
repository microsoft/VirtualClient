// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;

    /// <summary>
    /// The base class for all Virtual Client profile actions and monitors.
    /// </summary>
    public abstract class VirtualClientComponent : IDisposable
    {
        /// <summary>
        /// Common delimiters for string-formatted collections "," and ";".
        /// </summary>
        public static readonly char[] CommonDelimiters = new char[] { ',', ';' };

        /// <summary>
        /// The assembly containing the component base class and types.
        /// </summary>
        public static readonly Assembly DllAssembly = Assembly.GetAssembly(typeof(VirtualClientComponent));

        /// <summary>
        /// The executing assembly (e.g. VirtualClient.exe).
        /// </summary>
        public static readonly Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();

        private ISystemInfo systemInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="VirtualClientComponent"/> class.
        /// </summary>
        /// <param name="dependencies">Provides all of the required dependencies to the Virtual Client component.</param>
        /// <param name="parameters">
        /// Parameters defined in the execution profile or supplied to the Virtual Client on the command line.
        /// </param>
        protected VirtualClientComponent(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
        {
            dependencies.ThrowIfNull(nameof(dependencies));
            this.TypeName = this.GetType().Name;

            if (parameters?.Any() == true)
            {
                this.Parameters = new Dictionary<string, IConvertible>(parameters, StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                this.Parameters = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            }

            this.Dependencies = dependencies;
            this.Logger = NullLogger.Instance;

            if (dependencies.TryGetService<ILogger>(out ILogger logger))
            {
                this.Logger = logger;
            }

            if (dependencies.TryGetService<EnvironmentLayout>(out EnvironmentLayout layout))
            {
                this.Layout = layout;
            }

            this.systemInfo = this.Dependencies.GetService<ISystemInfo>();
            this.AgentId = this.systemInfo.AgentId;
            this.ExperimentId = this.systemInfo.ExperimentId;
            this.LogSuccessFailMetrics = true;
            this.PlatformSpecifics = this.systemInfo.PlatformSpecifics;
            this.Platform = this.systemInfo.Platform;
            this.CpuArchitecture = this.systemInfo.CpuArchitecture;
            this.SupportingExecutables = new List<string>();
            this.CleanupTasks = new List<Action>();
        }

        /// <summary>
        /// The ID of the Virtual Client instance/agent as part of the larger experiment.
        /// </summary>
        public string AgentId { get; }

        /// <summary>
        /// The CPU/processor architecture (e.g. amd64, arm).
        /// </summary>
        public Architecture CpuArchitecture { get; }

        /// <summary>
        /// Provides all of the required dependencies to the component.
        /// </summary>
        public IServiceCollection Dependencies { get; set; }

        /// <summary>
        /// Action end time
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// Random execution seed
        /// </summary>
        public int? ExecutionSeed { get; set; }

        /// <summary>
        /// The ID of the larger experiment in which the Virtual Client instance
        /// is participating.
        /// </summary>
        public string ExperimentId { get; }

        /// <summary>
        /// The client environment/topology layout provided to the Virtual Client application.
        /// </summary>
        public EnvironmentLayout Layout { get; }

        /// <summary>
        /// The Logger for this component
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Defines the metric filter as provided in the profile. This defines the list of metrics to include in 
        /// the output of a particular workload executor or monitor. The terms can be partial terms depending
        /// upon how the executor or monitor implements filtering.
        /// </summary>
        public IEnumerable<string> MetricFilters
        {
            get
            {
                IConvertible filters;
                if (!this.Parameters.TryGetValue(nameof(this.MetricFilters), out filters))
                {
                    // Note:
                    // The original profile parameters was called 'MetricFilter'
                    this.Parameters.TryGetValue("MetricFilter", out filters);
                }

                return filters != null
                    ? filters.ToString().Split(VirtualClientComponent.CommonDelimiters, StringSplitOptions.RemoveEmptyEntries)
                    : Array.Empty<string>();
            }

            set
            {
                this.Parameters[nameof(this.MetricFilters)] = string.Join(VirtualClientComponent.CommonDelimiters.First(), value);
            }
        }

        /// <summary>
        /// Defines the name of the package associated with the component.
        /// </summary>
        public string PackageName
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.PackageName), out IConvertible packageName);
                return packageName?.ToString();
            }

            protected set
            {
                this.Parameters[nameof(this.PackageName)] = value;
            }
        }

        /// <summary>
        /// Cycle/Iteration number for whole profile.
        /// </summary>
        public int ProfileIteration
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.ProfileIteration), out IConvertible profileIteration);
                return profileIteration != null ? (int)Convert.ChangeType(profileIteration, typeof(int)) : 0;
            }
        }

        /// <summary>
        /// Profile Cycle/Iteration
        /// </summary>
        public string ProfileIterationStartTime
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.ProfileIterationStartTime), out IConvertible profileIterationStartTime);
                return profileIterationStartTime != null ? profileIterationStartTime.ToString() : string.Empty;
            }
        }

        /// <summary>
        /// Any parameters necessary for this workload action
        /// </summary>
        public IDictionary<string, IConvertible> Parameters { get; internal set; }

        /// <summary>
        /// The OS/system platform (e.g. Windows, Unix).
        /// </summary>
        public PlatformID Platform { get; }

        /// <summary>
        /// Provides OS/system platform specific information.
        /// </summary>
        public PlatformSpecifics PlatformSpecifics { get; }

        /// <summary>
        /// Defines true/false whether profiling is enabled.
        /// </summary>
        public bool ProfilingEnabled
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.ProfilingEnabled), false);
            }

            protected set
            {
                this.Parameters[nameof(this.ProfilingEnabled)] = value;
            }
        }

        /// <summary>
        /// Defines the interval at which the monitor should be profiling. Within each of these intervals, the profiler
        /// will profile for a period of time as specified by the 'ProfilingPeriod' parameter. For example given the interval
        /// is 1 minute and the profiling period is 30 seconds, profiling will happen every 1 minute for 30 seconds.
        /// </summary>
        public TimeSpan ProfilingInterval
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.ProfilingInterval), TimeSpan.Zero);
            }

            protected set
            {
                this.Parameters[nameof(this.ProfilingInterval)] = value.ToString();
            }
        }

        /// <summary>
        /// Returns the mode of profiling operations (e.g. Interval or OnDemand).
        /// </summary>
        public ProfilingMode ProfilingMode
        {
            get
            {
                return this.Parameters.GetEnumValue<ProfilingMode>(nameof(this.ProfilingMode), ProfilingMode.None);
            }

            protected set
            {
                this.Parameters[nameof(this.ProfilingMode)] = value.ToString();
            }
        }

        /// <summary>
        /// Defines the length of time to run profiling operations.
        /// </summary>
        public TimeSpan ProfilingPeriod
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.ProfilingPeriod), TimeSpan.Zero);
            }

            protected set
            {
                this.Parameters[nameof(this.ProfilingPeriod)] = value.ToString();
            }
        }

        /// <summary>
        /// Defines the length of time to wait allowing the system to warm-up before running
        /// profiling operations.
        /// </summary>
        public TimeSpan ProfilingWarmUpPeriod
        {
            get
            {
                return this.Parameters.GetTimeSpanValue(nameof(this.ProfilingWarmUpPeriod), TimeSpan.Zero);
            }

            protected set
            {
                this.Parameters[nameof(this.ProfilingWarmUpPeriod)] = value.ToString();
            }
        }

        /// <summary>
        /// Defines the scenario associated with the profiling request/operations.
        /// </summary>
        public string ProfilingScenario
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.ProfilingScenario), out IConvertible profilingScenario);
                return profilingScenario?.ToString();
            }

            protected set
            {
                this.Parameters[nameof(this.ProfilingScenario)] = value;
            }
        }

        /// <summary>
        /// The roles for which we want to execute the given component.
        /// </summary>
        public IEnumerable<string> Roles
        {
            get
            {
                IEnumerable<string> rolesList = null;
                if (this.Parameters.TryGetValue("Role", out IConvertible roles))
                {
                    rolesList = roles?.ToString().Split(VirtualClientComponent.CommonDelimiters, StringSplitOptions.None);
                }

                return rolesList;
            }
        }

        /// <summary>
        /// The scenario for which the component/executor is related.
        /// </summary>
        public string Scenario
        {
            get
            {
                this.Parameters.TryGetValue(nameof(VirtualClientComponent.Scenario), out IConvertible scenario);
                return scenario?.ToString();
            }

            protected set
            {
                this.Parameters[nameof(this.Scenario)] = value;
            }
        }

        /// <summary>
        /// Action start time
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// The roles that are supported for the executor (e.g. Client, Server). Not all executors support
        /// multi-role scenarios.
        /// </summary>
        public IEnumerable<string> SupportedRoles { get; protected set; }

        /// <summary>
        /// A set of paths for supporting executables of the main process 
        /// (e.g. geekbench_x86_64, geekbench_aarch64). These typically need to 
        /// be cleaned up/terminated at the end of each round of processing.
        /// </summary>
        public List<string> SupportingExecutables { get; private set; }

        /// <summary>
        /// The tags defined in the profile arguments.
        /// </summary>
        public IEnumerable<string> Tags
        {
            get
            {
                this.Parameters.TryGetValue(nameof(VirtualClientComponent.Tags), out IConvertible tags);
                return tags?.ToString().Split(VirtualClientComponent.CommonDelimiters).ToList() ?? new List<string>();
            }
        }

        /// <summary>
        /// Returns the data type name for the component (e.g. GeekbenchExecutor).
        /// </summary>
        public string TypeName { get; protected set; }

        /// <summary>
        /// Cleanup tasks to execute when the component operations complete.
        /// </summary>
        protected IList<Action> CleanupTasks { get; }

        /// <summary>
        /// The toolname or component name to use when logging completion metrics.
        /// </summary>
        protected bool LogSuccessFailMetrics { get; set; }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns true if the component is supported on the current system.
        /// </summary>
        /// <param name="component">The component to validate.</param>
        public static bool IsSupported(VirtualClientComponent component)
        {
            return component.IsSupported();
        }

        /// <summary>
        /// When overriden in a derived class, executes the component logic.
        /// </summary>
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            PlatformSpecifics.ThrowIfNotSupported(this.Platform);
            PlatformSpecifics.ThrowIfNotSupported(this.CpuArchitecture);

            if (this.IsSupported())
            {
                EventContext telemetryContext = EventContext.Persisted().AddParameters(this.Parameters);

                await this.Logger.LogMessageAsync($"{this.TypeName}.Execute", telemetryContext, async () =>
                {
                    bool succeeded = false;
                    DateTime executionStartTime = DateTime.UtcNow;

                    try
                    {
                        this.ValidateParameters();

                        await this.InitializeAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                        await this.ExecuteAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                        succeeded = true;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected for cases where a cancellation token is cancelled.
                    }
                    finally
                    {
                        if (this.LogSuccessFailMetrics)
                        {
                            if (succeeded)
                            {
                                this.LogSuccessMetric(scenarioStartTime: executionStartTime, scenarioEndTime: DateTime.UtcNow);
                            }
                            else
                            {
                                this.LogFailedMetric(scenarioStartTime: executionStartTime, scenarioEndTime: DateTime.UtcNow);
                            }
                        }

                        await this.CleanupAsync(telemetryContext, cancellationToken).ConfigureAwait(false);
                    }
                }, displayErrors: true);
            }
        }

        /// <summary>
        /// Allows any final cleanup work to be performed.
        /// </summary>
        protected virtual Task CleanupAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            if (this.CleanupTasks.Any())
            {
                try
                {
                    foreach (Action cleanupTask in this.CleanupTasks)
                    {
                        try
                        {
                            cleanupTask.Invoke();
                        }
                        catch (Exception exc)
                        {
                            // Best effort...but logged
                            this.Logger.LogMessage($"{this.TypeName}.CleanupError", LogLevel.Warning, telemetryContext.Clone().AddError(exc));
                        }
                    }
                }
                catch (Exception exc)
                {
                    // Best effort...but logged
                    this.Logger.LogMessage($"{this.TypeName}.CleanupError", LogLevel.Warning, telemetryContext.Clone().AddError(exc));
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// When overriden in a derived class, executes the component logic.
        /// </summary>
        /// <param name="telemetryContext">Provides context information that will be captured with telemetry events.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operation.</param>
        protected abstract Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken);

        /// <summary>
        /// Enables the component to setup dependencies required for operation.
        /// </summary>
        protected virtual Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns true if the local Virtual Client instance is in the role specified
        /// and that matches a client instance from the environment layout.
        /// </summary>
        /// <param name="role">The role to check (e.g. Client, Server).</param>
        /// <returns>
        /// True if the client is in the role specified and has a matching client instance (by IP address)
        /// in the environment layout. False if not.
        /// </returns>
        protected virtual bool IsInRole(string role)
        {
            bool inRole = false;
            if (this.Layout != null)
            {
                IEnumerable<ClientInstance> clientInstances = this.GetLayoutClientInstances(role);
                inRole = clientInstances.FirstOrDefault(client => this.IsMe(client)
                    && string.Equals(this.AgentId, client.Name, StringComparison.OrdinalIgnoreCase)) != null;
            }

            return inRole;
        }

        /// <summary>
        /// Returns true if the component is supported on the current system, platform and architecture. This
        /// determines whether it will be executed.
        /// </summary>
        /// <returns>True if component should be executed, false if not.</returns>
        protected virtual bool IsSupported()
        {
            bool shouldExecute = true;

            // Execution Criteria
            // 1) If there are no roles defined for the component, then it executes.
            // 2) If there is just 1 instance in layout, then it executes
            // 3) If there are roles defined, the environment layout defines a role for each of the
            //    instances and the roles match the client instance role, then it executes.
            // 4) If not #1 or #2 or #3 fails, the component does not execute.
            //
            // If there are roles defined and this is a multi-role environment layout
            // scenario, we check to see if this instance of the Virtual Client is targeted
            // for at least 1 of the roles.
            if (this.Layout?.Clients?.Count() >= 2 && this.Roles?.Any() == true)
            {
                ClientInstance clientInstance = this.GetLayoutClientInstance(this.AgentId, throwIfNotExists: false);
                if (clientInstance != null && !string.IsNullOrWhiteSpace(clientInstance.Role))
                {
                    shouldExecute = this.Roles.Contains(clientInstance.Role, StringComparer.OrdinalIgnoreCase);
                }
            }

            return shouldExecute;
        }

        /// <summary>
        /// Allows components to validate the parameters provided.
        /// </summary>
        protected virtual void ValidateParameters()
        {
        }

        /// <summary>
        /// Causes the process to idle until the operations are cancelled.
        /// </summary>
        /// <param name="cancellationToken">A token that can be used to cancel the wait operation.</param>
        protected virtual Task WaitAsync(CancellationToken cancellationToken)
        {
            return this.systemInfo.WaitAsync(cancellationToken);
        }

        /// <summary>
        /// Causes the process to idle until the time defined by the timeout or until the operations
        /// are cancelled.
        /// </summary>
        /// <param name="timeout">The date/time at which the wait ends and execution should continue.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the wait operation.</param>
        protected virtual Task WaitAsync(DateTime timeout, CancellationToken cancellationToken)
        {
            return this.systemInfo.WaitAsync(timeout, cancellationToken);
        }

        /// <summary>
        /// Causes the process to idle for the period of time defined by the timeout or until the operations
        /// are cancelled.
        /// </summary>
        /// <param name="timeout">The maximum time to wait before continuing.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the wait operation.</param>
        protected virtual Task WaitAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            return this.systemInfo.WaitAsync(timeout, cancellationToken);
        }

        private bool IsMe(ClientInstance clientInstance)
        {
            bool isMatch = false;
            if (this.Layout != null)
            {
                if (this.systemInfo.IsLocalIPAddress(clientInstance.IPAddress))
                {
                    isMatch = true;
                }
            }

            return isMatch;
        }
    }
}