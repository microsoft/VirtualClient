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
    using VirtualClient.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Common.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class HpcgExecutorTests
    {
        private MockFixture mockFixture;

        [Test]
        public void HpcgExecutorThrowsIfCannotFindSpackPackage()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            this.mockFixture.PackageManager.OnGetPackage("spack").ReturnsAsync(value: null);

            using (TestHpcgExecutor HpcgExecutor = new TestHpcgExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => HpcgExecutor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public async Task HpcgExecutorMakeRunShellExecutable()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);

            // Mocking 100GB of memory
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1024 * 1024 * 100));

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedCommand = @$"sudo chmod +x ""{this.mockFixture.GetPackagePath()}/hpcg/runhpcg.sh""";

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommand == $"{exe} {arguments}")
                {
                    commandExecuted = true;
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

            using (TestHpcgExecutor HpcgExecutor = new TestHpcgExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await HpcgExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task HpcgExecutorRunsRunHpcgShell()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);

            // Mocking 100GB of memory
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1024 * 1024 * 100));

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedCommand = @$"sudo bash {this.mockFixture.GetPackagePath()}/hpcg/runhpcg.sh";

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (expectedCommand == $"{exe} {arguments}")
                {
                    commandExecuted = true;
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

            using (TestHpcgExecutor HpcgExecutor = new TestHpcgExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await HpcgExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task HpcgExecutorCalculatesRightSizeAndWriteHpcgDatFile()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);

            string datFilePath = $"{this.mockFixture.GetPackagePath()}/hpcg/hpcg.dat";

            // Mocking 100GB of memory
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1024 * 1024 * 100));

            this.mockFixture.File.Setup(f => f.Exists(datFilePath))
                .Returns(false);

            // Math.Cbrt(100GB * 1024 * 1024 * 0.25 / 3.4) / 8) * 8
            string expectedFile = "HPCG benchmark input file" + Environment.NewLine
                    + "HPC Benchmarking team, Microsoft Azure" + Environment.NewLine
                    + $"200 200 200" + Environment.NewLine
                    + $"1800";

            bool fileWritten = false;
            this.mockFixture.File.OnWriteAllTextAsync(datFilePath)
                .Callback((string filePath, string context, CancellationToken token) =>
                {
                    if (string.Equals(expectedFile, context))
                    {
                        fileWritten = true;
                    }
                });

            using (TestHpcgExecutor HpcgExecutor = new TestHpcgExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await HpcgExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(fileWritten);
        }

        [Test]
        public async Task HpcgExecutorWritesExpectedRunShellFile()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            string runShellPath = $"{this.mockFixture.GetPackagePath()}/hpcg/runhpcg.sh";

            // Mocking 100GB of memory
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1024 * 1024 * 100));

            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetCpuInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CpuInfo("cpu", "description", 7, 8, 9, 10, false));

            // First time set the file to not exist so it writes the file. Second return true so that the code will make the shell executable.
            this.mockFixture.File.SetupSequence(f => f.Exists(runShellPath))
                .Returns(false)
                .Returns(true);

            string expectedFile = $". {this.mockFixture.GetPackagePath()}/JavaDevelopmentKit/share/spack/setup-env.sh" + Environment.NewLine
                    + "spack install --reuse -n -y hpcg@9.8 %gcc +openmp ^openmpi@6.66.666" + Environment.NewLine
                    + $"spack load hpcg@9.8 %gcc ^openmpi@6.66.666" + Environment.NewLine
                    + $"mpirun --np 7 --use-hwthread-cpus --allow-run-as-root xhpcg";

            bool fileWritten = false;
            this.mockFixture.File.OnWriteAllTextAsync(runShellPath)
                .Callback((string filePath, string context, CancellationToken token) =>
                {
                    if (string.Equals(expectedFile, context))
                    {
                        fileWritten = true;
                    }
                });

            using (TestHpcgExecutor HpcgExecutor = new TestHpcgExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await HpcgExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(fileWritten);
        }

        private class TestHpcgExecutor : HpcgExecutor
        {
            public TestHpcgExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }
        }

        private void SetupDefaultMockBehaviors(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);

            Dictionary<string, IConvertible> specifics = new Dictionary<string, IConvertible>()
            {
                { PackageMetadata.ExecutablePath, "spack" }
            };

            DependencyPath mockSpackPackage = new DependencyPath(
                "JavaDevelopmentKit",
                this.mockFixture.PlatformSpecifics.GetPackagePath("JavaDevelopmentKit"),
                metadata: specifics);

            DependencyPath mockJbbPackage = new DependencyPath("spack", this.mockFixture.PlatformSpecifics.GetPackagePath("Hpcg2015"));

            this.mockFixture.PackageManager.OnGetPackage("spack").ReturnsAsync(mockSpackPackage);

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "HpcgVersion", "9.8" },
                { "OpenMpiVersion", "6.66.666" },
                { "PackageName", "hpcg" },
                { "SpackPackageName", "spack" }
            };
        }
    }
}
