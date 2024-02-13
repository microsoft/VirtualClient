// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.PostgreSQLExecutor;

    [TestFixture]
    [Category("Unit")]
    public class PostgreSQLClientExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPostgreSqlPackage;
        private DependencyPath mockHammerDbPackage;
        private string mockResults;
        private string tclFileContents;

        public void SetupDefaults(PlatformID platform = PlatformID.Win32NT, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
            this.mockPostgreSqlPackage = new DependencyPath("postgresql", this.mockFixture.GetPackagePath("postgresql"), metadata: new Dictionary<string, IConvertible>
            {
                // Currently, we put the installation path locations in the PostgreSQL package that we download from
                // the package store (i.e. in the *.vcpkg file).
                [$"{PackageMetadata.InstallationPath}-linux-x64"] = "/etc/postgresql/14/main",
                [$"{PackageMetadata.InstallationPath}-linux-arm64"] = "/etc/postgresql/14/main",
                [$"{PackageMetadata.InstallationPath}-win-x64"] = "C:\\Program Files\\PostgreSQL\\14",
                [$"{PackageMetadata.InstallationPath}-win-arm64"] = "C:\\Program Files\\PostgreSQL\\14"
            });

            this.mockHammerDbPackage = new DependencyPath("hammerdb", this.mockFixture.GetPackagePath("hammerdb"));

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockPostgreSqlPackage.Name,
                ["HammerDBPackageName"] = this.mockHammerDbPackage.Name,
                ["Benchmark"] = "tpcc",
                ["DatabaseName"] = "tpcc",
                ["Port"] = 5431
            };

            // Setup: Required packages exist on the system.
            this.mockFixture.PackageManager.OnGetPackage("postgresql").ReturnsAsync(this.mockPostgreSqlPackage);
            this.mockFixture.PackageManager.OnGetPackage("hammerdb").ReturnsAsync(this.mockHammerDbPackage);

            // Setup: Server state information for client/server communications.
            string serverState = JsonConvert.SerializeObject(this.mockFixture.Parameters, new ParameterDictionaryJsonConverter());

            this.mockFixture.File.Setup(f => f.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(serverState);

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockFixture.Directory.Setup(f => f.Exists(It.IsAny<string>()))
                .Returns(true);

            this.mockResults = File.ReadAllText(MockFixture.GetDirectory(typeof(PostgreSQLClientExecutorTests), "Examples", @"PostgreSQL", "PostgresqlresultsExample.txt"));
            this.tclFileContents = File.ReadAllText(MockFixture.GetDirectory(typeof(PostgreSQLClientExecutorTests), "Examples", "PostgreSQL", "runTransactions.tcl"));

            this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
            {
                process.StandardOutput.Clear();
                if (process.FullCommand().Contains("auto runTransactions.tcl"))
                {
                    process.StandardOutput.Append(this.mockResults);
                }
            };

            // Setup: Server state
            var expectedState = new Item<State>(nameof(PostgreSQLServerState), new PostgreSQLServerState
            {
                DatabaseInitialized = true,
                UserName = "anyUser",
                Password = "anyValue",
                WarehouseCount = 1000,
                UserCount = 1000
            });

            this.mockFixture.ApiClient.OnGetState(nameof(PostgreSQLServerState))
                .ReturnsAsync(() => this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedState));

            this.mockFixture.ApiClient.OnGetServerOnline()
                .ReturnsAsync(() => this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK));

            this.mockFixture.ApiClientManager.Setup(mgr => mgr.GetOrCreateApiClient(It.IsAny<string>(), It.IsAny<ClientInstance>()))
                .Returns(this.mockFixture.ApiClient.Object);
        }

        [Test]
        public void PostgreSQLClientExecutorThrowsIfTheExpectedConfigurationFilesRequiredToRunTransactionsDoNotExistInTheHammerDBPackageOnLinuxSystems()
        {
            this.SetupDefaults(PlatformID.Unix);
            using (var executor = new TestPostgreSQLClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // The script and TCL files are not found.
                this.mockFixture.File.Setup(file => file.Exists(It.Is<string>(path => path.EndsWith(".tcl")))).Returns(false);
                Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public void PostgreSQLClientExecutorThrowsIfTheExpectedConfigurationFilesRequiredToRunTransactionsDoNotExistInTheHammerDBPackageOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);
            using (var executor = new TestPostgreSQLClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // The TCL files are not found.
                this.mockFixture.File.Setup(file => file.Exists(It.Is<string>(path => path.EndsWith(".tcl")))).Returns(false);

                Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(CancellationToken.None));
            }
        }

        [Test]
        public async Task PostgreSQLClientExecutorInitializesTheRequiredConfigurationFilesForRunningTransactionsAgainstTheDatabaseOnLinuxSystems()
        {
            this.SetupDefaults(PlatformID.Unix);

            // Example contents of the runTransactions.tcl file.
            string tclFileContent = await File.ReadAllTextAsync(MockFixture.GetDirectory(typeof(PostgreSQLClientExecutorTests), "Examples", "PostgreSQL", "runTransactions.tcl"));

            // Reading the contexts of the runTransactions.tcl file
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(It.Is<string>(path => path.EndsWith("/linux-x64/runTransactions.tcl")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => tclFileContent);

            // Writing the contents back to the file.
            this.mockFixture.File
                .Setup(file => file.WriteAllTextAsync(It.Is<string>(path => path.EndsWith("/linux-x64/runTransactions.tcl")), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((filePath, content, token) => tclFileContent = content);

            using (var executor = new TestPostgreSQLClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                if (!tclFileContent.Contains("diset connection pg_host <HOSTNAME>")
                    || !tclFileContent.Contains("diset tpcc pg_user <USERNAME>")
                    || !tclFileContent.Contains("diset tpcc pg_pass <PASSWORD>")
                    || !tclFileContent.Contains("set z <VIRTUALUSERS>"))
                {
                    Assert.Inconclusive("The example TCL file is missing 1 or more of the expected parameters");
                }

                await executor.ExecuteAsync(CancellationToken.None);

                // The script and TCL file...
                string expectedTclFile = this.mockFixture.Combine(this.mockHammerDbPackage.Path, $"linux-x64/benchmarks/{executor.Benchmark}/postgresql", "runTransactions.tcl");

                // Are copied to the root directory of the HammerDB package.
                string expectedTclCopy = this.mockFixture.Combine(this.mockHammerDbPackage.Path, "linux-x64/runTransactions.tcl");

                this.mockFixture.File.Verify(file => file.Copy(expectedTclFile, expectedTclCopy, true));
                this.mockFixture.File.Verify(file => file.WriteAllTextAsync(expectedTclCopy, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(7));

                // Settings defined in the setup above.
                Assert.IsTrue(tclFileContent.Contains("diset connection pg_host 1.2.3.5"));
                Assert.IsTrue(tclFileContent.Contains("diset connection pg_port 5431"));
                Assert.IsTrue(tclFileContent.Contains("diset tpcc pg_user anyUser"));
                Assert.IsTrue(tclFileContent.Contains("diset tpcc pg_pass anyValue"));
                Assert.IsTrue(tclFileContent.Contains("diset tpcc pg_superuserpass anyValue"));
                Assert.IsTrue(tclFileContent.Contains("diset tpcc pg_dbase tpcc"));
                Assert.IsTrue(tclFileContent.Contains("set z 1000"));
            }
        }

        [Test]
        public async Task PostgreSQLClientExecutorInitializesTheRequiredConfigurationFilesForRunningTransactionsAgainstTheDatabaseOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            // Example contents of the runTransactions.tcl file.
            string tclFileContent = await File.ReadAllTextAsync(MockFixture.GetDirectory(typeof(PostgreSQLClientExecutorTests), "Examples", "PostgreSQL", "runTransactions.tcl"));

            // Reading the contexts of the runTransactions.tcl file
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(It.Is<string>(path => path.EndsWith("\\win-x64\\runTransactions.tcl")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => tclFileContent);

            // Writing the contents back to the file.
            this.mockFixture.File
                .Setup(file => file.WriteAllTextAsync(It.Is<string>(path => path.EndsWith("\\win-x64\\runTransactions.tcl")), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, string, CancellationToken>((filePath, content, token) => tclFileContent = content);

            using (var executor = new TestPostgreSQLClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                if (!tclFileContent.Contains("diset connection pg_host <HOSTNAME>")
                    || !tclFileContent.Contains("diset tpcc pg_user <USERNAME>")
                    || !tclFileContent.Contains("diset tpcc pg_pass <PASSWORD>")
                    || !tclFileContent.Contains("set z <VIRTUALUSERS>"))
                {
                    Assert.Inconclusive("The example TCL file is missing 1 or more of the expected parameters");
                }

                await executor.ExecuteAsync(CancellationToken.None);

                // The TCL file...
                string expectedTclFile = this.mockFixture.Combine(this.mockHammerDbPackage.Path, $"win-x64\\benchmarks\\{executor.Benchmark}\\postgresql", "runTransactions.tcl");

                // Is copied to the root directory of the HammerDB package.
                string expectedTclCopy = this.mockFixture.Combine(this.mockHammerDbPackage.Path, "win-x64\\runTransactions.tcl");

                this.mockFixture.File.Verify(file => file.Copy(expectedTclFile, expectedTclCopy, true));
                this.mockFixture.File.Verify(file => file.WriteAllTextAsync(expectedTclCopy, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(7));

                // Settings defined in the setup above.
                Assert.IsTrue(tclFileContent.Contains("diset connection pg_host 1.2.3.5"));
                Assert.IsTrue(tclFileContent.Contains("diset connection pg_port 5431"));
                Assert.IsTrue(tclFileContent.Contains("diset tpcc pg_user anyUser"));
                Assert.IsTrue(tclFileContent.Contains("diset tpcc pg_pass anyValue"));
                Assert.IsTrue(tclFileContent.Contains("diset tpcc pg_superuserpass anyValue"));
                Assert.IsTrue(tclFileContent.Contains("diset tpcc pg_dbase tpcc"));
                Assert.IsTrue(tclFileContent.Contains("set z 1000"));
            }
        }

        [Test]
        public void PostgreSQLClientExecutorThrowsIfTheServerDoesNotHaveTheDatabaseOnlineBeforePollingTimeout()
        {
            this.SetupDefaults();

            this.mockFixture.ApiClient
                .OnGetState(nameof(PostgreSQLServerState))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));

            using (var executor = new TestPostgreSQLClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // Cause a polling timeout
                executor.PollingTimeout = TimeSpan.Zero;

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.ApiStatePollingTimeout, error.Reason);
            }
        }

        [Test]
        public void PostgreSQLClientExecutorThrowsIfTheServerDoesNotProvideRequiredSettingsForAuthenticationWithTheDatabaseBeforeTimeout()
        {
            this.SetupDefaults();

            this.mockFixture.ApiClient
                .OnGetState(nameof(PostgreSQLServerState))
                .ReturnsAsync(() => this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.NotFound));

            using (var executor = new TestPostgreSQLClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // Cause a polling timeout
                executor.PollingTimeout = TimeSpan.Zero;

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.ApiStatePollingTimeout, error.Reason);
            }
        }

        [Test]
        public async Task PostgreSQLClientExecutorExecutesTheCommandsExpectedOnLinuxSystems()
        {
            this.SetupDefaults(PlatformID.Unix);

            using (var executor = new TestPostgreSQLClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                string expectedHammerDbWorkingDir = $"{this.mockHammerDbPackage.Path}/linux-x64";

                List<string> expectedCommands = new List<string>
                {
                    // Format:
                    // {command} {command_arguments} ||--> {working_dir}
                    $"bash -c \"{this.mockHammerDbPackage.Path}/linux-x64/hammerdbcli auto runTransactions.tcl\" ||--> {expectedHammerDbWorkingDir}"
                };

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    expectedCommands.Remove($"{process.FullCommand()} ||--> {process.StartInfo.WorkingDirectory}".Trim());

                    process.StandardOutput.Clear();
                    if (process.FullCommand().Contains("auto runTransactions.tcl"))
                    {
                        process.StandardOutput.Append(this.mockResults);
                    }
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task PostgreSQLClientExecutorExecutesThecommandsExpectedOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            using (var executor = new TestPostgreSQLClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                string expectedHammerDbWorkingDir = @"C:\users\any\tools\VirtualClient\packages\hammerdb\win-x64";

                List<string> expectedCommands = new List<string>
                {
                    // Format:
                    // {command} {command_arguments} ||--> {working_dir}
                    $@"{expectedHammerDbWorkingDir}\hammerdbcli.bat auto runTransactions.tcl ||--> {expectedHammerDbWorkingDir}",
                };

                this.mockFixture.ProcessManager.OnProcessCreated = (process) =>
                {
                    expectedCommands.Remove($"{process.FullCommand()} ||--> {process.StartInfo.WorkingDirectory}".Trim());

                    process.StandardOutput.Clear();
                    if (process.FullCommand().Contains("auto runTransactions.tcl"))
                    {
                        process.StandardOutput.Append(this.mockResults);
                    }
                };

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsEmpty(expectedCommands);
            }
        }

        private class TestPostgreSQLClientExecutor : PostgreSQLClientExecutor
        {
            public TestPostgreSQLClientExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public new string HammerDBPackagePath => base.HammerDBPackagePath;

            public new TimeSpan PollingTimeout
            {
                get
                {
                    return base.PollingTimeout;
                }
                set
                {
                    base.PollingTimeout = value;
                }
            }

            public new string PostgreSqlInstallationPath => base.PostgreSqlInstallationPath;

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}