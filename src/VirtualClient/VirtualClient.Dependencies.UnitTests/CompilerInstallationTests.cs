// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
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
        public async Task CompilerInstallationRunsTheExpectedWorkloadCommandInLinuxForGcc()
        {
            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
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

            Assert.AreEqual(expectedCommands.Count(), commandExecuted);
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
        public async Task CompilerInstallationInLinuxDefaultsToEmptyIfNoExistingVersion()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>();

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "sudo gcc -dumpversion",
                "sudo add-apt-repository ppa:ubuntu-toolchain-r/test -y",
                "sudo apt update",
                "sudo apt install build-essential gcc g++ gfortran make -y --quiet"
            };

            int commandExecuted = 0;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    commandExecuted++;
                }

                if (exe == "sudo" && arguments.Contains("-dumpversion"))
                {
                    IProcessProxy process = new InMemoryProcess
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = exe,
                            Arguments = arguments
                        },
                        ExitCode = 1,
                        OnStart = () => true,
                        OnHasExited = () => true
                    };
                    return process;
                }
                else
                {
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
                }
            };

            using (TestCompilerInstallation compilerInstallation = new TestCompilerInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await compilerInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.GreaterOrEqual(commandExecuted, expectedCommands.Count());
        }

        [Test]
        public async Task CompilerInstallationInLinuxDefaultsToEmptyIfExistingVersion()
        {
            this.mockFixture.Parameters = new Dictionary<string, IConvertible>();

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "sudo gcc -dumpversion",
                "sudo add-apt-repository ppa:ubuntu-toolchain-r/test -y",
                "sudo apt update"
            };
            List<string> unexpectedCommands = new List<string>()
            {
                "sudo apt install build-essential gcc g++ gfortran make -y --quiet"
            };

            int expectedCommandExecuted = 0;
            int unexpectedCommandExecuted = 0;

            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    expectedCommandExecuted++;
                }

                if (unexpectedCommands.Any(c => c == $"{exe} {arguments}"))
                {
                    unexpectedCommandExecuted++;
                }

                if (exe == "sudo" && arguments.Contains("-dumpversion"))
                {
                    IProcessProxy process = new InMemoryProcess
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = exe,
                            Arguments = arguments
                        },
                        ExitCode = 0,
                        OnStart = () => true,
                        OnHasExited = () => true,
                        StandardOutput = new ConcurrentBuffer(new StringBuilder("10"))
                    };
                    return process;
                }
                else
                {
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
                }
            };

            using (TestCompilerInstallation compilerInstallation = new TestCompilerInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await compilerInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.GreaterOrEqual(expectedCommandExecuted, expectedCommands.Count());
            Assert.AreEqual(unexpectedCommandExecuted, 0);
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