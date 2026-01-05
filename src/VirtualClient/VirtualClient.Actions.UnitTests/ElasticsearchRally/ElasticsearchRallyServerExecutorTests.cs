// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using CRC.VirtualClient.Actions;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class ElasticsearchRallyServerExecutorTests : MockFixture
    {
        private IEnumerable<Disk> disks;
        [SetUp]
        public void SetupTest()
        {
            this.Setup(PlatformID.Unix);

            this.File.Reset();
            this.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.FileSystem.SetupGet(fs => fs.File).Returns(this.File.Object);

            string agentId = $"{Environment.MachineName}";
            this.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            this.disks = this.CreateDisks(PlatformID.Unix, true);

            this.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => this.disks);

            this.ApiClient.Setup(client => client.GetHeartbeatAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.ApiClient.Setup(client => client.GetServerOnlineStatusAsync(It.IsAny<CancellationToken>(), It.IsAny<IAsyncPolicy<HttpResponseMessage>>()))
                .ReturnsAsync(this.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(ElasticsearchRallyServerExecutor.DiskFilter), "osdisk:false&biggestsize" },
                { nameof(ElasticsearchRallyServerExecutor.Port), "9200" },
                { nameof(ElasticsearchRallyServerExecutor.PackageName), "elasticsearchrally" },
            };
        }

        [Test]
        public void TestElasticsearchRallyServerExecutorInitializeYmlNotFound()
        {
            SetupTest();

            bool commandExecuted = false;

            using (TestElasticsearchRallyServerExecutor executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                executor.OnRunCommand = (command, arguments) =>
                {
                    commandExecuted = true;
                };
                
                Assert.ThrowsAsync<WorkloadException>(() => executor.InitializeAsync(EventContext.None, CancellationToken.None));
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task TestElasticsearchRallyServerExecutorInitialize()
        {
            SetupTest();

            bool commandExecuted = false;

            using (TestElasticsearchRallyServerExecutor executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                executor.OnRunCommand = (command, arguments) =>
                {
                    commandExecuted = true;
                };

                executor.FileExists = true;
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task TestElasticsearchRallyServerExecutorExpectedRun()
        {
            SetupTest();

            bool commandExecuted = false;

            using (TestElasticsearchRallyServerExecutor executor = new TestElasticsearchRallyServerExecutor(this.Dependencies, this.Parameters))
            {
                await executor.ExecuteAsync(EventContext.None, CancellationToken.None);
                commandExecuted = true;
            }

            Assert.IsTrue(commandExecuted);
        }

        private class TestElasticsearchRallyServerExecutor : ElasticsearchRallyServerExecutor
        {
            public Action<string, string> OnRunCommand { get; set; }
            public bool FileExists { get; set; }
            public TestElasticsearchRallyServerExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task InitializeAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(context, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }

            protected override bool RunCommand(string command, string arguments, out string output, out string error)
            {
                output = string.Empty;
                error = string.Empty;

                OnRunCommand?.Invoke(command, arguments);

                return true;
            }

            protected override bool CheckFileExists(string path)
            {
                return this.FileExists;
            }

            protected override void WriteAllText(string path, string content)
            {
                
            }

            protected override string ReadAllText(string path)
            {
                return "sample text";
            }
        }

    }
}
