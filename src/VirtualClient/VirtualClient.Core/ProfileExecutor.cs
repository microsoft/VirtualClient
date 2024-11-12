// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts.Extensions;
    using VirtualClient.Contracts.Metadata;

    /// <summary>
    /// This is the main class that will take an execution profile and execute it
    /// </summary>
    public class ProfileExecutor : IDisposable
    {
        private static int currentIteration;
        private MetadataContract metadataContract;
        private bool disposed;
        
        /// <summary>
        /// Constructs an instance of a profile executor, the main class that will execute a profile
        /// </summary>
        /// <param name="profile">The profile to execute.</param>
        /// <param name="dependencies">Shared platform dependencies to pass along to individual components in the profile.</param>
        /// <param name="scenarios">A specific set of profile scenarios to execute or to exclude.</param>
        /// <param name="logger">A logger to use for capturing telemetry.</param>
        public ProfileExecutor(ExecutionProfile profile, IServiceCollection dependencies, IEnumerable<string> scenarios = null, ILogger logger = null)
        {
            profile.ThrowIfNull(nameof(profile));
            dependencies.ThrowIfNull(nameof(dependencies));

            this.Profile = profile;
            this.Dependencies = dependencies;
            this.Scenarios = scenarios;
            this.ExecuteActions = true;
            this.ExecuteMonitors = true;
            this.ExecuteDependencies = true;
            this.ExitWait = TimeSpan.FromSeconds(10);
            this.RandomizationSeed = 777;
            this.Logger = logger ?? NullLogger.Instance;

            this.metadataContract = new MetadataContract();
            this.ExecutionMinimumInterval = profile.MinimumExecutionInterval;
        }

        /// <summary>
        /// Event handler is invoked before an individual action is executed.
        /// </summary>
        public event EventHandler<ComponentEventArgs> ActionBegin;

        /// <summary>
        /// Event handler is invoked after an individual action is executed.
        /// </summary>
        public event EventHandler<ComponentEventArgs> ActionEnd;

        /// <summary>
        /// Event handler is invoked just before the profile executor begins exiting.
        /// </summary>
        public event EventHandler BeforeExiting;

        /// <summary>
        /// Event handler is invoked after a component (action, monitor or dependency handler) is
        /// created.
        /// </summary>
        public event EventHandler<ComponentEventArgs> ComponentCreated;

        /// <summary>
        /// Event handler is invoked before each round of all actions are executed.
        /// </summary>
        public event EventHandler IterationBegin;

        /// <summary>
        /// Event handler is invoked at the end of each round of all actions being
        /// executed.
        /// </summary>
        public event EventHandler IterationEnd;

        /// <summary>
        /// 
        /// </summary>
        public static int CurrentIteration
        {
            get
            {
                return currentIteration;
            }
        }

        /// <summary>
        /// Shared platform dependencies to pass along to individual components in the profile.
        /// </summary>
        public IServiceCollection Dependencies { get; }

        /// <summary>
        /// True if the actions defined in the profile should be executed. Default = true.
        /// </summary>
        public bool ExecuteActions { get; set; }

        /// <summary>
        /// True if the dependencies defined in the profile should be executed/installed. Default = true.
        /// </summary>
        public bool ExecuteDependencies { get; set; }

        /// <summary>
        /// Defines an interval at which the execution of actions will be gated.
        /// This is sometimes referred to as the "minimum execution interval". If individual
        /// actions complete before this interval of time, the profile will wait for the next
        /// interval before executing the next action.
        /// </summary>
        public TimeSpan? ExecutionMinimumInterval { get; set; }

        /// <summary>
        /// True if the monitors defined in the profile should be executed. Default = true.
        /// </summary>
        public bool ExecuteMonitors { get; set; }

        /// <summary>
        /// Defines an explicit time for which the application will wait before exiting. This is correlated with
        /// the exit/flush wait supplied by the user on the command line.
        /// </summary>
        public TimeSpan ExitWait { get; set; }

        /// <summary>
        /// True if VC should exit/crash on first/any error(s) regardless of 
        /// their severity. Default = false.
        /// </summary>
        public bool FailFast { get; set; }

        /// <summary>
        /// Logs things to various sources
        /// </summary>
        public ILogger Logger { get; }

        /// <summary>
        /// True if VC should log output to file.
        /// </summary>
        public bool LogToFile { get; set; }

        /// <summary>
        /// The profile to execute.
        /// </summary>
        public ExecutionProfile Profile { get; }

        /// <summary>
        /// A seed to use with profile actions to ensure consistency.
        /// </summary>
        public int RandomizationSeed { get; set; }

        /// <summary>
        /// A set of scenarios to execute from the workload profile (vs. the entire
        /// profile).
        /// </summary>
        public IEnumerable<string> Scenarios { get; }

        /// <summary>
        /// The set of actions to execute as defined in the profile.
        /// </summary>
        protected IEnumerable<VirtualClientComponent> ProfileActions { get; set; }

        /// <summary>
        /// The set of dependencies to install as as defined in the profile.
        /// </summary>
        protected IEnumerable<VirtualClientComponent> ProfileDependencies { get; set; }

        /// <summary>
        /// The set of monitors to run as defined in the profile.
        /// </summary>
        protected IEnumerable<VirtualClientComponent> ProfileMonitors { get; set; }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Executes the profile.
        /// </summary>
        /// <param name="timing">Defines the timing/timeout constraints for the profile execution.</param>
        /// <param name="cancellationToken">A token that can be used to cancel the operations.</param>
        public async Task ExecuteAsync(ProfileTiming timing, CancellationToken cancellationToken)
        {
            timing.ThrowIfNull(nameof(timing));

            using (CancellationTokenSource tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                CancellationToken profileCancellationToken = tokenSource.Token;
                if (!profileCancellationToken.IsCancellationRequested)
                {
                    if (this.Profile.Metadata?.Any() == true)
                    {
                        VirtualClientRuntime.Metadata.AddRange(this.Profile.Metadata, true);
                    }

                    // The parent context is created when the profile operations start up. We can use the
                    // activity ID of the parent to enable correlation of events all the way down the
                    // callstack.
                    EventContext parentContext = EventContext.Persist(Guid.NewGuid());
                    this.metadataContract.Apply(parentContext);

                    this.Initialize();

                    if (this.ExecuteDependencies)
                    {
                        await this.InstallDependenciesAsync(parentContext, profileCancellationToken);
                    }

                    if (VirtualClientRuntime.IsRebootRequested)
                    {
                        return;
                    }

                    if (this.ExecuteActions || this.ExecuteMonitors)
                    {
                        // Keep a watch on the executing timing to ensure the application times out when
                        // the timing constraints are met.
                        Task timeoutTask = timing.MonitorTimeoutAsync(this, cancellationToken);

                        Task profileMonitorsTask = Task.CompletedTask;
                        if (this.ExecuteMonitors)
                        {
                            profileMonitorsTask = this.ExecuteMonitorsAsync(parentContext, profileCancellationToken);
                        }

                        Task profileActionsTask = Task.CompletedTask;
                        if (this.ExecuteActions)
                        {
                            profileActionsTask = this.ExecuteActionsAsync(timing, parentContext, profileCancellationToken);
                        }

                        // We have to watch for requests to reboot. VC components "request" a reboot but do not
                        // actually make the call to reboot. We avoid this to allow VC to make a graceful exit best
                        // ensuring that all telemetry is captured/emitted beforehand.
                        Task rebootRequestedTask = Task.Run(async () =>
                        {
                            while (!cancellationToken.IsCancellationRequested && !VirtualClientRuntime.IsRebootRequested)
                            {
                                await Task.Delay(2000);
                            }
                        });

                        if (this.ProfileActions?.Any() == true)
                        {
                            // Wait for any workload execution actions to complete. Exit if the timeout supplied on
                            // the command line passes or a reboot is requested. If there are no actions defined, this
                            // task will complete immediately.
                            await Task.WhenAny(profileActionsTask, timeoutTask, rebootRequestedTask);

                            if (!profileActionsTask.IsFaulted)
                            {
                                // Wait for any background monitors to complete. Exit if the timeout supplied on
                                // the command line passes or a reboot is requested. If there are no monitors defined
                                // this task will complete immediately.
                                await Task.WhenAny(profileMonitorsTask, timeoutTask, rebootRequestedTask);
                            }
                        }
                        else if (this.ProfileMonitors?.Any() == true)
                        {
                            // Wait for any background monitors to complete. Exit if the timeout supplied on
                            // the command line passes or a reboot is requested. If there are no monitors defined
                            // this task will complete immediately.
                            await Task.WhenAny(profileMonitorsTask, timeoutTask, rebootRequestedTask);
                        }

                        // If we timeout or a reboot is requested, we will request all background processes to cancel/exit.
                        await tokenSource.CancelAsync();

                        // We allow the user to supply an instruction on the command line to force the application
                        // to wait for an explicit/longer period of time before exiting. This allows for actions, monitors
                        // and telemetry flushes more time to complete.
                        this.BeforeExiting?.Invoke(this, new EventArgs());

                        ConsoleLogger.Default.LogInformation("Profile: Wait for Exit...");

                        // Attempt to allow a graceful exit for any background tasks, actions or monitors
                        // that are running. There is an exit wait grace period before we will definitively exit.
                        await Task.WhenAny(Task.WhenAll(profileActionsTask, profileMonitorsTask), Task.Delay(this.ExitWait));

                        ConsoleLogger.Default.LogInformation("Profile: Exited");
                        profileActionsTask.ThrowIfErrored();
                        profileMonitorsTask.ThrowIfErrored();
                    }
                }
            }
        }

        /// <summary>
        /// Disposes of resources used by the instance.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.disposed)
                {
                    if (this.ProfileActions?.Any() == true)
                    {
                        this.ProfileActions.ToList().ForEach(a => a.Dispose());
                    }

                    if (this.ProfileDependencies?.Any() == true)
                    {
                        this.ProfileDependencies.ToList().ForEach(a => a.Dispose());
                    }

                    if (this.ProfileMonitors?.Any() == true)
                    {
                        this.ProfileMonitors.ToList().ForEach(m => m.Dispose());
                    }
                }

                this.disposed = true;
            }
        }

        /// <summary>
        /// Executes actions defined in the profile.
        /// </summary>
        protected Task ExecuteActionsAsync(ProfileTiming timing, EventContext parentContext, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                if (this.ProfileActions?.Any() == true)
                {
                    ConsoleLogger.Default.LogInformation("Profile: Execute Actions");

                    DateTime? nextRoundOfExecution = null;
                    bool isFirstAction = true;

                    while (!cancellationToken.IsCancellationRequested && !timing.IsTimedOut)
                    {
                        try
                        {
                            // Note:
                            // Any component can request a system reboot. The system reboot itself is handled just before the Virtual Client
                            // application itself exits to ensure all telemetry is captured before reboot.
                            if (VirtualClientRuntime.IsRebootRequested)
                            {
                                break;
                            }

                            Interlocked.Increment(ref currentIteration);

                            DateTime startTime = DateTime.UtcNow;
                            this.IterationBegin?.Invoke(this, new EventArgs());

                            // Program Startup - ActivityID = Correlation ID 1
                            //
                            // Round/Iteration 1
                            // ---------------------------------
                            // Profile Executor Iteration 1 - ActivityID = Correlation ID 2, Parent Activity ID = Correlation ID 1
                            // Action 1 - ActivityID = Correlation ID 4, Parent Activity ID = Correlation ID 2
                            // Action 2 - ActivityID = Correlation ID 5, Parent Activity ID = Correlation ID 2
                            // Action 3 - ActivityID = Correlation ID 6, Parent Activity ID = Correlation ID 2
                            //
                            // Round/Iteration 2
                            // ---------------------------------
                            // Profile Executor Iteration 2 - ActivityID = Correlation ID 3, Parent Activity ID = Correlation ID 1
                            // Action 1 - ActivityID = Correlation ID 7, Parent Activity ID = Correlation ID 2
                            // Action 2 - ActivityID = Correlation ID 8, Parent Activity ID = Correlation ID 2

                            EventContext actionExecutionContext = EventContext.Persist(Guid.NewGuid(), parentContext.ActivityId)
                                .AddContext("timing", timing)
                                .AddContext("iteration", currentIteration)
                                .AddContext("components", this.ProfileActions.Select(d => new
                                {
                                    type = d.TypeName,
                                    parameters = d.Parameters?.ObscureSecrets()
                                }));

                            if (this.ExecutionMinimumInterval != null)
                            {
                                nextRoundOfExecution = DateTime.UtcNow.Add(this.ExecutionMinimumInterval.Value);
                                actionExecutionContext.AddContext("iterationNextExecutionTime", nextRoundOfExecution);
                            }

                            await this.Logger.LogMessageAsync($"{nameof(ProfileExecutor)}.ExecuteActions", LogLevel.Debug, actionExecutionContext, async () =>
                            {
                                // When we have one of the actions fail, we do not want to reset the workflow. We will move
                                // forward with the next action.
                                for (int i = 0; i < this.ProfileActions.Count(); i++)
                                {
                                    // Note:
                                    // Any component can request a system reboot. The system reboot itself is handled just before the Virtual Client
                                    // application itself exits to ensure all telemetry is captured before reboot.
                                    if (VirtualClientRuntime.IsRebootRequested)
                                    {
                                        break;
                                    }

                                    if (timing.IsTimedOut)
                                    {
                                        break;
                                    }

                                    if (!isFirstAction && nextRoundOfExecution != null)
                                    {
                                        while (!timing.IsTimedOut && DateTime.UtcNow < nextRoundOfExecution)
                                        {
                                            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken)
                                                .ConfigureAwait(false);
                                        }
                                    }

                                    isFirstAction = false;

                                    if (!cancellationToken.IsCancellationRequested)
                                    {
                                        VirtualClientComponent action = null;

                                        try
                                        {
                                            // The context persisted here will be picked up by the individual component. This allows
                                            // the telemetry for each round of execution of components to be correlated together while
                                            // also being correlated with each round of profile actions processing.
                                            EventContext.Persist(Guid.NewGuid(), actionExecutionContext.ActivityId);

                                            action = this.ProfileActions.ElementAt(i);
                                            action.Parameters[nameof(VirtualClientComponent.ProfileIteration)] = currentIteration;
                                            action.Parameters[nameof(VirtualClientComponent.ProfileIterationStartTime)] = startTime;
                                            
                                            this.ActionBegin?.Invoke(this, new ComponentEventArgs(action));

                                            try
                                            {
                                                ProfileExecutor.OutputComponentStart("Action", action);
                                                await action.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                                            }
                                            finally
                                            {
                                                this.ActionEnd?.Invoke(this, new ComponentEventArgs(action));
                                            }
                                        }
                                        catch (VirtualClientException exc) when ((int)exc.Reason >= 500 || this.FailFast || action?.FailFast == true)
                                        {
                                            // Error reasons have numeric/integer values that indicate their severity. Error reasons
                                            // with a value >= 500 are terminal situations where the workload cannot run successfully
                                            // regardless of how many times we attempt it.
                                            throw;
                                        }
                                        catch (VirtualClientException)
                                        {
                                            // Exceptions with error reasons < 500 are potentially transient issues. We do not want to 
                                            // cause VC to exit in these cases but to give it a chance to retry the logic that failed on
                                            // subsequent rounds of processing.
                                            //
                                            // Exceptions having error reasons with a value between 400 - 499 are serious errors but they represent
                                            // issues that may be transient and that can be resolve after a period of time. When
                                            // we catch these type of errors, we may want to reset and start over in the test workflow.
                                        }
                                        catch (MissingMemberException exc)
                                        {
                                            throw new DependencyException(
                                                "Assembly/.dll mismatch. This can occur when an extensions assembly exists in the application directory that was compiled " +
                                                "against a version of the Virtual Client that has breaking changes. Verify the version of the extensions assemblies in the " +
                                                "application directory can be used with the current application version. Valid extensions assemblies typically have the same " +
                                                "major version as the application.",
                                                exc,
                                                ErrorReason.ExtensionAssemblyInvalid);
                                        }
                                    }
                                }
                            }).ConfigureAwait(false);
                        }
                        finally
                        {
                            this.IterationEnd?.Invoke(this, new EventArgs());
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Executes monitors defined in the profile.
        /// </summary>
        protected Task ExecuteMonitorsAsync(EventContext parentContext, CancellationToken cancellationToken)
        {
            if (!this.ExecuteMonitors || this.ProfileMonitors?.Any() != true)
            {
                return Task.CompletedTask;
            }

            List<Task> monitoringTasks = new List<Task>();
            ConsoleLogger.Default.LogInformation("Profile: Execute Monitors");

            EventContext monitorExecutionContext = EventContext.Persist(Guid.NewGuid(), parentContext.ActivityId)
                .AddContext("components", this.ProfileMonitors.Select(d => new
                {
                    type = d.TypeName,
                    parameters = d.Parameters?.ObscureSecrets()
                }));

            return this.Logger.LogMessageAsync($"{nameof(ProfileExecutor)}.ExecuteMonitors", LogLevel.Debug, monitorExecutionContext, async () =>
            {
                foreach (VirtualClientComponent monitor in this.ProfileMonitors)
                {
                    try
                    {
                        // Note:
                        // Any component can request a system reboot. The system reboot itself is handled just before the Virtual Client
                        // application itself exits to ensure all telemetry is captured before reboot.
                        if (VirtualClientRuntime.IsRebootRequested)
                        {
                            break;
                        }

                        // The context persisted here will be picked up by the individual component. This allows
                        // the telemetry for each round of execution of components to be correlated together while
                        // also being correlated with the parent context defined at the beginning of the profile execution.
                        EventContext.Persist(Guid.NewGuid(), parentContext.ActivityId);

                        if (!cancellationToken.IsCancellationRequested)
                        {
                            ProfileExecutor.OutputComponentStart("Monitor", monitor);
                            monitoringTasks.Add(monitor.ExecuteAsync(cancellationToken));
                        }
                    }
                    catch (VirtualClientException)
                    {
                        throw;
                    }
                    catch (MissingMemberException exc)
                    {
                        throw new DependencyException(
                            "Assembly/.dll mismatch. This can occur when an extensions assembly exists in the application directory that was compiled " +
                            "against a version of the Virtual Client that has breaking changes. Verify the version of the extensions assemblies in the " +
                            "application directory can be used with the current application version. Valid extensions assemblies typically have the same " +
                            "major version as the application.",
                            exc,
                            ErrorReason.ExtensionAssemblyInvalid);
                    }
                    catch (Exception exc)
                    {
                        // Error reasons have numeric/integer values that indicate their severity. Error reasons
                        // with a value >= 500 are terminal situations where the workload cannot run successfully
                        // regardless of how many times we attempt it.
                        throw new MonitorException(
                            $"Monitor execution failed for component '{monitor.TypeName}'.",
                            exc,
                            ErrorReason.DependencyInstallationFailed);
                    }
                }

                await Task.WhenAll(monitoringTasks);
            });
        }

        /// <summary>
        /// Initializes the profile dependencies, actions and monitors for execution.
        /// </summary>
        protected void Initialize()
        {
            ConsoleLogger.Default.LogInformation("Profile: Initialize");

            IEnumerable<string> includedScenarios = null;
            IEnumerable<string> excludedScenarios = null;

            if (this.Scenarios?.Any() == true)
            {
                includedScenarios = this.Scenarios.Where(sc => !sc.Trim().StartsWith("-"));
                excludedScenarios = this.Scenarios.Where(sc => sc.Trim().StartsWith("-"));
            }

            // You can only 'exclude' scenarios for Dependencies and Monitors. Actions support both includes and
            // excludes.
            if (this.ExecuteDependencies)
            {
                this.ProfileDependencies = this.CreateComponents(this.Profile.Dependencies, excludeScenarios: excludedScenarios);
            }

            if (this.ExecuteActions)
            {
                this.ProfileActions = this.CreateComponents(this.Profile.Actions, includeScenarios: includedScenarios, excludeScenarios: excludedScenarios);
            }

            if (this.ExecuteMonitors)
            {
                this.ProfileMonitors = this.CreateComponents(this.Profile.Monitors, excludeScenarios: excludedScenarios);
            }
        }

        /// <summary>
        /// Installs dependencies defined in the profile.
        /// </summary>
        protected async Task InstallDependenciesAsync(EventContext parentContext, CancellationToken cancellationToken)
        {
            if (this.ExecuteDependencies && this.ProfileDependencies?.Any() == true)
            {
                ConsoleLogger.Default.LogInformation("Profile: Install Dependencies");

                EventContext dependencyInstallationContext = EventContext.Persist(Guid.NewGuid(), parentContext.ActivityId)
                    .AddContext("components", this.ProfileDependencies.Select(d => new
                    {
                        type = d.TypeName,
                        parameters = d.Parameters?.ObscureSecrets()
                    }));

                await this.Logger.LogMessageAsync($"{nameof(ProfileExecutor)}.InstallDependencies", LogLevel.Debug, dependencyInstallationContext, async () =>
                {
                    foreach (VirtualClientComponent dependency in this.ProfileDependencies)
                    {
                        if (!cancellationToken.IsCancellationRequested && !VirtualClientRuntime.IsRebootRequested)
                        {
                            try
                            {
                                // The context persisted here will be picked up by the individual component. This allows
                                // the telemetry for each round of execution of components to be correlated together while
                                // also being correlated with the parent context defined at the beginning of the profile execution.
                                EventContext.Persist(Guid.NewGuid(), parentContext.ActivityId);

                                ProfileExecutor.OutputComponentStart("Dependency", dependency);
                                await dependency.ExecuteAsync(cancellationToken).ConfigureAwait(false);
                            }
                            catch (VirtualClientException)
                            {
                                // Error reasons have numeric/integer values that indicate their severity. Error reasons
                                // with a value >= 500 are terminal situations where the workload cannot run successfully
                                // regardless of how many times we attempt it.
                                throw;
                            }
                            catch (MissingMemberException exc)
                            {
                                throw new DependencyException(
                                    "Assembly/.dll mismatch. This can occur when an extensions assembly exists in the application directory that was compiled " +
                                    "against a version of the Virtual Client that has breaking changes. Verify the version of the extensions assemblies in the " +
                                    "application directory can be used with the current application version. Valid extensions assemblies typically have the same " +
                                    "major version as the application.",
                                    exc,
                                    ErrorReason.ExtensionAssemblyInvalid);
                            }
                            catch (Exception exc)
                            {
                                // Error reasons have numeric/integer values that indicate their severity. Error reasons
                                // with a value >= 500 are terminal situations where the workload cannot run successfully
                                // regardless of how many times we attempt it.
                                throw new DependencyException(
                                    $"Dependency installation failed for component '{dependency.TypeName}'.",
                                    exc,
                                    ErrorReason.DependencyInstallationFailed);
                            }
                        }
                    }
                });
            }
        }

        private static void OutputComponentStart(string componentType, VirtualClientComponent component)
        {
            VirtualClientComponentCollection componentCollection = component as VirtualClientComponentCollection;
            if (componentCollection != null)
            {
                foreach (VirtualClientComponent subComponent in componentCollection)
                {
                    if (!string.IsNullOrWhiteSpace(component.Scenario))
                    {
                        ConsoleLogger.Default.LogInformation($"Profile: Parallel {componentType} = {subComponent.TypeName} (scenario={subComponent.Scenario})");
                    }
                    else
                    {
                        ConsoleLogger.Default.LogInformation($"Profile: Parallel {componentType} = {subComponent.TypeName}");
                    }
                }
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(component.Scenario))
                {
                    ConsoleLogger.Default.LogInformation($"Profile: {componentType} = {component.TypeName} (scenario={component.Scenario})");
                }
                else
                {
                    ConsoleLogger.Default.LogInformation($"Profile: {componentType} = {component.TypeName}");
                }
            }
        }

        private List<VirtualClientComponent> CreateComponents(
            IEnumerable<ExecutionProfileElement> profileComponents, IEnumerable<string> includeScenarios = null, IEnumerable<string> excludeScenarios = null)
        {
            List<VirtualClientComponent> components = new List<VirtualClientComponent>();

            if (profileComponents?.Any() == true)
            {
                foreach (ExecutionProfileElement component in profileComponents)
                {
                    // Components may be included based upon the user having defined scenarios on the command
                    // line. The user can define a scenario as "included" or "excluded". Excluded scenarios will
                    // be supplied in the format -{scenario_name} (e.g. -RunWorkload). Not that this is supported
                    // for Actions and Monitors only.
                    bool scenarioIncluded = includeScenarios?.Any() == true;
                    if (includeScenarios?.Any() == true)
                    {
                        scenarioIncluded = component.IsTargetedScenario(includeScenarios);
                        if (!scenarioIncluded)
                        {
                            continue;
                        }
                    }

                    // Included scenarios take precedence over excluded (e.g. Scenario1,-Scenario1 -> Scenario1 will be included).
                    if (!scenarioIncluded && excludeScenarios?.Any() == true && component.IsExcludedScenario(excludeScenarios))
                    {
                        continue;
                    }

                    if (ComponentTypeCache.Instance.TryGetComponentType(component.Type, out Type componentType))
                    {
                        bool executeComponent = true;
                        VirtualClientComponent runtimeComponent = ComponentFactory.CreateComponent(component, this.Dependencies, this.RandomizationSeed);
                        runtimeComponent.FailFast = this.FailFast;
                        runtimeComponent.LogToFile = this.LogToFile;

                        // Global metadata. Supplied on the command line.
                        //
                        // e.g.
                        // VirtualClient.exe --profile=PERF-CPU-OPENSSL.json --metadata="Meta1=Value1,,,Meta2=1234"
                        if (VirtualClientRuntime.Metadata?.Any() == true)
                        {
                            runtimeComponent.Metadata.AddRange(
                                VirtualClientRuntime.Metadata.Select(entry => new KeyValuePair<string, IConvertible>(entry.Key.CamelCased(), entry.Value)),
                                true);
                        }

                        // Profile Component-level metadata. Defined in the individual component within the profile (overrides global).
                        //
                        // e.g.
                        // {
                        //    "Type": "GeekbenchExecutor",
                        //    "Metadata": {
                        //        "ComponentMetadata1": 98765,
                        //        "ComponentMetadata2": true,
                        //        "ComponentMetadata3": "ValueX"
                        //    },
                        //    "Parameters": {
                        //        "Scenario": "ExecuteGeekBench6Benchmark",
                        //        "CommandLine": "--no-upload",
                        //        "PackageName": "geekbench6"
                        //    }
                        // }
                        if (component.Metadata?.Any() == true)
                        {
                            runtimeComponent.Metadata.AddRange(component.Metadata, true);
                        }

                        if (!VirtualClientComponent.IsSupported(runtimeComponent))
                        {
                            executeComponent = false;
                        }

                        if (executeComponent)
                        {
                            // Profile component-level extensions metadata. Defined in the individual component within the profile.
                            //
                            // e.g.
                            // {
                            //    "Type": "GeekbenchExecutor",
                            //    "Parameters": {
                            //        "Scenario": "ExecuteGeekBench6Benchmark",
                            //        "CommandLine": "--no-upload",
                            //        "PackageName": "geekbench6"
                            //    },
                            //    "ExtensionsSection": {
                            //        "CustomValue": "Custom1",
                            //        "CustomList": [ "Item1", "Item2", "Item3" ],
                            //        "CustomDictionary": {
                            //            "Key1": "Value1",
                            //            "Key2": 445566
                            //        }
                            //    }
                            // }
                            if (component.Extensions?.Any() == true)
                            {
                                runtimeComponent.Extensions.AddRange(component.Extensions, withReplace: true);
                            }

                            components.Add(runtimeComponent);
                            this.ComponentCreated?.Invoke(this, new ComponentEventArgs(runtimeComponent));
                        }
                    }
                    else
                    {
                        throw new TypeLoadException(
                            $"Invalid profile definition. The component '{component.Type}' is not a valid workload profile component " +
                            $"because it does not exist or does not inherit from '{nameof(VirtualClientComponent)}'. If this type is defined in an extensions " +
                            $"assembly, ensure the assembly/.dll exists in the same directory as the Virtual Client executable itself and is the assembly/.dll is " +
                            $"itself compiled with the required assembly-level attribute '{typeof(VirtualClientComponentAssemblyAttribute)}'.");
                    }
                }
            }

            return components;
        }
    }
}