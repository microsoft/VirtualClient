// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Metadata
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts;
    using VirtualClient.Contracts.Metadata;

    [TestFixture]
    [Category("Unit")]
    internal class MetadataContractTests
    {
        [SetUp]
        public void SetupTest()
        {
            // Reset the metadata contract before each test iteration.
            MetadataContract.ResetPersisted();
        }

        // *************************************** IMPORTANT ********************************************
        // These tests are evaluating static class behaviors. As such they MUST run ordered, 1 at a time.
        // **********************************************************************************************
        [Test]
        [Order(1)]
        public void MetadataContractReturnsTheExpectedCategoryMetadata_Persisted_Properties()
        {
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>
            {
                { "AnyProperty", "AnyValue" }
            };

            MetadataContract.Persist(expectedMetadata, "metadata_ext");
            IDictionary<string, object> actualMetadata = MetadataContract.GetPersisted("metadata_ext");

            Assert.IsNotNull(actualMetadata);
            Assert.IsTrue(actualMetadata.TryGetValue("AnyProperty", out object value) && value.ToString() == "AnyValue");
        }

        [Test]
        [Order(2)]
        public void MetadataContractReturnsTheExpectedCategoryMetadata_Instance_Properties()
        {
            MetadataContract contract = new MetadataContract();
            IDictionary<string, object> expectedMetadata = new Dictionary<string, object>
            {
                { "AnyProperty", "AnyValue" }
            };

            contract.Add(expectedMetadata, "metadata_ext");
            IDictionary<string, object> actualMetadata = contract.Get("metadata_ext");

            Assert.IsNotNull(actualMetadata);
            Assert.IsTrue(actualMetadata.TryGetValue("AnyProperty", out object value) && value.ToString() == "AnyValue");
        }

        [Test]
        [Order(3)]
        public void MetadataContractCategoriesMustFollowThePrescribedNamingConvention()
        {
            Assert.DoesNotThrow(() => MetadataContract.Persist("AnyProperty1", "AnyValue1", "metadata_ext"));
            Assert.Throws<SchemaException>(() => MetadataContract.Persist("AnyProperty2", "AnyValue2", "notavalidprefix_ext"));

            MetadataContract contract = new MetadataContract();
            Assert.DoesNotThrow(() => contract.Add("AnyProperty1", "AnyValue1", "metadata_ext"));
            Assert.Throws<SchemaException>(() => contract.Add("AnyProperty2", "AnyValue2", "notavalidprefix_ext"));
        }

        [Test]
        [Order(4)]
        public void MetadataContractCategoriesAreNotCaseSensitive()
        {
            string category = "metadata_ext";
            MetadataContract.Persist("AnyProperty", "AnyValue", category);
            IDictionary<string, object> metadata = MetadataContract.GetPersisted(category.ToUpperInvariant());
            Assert.IsNotNull(metadata);

            MetadataContract contract = new MetadataContract();
            contract.Add("AnyProperty", "AnyValue", category);
            metadata = contract.Get(category.ToUpperInvariant());
            Assert.IsNotNull(metadata);
        }

        [Test]
        [Order(5)]
        [TestCase(MetadataContract.DefaultCategory)]
        [TestCase(MetadataContract.DependenciesCategory)]
        [TestCase(MetadataContract.HostCategory)]
        [TestCase(MetadataContract.RuntimeCategory)]
        [TestCase(MetadataContract.ScenarioCategory)]
        [TestCase(MetadataContract.ScenarioExtensionsCategory)]
        public void MetadataContractPersistsMetadataToTheExpectedStandardCategories_1(string expectedCategoryName)
        {
            MetadataContract.Persist("AnyProperty", "AnyValue", expectedCategoryName);
            IDictionary<string, object> metadata = MetadataContract.GetPersisted(expectedCategoryName);

            Assert.IsNotNull(metadata);
            Assert.IsTrue(metadata.TryGetValue("AnyProperty", out object value));
            Assert.AreEqual("AnyValue", value);
        }

        [Test]
        [Order(7)]
        [TestCase(MetadataContract.DefaultCategory)]
        [TestCase(MetadataContract.DependenciesCategory)]
        [TestCase(MetadataContract.HostCategory)]
        [TestCase(MetadataContract.RuntimeCategory)]
        [TestCase(MetadataContract.ScenarioCategory)]
        [TestCase(MetadataContract.ScenarioExtensionsCategory)]
        public void MetadataContractInstanceMetadataToTheExpectedStandardCategories_1(string expectedCategoryName)
        {
            MetadataContract contract = new MetadataContract();
            contract.Add("AnyProperty", "AnyValue", expectedCategoryName);
            IDictionary<string, object> metadata = contract.Get(expectedCategoryName);

            Assert.IsNotNull(metadata);
            Assert.IsTrue(metadata.TryGetValue("AnyProperty", out object value));
            Assert.AreEqual("AnyValue", value);
        }

        [Test]
        [Order(9)]
        public void PersistedMetadataIsAppliedToEventContextObjectsAsExpected()
        {
            MetadataContract contract = new MetadataContract();
            EventContext telemetryContext = new EventContext(Guid.NewGuid());
            MetadataContract.Persist("Default", "Prop01", MetadataContract.DefaultCategory);
            MetadataContract.Persist("Dependency", "Dependency01", MetadataContract.DependenciesCategory);
            MetadataContract.Persist("Host", "Host01", MetadataContract.HostCategory);
            MetadataContract.Persist("Scenario", "Scenario01", MetadataContract.ScenarioCategory);
            MetadataContract.Persist("ScenarioExtension", JObject.Parse("{'any':'extensions'}"), MetadataContract.ScenarioExtensionsCategory);
            MetadataContract.Persist("Runtime", "Version1", MetadataContract.RuntimeCategory);
            contract.Apply(telemetryContext);

            // Example Format
            // {
            //     "activityId": "d58e13f2-4e84-4cf2-b81f-b270dddcd21f",
            //     "parentActivityId": "00000000-0000-0000-0000-000000000000",
            //     "durationMs": 0,
            //     "transactionId": "eb03c8e0-7c40-4641-84cd-4954a31018b3",
            //     "properties": {
            //         "metadata": {
            //             "default": "Prop01"
            //         },
            //         "metadata_dependencies": {
            //             "dependency": "Dependency01"
            //         },
            //         "metadata_host": {
            //             "host": "Host01"
            //         },
            //         "metadata_scenario": {
            //             "scenario": "Scenario01"
            //         },
            //         "metadata_scenario_ext": {
            //             "scenarioExtension": {"any":"extensions"}"
            //         },
            //         "metadata_runtime": {
            //             "runtime": "Version1"
            //         }
            //     },
            // }
            string json = telemetryContext.ToJson().RemoveWhitespace();

            // Note that the property names should be camel-cased all the way down
            // the nested JSON hierarchy.
            Assert.IsTrue(json.Contains("\"metadata\":{\"default\":\"Prop01\"}"));
            Assert.IsTrue(json.Contains("\"metadata_dependencies\":{\"dependency\":\"Dependency01\"}"));
            Assert.IsTrue(json.Contains("\"metadata_host\":{\"host\":\"Host01\"}"));
            Assert.IsTrue(json.Contains("\"metadata_scenario\":{\"scenario\":\"Scenario01\"}"));
            Assert.IsTrue(json.Contains("\"metadata_scenario_ext\":{\"scenarioExtension\":{\"any\":\"extensions\"}}"));
            Assert.IsTrue(json.Contains("\"metadata_runtime\":{\"runtime\":\"Version1\"}"));
        }

        [Test]
        [Order(9)]
        public void MetadataIsAppliedToEventContextObjectsAsExpected()
        {
            MetadataContract contract = new MetadataContract();
            EventContext telemetryContext = new EventContext(Guid.NewGuid());
            contract.Add("Default", "Prop01", MetadataContract.DefaultCategory);
            contract.Add("Dependency", "Dependency01", MetadataContract.DependenciesCategory);
            contract.Add("Host", "Host01", MetadataContract.HostCategory);
            contract.Add("Scenario", "Scenario01", MetadataContract.ScenarioCategory);
            contract.Add("ScenarioExtension", JObject.Parse("{'any':'extensions'}"), MetadataContract.ScenarioExtensionsCategory);
            contract.Add("Runtime", "Version1", MetadataContract.RuntimeCategory);
            contract.Apply(telemetryContext);

            // Example Format
            // {
            //     "activityId": "d58e13f2-4e84-4cf2-b81f-b270dddcd21f",
            //     "parentActivityId": "00000000-0000-0000-0000-000000000000",
            //     "durationMs": 0,
            //     "transactionId": "eb03c8e0-7c40-4641-84cd-4954a31018b3",
            //     "properties": {
            //         "metadata": {
            //             "default": "Prop01"
            //         },
            //         "metadata_dependencies": {
            //             "dependency": "Dependency01"
            //         },
            //         "metadata_host": {
            //             "host": "Host01"
            //         },
            //         "metadata_scenario": {
            //             "scenario": "Scenario01"
            //         },
            //         "metadata_scenario_ext": {
            //             "scenarioExtension": {"any":"extensions"}"
            //         },
            //         "metadata_runtime": {
            //             "runtime": "Version1"
            //         }
            //     },
            // }
            string json = telemetryContext.ToJson().RemoveWhitespace();

            // Note that the property names should be camel-cased all the way down
            // the nested JSON hierarchy.
            Assert.IsTrue(json.Contains("\"metadata\":{\"default\":\"Prop01\"}"));
            Assert.IsTrue(json.Contains("\"metadata_dependencies\":{\"dependency\":\"Dependency01\"}"));
            Assert.IsTrue(json.Contains("\"metadata_host\":{\"host\":\"Host01\"}"));
            Assert.IsTrue(json.Contains("\"metadata_scenario\":{\"scenario\":\"Scenario01\"}"));
            Assert.IsTrue(json.Contains("\"metadata_scenario_ext\":{\"scenarioExtension\":{\"any\":\"extensions\"}}"));
            Assert.IsTrue(json.Contains("\"metadata_runtime\":{\"runtime\":\"Version1\"}"));
        }

        [Test]
        [Order(10)]
        public void PersistedAndInstanceLevelMetadataIsAppliedToEventContextObjectsAsExpected()
        {
            MetadataContract contract = new MetadataContract();
            EventContext telemetryContext = new EventContext(Guid.NewGuid());

            MetadataContract.Persist("Default1", "Prop01", MetadataContract.DefaultCategory);
            MetadataContract.Persist("Dependency1", "Dependency01", MetadataContract.DependenciesCategory);
            MetadataContract.Persist("Host1", "Host01", MetadataContract.HostCategory);
            MetadataContract.Persist("Scenario1", "Scenario01", MetadataContract.ScenarioCategory);
            MetadataContract.Persist("Runtime1", "Version1", MetadataContract.RuntimeCategory);

            contract.Add("Default2", "Prop02", MetadataContract.DefaultCategory);
            contract.Add("Dependency2", "Dependency02", MetadataContract.DependenciesCategory);
            contract.Add("Host2", "Host02", MetadataContract.HostCategory);
            contract.Add("Scenario2", "Scenario02", MetadataContract.ScenarioCategory);
            contract.Add("Runtime2", "Version2", MetadataContract.RuntimeCategory);
            contract.Apply(telemetryContext);

            // Example Format
            // {
            //     "activityId": "d58e13f2-4e84-4cf2-b81f-b270dddcd21f",
            //     "parentActivityId": "00000000-0000-0000-0000-000000000000",
            //     "durationMs": 0,
            //     "transactionId": "eb03c8e0-7c40-4641-84cd-4954a31018b3",
            //     "properties": {
            //         "metadata": {
            //             "default1": "Prop01",
            //             "default2": "Prop02"
            //         },
            //         "metadata_dependencies": {
            //             "dependency1": "Dependency01",
            //             "dependency2": "Dependency02"
            //         },
            //         "metadata_host": {
            //             "host1": "Host01",
            //             "host2": "Host02"
            //         },
            //         "metadata_scenario": {
            //             "scenario1": "Scenario01",
            //             "scenario2": "Scenario02"
            //         },
            //         "metadata_runtime": {
            //             "runtime1": "Version1",
            //             "runtime2": "Version2"
            //         }
            //     },
            // }
            string json = telemetryContext.ToJson().RemoveWhitespace();

            // Note that the property names should be camel-cased all the way down
            // the nested JSON hierarchy.
            Assert.IsTrue(json.Contains("\"metadata\":{\"default1\":\"Prop01\",\"default2\":\"Prop02\"}"));
            Assert.IsTrue(json.Contains("\"metadata_dependencies\":{\"dependency1\":\"Dependency01\",\"dependency2\":\"Dependency02\"}"));
            Assert.IsTrue(json.Contains("\"metadata_host\":{\"host1\":\"Host01\",\"host2\":\"Host02\"}"));
            Assert.IsTrue(json.Contains("\"metadata_scenario\":{\"scenario1\":\"Scenario01\",\"scenario2\":\"Scenario02\"}"));
            Assert.IsTrue(json.Contains("\"metadata_runtime\":{\"runtime1\":\"Version1\",\"runtime2\":\"Version2\"}"));
        }

        [Test]
        [Order(11)]
        public void InstanceLevelMetadataPropertiesOverwritePersistedPropertiesWhenApplied_1()
        {
            MetadataContract contract = new MetadataContract();
            EventContext telemetryContext = new EventContext(Guid.NewGuid());

            MetadataContract.Persist("Default", "Prop01", MetadataContract.DefaultCategory);
            MetadataContract.Persist("Dependency", "Dependency01", MetadataContract.DependenciesCategory);
            MetadataContract.Persist("Host", "Host01", MetadataContract.HostCategory);
            MetadataContract.Persist("Scenario", "Scenario01", MetadataContract.ScenarioCategory);
            MetadataContract.Persist("Runtime", "Version1", MetadataContract.RuntimeCategory);

            contract.Add("Default", "Prop02", MetadataContract.DefaultCategory);
            contract.Add("Dependency", "Dependency02", MetadataContract.DependenciesCategory);
            contract.Apply(telemetryContext);

            // Example Format
            // {
            //     "activityId": "d58e13f2-4e84-4cf2-b81f-b270dddcd21f",
            //     "parentActivityId": "00000000-0000-0000-0000-000000000000",
            //     "durationMs": 0,
            //     "transactionId": "eb03c8e0-7c40-4641-84cd-4954a31018b3",
            //     "properties": {
            //         "metadata": {
            //             "default": "Prop02"
            //         },
            //         "metadata_dependencies": {
            //             "dependency": "Dependency02"
            //         },
            //         "metadata_host": {
            //             "host": "Host01"
            //         },
            //         "metadata_scenario": {
            //             "scenario": "Scenario01"
            //         },
            //         "metadata_runtime": {
            //             "runtime": "Version1"
            //         }
            //     },
            // }
            string json = telemetryContext.ToJson().RemoveWhitespace();

            // Note that the property names should be camel-cased all the way down
            // the nested JSON hierarchy.
            Assert.IsTrue(json.Contains("\"metadata\":{\"default\":\"Prop02\"}"));
            Assert.IsTrue(json.Contains("\"metadata_dependencies\":{\"dependency\":\"Dependency02\"}"));
            Assert.IsTrue(json.Contains("\"metadata_host\":{\"host\":\"Host01\"}"));
            Assert.IsTrue(json.Contains("\"metadata_scenario\":{\"scenario\":\"Scenario01\"}"));
            Assert.IsTrue(json.Contains("\"metadata_runtime\":{\"runtime\":\"Version1\"}"));
        }

        [Test]
        [Order(12)]
        public void InstanceLevelMetadataPropertiesOverwritePersistedPropertiesWhenApplied_2()
        {
            MetadataContract contract = new MetadataContract();
            EventContext telemetryContext = new EventContext(Guid.NewGuid());

            MetadataContract.Persist("Default", "Prop01", MetadataContract.DefaultCategory);
            MetadataContract.Persist("Dependency", "Dependency01", MetadataContract.DependenciesCategory);
            MetadataContract.Persist("Host", "Host01", MetadataContract.HostCategory);
            MetadataContract.Persist("Scenario", "Scenario01", MetadataContract.ScenarioCategory);
            MetadataContract.Persist("Runtime", "Version1", MetadataContract.RuntimeCategory);

            contract.Add("Default", "Prop02", MetadataContract.DefaultCategory);
            contract.Add("Dependency", "Dependency02", MetadataContract.DependenciesCategory);
            contract.Add("Host", "Host02", MetadataContract.HostCategory);
            contract.Add("Scenario", "Scenario02", MetadataContract.ScenarioCategory);
            contract.Add("Runtime", "Version2", MetadataContract.RuntimeCategory);
            contract.Apply(telemetryContext);

            // Example Format
            // {
            //     "activityId": "d58e13f2-4e84-4cf2-b81f-b270dddcd21f",
            //     "parentActivityId": "00000000-0000-0000-0000-000000000000",
            //     "durationMs": 0,
            //     "transactionId": "eb03c8e0-7c40-4641-84cd-4954a31018b3",
            //     "properties": {
            //         "metadata": {
            //             "default": "Prop02"
            //         },
            //         "metadata_dependencies": {
            //             "dependency": "Dependency02"
            //         },
            //         "metadata_host": {
            //             "host": "Host02"
            //         },
            //         "metadata_scenario": {
            //             "scenario": "Scenario02"
            //         },
            //         "metadata_runtime": {
            //             "runtime": "Version2"
            //         }
            //     },
            // }
            string json = telemetryContext.ToJson().RemoveWhitespace();

            // Note that the property names should be camel-cased all the way down
            // the nested JSON hierarchy.
            Assert.IsTrue(json.Contains("\"metadata\":{\"default\":\"Prop02\"}"));
            Assert.IsTrue(json.Contains("\"metadata_dependencies\":{\"dependency\":\"Dependency02\"}"));
            Assert.IsTrue(json.Contains("\"metadata_host\":{\"host\":\"Host02\"}"));
            Assert.IsTrue(json.Contains("\"metadata_scenario\":{\"scenario\":\"Scenario02\"}"));
            Assert.IsTrue(json.Contains("\"metadata_runtime\":{\"runtime\":\"Version2\"}"));
        }

        [Test]
        [Order(13)]
        public void ComponentExtensionPropertiesRemainValidJsonWhenAppliedToTheMetadataContract()
        {
            // This is the implementation model used in the VirtualClientComponent
            // base class to add 'extensions' info to the telemetry EventContext.
            IDictionary<string, JToken> extensions = new Dictionary<string, JToken>
            {
                {
                    "ListExtension",
                    JToken.FromObject(new List<string> { "String1", "String2" })
                },
                {
                    "DictionaryExtension",
                    JToken.FromObject(new Dictionary<string, int>
                    {
                        ["Key1"] = 1234,
                        ["Key2"] = 5678
                    })
                },
                {
                    "ObjectExtension",
                    JToken.FromObject(new TestClass { Id = "777", Count = 5 })
                }
            };

            MetadataContract metadataContract = new MetadataContract();
            EventContext telemetryContext = new EventContext(Guid.NewGuid());

            foreach (var entry in extensions)
            {
                metadataContract.Add(
                    extensions.Keys.ToDictionary(key => key, entry => extensions[entry] as object),
                    MetadataContract.ScenarioExtensionsCategory,
                    replace: true);
            }

            metadataContract.Apply(telemetryContext);
            string json = telemetryContext.ToJson();

            Assert.IsTrue(json.Contains("\"metadata_scenario_ext\":{\"listExtension\":[\"String1\",\"String2\"],\"dictionaryExtension\":{\"Key1\":1234,\"Key2\":5678},\"objectExtension\":{\"Id\":\"777\",\"Count\":5}}"));
        }

        private class TestClass
        {
            public string Id { get; set; }

            public int Count { get; set; }
        }
    }
}
