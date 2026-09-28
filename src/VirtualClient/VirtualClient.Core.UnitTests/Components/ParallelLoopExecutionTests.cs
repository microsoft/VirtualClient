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
                { "Duration", "00:00:01" }
            };
        }

        [Test]
        public async Task ParallelLoopExecution_CancelsAndAwaitsTheInFlightIterationWhenTheDurationElapses()
        {
            this.fixture.Parameters["MinimumIterations"] = 0;
            bool cancellationObserved = false;
            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, async token =>
            {
                try
                {
                    await Task.Delay(5000, token);
                }
                catch (OperationCanceledException)
                {
                    await Task.Delay(100);
                    cancellationObserved = true;
                    throw;
                }
            });

            var collection = new TestParallelLoopExecution(this.fixture);
            collection.Add(component);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            await collection.ExecuteAsync(EventContext.None, CancellationToken.None);
            sw.Stop();

            Assert.LessOrEqual(sw.Elapsed.TotalSeconds, 2.5, "Execution did not respect the Duration parameter.");
            Assert.IsTrue(cancellationObserved, "The in-flight iteration was abandoned instead of cancelled and awaited.");
            Assert.AreEqual(1, component.ExecutionCount);
            Assert.AreEqual(0, component.CompletedExecutionCount);
        }

        [Test]
        public async Task ParallelLoopExecution_CompletesTheFirstIterationByDefaultEvenWhenItExceedsTheDuration()
        {
            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, async token =>
            {
                await Task.Delay(1500, token);
            });

            var collection = new TestParallelLoopExecution(this.fixture);
            collection.Add(component);

            await collection.ExecuteAsync(EventContext.None, CancellationToken.None);

            Assert.AreEqual(1, component.ExecutionCount);
            Assert.AreEqual(1, component.CompletedExecutionCount);
        }

        [Test]
        public async Task ParallelLoopExecution_CancelsAndAwaitsAnIterationBeyondTheMinimumWhenTheDurationElapses()
        {
            this.fixture.Parameters["MinimumIterations"] = 1;
            bool cancellationObserved = false;
            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, async token =>
            {
                try
                {
                    await Task.Delay(600, token);
                }
                catch (OperationCanceledException)
                {
                    cancellationObserved = true;
                    throw;
                }
            });

            var collection = new TestParallelLoopExecution(this.fixture);
            collection.Add(component);

            await collection.ExecuteAsync(EventContext.None, CancellationToken.None);

            Assert.AreEqual(2, component.ExecutionCount);
            Assert.AreEqual(1, component.CompletedExecutionCount);
            Assert.IsTrue(cancellationObserved);
        }

        [Test]
        public async Task ParallelLoopExecution_LoopsUntilCancelledWhenNoDurationIsDefined()
        {
            this.fixture.Parameters.Remove("Duration");
            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, async token =>
            {
                await Task.Delay(50, token);
            });

            var collection = new TestParallelLoopExecution(this.fixture);
            collection.Add(component);

            using (var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(500)))
            {
                await collection.ExecuteAsync(EventContext.None, cts.Token);
            }

            Assert.Greater(component.CompletedExecutionCount, 3);
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