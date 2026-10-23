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
                Assert.AreEqual(password, executor.GetSuperUserPassword());
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
                WorkloadException exception = Assert.Throws<WorkloadException>(() => executor.GetSuperUserPassword());
                Assert.AreEqual(ErrorReason.InvalidProfileDefinition, exception.Reason);
                StringAssert.Contains("PGPASSWORD", exception.Message);
            }
            finally
            {
                Environment.SetEnvironmentVariable("PGPASSWORD", originalPassword);
            }
        }

        [TestCase(0, "password with 'quotes' and $symbols")]
        [TestCase(1, "password with 'quotes' and $symbols")]
        [TestCase(0, "1")]
        public async Task ProcessOutputObscuresCredentialInTelemetryFilesAndFailures(int exitCode, string password)
        {
            this.fixture.Parameters[nameof(HammerDBExecutor.LogToFile)] = true;
            List<string> logFiles = new List<string>();
            List<string> loggedOutput = new List<string>();
            this.fixture.File.Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((path, content, token) =>
                {
                    if (path.EndsWith(".log", StringComparison.Ordinal))
                    {
                        logFiles.Add(content);
                    }
                })
                .Returns(Task.CompletedTask);
            this.fixture.Logger.OnLog = (level, eventInfo, state, error) =>
            {
                if (eventInfo.Name.EndsWith(".ProcessDetails", StringComparison.Ordinal))
                {
                    JObject details = JObject.FromObject(((EventContext)state).Properties["process"]);
                    loggedOutput.Add(details["standardOutput"].Value<string>());
                    loggedOutput.Add(details["standardError"].Value<string>());
                }
            };

            using InMemoryProcess process = new InMemoryProcess
            {
                StartInfo = new ProcessStartInfo { FileName = "hammerdbcli", Arguments = "auto runTransactions.tcl" },
                ExitCode = exitCode,
                OnHasExited = () => true
            };
            string output = $"Changed tpcc:pg_pass from postgres to {password} for PostgreSQL\nTransactions/sec: 120";
            process.StandardOutput.Append(output);
            process.StandardError.Append($"Connection details: {password}");

            using TestHammerDBExecutor executor = new TestHammerDBExecutor(this.fixture.Dependencies, this.fixture.Parameters);
            await executor.OnLogPostgreSQLProcessDetails(process, password, EventContext.None, "HammerDB");

            Assert.AreEqual(1, logFiles.Count);
            Assert.AreEqual(2, loggedOutput.Count);
            Assert.AreEqual(output.Replace(password, "...", StringComparison.Ordinal), loggedOutput[0]);
            Assert.AreEqual("Connection details: ...", loggedOutput[1]);
            StringAssert.Contains(loggedOutput[0], logFiles[0]);
            StringAssert.Contains(loggedOutput[1], logFiles[0]);
            StringAssert.DoesNotContain($"to {password} for PostgreSQL", logFiles[0]);
            StringAssert.DoesNotContain($"Connection details: {password}", logFiles[0]);

            if (exitCode == 0)
            {
                Assert.AreEqual(output, process.StandardOutput.ToString(), "Metric input must remain unchanged.");
            }
            else
            {
                WorkloadException exception = Assert.Throws<WorkloadException>(() =>
                    process.ThrowIfErrored<WorkloadException>(process.StandardError.ToString(), ErrorReason.WorkloadFailed));
                StringAssert.DoesNotContain(password, exception.ToString());
            }
        }

        private class TestHammerDBExecutor : HammerDBExecutor
        {
            public TestHammerDBExecutor(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters = null)
                : base(dependencies, parameters)
            {
            }

            public Func<EventContext, CancellationToken, Task> OnInitialize => base.InitializeAsync;

            public Func<IProcessProxy, string, EventContext, string, Task> OnLogPostgreSQLProcessDetails => base.LogPostgreSQLProcessDetailsAsync;
        }
    }
}
