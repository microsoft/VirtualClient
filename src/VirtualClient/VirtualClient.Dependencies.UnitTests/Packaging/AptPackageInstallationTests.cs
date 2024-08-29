// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Dependencies
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class AptPackageInstallationTests
    {
        private MockFixture fixture;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);

            this.fixture.File.Reset();
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);
            this.fixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.fixture.FileSystem.SetupGet(fs => fs.File).Returns(this.fixture.File.Object);
        }

        [Test]
        public async Task AptPackageInstallationRunsTheExpectedCommandForSinglePackageAndRepo()
        {
            this.fixture.FileSystem.SetupGet(fs => fs.File).Returns(this.fixture.File.Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(AptPackageInstallation.Packages), "pack1" },
                { nameof(AptPackageInstallation.Repositories), "some repo1" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "sudo add-apt-repository \"some repo1\" -y",
                "sudo apt update",
                "sudo apt install pack1 --yes --quiet",
                "sudo apt list pack1"
            };

            int commandExecuted = 0;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestAptPackageInstallation aptPackageInstallation = new TestAptPackageInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await aptPackageInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(4, commandExecuted);
        }

        [Test]
        public async Task AptPackageInstallationRunsTheExpectedCommandForMultiPackageAndRepo()
        {
            this.fixture.FileSystem.SetupGet(fs => fs.File).Returns(this.fixture.File.Object);

            this.fixture.Parameters = new Dictionary<string, IConvertible>()
            {
                { nameof(AptPackageInstallation.Packages), "pack1,pack2,pack3" },
                { nameof(AptPackageInstallation.Repositories), "some repo1,some repo2,some repo3" }
            };

            ProcessStartInfo expectedInfo = new ProcessStartInfo();
            List<string> expectedCommands = new List<string>()
            {
                "sudo add-apt-repository \"some repo1\" -y",
                "sudo add-apt-repository \"some repo2\" -y",
                "sudo add-apt-repository \"some repo3\" -y",
                "sudo apt update",
                "sudo apt install pack1 pack2 pack3 --yes --quiet",
                "sudo apt list pack1",
                "sudo apt list pack2",
                "sudo apt list pack3"
            };

            int commandExecuted = 0;
            this.fixture.ProcessManager.OnCreateProcess = (exe, arguments, workingDir) =>
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

            using (TestAptPackageInstallation aptPackageInstallation = new TestAptPackageInstallation(this.fixture.Dependencies, this.fixture.Parameters))
            {
                await aptPackageInstallation.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);
            }

            Assert.AreEqual(8, commandExecuted);
        }

        private class TestAptPackageInstallation : AptPackageInstallation
        {
            public TestAptPackageInstallation(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
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