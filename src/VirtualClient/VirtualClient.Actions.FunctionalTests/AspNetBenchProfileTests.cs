// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class AspNetBenchProfileTests
    {
        private DependencyFixture fixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.fixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-ASPNETBENCH.json")]
        public void AspNetBenchWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.fixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-ASPNETBENCH.json")]
        public async Task AspNetBenchWorkloadProfileExecutesTheExpectedWorkloadsOnWindowsPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(PlatformID.Win32NT);
            this.SetupDefaultMockBehaviors(PlatformID.Win32NT);
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("bombardier", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_AspNetBench.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-ASPNETBENCH.json")]
        public async Task AspNetBenchWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(PlatformID.Unix);
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.

            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("bombardier", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_AspNetBench.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        private IEnumerable<string> GetProfileExpectedCommands(PlatformID platform)
        {
            List<string> commands = null;
            switch (platform)
            {
                case PlatformID.Win32NT:
                    commands = new List<string>
                    {
                        @"dotnet\.exe build -c Release -p:BenchmarksTargetFramework=net8.0",
                        @"dotnet\.exe .+Benchmarks.dll --nonInteractive true --scenarios json --urls http://localhost:9876 --server Kestrel --kestrelTransport Sockets --protocol http --header ""Accept:.+ keep-alive",
                        @"bombardier\.exe --duration 15s --connections 256 --timeout 10s --fasthttp --insecure -l http://localhost:9876/json --print r --format json"
                    };
                    break;

                case PlatformID.Unix:
                    commands = new List<string>
                    {
                        @"chmod \+x .+bombardier",
                        @"dotnet build -c Release -p:BenchmarksTargetFramework=net8.0",
                        @"dotnet .+Benchmarks.dll --nonInteractive true --scenarios json --urls http://localhost:9876 --server Kestrel --kestrelTransport Sockets --protocol http --header ""Accept:.+ keep-alive",
                        @"bombardier --duration 15s --connections 256 --timeout 10s --fasthttp --insecure -l http://localhost:9876/json --print r --format json"
                    };
                    break;
            }

            return commands;
        }

        private void SetupDefaultMockBehaviors(PlatformID platform)
        {
            if (platform == PlatformID.Win32NT)
            {
                this.fixture.Setup(PlatformID.Win32NT);
                this.fixture.SetupWorkloadPackage("aspnetbenchmarks", expectedFiles: @"aspnetbench");
                this.fixture.SetupWorkloadPackage("bombardier", expectedFiles: @"win-x64\bombardier.exe");
                this.fixture.SetupWorkloadPackage("dotnetsdk", expectedFiles: @"packages\dotnet\dotnet.exe");
            }
            else
            {
                this.fixture.Setup(PlatformID.Unix);

                this.fixture.SetupWorkloadPackage("aspnetbenchmarks", expectedFiles: @"aspnetbench");
                this.fixture.SetupWorkloadPackage("bombardier", expectedFiles: @"linux-x64\bombardier");
                this.fixture.SetupWorkloadPackage("dotnetsdk", expectedFiles: @"packages\dotnet\dotnet");
            }

            this.fixture.SetupDisks(withRemoteDisks: false);
        }
    }
}
