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
        private MockFixture fixture;

        [Test]
        public void SpecJbbExecutorThrowsIfCannotFindSpecJbbPackage()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Win32NT);
            this.fixture.PackageManager.OnGetPackage("specjbb2015").ReturnsAsync(value: null);

            using (TestSpecJbbExecutor specJbbExecutor = new TestSpecJbbExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => specJbbExecutor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public void SpecJbbExecutorThrowsIfCannotFindJdkPackage()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            this.fixture.PackageManager.OnGetPackage("javadevelopmentkit").ReturnsAsync(value: null);

            using (TestSpecJbbExecutor specJbbExecutor = new TestSpecJbbExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => specJbbExecutor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public async Task SpecJbbExecutorRunsTheExpectedWorkloadCommandInWindows()
        {
            this.SetupDefaultMockBehaviors(PlatformID.Win32NT);
            // Mocking 100GB of memory
            this.fixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1024 * 1024 * 100));

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            // 87040MB is 85GB
            string expectedCommand =
                @$"java.exe -XX:+AlwaysPreTouch -XX:+UseLargePages -XX:+UseParallelGC -XX:ParallelGCThreads={Environment.ProcessorCount} -Xms87040m -Xmx87040m " +
                $"-Xlog:gc*,gc+ref=debug,gc+phases=debug,gc+age=trace,safepoint:file=gc.log -jar specjbb2015.jar -m composite -ikv";

            bool commandExecuted = false;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestSpecJbbExecutor specJbbExecutor = new TestSpecJbbExecutor(this.fixture.Dependencies, this.fixture.Parameters))
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
            this.fixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1024 * 1024 * 100));

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedCommand = 
                @$"sudo java -XX:+AlwaysPreTouch -XX:+UseLargePages -XX:+UseParallelGC -XX:ParallelGCThreads={Environment.ProcessorCount} -Xms87040m -Xmx87040m " +
                $"-Xlog:gc*,gc+ref=debug,gc+phases=debug,gc+age=trace,safepoint:file=gc.log -jar specjbb2015.jar -m composite -ikv";

            bool commandExecuted = false;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestSpecJbbExecutor specJbbExecutor = new TestSpecJbbExecutor(this.fixture.Dependencies, this.fixture.Parameters))
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
            this.fixture.SystemManagement.Setup(mgr => mgr.GetMemoryInfoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(new MemoryInfo(1024 * 1024 * 100));

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            string expectedCommand = 
                @$"sudo java -Flag1 -Flag2 -Xms1234m -XX:ParallelGCThreads={Environment.ProcessorCount} -Xmx87040m " +
                $"-Xlog:gc*,gc+ref=debug,gc+phases=debug,gc+age=trace,safepoint:file=gc.log -jar specjbb2015.jar -m composite -ikv";

            this.fixture.Parameters["JavaFlags"] = "-Flag1 -Flag2 -Xms1234m";

            bool commandExecuted = false;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestSpecJbbExecutor specJbbExecutor = new TestSpecJbbExecutor(this.fixture.Dependencies, this.fixture.Parameters))
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
                this.fixture = new MockFixture();
                this.fixture.Setup(PlatformID.Win32NT);

                Dictionary<string, IConvertible> specifics = new Dictionary<string, IConvertible>()
                {
                    { PackageMetadata.ExecutablePath, "java.exe" }
                };

                DependencyPath mockJdkPackage = new DependencyPath(
                    "javadevelopmentkit",
                    this.fixture.PlatformSpecifics.GetPackagePath("javadevelopmentkit"),
                    metadata: specifics);

                DependencyPath mockJbbPackage = new DependencyPath("specjbb2015", this.fixture.PlatformSpecifics.GetPackagePath("specjbb2015"));

                this.fixture.PackageManager.OnGetPackage("javadevelopmentkit").ReturnsAsync(mockJdkPackage);
                this.fixture.PackageManager.OnGetPackage("specjbb2015").ReturnsAsync(mockJbbPackage);
            }
            else
            {
                this.fixture = new MockFixture();
                this.fixture.Setup(PlatformID.Unix);

                Dictionary<string, IConvertible> specifics = new Dictionary<string, IConvertible>()
                {
                    { PackageMetadata.ExecutablePath, "java" }
                };

                DependencyPath mockJdkPackage = new DependencyPath(
                    "javadevelopmentkit",
                    this.fixture.PlatformSpecifics.GetPackagePath("javadevelopmentkit"),
                    metadata: specifics);

                DependencyPath mockJbbPackage = new DependencyPath("specjbb2015", this.fixture.PlatformSpecifics.GetPackagePath("specjbb2015"));

                this.fixture.PackageManager.OnGetPackage("javadevelopmentkit").ReturnsAsync(mockJdkPackage);
                this.fixture.PackageManager.OnGetPackage("specjbb2015").ReturnsAsync(mockJbbPackage);
            }

            this.fixture.File.Reset();
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.FileSystem.SetupGet(fs => fs.File).Returns(this.fixture.File.Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecJbbExecutor.PackageName), "specjbb2015" },
                { nameof(SpecJbbExecutor.JdkPackageName), "javadevelopmentkit" }
            };

            // Remove any mock blob managers so that we do not evaluate the code paths that
            // upload log files by default.
            this.fixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();
            this.fixture.ProcessManager.OnGetProcess = (id) => null;
        }
    }
}