// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class CompilerInstallationTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);
        }

        [Test]
        public void CompilerInstallationThrowsForUnsupportedCompiler()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CompilerInstallation.CompilerName), "icc" },
                { nameof(CompilerInstallation.CompilerVersion), "123" }
            };

            using (TestCompilerInstallation compilerInstallation = new TestCompilerInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<NotSupportedException>(() => compilerInstallation.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public async Task CompilerInstallationRunsTheExpectedWorkloadCommandInLinuxForGcc()
        {
            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CompilerInstallation.CompilerName), "gcc" },
                { nameof(CompilerInstallation.CompilerVersion), "123" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "sudo update-alternatives --remove-all gcc",
                "sudo update-alternatives --remove-all gfortran",
                "sudo add-apt-repository ppa:ubuntu-toolchain-r/test -y",
                "sudo apt update",
                "sudo apt install build-essential gcc-123 g++-123 gfortran-123 -y --quiet",
                "sudo update-alternatives --install /usr/bin/gcc gcc /usr/bin/gcc-123 1230 " +
                    $"--slave /usr/bin/g++ g++ /usr/bin/g++-123 " +
                    $"--slave /usr/bin/gcov gcov /usr/bin/gcov-123 " +
                    $"--slave /usr/bin/gcc-ar gcc-ar /usr/bin/gcc-ar-123 " +
                    $"--slave /usr/bin/gcc-ranlib gcc-ranlib /usr/bin/gcc-ranlib-123 " +
                    $"--slave /usr/bin/gfortran gfortran /usr/bin/gfortran-123",
                "sudo update-alternatives --remove-all cpp",
                "sudo update-alternatives --install /usr/bin/cpp cpp /usr/bin/cpp-123 1230",
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess
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
                process.StandardOutput.AppendLine("gcc (Ubuntu 10.3.0-1ubuntu1~20.04) 123.3.0");
                process.StandardOutput.AppendLine("cc (Ubuntu 10.3.0-1ubuntu1~20.04) 123.3.0");
                return process;
            };

            using (TestCompilerInstallation compilerInstallation = new TestCompilerInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await compilerInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(9, commandExecuted);
        }

        [Test]
        [TestCase(Architecture.X64)]
        [TestCase(Architecture.Arm64)]
        public async Task CompilerInstallationRunsTheExpectedCommandForCharmPlusPlusOnLinux(Architecture architecture)
        {
            this.mockFixture.Setup(PlatformID.Unix, architecture);

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CompilerInstallation.CompilerName), "charm++" },
                { nameof(CompilerInstallation.CompilerVersion), "6.5.0" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "sudo wget https://charm.cs.illinois.edu/distrib/charm-6.5.0.tar.gz -O charm.tar.gz",
                "sudo tar -xzf charm.tar.gz",
                "sudo ./build charm++ netlrts-linux-x86_64 --with-production -j4",
                "sudo ./build charm++ netlrts-linux-arm8 --with-production -j4"
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess
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
                process.StandardOutput.AppendLine("gcc (Ubuntu 10.3.0-1ubuntu1~20.04) 123.3.0");
                process.StandardOutput.AppendLine("cc (Ubuntu 10.3.0-1ubuntu1~20.04) 123.3.0");
                return process;
            };

            using (TestCompilerInstallation compilerInstallation = new TestCompilerInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await compilerInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(3, commandExecuted);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("python3")]
        public async Task CompilerInstallationRunsTheExpectedWorkloadCommandInWindowsForGcc(string packages)
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.SetEnvironmentVariable("ChocolateyToolsLocation", this.mockFixture.Combine("C:", "tools"));

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CompilerInstallation.CompilerName), "gcc" },
                { nameof(CompilerInstallation.CygwinPackages), packages }
            };

            string cygwinPath = this.mockFixture.PlatformSpecifics.Combine("C:", "tools", "cygwin"); 
            string cygwinInstallerPath = this.mockFixture.PlatformSpecifics.Combine(cygwinPath, "cygwinsetup.exe");
            ProcessStartInfo expectedInfo = new ProcessStartInfo();

            List<string> expectedCommands = new List<string>()
            {
                $@"{cygwinInstallerPath} --quiet-mode --root {cygwinPath} --site http://cygwin.mirror.constant.com --packages make,cmake,python3",
                $@"{cygwinInstallerPath} --quiet-mode --root {cygwinPath} --site http://cygwin.mirror.constant.com --packages make,cmake"
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => $"{c}" ==  $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess
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

                return process;
            };

            using (TestCompilerInstallation compilerInstallation = new TestCompilerInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await compilerInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(1, commandExecuted);
        }

        [Test]
        public async Task CompilerInstallationInLinuxDefaultsToGcc10()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>();

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "sudo update-alternatives --remove-all gcc",
                "sudo update-alternatives --remove-all gfortran",
                "sudo add-apt-repository ppa:ubuntu-toolchain-r/test -y",
                "sudo apt update",
                "sudo apt install build-essential gcc-10 g++-10 gfortran-10 -y --quiet",
                "sudo update-alternatives --install /usr/bin/gcc gcc /usr/bin/gcc-10 100 " +
                    $"--slave /usr/bin/g++ g++ /usr/bin/g++-10 " +
                    $"--slave /usr/bin/gcov gcov /usr/bin/gcov-10 " +
                    $"--slave /usr/bin/gcc-ar gcc-ar /usr/bin/gcc-ar-10 " +
                    $"--slave /usr/bin/gcc-ranlib gcc-ranlib /usr/bin/gcc-ranlib-10 " +
                    $"--slave /usr/bin/gfortran gfortran /usr/bin/gfortran-10",
                "sudo update-alternatives --remove-all cpp",
                "sudo update-alternatives --install /usr/bin/cpp cpp /usr/bin/cpp-10 100",
            };

            int commandExecuted = 0;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                IProcessProxy process = new InMemoryProcess
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
                process.StandardOutput.AppendLine("gcc (Ubuntu 10.3.0-1ubuntu1~20.04) 10.3.0");
                process.StandardOutput.AppendLine("cc (Ubuntu 10.3.0-1ubuntu1~20.04) 10.3.0");
                return process;
            };

            using (TestCompilerInstallation compilerInstallation = new TestCompilerInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await compilerInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(9, commandExecuted);
        }

        [Test]
        public async Task CompilerInstallationRunsTheExpectedWorkloadCommandInLinuxForAocc()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(CompilerInstallation.CompilerName), "Aocc" },
                { nameof(CompilerInstallation.CompilerVersion), "5.6.7" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "sudo wget https://developer.amd.com/wordpress/media/files/aocc-compiler-5.6.7.tar",
                "sudo tar -xvf aocc-compiler-5.6.7.tar",
                "sudo bash install.sh"
            };

            int commandExecuted = 0;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
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

            using (TestCompilerInstallation compilerInstallation = new TestCompilerInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await compilerInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(3, commandExecuted);
        }

        [Test]
        [TestCase("(Ubuntu 7.5.0-3ubuntu1~18.04) 7.5.0", "7")]
        [TestCase("(Ubuntu 7.5.0-3ubuntu1~18.04) 7.5.0", "7.5.0")]
        [TestCase("(Ubuntu 9.4.0-3ubuntu1~18.04) 9.4.0", "9")]
        [TestCase("(Ubuntu 9.4.0-3ubuntu1~18.04) 9.4.0", "9.4.0")]
        [TestCase("(Ubuntu 10.3.0-3ubuntu1~18.04) 10.3.0", "10")]
        [TestCase("(Ubuntu 10.3.0-3ubuntu1~18.04) 10.3.0", "10.3.0")]
        [TestCase("(Ubuntu 9.4.0-1ubuntu1~20.04) 9.4.0", "9")]
        [TestCase("(Ubuntu 9.4.0-1ubuntu1~20.04) 9.4.0", "9.4.0")]
        [TestCase("(Ubuntu 10.3.0-1ubuntu1~20.04) 10.3.0", "10")]
        [TestCase("(Ubuntu 10.3.0-1ubuntu1~20.04) 10.3.0", "10.3.0")]
        public void CompilerInstallationConfirmsTheInstalledVersionOfGCCAsExpected(string versionOutput, string expectedVersion)
        {
            using (TestCompilerInstallation compilerInstallation = new TestCompilerInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                compilerInstallation.CompilerName = "gcc";
                compilerInstallation.CompilerVersion = expectedVersion;

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    process.StandardOutput.AppendLine($"{process.StartInfo.FileName} {versionOutput}");
                };

                Assert.IsTrue(compilerInstallation.ConfirmGccVersionInstalledAsync(CancellationToken.None)
                    .GetAwaiter().GetResult());
            }
        }

        [Test]
        public void CompilerInstallationThrowsIfGccVersionIsNotConfirmed()
        {
            using (TestCompilerInstallation compilerInstallation = new TestCompilerInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                compilerInstallation.CompilerName = "gcc";
                compilerInstallation.CompilerVersion = "10";

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    process.StandardOutput.AppendLine($"{process.StartInfo.FileName} (Ubuntu 9.4.0-3ubuntu1~18.04) 9.4.0");
                };

                Assert.ThrowsAsync<DependencyException>(() => compilerInstallation.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public void CompilerInstallationConfirmsExpectedCompilersForGcc()
        {
            using (TestCompilerInstallation compilerInstallation = new TestCompilerInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                compilerInstallation.CompilerName = "gcc";
                compilerInstallation.CompilerVersion = "9";

                Dictionary<string, bool> compilers = new Dictionary<string, bool>()
                {
                    { "gcc", false },
                    { "cc", false }
                };

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    process.StandardOutput.AppendLine($"{process.StartInfo.FileName} (Ubuntu 9.4.0-3ubuntu1~18.04) 9.4.0");
                    compilers[process.StartInfo.FileName] = true;
                };

                Assert.IsTrue(compilerInstallation.ConfirmGccVersionInstalledAsync(CancellationToken.None)
                    .GetAwaiter().GetResult());
                Assert.IsTrue(compilers.Values.All(installed => installed));
            }
        }

        private class TestCompilerInstallation : CompilerInstallation
        {
            public TestCompilerInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }

            public new Task<bool> ConfirmGccVersionInstalledAsync(CancellationToken cancellationToken)
            {
                return base.ConfirmGccVersionInstalledAsync(cancellationToken);
            }
        }
    }
}