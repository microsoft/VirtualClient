// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Actions.Memtier;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MemcachedServerExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockMemcachedPackage;

        [SetUp]
        public void SetupDefaults()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);

            this.mockMemcachedPackage = new DependencyPath("memcached", this.fixture.GetPackagePath("memcached"));

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["Scenario"] = "Memtier_Scenario",
                ["PackageName"] = this.mockMemcachedPackage.Name,
                ["CommandLine"] = "-p {Port} -t {ServerThreadCount} -m 8192 -c {ServerMaxConnections}",
                ["BindToCores"] = true,
                ["Port"] = 6379,
                ["Username"] = "testuser",
                ["ServerThreadCount"] = 4,
                ["ServerMaxConnections"] = 4096
            };

            this.fixture.SystemManagement.SetupGet(obj => obj.AgentId)
                .Returns($"{Environment.MachineName}-Server");

            this.fixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(this.mockMemcachedPackage.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.mockMemcachedPackage);

            this.fixture.FileSystem.Setup(fe => fe.File.Exists(It.IsAny<string>())).Returns(true);

            this.fixture.ApiClient.OnUpdateState<ServerState>(nameof(ServerState))
                .ReturnsAsync(this.fixture.CreateHttpResponse(HttpStatusCode.OK));
        }


        [Test]
        public async Task MemcachedServerExecutorOnInitializationGetsExpectedPackagesLocationOnServerRole()
        {
            using (var component = new TestMemcachedMemtierServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    return this.fixture.Process;
                };

                await component.InitializeAsync(EventContext.None, CancellationToken.None);
                this.fixture.PackageManager.Verify(mgr => mgr.GetPackageAsync(this.mockMemcachedPackage.Name, It.IsAny<CancellationToken>()));
            }
        }

        [Test]
        public async Task MemcachedServerExecutorExecutesExpectedProcessWhenBindingToCores()
        {
            using (var executor = new TestMemcachedMemtierServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                List<string> expectedCommands = new List<string>()
                {
                    // Make the Memcached server toolset executable
                    $"sudo chmod +x \"{this.mockMemcachedPackage.Path}/memcached\"",

                    // Run the Memcached server. We run 1 server instance bound to each of the logical cores on the system.
                    $"sudo -u testuser bash -c \"numactl -C {string.Join(",", Enumerable.Range(0, Environment.ProcessorCount))} {this.mockMemcachedPackage.Path}/memcached -p {executor.Port} -t 4 -m 8192 -c 4096\""

               };

                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{exe} {arguments}");
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task MemcachedServerExecutorExecutesExpectedProcessWhenNotBindingToCores()
        {
            using (var executor = new TestMemcachedMemtierServerExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                executor.Parameters[nameof(executor.BindToCores)] = false;

                List<string> expectedCommands = new List<string>()
                {
                    // Make the Memcached server toolset executable
                    $"sudo chmod +x \"{this.mockMemcachedPackage.Path}/memcached\"",

                    // Run the Memcached server. We run 1 server instance bound to each of the logical cores on the system.
                    $"sudo -u testuser bash -c \"{this.mockMemcachedPackage.Path}/memcached -p {executor.Port} -t 4 -m 8192 -c 4096\""

               };

                this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{exe} {arguments}");
                    return this.fixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        private class TestMemcachedMemtierServerExecutor : MemcachedServerExecutor
        {
            public TestMemcachedMemtierServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
