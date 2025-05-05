// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;

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
        [TestCase("TEST-PROFILE-3-PARALLEL.json")]
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
        [TestCase("TEST-PROFILE-4.json")]
        public void ComponentFactoryAddsExpectedComponentLevelMetadata(string profileName)
        {
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();

            foreach (ExecutionProfileElement action in profile.Actions)
            {
                VirtualClientComponent component = ComponentFactory.CreateComponent(action, this.mockFixture.Dependencies);
                Assert.IsNotNull(component);
                Assert.IsNotEmpty(component.Metadata);

                IDictionary<string, IConvertible> expectedMetadata = new Dictionary<string, IConvertible>
                {
                    { "Property4", 7777 },
                    { "Property5", "Value_B" }
                };

                Assert.AreEqual(expectedMetadata["Property4"], component.Metadata["Property4"]);
                Assert.AreEqual(expectedMetadata["Property5"], component.Metadata["Property5"]);
            }
        }

        [Test]
        [TestCase("TEST-PROFILE-4-PARALLEL.json")]
        [TestCase("TEST-PROFILE-2-PARALLEL-LOOP.json")]
        public void ComponentFactoryAddsExpectedComponentLevelMetadataToSubComponents(string profileName)
        {
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();

            VirtualClientComponent component = ComponentFactory.CreateComponent(profile.Actions.First(), this.mockFixture.Dependencies);
            Assert.IsNotNull(component);
            Assert.IsInstanceOf<VirtualClientComponentCollection>(component);
            Assert.IsNotEmpty(component.Metadata);

            IDictionary<string, IConvertible> expectedMetadata = new Dictionary<string, IConvertible>
            {
                { "Property4", 7777 },
                { "Property5", "Value_B" },
            };

            Assert.AreEqual(expectedMetadata["Property4"], component.Metadata["Property4"]);
            Assert.AreEqual(expectedMetadata["Property5"], component.Metadata["Property5"]);

            VirtualClientComponent subComponent1 = (component as VirtualClientComponentCollection).ElementAt(0);
            IDictionary<string, IConvertible> subComponent1ExpectedMetadata = new Dictionary<string, IConvertible>
            {
                { "Property4", 7777 },
                { "Property5", "Value_B" },
                { "Property6", 1111 },
                { "Property7", "Value_C" },
            };

            CollectionAssert.AreEquivalent(subComponent1ExpectedMetadata, subComponent1.Metadata);

            VirtualClientComponent subComponent2 = (component as VirtualClientComponentCollection).ElementAt(1);
            IDictionary<string, IConvertible> subComponent2ExpectedMetadata = new Dictionary<string, IConvertible>
            {
                { "Property4", 7777 },
                { "Property5", "Value_B" },
                { "Property6", 2222 },
                { "Property7", "Value_D" },
            };

            CollectionAssert.AreEquivalent(subComponent2ExpectedMetadata, subComponent2.Metadata);
        }

        [Test]
        [TestCase("TEST-PROFILE-1-PARALLEL-LOOP.json")]
        public void ComponentFactoryCreatesExpectedParallelLoopExecutionComponentsFromAnExecutionProfile(string profileName)
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

                    ParallelLoopExecution parallelExecutionComponent = component as ParallelLoopExecution;
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
        public void ComponentFactoryAddsExpectedComponentLevelMetadataToSubComponents_Deep_Nesting()
        {
            // Setup:
            // 3-levels deep nested hierarchy:
            // Level 1: VirtualClientComponentCollection
            //     Components [
            //          Level 2: VirtualClientComponentCollection
            //              Components: [
            //                  Level 3: VirtualClientComponent
            //              ]
            //     ]
            //
            // The metadata from Level 1 should be passed to Level 2, then the metadata at
            // level 2 should be passed to level 3. The outcome should be that the metadata
            // at level 3 should be a merger of all metadata.
            ExecutionProfileElement level3Component = new ExecutionProfileElement(
                nameof(TestExecutor),
                parameters: null,
                metadata: new Dictionary<string, IConvertible>
                {
                    { "Property3", 3 }
                });

            ExecutionProfileElement level2Component = new ExecutionProfileElement(
                nameof(ParallelExecution),
                parameters: null,
                metadata: new Dictionary<string, IConvertible>
                {
                    { "Property2", 2 }
                },
                components: new List<ExecutionProfileElement> { level3Component });

            ExecutionProfileElement level1Component = new ExecutionProfileElement(
                nameof(ParallelExecution),
                parameters: null,
                metadata: new Dictionary<string, IConvertible>
                {
                    { "Property1", 1 }
                },
                components: new List<ExecutionProfileElement> { level2Component });

            VirtualClientComponent level1 = ComponentFactory.CreateComponent(level1Component, this.mockFixture.Dependencies);

            Assert.IsNotNull(level1);
            Assert.IsInstanceOf<VirtualClientComponentCollection>(level1);
            Assert.IsNotEmpty(level1.Metadata);
            Assert.AreEqual(1, level1.Metadata["Property1"]);

            VirtualClientComponent level2 = (level1 as VirtualClientComponentCollection).First();
            Assert.IsInstanceOf<VirtualClientComponentCollection>(level2);
            Assert.AreEqual(1, level2.Metadata["Property1"]);
            Assert.AreEqual(2, level2.Metadata["Property2"]);

            VirtualClientComponent level3 = (level2 as VirtualClientComponentCollection).First();
            Assert.IsInstanceOf<VirtualClientComponent>(level3);
            Assert.AreEqual(1, level3.Metadata["Property1"]);
            Assert.AreEqual(2, level3.Metadata["Property2"]);
            Assert.AreEqual(3, level3.Metadata["Property3"]);
        }

        [Test]
        public void ComponentFactoryAppliesMetadataWithPriorityAtHigherLevelsOverLowerLevelsInANestedHierarchyOfComponents()
        {
            // Setup:
            // 3-levels deep nested hierarchy:
            // Level 1: VirtualClientComponentCollection
            //     Components [
            //          Level 2: VirtualClientComponentCollection
            //              Components: [
            //                  Level 3: VirtualClientComponent
            //              ]
            //     ]
            //
            ExecutionProfileElement level3Component = new ExecutionProfileElement(
                nameof(TestExecutor),
                parameters: null,
                metadata: new Dictionary<string, IConvertible>
                {
                    { "Property", 3 }
                });

            ExecutionProfileElement level2Component = new ExecutionProfileElement(
                nameof(ParallelExecution),
                parameters: null,
                metadata: new Dictionary<string, IConvertible>
                {
                    { "Property", 2 }
                },
                components: new List<ExecutionProfileElement> { level3Component });

            // The metadata from Level 1 should override metadata further down the child
            // hierarchy.
            ExecutionProfileElement level1Component = new ExecutionProfileElement(
                nameof(ParallelExecution),
                parameters: null,
                metadata: new Dictionary<string, IConvertible>
                {
                    // The metadata at the highest level (the parent) takes precedence
                    // over child component metadata.
                    { "Property", 1 }
                },
                components: new List<ExecutionProfileElement> { level2Component });

            VirtualClientComponent level1 = ComponentFactory.CreateComponent(level1Component, this.mockFixture.Dependencies);
            Assert.IsNotNull(level1);
            Assert.IsInstanceOf<VirtualClientComponentCollection>(level1);
            Assert.IsNotEmpty(level1.Metadata);
            Assert.AreEqual(1, level1.Metadata["Property"]);

            VirtualClientComponent level2 = (level1 as VirtualClientComponentCollection).First();
            Assert.IsInstanceOf<VirtualClientComponentCollection>(level2);
            Assert.AreEqual(1, level2.Metadata["Property"]);

            VirtualClientComponent level3 = (level2 as VirtualClientComponentCollection).First();
            Assert.IsInstanceOf<VirtualClientComponent>(level3);
            Assert.AreEqual(1, level3.Metadata["Property"]);
        }

        [Test]
        [TestCase("TEST-PROFILE-4.json")]
        public void ComponentFactoryAddsExpectedComponentLevelExtensions(string profileName)
        {
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();

            foreach (ExecutionProfileElement action in profile.Actions)
            {
                VirtualClientComponent component = ComponentFactory.CreateComponent(action, this.mockFixture.Dependencies);
                Assert.IsNotNull(component);
                Assert.IsNotEmpty(component.Extensions);
                Assert.IsTrue(action.Extensions.TryGetValue("Documentation", out JToken expectedExtensions));
                Assert.IsTrue(component.Extensions.TryGetValue("Documentation", out JToken actualExtensions));
                Assert.AreEqual(expectedExtensions.ToJson().RemoveWhitespace(), actualExtensions.ToJson().RemoveWhitespace());
            }
        }

        [Test]
        [TestCase("TEST-PROFILE-4-PARALLEL.json")]
        public void ComponentFactoryAddsExpectedComponentLevelExtensionsToSubComponents(string profileName)
        {
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();

            foreach (ExecutionProfileElement action in profile.Actions)
            {
                VirtualClientComponent component = ComponentFactory.CreateComponent(action, this.mockFixture.Dependencies);
                Assert.IsNotNull(component);
                Assert.IsInstanceOf<VirtualClientComponentCollection>(component);
                Assert.IsNotEmpty(component.Extensions);
                Assert.IsTrue(action.Extensions.TryGetValue("Contacts", out JToken expectedExtensions));
                Assert.IsTrue(component.Extensions.TryGetValue("Contacts", out JToken actualExtensions));
                Assert.AreEqual(expectedExtensions.ToJson().RemoveWhitespace(), actualExtensions.ToJson().RemoveWhitespace());

                foreach (VirtualClientComponent subComponent in component as VirtualClientComponentCollection)
                {
                    Assert.IsTrue(subComponent.Extensions.TryGetValue("Contacts", out actualExtensions));
                    Assert.AreEqual(expectedExtensions.ToJson().RemoveWhitespace(), actualExtensions.ToJson().RemoveWhitespace());
                }
            }
        }

        [Test]
        [TestCase("TEST-PROFILE-EXTENSIONS-1-PARALLEL.json")]
        public void ComponentFactoryHandlesComponentLevelExtensionAnomalies_1(string profileName)
        {
            // Scenario:
            // A partner team is trying to pass in extensions objects to components within side
            // of a parallel execution block. The information in the extensions objects for the first component
            // in the parallel execution block seems to be overriding the ones that come afterwards.
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();

            ExecutionProfileElement parallelLoop = profile.Actions.First();
            IEnumerable<ExecutionProfileElement> parallelLoopComponents = parallelLoop.Components;

            Assert.IsTrue(parallelLoopComponents?.Count() == 2);

            VirtualClientComponentCollection parallelExecution = ComponentFactory.CreateComponent(parallelLoop, this.mockFixture.Dependencies) as VirtualClientComponentCollection;

            for (int i = 0; i < parallelLoopComponents.Count(); i++)
            {
                ExecutionProfileElement profileElement = parallelLoopComponents.ElementAt(i);
                VirtualClientComponent runtimeComponent = parallelExecution.ElementAt(i);

                Assert.IsTrue(profileElement.Extensions.TryGetValue("ActionCustomParameters", out JToken expectedExtensions));
                Assert.IsTrue(runtimeComponent.Extensions.TryGetValue("ActionCustomParameters", out JToken actualExtensions));
                Assert.AreEqual(expectedExtensions.ToJson().RemoveWhitespace(), actualExtensions.ToJson().RemoveWhitespace());
            }
        }

        [Test]
        public void ComponentFactoryAddsExpectedComponentLevelExtensionsToSubComponents_Deep_Nesting()
        {
            // Setup:
            // 3-levels deep nested hierarchy:
            // Level 1: VirtualClientComponentCollection
            //     Components [
            //          Level 2: VirtualClientComponentCollection
            //              Components: [
            //                  Level 3: VirtualClientComponent
            //              ]
            //     ]
            //
            // The extensions from Level 1 should be passed to Level 2, then the extensions at
            // level 2 should be passed to level 3. The outcome should be that the extensions
            // at level 3 should be a merger of all extensions.
            ExecutionProfileElement level3Component = new ExecutionProfileElement(
                nameof(TestExecutor),
                parameters: null);

            level3Component.Extensions.Add("Property3", JToken.Parse("{ 'Value': 3 }"));

            ExecutionProfileElement level2Component = new ExecutionProfileElement(
                nameof(ParallelExecution),
                parameters: null,
                components: new List<ExecutionProfileElement> { level3Component });

            level2Component.Extensions.Add("Property2", JToken.Parse("{ 'Value': 2 }"));

            ExecutionProfileElement level1Component = new ExecutionProfileElement(
                nameof(ParallelExecution),
                parameters: null,
                components: new List<ExecutionProfileElement> { level2Component });

            level1Component.Extensions.Add("Property1", JToken.Parse("{ 'Value': 1 }"));

            VirtualClientComponent level1 = ComponentFactory.CreateComponent(level1Component, this.mockFixture.Dependencies);

            Assert.IsNotNull(level1);
            Assert.IsInstanceOf<VirtualClientComponentCollection>(level1);
            Assert.IsNotEmpty(level1.Extensions);

            Assert.AreEqual(
                level1Component.Extensions["Property1"].ToJson().RemoveWhitespace(),
                level1.Extensions["Property1"].ToJson().RemoveWhitespace());

            VirtualClientComponent level2 = (level1 as VirtualClientComponentCollection).First();
            Assert.IsInstanceOf<VirtualClientComponentCollection>(level2);
            Assert.IsNotEmpty(level2.Extensions);

            Assert.AreEqual(
                level1Component.Extensions["Property1"].ToJson().RemoveWhitespace(),
                level2.Extensions["Property1"].ToJson().RemoveWhitespace());

            Assert.AreEqual(
                level2Component.Extensions["Property2"].ToJson().RemoveWhitespace(),
                level2.Extensions["Property2"].ToJson().RemoveWhitespace());

            VirtualClientComponent level3 = (level2 as VirtualClientComponentCollection).First();
            Assert.IsInstanceOf<VirtualClientComponent>(level3);
            Assert.IsNotEmpty(level3.Extensions);

            Assert.AreEqual(
                level1Component.Extensions["Property1"].ToJson().RemoveWhitespace(),
                level3.Extensions["Property1"].ToJson().RemoveWhitespace());

            Assert.AreEqual(
                level2Component.Extensions["Property2"].ToJson().RemoveWhitespace(),
                level3.Extensions["Property2"].ToJson().RemoveWhitespace());

            Assert.AreEqual(
                level3Component.Extensions["Property3"].ToJson().RemoveWhitespace(),
                level3.Extensions["Property3"].ToJson().RemoveWhitespace());
        }

        [Test]
        public void ComponentFactoryAppliesExtensionsWithPriorityAtHigherLevelsOverLowerLevelsInANestedHierarchyOfComponents()
        {
            // Setup:
            // 3-levels deep nested hierarchy:
            // Level 1: VirtualClientComponentCollection
            //     Components [
            //          Level 2: VirtualClientComponentCollection
            //              Components: [
            //                  Level 3: VirtualClientComponent
            //              ]
            //     ]
            //
            // The extensions from Level 1 should be passed to Level 2, then the extensions at
            // level 2 should be passed to level 3. The outcome should be that the extensions
            // at level 3 should be a merger of all extensions.
            ExecutionProfileElement level3Component = new ExecutionProfileElement(
                nameof(TestExecutor),
                parameters: null);

            level3Component.Extensions.Add("Property", JToken.Parse("{ 'Value': 3 }"));

            ExecutionProfileElement level2Component = new ExecutionProfileElement(
                nameof(ParallelExecution),
                parameters: null,
                components: new List<ExecutionProfileElement> { level3Component });

            level2Component.Extensions.Add("Property", JToken.Parse("{ 'Value': 2 }"));

            ExecutionProfileElement level1Component = new ExecutionProfileElement(
                nameof(ParallelExecution),
                parameters: null,
                components: new List<ExecutionProfileElement> { level2Component });

            // The extensions from Level 1 should override extensions further down the child
            // hierarchy.
            level1Component.Extensions.Add("Property", JToken.Parse("{ 'Value': 1 }"));

            VirtualClientComponent level1 = ComponentFactory.CreateComponent(level1Component, this.mockFixture.Dependencies);

            Assert.IsNotNull(level1);
            Assert.IsInstanceOf<VirtualClientComponentCollection>(level1);
            Assert.IsNotEmpty(level1.Extensions);

            Assert.AreEqual(
                level1Component.Extensions["Property"].ToJson().RemoveWhitespace(),
                level1.Extensions["Property"].ToJson().RemoveWhitespace());

            VirtualClientComponent level2 = (level1 as VirtualClientComponentCollection).First();
            Assert.IsInstanceOf<VirtualClientComponentCollection>(level2);
            Assert.IsNotEmpty(level2.Extensions);

            Assert.AreEqual(
                level1Component.Extensions["Property"].ToJson().RemoveWhitespace(),
                level2.Extensions["Property"].ToJson().RemoveWhitespace());

            VirtualClientComponent level3 = (level2 as VirtualClientComponentCollection).First();
            Assert.IsInstanceOf<VirtualClientComponent>(level3);
            Assert.IsNotEmpty(level3.Extensions);

            Assert.AreEqual(
                level1Component.Extensions["Property"].ToJson().RemoveWhitespace(),
                level3.Extensions["Property"].ToJson().RemoveWhitespace());
        }

        [Test]
        [TestCase("TEST-PROFILE-4.json")]
        public void ComponentFactoryAppliesCommandLineInstructionsProvidedToComponents(string profileName)
        {
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();

            foreach (ExecutionProfileElement action in profile.Actions)
            {
                VirtualClientComponent component = ComponentFactory.CreateComponent(
                    action,
                    this.mockFixture.Dependencies,
                    randomizationSeed: 123,
                    failFast: true,
                    logToFile: true);

                Assert.IsNotNull(component);
                Assert.AreEqual(123, component.ExecutionSeed);
                Assert.IsTrue(component.FailFast);
                Assert.IsTrue(component.LogToFile);
            }
        }

        [Test]
        [TestCase("TEST-PROFILE-4-PARALLEL.json")]
        public void ComponentFactoryAppliesCommandLineInstructionsProvidedToSubComponents(string profileName)
        {
            ExecutionProfile profile = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "Resources", profileName))
                .FromJson<ExecutionProfile>();

            foreach (ExecutionProfileElement action in profile.Actions)
            {
                VirtualClientComponent component = ComponentFactory.CreateComponent(
                    action,
                    this.mockFixture.Dependencies,
                    randomizationSeed: 123,
                    failFast: true,
                    logToFile: true);

                Assert.IsNotNull(component);
                Assert.AreEqual(123, component.ExecutionSeed);
                Assert.IsTrue(component.FailFast);
                Assert.IsTrue(component.LogToFile);

                foreach (VirtualClientComponent subComponent in component as VirtualClientComponentCollection)
                {
                    Assert.AreEqual(123, subComponent.ExecutionSeed);
                    Assert.IsTrue(subComponent.FailFast);
                    Assert.IsTrue(subComponent.LogToFile);
                }
            }
        }
    }
}
