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
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-ASPNETBENCH.json")]
        public void AspNetBenchWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
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

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("bombardier", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_AspNetBench.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
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

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("bombardier", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_AspNetBench.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
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
                        @"dotnet\.exe build -c Release -p:BenchmarksTargetFramework=net7.0",
                        @"dotnet\.exe .+Benchmarks.dll --nonInteractive true --scenarios json --urls http://localhost:9876 --server Kestrel --kestrelTransport Sockets --protocol http --header ""Accept:.+ keep-alive",
                        @"bombardier\.exe --duration 15s --connections 256 --timeout 10s --fasthttp --insecure -l http://localhost:9876/json --print r --format json"
                    };
                    break;

                case PlatformID.Unix:
                    commands = new List<string>
                    {
                        @"chmod \+x .+bombardier",
                        @"dotnet build -c Release -p:BenchmarksTargetFramework=net7.0",
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
                this.mockFixture.Setup(PlatformID.Win32NT);
                this.mockFixture.SetupWorkloadPackage("aspnetbenchmarks", expectedFiles: @"aspnetbench");
                this.mockFixture.SetupWorkloadPackage("bombardier", expectedFiles: @"win-x64\bombardier.exe");
                this.mockFixture.SetupWorkloadPackage("dotnetsdk", expectedFiles: @"packages\dotnet\dotnet.exe");
            }
            else
            {
                this.mockFixture.Setup(PlatformID.Unix);

                this.mockFixture.SetupWorkloadPackage("aspnetbenchmarks", expectedFiles: @"aspnetbench");
                this.mockFixture.SetupWorkloadPackage("bombardier", expectedFiles: @"linux-x64\bombardier");
                this.mockFixture.SetupWorkloadPackage("dotnetsdk", expectedFiles: @"packages\dotnet\dotnet");
            }

            this.mockFixture.SetupDisks(withRemoteDisks: false);
        }
    }
}
