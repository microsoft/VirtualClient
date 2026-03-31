// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SequentialExecutionTests
    {
        private MockFixture fixture;

        [SetUp]
        public void SetupDefaults()
        {
            this.fixture = new MockFixture();
            this.fixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "LoopCount", 3 }
            };
        }

        [Test]
        public async Task SequentialExecutionExecutesComponentsTheSpecifiedNumberOfTimes()
        {
            int loopCount = 5;
            this.fixture.Parameters["LoopCount"] = loopCount;

            var component1 = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                return Task.CompletedTask;
            });

            var component2 = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                return Task.CompletedTask;
            });

            var collection = new TestSequentialExecution(this.fixture);
            collection.Add(component1);
            collection.Add(component2);

            await collection.ExecuteAsync(EventContext.None, CancellationToken.None);

            Assert.AreEqual(loopCount, component1.ExecutionCount, "Component1 was not executed the expected number of times.");
            Assert.AreEqual(loopCount, component2.ExecutionCount, "Component2 was not executed the expected number of times.");
        }

        [Test]
        public async Task SequentialExecutionRespectsCancellationToken()
        {
            int loopCount = 100;
            this.fixture.Parameters["LoopCount"] = loopCount;

            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, async token =>
            {
                await Task.Delay(100, token);
            });

            var collection = new TestSequentialExecution(this.fixture);
            collection.Add(component);

            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250)))
            {
                try
                {
                    await collection.ExecuteAsync(EventContext.None, cts.Token);
                }
                catch (OperationCanceledException) { }
            }

            Assert.Less(component.ExecutionCount, loopCount, "Component should not have executed all iterations due to cancellation.");
        }

        [Test]
        public void SequentialExecutionThrowsWorkloadException_WhenComponentThrows()
        {
            this.fixture.Parameters["LoopCount"] = 2;

            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                throw new WorkloadException("Test exception");
            });

            var collection = new TestSequentialExecution(this.fixture);
            collection.Add(component);

            var ex = Assert.ThrowsAsync<WorkloadException>(() => collection.ExecuteAsync(EventContext.None, CancellationToken.None));
            Assert.That(ex.Message, Does.Contain("Test exception"));
        }

        [Test]
        public async Task SequentialExecutionEnsuresEachComponentExecutesAtLeastOnceWhenUsingDeterministicExecutionStrategy()
        {
            List<int> componentsExecuted = new List<int>();

            var component1 = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                componentsExecuted.Add(1);
                throw new WorkloadException("Component 1 error");
            });

            var component2 = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                componentsExecuted.Add(2);
                throw new WorkloadException("Component 2 error");
            });

            var component3 = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                componentsExecuted.Add(3);
                throw new WorkloadException("Component 3 error");
            });

            var collection = new TestSequentialExecution(this.fixture)
            {
                component1,
                component2,
                component3
            };

            collection.Parameters[nameof(SequentialExecution.ExecutionStrategy)] = "Deterministic";

            try
            {
                await collection.ExecuteAsync(EventContext.None, CancellationToken.None);
            }
            catch
            {
            }

            Assert.AreEqual(3, componentsExecuted.Count);
            Assert.AreEqual(1, componentsExecuted[0]);
            Assert.AreEqual(2, componentsExecuted[1]);
            Assert.AreEqual(3, componentsExecuted[2]);
        }

        [Test]
        public void SequentialExecutionHandlesExceptionsAsExpectedWhenUsingDeterministicExecutionStrategy_Scenario_1()
        {
            // Scenario:
            // Formal VirtualClientException instances are thrown by the components.
            var component1 = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                throw new WorkloadException("Component 1 error", ErrorReason.WorkloadFailed);
            });

            var component2 = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                // The highest priority error reason should be used for the final failure reason.
                throw new WorkloadException("Component 2 error", ErrorReason.WorkloadDependencyMissing);
            });

            var component3 = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                throw new WorkloadException("Component 3 error", ErrorReason.WorkloadFailed);
            });

            var collection = new TestSequentialExecution(this.fixture)
            {
                component1,
                component2,
                component3
            };

            collection.Parameters[nameof(SequentialExecution.ExecutionStrategy)] = "Deterministic";

            ComponentException error = Assert.ThrowsAsync<ComponentException>(() => collection.ExecuteAsync(EventContext.None, CancellationToken.None));
            Assert.AreEqual("Sequential operations failed.", error.Message);
            Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
        }

        [Test]
        public void SequentialExecutionHandlesExceptionsAsExpectedWhenUsingDeterministicExecutionStrategy_Scenario_3()
        {
            // Scenario:
            // Formal VirtualClientException instances are thrown by some but not all components.
            var component1 = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                throw new NotSupportedException("Component 1 error");
            });

            var component2 = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                throw new NotSupportedException("Component 2 error");
            });

            var component3 = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                // The highest priority error reason should be used for the final failure reason.
                throw new WorkloadException("Component 3 error", ErrorReason.WorkloadFailed);
            });

            var collection = new TestSequentialExecution(this.fixture)
            {
                component1,
                component2,
                component3
            };

            collection.Parameters[nameof(SequentialExecution.ExecutionStrategy)] = "Deterministic";

            ComponentException error = Assert.ThrowsAsync<ComponentException>(() => collection.ExecuteAsync(EventContext.None, CancellationToken.None));
            Assert.AreEqual("Sequential operations failed.", error.Message);
            Assert.AreEqual(ErrorReason.WorkloadFailed, error.Reason);
        }

        [Test]
        [TestCase(ComponentType.Action, ErrorReason.WorkloadFailed)]
        [TestCase(ComponentType.Dependency, ErrorReason.DependencyInstallationFailed)]
        [TestCase(ComponentType.Monitor, ErrorReason.MonitorFailed)]
        public void SequentialExecutionHandlesExceptionsAsExpectedWhenUsingDeterministicExecutionStrategy_Scenario_3(ComponentType componentType, ErrorReason expectedReason)
        {
            // Scenario:
            // No VirtualClientException instances are thrown
            var component1 = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                throw new NotSupportedException("Component 1 error");
            });

            var component2 = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                throw new NotSupportedException("Component 2 error");
            });

            var component3 = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                throw new NotSupportedException("Component 3 error");
            });

            var collection = new TestSequentialExecution(this.fixture)
            {
                component1,
                component2,
                component3
            };

            collection.ComponentType = componentType;
            collection.Parameters[nameof(SequentialExecution.ExecutionStrategy)] = "Deterministic";

            ComponentException error = Assert.ThrowsAsync<ComponentException>(() => collection.ExecuteAsync(EventContext.None, CancellationToken.None));
            Assert.AreEqual("Sequential operations failed.", error.Message);
            Assert.AreEqual(expectedReason, error.Reason);
        }

        [Test]
        public async Task SequentialExecutionSkipsUnsupportedComponents()
        {
            int loopCount = 3;
            this.fixture.Parameters["LoopCount"] = loopCount;

            var supportedComponent = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token => Task.CompletedTask, isSupported: true);
            var unsupportedComponent = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token => Task.CompletedTask, isSupported: false);

            var collection = new TestSequentialExecution(this.fixture);
            collection.Add(supportedComponent);
            collection.Add(unsupportedComponent);

            await collection.ExecuteAsync(EventContext.None, CancellationToken.None);

            Assert.AreEqual(loopCount, supportedComponent.ExecutionCount, "Supported component should be executed the expected number of times.");
            Assert.AreEqual(0, unsupportedComponent.ExecutionCount, "Unsupported component should not be executed.");
        }

        private class TestComponent : VirtualClientComponent
        {
            private readonly Func<CancellationToken, Task> onExecuteAsync;
            private readonly bool isSupported;

            public int ExecutionCount { get; private set; }

            public TestComponent(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters, Func<CancellationToken, Task> onExecuteAsync = null, bool isSupported = true)
                : base(dependencies, parameters)
            {
                this.onExecuteAsync = onExecuteAsync ?? (_ => Task.CompletedTask);
                this.isSupported = isSupported;
            }

            protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                this.ExecutionCount++;
                await this.onExecuteAsync(cancellationToken);
            }

            protected override bool IsSupported()
            {
                return this.isSupported;
            }
        }

        private class TestSequentialExecution : SequentialExecution
        {
            public TestSequentialExecution(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(telemetryContext, cancellationToken);
            }
        }
    }
}