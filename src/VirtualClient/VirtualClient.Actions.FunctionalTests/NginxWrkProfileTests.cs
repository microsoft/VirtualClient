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
    }
}
