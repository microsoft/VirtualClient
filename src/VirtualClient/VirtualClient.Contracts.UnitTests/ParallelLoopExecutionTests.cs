// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
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
                { "Duration", "00:00:01" }, // Default duration for tests
                { "MinimumIteration", 0 } // Default minimum iterations
            };
        }

        [Test]
        public async Task ParallelLoopExecution_RespectsDurationParameter()
        {
            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, async token =>
            {
                await Task.Delay(5000, token); // Simulate long-running task
            });

            var collection = new TestParallelLoopExecution(this.fixture);
            collection.Add(component);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            await collection.ExecuteAsync(EventContext.None, CancellationToken.None);
            sw.Stop();

            // Assert: Should not run for more than ~2 seconds (buffer for scheduling)
            Assert.LessOrEqual(sw.Elapsed.TotalSeconds, 2.5, "Execution did not respect the Duration parameter.");
        }

        [Test]
        public async Task ParallelLoopExecution_RespectsMinimumIterationParameterAndTimeout()
        {
            this.fixture.Parameters["MinimumIteration"] = 2;
            this.fixture.Parameters["Duration"] = "00:00:01";

            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, async token =>
            {
                await Task.Delay(600, token); // Simulate long-running task
            });

            var collection = new TestParallelLoopExecution(this.fixture);
            collection.Add(component);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
            {
                try
                {
                    await collection.ExecuteAsync(EventContext.None, cts.Token);
                }
                catch { /* ignore */ }
            }

            // Assert: Should run at exactly 2 times, as each iteration takes 600ms,
            // Timeout is 1 second and Cancellation Token comes at 2 seconds
            Assert.AreEqual(component.ExecutionCount, 2, "Did not execute the minimum number of iterations.");
        }

        [Test]
        public async Task ParallelLoopExecution_RespectsMinimumIterationParameter()
        {
            this.fixture.Parameters["MinimumIteration"] = 7;

            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                return Task.CompletedTask;
            });

            var collection = new TestParallelLoopExecution(this.fixture);
            collection.Add(component);

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2)))
            {
                try
                {
                    await collection.ExecuteAsync(EventContext.None, cts.Token);
                }
                catch { /* ignore */ }
            }

            // Assert: Should run at least MinimumIteration times
            Assert.GreaterOrEqual(component.ExecutionCount, 7, "Did not execute the minimum number of iterations.");
        }

        [Test]
        public void ParallelLoopExecution_ThrowsWorkloadException_WhenComponentThrows()
        {
            var component = new TestComponent(this.fixture.Dependencies, this.fixture.Parameters, token =>
            {
                throw new InvalidOperationException("Test exception");
            });

            var collection = new TestParallelLoopExecution(this.fixture);
            collection.Add(component);

            var ex = Assert.ThrowsAsync<WorkloadException>(
                () => collection.ExecuteAsync(EventContext.None, CancellationToken.None));
            Assert.That(ex.Message, Does.Contain("task execution failed"));
            Assert.IsInstanceOf<InvalidOperationException>(ex.InnerException);
        }

        private class TestComponent : VirtualClientComponent
        {
            private readonly Func<CancellationToken, Task> onExecuteAsync;

            public int ExecutionCount { get; private set; }

            public TestComponent(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters, Func<CancellationToken, Task> onExecuteAsync = null)
                : base(dependencies, parameters)
            {
                this.onExecuteAsync = onExecuteAsync ?? (_ => Task.CompletedTask);
            }

            protected override async Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                this.ExecutionCount++;
                await this.onExecuteAsync(cancellationToken);
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