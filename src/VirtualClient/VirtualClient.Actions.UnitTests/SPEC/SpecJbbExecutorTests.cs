// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common.Telemetry;
    using global::VirtualClient.Contracts;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class SpecJbbExecutorTests
    {
        private MockFixture mockFixture;

        [Test]
        public void SpecJbbExecutorThrowsIfCannotFindSpecJbbPackage()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Win32NT);
            this.mockFixture.PackageManager.OnGetPackage("specjbb2015").ReturnsAsync(value: null);

            using (TestSpecJbbExecutor specJbbExecutor = new TestSpecJbbExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => specJbbExecutor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public void SpecJbbExecutorThrowsIfCannotFindJdkPackage()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            this.mockFixture.PackageManager.OnGetPackage("javadevelopmentkit").ReturnsAsync(value: null);

            using (TestSpecJbbExecutor specJbbExecutor = new TestSpecJbbExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => specJbbExecutor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public async Task SpecJbbExecutorRunsTheExpectedWorkloadCommandInWindows()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Win32NT);
            // Mocking 100GB of memory
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetTotalSystemMemoryKiloBytes()).Returns(1024 * 1024 * 100);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetSystemCoreCount()).Returns(34);

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            // 87040MB is 85GB
            string expectedCommand = @$"java.exe -XX:+AlwaysPreTouch -XX:+UseLargePages -XX:+UseParallelGC -XX:ParallelGCThreads=34 -Xms87040m -Xmx87040m -Xlog:gc*,gc+ref=debug,gc+phases=debug,gc+age=trace,safepoint:file=gc.log -jar specjbb2015.jar -m composite -ikv";

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

            using (TestSpecJbbExecutor specJbbExecutor = new TestSpecJbbExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specJbbExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task SpecJbbExecutorRunsTheExpectedWorkloadCommandInLinux()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            // Mocking 100GB of memory
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetTotalSystemMemoryKiloBytes()).Returns(1024 * 1024 * 100);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetSystemCoreCount()).Returns(71);
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedCommand = @$"sudo java -XX:+AlwaysPreTouch -XX:+UseLargePages -XX:+UseParallelGC -XX:ParallelGCThreads=71 -Xms87040m -Xmx87040m -Xlog:gc*,gc+ref=debug,gc+phases=debug,gc+age=trace,safepoint:file=gc.log -jar specjbb2015.jar -m composite -ikv";

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

            using (TestSpecJbbExecutor specJbbExecutor = new TestSpecJbbExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specJbbExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task SpecJbbExecutorRunsTheExpectedWorkloadCommandInLinuxWithUserOverwriteCommand()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            // Mocking 100GB of memory
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetTotalSystemMemoryKiloBytes()).Returns(1024 * 1024 * 100);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetSystemCoreCount()).Returns(71);
            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedCommand = @$"sudo java -Flag1 -Flag2 -Xms1234m -XX:ParallelGCThreads=71 -Xmx87040m -Xlog:gc*,gc+ref=debug,gc+phases=debug,gc+age=trace,safepoint:file=gc.log -jar specjbb2015.jar -m composite -ikv";
            this.mockFixture.Parameters["JavaFlags"] = "-Flag1 -Flag2 -Xms1234m";

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

            using (TestSpecJbbExecutor specJbbExecutor = new TestSpecJbbExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specJbbExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        private class TestSpecJbbExecutor : SpecJbbExecutor
        {
            public TestSpecJbbExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
            if (platform == PlatformID.Win32NT)
            {
                this.mockFixture = new MockFixture();
                this.mockFixture.Setup(PlatformID.Win32NT);

                Dictionary<string, IConvertible> specifics = new Dictionary<string, IConvertible>()
                {
                    { PackageMetadata.ExecutablePath, "java.exe" }
                };

                DependencyPath mockJdkPackage = new DependencyPath(
                    "javadevelopmentkit",
                    this.mockFixture.PlatformSpecifics.GetPackagePath("javadevelopmentkit"),
                    metadata: specifics);

                DependencyPath mockJbbPackage = new DependencyPath("specjbb2015", this.mockFixture.PlatformSpecifics.GetPackagePath("specjbb2015"));

                this.mockFixture.PackageManager.OnGetPackage("javadevelopmentkit").ReturnsAsync(mockJdkPackage);
                this.mockFixture.PackageManager.OnGetPackage("specjbb2015").ReturnsAsync(mockJbbPackage);
            }
            else
            {
                this.mockFixture = new MockFixture();
                this.mockFixture.Setup(PlatformID.Unix);

                Dictionary<string, IConvertible> specifics = new Dictionary<string, IConvertible>()
                {
                    { PackageMetadata.ExecutablePath, "java" }
                };

                DependencyPath mockJdkPackage = new DependencyPath(
                    "javadevelopmentkit",
                    this.mockFixture.PlatformSpecifics.GetPackagePath("javadevelopmentkit"),
                    metadata: specifics);

                DependencyPath mockJbbPackage = new DependencyPath("specjbb2015", this.mockFixture.PlatformSpecifics.GetPackagePath("specjbb2015"));

                this.mockFixture.PackageManager.OnGetPackage("javadevelopmentkit").ReturnsAsync(mockJdkPackage);
                this.mockFixture.PackageManager.OnGetPackage("specjbb2015").ReturnsAsync(mockJbbPackage);
            }

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecJbbExecutor.PackageName), "specjbb2015" },
                { nameof(SpecJbbExecutor.JdkPackageName), "javadevelopmentkit" }
            };

            // Remove any mock blob managers so that we do not evaluate the code paths that
            // upload log files by default.
            this.mockFixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();
            this.mockFixture.ProcessManager.OnGetProcess = (id) => null;
        }
    }
}