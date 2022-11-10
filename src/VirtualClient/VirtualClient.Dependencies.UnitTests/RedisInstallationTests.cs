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
using VirtualClient.Contracts;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    [TestFixture]
    [Category("Unit")]
    public class RedisInstallationTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);

            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);
        }

        [Test]
        public void RedisInstallationThrowsIfDistroNotSupported()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.SUSE
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Version", "1.4.0" }
            };

            using (TestRedisInstallation testRedisInstallation = new TestRedisInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => testRedisInstallation.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LinuxDistributionNotSupported, exception.Reason);
            }
        }

        [Test]
        public void RedisInstallationThrowsIfPlatformNotSupported()
        {
            this.mockFixture.Setup(PlatformID.Win32NT);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Version", "6.2.1" }
            };

            using (TestRedisInstallation testRedisInstallation = new TestRedisInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => testRedisInstallation.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.PlatformNotSupported, exception.Reason);
            }
        }

        [Test]
        public async Task RedisInstallationExecutesExpectedCommandsOnUbuntu()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                OperationSystemFullName = "TestUbuntu",
                LinuxDistribution = LinuxDistribution.Ubuntu
            };
            this.mockFixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { "Version", "6.2.1" }
            };

            List<string> expectedCommands = new List<string>()
            {
                "sudo wget https://github.com/redis/redis/archive/refs/tags/6.2.1.tar.gz",
                "sudo tar -xvzf 6.2.1.tar.gz",
                "sudo make"
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
                return process;
            };

            using (TestRedisInstallation testRedisInstallation = new TestRedisInstallation(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await testRedisInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(3, commandExecuted);
        }

        private class TestRedisInstallation : RedisInstallation
        {
            public TestRedisInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public VirtualClientComponent InstantiatedInstaller { get; set; }

            public new Task InitializeAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(context, cancellationToken);
            }

            public new Task ExecuteAsync(EventContext context, CancellationToken cancellationToken)
            {
                return base.ExecuteAsync(context, cancellationToken);
            }
        }
    }
}
