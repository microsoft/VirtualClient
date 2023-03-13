// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using Polly;
    using VirtualClient;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.PostgreSQLExecutor;

    [TestFixture]
    [Category("Unit")]
    public class PostgreSQLServerExecutorTests
    {
        private MockFixture mockFixture;
        private DependencyPath mockPostgreSqlPackage;
        private DependencyPath mockHammerDBPackage;

        public void SetupDefaults(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);

            this.mockFixture.FileSystem.Setup(rt => rt.File.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("mock Makefile");

            this.mockPostgreSqlPackage = new DependencyPath("postgresql", this.mockFixture.GetPackagePath("postgresql"), metadata: new Dictionary<string, IConvertible>
            {
                // Currently, we put the installation path locations in the PostgreSQL package that we download from
                // the package store (i.e. in the *.vcpkg file).
                [$"{PackageMetadata.InstallationPath}-linux-x64"] = "/etc/postgresql/14/main",
                [$"{PackageMetadata.InstallationPath}-linux-arm64"] = "/etc/postgresql/14/main",
                [$"{PackageMetadata.InstallationPath}-win-x64"] = "C:\\Program Files\\PostgreSQL\\14",
                [$"{PackageMetadata.InstallationPath}-win-arm64"] = "C:\\Program Files\\PostgreSQL\\14"
            });

            this.mockHammerDBPackage = new DependencyPath("hammerdb", this.mockFixture.GetPackagePath("hammerdb"));

            this.mockFixture.Parameters = new Dictionary<string, IConvertible>()
            {
                ["PackageName"] = this.mockPostgreSqlPackage.Name,
                ["HammerDBPackageName"] = this.mockHammerDBPackage.Name,
                ["DatabaseName"] = "tpcc",
                ["ReuseDatabase"] = true,
                ["Username"] = "anyuser",
                ["Password"] = "anyvalue",
                ["UserCount"] = 100,
                ["WarehouseCount"] = 100
            };

            this.mockFixture.PackageManager.OnGetPackage("postgresql").ReturnsAsync(this.mockPostgreSqlPackage);
            this.mockFixture.PackageManager.OnGetPackage("hammerdb").ReturnsAsync(this.mockHammerDBPackage);

            // Setup:
            // The server will be checking for state objects. State is how the server communicates required information
            // to the client.
            this.mockFixture.ApiClient.OnGetState(nameof(PostgreSQLServerState))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK, new Item<PostgreSQLServerState>(nameof(PostgreSQLServerState), new PostgreSQLServerState())));

            this.mockFixture.ApiClient.OnUpdateState<PostgreSQLServerState>(nameof(PostgreSQLServerState))
                .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK));

            this.mockFixture.File.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);

        }

        [Test]
        public async Task PostgreSQLServerExecutorInitializeDependenciesAsExpectedOnLinuxSystems()
        {
            this.SetupDefaults(PlatformID.Unix, Architecture.X64);
            using (TestPostgreSQLServerExecutor executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                await executor.InitializeAsync(EventContext.None, CancellationToken.None);

                // The createDBscript.sh must be copied from a location within the HammerDB package to the root directory
                // of that package for the platform/architecture
                string createDBScriptCopyFromPath = this.mockFixture.Combine(this.mockHammerDBPackage.Path, "linux-x64", "postgresql", "createDBScript.sh");
                string createDBScriptCopyToPath = this.mockFixture.Combine(this.mockHammerDBPackage.Path, "linux-x64", "createDBScript.sh");

                // createDB.tcl
                string createDBTclCopyFromPath = this.mockFixture.Combine(this.mockHammerDBPackage.Path, "linux-x64", "postgresql", "createDBScript.sh");
                string createDBTclCopyToPath = this.mockFixture.Combine(this.mockHammerDBPackage.Path, "linux-x64", "createDBScript.sh");

                this.mockFixture.File.Verify(f => f.Copy(createDBScriptCopyFromPath, createDBScriptCopyToPath, true));
                this.mockFixture.File.Verify(f => f.Copy(createDBTclCopyFromPath, createDBTclCopyToPath, true));
            }
        }

        [Test]
        public void PostgreSQLServerExecutorThrowsOnFailingToSaveTheServerState()
        {
            this.SetupDefaults();

            using (var executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                this.mockFixture.ApiClient.OnUpdateState<PostgreSQLServerState>(nameof(PostgreSQLServerState))
                    .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.BadRequest));

                WorkloadException error = Assert.ThrowsAsync<WorkloadException>(() => executor.ExecuteAsync(CancellationToken.None));
                Assert.AreEqual(ErrorReason.Http400BadRequestResponse, error.Reason);
            }
        }

        [Test]
        public async Task PostgreSQLServerExecutorWritesTheExpectedInformationToTheServerState()
        {
            this.SetupDefaults();

            using (var executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                bool confirmed = false;
                this.mockFixture.ApiClient.OnUpdateState<PostgreSQLServerState>(nameof(PostgreSQLServerState))
                    .Callback<string, object, CancellationToken, IAsyncPolicy<HttpResponseMessage>>((stateId, state, token, retryPolicy) =>
                    {
                        Item<PostgreSQLServerState> actualState = state as Item<PostgreSQLServerState>;

                        // Based on setup at the top. On first call, the database has not been created yet.
                        Assert.IsNotNull(actualState);
                        Assert.IsTrue(actualState.Definition.DatabaseInitialized);
                        Assert.AreEqual(100, actualState.Definition.WarehouseCount);
                        Assert.AreEqual(100, actualState.Definition.UserCount);
                        Assert.AreEqual("anyuser", actualState.Definition.UserName);
                        Assert.AreEqual("anyvalue", actualState.Definition.Password);
                        confirmed = true;
                    })
                    .ReturnsAsync(this.mockFixture.CreateHttpResponse(HttpStatusCode.OK));

                await executor.ExecuteAsync(CancellationToken.None);
                Assert.IsTrue(confirmed);
            }
        }

        [Test]
        public async Task PostgreSQLServerExecutorExecutesExpectedCommandsOnWindowsSystemsToCreateTheDatabase()
        {
            this.SetupDefaults(PlatformID.Win32NT,Architecture.X64);
            using (var executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // e.g.
                // C:\Program Files\PostgreSQL\14
                string postgreSqlInstallationPath = this.mockPostgreSqlPackage.Metadata[$"{PackageMetadata.InstallationPath}-win-x64"].ToString();
                
                // e.g. 
                // C:\Users\Any\VirtualClient\packages\hammerdb\win-x64
                string hammerDBPath = this.mockHammerDBPackage.Path;

                List<string> expectedCommands = new List<string>()
                {
                    // Set the database server configuration.
                    $"powershell -Command \"& {{Add-Content -Path '{this.mockFixture.Combine(postgreSqlInstallationPath, "data", "pg_hba.conf")}' -Value 'host  all  all  0.0.0.0/0  md5'}}\"",

                    // Restart the database services.
                    $"{postgreSqlInstallationPath}\\bin\\pg_ctl.exe restart -D \"{postgreSqlInstallationPath}\\data\"",

                    // Drop the TPCC database if it already exists.
                    $"{postgreSqlInstallationPath}\\bin\\psql.exe -U postgres -c \"DROP DATABASE IF EXISTS tpcc;\"",

                    // Drop the user if it exists.
                    $"{postgreSqlInstallationPath}\\bin\\psql.exe -U postgres -c \"DROP ROLE IF EXISTS anyuser;\"",

                    // Create the user for access to the database.
                    $"{postgreSqlInstallationPath}\\bin\\psql.exe -U postgres -c \"CREATE USER anyuser PASSWORD 'anyvalue';\"",

                    // Create the database and populate it with data.
                    $"{hammerDBPath}\\win-x64\\hammerdbcli.bat auto createDB.tcl",
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{file} {arguments}");
                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);

                Assert.IsEmpty(expectedCommands);
            }
        }

        [Test]
        public async Task PostgreSQLServerExecutorExecutesExpectedProcessOnUnix()
        {
            this.SetupDefaults(PlatformID.Unix, Architecture.X64);
            using (var executor = new TestPostgreSQLServerExecutor(this.mockFixture.Dependencies, this.mockFixture.Parameters))
            {
                // e.g.
                // /etc/postgresql/14/main
                string postgreSqlInstallationPath = this.mockPostgreSqlPackage.Metadata[$"{PackageMetadata.InstallationPath}-linux-x64"].ToString();

                // e.g. 
                // /home/user/VirtualClient/packages/hammerdb/linux-x64
                string hammerDBPath = this.mockHammerDBPackage.Path;

                List<string> expectedCommands = new List<string>()
                {
                    // Set the database server configuration.
                    $"powershell -Command \"& {{Add-Content -Path '{this.mockFixture.Combine(postgreSqlInstallationPath, "data", "pg_hba.conf")}' -Value 'host  all  all  0.0.0.0/0  md5'}}\"",

                    // Restart the database services.
                    $"{postgreSqlInstallationPath}\\bin\\pg_ctl.exe restart -D \"{postgreSqlInstallationPath}\\data\"",

                    // Drop the TPCC database if it already exists.
                    $"{postgreSqlInstallationPath}\\bin\\psql.exe -U postgres -c \"DROP DATABASE IF EXISTS tpcc;\"",

                    // Drop the user if it exists.
                    $"{postgreSqlInstallationPath}\\bin\\psql.exe -U postgres -c \"DROP ROLE IF EXISTS anyuser;\"",

                    // Create the user for access to the database.
                    $"{postgreSqlInstallationPath}\\bin\\psql.exe -U postgres -c \"CREATE USER anyuser PASSWORD 'anyvalue';\"",

                    // Create the database and populate it with data.
                    $"{hammerDBPath}\\win-x64\\hammerdbcli.bat auto createDB.tcl",
                };

                this.mockFixture.ProcessManager.OnCreateProcess = (file, arguments, workingDirectory) =>
                {
                    expectedCommands.Remove($"{file} {arguments}");
                    return this.mockFixture.Process;
                };

                await executor.ExecuteAsync(CancellationToken.None);

                Assert.IsEmpty(expectedCommands);
            }
        }

        private class TestPostgreSQLServerExecutor : PostgreSQLServerExecutor
        {
            public TestPostgreSQLServerExecutor(IServiceCollection services, IDictionary<string, IConvertible> parameters = null)
                : base(services, parameters)
            {
            }

            public new string HammerDBPackagePath => base.HammerDBPackagePath;

            public new int UserCount => base.UserCount;

            public new string Password => base.ClientPassword;

            public new string PostgreSqlInstallationPath => base.PostgreSqlInstallationPath;

            public new string Username => base.ClientUsername;

            public new int WarehouseCount => base.WarehouseCount;

            public new Task InitializeAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return base.InitializeAsync(telemetryContext, cancellationToken);
            }
        }
    }
}