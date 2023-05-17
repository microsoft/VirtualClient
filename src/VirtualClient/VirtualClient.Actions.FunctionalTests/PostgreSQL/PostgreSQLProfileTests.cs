// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;
    using static VirtualClient.Actions.PostgreSQLExecutor;

    [TestFixture]
    [Category("Functional")]
    public class PostgreSQLProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-SQL-POSTGRESQL.json")]
        public void PostgreSQLWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-SQL-POSTGRESQL.json")]
        public void PostgreSQLWorkloadProfileValidatesRequiredPackagesForClientRole(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT);

            // We ensure the workload package does not exist.
            this.mockFixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.AreEqual(ErrorReason.WorkloadDependencyMissing, error.Reason);
                Assert.IsFalse(this.mockFixture.ProcessManager.Commands.Contains("postgresql"));
            }
        }

        [Test]
        [TestCase("PERF-SQL-POSTGRESQL.json")]
        public async Task PostgreSQLWorkloadProfileInstallsTheExpectedDependenciesOnUnix_ClientRole_Windows(string profile)
        {
            this.SetupClientRole(PlatformID.Win32NT);

            this.mockFixture.SetupWorkloadPackage("postgresql", metadata: new Dictionary<string, IConvertible>
            {
                // Currently, we put the installation path locations in the PostgreSQL package that we download from
                // the package store (i.e. in the *.vcpkg file).
                [$"{PackageMetadata.InstallationPath}-win-x64"] = "C:\\Program Files\\PostgreSQL\\14",
            });

            this.mockFixture.SetupFile("postgresql", "win-x64\\superuser.txt", "superuser");

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None);

                // Workload dependency package expectations
                // The workload dependency package should have been installed at this point.
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "postgresql");
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "hammerdb");
            }
        }

        [Test]
        [TestCase("PERF-SQL-POSTGRESQL.json")]
        public async Task PostgreSQLWorkloadProfileInstallsTheExpectedDependencies_ClientRole_Unix(string profile)
        {
            this.SetupClientRole(PlatformID.Unix);

            this.mockFixture.SetupWorkloadPackage(
                "postgresql",
                metadata: new Dictionary<string, IConvertible>
                {
                    // Currently, we put the installation path locations in the PostgreSQL package that we download from
                    // the package store (i.e. in the *.vcpkg file).
                    [$"{PackageMetadata.InstallationPath}-linux-x64"] = "/etc/postgresql/14/main",
                },
                expectedFiles: new string[] { "/linux-x64/ubuntu/configure.sh", "/linux-x64/ubuntu/install.sh" });

            this.mockFixture.SetupFile("postgresql", $"linux-x64/superuser.txt", "superuser");

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None);

                // Workload dependency package expectations
                // The workload dependency package should have been installed at this point.
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "postgresql");
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "hammerdb");
            }
        }

        [Test]
        [TestCase("PERF-SQL-POSTGRESQL.json")]
        public async Task PostgreSQLWorkloadProfileInstallsTheExpectedDependencies_ServerRole_Windows(string profile)
        {
            this.SetupClientRole(PlatformID.Win32NT);

            this.mockFixture.SetupWorkloadPackage("postgresql", metadata: new Dictionary<string, IConvertible>
            {
                // Currently, we put the installation path locations in the PostgreSQL package that we download from
                // the package store (i.e. in the *.vcpkg file).
                [$"{PackageMetadata.InstallationPath}-win-x64"] = "C:\\Program Files\\PostgreSQL\\14",
            });

            this.mockFixture.SetupFile("postgresql", "win-x64\\superuser.txt", "superuser");

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None);

                // Workload dependency package expectations
                // The workload dependency package should have been installed at this point.
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "postgresql");
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "hammerdb");
            }
        }

        [Test]
        [TestCase("PERF-SQL-POSTGRESQL.json")]
        public async Task PostgreSQLWorkloadProfileInstallsTheExpectedDependencies_ServerRole_Unix(string profile)
        {
            this.SetupClientRole(PlatformID.Unix);

            this.mockFixture.SetupWorkloadPackage(
                "postgresql",
                metadata: new Dictionary<string, IConvertible>
                {
                    // Currently, we put the installation path locations in the PostgreSQL package that we download from
                    // the package store (i.e. in the *.vcpkg file).
                    [$"{PackageMetadata.InstallationPath}-linux-x64"] = "/etc/postgresql/14/main",
                },
                expectedFiles: new string[] { "/linux-x64/ubuntu/configure.sh", "/linux-x64/ubuntu/install.sh" });

            this.mockFixture.SetupFile("postgresql", $"linux-x64/superuser.txt", "superuser");

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None);

                // Workload dependency package expectations
                // The workload dependency package should have been installed at this point.
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "postgresql");
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "hammerdb");
            }
        }

        [Test]
        [TestCase("PERF-SQL-POSTGRESQL.json")]
        public async Task PostgreSQLWorkloadProfileExecutesTheExpectedOperations_ClientRole_Windows(string profile)
        {
            this.SetupClientRole(PlatformID.Win32NT);

            this.mockFixture.SetupWorkloadPackage("postgresql", metadata: new Dictionary<string, IConvertible>
            {
                // Currently, we put the installation path locations in the PostgreSQL package that we download from
                // the package store (i.e. in the *.vcpkg file).
                [$"{PackageMetadata.InstallationPath}-win-x64"] = "C:\\Program Files\\PostgreSQL\\14",
            });

            this.mockFixture.SetupWorkloadPackage("hammerdb", expectedFiles: "/win-x64/benchmarks/tpcc/postgresql/runTransactions.tcl");

            string packagesPath = this.mockFixture.GetPackagePath();

            List<string> commands = new List<string>
            {
                $"{packagesPath}\\hammerdb\\win-x64\\hammerdbcli.bat auto runTransactions.tcl"
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                StringBuilder builder = new StringBuilder();
                foreach (var command in this.mockFixture.ProcessManager.Commands)
                {
                    builder.AppendLine(command);
                }

                string hereitis = builder.ToString();

                WorkloadAssert.CommandsExecuted(this.mockFixture, commands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-SQL-POSTGRESQL.json")]
        public async Task PostgreSQLWorkloadProfileExecutesTheExpectedOperations_ServerRole_Windows(string profile)
        {
            this.SetupServerRole(PlatformID.Win32NT);

            this.mockFixture.SetupWorkloadPackage("postgresql",
                metadata: new Dictionary<string, IConvertible>
                {
                    // Currently, we put the installation path locations in the PostgreSQL package that we download from
                    // the package store (i.e. in the *.vcpkg file).
                    [$"{PackageMetadata.InstallationPath}-win-x64"] = "C:\\Program Files\\PostgreSQL\\14",
                },
                expectedFiles: new string[] { "/win-x64/configure.cmd", "/win-x64/install.cmd" });

            this.mockFixture.SetupFile("postgresql", "/win-x64/superuser.txt", "superuser");
            this.mockFixture.SetupWorkloadPackage("hammerdb", expectedFiles: "/win-x64/benchmarks/tpcc/postgresql/createDB.tcl");

            string packagesDirectory = this.mockFixture.GetPackagePath();

            List<string> commands = new List<string>
            {
                $"{packagesDirectory}\\postgresql\\win-x64\\configure.cmd 5432",
                $"C:\\Program Files\\PostgreSQL\\14\\bin\\psql.exe -U postgres -c \"DROP DATABASE IF EXISTS tpcc;\"",
                $"{packagesDirectory}\\hammerdb\\win-x64\\hammerdbcli.bat auto createDB.tcl"
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, commands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-SQL-POSTGRESQL.json")]
        public async Task PostgreSQLWorkloadProfileExecutesTheExpectedOperations_ClientRole_Unix(string profile)
        {
            this.SetupClientRole(PlatformID.Unix);

            this.mockFixture.SetupWorkloadPackage("postgresql", metadata: new Dictionary<string, IConvertible>
            {
                // Currently, we put the installation path locations in the PostgreSQL package that we download from
                // the package store (i.e. in the *.vcpkg file).
                [$"{PackageMetadata.InstallationPath}-linux-x64"] = "/etc/postgresql/14/main",
            },
            expectedFiles: new string[]
            {
                "/linux-x64/ubuntu/configure.sh"
            });

            this.mockFixture.SetupWorkloadPackage("hammerdb", expectedFiles: new string[]
            {
                "/linux-x64/benchmarks/tpcc/postgresql/runTransactions.tcl",
                "/linux-x64/hammerdbcli"
            });

            this.mockFixture.SetupDirectory("hammerdb", "/linux-x64/bin");

            string packagesDirectory = this.mockFixture.GetPackagePath();

            List<string> commands = new List<string>
            {
                // Attribute the hammerdbcli script as executable
                $"sudo chmod +x \"{packagesDirectory}/hammerdb/linux-x64/hammerdbcli\"",

                // Attribute the files in the /bin directory as executable
                $"sudo chmod -R 2777 \"{packagesDirectory}/hammerdb/linux-x64/bin\"",

                // Attribute the configure.sh script as executable
                $"sudo chmod +x \"{packagesDirectory}/postgresql/linux-x64/ubuntu/configure.sh\"",

                // Run transactions against the database
                $"bash -c \"{packagesDirectory}/hammerdb/linux-x64/hammerdbcli auto runTransactions.tcl\""
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, commands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-SQL-POSTGRESQL.json")]
        public async Task PostgreSQLWorkloadProfileExecutesTheExpectedOperations_ServerRole_Unix(string profile)
        {
            this.SetupServerRole(PlatformID.Unix);

            this.mockFixture.SetupWorkloadPackage("postgresql",
                metadata: new Dictionary<string, IConvertible>
                {
                    // Currently, we put the installation path locations in the PostgreSQL package that we download from
                    // the package store (i.e. in the *.vcpkg file).
                    [$"{PackageMetadata.InstallationPath}-linux-x64"] = "/etc/postgresql/14/main",
                },
                expectedFiles: new string[] { "/linux-x64/ubuntu/configure.sh", "/linux-x64/ubuntu/install.sh" });

            this.mockFixture.SetupFile("postgresql", "/linux-x64/superuser.txt", "superuser");
            this.mockFixture.SetupWorkloadPackage("hammerdb", expectedFiles: new string[]
            {
                "/linux-x64/benchmarks/tpcc/postgresql/createDB.tcl",
                "/linux-x64/hammerdbcli"
            });

            this.mockFixture.SetupDirectory("hammerdb", "/linux-x64/bin");

            string packagesDirectory = this.mockFixture.GetPackagePath();

            List<string> commands = new List<string>
            {
                // Attribute the hammerdbcli script as executable
                $"sudo chmod +x \"{packagesDirectory}/hammerdb/linux-x64/hammerdbcli\"",

                // Attribute the files in the /bin directory as executable
                $"sudo chmod -R 2777 \"{packagesDirectory}/hammerdb/linux-x64/bin\"",

                // Attribute the configure.sh script as executable
                $"sudo chmod +x \"{packagesDirectory}/postgresql/linux-x64/ubuntu/configure.sh\"",

                // Configure the PostgreSQL server before creating the database.
                $"sudo {packagesDirectory}/postgresql/linux-x64/ubuntu/configure.sh 5432",

                // Drop the database if it exists.
                $"sudo -u postgres psql -c \"DROP DATABASE IF EXISTS tpcc;\"",

                // Create the database.
                $"bash -c \"{packagesDirectory}/hammerdb/linux-x64/hammerdbcli auto createDB.tcl\""
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, commands.ToArray());
            }
        }

        private void SetupClientRole(PlatformID platform)
        {
            this.mockFixture.Setup(platform, Architecture.X64, "Client01").SetupLayout(
               new ClientInstance("Client01", "1.2.3.4", "Client"),
               new ClientInstance("Server01", "1.2.3.5", "Server"));

            this.mockFixture.ApiClient.CreateStateAsync<PostgreSQLServerState>(
                nameof(PostgreSQLServerState),
                new PostgreSQLServerState
                {
                    DatabaseInitialized = true,
                    UserCount = 10,
                    WarehouseCount = 10,
                    UserName = "anyuser",
                    Password = "anyvalue"
                },
                CancellationToken.None).GetAwaiter().GetResult();

            this.mockFixture.ApiClientManager.OnGetOrCreateApiClient = (id, ipaddress, uri) => this.mockFixture.ApiClient;
        }

        private void SetupServerRole(PlatformID platform)
        {
            this.mockFixture.Setup(platform, Architecture.X64, "Server01").SetupLayout(
               new ClientInstance("Client01", "1.2.3.4", "Client"),
               new ClientInstance("Server01", "1.2.3.5", "Server"));
        }
    }
}
