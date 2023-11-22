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
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class VirtualClientComponentTests
    {
        private MockFixture mockFixture;

        [SetUp]
        public void SetupTest()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.SetupMocks();
        }

        [Test]
        public void VirtualClientComponentConstructorsValidateRequiredParameters()
        {
            Assert.Throws<ArgumentException>(() => new TestVirtualClientComponent(null, this.mockFixture.Parameters));
        }

        [Test]
        public void VirtualClientComponentConstructorsSetPropertiesToExpectedValues()
        {
            VirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            Assert.IsTrue(object.ReferenceEquals(this.mockFixture.Dependencies, component.Dependencies));
            CollectionAssert.AreEquivalent(
                this.mockFixture.Parameters.Select(p => $"{p.Key}={p.Value}"),
                component.Parameters.Select(p => $"{p.Key}={p.Value}"));
        }

        [Test]
        public void VirtualClientComponentTagsPropertyIsNeverNull()
        {
            VirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, new Dictionary<string, IConvertible>());

            Assert.IsNotNull(component.Tags);
            Assert.IsEmpty(component.Tags);
        }

        [Test]
        public void VirtualClientComponentTagsSupportsCommonListMergingScenario()
        {
            VirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, new Dictionary<string, IConvertible>());

            List<string> mergedTags = new List<string>(component.Tags) { "AnyOtherTag" };

            Assert.IsNotNull(mergedTags);
            Assert.IsTrue(mergedTags.Count == 1);
            Assert.IsTrue(mergedTags.First() == "AnyOtherTag");
        }

        [Test]
        public void VirtualClientComponentCorrectlyDeterminesWhenItIsInAGivenRole_Scenario1()
        {
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.4", "Client"),
                new ClientInstance("AnyOtherClientInstance", "1.2.3.5", "Server")
            });

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // Even if the client is in the Client role, an IP address on the local system must
            // match the address defined in the matching environment layout client instance
            // for that role.
            this.mockFixture.SystemManagement.Setup(sm => sm.IsLocalIPAddress(It.IsAny<string>()))
                .Returns<string>(ip => ip == "1.2.3.4");

            Assert.IsTrue(component.IsInRole("Client"));
            Assert.IsFalse(component.IsInRole("Server"));
        }

        [Test]
        public void VirtualClientComponentCorrectlyDeterminesWhenItIsInAGivenRole_Scenario2()
        {
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance("AnyOtherClientInstance", "1.2.3.4", "Client"),
                new ClientInstance(Environment.MachineName, "1.2.3.5", "Server")
            });

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // Even if the client is in the Server role, an IP address on the local system must
            // match the address defined in the matching environment layout client instance
            // for that role.
            this.mockFixture.SystemManagement.Setup(sm => sm.IsLocalIPAddress(It.IsAny<string>()))
                .Returns<string>(ip => ip == "1.2.3.5");

            Assert.IsTrue(component.IsInRole("Server"));
            Assert.IsFalse(component.IsInRole("Client"));
        }

        [Test]
        public void VirtualClientComponentCorrectlyDeterminesWhenItIsInAGivenRole_Scenario3()
        {
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance("AnyOtherClientInstance1", "1.2.3.4", "Client"),
                new ClientInstance("AnyOtherClientInstance2", "1.2.3.5", "Server"),
                new ClientInstance(Environment.MachineName, "1.2.3.6", "Other")
            });

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // Even if the client is in the Other role, an IP address on the local system must
            // match the address defined in the matching environment layout client instance
            // for that role.
            this.mockFixture.SystemManagement.Setup(sm => sm.IsLocalIPAddress(It.IsAny<string>()))
                .Returns<string>(ip => ip == "1.2.3.6");

            Assert.IsTrue(component.IsInRole("Other"));
            Assert.IsFalse(component.IsInRole("Server"));
            Assert.IsFalse(component.IsInRole("Client"));
        }

        [Test]
        public void VirtualClientComponentRolesAreNotCaseSensitive_Scenario1()
        {
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.4", "Client"),
                new ClientInstance("AnyOtherClientInstance2", "1.2.3.5", "Server"),
            });

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // Even if the client is in the Client role, an IP address on the local system must
            // match the address defined in the matching environment layout client instance
            // for that role.
            this.mockFixture.SystemManagement.Setup(sm => sm.IsLocalIPAddress(It.IsAny<string>()))
                .Returns<string>(ip => ip == "1.2.3.4");

            Assert.IsTrue(component.IsInRole("Client".ToUpperInvariant()));
            Assert.IsTrue(component.IsInRole("Client".ToLowerInvariant()));
            Assert.IsFalse(component.IsInRole("Server".ToUpperInvariant()));
            Assert.IsFalse(component.IsInRole("Server".ToLowerInvariant()));
        }

        [Test]
        public void VirtualClientComponentRolesAreNotCaseSensitive_Scenario2()
        {
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance("AnyOtherClientInstance2", "1.2.3.4", "Client"),
                new ClientInstance(Environment.MachineName, "1.2.3.5", "Server"),
            });

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            // Even if the client is in the Server role, an IP address on the local system must
            // match the address defined in the matching environment layout client instance
            // for that role.
            this.mockFixture.SystemManagement.Setup(sm => sm.IsLocalIPAddress(It.IsAny<string>()))
                .Returns<string>(ip => ip == "1.2.3.5");

            Assert.IsTrue(component.IsInRole("Server".ToUpperInvariant()));
            Assert.IsTrue(component.IsInRole("Server".ToLowerInvariant()));
            Assert.IsFalse(component.IsInRole("Client".ToUpperInvariant()));
            Assert.IsFalse(component.IsInRole("Client".ToLowerInvariant()));
        }

        [Test]
        public void VirtualClientComponentReturnsTheExpectedClientInstancseFromTheEnvironmentLayoutByRole()
        {
            ClientInstance expectedInstance = this.mockFixture.Layout.Clients.ElementAt(1);
            VirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);

            IEnumerable<ClientInstance> actualInstances = component.GetLayoutClientInstances(expectedInstance.Role);
            Assert.IsNotNull(actualInstances);
            Assert.IsTrue(object.ReferenceEquals(expectedInstance, actualInstances.First()));
        }

        [Test]
        public void VirtualClientComponentReturnsTheExpectedRoleFromGivenClientInstance()
        {
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.5", "Server"),
            });

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            Assert.AreEqual("Server", component.GetLayoutClientInstance().Role);
        }

        [Test]
        public async Task VirtualClientComponentAlwaysExecutesOnSingleVM_Scenario1()
        {
            this.mockFixture.Layout = null;
            this.mockFixture.Parameters.Add("Roles", "Other,Roles");
            
            bool executed = false;

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);
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
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.5")
            });

            this.mockFixture.Parameters.Add("Roles", "Other,Roles");

            bool executed = false;
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);
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
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.5"),
                new ClientInstance($"{Environment.MachineName}-2", "1.2.3.6"),
                new ClientInstance($"{Environment.MachineName}-3", "1.2.3.7")
            });

            this.mockFixture.Parameters.Add("Roles", "Other,Roles");

            bool executed = false;
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);
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

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);
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
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.5", "server"),
            });
            this.mockFixture.Parameters.Add("Roles", "Other,Server");


            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);
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
            this.mockFixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.5", "Client"),
                new ClientInstance(Environment.MachineName+"Database", "1.2.3.6", "Database")
            });
            this.mockFixture.Parameters.Add("Role", "Other,Server");


            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);
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

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            component.Parameters.Clear();
            await component.ExecuteAsync(CancellationToken.None);

            var messageLogged = this.mockFixture.Logger.MessagesLogged($"{component.TypeName}.ScenarioResult");
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

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);
            component.Parameters[nameof(component.Scenario)] = "AnyScenarioDefined";
            await component.ExecuteAsync(CancellationToken.None);

            var messageLogged = this.mockFixture.Logger.MessagesLogged($"{component.TypeName}.ScenarioResult");
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
        public async Task VirtualClientComponentLogsExpectedMetricsOnFailedExecutions_Scenario1()
        {
            // Scenario:
            // The component parameters does NOT have a 'Scenario' defined.

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);
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

            var messageLogged = this.mockFixture.Logger.MessagesLogged($"{component.TypeName}.ScenarioResult");
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

            TestVirtualClientComponent component = new TestVirtualClientComponent(this.mockFixture.Dependencies, this.mockFixture.Parameters);
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

            var messageLogged = this.mockFixture.Logger.MessagesLogged($"{component.TypeName}.ScenarioResult");
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
    }
}
