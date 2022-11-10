// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using VirtualClient.Common.Telemetry;

    [TestFixture]
    [Category("Unit")]
    public class VirtualClientComponentExtensionsTests
    {
        private MockFixture fixture;

        [SetUp]
        public void SetupDefaults()
        {
            this.fixture = new MockFixture();
        }

        [Test]
        public void CombineExtensionProducesTheExpectedPathOnWindowsSystems()
        {
            this.fixture.Setup(PlatformID.Win32NT);
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                Assert.AreEqual(@"C:\any\path\with\sub\directories", component.Combine(@"C:\any\path", "with", "sub", "directories"));
            }
        }

        [Test]
        public void CombineExtensionProducesTheExpectedPathOnUnixSystems()
        {
            this.fixture.Setup(PlatformID.Unix);
            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                Assert.AreEqual("/any/path/with/sub/directories", component.Combine("/any/path", "with", "sub", "directories"));
            }
        }

        [Test]
        public void VerifyLayoutDefinedExtensionThrowsWhenVerifyingTheEnvironmentLayoutIfItDoesNotExist()
        {
            // The environment layout is not provided.
            this.fixture.Dependencies.RemoveAll<EnvironmentLayout>();
            VirtualClientComponent component = new TestVirtualClientComponent(this.fixture);
            this.fixture.Dependencies.RemoveAll<EnvironmentLayout>();

            DependencyException error = Assert.Throws<DependencyException>(() => component.ThrowIfLayoutNotDefined());
            Assert.AreEqual(ErrorReason.EnvironmentLayoutNotDefined, error.Reason);
        }

        [Test]
        public void GetLayoutClientInstanceExtensionThrowsWhenMatchingClientInstancesDoNotExistInTheEnvironmentLayout()
        {
            VirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            // Matching client instance does not exist by agent ID
            DependencyException error = Assert.Throws<DependencyException>(() => component.GetLayoutClientInstance("NonExistentAgentID"));
            Assert.AreEqual(ErrorReason.EnvironmentLayoutClientInstancesNotFound, error.Reason);
        }

        [Test]
        public void GetLayoutClientInstancesExtensionThrowsWhenMatchingClientInstancesDoNotExistInTheEnvironmentLayoutWithTheRoleSpecified()
        {
            VirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            // Matching client instance(s) not found by role
            DependencyException error = Assert.Throws<DependencyException>(() => component.GetLayoutClientInstances("NonExistentAgentRole"));
            Assert.AreEqual(ErrorReason.EnvironmentLayoutClientInstancesNotFound, error.Reason);
        }

        [Test]
        public void GetLayoutClientInstanceExtensionReturnsTheExpectedClientInstanceFromTheEnvironmentLayoutForAGivenAgentId()
        {
            ClientInstance expectedInstance = this.fixture.Layout.Clients.ElementAt(1);
            VirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            ClientInstance actualInstance = component.GetLayoutClientInstance(expectedInstance.Name);
            Assert.IsTrue(object.ReferenceEquals(expectedInstance, actualInstance));
        }

        [Test]
        public void GetLayoutClientInstanceExtensionReturnsTheExpectedClientInstanceFromTheEnvironmentLayoutWhenAnAgentIdIsNotExplicitlyProvided()
        {
            // The mock fixture will set one of the client instances to have the same
            // name/agent ID as the current machine name.
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance(Environment.MachineName, "1.2.3.4", "Client"),
                new ClientInstance("AnyOtherClientInstance", "1.2.3.5", "Server")
            });

            ClientInstance expectedInstance = this.fixture.Layout.Clients.First();
            VirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            // When the agent ID is not provided, the ID of the current instance of the Virtual Client will
            // be used. In test scenarios, this will be the current machine name.
            ClientInstance actualInstance = component.GetLayoutClientInstance();
            Assert.IsTrue(object.ReferenceEquals(expectedInstance, actualInstance));
        }

        [Test]
        public void IsMultiRoleLayoutExtensionCorrectlyDeterminesWhenItIsInAMultiRoleScenarioBasedOnTheEnvironmentLayout()
        {
            // No layout provided, we have to assume we are not in a multi-role scenario.
            this.fixture.Layout = null;
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);
            Assert.IsFalse(component.IsMultiRoleLayout());

            // A layout is provided but there are no roles defined.
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance("AnyClient1", "1.2.3.4"),
                new ClientInstance("AnyClient2", "1.2.3.5")
            });

            component = new TestVirtualClientComponent(this.fixture);
            Assert.IsFalse(component.IsMultiRoleLayout());

            // A layout is provided but there is only 1 distinct role.
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance("AnyClient1", "1.2.3.4", "Client"),
                new ClientInstance("AnyClient2", "1.2.3.5", "Client")
            });

            component = new TestVirtualClientComponent(this.fixture);
            Assert.IsFalse(component.IsMultiRoleLayout());

            // A layout is provided and there are more than 1 distinct role. This is multi-role.
            this.fixture.Layout = new EnvironmentLayout(new List<ClientInstance>
            {
                new ClientInstance("AnyClient1", "1.2.3.4", "Client"),
                new ClientInstance("AnyClient2", "1.2.3.5", "Server")
            });

            component = new TestVirtualClientComponent(this.fixture);
            Assert.IsTrue(component.IsMultiRoleLayout());
        }

        [Test]
        public void ThrowIfParameterNotDefinedDoesNotThrowWhenExpectedParametersExist()
        {
            this.fixture.Parameters["RequiredParameter"] = "Value";
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            Assert.DoesNotThrow(() => component.ThrowIfParameterNotDefined("RequiredParameter"));
        }

        [Test]
        public void ThrowIfParameterNotDefinedThrowsIfAnExpectedParameterIsNotDefined()
        {
            this.fixture.Parameters.Clear();
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            Assert.Throws<DependencyException>(() => component.ThrowIfParameterNotDefined("AnyRequiredParameter"));
        }

        [Test]
        [TestCase("Value1", "Value2")]
        [TestCase(1, 2)]
        public void ThrowIfParameterNotDefinedThrowsIfAParameterHasAnUnsupportedValue(IConvertible value, IConvertible supportedValues)
        {
            this.fixture.Parameters["RequiredParameter"] = value;
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            Assert.Throws<DependencyException>(() => component.ThrowIfParameterNotDefined("RequiredParameter", supportedValues));
        }

        [Test]
        [TestCase(1, "1")]
        public void ThrowIfParameterNotDefinedHandlesConvertibleValuesForUnsupportedValue(IConvertible value, IConvertible supportedValues)
        {
            this.fixture.Parameters["RequiredParameter"] = value;
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            Assert.DoesNotThrow(() => component.ThrowIfParameterNotDefined("RequiredParameter", supportedValues));
        }

        [Test]
        public void ThrowIfRoleNotSupportedExtensionThrowsIfTheRoleSuppliedIsNotSupportedByTheWorkload()
        {
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            component.SupportedRoles = new List<string> { "AnyRole" };
            Assert.Throws<NotSupportedException>(() => component.ThrowIfRoleNotSupported("AnyOtherRole"));
        }

        [Test]
        public void ThrowIfRoleNotSupportedExtensionDoesNotThrowsIfTheWorkloadHasNoSupportedRolesDefined()
        {
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            component.SupportedRoles = null;
            Assert.DoesNotThrow(() => component.ThrowIfRoleNotSupported("AnyRole"));
        }

        [Test]
        public void ThrowIfRoleNotSupportedExtensionDoesNotThrowsIfTheRoleIsSupportedByTheWorkload()
        {
            TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture);

            component.SupportedRoles = new List<string> { "AnyRole" };
            Assert.DoesNotThrow(() => component.ThrowIfRoleNotSupported("AnyRole"));
        }

        [Test]
        public void ToPlatformSpecificPathExtensionProducesTheExpectedPathOnUnixSystems()
        {
            this.fixture.Setup(PlatformID.Unix);
            DependencyPath dependency = new DependencyPath("AnyDependency", "/any/path");

            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                Assert.AreEqual(
                    $"{dependency.Path}/linux",
                    component.ToPlatformSpecificPath(dependency, PlatformID.Unix).Path);

                Assert.AreEqual(
                   $"{dependency.Path}/linux-x64",
                   component.ToPlatformSpecificPath(dependency, PlatformID.Unix, Architecture.X64).Path);

                Assert.AreEqual(
                   $"{dependency.Path}/linux-arm64",
                   component.ToPlatformSpecificPath(dependency, PlatformID.Unix, Architecture.Arm64).Path);
            }
        }

        [Test]
        public void ToPlatformSpecificPathExtensionProducesTheExpectedPathOnWindowsSystems()
        {
            this.fixture.Setup(PlatformID.Win32NT);
            DependencyPath dependency = new DependencyPath("AnyDependency", @"C:\any\path");

            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                Assert.AreEqual(
                    $@"{dependency.Path}\win",
                    component.ToPlatformSpecificPath(dependency, PlatformID.Win32NT).Path);

                Assert.AreEqual(
                   $@"{dependency.Path}\win-x64",
                   component.ToPlatformSpecificPath(dependency, PlatformID.Win32NT, Architecture.X64).Path);

                Assert.AreEqual(
                   $@"{dependency.Path}\win-arm64",
                   component.ToPlatformSpecificPath(dependency, PlatformID.Win32NT, Architecture.Arm64).Path);
            }
        }

        [Test]
        [TestCase(PlatformID.Other)]
        public void ToPlatformSpecificPathThrowsIfThePlatformDefinedIsNotSupported(PlatformID unsupportedPlatform)
        {
            this.fixture.Setup(PlatformID.Win32NT);
            DependencyPath dependency = new DependencyPath("AnyDependency", @"C:\any\path");

            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                Assert.Throws<NotSupportedException>(() => component.ToPlatformSpecificPath(dependency, unsupportedPlatform));
            }
        }

        [Test]
        [TestCase(Architecture.Arm)]
        [TestCase(Architecture.Wasm)]
        [TestCase(Architecture.X86)]
        public void ToPlatformSpecificPathThrowsIfTheArchitectureDefinedIsNotSupported(Architecture unsupportedArchitecture)
        {
            this.fixture.Setup(PlatformID.Win32NT);
            DependencyPath dependency = new DependencyPath("AnyDependency", @"C:\any\path");

            using (TestVirtualClientComponent component = new TestVirtualClientComponent(this.fixture))
            {
                Assert.Throws<NotSupportedException>(
                    () => component.ToPlatformSpecificPath(dependency, PlatformID.Unix, unsupportedArchitecture));
            }
        }

        private class TestVirtualClientComponent : VirtualClientComponent
        {
            public TestVirtualClientComponent(MockFixture fixture)
                : base(fixture.Dependencies, fixture.Parameters)
            {
            }

            public new IEnumerable<string> SupportedRoles
            {
                get
                {
                    return base.SupportedRoles;
                }

                set
                {
                    base.SupportedRoles = value;
                }
            }

            protected override Task ExecuteAsync(EventContext telemetryContext, CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }
}
