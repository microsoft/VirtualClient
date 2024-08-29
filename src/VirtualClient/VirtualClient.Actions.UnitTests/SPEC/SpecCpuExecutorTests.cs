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
    using VirtualClient.Common;

    [TestFixture]
    [Category("Unit")]
    public class SpecCpuExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;

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
        public async Task SpecCpuExecutorExecutesTheCorrectCommandsWithInstallationInLinux()
        {
            this.SetupLinux();
            ProcessStartInfo expectedInfo = new ProcessStartInfo();

            int coreCount = Environment.ProcessorCount;
            List<string> expectedCommands = new List<string>
            {
                $"sudo mount -t iso9660 -o ro,exec,loop {this.mockPackage.Path}/speccpu.iso {this.fixture.GetPackagePath()}/speccpu_mount",
                $"sudo ./install.sh -f -d {this.mockPackage.Path}",
                $"sudo chmod -R ugo=rwx {this.mockPackage.Path}",
                $"sudo umount {this.fixture.GetPackagePath()}/speccpu_mount",
                $"sudo bash runspeccpu.sh \"--config vc-linux-x64.cfg --iterations 2 --copies {coreCount} --threads {coreCount} --tune all --reportable intrate\""
            };

            int processCount = 0;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(5, processCount);
        }

        [Test]
        public async Task SpecCpuExecutorExecutesTheCorrectCommandsWithInstallationInWindows()
        {
            this.SetupWindows();
            ProcessStartInfo expectedInfo = new ProcessStartInfo();

            int coreCount = Environment.ProcessorCount;
            List<string> expectedCommands = new List<string>
            {
                $"powershell -Command \"Mount-DiskImage -ImagePath {this.mockPackage.Path}\\speccpu.iso\"",
                $"powershell -Command \"(Get-DiskImage -ImagePath {this.mockPackage.Path}\\speccpu.iso| Get-Volume).DriveLetter\"",
                $"cmd /c echo 1 | X:\\install.bat {this.mockPackage.Path}",
                $"powershell -Command \"Dismount-DiskImage -ImagePath {this.mockPackage.Path}\\speccpu.iso\"",
                $"cmd /c runspeccpu.bat --config vc-win-x64.cfg --iterations 2 --copies {coreCount} --threads {coreCount} --tune all --noreportable intrate"
            };

            int processCount = 0;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                Assert.AreEqual(expectedCommands.ElementAt(processCount), $"{exe} {arguments}");
                processCount++;
                ConcurrentBuffer output = new ConcurrentBuffer();
                if (arguments.Contains("Get-DiskImage"))
                {
                    output.Append("X");
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
                    OnHasExited = () => true,
                    StandardOutput = output
                };
            };

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(5, processCount);
        }

        [Test]
        public async Task SpecCpuExecutorExecutesTheCorrectCommandsWithDifferentProfilesInLinux()
        {
            this.SetupLinux();
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.SpecProfile), "fprate" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), false },
            };

            bool commandCalled = false;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                int coreCount = Environment.ProcessorCount;
                if (arguments == $"bash runspeccpu.sh \"--config vc-linux-x64.cfg --iterations 2 --copies {coreCount} --threads {coreCount} --tune base --reportable fprate\"")
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

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandCalled);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.SpecProfile), "intspeed" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), true }
            };

            commandCalled = false;
            int coreCount = Environment.ProcessorCount;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (arguments == $"bash runspeccpu.sh \"--config vc-linux-x64.cfg --iterations 2 --copies {coreCount} --threads {coreCount} --tune all --reportable intspeed\"")
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

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandCalled);
        }

        [Test]
        public async Task SpecCpuExecutorExecutesTheCorrectCommandsWithDifferentProfilesInWindows()
        {
            this.SetupWindows();
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.SpecProfile), "fprate" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), false },
            };

            bool commandCalled = false;
            int coreCount = Environment.ProcessorCount;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (arguments == $"/c runspeccpu.bat --config vc-win-x64.cfg --iterations 2 --copies {coreCount} --threads {coreCount} --tune base --noreportable fprate")
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

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandCalled);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.SpecProfile), "intspeed" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), true }
            };

            commandCalled = false;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (arguments == $"/c runspeccpu.bat --config vc-win-x64.cfg --iterations 2 --copies {coreCount} --threads {coreCount} --tune all --noreportable intspeed")
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

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandCalled);
        }

        private void SetupLinux()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("SPECcpu", this.fixture.PlatformSpecifics.GetPackagePath("speccpu", "1.1.8"));

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.fixture.Directory.Setup(dir => dir.GetFiles(It.IsAny<string>(), "*.iso", It.IsAny<SearchOption>()))
                .Returns(new string[] { this.fixture.Combine(this.mockPackage.Path, "speccpu.iso") });

            this.fixture.File.Reset();
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            string mockProfileText = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SPEC", "mockspeccpu.cfg"));
            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockProfileText);
            this.fixture.FileSystem.SetupGet(fs => fs.File).Returns(this.fixture.File.Object);
            this.fixture.FileInfo.Setup(file => file.New(It.IsAny<string>()))
                .Returns(new Mock<IFileInfo>().Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.CompilerVersion), "10" },
                { nameof(SpecCpuExecutor.SpecProfile), "intrate" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), true }
            };
        }

        private void SetupWindows()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Win32NT);
            this.mockPackage = new DependencyPath("SPECcpu", this.fixture.PlatformSpecifics.GetPackagePath("speccpu", "1.1.8"));

            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.fixture.Directory.Setup(dir => dir.GetFiles(It.IsAny<string>(), "*.iso", It.IsAny<SearchOption>()))
                .Returns(new string[] { this.fixture.Combine(this.mockPackage.Path, "speccpu.iso") });

            this.fixture.File.Reset();
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            string mockProfileText = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SPEC", "mockspeccpu.cfg"));
            this.fixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockProfileText);
            this.fixture.FileSystem.SetupGet(fs => fs.File).Returns(this.fixture.File.Object);
            this.fixture.FileInfo.Setup(file => file.New(It.IsAny<string>()))
                .Returns(new Mock<IFileInfo>().Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.CompilerVersion), "10" },
                { nameof(SpecCpuExecutor.SpecProfile), "intrate" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), true }
            };
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