// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Metadata
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Common.Extensions;
    using VirtualClient.Common.Telemetry;
    using VirtualClient.Contracts.Metadata;

    [TestFixture]
    [Category("Unit")]
    internal class MetadataContractExtensionsTests
    {
        [Test]
        public void ExtensionsReturnsTheExpectedCategoryMetadata()
        {
            EventContext telemetryContext = new EventContext(Guid.NewGuid());
            telemetryContext.Properties["metadata_ext"] = new Dictionary<string, object>
            {
                { "AnyProperty", "AnyValue" }
            };

            IDictionary<string, object> metadata = telemetryContext.GetMetadata("metadata_ext");
            Assert.IsNotNull(metadata);
            Assert.IsTrue(metadata.TryGetValue("AnyProperty", out object value) && value.ToString() == "AnyValue");
        }

        [Test]
        public void MetadataContractCategoriesMustFollowThePrescribedNamingConvention()
        {
            EventContext telemetryContext = new EventContext(Guid.NewGuid());
            Assert.DoesNotThrow(() => telemetryContext.AddMetadata("AnyProperty1", "AnyValue1", "metadata_ext"));
            Assert.Throws<SchemaException>(() => telemetryContext.AddMetadata("AnyProperty2", "AnyValue2", "notavalidprefix_ext"));
        }

        [Test]
        public void MetadataContractCategoriesAreNotCaseSensitive()
        {
            EventContext telemetryContext = new EventContext(Guid.NewGuid());

            string category = "metadata_ext";
            telemetryContext.AddMetadata("AnyProperty", "AnyValue", category);

            IDictionary<string, object> metadata = telemetryContext.GetMetadata(category.ToUpperInvariant());
            Assert.IsNotNull(metadata);
        }

        [Test]
        public void AddMetadataExtensionAddsMetadataToTheExpectedCategory_1()
        {
            EventContext telemetryContext = new EventContext(Guid.NewGuid());
            telemetryContext.AddMetadata("AnyProperty", "AnyValue", "metadata_ext");

            IDictionary<string, object> metadata = telemetryContext.GetMetadata("metadata_ext");
            Assert.IsNotNull(metadata);
            Assert.IsTrue(metadata.TryGetValue("AnyProperty", out object value));
            Assert.AreEqual("AnyValue", value);
        }

        [Test]
        [TestCase(MetadataContractCategory.Default, MetadataContractExtensions.CategoryDefault)]
        [TestCase(MetadataContractCategory.Hardware, MetadataContractExtensions.CategoryHardware)]
        [TestCase(MetadataContractCategory.Host, MetadataContractExtensions.CategoryHost)]
        [TestCase(MetadataContractCategory.Runtime, MetadataContractExtensions.CategoryRuntime)]
        [TestCase(MetadataContractCategory.Scenario, MetadataContractExtensions.CategoryScenario)]
        public void AddMetadataExtensionAddsMetadataToTheExpectedStandardCategories_1(MetadataContractCategory category, string expectedCategoryName)
        {
            EventContext telemetryContext = new EventContext(Guid.NewGuid());
            telemetryContext.AddMetadata("AnyProperty", "AnyValue", category);

            IDictionary<string, object> metadata = telemetryContext.GetMetadata(expectedCategoryName);
            Assert.IsNotNull(metadata);
            Assert.IsTrue(metadata.TryGetValue("AnyProperty", out object value));
            Assert.AreEqual("AnyValue", value);
        }

        [Test]
        [TestCase(MetadataContractCategory.Default)]
        [TestCase(MetadataContractCategory.Hardware)]
        [TestCase(MetadataContractCategory.Host)]
        [TestCase(MetadataContractCategory.Runtime)]
        [TestCase(MetadataContractCategory.Scenario)]
        public void AddMetadataExtensionAddsMetadataToTheExpectedStandardCategories_2(MetadataContractCategory category)
        {
            EventContext telemetryContext = new EventContext(Guid.NewGuid());
            telemetryContext.AddMetadata("AnyProperty", "AnyValue", category);

            IDictionary<string, object> metadata = telemetryContext.GetMetadata(category);
            Assert.IsNotNull(metadata);
            Assert.IsTrue(metadata.TryGetValue("AnyProperty", out object value));
            Assert.AreEqual("AnyValue", value);
        }

        [Test]
        public void MetadataContractsSerializeAsExpectedWithEventContextObjects()
        {
            EventContext telemetryContext = new EventContext(Guid.NewGuid());
            telemetryContext.AddMetadata("Default", "Prop01", MetadataContractCategory.Default);
            telemetryContext.AddMetadata("HW", "HW01", MetadataContractCategory.Hardware);
            telemetryContext.AddMetadata("Host", "Host01", MetadataContractCategory.Host);
            telemetryContext.AddMetadata("Scenario", "Scenario01", MetadataContractCategory.Scenario);
            telemetryContext.AddMetadata("Runtime", "Version", MetadataContractCategory.Runtime);

            // Example Format
            // {
            //     "activityId": "d58e13f2-4e84-4cf2-b81f-b270dddcd21f",
            //     "parentActivityId": "00000000-0000-0000-0000-000000000000",
            //     "durationMs": 0,
            //     "transactionId": "eb03c8e0-7c40-4641-84cd-4954a31018b3",
            //     "properties": {
            //         "metadata_hw": {
            //             "hw": "HW01"
            //         },
            //         "metadata_os": {
            //             "operatingSystem": "Windows"
            //         },
            //         "metadata_scenario": {
            //             "scenario": "Scenario01"
            //         },
            //         "metadata_vc": {
            //             "virtualClient": "Version"
            //         },
            //         "metadata_host": {
            //             "host": "Host01"
            //         }
            //     },
            // }
            string json = telemetryContext.ToJson().RemoveWhitespace();

            // Note that the property names should be camel-cased all the way down
            // the nested JSON hierarchy.
            Assert.IsTrue(json.Contains("\"metadata\":{\"default\":\"Prop01\"}"));
            Assert.IsTrue(json.Contains("\"metadata_hw\":{\"hw\":\"HW01\"}"));
            Assert.IsTrue(json.Contains("\"metadata_host\":{\"host\":\"Host01\"}"));
            Assert.IsTrue(json.Contains("\"metadata_scenario\":{\"scenario\":\"Scenario01\"}"));
            Assert.IsTrue(json.Contains("\"metadata_runtime\":{\"runtime\":\"Version\"}"));
        }

        [Test]
        public void GetMetadataExtensionThrowsIfAnInvalidNonDictionaryTypeIsUsedForTheMetadataSet_1()
        {
            EventContext telemetryContext = new EventContext(Guid.NewGuid());
            telemetryContext.Properties["metadata_ext"] = "Not a dictionary at all";

            Assert.Throws<SchemaException>(() => telemetryContext.GetMetadata("metadata_ext"));
        }

        [Test]
        public void GetMetadataExtensionThrowsIfAnInvalidNonDictionaryTypeIsUsedForTheMetadataSet_2()
        {
            EventContext telemetryContext = new EventContext(Guid.NewGuid());
            telemetryContext.Properties["metadata_ext"] = new Dictionary<string, string>
            {
                // Wrong dictionary data type. Expected IDictionary<string, IConvertible>
                { "AnyProperty", "AnyValue" }
            };

            Assert.Throws<SchemaException>(() => telemetryContext.GetMetadata("metadata_ext"));
        }
    }
}
