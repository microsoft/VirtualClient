// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class MetaseqExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;
        private IEnumerable<Disk> disks;

        [SetUp]
        public void SetupTests()
        {
            this.mockFixture = new MockFixture();
            this.SetupDefaultMockBehavior(PlatformID.Unix);
        }

        [Test]
        public void MetaseqStateIsSerializeable()
        {
            State state = new State(new Dictionary<string, IConvertible>
            {
                ["MetaseqInitialized"] = true
            });

            string serializedState = state.ToJson();
            JObject deserializedState = JObject.Parse(serializedState);

            MetaseqExecutor.MetaseqState result = deserializedState?.ToObject<MetaseqExecutor.MetaseqState>();
            Assert.AreEqual(true, result.MetaseqInitialized);
        }

        [Test]
        public async Task MetaseqExecutorInitializesWorkloadAsExpected()
        {
            List<string> expectedCommands = new List<string>
            {
                "ping -W 1000 -c 1 node1",
                "ping -W 1000 -c 1 node2",
                "ping -W 1000 -c 1 node3",
                "mdadm --create /dev/md128 --level 0 --raid-devices 2 /dev/nvme1n1 /dev/nvme1n2",
                "mkfs.xfs /dev/md128",
                "mount /dev/md128 /mnt/resource_nvme",
                "chmod 777 /mnt/resource_nvme",
                "apptainer pull metaseq_mockVersion.sif oras://aisweco.azurecr.io/metaseq_cuda:mockVersion",                
            };

            List<string> commandsExecuted = new List<string>();
            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.mockFixture.Process;
            };

            using (TestMetaseqExecutor metaseqExecutor = new TestMetaseqExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await metaseqExecutor.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            CollectionAssert.AreEqual(expectedCommands, commandsExecuted);
        }

        [Test]
        public async Task MetaseqExecutorSkipsInitializationOfTheWorkloadForExecutionAfterTheFirstRun()
        {
            bool initializationVerified = false;

            this.mockFixture.StateManager.OnGetState()
            .Callback<String, CancellationToken, IAsyncPolicy>((stateId, cancellationToken, policy) =>
            {
                initializationVerified = true;
            })
            .ReturnsAsync(JObject.FromObject(new MetaseqExecutor.MetaseqState()
            {
                MetaseqInitialized = true
            }));

            using (TestMetaseqExecutor metaseqExecutor = new TestMetaseqExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await metaseqExecutor.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(initializationVerified);
        }

        [Test]
        public async Task MetaseqExecutorExecutesAsExpected()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);
            IEnumerable<string> expectedCommands = this.GetExpectedCommands();

            List<string> commandsExecuted = new List<string>();

            this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
            {
                commandsExecuted.Add($"{file} {arguments}".Trim());
                return this.mockFixture.Process;
            };

            using (TestMetaseqExecutor metaseqExecutor = new TestMetaseqExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await metaseqExecutor.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
                await metaseqExecutor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            CollectionAssert.AreEqual(expectedCommands.ToArray(), commandsExecuted);
        }

        [Test]
        public async Task MetaseqExecutorWritesCorrectHostnamesInHostFile()
        {
            this.SetupDefaultMockBehavior(PlatformID.Unix);

            List<string> commandsExecuted = new List<string>();

            this.mockFixture.File.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, contents, token) => {
                   if (path.Contains("hostfile.txt"))
                    {
                        string expectedContent = $"node1\nnode2\nnode3";
                        Assert.IsTrue(expectedContent.Equals(contents));
                    }
                });

            using (TestMetaseqExecutor metaseqExecutor = new TestMetaseqExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await metaseqExecutor.InitializeAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }
        }

        private void SetupDefaultMockBehavior(PlatformID platformID)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platformID);
            this.mockPackage = new DependencyPath("Metaseq", this.mockFixture.PlatformSpecifics.GetPackagePath("metaseq"));

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);
            this.mockFixture.File.Reset();

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.FileSystem.Setup(f => f.Directory.GetFiles(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new string[] { "/dev/nvme1n1", "/dev/nvme1n2" });

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix, true);

            this.mockFixture.DiskManager.Setup(dm => dm.GetDisksAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.disks);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(MetaseqExecutor.ApptainerImageVersion), "mockVersion" },
                { nameof(MetaseqExecutor.TrainingScript), "mockScript" },
                { nameof(MetaseqExecutor.Hostnames), "node1,node2,node3"}
            };

            // this.mockFixture.File.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()));
        }

        private IEnumerable<string> GetExpectedCommands()
        {
            List<string> commands = null;
            commands = new List<string>
            {
                "ping -W 1000 -c 1 node1",
                "ping -W 1000 -c 1 node2",
                "ping -W 1000 -c 1 node3",
                "mdadm --create /dev/md128 --level 0 --raid-devices 2 /dev/nvme1n1 /dev/nvme1n2",
                "mkfs.xfs /dev/md128",
                "mount /dev/md128 /mnt/resource_nvme",
                "chmod 777 /mnt/resource_nvme",
                "apptainer pull metaseq_mockVersion.sif oras://aisweco.azurecr.io/metaseq_cuda:mockVersion",
                $"{this.mockPackage.Path.ToString()}/run_training.sh {this.mockPackage.Path.ToString()}/metaseq_mockVersion.sif {this.mockPackage.Path.ToString()}/hostfile.txt mockScript"
            };

            return commands;
        }

        protected class TestMetaseqExecutor : MetaseqExecutor
        {
            public TestMetaseqExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }

            public new Task InitializeAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(context, cancellationToken);
            }
        }
    }
}
