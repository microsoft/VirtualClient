// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using AutoFixture;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient.Common;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts; 
    using static VirtualClient.Actions.RallyExecutor;

    [TestFixture]
    [Category("Unit")]
    public class RallyExecutorTests
    {
        private MockFixture fixture;
        private string packagePath;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new MockFixture();
        }

        [Test]
        public void RallyExecutorThrowsOnUnsupportedDistroAsync()
        {
            this.fixture.Setup(PlatformID.Unix);

            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.SUSE
            };

            this.fixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            using (TestRallyExecutor RallyExecutor = new TestRallyExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => RallyExecutor.ExecuteAsync(CancellationToken.None));

                Assert.AreEqual(ErrorReason.LinuxDistributionNotSupported, exception.Reason);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        public async Task RallyExecutorInstallsWorkloadPackageUnix(PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(RallyExecutor.PackageName), "esrally" },
            };

            DependencyPath mockPackage = new DependencyPath("esrally", this.fixture.PlatformSpecifics.GetPackagePath("esrally"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(mockPackage);

            this.packagePath = this.fixture.ToPlatformSpecificPath(mockPackage, platform, architecture).Path;

            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

            IEnumerable<Disk> disks = this.fixture.CreateDisks(platform, withVolume: true);
            this.fixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => disks);

            string expectedCommand = $"python3 {this.packagePath}/install.py";
            bool commandExecuted = false;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                if (Equals($"{exe} {arguments}", expectedCommand))
                {
                    commandExecuted = true;
                }

                Assert.IsTrue(commandExecuted);

                InMemoryProcess process = new InMemoryProcess
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

            using (TestRallyExecutor RallyExecutor = new TestRallyExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await RallyExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);
            }
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        public async Task RallyExecutorDoesNotInstallsWorkloadPackageOnInstalledStateUnix(PlatformID platform, Architecture architecture)
        {
            this.fixture.Setup(platform);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(RallyExecutor.PackageName), "esrally" },
            };

            this.fixture.StateManager.OnGetState().ReturnsAsync(JObject.FromObject(new RallyExecutor.RallyState()
            {
                RallyInitialized = true
            }));

            DependencyPath mockPackage = new DependencyPath("esrally", this.fixture.PlatformSpecifics.GetPackagePath("esrally"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(mockPackage);

            this.packagePath = this.fixture.ToPlatformSpecificPath(mockPackage, platform, architecture).Path;

            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

            IEnumerable<Disk> disks = this.fixture.CreateDisks(platform, withVolume: true);
            this.fixture.DiskManager.Setup(mgr => mgr.GetDisksAsync(It.IsAny<CancellationToken>())).ReturnsAsync(() => disks);

            int commandsExecuted = 0;

            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
            {
                commandsExecuted++;

                InMemoryProcess process = new InMemoryProcess
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

            using (TestRallyExecutor RallyExecutor = new TestRallyExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await RallyExecutor.OnInitialize.Invoke(EventContext.None, CancellationToken.None);
            }

            Assert.AreEqual(0, commandsExecuted);
        }

        private class TestRallyExecutor : RallyExecutor
        {
            public TestRallyExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
                : base(dependencies, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;
        }
    }
}
