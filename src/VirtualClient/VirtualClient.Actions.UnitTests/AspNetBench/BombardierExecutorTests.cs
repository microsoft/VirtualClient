// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class BombardierExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPackage;

        [SetUp]
        public void SetupDefaults()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Unix);

            this.mockPackage = new DependencyPath("bombardier", this.mockFixture.GetPackagePath("bombardier"));

            this.mockFixture.File.Reset();
            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
            this.mockFixture.FileSystem.SetupGet(fs => fs.File).Returns(this.mockFixture.File.Object);

            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}", "1.2.3.4", ClientRole.Client),
                new ClientInstance($"{Environment.MachineName}-Server", "1.2.3.5", ClientRole.Server)
            });

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>
            {
                { nameof(BombardierExecutor.PackageName), "bombardier" },
                { nameof(BombardierExecutor.Scenario), "Bombardier_Benchmark" },
                { nameof(BombardierExecutor.CommandArguments), "--connections 200 --duration 15s http://{ServerIp}:9090/json" }
            };
        }

        [Test]
        public async Task BombardierExecutorGetBombardierVersionParsesVersionWithVPrefix()
        {
            this.mockFixture.SetupProcessOutput(".*--version.*", "bombardier version v1.2.5 linux/arm64");

            using (TestBombardierExecutor executor = new TestBombardierExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                executor.PackageDirectory = this.mockPackage.Path;
                string version = await executor.GetBombardierVersionAsync(EventContext.None, CancellationToken.None);
                Assert.AreEqual("1.2.5", version);
            }
        }

        [Test]
        public async Task BombardierExecutorGetBombardierVersionParsesVersionWithoutVPrefix()
        {
            this.mockFixture.SetupProcessOutput(".*--version.*", "bombardier version 1.2.5");

            using (TestBombardierExecutor executor = new TestBombardierExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                executor.PackageDirectory = this.mockPackage.Path;
                string version = await executor.GetBombardierVersionAsync(EventContext.None, CancellationToken.None);
                Assert.AreEqual("1.2.5", version);
            }
        }

        [Test]
        public async Task BombardierExecutorGetBombardierVersionReturnsNullOnUnparsableOutput()
        {
            this.mockFixture.SetupProcessOutput(".*--version.*", "unrecognized output");

            using (TestBombardierExecutor executor = new TestBombardierExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                executor.PackageDirectory = this.mockPackage.Path;
                string version = await executor.GetBombardierVersionAsync(EventContext.None, CancellationToken.None);
                Assert.IsNull(version);
            }
        }

        private class TestBombardierExecutor : BombardierExecutor
        {
            public TestBombardierExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
                : base(dependencies, parameters)
            {
            }

            public new string PackageDirectory
            {
                get => base.PackageDirectory;
                set => base.PackageDirectory = value;
            }

            public new async Task<string> GetBombardierVersionAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return await base.GetBombardierVersionAsync(telemetryContext, cancellationToken);
            }
        }
    }
}
