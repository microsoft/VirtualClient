// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using YamlDotNet.Serialization;

    [TestFixture]
    [Category("Unit")]
    public class ProfileParameterAliasTests
    {
        private const string ProfileJson = @"{
            'Description': 'Profile aliases',
            'ParameterAliases': { 'Threads': 'ThreadCount' },
            'Parameters': { 'ThreadCount': 5 },
            'Actions': [{ 'Type': 'AnyAction', 'Parameters': { 'ThreadCount': '$.Parameters.ThreadCount' } }]
        }";

        [TestCase("JSON")]
        [TestCase("Copy")]
        [TestCase("YAML")]
        public void ParameterAliasesSurviveProfileSerializationAndCopying(string format)
        {
            ExecutionProfile profile = JsonConvert.DeserializeObject<ExecutionProfile>(ProfileJson);
            if (format == "JSON")
            {
                profile = JsonConvert.DeserializeObject<ExecutionProfile>(JsonConvert.SerializeObject(profile));
            }
            else if (format == "Copy")
            {
                profile = new ExecutionProfile(profile);
            }
            else
            {
                string yaml = new SerializerBuilder()
                    .WithTypeConverter(new YamlParameterDictionaryTypeConverter())
                    .Build()
                    .Serialize(new ExecutionProfileYamlShim(profile));
                var shim = new DeserializerBuilder()
                    .WithTypeConverter(new YamlParameterDictionaryTypeConverter())
                    .Build()
                    .Deserialize<ExecutionProfileYamlShim>(yaml);
                profile = new ExecutionProfile(shim);
            }

            Assert.AreEqual("ThreadCount", profile.ParameterAliases["threads"]);
            profile.ApplyParameterOverrides(new Dictionary<string, IConvertible> { ["THREADS"] = "17" });
            profile.Inline();
            Assert.AreEqual("17", profile.Actions[0].Parameters["ThreadCount"]);
        }

        [Test]
        public void ApplyingAliasesDoesNotChangeCallerOverridesOrCreateChainedOverrides()
        {
            ExecutionProfile profile = JsonConvert.DeserializeObject<ExecutionProfile>(ProfileJson);
            profile.ParameterAliases["ThreadCount"] = "UnrelatedCount";
            profile.Parameters["UnrelatedCount"] = 29;
            var overrides = new Dictionary<string, IConvertible> { ["Threads"] = "17" };

            profile.ApplyParameterOverrides(overrides);

            Assert.AreEqual("17", profile.Parameters["ThreadCount"]);
            Assert.AreEqual(29, profile.Parameters["UnrelatedCount"]);
            Assert.AreEqual(1, overrides.Count);
            Assert.IsFalse(overrides.ContainsKey("ThreadCount"));
        }

        [Test]
        public void EmptyOverridesLeaveDefaultsAndAliasesUnchanged()
        {
            ExecutionProfile profile = JsonConvert.DeserializeObject<ExecutionProfile>(ProfileJson);
            profile.ApplyParameterOverrides(new Dictionary<string, IConvertible>());
            profile.Inline();

            Assert.AreEqual("5", profile.Actions[0].Parameters["ThreadCount"].ToString());
            Assert.IsFalse(profile.Parameters.ContainsKey("Threads"));
        }
    }
}
