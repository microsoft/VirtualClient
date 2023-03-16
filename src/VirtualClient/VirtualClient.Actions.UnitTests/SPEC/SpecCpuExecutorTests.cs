// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common.Contracts;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class SpecCpuExecutorTests
    {
        private MockFixture mockFixture;
        private IEnumerable<Disk> disks;
        private DependencyPath mockPackage;

        [SetUp]
        public void SetupDefaultBehavior()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.disks = this.mockFixture.CreateDisks(PlatformID.Unix);
            this.mockPackage = new DependencyPath("SPECcpu", this.mockFixture.PlatformSpecifics.GetPackagePath("speccpu", "1.1.8"));

            this.mockFixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(this.disks);
            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.mockFixture.Directory.Setup(dir => dir.GetFiles(It.IsAny<string>(), "*.iso", It.IsAny<SearchOption>()))
                .Returns(new string[] { this.mockFixture.Combine(this.mockPackage.Path, "speccpu.iso") });

            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetSystemCoreCount()).Returns(71);
            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            string mockProfileText = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SPEC", "mockspeccpu.cfg"));
            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockProfileText);
            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.FileInfo.Setup(file => file.FromFileName(It.IsAny<string>()))
                .Returns(new Mock<IFileInfo>().Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.CompilerVersion), "10" },
                { nameof(SpecCpuExecutor.SpecProfile), "intrate" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), true }
            };
        }

        [Test]
        public void SpecCpuStateIsSerializeable()
        {
            State state = new State(new Dictionary<string, IConvertible>
            {
                ["SpecCpuInitialized"] = true
            });

            string serializedState = state.ToJson();
            JObject deserializedState = JObject.Parse(serializedState);

            SpecCpuExecutor.SpecCpuState result = deserializedState?.ToObject<SpecCpuExecutor.SpecCpuState>();
            Assert.AreEqual(true, result.SpecCpuInitialized);
        }

        [Test]
        public async Task SpecCpuExecutorExecutesTheCorrectCommandsWithInstallation()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>
            {
                $"sudo mount -t iso9660 -o ro,exec,loop {this.mockPackage.Path}/speccpu.iso {this.mockFixture.GetPackagePath()}/speccpu_mount",
                $"sudo ./install.sh -f -d {this.mockPackage.Path}",
                $"sudo chmod -R ugo=rwx {this.mockPackage.Path}",
                $"sudo umount {this.mockFixture.GetPackagePath()}/speccpu_mount",
                $"sudo bash runspeccpu.sh \"--config vc-linux-x64.cfg --iterations 2 --copies 71 --threads 71 --tune all --reportable intrate\""
            };

            int processCount = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                Assert.AreEqual(expectedCommands.ElementAt(processCount), $"{exe} {arguments}");
                processCount++;

                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(5, processCount);
        }

        [Test]
        public async Task SpecCpuExecutorExecutesTheCorrectCommandsWithDifferentProfiles()
        {
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.SpecProfile), "fprate" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), false },
            };

            bool commandCalled = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (arguments == $"bash runspeccpu.sh \"--config vc-linux-x64.cfg --iterations 2 --copies 71 --threads 71 --tune base --reportable fprate\"")
                {
                    commandCalled = true;
                }

                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandCalled);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.SpecProfile), "intspeed" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), true }
            };

            commandCalled = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (arguments == $"bash runspeccpu.sh \"--config vc-linux-x64.cfg --iterations 2 --copies 71 --threads 71 --tune all --reportable intspeed\"")
                {
                    commandCalled = true;
                }

                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = exe,
                        Arguments = arguments
                    },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandCalled);
        }

        private class TestSpecCpuExecutor : SpecCpuExecutor
        {
            public TestSpecCpuExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}