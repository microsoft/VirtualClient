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
    public class NginxWrkProfileTests
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
        [TestCase("PERF-WEB-NGINX-WRK.json")]
        [TestCase("PERF-WEB-NGINX-WRK2.json")]
        public void NginxWrkProfileParametersAreInlinedCorrectly(string profile)
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
        [TestCase("PERF-WEB-NGINX-WRK.json")]
        [TestCase("PERF-WEB-NGINX-WRK2.json")]
        public void NginxWrkProfileParametersAreAvailable(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix, agentId: this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.5", ClientRole.Client),
                new ClientInstance(this.serverAgentId, "1.2.3.4", ClientRole.Server));

            var serverPrams = new List<string> { "PackageName", "Role", "Timeout" };

            var reverseProxyPrams = new List<string> { "PackageName", "Role", "Timeout" };

            var clientPrams = new List<string> { "PackageName", "Role", "Timeout", "TestDuration", "FileSizeInKB", "Connection", "ThreadCount", "CommandArguments", "MetricScenario", "Scenario" };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                foreach (var actionBlock in executor.Profile.Actions)
                {
                    string role = actionBlock.Parameters["Role"].ToString();

                    if (role.Equals("server", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var pram in serverPrams)
                        {
                            if (!actionBlock.Parameters.ContainsKey(pram))
                            {
                                Assert.False(true, $"{actionBlock.Type} does not have {pram} parameter.");
                            }
                        }
                    }
                    else if (role.Equals("reverseproxy", StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var pram in reverseProxyPrams)
                        {
                            if (!actionBlock.Parameters.ContainsKey(pram))
                            {
                                Assert.False(true, $"{actionBlock.Type} does not have {pram} parameter.");
                            }
                        }
                    }
                    else
                    {
                        foreach (var pram in clientPrams)
                        {
                            if (!actionBlock.Parameters.ContainsKey(pram))
                            {
                                Assert.False(true, $"{actionBlock.Type} does not have {pram} parameter.");
                            }
                        }
                    }
                }
            }
        }

        [Test]
        [TestCase("PERF-WEB-NGINX-WRK.json")]
        public async Task NginxWrkProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands();
            this.SetupDefaultMockBehaviors();

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                
                // Handle nginx version check (uses stderr)
                if (arguments.Contains("nginx -V", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardError.Append("nginx version: nginx/1.18.0\nbuilt with OpenSSL 1.1.1f\nTLS SNI support enabled");
                }
                
                // Handle nginx setup scripts
                if (arguments.Contains("setup-reset.sh") || arguments.Contains("setup-content.sh") || arguments.Contains("setup-config.sh") || arguments.Contains("reset.sh"))
                {
                    process.StandardOutput.Append("Script executed successfully");
                }
                
                // Handle nginx start/stop commands
                if (arguments.Contains("systemctl"))
                {
                    process.StandardOutput.Append("Service command executed");
                }
                
                // Add wrk results for any wrk execution
                if (command.Contains("wrk", StringComparison.OrdinalIgnoreCase) || 
                    arguments.Contains("wrk", StringComparison.OrdinalIgnoreCase))
                {
                    if (arguments.Contains("--version"))
                    {
                        process.StandardOutput.Append("wrk 4.2.0 [epoll] Copyright (C) 2012 Will Glozer");
                    }
                    else
                    {
                        process.StandardOutput.Append(TestDependencies.GetResourceFileContents("wrkStandardExample1.txt"));
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
        [TestCase("PERF-WEB-NGINX-WRK2.json")]
        public async Task NginxWrk2ProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommandsForWrk2();
            this.SetupDefaultMockBehaviors();

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                
                // Handle nginx version check (uses stderr)
                if (arguments.Contains("nginx -V", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardError.Append("nginx version: nginx/1.18.0\nbuilt with OpenSSL 1.1.1f\nTLS SNI support enabled");
                }
                
                // Handle nginx setup scripts
                if (arguments.Contains("setup-reset.sh") || arguments.Contains("setup-content.sh") || arguments.Contains("setup-config.sh") || arguments.Contains("reset.sh"))
                {
                    process.StandardOutput.Append("Script executed successfully");
                }
                
                // Handle nginx start/stop commands
                if (arguments.Contains("systemctl"))
                {
                    process.StandardOutput.Append("Service command executed");
                }
                
                // Add wrk2 results for any wrk execution
                if (command.Contains("wrk", StringComparison.OrdinalIgnoreCase) || 
                    arguments.Contains("wrk", StringComparison.OrdinalIgnoreCase))
                {
                    if (arguments.Contains("--version"))
                    {
                        process.StandardOutput.Append("wrk 4.2.0 [epoll] Copyright (C) 2012 Will Glozer");
                    }
                    else
                    {
                        process.StandardOutput.Append(TestDependencies.GetResourceFileContents("wrkStandardExample1.txt"));
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

        private IEnumerable<string> GetProfileExpectedCommands()
        {
            // Expected commands for PERF-WEB-NGINX-WRK.json
            return new List<string>
            {
                @"chmod \+x .+wrk",
                @"sudo systemctl stop nginx",
                @"sudo systemctl start nginx",
                @"bash .+runwrk\.sh --version",
                // Sample commands for various connection scenarios
                @"bash .+runwrk\.sh ""--latency --threads \d+ --connections 100 --duration 150s --timeout 10s https://1\.2\.3\.4/api_new/1kb""",
                @"bash .+runwrk\.sh ""--latency --threads \d+ --connections 1000 --duration 150s --timeout 10s https://1\.2\.3\.4/api_new/1kb""",
                @"bash .+runwrk\.sh ""--latency --threads \d+ --connections 5000 --duration 150s --timeout 10s https://1\.2\.3\.4/api_new/1kb""",
                @"bash .+runwrk\.sh ""--latency --threads \d+ --connections 10000 --duration 150s --timeout 10s https://1\.2\.3\.4/api_new/1kb"""
            };
        }

        private IEnumerable<string> GetProfileExpectedCommandsForWrk2()
        {
            // Expected commands for PERF-WEB-NGINX-WRK2.json
            return new List<string>
            {
                @"chmod \+x .+wrk",
                @"sudo systemctl stop nginx",
                @"sudo systemctl start nginx",
                @"bash .+runwrk\.sh --version",
                // Sample commands for various rate and connection scenarios
                @"bash .+runwrk\.sh ""--rate 1000 --latency --threads \d+ --connections 100 --duration 150s --timeout 10s https://1\.2\.3\.4/api_new/1kb""",
                @"bash .+runwrk\.sh ""--rate 1000 --latency --threads \d+ --connections 1000 --duration 150s --timeout 10s https://1\.2\.3\.4/api_new/1kb"""
            };
        }

        private void SetupDefaultMockBehaviors()
        {
            this.mockFixture.Setup(PlatformID.Unix, agentId: this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.5", ClientRole.Client),
                new ClientInstance(this.serverAgentId, "1.2.3.4", ClientRole.Server));

            // Setup nginx configuration package with expected files
            string nginxPackagePath = this.mockFixture.GetPackagePath("nginxconfiguration");
            this.mockFixture.SetupPackage("nginxconfiguration", expectedFiles: new string[]
            {
                "setup-reset.sh",
                "setup-config.sh",
                "setup-content.sh",
                "reset.sh",
                "nginx.conf"
            });

            // Mock the required files exist in the filesystem
            this.mockFixture.SetupFile($"{nginxPackagePath}/linux-x64/setup-reset.sh");
            this.mockFixture.SetupFile($"{nginxPackagePath}/linux-x64/setup-config.sh");
            this.mockFixture.SetupFile($"{nginxPackagePath}/linux-x64/setup-content.sh");
            this.mockFixture.SetupFile($"{nginxPackagePath}/linux-x64/reset.sh");

            // Setup wrk configuration package
            this.mockFixture.SetupPackage("wrkconfiguration", expectedFiles: @"runwrk.sh");
            
            // Setup wrk package
            this.mockFixture.SetupPackage("wrk", expectedFiles: @"wrk");

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
