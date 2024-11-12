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
    using Microsoft.CodeAnalysis;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Newtonsoft.Json.Linq;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts.Metadata;

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

        private const string Role = "Role";
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

            this.systemInfo = dependencies.GetService<ISystemInfo>();
            this.AgentId = this.systemInfo.AgentId;
            this.CpuArchitecture = this.systemInfo.CpuArchitecture;
            this.Dependencies = dependencies;
            this.ExperimentId = this.systemInfo.ExperimentId;
            this.Logger = NullLogger.Instance;
            this.Metadata = new Dictionary<string, IConvertible>(StringComparer.OrdinalIgnoreCase);
            this.MetadataContract = new MetadataContract();
            this.PlatformSpecifics = this.systemInfo.PlatformSpecifics;
            this.Platform = this.systemInfo.Platform;
            this.SupportingExecutables = new List<string>();
            this.CleanupTasks = new List<Action>();
            this.Extensions = new Dictionary<string, JToken>();

            if (dependencies.TryGetService<ILogger>(out ILogger logger))
            {
                this.Logger = logger;
            }

            if (dependencies.TryGetService<EnvironmentLayout>(out EnvironmentLayout layout))
            {
                if (this.Roles?.Any() != true)
                {
                    // Backwards Compatibility:
                    // Add in the roles from the layout if they are defined within it.
                    ClientInstance clientInstance = this.GetLayoutClientInstance(throwIfNotExists: false);

                    if (clientInstance != null && !string.IsNullOrWhiteSpace(clientInstance.Role))
                    {
                        this.Parameters[VirtualClientComponent.Role] = clientInstance.Role;
                    }
                }
            }
        }

        /// <summary>
        /// Parameter defines the content path template to use when uploading content
        /// to target storage resources. When not defined the default template will be used.
        /// </summary>
        public static string ContentPathTemplate { get; set; }

        /// <summary>
        /// The ID of the Virtual Client instance/agent as part of the larger experiment.
        /// </summary>
        public string AgentId { get; }

        /// <summary>
        /// Cleanup tasks to execute when the component operations complete.
        /// </summary>
        public IList<Action> CleanupTasks { get; }

        /// <summary>
        /// Defines a client request ID to associate with the component operations. This information
        /// is used to correlate client-side operations with corresponding server-side operations
        /// (e.g. network client/server scenarios).
        /// </summary>
        public Guid? ClientRequestId
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.ClientRequestId), out IConvertible clientRequestId);
                return clientRequestId != null ? Guid.Parse(clientRequestId.ToString()) : null;
            }

            set
            {
                if (value == null)
                {
                    this.Parameters.Remove(nameof(this.ClientRequestId));
                }
                else
                {
                    this.Parameters[nameof(this.ClientRequestId)] = value.ToString();
                }
            }
        }

        /// <summary>
        /// The CPU/processor architecture (e.g. amd64, arm).
        /// </summary>
        public Architecture CpuArchitecture { get; }

        /// <summary>
        /// Provides all of the required dependencies to the component.
        /// </summary>
        public IServiceCollection Dependencies { get; set; }

        /// <summary>
        /// Component end time
        /// </summary>
        public DateTime EndTime { get; private set; }

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
        /// Extensions defined in the profile component.
        /// </summary>
        public IDictionary<string, JToken> Extensions { get; }

        /// <summary>
        /// True if VC should exit/crash on first/any error(s) regardless of 
        /// their severity. Default = false.
        /// </summary>
        public bool FailFast 
        {
            get
            {
                return this.Parameters.GetValue<bool>(nameof(this.FailFast), false);
            }

            set
            {
                this.Parameters[nameof(this.FailFast)] = value;
            }
        }

        /// <summary>
        /// The client environment/topology layout provided to the Virtual Client application.
        /// </summary>
        public EnvironmentLayout Layout
        {
            get
            {
                this.Dependencies.TryGetService<EnvironmentLayout>(out EnvironmentLayout layout);
                return layout;
            }
        }

        /// <summary>
        /// The Logger for this component
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// True/false whether the output of processes should be logged to files 
        /// in the logs directory.
        /// </summary>
        public bool LogToFile { get; set; }

        /// <summary>
        /// Metadata provided to the application on the command line.
        /// </summary>
        public IDictionary<string, IConvertible> Metadata { get; }

        /// <summary>
        /// Metadata to add to the "standard data contract" in the telemetry
        /// emitted by the application.
        /// </summary>
        public MetadataContract MetadataContract { get; }

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
        /// Defines the name/description to use for metrics scenario (e.g. fio_randwrite_496GB_4k_d64_th16).
        /// </summary>
        public string MetricScenario
        {
            get
            {
                this.Parameters.TryGetValue(nameof(this.MetricScenario), out IConvertible scenario);
                return scenario?.ToString();
            }

            protected set
            {
                this.Parameters[nameof(this.MetricScenario)] = value;
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
        /// Any parameters necessary for this workload action
        /// </summary>
        public IDictionary<string, IConvertible> Parameters { get; internal set; }

        /// <summary>
        /// True/false whether the parameters have been evaluated. Parameter evaluation allows
        /// placeholders and well-known terms to be replaced in the values of the parameters before
        /// execution of workloads, monitors or dependencies.
        /// </summary>
        public bool ParametersEvaluated { get; internal set; }

        /// <summary>
        /// The OS/system platform (e.g. Windows, Unix).
        /// </summary>
        public PlatformID Platform { get; }

        /// <summary>
        /// Provides OS/system platform specific information.
        /// </summary>
        public PlatformSpecifics PlatformSpecifics { get; }

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
                IConvertible roles;

                if (this.Parameters.TryGetValue("Role", out roles) || this.Parameters.TryGetValue(nameof(this.Roles), out roles))
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
        public DateTime StartTime { get; private set; }

        /// <summary>
        /// Parameter describes the platform/architectures for which the component is supported.
        /// </summary>
        public IEnumerable<string> SupportedPlatforms
        {
            get
            {
                if (!this.Parameters.TryGetCollection<string>(nameof(this.SupportedPlatforms), out IEnumerable<string> platforms))
                {
                    // Backwards compatibility.
                    this.Parameters.TryGetCollection<string>("Platforms", out platforms);
                }

                return platforms ?? Array.Empty<string>();
            }
        }

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
        /// The name of the platform/architecture for the system on which the application is
        /// running (e.g. linux-x64, linux-arm64, win-x64, win-arm64).
        /// </summary>
        protected string PlatformArchitectureName
        {
            get
            {
                return this.PlatformSpecifics.PlatformArchitectureName;
            }
        }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns true if the component is supported on the current platform.
        /// </summary>
        /// <param name="component">The component to validate.</param>
        public static bool IsSupported(VirtualClientComponent component)
        {
            SupportedPlatformsAttribute attribute = (SupportedPlatformsAttribute)component.GetType().GetCustomAttribute(typeof(SupportedPlatformsAttribute), true);
            bool platformSupported = true;
            if (attribute != null)
            {
                platformSupported = attribute.CompatiblePlatforms.Contains(component.PlatformArchitectureName);

                if (!platformSupported)
                {
                    component.Logger.LogNotSupported(component.GetType().Name, component.Platform, component.CpuArchitecture, EventContext.Persisted());

                    if (attribute.ThrowError)
                    {
                        throw new PlatformNotSupportedException($"'{component.GetType().Name}' is not supported on current platform '{component.PlatformArchitectureName}'." +
                        $"Supported platforms are '{string.Join(',', attribute.CompatiblePlatforms)}'.");
                    }
                }
            }

            return platformSupported && component.IsSupported();
        }

        /// <summary>
        /// When overriden in a derived class, executes the component logic.
        /// </summary>
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            this.StartTime = DateTime.UtcNow;

            try
            {
                PlatformSpecifics.ThrowIfNotSupported(this.Platform);
                PlatformSpecifics.ThrowIfNotSupported(this.CpuArchitecture);

                if (this.IsSupported())
                {
                    EventContext telemetryContext = EventContext.Persisted();

                    if (this.ClientRequestId != null)
                    {
                        telemetryContext.AddClientRequestId(this.ClientRequestId);
                    }

                    if (!this.ParametersEvaluated)
                    {
                        await this.EvaluateParametersAsync(cancellationToken);
                    }

                    if (this.Metadata?.Any() == true)
                    {
                        this.MetadataContract.Add(
                            this.Metadata.Keys.ToDictionary(key => key, entry => this.Metadata[entry] as object).ObscureSecrets(),
                            MetadataContractCategory.Default,
                            replace: true);
                    }

                    if (this.Parameters?.Any() == true)
                    {
                        this.MetadataContract.Add(
                            this.Parameters.Keys.ToDictionary(key => key, entry => this.Parameters[entry] as object).ObscureSecrets(),
                            MetadataContractCategory.Scenario,
                            replace: true);
                    }

                    // Extensions allow profile authors/developers to add extensions to components in the profile.
                    // These extensions may be purely informational or may allow the developer to define objects that
                    // can be deserialized into usable objects at runtime within the component logic.
                    if (this.Extensions?.Any() == true)
                    {
                        foreach (var entry in this.Extensions)
                        {
                            this.MetadataContract.Add(
                                this.Extensions.Keys.ToDictionary(key => key, entry => this.Extensions[entry] as object),
                                MetadataContractCategory.ScenarioExtensions,
                                replace: true);
                        }
                    }

                    this.MetadataContract.Apply(telemetryContext);

                    await this.Logger.LogMessageAsync($"{this.TypeName}.Execute", telemetryContext, async () =>
                    {
                        bool succeeded = false;

                        try
                        {
                            await this.InitializeAsync(telemetryContext, cancellationToken);
                            this.Validate();

                            await this.ExecuteAsync(telemetryContext, cancellationToken);
                            succeeded = true;
                        }
                        catch (OperationCanceledException)
                        {
                            // Expected for cases where a cancellation token is cancelled.
                        }
                        catch (Exception)
                        {
                            // Occasionally some of the workloads throw exceptions right as VC receives a
                            // cancellation/exit request.
                            if (!cancellationToken.IsCancellationRequested)
                            {
                                throw;
                            }
                        }
                        finally
                        {
                            this.EndTime = DateTime.UtcNow;

                            if (succeeded)
                            {
                                this.LogSuccessMetric(scenarioStartTime: this.StartTime, scenarioEndTime: this.EndTime);
                            }
                            else
                            {
                                this.LogFailedMetric(scenarioStartTime: this.StartTime, scenarioEndTime: this.EndTime);
                            }
                        }

                        await this.CleanupAsync(telemetryContext, cancellationToken);

                    });
                }
            }
            catch
            {
                this.EndTime = DateTime.UtcNow;
                throw;
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
            bool isSupported = true;

            // We execute only if the current platform/architecture matches those
            // defined in the parameters.
            if (this.SupportedPlatforms?.Any() == true && !this.SupportedPlatforms.Contains(this.PlatformArchitectureName))
            {
                isSupported = false;
            }
            else if (this.Layout?.Clients?.Count() >= 2 && this.Roles?.Any() == true)
            {
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

                ClientInstance clientInstance = this.GetLayoutClientInstance(this.AgentId, throwIfNotExists: false);
                if (clientInstance != null && !string.IsNullOrWhiteSpace(clientInstance.Role))
                {
                    isSupported = this.Roles.Contains(clientInstance.Role, StringComparer.OrdinalIgnoreCase);
                }
            }

            return isSupported;
        }

        /// <summary>
        /// Allows components to validate it can be executed.
        /// </summary>
        protected virtual void Validate()
        {
            // For derived component classes to implement.
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