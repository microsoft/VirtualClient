// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
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
        private string clientAgentId;
        private string serverAgentId;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.clientAgentId = $"{Environment.MachineName}-Client";
            this.serverAgentId = $"{Environment.MachineName}-Server";

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [SetUp]
        public void Setup()
        {
            this.mockFixture = new DependencyFixture();
        }

        [Test]
        [TestCase("PERF-ASPNETBENCH.json")]
        public void AspNetBenchWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix, agentId: this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.5", ClientRole.Client),
                new ClientInstance(this.serverAgentId, "1.2.3.4", ClientRole.Server));

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-ASPNETBENCH-AFFINITY.json")]
        public void AspNetBenchAffinityWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix, agentId: this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.5", ClientRole.Client),
                new ClientInstance(this.serverAgentId, "1.2.3.4", ClientRole.Server));

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

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                
                // Add bombardier results for any bombardier execution (with or without affinity)
                if (command.Contains("bombardier", StringComparison.OrdinalIgnoreCase) || 
                    arguments.Contains("bombardier", StringComparison.OrdinalIgnoreCase))
                {
                    if (arguments.Contains("--version"))
                    {
                        process.StandardOutput.Append("bombardier version 1.2.5");
                    }
                    else
                    {
                        process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_AspNetBench.txt"));
                    }
                }

                return process;
            };

            // Setup API client for client-server communication
            this.SetupApiClient(this.serverAgentId, "1.2.3.4");

            // Execute server actions
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(this.serverAgentId);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
            }

            // Execute client actions
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(this.clientAgentId);
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

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                
                // Add bombardier results for any bombardier execution (with or without affinity)
                if (command.Contains("bombardier", StringComparison.OrdinalIgnoreCase) || 
                    arguments.Contains("bombardier", StringComparison.OrdinalIgnoreCase))
                {
                    if (arguments.Contains("--version"))
                    {
                        process.StandardOutput.Append("bombardier version 1.2.5");
                    }
                    else
                    {
                        process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_AspNetBench.txt"));
                    }
                }

                return process;
            };

            // Setup API client for client-server communication
            this.SetupApiClient(this.serverAgentId, "1.2.3.4");

            // Execute server actions
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(this.serverAgentId);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
            }

            // Execute client actions
            this.mockFixture.SystemManagement.SetupGet(obj => obj.AgentId).Returns(this.clientAgentId);
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
                        @"pkill dotnet",
                        @"fuser -n tcp -k 9876",
                        @"dotnet\.exe build -c Release -p:BenchmarksTargetFramework=net8.0",
                        @"dotnet\.exe .+Benchmarks\.dll --nonInteractive true --scenarios json --urls http://\*:9876 --server Kestrel --kestrelTransport Sockets --protocol http --header ""Accept:.+ keep-alive",
                        @"bombardier\.exe --duration 15s --connections 256 --timeout 10s --fasthttp --insecure -l http://1\.2\.3\.4:9876/json --print r --format json"
                    };
                    break;

                case PlatformID.Unix:
                    commands = new List<string>
                    {
                        @"chmod \+x .+bombardier",
                        @"pkill dotnet",
                        @"fuser -n tcp -k 9876",
                        @"dotnet build -c Release -p:BenchmarksTargetFramework=net8.0",
                        @"dotnet .+Benchmarks\.dll --nonInteractive true --scenarios json --urls http://\*:9876 --server Kestrel --kestrelTransport Sockets --protocol http --header ""Accept:.+ keep-alive",
                        @"bombardier --duration 15s --connections 256 --timeout 10s --fasthttp --insecure -l http://1\.2\.3\.4:9876/json --print r --format json"
                    };
                    break;
            }

            return commands;
        }

        private void SetupDefaultMockBehaviors(PlatformID platform)
        {
            if (platform == PlatformID.Win32NT)
            {
                this.mockFixture.Setup(PlatformID.Win32NT, agentId: this.clientAgentId).SetupLayout(
                    new ClientInstance(this.clientAgentId, "1.2.3.5", ClientRole.Client),
                    new ClientInstance(this.serverAgentId, "1.2.3.4", ClientRole.Server));

                this.mockFixture.SetupPackage("aspnetbenchmarks", expectedFiles: @"aspnetbench");
                this.mockFixture.SetupPackage("bombardier", expectedFiles: @"bombardier.exe");
                this.mockFixture.SetupPackage("dotnetsdk", expectedFiles: @"dotnet.exe");
            }
            else
            {
                this.mockFixture.Setup(PlatformID.Unix, agentId: this.clientAgentId).SetupLayout(
                    new ClientInstance(this.clientAgentId, "1.2.3.5", ClientRole.Client),
                    new ClientInstance(this.serverAgentId, "1.2.3.4", ClientRole.Server));

                this.mockFixture.SetupPackage("aspnetbenchmarks", expectedFiles: @"aspnetbench");
                this.mockFixture.SetupPackage("bombardier", expectedFiles: @"bombardier");
                this.mockFixture.SetupPackage("dotnetsdk", expectedFiles: @"dotnet");
            }

            this.mockFixture.SetupDisks(withRemoteDisks: false);
        }

        private void SetupApiClient(string serverName, string serverIPAddress)
        {
            IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress);
            IApiClient apiClient = this.mockFixture.ApiClientManager.GetOrCreateApiClient(serverName, ipAddress);

            State state = new State();
            state.Online(true);

            apiClient.CreateStateAsync(nameof(State), state, CancellationToken.None)
                .GetAwaiter().GetResult();
        }
    }
}
