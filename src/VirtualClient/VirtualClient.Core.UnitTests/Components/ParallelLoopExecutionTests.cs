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
    public class ParallelLoopExecutionTests
    {
        private MockFixture fixture;

        [SetUp]
        public void SetupDefaults()
        {
            this.fixture = new MockFixture();
            this.fixture.Parameters = new Dictionary<string, IConvertible>
            {
                { "Duration", "00:00:01" }, // Default duration of 1 second
                { "MinimumIterations", 1 } // Default minimum iterations
            };
        }

        [Test]
        public async Task ParallelLoopExecution_DoesNotStartAnIterationThatCannotCompleteWithinTheRemainingDuration()
        {
            this.fixture.Parameters["Duration"] = "00:00:01";
            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, async token =>
            {
                await Task.Delay(600, token);
            });

            var collection = new TestParallelLoopExecution(this.fixture);
            collection.Add(component);

            await collection.ExecuteAsync(EventContext.None, CancellationToken.None);

            Assert.AreEqual(1, component.ExecutionCount);
            Assert.AreEqual(1, component.CompletedExecutionCount);
        }

        [Test]
        public async Task ParallelLoopExecution_CancelsAndAwaitsAnIterationThatExceedsTheRemainingDuration()
        {
            this.fixture.Parameters["Duration"] = "00:00:01";
            bool cancellationObserved = false;
            bool cancelledIterationCompleted = false;
            int executionNumber = 0;

            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, async token =>
            {
                executionNumber++;
                if (executionNumber == 1)
                {
                    await Task.Delay(200, token);
                }
                else
                {
                    try
                    {
                        await Task.Delay(5000, token);
                    }
                    catch (OperationCanceledException)
                    {
                        cancellationObserved = true;
                        cancelledIterationCompleted = true;
                        throw;
                    }
                }
            });

            var collection = new TestParallelLoopExecution(this.fixture);
            collection.Add(component);

            await collection.ExecuteAsync(EventContext.None, CancellationToken.None);

            Assert.AreEqual(2, component.ExecutionCount);
            Assert.AreEqual(1, component.CompletedExecutionCount);
            Assert.IsTrue(cancellationObserved);
            Assert.IsTrue(cancelledIterationCompleted);
        }

        [Test]
        public async Task ParallelLoopExecution_RespectsMinimumIterationsParameterAndTimeout()
        {
            this.fixture.Parameters["MinimumIterations"] = 2;
            this.fixture.Parameters["Duration"] = "00:00:01";

            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, async token =>
            {
                await Task.Delay(600, token); // Simulate a small task
            });

            var collection = new TestParallelLoopExecution(this.fixture);
            collection.Add(component);

            await collection.ExecuteAsync(EventContext.None, CancellationToken.None);

            Assert.AreEqual(2, component.ExecutionCount);
            Assert.AreEqual(2, component.CompletedExecutionCount);
        }

        [Test]
        public async Task ParallelLoopExecution_RespectsMinimumIterationsParameter()
        {
            this.fixture.Parameters["MinimumIterations"] = 7;
            this.fixture.Parameters["Duration"] = "00:00:00.100";

            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, async token =>
            {
                await Task.Delay(50, token);
            });

            var collection = new TestParallelLoopExecution(this.fixture);
            collection.Add(component);

            await collection.ExecuteAsync(EventContext.None, CancellationToken.None);

            Assert.AreEqual(7, component.ExecutionCount);
            Assert.AreEqual(7, component.CompletedExecutionCount);
        }

        [Test]
        public void ParallelLoopExecutionHandlesExceptionsAsExpected()
        {
            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                throw new InvalidOperationException("Test exception");
            });

            var collection = new TestParallelLoopExecution(this.fixture);
            collection.Add(component);

            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => collection.ExecuteAsync(EventContext.None, CancellationToken.None));

            Assert.That(ex.Message, Does.Contain("Test exception"));
        }

        private class TestComponent : VirtualClientComponent
        {
            private readonly Func<CancellationToken, Task> onExecuteAsync;

            public int ExecutionCount { get; private set; }

            public int CompletedExecutionCount { get; private set; }

            public TestComponent(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters, Func<CancellationToken, Task> onExecuteAsync = null)
                : base(dependencies, parameters)
            {
                this.onExecuteAsync = onExecuteAsync ?? (_ => Task.CompletedTask);
            }

            protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                this.ExecutionCount++;
                await this.onExecuteAsync(cancellationToken);
                this.CompletedExecutionCount++;
            }
        }

        private class TestParallelLoopExecution : ParallelLoopExecution
        {
            public TestParallelLoopExecution(MockFixture fixture)
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