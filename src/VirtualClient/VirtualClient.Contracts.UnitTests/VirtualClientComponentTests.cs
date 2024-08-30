// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class VirtualClientComponentTests
    {
        private MockFixture fixture;

        [SetUp]
        public void SetupTest()
        {
            this.fixture = new MockFixture();
            this.fixture.SetupMocks();
        }

        [Test]
        public void VirtualClientComponentConstructorsValidateRequiredParameters()
        {
            Assert.Throws<ArgumentException>(() => new TestVirtualClientComponent(null, this.fixture.Parameters));
        }

        [Test]
        public void VirtualClientComponentConstructorsSetPropertiesToExpectedValues()
        {
            // The existence of the layout causes the 'Role' parameter to be added automatically.
            // We want to do a pure parameter comparison.
            this.fixture.Dependencies.RemoveAll<EnvironmentLayout>();
            VirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);

            Assert.IsTrue(object.ReferenceEquals(this.fixture.Dependencies, component.Dependencies));

            CollectionAssert.AreEquivalent(
                this.fixture.Parameters.Select(p => $"{p.Key}={p.Value}"),
                component.Parameters.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void VirtualClientComponentTagsPropertyIsNeverNull()
        {
            VirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, new Dictionary<string, IConvertible>());

            Assert.IsNotNull(component.Tags);
            Assert.IsEmpty(component.Tags);
        }

        [Test]
        public void VirtualClientComponentTagsSupportsCommonListMergingScenario()
        {
            VirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, new Dictionary<string, IConvertible>());

            List<string> mergedTags = new List<string>(component.Tags) { "AnyOtherTag" };

            Assert.IsNotNull(mergedTags);
            Assert.IsTrue(mergedTags.Count == 1);
            Assert.IsTrue(mergedTags.First() == "AnyOtherTag");
        }

        [Test]
        [TestCase("Client", "Client")]
        [TestCase("Client,User", "Client,User")]
        [TestCase("Client;User", "Client,User")]
        public void VirtualClientComponentRolesMatchThoseDefinedInTheParameters(string roles, string expectedRoles)
        {
            // Both 'Role' and 'Roles' are supported parameters.
            this.fixture.Parameters["Role"] = roles;
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            Assert.AreEqual(expectedRoles, string.Join(",", component.Roles));

            this.fixture.Parameters["Roles"] = roles;
            component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            Assert.AreEqual(expectedRoles, string.Join(",", component.Roles));
        }

        [Test]
        [TestCase("Client", "Client")]
        [TestCase("Client,User", "Client,User")]
        [TestCase("Client;User", "Client,User")]
        public void VirtualClientComponentRolesMatchThoseDefinedInTheEnvironmentLayoutWhenTheyAreNotDefinedInTheParameters(string roles, string expectedRoles)
        {
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.4", roles),
                new ClientInstance("AnyOtherClientInstance", "1.2.3.5", "Server")
            });

            // Ensure there are no parameters that define the role.
            this.fixture.Parameters.Remove("Role");
            this.fixture.Parameters.Remove("Roles");

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);

            Assert.AreEqual(expectedRoles, string.Join(",", component.Roles));
        }

        [Test]
        public void VirtualClientComponentCorrectlyDeterminesWhenItIsInAGivenRole_Scenario1()
        {
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.4", "Client"),
                new ClientInstance("AnyOtherClientInstance", "1.2.3.5", "Server")
            });

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);

            // Even if the client is in the Client role, an IP address on the local system must
            // match the address defined in the matching environment layout client instance
            // for that role.
            this.fixture.SystemManagement.Setup(sm => sm.IsLocalIPAddress(It.IsAny<string>()))
                .Returns<string>(ip => ip == "1.2.3.4");

            Assert.IsTrue(component.IsInRole("Client"));
            Assert.IsFalse(component.IsInRole("Server"));
        }

        [Test]
        public void VirtualClientComponentCorrectlyDeterminesWhenItIsInAGivenRole_Scenario2()
        {
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance("AnyOtherClientInstance", "1.2.3.4", "Client"),
                new ClientInstance(Environment.MachineName, "1.2.3.5", "Server")
            });

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);

            // Even if the client is in the Server role, an IP address on the local system must
            // match the address defined in the matching environment layout client instance
            // for that role.
            this.fixture.SystemManagement.Setup(sm => sm.IsLocalIPAddress(It.IsAny<string>()))
                .Returns<string>(ip => ip == "1.2.3.5");

            Assert.IsTrue(component.IsInRole("Server"));
            Assert.IsFalse(component.IsInRole("Client"));
        }

        [Test]
        public void VirtualClientComponentCorrectlyDeterminesWhenItIsInAGivenRole_Scenario3()
        {
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance("AnyOtherClientInstance1", "1.2.3.4", "Client"),
                new ClientInstance("AnyOtherClientInstance2", "1.2.3.5", "Server"),
                new ClientInstance(Environment.MachineName, "1.2.3.6", "Other")
            });

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);

            // Even if the client is in the Other role, an IP address on the local system must
            // match the address defined in the matching environment layout client instance
            // for that role.
            this.fixture.SystemManagement.Setup(sm => sm.IsLocalIPAddress(It.IsAny<string>()))
                .Returns<string>(ip => ip == "1.2.3.6");

            Assert.IsTrue(component.IsInRole("Other"));
            Assert.IsFalse(component.IsInRole("Server"));
            Assert.IsFalse(component.IsInRole("Client"));
        }

        [Test]
        public void VirtualClientComponentRolesAreNotCaseSensitive_Scenario1()
        {
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.4", "Client"),
                new ClientInstance("AnyOtherClientInstance2", "1.2.3.5", "Server"),
            });

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);

            // Even if the client is in the Client role, an IP address on the local system must
            // match the address defined in the matching environment layout client instance
            // for that role.
            this.fixture.SystemManagement.Setup(sm => sm.IsLocalIPAddress(It.IsAny<string>()))
                .Returns<string>(ip => ip == "1.2.3.4");

            Assert.IsTrue(component.IsInRole("Client".ToUpperInvariant()));
            Assert.IsTrue(component.IsInRole("Client".ToLowerInvariant()));
            Assert.IsFalse(component.IsInRole("Server".ToUpperInvariant()));
            Assert.IsFalse(component.IsInRole("Server".ToLowerInvariant()));
        }

        [Test]
        public void VirtualClientComponentRolesAreNotCaseSensitive_Scenario2()
        {
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance("AnyOtherClientInstance2", "1.2.3.4", "Client"),
                new ClientInstance(Environment.MachineName, "1.2.3.5", "Server"),
            });

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);

            // Even if the client is in the Server role, an IP address on the local system must
            // match the address defined in the matching environment layout client instance
            // for that role.
            this.fixture.SystemManagement.Setup(sm => sm.IsLocalIPAddress(It.IsAny<string>()))
                .Returns<string>(ip => ip == "1.2.3.5");

            Assert.IsTrue(component.IsInRole("Server".ToUpperInvariant()));
            Assert.IsTrue(component.IsInRole("Server".ToLowerInvariant()));
            Assert.IsFalse(component.IsInRole("Client".ToUpperInvariant()));
            Assert.IsFalse(component.IsInRole("Client".ToLowerInvariant()));
        }

        [Test]
        public void VirtualClientComponentReturnsTheExpectedClientInstancseFromTheEnvironmentLayoutByRole()
        {
            ClientInstance expectedInstance = this.fixture.Layout.Clients.ElementAt(1);
            VirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);

            IEnumerable<ClientInstance> actualInstances = component.GetLayoutClientInstances(expectedInstance.Role);
            Assert.IsNotNull(actualInstances);
            Assert.IsTrue(object.ReferenceEquals(expectedInstance, actualInstances.First()));
        }

        [Test]
        public void VirtualClientComponentReturnsTheExpectedRoleFromGivenClientInstance()
        {
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.5", "Server"),
            });

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            Assert.AreEqual("Server", component.GetLayoutClientInstance().Role);
        }

        [Test]
        public async Task VirtualClientComponentAlwaysExecutesOnSingleVM_Scenario1()
        {
            this.fixture.Layout = null;
            this.fixture.Parameters.Add("Roles", "Other,Roles");
            
            bool executed = false;

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            component.OnExecute = (EventContext telemetryContext, CancellationToken cancellationToken) =>
            {
                executed = true;
            };

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(executed);
        }

        [Test]
        public async Task VirtualClientComponentAlwaysExecutesOnSingleVM_Scenario2()
        {
            // Scenario:
            // A layout is defined, but there is no role defined.
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.5")
            });

            this.fixture.Parameters.Add("Roles", "Other,Roles");

            bool executed = false;
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            component.OnExecute = (EventContext telemetryContext, CancellationToken cancellationToken) =>
            {
                executed = true;
            };

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(executed);
        }

        [Test]
        public async Task VirtualClientComponentAlwaysExecutesOnSingleVM_Scenario3()
        {
            // Scenario:
            // A layout is defined and there are multiple VC client instances, but there
            // is no role defined.
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.5"),
                new ClientInstance($"{Environment.MachineName}-2", "1.2.3.6"),
                new ClientInstance($"{Environment.MachineName}-3", "1.2.3.7")
            });

            this.fixture.Parameters.Add("Roles", "Other,Roles");

            bool executed = false;
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            component.OnExecute = (EventContext telemetryContext, CancellationToken cancellationToken) =>
            {
                executed = true;
            };

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(executed);
        }

        [Test]
        public async Task VirtualClientComponentAlwaysExecutesOnNoRolesProvided()
        {
            bool executed = false;

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            component.OnExecute = (EventContext telemetryContext, CancellationToken cancellationToken) =>
            {
                executed = true;
            };

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(executed);
        }

        [Test]
        public async Task VirtualClientComponentExecutesOnSpecifiedRoles()
        {
            bool executed = false;
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.5", "server"),
            });
            this.fixture.Parameters.Add("Roles", "Other,Server");


            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            component.OnExecute = (EventContext telemetryContext, CancellationToken cancellationToken) =>
            {
                executed = true;
            };

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(executed);
        }

        [Test]
        public async Task VirtualClientComponentDoesNotExecutesOnSpecifiedRolesDoesNotMatch()
        {
            bool executed = false;
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.5", "Client"),
                new ClientInstance(Environment.MachineName+"Database", "1.2.3.6", "Database")
            });
            this.fixture.Parameters.Add("Role", "Other,Server");


            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            component.OnExecute = (EventContext telemetryContext, CancellationToken cancellationToken) =>
            {
                executed = true;
            };

            await component.ExecuteAsync(CancellationToken.None).ConfigureAwait(false);

            Assert.IsTrue(executed == false);
        }

        [Test]
        public async Task VirtualClientComponentLogsExpectedMetricsOnSuccessfulExecutions_Scenario1()
        {
            // Scenario:
            // The component parameters does NOT have a 'Scenario' defined.

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            component.Parameters.Clear();
            await component.ExecuteAsync(CancellationToken.None);

            var messageLogged = this.fixture.Logger.MessagesLogged($"{component.TypeName}.ScenarioResult");
            Assert.IsTrue(messageLogged.Count() == 1);

            EventContext context = messageLogged.First().Item3 as EventContext;

            Assert.IsNotNull(context);
            Assert.IsTrue(
                context.Properties.ContainsKey("scenarioName")
                && context.Properties.ContainsKey("metricName")
                && context.Properties.ContainsKey("metricValue")
                && context.Properties.ContainsKey("metricDescription")
                && context.Properties.ContainsKey("metricRelativity")
                && context.Properties.ContainsKey("toolName")
                && context.Properties["scenarioName"].ToString() == "Outcome"
                && context.Properties["metricName"].ToString() == "Succeeded"
                && context.Properties["metricValue"].ToString() == "1"
                && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution succeeded for the scenario defined."
                && context.Properties["metricRelativity"].ToString() == MetricRelativity.HigherIsBetter.ToString()
                && context.Properties["toolName"].ToString() == component.TypeName);
        }

        [Test]
        public async Task VirtualClientComponentLogsExpectedMetricsOnSuccessfulExecutions_Scenario2()
        {
            // Scenario:
            // The component parameters have a 'Scenario' defined.

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            component.Parameters[nameof(component.Scenario)] = "AnyScenarioDefined";
            await component.ExecuteAsync(CancellationToken.None);

            var messageLogged = this.fixture.Logger.MessagesLogged($"{component.TypeName}.ScenarioResult");
            Assert.IsTrue(messageLogged.Count() == 1);

            EventContext context = messageLogged.First().Item3 as EventContext;

            Assert.IsNotNull(context);
            Assert.IsTrue(
                context.Properties.ContainsKey("scenarioName")
                && context.Properties.ContainsKey("metricName")
                && context.Properties.ContainsKey("metricValue")
                && context.Properties.ContainsKey("metricDescription")
                && context.Properties.ContainsKey("metricRelativity")
                && context.Properties.ContainsKey("toolName")
                && context.Properties["scenarioName"].ToString() == "AnyScenarioDefined"
                && context.Properties["metricName"].ToString() == "Succeeded"
                && context.Properties["metricValue"].ToString() == "1"
                && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution succeeded for the scenario defined."
                && context.Properties["metricRelativity"].ToString() == MetricRelativity.HigherIsBetter.ToString()
                && context.Properties["toolName"].ToString() == component.TypeName);
        }

        [Test]
        public async Task VirtualClientComponentLogsExpectedMetricsOnSuccessfulExecutions_Scenario3()
        {
            // Scenario:
            // The component parameters have a 'Scenario' defined as well as a 'MetricScenario'.

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            component.Parameters[nameof(component.Scenario)] = "AnyScenarioDefined";
            component.Parameters[nameof(component.MetricScenario)] = "AnyMetricScenarioDefined";

            await component.ExecuteAsync(CancellationToken.None);

            var messageLogged = this.fixture.Logger.MessagesLogged($"{component.TypeName}.ScenarioResult");
            Assert.IsTrue(messageLogged.Count() == 1);

            EventContext context = messageLogged.First().Item3 as EventContext;

            Assert.IsNotNull(context);
            Assert.IsTrue(
                context.Properties.ContainsKey("scenarioName")
                && context.Properties.ContainsKey("metricName")
                && context.Properties.ContainsKey("metricValue")
                && context.Properties.ContainsKey("metricDescription")
                && context.Properties.ContainsKey("metricRelativity")
                && context.Properties.ContainsKey("toolName")
                && context.Properties["scenarioName"].ToString() == "AnyMetricScenarioDefined"
                && context.Properties["metricName"].ToString() == "Succeeded"
                && context.Properties["metricValue"].ToString() == "1"
                && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution succeeded for the scenario defined."
                && context.Properties["metricRelativity"].ToString() == MetricRelativity.HigherIsBetter.ToString()
                && context.Properties["toolName"].ToString() == component.TypeName);
        }

        [Test]
        public async Task VirtualClientComponentLogsExpectedMetricsOnFailedExecutions_Scenario1()
        {
            // Scenario:
            // The component parameters does NOT have a 'Scenario' defined.

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            component.Parameters.Clear();

            try
            {
                // Cause the execution to fail.
                component.OnExecute = (telemetryContext, token) => throw new WorkloadException($"Any failure reason");
                await component.ExecuteAsync(CancellationToken.None);
            }
            catch
            {
                // Exception is expected to surface.
            }

            var messageLogged = this.fixture.Logger.MessagesLogged($"{component.TypeName}.ScenarioResult");
            Assert.IsTrue(messageLogged.Count() == 1);

            EventContext context = messageLogged.First().Item3 as EventContext;

            Assert.IsNotNull(context);
            Assert.IsTrue(
                context.Properties.ContainsKey("scenarioName")
                && context.Properties.ContainsKey("metricName")
                && context.Properties.ContainsKey("metricValue")
                && context.Properties.ContainsKey("metricDescription")
                && context.Properties.ContainsKey("metricRelativity")
                && context.Properties.ContainsKey("toolName")
                && context.Properties["scenarioName"].ToString() == "Outcome"
                && context.Properties["metricName"].ToString() == "Failed"
                && context.Properties["metricValue"].ToString() == "1"
                && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution failed for the scenario defined."
                && context.Properties["metricRelativity"].ToString() == MetricRelativity.LowerIsBetter.ToString()
                && context.Properties["toolName"].ToString() == component.TypeName);
        }

        [Test]
        public async Task VirtualClientComponentLogsExpectedMetricsOnFailedExecutions_Scenario2()
        {
            // Scenario:
            // The component parameters have a 'Scenario' defined.

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            component.Parameters[nameof(component.Scenario)] = "AnyScenarioDefined";

            try
            {
                // Cause the execution to fail.
                component.OnExecute = (telemetryContext, token) => throw new WorkloadException($"Any failure reason");
                await component.ExecuteAsync(CancellationToken.None);
            }
            catch
            {
                // Exception is expected to surface.
            }

            var messageLogged = this.fixture.Logger.MessagesLogged($"{component.TypeName}.ScenarioResult");
            Assert.IsTrue(messageLogged.Count() == 1);

            EventContext context = messageLogged.First().Item3 as EventContext;

            Assert.IsNotNull(context);
            Assert.IsTrue(
                context.Properties.ContainsKey("scenarioName")
                && context.Properties.ContainsKey("metricName")
                && context.Properties.ContainsKey("metricValue")
                && context.Properties.ContainsKey("metricDescription")
                && context.Properties.ContainsKey("metricRelativity")
                && context.Properties.ContainsKey("toolName")
                && context.Properties["scenarioName"].ToString() == "AnyScenarioDefined"
                && context.Properties["metricName"].ToString() == "Failed"
                && context.Properties["metricValue"].ToString() == "1"
                && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution failed for the scenario defined."
                && context.Properties["metricRelativity"].ToString() == MetricRelativity.LowerIsBetter.ToString()
                && context.Properties["toolName"].ToString() == component.TypeName);
        }

        [Test]
        public async Task VirtualClientComponentLogsExpectedMetricsOnFailedExecutions_Scenario3()
        {
            // Scenario:
            // The component parameters have a 'Scenario' defined as well as a 'MetricScenario'.

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture.Dependencies, this.fixture.Parameters);
            component.Parameters[nameof(component.Scenario)] = "AnyScenarioDefined";
            component.Parameters[nameof(component.MetricScenario)] = "AnyMetricScenarioDefined";

            try
            {
                // Cause the execution to fail.
                component.OnExecute = (telemetryContext, token) => throw new WorkloadException($"Any failure reason");
                await component.ExecuteAsync(CancellationToken.None);
            }
            catch
            {
                // Exception is expected to surface.
            }

            var messageLogged = this.fixture.Logger.MessagesLogged($"{component.TypeName}.ScenarioResult");
            Assert.IsTrue(messageLogged.Count() == 1);

            EventContext context = messageLogged.First().Item3 as EventContext;

            Assert.IsNotNull(context);
            Assert.IsTrue(
                context.Properties.ContainsKey("scenarioName")
                && context.Properties.ContainsKey("metricName")
                && context.Properties.ContainsKey("metricValue")
                && context.Properties.ContainsKey("metricDescription")
                && context.Properties.ContainsKey("metricRelativity")
                && context.Properties.ContainsKey("toolName")
                && context.Properties["scenarioName"].ToString() == "AnyMetricScenarioDefined"
                && context.Properties["metricName"].ToString() == "Failed"
                && context.Properties["metricValue"].ToString() == "1"
                && context.Properties["metricDescription"].ToString() == "Indicates the component or toolset execution failed for the scenario defined."
                && context.Properties["metricRelativity"].ToString() == MetricRelativity.LowerIsBetter.ToString()
                && context.Properties["toolName"].ToString() == component.TypeName);
        }

        [Test]
        public void VirtualClientComponentIsSupportedRespectsSupportedPlatformAttribute()
        {
            this.mockFixture.Setup(PlatformID.Unix, System.Runtime.InteropServices.Architecture.Arm64);
            TestVirtualClientComponent2 component = new TestVirtualClientComponent2(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            Assert.IsFalse(VirtualClientComponent.IsSupported(component));
        }

        private class TestVirtualClientComponent : VirtualClientComponent
        {
            public TestVirtualClientComponent(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public Action<EventContext, CancellationToken> OnExecute { get; set; }

            public new bool IsInRole(string role)
            {
                return base.IsInRole(role);
            }

            protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                this.OnExecute?.Invoke(telemetryContext, cancellationToken);
                return Task.CompletedTask;
            }

            public Func<string, bool> OnIsIPAddressPresent { get; set; }
        }

        /// <summary>
        /// This class is only to test the SupportedPlatforms attribute. Only not supported platform is linux-arm64.
        /// </summary>
        [SupportedPlatforms("linux-x64,win-arm64,win-x64")]
        private class TestVirtualClientComponent2 : VirtualClientComponent
        {
            public TestVirtualClientComponent2(IServiceCollection dependencies, IDictionary<string, IConvertible> parameters)
                : base(dependencies, parameters)
            {
            }

            public Action<EventContext, CancellationToken> OnExecute { get; set; }

            protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                this.OnExecute?.Invoke(telemetryContext, cancellationToken);
                return Task.CompletedTask;
            }

            public new bool IsSupported()
            {
                return base.IsSupported();
            }
        }
    }
}
