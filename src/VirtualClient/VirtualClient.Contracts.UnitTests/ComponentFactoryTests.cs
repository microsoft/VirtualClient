// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class ComponentFactoryTests
    {
        private MockFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(PlatformID.Win32NT);

            ComponentTypeCache.Instance.LoadComponentTypes(MockFixture.TestAssemblyDirectory);
        }

        [Test]
        [TestCase("TEST-PROFILE-1.json")]
        [TestCase("TEST-PROFILE-2.json")]
        public void ComponentFactoryCreatesExpectedComponentsFromAnExecutionProfile(string profileName)
        {
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();

            foreach (ExecutionProfileElement action in profile.Actions)
            {
                Assert.DoesNotThrow(() =>
                {
                    VirtualClientComponent component = ComponentFactory.CreateComponent(action, this.mockFixture.Dependencies);
                    Assert.IsNotNull(component);
                    Assert.IsNotEmpty(component.Dependencies);
                    Assert.IsNotNull(component.Parameters);
                });
            }

            foreach (ExecutionProfileElement dependency in profile.Dependencies)
            {
                Assert.DoesNotThrow(() =>
                {
                    VirtualClientComponent component = ComponentFactory.CreateComponent(dependency, this.mockFixture.Dependencies);
                    Assert.IsNotNull(component);
                    Assert.IsNotEmpty(component.Dependencies);
                    Assert.IsNotNull(component.Parameters);
                });
            }

            foreach (ExecutionProfileElement monitor in profile.Monitors)
            {
                Assert.DoesNotThrow(() =>
                {
                    VirtualClientComponent component = ComponentFactory.CreateComponent(monitor, this.mockFixture.Dependencies);
                    Assert.IsNotNull(component);
                    Assert.IsNotEmpty(component.Dependencies);
                    Assert.IsNotNull(component.Parameters);
                });
            }
        }

        [Test]
        [TestCase("TEST-PROFILE-3.json")]
        public void ComponentFactoryCreatesExpectedParallelExecutionComponentsFromAnExecutionProfile(string profileName)
        {
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();

            bool confirmed = false;
            foreach (ExecutionProfileElement action in profile.Actions)
            {
                Assert.DoesNotThrow(() =>
                {
                    VirtualClientComponent component = ComponentFactory.CreateComponent(action, this.mockFixture.Dependencies);
                    Assert.IsNotNull(component);
                    Assert.IsNotEmpty(component.Dependencies);
                    Assert.IsNotNull(component.Parameters);

                    ParallelExecution parallelExecutionComponent = component as ParallelExecution;
                    if (parallelExecutionComponent != null)
                    {
                        Assert.IsNotEmpty(parallelExecutionComponent);
                        Assert.IsTrue(parallelExecutionComponent.Count() == 2);
                        Assert.IsTrue(parallelExecutionComponent.ElementAt(0) is TestExecutor);
                        Assert.IsTrue(parallelExecutionComponent.ElementAt(1) is TestExecutor);
                        Assert.IsTrue(parallelExecutionComponent.ElementAt(0).Parameters["Scenario"].ToString() == "Scenario2");
                        Assert.IsTrue(parallelExecutionComponent.ElementAt(1).Parameters["Scenario"].ToString() == "Scenario3");
                        confirmed = true;
                    }
                });
            }

            Assert.IsTrue(confirmed);
        }

        [Test]
        [TestCase("TEST-PROFILE-1.json")]
        [TestCase("TEST-PROFILE-2.json")]
        public void ComponentFactoryCreatesExpectedComponentsFromComponentType(string profileName)
        {
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();

            foreach (ExecutionProfileElement action in profile.Actions)
            {
                VirtualClientComponent component;

                component = ComponentFactory.CreateComponent(action, this.mockFixture.Dependencies);
                Assert.IsNotNull(component);
                Assert.IsNotEmpty(component.Dependencies);
                Assert.IsNotNull(component.Parameters);

                component.Parameters.Add("mockParameter", "mockValue");

                Assert.DoesNotThrow(() =>
                {
                    component = ComponentFactory.CreateComponent(component.Parameters, "TestServerExecutor" ,this.mockFixture.Dependencies);
                    Assert.IsNotNull(component);
                    Assert.IsNotEmpty(component.Dependencies);
                    Assert.IsNotNull(component.Parameters);
                    Assert.AreEqual(component.Parameters["mockParameter"], "mockValue");
                });
            }
            
        }
    }
}
