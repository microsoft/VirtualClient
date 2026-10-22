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
    using static VirtualClient.Actions.HammerDBExecutor;

    [TestFixture]
    [Category("Unit")]
    [NonParallelizable]
    public class HammerDBExecutorTests
    {
        private MockFixture fixture;
        private DependencyPath mockPackage;
        private string scriptPath;
        private string mockPackagePath;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new MockFixture();
            this.fixture.Setup(PlatformID.Unix);
            this.mockPackage = new DependencyPath("hammerdb", this.fixture.PlatformSpecifics.GetPackagePath("hammerdb"));
            this.fixture.PackageManager.OnGetPackage().ReturnsAsync(this.mockPackage);

            this.mockPackagePath = this.mockPackage.Path;
            this.scriptPath = this.fixture.PlatformSpecifics.GetScriptPath("hammerdb");
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance($"{Environment.MachineName}-Server", "1.2.3.4", "Server"),
                new ClientInstance($"{Environment.MachineName}-Client", "1.2.3.5", "Client")
            });

            this.fixture.FileSystem.Setup(fe => fe.Directory.Exists(It.IsAny<string>())).Returns(true);
            this.fixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        }

        [Test]
        public void HammerDBPExecutorThrowsOnUnsupportedDistroAsync()
        {
            LinuxDistributionInfo mockInfo = new LinuxDistributionInfo()
            {
                Name = "TestUbuntu",
                Distribution = LinuxDistribution.OpenSuse
            };
            this.fixture.SystemManagement.Setup(sm => sm.GetLinuxDistributionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(mockInfo);

            using (TestHammerDBExecutor HammerDBExecutor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters))
            {
                WorkloadException exception = Assert.ThrowsAsync<WorkloadException>(
                    () => HammerDBExecutor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.LinuxDistributionNotSupported, exception.Reason);
            }
        }

        [Test]
        [TestCase("password with 'quotes' and $symbols")]
        public void SuperUserPasswordReadsEnvironmentWithoutTransformingValue(string password)
        {
            string originalPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
            try
            {
                Environment.SetEnvironmentVariable("PGPASSWORD", password);
                using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
                Assert.AreEqual(password, executor.SuperUserPassword);
            }
            finally
            {
                Environment.SetEnvironmentVariable("PGPASSWORD", originalPassword);
            }
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void SuperUserPasswordRejectsMissingEnvironmentVariable(string password)
        {
            string originalPassword = Environment.GetEnvironmentVariable("PGPASSWORD");
            try
            {
                Environment.SetEnvironmentVariable("PGPASSWORD", password);
                using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
                WorkloadException exception = Assert.Throws<WorkloadException>(() => _ = executor.SuperUserPassword);
                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
                StringAssert.Contains("PGPASSWORD", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("PGPASSWORD", originalPassword);
            }
        }

        private class TestHammerDBExecutor : HammerDBExecutor
        {
            public TestHammerDBExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
                : base(dependencies, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;
        }
    }
}
