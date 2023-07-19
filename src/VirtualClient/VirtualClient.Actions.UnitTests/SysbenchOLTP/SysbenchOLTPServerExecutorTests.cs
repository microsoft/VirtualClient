// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SysbenchOLTPServerExecutorTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);

            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}-Server", "1.2.3.4", "Server"),
                new ClientInstance($"{Environment.MachineName}-Client", "1.2.3.5", "Client")
            });

            string agentId = $"{Environment.MachineName}-Server";
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(agentId);

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.Directory.Setup(d => d.Exists(It.IsAny<string>())).Returns(true);

            this.mockFixture.Parameters["PackageName"] = "sysbench";
        }

        [Test]
        public async Task SysbenchOLTPServerExecutorExcutesExpectedProcessInMemoryScenario()
        {
            SetupDefaultBehavior();
            int commandsExecuted = 0;
            this.mockFixture.Parameters["DatabaseScenario"] = "InMemory";

            using TestSysbenchOLTPServerExecutor executor = new TestSysbenchOLTPServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            string scriptPath = this.mockFixture.PlatformSpecifics.GetScriptPath("sysbencholtp");
            
            // Mocking 8GB of memory
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1024 * 1024 * 8));

            string[] expectedCommands =
            {
                $"sudo chmod +x \"{scriptPath}/inmemory.sh\"",
                $"sudo chmod +x \"{scriptPath}/balancedServer.sh\"",
                $"sudo chmod +x \"{scriptPath}/balancedClient.sh\"",
                $"sudo {scriptPath}/inmemory.sh 8192"
            };

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandsExecuted++;
                }

                return this.mockFixture.Process;
            };

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(100);
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await executor.ExecuteAsync(cancellationToken);

            Assert.AreEqual(4, commandsExecuted);
        }

        [Test]
        public async Task SysbenchOLTPServerExecutorExcutesExpectedProcessBalancedScenario()
        {
            SetupDefaultBehavior();
            int commandsExecuted = 0;
            this.mockFixture.Parameters["DatabaseScenario"] = "Balanced";

            using TestSysbenchOLTPServerExecutor executor = new TestSysbenchOLTPServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            string scriptPath = this.mockFixture.PlatformSpecifics.GetScriptPath("sysbencholtp");

            IEnumerable<Disk> disks;
            disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);
            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => disks);

            disks.ToList().ForEach(disk => disk.Volumes.ToList().ForEach(vol => (vol.AccessPaths as List<string>).Clear()));

            List<Tuple<DiskVolume, string>> mountPointsCreated = new List<Tuple<DiskVolume, string>>();

            this.mockFixture.DiskManager
                .Setup(mgr => mgr.CreateMountPointAsync(It.IsAny<DiskVolume>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<DiskVolume, string, CancellationToken>((volume, mountPoint, token) =>
                {
                    (volume.AccessPaths as List<string>).Add(mountPoint);
                })
                .Returns(Task.CompletedTask);

            string mountPaths = $"{Path.Combine(MockFixture.TestAssemblyDirectory, "vcmnt_dev_sdc1")} " +
                $"{Path.Combine(MockFixture.TestAssemblyDirectory, "vcmnt_dev_sdd1")} " +
                $"{Path.Combine(MockFixture.TestAssemblyDirectory, "vcmnt_dev_sde1")} " +
                $"{Path.Combine(MockFixture.TestAssemblyDirectory, "vcmnt_dev_sdf1")}";

            string[] expectedCommands =
            {
                $"sudo chmod +x \"{scriptPath}/inmemory.sh\"",
                $"sudo chmod +x \"{scriptPath}/balancedServer.sh\"",
                $"sudo chmod +x \"{scriptPath}/balancedClient.sh\"",
                $"sudo {scriptPath}/balancedServer.sh {mountPaths}"
            };

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandsExecuted++;
                }

                return this.mockFixture.Process;
            };

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1500);
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await executor.ExecuteAsync(cancellationToken);

            Assert.AreEqual(4, commandsExecuted);
        }

        [Test]
        public async Task SysbenchOLTPServerExecutorDoesNotExecuteBalancedScenarioOnInitializedState()
        {
            SetupDefaultBehavior();
            int commandsExecuted = 0;
            this.mockFixture.Parameters["DatabaseScenario"] = "Balanced";

            using TestSysbenchOLTPServerExecutor executor = new TestSysbenchOLTPServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            string scriptPath = this.mockFixture.PlatformSpecifics.GetScriptPath("sysbencholtp");

            this.mockFixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new SysbenchOLTPExecutor.SysbenchOLTPState()
            {
                DatabaseScenarioInitialized = true,
                DiskPathsArgument = "/testdrive1 /testdrive2"
            }));

            string[] expectedCommands =
            {
                $"sudo chmod +x \"{scriptPath}/inmemory.sh\"",
                $"sudo chmod +x \"{scriptPath}/balancedServer.sh\"",
                $"sudo chmod +x \"{scriptPath}/balancedClient.sh\"",
                $"sudo {scriptPath}/balancedServer.sh"
            };

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDirectory) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandsExecuted++;
                }

                return this.mockFixture.Process;
            };

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.CancelAfter(1500);
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            await executor.ExecuteAsync(cancellationToken);

            Assert.AreEqual(3, commandsExecuted);
        }

        private class TestSysbenchOLTPServerExecutor : SysbenchOLTPServerExecutor
        {
            public TestSysbenchOLTPServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

        }
    }
}
