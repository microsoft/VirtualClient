// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using AutoFixture;
    using System.Globalization;
    using VirtualClient.Common.Contracts;

    [TestFixture]
    [Category("Unit")]
    class ExecutionProfileExtensionsTests
    {
        private Fixture fixture;
        private ExecutionProfile profile;
        private ExecutionProfileElement element;

        [SetUp]
        public void SetUpTests()
        {
            this.fixture = new Fixture();
            this.fixture.SetupMocks(randomization: true);

            this.profile = this.fixture.Create<ExecutionProfile>();
            this.element = this.fixture.Create<ExecutionProfileElement>();
            this.profile.Actions.Add(this.element);
            this.profile.Actions.Add(this.fixture.Create<ExecutionProfileElement>());
            this.profile.Dependencies.Add(this.fixture.Create<ExecutionProfileElement>());
            this.profile.Monitors.Add(this.fixture.Create<ExecutionProfileElement>());
            this.profile.Monitors.Add(this.fixture.Create<ExecutionProfileElement>());
            this.profile.Monitors.Add(this.fixture.Create<ExecutionProfileElement>());

            this.profile.Parameters.Add("Parameter1", "Parameter1Value");
            this.profile.Parameters.Add("Parameter2", 1234);

            this.profile.Metadata.Add("Metadata1", 9876);
            this.profile.Metadata.Add("Metadata2", "Metadata2Value");
        }

        [Test]
        public void ExecutionProfileInlinesCorrectlyWhenParametersAreReferenced()
        {
            string parameterKey = "myParameterKey";
            string parameterName = "myParameter";
            string parameterValue = "myResolvedValue";

            this.element.Parameters[parameterKey] = $"{ExecutionProfile.ParameterPrefix}{parameterName}";
            this.profile.Parameters[parameterName] = parameterValue;

            this.profile.Inline();

            Assert.AreEqual(parameterValue, this.element.Parameters[parameterKey]);
        }

        [Test]
        public void ExecutionProfileInlinesParameterReferencesInSubComponents()
        {
            ExecutionProfileElement elementWithSubcomponents = new ExecutionProfileElement("AnyTypeWithSubComponents", null, null, new List<ExecutionProfileElement>
            {
                new ExecutionProfileElement("AnyType", new Dictionary<string, IConvertible>
                {
                    { "Parameter1", "$.Parameters.Parameter1" },
                    { "Parameter2", "$.Parameters.Parameter2" }
                }),
                new ExecutionProfileElement("AnyOtherType", new Dictionary<string, IConvertible>
                {
                    { "Parameter1", "$.Parameters.Parameter1" },
                    { "Parameter2", "$.Parameters.Parameter2" }
                })
            });

            this.profile.Actions.Add(elementWithSubcomponents);
            this.profile.Inline();

            ExecutionProfileElement parentElement = this.profile.Actions.Last();
            Assert.IsFalse(parentElement.Components.Any(comp => comp.Parameters.Values.Contains("$.Parameters.Parameter1")));
            Assert.IsFalse(parentElement.Components.Any(comp => comp.Parameters.Values.Contains("$.Parameters.Parameter2")));
            Assert.IsTrue(parentElement.Components.All(comp => comp.Parameters["Parameter1"].ToString() == "Parameter1Value"));
            Assert.IsTrue(parentElement.Components.All(comp => comp.Parameters["Parameter2"].ToInt32(CultureInfo.InvariantCulture) == 1234));
        }

        [Test]
        public void ExecutionProfileInlinesParameterReferencesInSubComponents_DeepRecursion()
        {
            IDictionary<string, IConvertible> parameters = new Dictionary<string, IConvertible>
            {
                { "Parameter1", "$.Parameters.Parameter1" },
                { "Parameter2", "$.Parameters.Parameter2" }
            };

            // Parameter inlining should support any number of recursive layers.
            ExecutionProfileElement elementWithSubcomponents = new ExecutionProfileElement("AnyTypeWithSubComponents", null, null, new List<ExecutionProfileElement>
            {
                new ExecutionProfileElement("SubComponentLayer1", parameters, null, new List<ExecutionProfileElement>
                {
                    new ExecutionProfileElement("SubComponentLayer2", parameters, null, new List<ExecutionProfileElement>
                    {
                        new ExecutionProfileElement("SubComponentLayer3", parameters, null, new List<ExecutionProfileElement>
                        {
                            new ExecutionProfileElement("SubComponentLayer4", parameters)
                        })
                    })
                })
            });

            this.profile.Actions.Add(elementWithSubcomponents);
            this.profile.Inline();

            ExecutionProfileElement parentElement = this.profile.Actions.Last();
            Assert.IsNotNull(parentElement);

            ExecutionProfileElement layer1SubcomponentElement = parentElement.Components?.FirstOrDefault();
            Assert.IsNotNull(layer1SubcomponentElement);

            ExecutionProfileElement layer2SubcomponentElement = layer1SubcomponentElement?.Components.FirstOrDefault();
            Assert.IsNotNull(layer2SubcomponentElement);

            ExecutionProfileElement layer3SubcomponentElement = layer2SubcomponentElement?.Components.FirstOrDefault();
            Assert.IsNotNull(layer3SubcomponentElement);

            ExecutionProfileElement layer4SubcomponentElement = layer3SubcomponentElement?.Components.FirstOrDefault();
            Assert.IsNotNull(layer4SubcomponentElement);

            Assert.IsFalse(layer1SubcomponentElement.Parameters.Any(p => p.Value == null || p.Value?.ToString() == "$.Parameters.Parameter1"));
            Assert.IsFalse(layer1SubcomponentElement.Parameters.Any(p => p.Value == null || p.Value?.ToString() == "$.Parameters.Parameter2"));
            Assert.IsFalse(layer2SubcomponentElement.Parameters.Any(p => p.Value == null || p.Value?.ToString() == "$.Parameters.Parameter1"));
            Assert.IsFalse(layer2SubcomponentElement.Parameters.Any(p => p.Value == null || p.Value?.ToString() == "$.Parameters.Parameter2"));
            Assert.IsFalse(layer3SubcomponentElement.Parameters.Any(p => p.Value == null || p.Value?.ToString() == "$.Parameters.Parameter1"));
            Assert.IsFalse(layer3SubcomponentElement.Parameters.Any(p => p.Value == null || p.Value?.ToString() == "$.Parameters.Parameter2"));
            Assert.IsFalse(layer4SubcomponentElement.Parameters.Any(p => p.Value == null || p.Value?.ToString() == "$.Parameters.Parameter1"));
            Assert.IsFalse(layer4SubcomponentElement.Parameters.Any(p => p.Value == null || p.Value?.ToString() == "$.Parameters.Parameter2"));

            Assert.IsTrue(layer1SubcomponentElement.Parameters["Parameter1"].ToString() == "Parameter1Value");
            Assert.IsTrue(layer1SubcomponentElement.Parameters["Parameter2"].ToInt32(CultureInfo.InvariantCulture) == 1234);
            Assert.IsTrue(layer2SubcomponentElement.Parameters["Parameter1"].ToString() == "Parameter1Value");
            Assert.IsTrue(layer2SubcomponentElement.Parameters["Parameter2"].ToInt32(CultureInfo.InvariantCulture) == 1234);
            Assert.IsTrue(layer3SubcomponentElement.Parameters["Parameter1"].ToString() == "Parameter1Value");
            Assert.IsTrue(layer3SubcomponentElement.Parameters["Parameter2"].ToInt32(CultureInfo.InvariantCulture) == 1234);
            Assert.IsTrue(layer4SubcomponentElement.Parameters["Parameter1"].ToString() == "Parameter1Value");
            Assert.IsTrue(layer4SubcomponentElement.Parameters["Parameter2"].ToInt32(CultureInfo.InvariantCulture) == 1234);
        }

        [Test]
        public void ExecutionProfileThrowsExceptionWhenInliningIfAGivenParameterReferenceDoesNotExist()
        {
            this.element.Parameters["missingParameter"] = $"{ExecutionProfile.ParameterPrefix}notpresent";
            Assert.Throws<SchemaException>(() =>  this.profile.Inline());
        }

        [Test]
        public void ExecutionProfileDoesNotAlterParametersWhenNoParametersAreReferenced()
        {
            ExecutionProfile profile = new ExecutionProfile(this.profile);
            profile.Inline();
            Assert.AreEqual(this.profile, profile);
        }

        [Test]
        public void ExecutionProfileElementsSupportScenarioTargeting()
        {
            List<string> targetScenarios = new List<string>
            {
                "Scenario1",
                "Scenario2",
                "Scenario3"
            };

            ExecutionProfileElement element = new ExecutionProfileElement(
                "Any.Type.Of.Executor",
                new Dictionary<string, IConvertible>());

            Assert.IsFalse(element.IsTargetedScenario(targetScenarios));

            element.Parameters.Add("Scenario", "Scenario3");
            Assert.IsTrue(element.IsTargetedScenario(targetScenarios));
        }

        [Test]
        public void ExecutionProfileElementsSupportScenarioExclusions()
        {
            List<string> excludedScenarios = new List<string>
            {
                "-Scenario1",
                "-Scenario2",
                "-Scenario3"
            };

            ExecutionProfileElement element = new ExecutionProfileElement(
                "Any.Type.Of.Executor",
                new Dictionary<string, IConvertible>());

            Assert.IsFalse(element.IsExcludedScenario(excludedScenarios));

            element.Parameters.Add("Scenario", "Scenario3");
            Assert.IsTrue(element.IsExcludedScenario(excludedScenarios));
        }

        [Test]
        public void MergeWithExtensionAddsTheExpectedParametersToTheOriginalProfile()
        {
            ExecutionProfile originalProfile = new ExecutionProfile(this.profile);
            ExecutionProfile otherProfile = new ExecutionProfile(this.profile);

            otherProfile.Parameters.Clear();
            otherProfile.Parameters.Add("Parameter3", 777);
            otherProfile.Parameters.Add("Parameter4", false);

            ExecutionProfile mergedProfile = originalProfile.MergeWith(otherProfile);

            Assert.IsTrue(mergedProfile.Parameters.Count == 4);
            Assert.IsTrue(mergedProfile.Parameters.ContainsKey("Parameter1"));
            Assert.IsTrue(mergedProfile.Parameters.ContainsKey("Parameter2"));
            Assert.IsTrue(mergedProfile.Parameters.ContainsKey("Parameter3"));
            Assert.IsTrue(mergedProfile.Parameters.ContainsKey("Parameter4"));
            Assert.AreEqual("Parameter1Value", mergedProfile.Parameters["Parameter1"]);
            Assert.AreEqual(1234, mergedProfile.Parameters["Parameter2"]);
            Assert.AreEqual(777, mergedProfile.Parameters["Parameter3"]);
            Assert.AreEqual(false, mergedProfile.Parameters["Parameter4"]);
        }

        [Test]
        public void MergeWithExtensionAddsTheExpectedMetadataToTheOriginalProfile()
        {
            ExecutionProfile originalProfile = new ExecutionProfile(this.profile);
            ExecutionProfile otherProfile = new ExecutionProfile(this.profile);

            otherProfile.Metadata.Clear();
            otherProfile.Metadata.Add("Metadata3", 222);
            otherProfile.Metadata.Add("Metadata4", true);

            ExecutionProfile mergedProfile = originalProfile.MergeWith(otherProfile);

            Assert.IsTrue(mergedProfile.Metadata.Count == 4);
            Assert.IsTrue(mergedProfile.Metadata.ContainsKey("Metadata1"));
            Assert.IsTrue(mergedProfile.Metadata.ContainsKey("Metadata2"));
            Assert.IsTrue(mergedProfile.Metadata.ContainsKey("Metadata3"));
            Assert.IsTrue(mergedProfile.Metadata.ContainsKey("Metadata4"));
            Assert.AreEqual(9876, mergedProfile.Metadata["Metadata1"]);
            Assert.AreEqual("Metadata2Value", mergedProfile.Metadata["Metadata2"]);
            Assert.AreEqual(222, mergedProfile.Metadata["Metadata3"]);
            Assert.AreEqual(true, mergedProfile.Metadata["Metadata4"]);
        }

        [Test]
        public void MergeWithExtensionAddsTheExpectedActionsToTheOriginalProfile()
        {
            ExecutionProfile originalProfile = new ExecutionProfile(this.profile);
            ExecutionProfile otherProfile = new ExecutionProfile(this.profile);
            ExecutionProfile mergedProfile = originalProfile.MergeWith(otherProfile);

            Assert.IsTrue(mergedProfile.Actions.Count == 6);
            CollectionAssert.AreEqual(
                originalProfile.Actions.Concat(otherProfile.Actions).Select(a => a.Type),
                mergedProfile.Actions.Select(a => a.Type));
        }

        [Test]
        public void MergeWithExtensionAddsTheExpectedDependenciesToTheOriginalProfile()
        {
            ExecutionProfile originalProfile = new ExecutionProfile(this.profile);
            ExecutionProfile otherProfile = new ExecutionProfile(this.profile);
            ExecutionProfile mergedProfile = originalProfile.MergeWith(otherProfile);

            Assert.IsTrue(mergedProfile.Dependencies.Count == 4);
            CollectionAssert.AreEqual(
                originalProfile.Dependencies.Concat(otherProfile.Dependencies).Select(a => a.Type),
                mergedProfile.Dependencies.Select(a => a.Type));
        }

        [Test]
        public void MergeWithExtensionAddsTheExpectedMonitorsToTheOriginalProfile()
        {
            ExecutionProfile originalProfile = new ExecutionProfile(this.profile);
            ExecutionProfile otherProfile = new ExecutionProfile(this.profile);
            ExecutionProfile mergedProfile = originalProfile.MergeWith(otherProfile);

            Assert.IsTrue(mergedProfile.Monitors.Count == 8);
            CollectionAssert.AreEqual(
                originalProfile.Monitors.Concat(otherProfile.Monitors).Select(a => a.Type),
                mergedProfile.Monitors.Select(a => a.Type));
        }

        [Test]
        public void MergeWithExtensionDoesNotModifyTheFundamentalPropertiesOfTheOriginalProfile()
        {
            ExecutionProfile originalProfile = new ExecutionProfile(this.profile);
            ExecutionProfile otherProfile = new ExecutionProfile(this.profile);
            ExecutionProfile mergedProfile = originalProfile.MergeWith(otherProfile);

            Assert.AreEqual(originalProfile.Description, mergedProfile.Description);
            Assert.AreEqual(originalProfile.MinimumExecutionInterval, mergedProfile.MinimumExecutionInterval);
        }
    }
}
