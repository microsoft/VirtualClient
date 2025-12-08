// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Controller
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Renci.SshNet;
    using Renci.SshNet.Common;
    using VirtualClient;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class VirtualClientControllerComponentTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
        }

        [Test]
        [TestCase(10)]
        [TestCase(100)]
        public async Task VirtualClientControllerComponentSupportsConcurrentSshClientOperations(int concurrentExecutions)
        {
            List<ISshClientProxy> targetAgents = new List<ISshClientProxy>();
            for (int i = 0; i < concurrentExecutions; i++)
            {
                targetAgents.Add(new InMemorySshClient(new PasswordConnectionInfo($"10.1.2.{i + 1}", "anyuser", "anypass")));
            }

            this.mockFixture.Dependencies.AddSingleton<IEnumerable<ISshClientProxy>>(targetAgents);

            using (var component = new TestVirtualClientControllerComponent(this.mockFixture))
            {
                int actualConcurrentExecutions = 0;
                component.OnExecute = (sshClient) =>
                {
                    Interlocked.Increment(ref actualConcurrentExecutions);
                };

                await component.ExecuteAsync(CancellationToken.None);

                Assert.AreEqual(concurrentExecutions, actualConcurrentExecutions);
            }
        }

        [Test]
        [TestCase(10)]
        [TestCase(100)]
        public async Task VirtualClientControllerComponentSupportsConcurrentSshClientOperationsWithFailures(int concurrentExecutions)
        {
            List<ISshClientProxy> targetAgents = new List<ISshClientProxy>();
            for (int i = 0; i < concurrentExecutions; i++)
            {
                targetAgents.Add(new InMemorySshClient(new PasswordConnectionInfo($"10.1.2.{i + 1}", "anyuser", "anypass")));
            }

            this.mockFixture.Dependencies.AddSingleton<IEnumerable<ISshClientProxy>>(targetAgents);

            using (var component = new TestVirtualClientControllerComponent(this.mockFixture))
            {
                int actualConcurrentExecutions = 0;
                component.OnExecute = (sshClient) =>
                {
                    Interlocked.Increment(ref actualConcurrentExecutions);
                    throw new WorkloadException();
                };

                try
                {
                    await component.ExecuteAsync(CancellationToken.None);
                }
                catch (WorkloadException)
                {
                    // Expected - failures should still attempt all operations
                }

                Assert.AreEqual(concurrentExecutions, actualConcurrentExecutions);
            }
        }

        private class TestVirtualClientControllerComponent : VirtualClientControllerComponent
        {
            public TestVirtualClientControllerComponent(MockFixture mockFixture)
                : base(mockFixture?.Dependencies, mockFixture?.Parameters)
            {
            }

            public Action<ISshClientProxy> OnExecute { get; set; }

            protected override Task ExecuteAsync(ISshClientProxy targetAgent, EventContext telemetryContext, CancellationToken cancellationToken)
            {
                this.OnExecute?.Invoke(targetAgent);
                return Task.CompletedTask;
            }
        }
    }
}
