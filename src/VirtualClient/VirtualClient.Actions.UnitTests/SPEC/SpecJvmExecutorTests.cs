// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class SpecJvmExecutorTests
    {
        private MockFixture mockFixture;

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void SpecJvmExecutorThrowsIfCannotFindSpecJvmPackage(PlatformID platform)
        {
            this.SetupDefaultBehaviors(PlatformID.Unix);
            this.mockFixture.PackageManager.OnGetPackage("specjvm2008").ReturnsAsync(null as DependencyPath);

            using (TestSpecJvmExecutor specJvmExecutor = new TestSpecJvmExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => specJvmExecutor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT)]
        [TestCase(PlatformID.Unix)]
        public void SpecJvmExecutorThrowsIfCannotFindJdkPackage(PlatformID platform)
        {
            this.SetupDefaultBehaviors(platform);
            this.mockFixture.PackageManager.OnGetPackage("javadevelopmentkit").ReturnsAsync(null as DependencyPath);

            using (TestSpecJvmExecutor specJvmExecutor = new TestSpecJvmExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                Assert.ThrowsAsync<DependencyException>(() => specJvmExecutor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public async Task SpecJvmExecutorRunsTheExpectedWorkloadCommandInWindows()
        {
            this.SetupDefaultBehaviors(PlatformID.Win32NT);

            // 87040MB is 85GB
            string expectedCommand = $@"java.exe -XX:ParallelGCThreads=[0-9]+ -XX:\+UseParallelGC -XX:\+UseAES -XX:\+UseSHA -Xms[0-9]+m -Xmx[0-9]+m -jar SPECjvm2008.jar -ikv -ict test1 test2";

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (Regex.IsMatch($"{exe} {arguments}", expectedCommand))
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

            using (TestSpecJvmExecutor specJvmExecutor = new TestSpecJvmExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specJvmExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        [Test]
        public async Task SpecJvmExecutorRunsTheExpectedWorkloadCommandInLinux()
        {
            this.SetupDefaultBehaviors(PlatformID.Unix);

            string expectedCommand = $@"sudo java -XX:ParallelGCThreads=[0-9]+ -XX:\+UseParallelGC -XX:\+UseAES -XX:\+UseSHA -Xms[0-9]+m -Xmx[0-9]+m -jar SPECjvm2008.jar -ikv -ict test1 test2";

            bool commandExecuted = false;
            this.mockFixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (Regex.IsMatch($"{exe} {arguments}", expectedCommand))
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

            using (TestSpecJvmExecutor specJvmExecutor = new TestSpecJvmExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await specJvmExecutor.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.IsTrue(commandExecuted);
        }

        private void SetupDefaultBehaviors(PlatformID platform)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform);

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>();

            if (platform == PlatformID.Win32NT)
            {
                metadata.Add(PackageMetadata.ExecutablePath, "java.exe");
            }
            else
            {
                metadata.Add(PackageMetadata.ExecutablePath, "java");
            }

            DependencyPath mockJvmPackage = new DependencyPath(
                "specjvm2008",
                this.mockFixture.PlatformSpecifics.GetPackagePath("specjvm2008"));

            DependencyPath mockJdkPackage = new DependencyPath(
                "javadevelopmentkit",
                this.mockFixture.PlatformSpecifics.GetPackagePath("javadevelopmentkit"),
                metadata: metadata);

            // Packages are found on the system by default.
            this.mockFixture.PackageManager.OnGetPackage("specjvm2008").ReturnsAsync(mockJvmPackage);
            this.mockFixture.PackageManager.OnGetPackage("javadevelopmentkit").ReturnsAsync(mockJdkPackage);

            // The Java process exited!
            this.mockFixture.ProcessManager.OnGetProcess = (id) => null;

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(SpecJvmExecutor.PackageName), "specjvm2008" },
                { nameof(SpecJvmExecutor.JdkPackageName), "javadevelopmentkit" },
                { nameof(SpecJvmExecutor.Workloads), "test1,test2" }
            };
        }

        private class TestSpecJvmExecutor : SpecJvmExecutor
        {
            public TestSpecJvmExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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
