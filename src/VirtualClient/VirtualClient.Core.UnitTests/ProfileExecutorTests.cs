// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using MathNet.Numerics.LinearAlgebra.Solvers;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Extensions;

    [TestFixture]
    [Category("Unit")]
    public class ProfileExecutorTests
    {
        private MockFixture mockFixture;
        private ExecutionProfile mockProfile;

        [OneTimeSetUp]
        public void InitializeFixture()
        {
            ComponentTypeCache.Instance.LoadComponentTypes(MockFixture.TestAssemblyDirectory);
        }

        [SetUp]
        public void SetupDefaults()
        {
            this.mockFixture = new MockFixture();

            // Note that the TestExecutor and TestMonitor classes are in the VirtualClient.TestFramework
            // project that is at the foundation of all unit + functional tests in the solution.
            this.mockProfile = new ExecutionProfile(
                description: "Any profile description",
                minimumExecutionInterval: TimeSpan.FromMicroseconds(10),
                actions: new List<ExecutionProfileElement>
                {
                    new ExecutionProfileElement(
                        "VirtualClient.TestExecutor",
                        new Dictionary<string, IConvertible> { ["Scenario"] = "Scenario1" }),
                    new ExecutionProfileElement(
                        "VirtualClient.TestExecutor",
                        new Dictionary<string, IConvertible> { ["Scenario"] = "Scenario2" }),
                    new ExecutionProfileElement(
                        "VirtualClient.TestCollectionExecutor",
                        new Dictionary<string, IConvertible> { ["Scenario"] = "Scenario3" },
                        components: new List<ExecutionProfileElement>()
                        {
                            new ExecutionProfileElement(
                                "VirtualClient.TestExecutor",
                                new Dictionary<string, IConvertible> { ["Scenario"] = "Scenario1" }),
                            new ExecutionProfileElement(
                                "VirtualClient.TestExecutor",
                                new Dictionary<string, IConvertible> { ["Scenario"] = "Scenario2" }),
                            new ExecutionProfileElement(
                                "VirtualClient.TestExecutor",
                                new Dictionary<string, IConvertible> { ["Scenario"] = "Scenario3" }),
                            new ExecutionProfileElement(
                                "VirtualClient.TestExecutor",
                                new Dictionary<string, IConvertible> { ["Scenario"] = "Scenario4" })
                        })
                },
                dependencies: new List<ExecutionProfileElement>
                {
                    new ExecutionProfileElement(
                        "VirtualClient.TestDependency",
                        new Dictionary<string, IConvertible> { ["Scenario"] = "DependencyScenario1", ["SomeParameter"] = "Value1" }),
                    new ExecutionProfileElement(
                        "VirtualClient.TestDependency",
                        new Dictionary<string, IConvertible> { ["Scenario"] = "DependencyScenario2", ["SomeParameter"] = "Value2" })
                },
                monitors: new List<ExecutionProfileElement>
                {
                    new ExecutionProfileElement(
                        "VirtualClient.TestMonitor",
                        new Dictionary<string, IConvertible> { ["Scenario"] = "MonitorScenario1", ["SomeOtherParameter"] = "Value3" }),
                    new ExecutionProfileElement(
                        "VirtualClient.TestMonitor",
                        new Dictionary<string, IConvertible> { ["Scenario"] = "MonitorScenario2", ["SomeOtherParameter"] = "Value4" })
                },
                metadata: new Dictionary<string, IConvertible>
                {
                    { "Metadata1", 1234 },
                    { "Metadata2", true }
                },
                parameters: new Dictionary<string, IConvertible>
                {
                    { "Parameter1", 9876 },
                    { "Parameter2", false }
                });
        }

        [Test]
        public void ProfileExecutorCreatesTheExpectedWorkloadActionExecutorsAsDefinedInTheProfile()
        {
            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies))
            {
                executor.Initialize();

                Assert.IsNotEmpty(executor.ProfileActions);
                Assert.IsTrue(executor.ProfileActions.Count() == 3);
                Assert.IsTrue(executor.ProfileActions.Where(action => action.GetType() == typeof(TestExecutor)).Count() == 2);
                Assert.IsTrue(executor.ProfileActions.Where(action => action.GetType() == typeof(TestCollectionExecutor)).Count() == 1);
                Assert.AreEqual("Scenario1", executor.ProfileActions.ElementAt(0).Parameters["Scenario"]);
                Assert.AreEqual("Scenario2", executor.ProfileActions.ElementAt(1).Parameters["Scenario"]);
                Assert.AreEqual("Scenario3", executor.ProfileActions.ElementAt(2).Parameters["Scenario"]);
            }
        }

        [Test]
        public void ProfileExecutorCreatesTheExpectedWorkloadDependencyExecutorsAsDefinedInTheProfile()
        {
            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies))
            {
                executor.Initialize();

                Assert.IsNotEmpty(executor.ProfileDependencies);
                Assert.IsTrue(executor.ProfileDependencies.Count() == 2);
                Assert.IsTrue(executor.ProfileDependencies.All(dependency => dependency.GetType() == typeof(TestDependency)));
                Assert.AreEqual("Value1", executor.ProfileDependencies.ElementAt(0).Parameters["SomeParameter"]);
                Assert.AreEqual("Value2", executor.ProfileDependencies.ElementAt(1).Parameters["SomeParameter"]);
            }
        }

        [Test]
        public void ProfileExecutorInstallsDependenciesOnlyWhenInstructed()
        {
            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies))
            {
                executor.ExecuteActions = false;
                executor.ExecuteMonitors = false;
                executor.ExecuteDependencies = true;
                executor.Initialize();

                Assert.IsNull(executor.ProfileActions);
                Assert.IsNull(executor.ProfileMonitors);
                Assert.IsNotEmpty(executor.ProfileDependencies);
                Assert.IsTrue(executor.ProfileDependencies.Count() == 2);
                Assert.IsTrue(executor.ProfileDependencies.All(dependency => dependency.GetType() == typeof(TestDependency)));
                Assert.AreEqual("Value1", executor.ProfileDependencies.ElementAt(0).Parameters["SomeParameter"]);
                Assert.AreEqual("Value2", executor.ProfileDependencies.ElementAt(1).Parameters["SomeParameter"]);
            }
        }

        [Test]
        public void ProfileExecutorCreatesTheExpectedWorkloadMonitorExecutorsAsDefinedInTheProfile()
        {
            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies))
            {
                executor.Initialize();

                Assert.IsNotEmpty(executor.ProfileMonitors);
                Assert.IsTrue(executor.ProfileMonitors.Count() == 2);
                Assert.IsTrue(executor.ProfileMonitors.All(mon => mon.GetType() == typeof(TestMonitor)));
                Assert.AreEqual("Value3", executor.ProfileMonitors.ElementAt(0).Parameters["SomeOtherParameter"]);
                Assert.AreEqual("Value4", executor.ProfileMonitors.ElementAt(1).Parameters["SomeOtherParameter"]);
            }
        }

        [Test]
        public async Task ProfileExecutorSupportsProfilesThatHaveOnlyMonitorsDefinedInThem()
        {
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource())
            {
                using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies, logger: this.mockFixture.Logger))
                {
                    executor.ExecuteActions = false;
                    executor.ExecuteDependencies = false;

                    List<VirtualClientComponent> monitorsRan = new List<VirtualClientComponent>();
                    executor.ComponentCreated += (sender, args) =>
                    {
                        TestMonitor monitor = args.Component as TestMonitor;
                        if (monitor != null)
                        {
                            monitor.OnExecute += (context, token) =>
                            {
                                Task.Delay(1000).GetAwaiter().GetResult();
                                monitorsRan.Add(monitor);
                            };
                        }
                    };

                    await executor.ExecuteAsync(new ProfileTiming(profileIterations: 5), cancellationTokenSource.Token)
                        .ConfigureAwait(false);

                    var monitorsStarted = this.mockFixture.Logger.MessagesLogged("TestMonitor.ExecuteStart");
                    var monitorsCompleted = this.mockFixture.Logger.MessagesLogged("TestMonitor.ExecuteStop");

                    Assert.IsNotEmpty(monitorsStarted);
                    Assert.IsNotEmpty(monitorsCompleted);
                    Assert.AreEqual(monitorsStarted.Count(), monitorsCompleted.Count());
                    CollectionAssert.AreEquivalent(executor.ProfileMonitors, monitorsRan);
                }
            }
        }

        [Test]
        public void ProfileExecutorSupportsUserSpecifiedScenarios_Subsets_Of_Scenarios()
        {
            List<string> targetScenarios = new List<string>()
            {
                "Scenario2",
                "Scenario3"
            };

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies, targetScenarios))
            {
                executor.Initialize();

                Assert.IsNotEmpty(executor.ProfileActions);
                Assert.IsTrue(executor.ProfileActions.Count() == 2);

                CollectionAssert.AreEquivalent(
                    this.mockProfile.Actions.Skip(1).Select(a => a.Parameters["Scenario"]),
                    executor.ProfileActions.Select(a => a.Parameters["Scenario"]));

                // Assert child components honor the scenario values.
                VirtualClientComponentCollection collectionComponent = executor.ProfileActions.First(action => action.GetType() == typeof(TestCollectionExecutor)) as VirtualClientComponentCollection;
                Assert.IsNotNull(collectionComponent);
                Assert.AreEqual(2, collectionComponent.Count);
                Assert.IsTrue(collectionComponent.All(component => targetScenarios.Contains(component.Parameters["Scenario"])));
            }
        }

        [Test]
        public void ProfileExecutorSupportsUserSpecifiedScenarios_Components_Have_Duplicate_ScenarioNames()
        {
            List<string> targetScenarios = new List<string>()
            {
                "Scenario1"
            };

            // Ensure we have components that share the same scenario name.
            this.mockProfile.Actions.Take(3).ToList().ForEach(a => a.Parameters["Scenario"] = "Scenario1");
            this.mockProfile.Actions.Last().Components.ToList().ForEach(a => a.Parameters["Scenario"] = "Scenario1");

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies, targetScenarios))
            {
                executor.Initialize();

                Assert.IsNotEmpty(executor.ProfileActions);
                Assert.IsTrue(executor.ProfileActions.Count() == 3);

                CollectionAssert.AreEquivalent(
                    this.mockProfile.Actions.Take(3).Select(a => a.Parameters["Scenario"]),
                    executor.ProfileActions.Select(a => a.Parameters["Scenario"]));

                // Assert child components honor the scenario values.
                VirtualClientComponentCollection collectionComponent = executor.ProfileActions.First(action => action.GetType() == typeof(TestCollectionExecutor)) as VirtualClientComponentCollection;
                Assert.IsNotNull(collectionComponent);
                Assert.AreEqual(4, collectionComponent.Count);
                Assert.IsTrue(collectionComponent.All(component => targetScenarios.Contains(component.Parameters["Scenario"])));
            }
        }

        [Test]
        public void ProfileExecutorSupportsUserSpecifiedScenarios_Non_Existent_Scenarios()
        {
            List<string> targetScenarios = new List<string>()
            {
                "ScenarioDoesNotExist"
            };

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies, targetScenarios))
            {
                executor.Initialize();

                Assert.IsEmpty(executor.ProfileActions);
            }
        }

        [Test]
        public void ProfileExecutorSupportsUserSpecifiedScenarioExclusionForActions()
        {
            List<string> excludedScenarios = new List<string>()
            {
                "-Scenario2",
                "-Scenario3"
            };

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies, excludedScenarios))
            {
                executor.Initialize();

                Assert.IsNotEmpty(executor.ProfileActions);
                Assert.IsTrue(executor.ProfileActions.Count() == 1);

                CollectionAssert.AreEquivalent(
                    this.mockProfile.Actions.Take(1).Select(a => a.Parameters["Scenario"]),
                    executor.ProfileActions.Select(a => a.Parameters["Scenario"]));
            }
        }

        [Test]
        public void ProfileExecutorSupportsUserSpecifiedScenarioExclusionForActionsAndChildComponents()
        {
            List<string> excludedScenarios = new List<string>()
            {
                "-Scenario1",
                "-Scenario2"
            };

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies, excludedScenarios))
            {
                executor.Initialize();

                Assert.IsNotEmpty(executor.ProfileActions);
                Assert.IsTrue(executor.ProfileActions.Count() == 1);

                CollectionAssert.AreEquivalent(
                    this.mockProfile.Actions.Skip(2).Take(1).Select(a => a.Parameters["Scenario"]),
                    executor.ProfileActions.Select(a => a.Parameters["Scenario"]));

                // Assert child components honor the scenario values.
                VirtualClientComponentCollection collectionComponent = executor.ProfileActions.First(action => action.GetType() == typeof(TestCollectionExecutor)) as VirtualClientComponentCollection;
                Assert.IsNotNull(collectionComponent);
                Assert.AreEqual(2, collectionComponent.Count);

                Assert.IsTrue(collectionComponent.First().Parameters["Scenario"].ToString() == "Scenario3");
                Assert.IsTrue(collectionComponent.Last().Parameters["Scenario"].ToString() == "Scenario4");
            }
        }

        [Test]
        public void ProfileExecutorSupportsUsingSpecifiedInclusionsForSpecificChildComponents()
        {
            List<string> includedScenarios = new List<string>()
            {
                "Scenario3",
                "Scenario4"
            };

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies, includedScenarios))
            {
                executor.Initialize();

                Assert.IsNotEmpty(executor.ProfileActions);
                Assert.IsTrue(executor.ProfileActions.Count() == 1);

                CollectionAssert.AreEquivalent(
                    this.mockProfile.Actions.Skip(2).Take(1).Select(a => a.Parameters["Scenario"]),
                    executor.ProfileActions.Select(a => a.Parameters["Scenario"]));

                // Assert child components honor the scenario values.
                VirtualClientComponentCollection collectionComponent = executor.ProfileActions.First(action => action.GetType() == typeof(TestCollectionExecutor)) as VirtualClientComponentCollection;
                Assert.IsNotNull(collectionComponent);
                Assert.AreEqual(2, collectionComponent.Count);

                Assert.IsTrue(collectionComponent.First().Parameters["Scenario"].ToString() == "Scenario3");
                Assert.IsTrue(collectionComponent.Last().Parameters["Scenario"].ToString() == "Scenario4");
            }
        }

        [Test]
        public void ProfileExecutorSupportsUsingSpecifiedExclusionsForSpecificChildComponents()
        {
            List<string> includedScenarios = new List<string>()
            {
                "-Scenario4"
            };

            this.mockProfile.Actions.Last().Parameters.Remove("Scenario");

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies, includedScenarios))
            {
                executor.Initialize();

                Assert.IsNotEmpty(executor.ProfileActions);
                Assert.IsTrue(executor.ProfileActions.Count() == 3);

                // Assert child components honor the scenario values.
                VirtualClientComponentCollection collectionComponent = executor.ProfileActions.First(action => action.GetType() == typeof(TestCollectionExecutor)) as VirtualClientComponentCollection;
                Assert.IsNotNull(collectionComponent);
                Assert.AreEqual(3, collectionComponent.Count);

                Assert.IsTrue(collectionComponent.All(component => component.Parameters["Scenario"].ToString() != "Scenario4"));
            }
        }

        [Test]
        public void ProfileExecutorSupportsUserSpecifiedScenarioExclusionForDependencies()
        {
            List<string> excludedScenarios = new List<string>()
            {
                "-DependencyScenario1"
            };

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies, excludedScenarios))
            {
                executor.Initialize();

                Assert.IsNotEmpty(executor.ProfileActions);
                Assert.IsTrue(executor.ProfileActions.Count() == 3);
                Assert.IsTrue(executor.ProfileDependencies.Count() == 1);
                Assert.IsTrue(executor.ProfileMonitors.Count() == 2);

                CollectionAssert.AreEquivalent(
                    this.mockProfile.Actions.Select(a => a.Parameters["Scenario"]),
                    executor.ProfileActions.Select(a => a.Parameters["Scenario"]),
                    $"None of the actions should be excluded.");

                CollectionAssert.AreEquivalent(
                    this.mockProfile.Monitors.Select(m => m.Parameters["Scenario"]),
                    executor.ProfileMonitors.Select(m => m.Parameters["Scenario"]),
                    $"None of the monitors should be excluded.");

                CollectionAssert.AreEquivalent(
                    this.mockProfile.Dependencies.Skip(1).Select(d => d.Parameters["Scenario"]),
                    executor.ProfileDependencies.Select(d => d.Parameters["Scenario"]));
            }
        }

        [Test]
        public void ProfileExecutorSupportsUserSpecifiedScenarioExclusionForMonitors()
        {
            List<string> excludedScenarios = new List<string>()
            {
                "-MonitorScenario1"
            };

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies, excludedScenarios))
            {
                executor.Initialize();

                Assert.IsNotEmpty(executor.ProfileActions);
                Assert.IsTrue(executor.ProfileActions.Count() == 3);
                Assert.IsTrue(executor.ProfileDependencies.Count() == 2);
                Assert.IsTrue(executor.ProfileMonitors.Count() == 1);

                CollectionAssert.AreEquivalent(
                    this.mockProfile.Actions.Select(a => a.Parameters["Scenario"]),
                    executor.ProfileActions.Select(a => a.Parameters["Scenario"]),
                    $"None of the actions should be excluded.");

                CollectionAssert.AreEquivalent(
                    this.mockProfile.Dependencies.Select(d => d.Parameters["Scenario"]),
                    executor.ProfileDependencies.Select(d => d.Parameters["Scenario"]),
                    $"None of the dependencies should be excluded.");

                CollectionAssert.AreEquivalent(
                    this.mockProfile.Monitors.Skip(1).Select(m => m.Parameters["Scenario"]),
                    executor.ProfileMonitors.Select(m => m.Parameters["Scenario"]),
                    $"None of the monitors should be excluded.");
            }
        }

        [Test]
        public void ProfileExecutorPrioritizesScenarioIncludesOverExcludes()
        {
            List<string> scenarios = new List<string>()
            {
                "Scenario1",
                "-Scenario1"
            };

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies, scenarios))
            {
                executor.Initialize();

                Assert.IsNotEmpty(executor.ProfileActions);
                Assert.IsTrue(executor.ProfileActions.Count() == 1);

                CollectionAssert.AreEquivalent(
                    this.mockProfile.Actions.Take(1).Select(a => a.Parameters["Scenario"]),
                    executor.ProfileActions.Select(a => a.Parameters["Scenario"]));
            }
        }

        [Test]
        public async Task ProfileExecutorHandlesExecutionTimingInstructionsCorrectly_ExplicitTimeoutScenario()
        {
            // Scenario:
            // An explicit timeout is provided to the profile executor.
            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies))
            {
                ProfileTiming explicitTimeout = new ProfileTiming(TimeSpan.FromMicroseconds(50));
                Task executionTask = executor.ExecuteAsync(explicitTimeout, CancellationToken.None);

                DateTime testTimeout = DateTime.UtcNow.AddMilliseconds(50);
                while (!executionTask.IsCompleted)
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }

                executionTask.ThrowIfErrored();

                Assert.IsTrue(executionTask.IsCompleted);
                Assert.IsTrue(explicitTimeout.IsTimedOut);
            }
        }

        [Test]
        public async Task ProfileExecutorHandlesExecutionTimingInstructionsCorrectly_ExplicitNumberIterationsScenario()
        {
            // Scenario:
            // An explicit number of profile iterations is provided to the profile executor.

            int iterationsStarted = 0;
            int iterationsCompleted = 0;
            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies))
            {
                executor.IterationBegin += (sender, args) => iterationsStarted++;
                executor.IterationEnd += (sender, args) => iterationsCompleted++;

                ProfileTiming explicitIterations = new ProfileTiming(profileIterations: 3);
                Task executionTask = executor.ExecuteAsync(explicitIterations, CancellationToken.None);

                DateTime testTimeout = DateTime.UtcNow.AddSeconds(10);
                while (!executionTask.IsCompleted)
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }

                executionTask.ThrowIfErrored();

                Assert.IsTrue(executionTask.IsCompleted);
                Assert.IsTrue(explicitIterations.IsTimedOut);
                Assert.IsTrue(iterationsStarted == 3);
                Assert.IsTrue(iterationsCompleted == 3);
            }
        }

        [Test]
        [TestCase(0)]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(500)]
        [TestCase(1000)]
        public async Task ProfileExecutorHandlesExecutionTimingInstructionsCorrectly_DeterministicScenario1(int timeoutMilliseconds)
        {
            // Scenario:
            // A timeout is supplied along with a request for deterministic behavior. In this scenario, the 
            // executor will exit on timeout but ONLY after the current action is completed.

            int iterationsStarted = 0;
            int iterationsCompleted = 0;
            int actionsStarted = 0;
            int actionsCompleted = 0;

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies))
            {
                executor.ActionBegin += (sender, args) => actionsStarted++;
                executor.ActionEnd += (sender, args) => actionsCompleted++;
                executor.IterationBegin += (sender, args) => iterationsStarted++;
                executor.IterationEnd += (sender, args) => iterationsCompleted++;

                ProfileTiming timing = new ProfileTiming(TimeSpan.FromMilliseconds(timeoutMilliseconds), DeterminismScope.IndividualAction);
                Task executionTask = executor.ExecuteAsync(timing, CancellationToken.None);

                DateTime testTimeout = DateTime.UtcNow.AddSeconds(5);
                while (!executionTask.IsCompleted)
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }

                executionTask.ThrowIfErrored();

                Assert.IsTrue(executionTask.IsCompleted);
                Assert.IsTrue(timing.IsTimedOut);
                Assert.IsFalse(iterationsStarted <= 0);
                Assert.IsFalse(actionsStarted <= 0);
                Assert.AreEqual(iterationsStarted, iterationsCompleted);
                Assert.AreEqual(actionsStarted, actionsCompleted);
            }
        }

        [Test]
        [TestCase(0)]
        [TestCase(10)]
        [TestCase(50)]
        [TestCase(100)]
        [TestCase(200)]
        [TestCase(500)]
        [TestCase(1000)]
        public async Task ProfileExecutorHandlesExecutionTimingInstructionsCorrectly_DeterministicScenario2(int timeoutMilliseconds)
        {
            // Scenario:
            // A timeout is supplied along with a request for deterministic behavior. In this scenario, the 
            // executor will exit on timeout but ONLY after the current action is completed.

            int iterationsStarted = 0;
            int iterationsCompleted = 0;
            int actionsStarted = 0;
            int actionsCompleted = 0;

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies))
            {
                executor.ActionBegin += (sender, args) => actionsStarted++;
                executor.ActionEnd += (sender, args) => actionsCompleted++;
                executor.IterationBegin += (sender, args) => iterationsStarted++;
                executor.IterationEnd += (sender, args) => iterationsCompleted++;

                ProfileTiming timing = new ProfileTiming(TimeSpan.FromMilliseconds(timeoutMilliseconds), DeterminismScope.AllActions);
                Task executionTask = executor.ExecuteAsync(timing, CancellationToken.None);

                DateTime testTimeout = DateTime.UtcNow.AddSeconds(5);
                while (!executionTask.IsCompleted)
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }

                executionTask.ThrowIfErrored();

                Assert.IsTrue(executionTask.IsCompleted);
                Assert.IsTrue(timing.IsTimedOut);
                Assert.IsFalse(iterationsStarted <= 0);
                Assert.IsFalse(actionsStarted <= 0);
                Assert.AreEqual(iterationsStarted, iterationsCompleted);
                Assert.AreEqual(actionsStarted, actionsCompleted);
                Assert.AreEqual(actionsCompleted / this.mockProfile.Actions.Count, iterationsCompleted);
            }
        }

        [Test]
        public async Task ProfileExecutorCorrelationIdentifiersAreCorrectForActionsExecutedThroughoutTheProfileOperations()
        {
            // Expected Correlation Semantics:
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
            // Action 3 - ActivityID = Correlation ID 9, Parent Activity ID = Correlation ID 2

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies, logger: this.mockFixture.Logger))
            {
                executor.ExecuteActions = true;
                executor.ExecuteDependencies = false;
                executor.ExecuteMonitors = false;

                Task executionTask = executor.ExecuteAsync(ProfileTiming.Iterations(2), CancellationToken.None);

                DateTime testTimeout = DateTime.UtcNow.AddSeconds(5);
                while (!executionTask.IsCompleted)
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }

                executionTask.ThrowIfErrored();

                var eventsLogged = this.mockFixture.Logger.Where(log => log.Item2.Name.StartsWith("TestExecutor"));
                Assert.IsNotNull(eventsLogged);
                Assert.IsNotEmpty(eventsLogged);

                var iterations = this.mockFixture.Logger.Where(log => log.Item2.Name == "ProfileExecutor.ExecuteActionsStart")?.Select(i => i.Item3 as EventContext);
                Assert.IsNotNull(iterations);
                Assert.IsNotEmpty(iterations);
                Assert.IsTrue(iterations.Count() == 2);

                var singleActions = this.mockFixture.Logger.Where(log => log.Item2.Name == "TestExecutor.ExecuteStart").Select(a => a.Item3 as EventContext);
                Assert.IsNotNull(singleActions);
                Assert.IsNotEmpty(singleActions);
                Assert.AreEqual(4, singleActions.Count());

                var collectionActions = this.mockFixture.Logger.Where(log => log.Item2.Name == "TestCollectionExecutor.ExecuteStart").Select(a => a.Item3 as EventContext);
                Assert.IsNotNull(collectionActions);
                Assert.IsNotEmpty(collectionActions);
                Assert.AreEqual(2, collectionActions.Count());

                var actions = singleActions.Union(collectionActions);
                // First round of actions should have the same parent ID but each action should have its
                // own unique activity ID.
                var iteration1Actions = actions.Where(a => a.ParentActivityId == iterations.ElementAt(0).ActivityId);
                Assert.IsNotNull(iteration1Actions);
                Assert.IsTrue(iteration1Actions.Count() == 3);
                Assert.IsTrue(iteration1Actions.Select(a => a.ParentActivityId).Distinct().Count() == 1);
                Assert.IsTrue(iteration1Actions.Select(a => a.ActivityId).Distinct().Count() == 3);
                Assert.IsEmpty(iteration1Actions.Select(a => a.ActivityId).Intersect(iterations.Select(i => i.ActivityId)));

                // Second round of actions should have the same parent ID but each action should have its
                // own unique activity ID.
                var iteration2Actions = actions.Where(a => a.ParentActivityId == iterations.ElementAt(1).ActivityId);
                Assert.IsNotNull(iteration2Actions);
                Assert.IsTrue(iteration2Actions.Count() == 3);
                Assert.IsTrue(iteration2Actions.Select(a => a.ParentActivityId).Distinct().Count() == 1);
                Assert.IsTrue(iteration2Actions.Select(a => a.ActivityId).Distinct().Count() == 3);
                Assert.IsEmpty(iteration2Actions.Select(a => a.ActivityId).Intersect(iterations.Select(i => i.ActivityId)));

                // None of the actions should have the same activity ID
                Assert.IsTrue(actions.Select(a => a.ActivityId).Distinct().Count() == actions.Count());
            }
        }

        [Test]
        public async Task ProfileExecutorCorrelationIdentifiersAreCorrectForDependenciesExecutedThroughoutTheProfileOperations()
        {
            // Expected Correlation Semantics:
            // Program Startup - ActivityID = Correlation ID 1
            //
            // Monitors running in the background
            // ---------------------------------
            // Monitor 1 - ActivityID = Correlation ID 2, Parent Activity ID = Correlation ID 1
            // Monitor 2 - ActivityID = Correlation ID 3, Parent Activity ID = Correlation ID 1

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies, logger: this.mockFixture.Logger))
            {
                executor.ExecuteActions = false;
                executor.ExecuteDependencies = true;
                executor.ExecuteMonitors = false;

                Task executionTask = executor.ExecuteAsync(ProfileTiming.Iterations(1), CancellationToken.None);

                DateTime testTimeout = DateTime.UtcNow.AddSeconds(5);
                while (!executionTask.IsCompleted)
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }

                executionTask.ThrowIfErrored();

                var eventsLogged = this.mockFixture.Logger.Where(log => log.Item2.Name.StartsWith("TestDependency"));
                Assert.IsNotNull(eventsLogged);
                Assert.IsNotEmpty(eventsLogged);

                var events = this.mockFixture.Logger.Where(log => log.Item2.Name == "ProfileExecutor.InstallDependenciesStart")?.Select(i => i.Item3 as EventContext);
                Assert.IsNotNull(events);
                Assert.IsNotEmpty(events);
                Assert.IsTrue(events.Count() == 1);

                var dependencies = this.mockFixture.Logger.Where(log => log.Item2.Name == "TestDependency.ExecuteStart").Select(a => a.Item3 as EventContext);
                Assert.IsNotNull(dependencies);
                Assert.IsNotEmpty(dependencies);
                Assert.IsTrue(dependencies.Count() == 2);

                // Monitors should have the same parent ID but each monitor should have its
                // own unique activity ID.
                Assert.IsTrue(dependencies.Select(a => a.ParentActivityId).Distinct().Count() == 1);
                Assert.IsTrue(dependencies.Select(a => a.ActivityId).Distinct().Count() == 2);
                Assert.IsEmpty(dependencies.Select(a => a.ActivityId).Intersect(events.Select(i => i.ActivityId)));

                // None of the monitors should have the same activity ID
                Assert.IsTrue(dependencies.Select(a => a.ActivityId).Distinct().Count() == dependencies.Count());
            }
        }

        [Test]
        public async Task ProfileExecutorCorrelationIdentifiersAreCorrectForMonitorsExecutedThroughoutTheProfileOperations()
        {
            // Expected Correlation Semantics:
            // Program Startup - ActivityID = Correlation ID 1
            //
            // Monitors running in the background
            // ---------------------------------
            // Monitor 1 - ActivityID = Correlation ID 2, Parent Activity ID = Correlation ID 1
            // Monitor 2 - ActivityID = Correlation ID 3, Parent Activity ID = Correlation ID 1

            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies, logger: this.mockFixture.Logger))
            {
                executor.ExecuteActions = false;
                executor.ExecuteDependencies = false;
                executor.ExecuteMonitors = true;

                Task executionTask = executor.ExecuteAsync(ProfileTiming.Iterations(1), CancellationToken.None);

                DateTime testTimeout = DateTime.UtcNow.AddSeconds(5);
                while (!executionTask.IsCompleted)
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }

                executionTask.ThrowIfErrored();

                var eventsLogged = this.mockFixture.Logger.Where(log => log.Item2.Name.StartsWith("TestMonitor"));
                Assert.IsNotNull(eventsLogged);
                Assert.IsNotEmpty(eventsLogged);

                var events = this.mockFixture.Logger.Where(log => log.Item2.Name == "ProfileExecutor.ExecuteMonitorsStart")?.Select(i => i.Item3 as EventContext);
                Assert.IsNotNull(events);
                Assert.IsNotEmpty(events);
                Assert.IsTrue(events.Count() == 1);

                var monitors = this.mockFixture.Logger.Where(log => log.Item2.Name == "TestMonitor.ExecuteStart").Select(a => a.Item3 as EventContext);
                Assert.IsNotNull(monitors);
                Assert.IsNotEmpty(monitors);
                Assert.IsTrue(monitors.Count() == 2);

                // Monitors should have the same parent ID but each monitor should have its
                // own unique activity ID.
                Assert.IsTrue(monitors.Select(a => a.ParentActivityId).Distinct().Count() == 1);
                Assert.IsTrue(monitors.Select(a => a.ActivityId).Distinct().Count() == 2);
                Assert.IsEmpty(monitors.Select(a => a.ActivityId).Intersect(events.Select(i => i.ActivityId)));

                // None of the monitors should have the same activity ID
                Assert.IsTrue(monitors.Select(a => a.ActivityId).Distinct().Count() == monitors.Count());
            }
        }

        [Test]
        [TestCase(ErrorReason.MonitorFailed)]
        [TestCase(ErrorReason.WorkloadFailed)]
        public async Task ProfileExecutorHandlesNonTerminalExceptionsIfTheFailFastOptionIsNotRequested(ErrorReason errorReason)
        {
            int iterationsExecuted = 0;
            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies))
            {
                executor.ExecuteActions = true;
                executor.FailFast = false;

                executor.ActionBegin += (sender, args) => throw new WorkloadException($"Expected to be handled", errorReason);
                executor.IterationEnd += (sender, args) => iterationsExecuted++;

                Task executionTask = executor.ExecuteAsync(new ProfileTiming(profileIterations: 3), CancellationToken.None);

                DateTime testTimeout = DateTime.UtcNow.AddSeconds(10);
                while (!executionTask.IsCompleted)
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }

                Assert.DoesNotThrow(() => executionTask.ThrowIfErrored());
                Assert.AreEqual(TaskStatus.RanToCompletion, executionTask.Status);
                Assert.AreEqual(3, iterationsExecuted);
            }
        }

        [Test]
        [TestCase(ErrorReason.MonitorFailed)]
        [TestCase(ErrorReason.WorkloadFailed)]
        [TestCase(ErrorReason.WorkloadDependencyMissing)]
        public async Task ProfileExecutorExitsImmediatelyOnAnyErrorWheneverTheFailFastOptionIsRequested(ErrorReason errorReason)
        {
            int iterationsExecuted = 0;
            using (TestProfileExecutor executor = new TestProfileExecutor(this.mockProfile, this.mockFixture.Dependencies))
            {
                executor.ExecuteActions = true;
                executor.FailFast = true;

                executor.ActionBegin += (sender, args) => throw new WorkloadException($"Expected to fail on first error", errorReason); 
                executor.IterationEnd += (sender, args) => iterationsExecuted++;

                Task executionTask = executor.ExecuteAsync(new ProfileTiming(profileIterations: 3), CancellationToken.None);

                DateTime testTimeout = DateTime.UtcNow.AddSeconds(10);
                while (!executionTask.IsCompleted)
                {
                    await Task.Delay(10).ConfigureAwait(false);
                }

                WorkloadException exception = Assert.Throws<WorkloadException>(() => executionTask.ThrowIfErrored());

                Assert.AreEqual(errorReason, exception.Reason);
                Assert.AreEqual(1, iterationsExecuted);
            }
        }

        private class TestProfileExecutor : ProfileExecutor
        {
            public TestProfileExecutor(ExecutionProfile profile, IServiceCollection dependencies, IEnumerable<string> scenarios = null, ILogger logger = null)
                : base(profile, dependencies, scenarios, logger)
            {
            }

            public new IEnumerable<VirtualClientComponent> ProfileActions
            {
                get
                {
                    return base.ProfileActions;
                }
            }

            public new IEnumerable<VirtualClientComponent> ProfileDependencies
            {
                get
                {
                    return base.ProfileDependencies;
                }
            }

            public new IEnumerable<VirtualClientComponent> ProfileMonitors
            {
                get
                {
                    return base.ProfileMonitors;
                }
            }

            public new void Initialize()
            {
                base.Initialize();
            }
        }
    }
}
