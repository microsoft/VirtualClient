// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
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
                ["HammerDBPackageName"] = this.mockHammerDbPackage.Name
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

            this.mockResults = File.ReadAllText(Path.Combine(MockFixture.ExamplesDirectory, @"PostgreSQL", "PostgresqlresultsExample.txt"));

            // Setup: Server state
            var expectedState = new Item<State>(nameof(PostgreSQLServerState), new PostgreSQLServerState
            {
                InitialSetupComplete = true,
                DatabaseCreated = true,
                UserName = "anyUser",
                Password = "anyValue",
                WarehouseCount = 1000,
                NumOfVirtualUsers = 1000
            });

            this.mockFixture.ApiClient.OnGetState(nameof(PostgreSQLServerState))
                .ReturnsAsync(() => this.mockFixture.CreateHttpResponse(System.Net.HttpStatusCode.OK, expectedState));

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

                // The script and TCL files are not found.
                this.mockFixture.File.Setup(file => file.Exists(It.Is<string>(path => path.EndsWith(".sh")))).Returns(true);

                Assert.ThrowsAsync<DependencyException>(() => executor.InitializeAsync(EventContext.None, CancellationToken.None));

                this.mockFixture.File.Setup(file => file.Exists(It.Is<string>(path => path.EndsWith(".tcl")))).Returns(true);
                this.mockFixture.File.Setup(file => file.Exists(It.Is<string>(path => path.EndsWith(".sh")))).Returns(false);

                Assert.ThrowsAsync<DependencyException>(() => executor.InitializeAsync(EventContext.None, CancellationToken.None));
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

                Assert.ThrowsAsync<DependencyException>(() => executor.InitializeAsync(EventContext.None, CancellationToken.None));
            }
        }

        [Test]
        public async Task PostgreSQLClientExecutorInitializesTheRequiredConfigurationFilesForRunningTransactionsAgainstTheDatabaseOnLinuxSystems()
        {
            this.SetupDefaults(PlatformID.Unix);

            // Example contents of the runTransactions.tcl file.
            string tclFileContent = await File.ReadAllTextAsync(Path.Combine(MockFixture.ExamplesDirectory, "PostgreSQL", "runTransactions.tcl"));

            // Reading the contexts of the runTransactions.tcl file
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(It.Is<string>(path => path.EndsWith("tcl")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => tclFileContent);

            // Writing the contents back to the file.
            this.mockFixture.File
                .Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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

                await executor.InitializeAsync(EventContext.None, CancellationToken.None);

                // The script and TCL file...
                string expectedScriptFile = this.mockFixture.Combine(this.mockHammerDbPackage.Path, "linux-x64/postgresql", "runTransactionsScript.sh");
                string expectedTclFile = this.mockFixture.Combine(this.mockHammerDbPackage.Path, "linux-x64/postgresql", "runTransactions.tcl");

                // Are copied to the root directory of the HammerDB package.
                string expectedScriptCopy = this.mockFixture.Combine(this.mockHammerDbPackage.Path, "linux-x64/runTransactionsScript.sh");
                string expectedTclCopy = this.mockFixture.Combine(this.mockHammerDbPackage.Path, "linux-x64/runTransactions.tcl");

                this.mockFixture.File.Verify(file => file.Copy(expectedScriptFile, expectedScriptCopy, true));
                this.mockFixture.File.Verify(file => file.Copy(expectedTclFile, expectedTclCopy, true));
                this.mockFixture.File.Verify(file => file.WriteAllTextAsync(expectedTclCopy, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(4));

                // Settings defined in the setup above.
                Assert.IsTrue(tclFileContent.Contains("diset connection pg_host 1.2.3.5"));
                Assert.IsTrue(tclFileContent.Contains("diset tpcc pg_user anyUser"));
                Assert.IsTrue(tclFileContent.Contains("diset tpcc pg_pass anyValue"));
                Assert.IsTrue(tclFileContent.Contains("set z 1000"));
            }
        }

        [Test]
        public async Task PostgreSQLClientExecutorInitializesTheRequiredConfigurationFilesForRunningTransactionsAgainstTheDatabaseOnWindowsSystems()
        {
            this.SetupDefaults(PlatformID.Win32NT);

            // Example contents of the runTransactions.tcl file.
            string tclFileContent = await File.ReadAllTextAsync(Path.Combine(MockFixture.ExamplesDirectory, "PostgreSQL", "runTransactions.tcl"));

            // Reading the contexts of the runTransactions.tcl file
            this.mockFixture.File.Setup(file => file.ReadAllTextAsync(It.Is<string>(path => path.EndsWith("tcl")), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => tclFileContent);

            // Writing the contents back to the file.
            this.mockFixture.File
                .Setup(file => file.WriteAllTextAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
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

                await executor.InitializeAsync(EventContext.None, CancellationToken.None);

                // The TCL file...
                string expectedTclFile = this.mockFixture.Combine(this.mockHammerDbPackage.Path, "win-x64/postgresql", "runTransactions.tcl");

                // Is copied to the root directory of the HammerDB package.
                string expectedTclCopy = this.mockFixture.Combine(this.mockHammerDbPackage.Path, "win-x64/runTransactions.tcl");

                this.mockFixture.File.Verify(file => file.Copy(expectedTclFile, expectedTclCopy, true));
                this.mockFixture.File.Verify(file => file.WriteAllTextAsync(expectedTclCopy, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(4));

                // Settings defined in the setup above.
                Assert.IsTrue(tclFileContent.Contains("diset connection pg_host 1.2.3.5"));
                Assert.IsTrue(tclFileContent.Contains("diset tpcc pg_user anyUser"));
                Assert.IsTrue(tclFileContent.Contains("diset tpcc pg_pass anyValue"));
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
        public async Task PostgreSQLClientExecutorExecutesThecommandsExpectedOnLinuxSystems()
        {
            this.SetupDefaults(PlatformID.Unix);

            using (var executor = new TestPostgreSQLClientExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                string expectedPostgreSqlWorkingDir = "/etc/postgresql/14/main";
                string expectedHammerDbWorkingDir = $"{this.mockHammerDbPackage.Path}/linux-x64";

                List<string> expectedCommands = new List<string>
                {
                    // Format:
                    // {command} {command_arguments} ||--> {working_dir}
                    //
                    // Depends upon the values set in the setup above
                    $"sudo sed -i \"s%host  all  all  0.0.0.0/0  md5%%g\" pg_hba.conf ||--> {expectedPostgreSqlWorkingDir}",
                    $"sudo sed \"1 a host  all  all  0.0.0.0/0  md5\" pg_hba.conf -i ||--> {expectedPostgreSqlWorkingDir}",
                    $"sudo sed -i \"s/#listen_addresses = 'localhost'/listen_addresses = '*'/g\" postgresql.conf ||--> {expectedPostgreSqlWorkingDir}",
                    $"sudo sed -i \"s/port = .*/port = 5432/g\" postgresql.conf ||--> {expectedPostgreSqlWorkingDir}",
                    $"sudo -u postgres psql -c \"ALTER USER postgres PASSWORD 'postgres';\" ||--> {expectedPostgreSqlWorkingDir}",
                    $"sudo systemctl restart postgresql ||--> {expectedPostgreSqlWorkingDir}",
                    $"sudo bash runTransactionsScript.sh ||--> {expectedHammerDbWorkingDir}"
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{file} {arguments} ||--> {workingDirectory}".Trim());
                    if (arguments.Contains("runTransactions"))
                    {
                        this.mockFixture.Process.StandardOutput.Append(this.mockResults);
                    }

                    return this.mockFixture.Process;
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
                string expectedPostgreSqlWorkingDir = @"C:\Program Files\PostgreSQL\14";
                string expectedHammerDbWorkingDir = @"C:\users\any\tools\VirtualClient\packages\hammerdb\win-x64";

                List<string> expectedCommands = new List<string>
                {
                    // Format:
                    // {command} {command_arguments} ||--> {working_dir}
                    //
                    // Depends upon the values set in the setup above
                    //
                    // Setup the environment for running transactions
                    $@"powershell -Command ""& {{Add-Content -Path '{expectedPostgreSqlWorkingDir}\data\pg_hba.conf' -Value 'host  all  all  0.0.0.0/0  md5'}}"" ||--> {expectedPostgreSqlWorkingDir}",
                    $@"{expectedPostgreSqlWorkingDir}\bin\pg_ctl.exe restart -D ""{expectedPostgreSqlWorkingDir}\data"" ||--> {expectedPostgreSqlWorkingDir}\bin",

                    // Run transactions
                    $@"{expectedHammerDbWorkingDir}\hammerdbcli.bat auto runTransactions.tcl ||--> {expectedHammerDbWorkingDir}",

                };

                StringBuilder builder = new StringBuilder();
                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    builder.AppendLine($"{file} {arguments} ||--> {workingDirectory}".Trim());
                    expectedCommands.Remove($"{file} {arguments} ||--> {workingDirectory}".Trim());
                    if (arguments.Contains("runTransactions"))
                    {
                        this.mockFixture.Process.StandardOutput.Append(this.mockResults);
                    }

                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);

                string output = builder.ToString();
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