// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class SysbenchOLTPClientProfileTests
    {
        private DependencyFixture mockFixture;
        private string clientAgentId;
        private string serverAgentId;

        [SetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            this.clientAgentId = $"{Environment.MachineName}-Client";
            this.serverAgentId = $"{Environment.MachineName}-Server";

            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-MYSQL-SYSBENCH-OLTP.json", PlatformID.Unix, Architecture.X64)]
        public async Task SysbenchOLTPWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(platform, architecture);
            this.SetupDefaultMockBehaviors(platform, architecture);
            this.SetupApiClient(this.serverAgentId, serverIPAddress: "1.2.3.5");

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("run", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_SysbenchOLTP.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);
                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        private IEnumerable<string> GetProfileExpectedCommands(PlatformID platform, Architecture architecture)
        {
            List<string> commands = null;
            commands = new List<string>
            {
                $"sudo su -c \"bash <( curl -s https://packagecloud.io/install/repositories/akopytov/sysbench/script.deb.sh)\"",
                $"sudo apt install sysbench --yes --quiet",

                $"sudo sysbench oltp_read_write --threads=8 --time=1800 --tables=16 --table-size=500 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=16 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_write --threads=8 --time=1800 --tables=16 --table-size=500 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_write --threads=16 --time=1800 --tables=16 --table-size=1000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=16 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_write --threads=16 --time=1800 --tables=16 --table-size=1000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_write --threads=16 --time=1800 --tables=16 --table-size=5000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=16 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_write --threads=16 --time=1800 --tables=16 --table-size=5000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_write --threads=32 --time=1800 --tables=16 --table-size=10000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=16 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_write --threads=32 --time=1800 --tables=16 --table-size=10000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_write --threads=8 --time=1800 --tables=32 --table-size=50000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=32 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_write --threads=8 --time=1800 --tables=32 --table-size=50000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_write --threads=16 --time=1800 --tables=32 --table-size=500000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=32 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_write --threads=16 --time=1800 --tables=32 --table-size=500000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_write --threads=92 --time=1800 --tables=4 --table-size=50000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=4 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_write --threads=92 --time=1800 --tables=4 --table-size=50000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_write --threads=152 --time=1800 --tables=4 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=4 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_write --threads=152 --time=1800 --tables=4 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_only --threads=8 --time=1800 --tables=16 --table-size=500 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=16 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_only --threads=8 --time=1800 --tables=16 --table-size=500 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_only --threads=16 --time=1800 --tables=16 --table-size=1000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=16 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_only --threads=16 --time=1800 --tables=16 --table-size=1000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_only --threads=16 --time=1800 --tables=16 --table-size=5000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=16 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_only --threads=16 --time=1800 --tables=16 --table-size=5000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_only --threads=32 --time=1800 --tables=16 --table-size=10000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=16 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_only --threads=32 --time=1800 --tables=16 --table-size=10000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_only --threads=8 --time=1800 --tables=32 --table-size=50000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=32 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_only --threads=8 --time=1800 --tables=32 --table-size=50000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_only --threads=16 --time=1800 --tables=32 --table-size=500000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=32 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_only --threads=16 --time=1800 --tables=32 --table-size=500000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_only --threads=92 --time=1800 --tables=4 --table-size=50000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=4 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_only --threads=92 --time=1800 --tables=4 --table-size=50000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_read_only --threads=152 --time=1800 --tables=4 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=4 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_read_only --threads=152 --time=1800 --tables=4 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_write_only --threads=8 --time=1800 --tables=16 --table-size=500 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=16 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_write_only --threads=8 --time=1800 --tables=16 --table-size=500 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_write_only --threads=16 --time=1800 --tables=16 --table-size=1000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=16 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_write_only --threads=16 --time=1800 --tables=16 --table-size=1000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_write_only --threads=16 --time=1800 --tables=16 --table-size=5000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=16 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_write_only --threads=16 --time=1800 --tables=16 --table-size=5000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_write_only --threads=32 --time=1800 --tables=16 --table-size=10000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=16 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_write_only --threads=32 --time=1800 --tables=16 --table-size=10000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_write_only --threads=8 --time=1800 --tables=32 --table-size=50000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=32 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_write_only --threads=8 --time=1800 --tables=32 --table-size=50000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_write_only --threads=16 --time=1800 --tables=32 --table-size=500000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=32 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_write_only --threads=16 --time=1800 --tables=32 --table-size=500000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_write_only --threads=92 --time=1800 --tables=4 --table-size=50000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=4 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_write_only --threads=92 --time=1800 --tables=4 --table-size=50000 --mysql-db=sbtest --mysql-host=1.2.3.5 run",

                $"sudo sysbench oltp_write_only --threads=152 --time=1800 --tables=4 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 cleanup",
                $"sudo sysbench oltp_common --tables=4 --mysql-db=sbtest --mysql-host=1.2.3.5 prepare",
                $"sudo sysbench oltp_write_only --threads=152 --time=1800 --tables=4 --table-size=100000 --mysql-db=sbtest --mysql-host=1.2.3.5 run"
            };

            return commands;
        }

        private void SetupApiClient(string serverName, string serverIPAddress)
        {
            IPAddress.TryParse(serverIPAddress, out IPAddress ipAddress);
            IApiClient apiClient = this.mockFixture.ApiClientManager.GetOrCreateApiClient(serverName, ipAddress);
        }

        private void SetupDefaultMockBehaviors(PlatformID platform, Architecture architecture)
        {
            this.mockFixture.Setup(platform, architecture);
            this.mockFixture.SetupWorkloadPackage("sysbench", expectedFiles: @"sysbench");
            this.mockFixture.Setup(PlatformID.Unix, architecture, this.clientAgentId).SetupLayout(
                new ClientInstance(this.clientAgentId, "1.2.3.4", "Client"),
                new ClientInstance(this.serverAgentId, "1.2.3.5", "Server"));
        }
    }
}
