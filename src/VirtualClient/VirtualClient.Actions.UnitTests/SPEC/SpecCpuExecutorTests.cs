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
    using System.Text;
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
        private MockFixture mockFixture;
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
                $"sudo mount -t iso9660 -o ro,exec,loop {this.mockPackage.Path}/speccpu.iso {this.mockFixture.GetPackagePath()}/speccpu_mount",
                $"sudo ./install.sh -f -d {this.mockPackage.Path}",
                $"sudo gcc -dumpversion",
                $"sudo chmod -R ugo=rwx {this.mockPackage.Path}",
                $"sudo umount {this.mockFixture.GetPackagePath()}/speccpu_mount",
                $"bash runspeccpu.sh \"--config vc-linux-x64.cfg --iterations 2 --copies 4 --threads 8 --tune all --reportable intrate\""
            };

            int processCount = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                Assert.AreEqual(expectedCommands.ElementAt(processCount), $"{exe} {arguments}");
                processCount++;

                if (exe == "sudo" && arguments == "gcc -dumpversion")
                {
                    return new InMemoryProcess
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = exe,
                            Arguments = arguments
                        },
                        StandardOutput = new ConcurrentBuffer(new StringBuilder("10")),
                        ExitCode = 0,
                        OnStart = () => true,
                        OnHasExited = () => true
                    };
                }
                else
                {
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
                }
            };

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(expectedCommands.Count, processCount);
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
                $"cmd /c runspeccpu.bat --config vc-win-x64.cfg --iterations 2 --copies 4 --threads 8 --tune all --noreportable intrate"
            };

            int processCount = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(processCount, expectedCommands.Count);
        }
        
        [Test]
        public async Task SpecCpuExecutorExecutesTheCorrectCommandsWithSpecificBenchmarksInLinux()
        {
            this.SetupLinux();
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.SpecProfile), "intrate" },
                { nameof(SpecCpuExecutor.Benchmarks), "549.fotonik3d_r" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), true },
                { nameof(SpecCpuExecutor.Threads), 8 },
                { nameof(SpecCpuExecutor.Copies), 4 }
            };

            int coreCount = Environment.ProcessorCount;
            List<string> expectedCommands = new List<string>
            {
                $"sudo mount -t iso9660 -o ro,exec,loop {this.mockPackage.Path}/speccpu.iso {this.mockFixture.GetPackagePath()}/speccpu_mount",
                $"sudo ./install.sh -f -d {this.mockPackage.Path}",
                $"sudo gcc -dumpversion",
                $"sudo chmod -R ugo=rwx {this.mockPackage.Path}",
                $"sudo umount {this.mockFixture.GetPackagePath()}/speccpu_mount",
                $"bash runspeccpu.sh \"--config vc-linux-x64.cfg --iterations 2 --copies 4 --threads 8 --tune all --noreportable 549.fotonik3d_r\""
            };

            int processCount = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                Assert.AreEqual(expectedCommands.ElementAt(processCount), $"{exe} {arguments}");
                processCount++;

                if (exe == "sudo" && arguments == "gcc -dumpversion")
                {
                    return new InMemoryProcess
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = exe,
                            Arguments = arguments
                        },
                        StandardOutput = new ConcurrentBuffer(new StringBuilder("10")),
                        ExitCode = 0,
                        OnStart = () => true,
                        OnHasExited = () => true
                    };
                }
                else
                {
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
                }
            };

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(expectedCommands.Count, processCount);
        }

        [Test]
        public async Task SpecCpuExecutorExecutesTheCorrectCommandsWithDifferentProfilesInLinux()
        {
            this.SetupLinux();
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.SpecProfile), "fprate" },
                { nameof(SpecCpuExecutor.Benchmarks), "fprate" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), false },
            };

            bool commandCalled = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                int coreCount = Environment.ProcessorCount;
                if (arguments == $"runspeccpu.sh \"--config vc-linux-x64.cfg --iterations 2 --copies {coreCount} --threads {coreCount} --tune base --reportable fprate\"")
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
                { nameof(SpecCpuExecutor.Benchmarks), "intspeed" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), true }
            };

            commandCalled = false;
            int coreCount = Environment.ProcessorCount;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (arguments == $"runspeccpu.sh \"--config vc-linux-x64.cfg --iterations 2 --copies {coreCount} --threads {coreCount} --tune all --reportable intspeed\"")
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

            // 
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.SpecProfile), "intspeed" },
                { nameof(SpecCpuExecutor.Benchmarks), "intspeed" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.Iterations), 1 },
                { nameof(SpecCpuExecutor.RunPeak), true }
            };
            commandCalled = false;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (arguments == $"runspeccpu.sh \"--config vc-linux-x64.cfg --iterations 1 --copies {coreCount} --threads {coreCount} --tune all --noreportable intspeed\"")
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

        [Test]
        public async Task SpecCpuExecutorExecutesTheCorrectCommandsWithDifferentProfilesInWindows()
        {
            this.SetupWindows();
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.SpecProfile), "fprate" },
                { nameof(SpecCpuExecutor.Benchmarks), "fprate" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), false },
            };

            bool commandCalled = false;
            int coreCount = Environment.ProcessorCount;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandCalled);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.SpecProfile), "intspeed" },
                { nameof(SpecCpuExecutor.Benchmarks), "intspeed" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), true }
            };

            commandCalled = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(EventContext.None, CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandCalled);
        }

        [Test]
        public async Task SpecCpuExecutorAppliesGcc15WorkaroundWhenGccVersionIs15OrGreaterOnLinux()
        {
            this.SetupLinux();

            string writtenConfigText = null;
            this.mockFixture.File.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) => writtenConfigText = content)
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (exe == "sudo" && arguments == "gcc -dumpversion")
                {
                    return new InMemoryProcess
                    {
                        StartInfo = new ProcessStartInfo { FileName = exe, Arguments = arguments },
                        StandardOutput = new ConcurrentBuffer(new StringBuilder("15")),
                        ExitCode = 0,
                        OnStart = () => true,
                        OnHasExited = () => true
                    };
                }

                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo { FileName = exe, Arguments = arguments },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsNotNull(writtenConfigText);
            Assert.IsTrue(writtenConfigText.Contains("%define GCCge15"), "Config should contain '%define GCCge15' when GCC version is 15 or greater.");
            Assert.IsTrue(writtenConfigText.Contains("%define GCCge10"), "Config should also contain '%define GCCge10' when GCC version is 15 or greater.");
            Assert.IsFalse(writtenConfigText.Contains("$Gcc15Workaround$"), "Placeholder '$Gcc15Workaround$' should be replaced.");
            Assert.IsFalse(writtenConfigText.Contains("$Gcc10Workaround$"), "Placeholder '$Gcc10Workaround$' should be replaced.");
        }

        [Test]
        public async Task SpecCpuExecutorDoesNotApplyGcc15WorkaroundWhenGccVersionIsLessThan15OnLinux()
        {
            this.SetupLinux();

            string writtenConfigText = null;
            this.mockFixture.File.Setup(f => f.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) => writtenConfigText = content)
                .Returns(Task.CompletedTask);

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (exe == "sudo" && arguments == "gcc -dumpversion")
                {
                    return new InMemoryProcess
                    {
                        StartInfo = new ProcessStartInfo { FileName = exe, Arguments = arguments },
                        StandardOutput = new ConcurrentBuffer(new StringBuilder("10")),
                        ExitCode = 0,
                        OnStart = () => true,
                        OnHasExited = () => true
                    };
                }

                return new InMemoryProcess
                {
                    StartInfo = new ProcessStartInfo { FileName = exe, Arguments = arguments },
                    ExitCode = 0,
                    OnStart = () => true,
                    OnHasExited = () => true
                };
            };

            using (TestSpecCpuExecutor specCpuExecutor = new TestSpecCpuExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specCpuExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsNotNull(writtenConfigText);
            Assert.IsFalse(writtenConfigText.Contains("%define GCCge15"), "Config should NOT contain '%define GCCge15' when GCC version is less than 15.");
            Assert.IsTrue(writtenConfigText.Contains("%define GCCge10"), "Config should contain '%define GCCge10' when GCC version is 10.");
            Assert.IsFalse(writtenConfigText.Contains("$Gcc15Workaround$"), "Placeholder '$Gcc15Workaround$' should be replaced.");
            Assert.IsFalse(writtenConfigText.Contains("$Gcc10Workaround$"), "Placeholder '$Gcc10Workaround$' should be replaced.");
        }

        private void SetupLinux()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("SPECcpu", this.mockFixture.PlatformSpecifics.GetPackagePath("speccpu", "1.1.8"));

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.mockFixture.Directory.Setup(dir => dir.GetFiles(It.IsAny<string>(), "*.iso", It.IsAny<SearchOption>()))
                .Returns(new string[] { this.mockFixture.Combine(this.mockPackage.Path, "speccpu.iso") });

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            string mockProfileText = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SPEC", "mockspeccpu.cfg"));
            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockProfileText);
            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);
            this.mockFixture.FileInfo.Setup(file => file.New(It.IsAny<string>()))
                .Returns(new Mock<IFileInfo>().Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.SpecProfile), "intrate" },
                { nameof(SpecCpuExecutor.Benchmarks), "intrate" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), true },
                { nameof(SpecCpuExecutor.Threads), 8 },
                { nameof(SpecCpuExecutor.Copies), 4 }
            };
        }

        private void SetupWindows()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockPackage = new DependencyPath("SPECcpu", this.mockFixture.PlatformSpecifics.GetPackagePath("speccpu", "1.1.8"));

            this.mockFixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.mockFixture.Directory.Setup(dir => dir.GetFiles(It.IsAny<string>(), "*.iso", It.IsAny<SearchOption>()))
                .Returns(new string[] { this.mockFixture.Combine(this.mockPackage.Path, "speccpu.iso") });

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            string mockProfileText = File.ReadAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "SPEC", "mockspeccpu.cfg"));
            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(mockProfileText);
            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);
            this.mockFixture.FileInfo.Setup(file => file.New(It.IsAny<string>()))
                .Returns(new Mock<IFileInfo>().Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecCpuExecutor.SpecProfile), "intrate" },
                { nameof(SpecCpuExecutor.Benchmarks), "intrate" },
                { nameof(SpecCpuExecutor.PackageName), "speccpu" },
                { nameof(SpecCpuExecutor.RunPeak), true },
                { nameof(SpecCpuExecutor.Threads), 8 },
                { nameof(SpecCpuExecutor.Copies), 4 }
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