// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class LoopExecutionTests
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
        public async Task LoopExecution_ExecutesComponentsTheSpecifiedNumberOfTimes()
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

            var collection = new TestLoopExecution(this.fixture);
            collection.Add(component1);
            collection.Add(component2);

            await collection.ExecuteAsync(EventContext.None, CancellationToken.None);

            Assert.AreEqual(loopCount, component1.ExecutionCount, "Component1 was not executed the expected number of times.");
            Assert.AreEqual(loopCount, component2.ExecutionCount, "Component2 was not executed the expected number of times.");
        }

        [Test]
        public async Task LoopExecution_RespectsCancellationToken()
        {
            int loopCount = 100;
            this.fixture.Parameters["LoopCount"] = loopCount;

            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, async token =>
            {
                await Task.Delay(100, token);
            });

            var collection = new TestLoopExecution(this.fixture);
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
        public void LoopExecution_ThrowsWorkloadException_WhenComponentThrows()
        {
            this.fixture.Parameters["LoopCount"] = 2;

            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                throw new InvalidOperationException("Test exception");
            });

            var collection = new TestLoopExecution(this.fixture);
            collection.Add(component);

            var ex = Assert.ThrowsAsync<WorkloadException>(
                () => collection.ExecuteAsync(EventContext.None, CancellationToken.None));
            Assert.That(ex.Message, Does.Contain("task execution failed"));
            Assert.IsInstanceOf<InvalidOperationException>(ex.InnerException);
        }

        [Test]
        public async Task LoopExecution_SkipsUnsupportedComponents()
        {
            int loopCount = 3;
            this.fixture.Parameters["LoopCount"] = loopCount;

            var supportedComponent = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token => Task.CompletedTask, isSupported: true);
            var unsupportedComponent = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token => Task.CompletedTask, isSupported: false);

            var collection = new TestLoopExecution(this.fixture);
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

        private class TestLoopExecution : LoopExecution
        {
            public TestLoopExecution(MockFixture fixture)
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