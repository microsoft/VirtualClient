// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using Newtonsoft.Json;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class ProfileParameterCompatibilityTests
    {
        private static IEnumerable<TestCaseData> PackagedProfileCases
        {
            get
            {
                var aliases = new Dictionary<string, string[]>
                {
                    ["GET-STARTED-REDIS"] = new[] { "ServerInstances=ServerInstanceCount", "ClientInstances=ClientInstanceCount" },
                    ["MONITORS-GPU-NVIDIA"] = new[] { "MonitorIterations=MonitorIterationCount" },
                    ["PERF-CPU-DOTNETRUNTIME"] = new[] { "NumberOfJvmInstances=JvmInstanceCount", "NumberOfWarehouses=WarehouseCount" },
                    ["PERF-CPU-HPLINPACK"] = new[] { "NumberOfProcesses=ProcessCount" },
                    ["PERF-CPU-HPLINPACK-AMD"] = new[] { "NumberOfProcesses=ProcessCount" },
                    ["PERF-CPU-HPLINPACK-ARM"] = new[] { "NumberOfProcesses=ProcessCount" },
                    ["PERF-CPU-HPLINPACK-INTEL"] = new[] { "NumberOfProcesses=ProcessCount" },
                    ["PERF-CPU-OPENSSL-TLS"] = new[] { "ClientInstances=ClientInstanceCount" },
                    ["PERF-MEMCACHED"] = new[] { "ServerMaxConnections=MaxServerConnectionCount", "ClientInstances=ClientInstanceCount" },
                    ["PERF-NETWORK-CTSTRAFFIC"] = new[] { "Connections=ConnectionCount", "Iterations=IterationCount" },
                    ["PERF-NETWORK-PING"] = new[] { "PingIterations=PingIterationCount" },
                    ["PERF-OPENFOAM"] = new[] { "Iterations=IterationCount" },
                    ["PERF-POSTGRESQL-HAMMERDB-TPCC"] = new[] { "VirtualUsers=VirtualUserCount" },
                    ["PERF-POSTGRESQL-HAMMERDB-TPCH"] = new[] { "VirtualUsers=VirtualUserCount" },
                    ["PERF-REDIS"] = new[] { "ServerInstances=ServerInstanceCount", "ClientInstances=ClientInstanceCount" },
                    ["PERF-WEB-NGINX-WRK"] = new[] { "Workers=WorkerCount" },
                    ["PERF-WEB-NGINX-WRK-RP"] = new[] { "Workers=WorkerCount" },
                    ["PERF-WEB-NGINX-WRK2"] = new[] { "Workers=WorkerCount" },
                    ["PERF-WEB-NGINX-WRK2-RP"] = new[] { "Workers=WorkerCount" },
                    ["PERF-APACHEBENCH"] = new[] { "NoOfRequests=RequestCount", "NoOfConcurrentRequests=ConcurrentRequestCount" }
                };

                foreach (string version in new[] { string.Empty, "2026" })
                {
                    foreach (string workload in new[] { "FPRATE", "FPSPEED", "INTRATE", "INTSPEED" })
                    {
                        aliases[$"PERF-SPECCPU{version}-{workload}"] = new[] { "Iterations=IterationCount", "Threads=ThreadCount", "Copies=CopyCount" };
                    }
                }

                foreach (var profile in aliases)
                {
                    foreach (string pair in profile.Value)
                    {
                        string[] names = pair.Split('=');
                        foreach (string scenario in new[] { "Legacy", "Canonical", "Both", "Default", "LegacyNull", "CanonicalNull" })
                        {
                            yield return new TestCaseData(profile.Key + ".json", names[0], names[1], scenario)
                                .SetName($"{profile.Key}_{names[0]}_{scenario}");
                        }
                    }
                }
            }
        }

        [TestCaseSource(nameof(PackagedProfileCases))]
        public async Task PackagedProfilesBindExplicitOverridesBeforeInlining(string profileName, string legacy, string canonical, string scenario)
        {
            string content = File.ReadAllText(Path.Combine(MockFixture.TestAssemblyDirectory, "packaged_profiles", profileName));
            ExecutionProfile original = JsonConvert.DeserializeObject<ExecutionProfile>(content);
            Assert.AreEqual(canonical, original.ParameterAliases[legacy]);
            var overrides = new Dictionary<string, IConvertible>();
            IConvertible expected = original.Parameters[canonical];

            switch (scenario)
            {
                case "Legacy":
                    overrides[legacy.ToLowerInvariant()] = expected = "17";
                    break;
                case "Canonical":
                    overrides[canonical.ToLowerInvariant()] = expected = "23";
                    break;
                case "Both":
                    overrides[canonical.ToLowerInvariant()] = expected = "23";
                    overrides[legacy.ToLowerInvariant()] = "17";
                    break;
                case "LegacyNull":
                    overrides[legacy] = expected = null;
                    break;
                case "CanonicalNull":
                    overrides[canonical] = expected = null;
                    overrides[legacy] = "17";
                    break;
            }

            ExecutionProfile initialized = await InitializeProfileAsync(profileName, content, overrides);
            Assert.AreEqual(expected, initialized.Parameters[canonical]);

            List<ExecutionProfileElement> sourceElements = GetElements(original).ToList();
            List<ExecutionProfileElement> boundElements = GetElements(initialized).ToList();
            int referencesChecked = 0;
            for (int index = 0; index < sourceElements.Count; index++)
            {
                foreach (var parameter in sourceElements[index].Parameters)
                {
                    if (parameter.Value?.ToString() == ExecutionProfile.ParameterPrefix + canonical)
                    {
                        Assert.AreEqual(expected, boundElements[index].Parameters[parameter.Key], $"{sourceElements[index].Type}.{parameter.Key}");
                        referencesChecked++;
                    }
                }
            }

            if (profileName == "PERF-CPU-OPENSSL-TLS.json")
            {
                // TLS currently exposes the top-level count without consuming it in a component.
                Assert.AreEqual(0, referencesChecked);
            }
            else
            {
                Assert.Greater(referencesChecked, 0, "The packaged profile must actually bind this parameter into a component.");
            }
        }

        [Test]
        public async Task CustomProfilesDoNotRewriteUndeclaredGenericNames()
        {
            const string content = @"{
                'Description': 'Custom profile with unrelated names',
                'Parameters': { 'Threads': 3, 'ThreadCount': 5, 'Iterations': 7, 'IterationCount': 11, 'Connections': 13, 'ConnectionCount': 19 },
                'Actions': [{
                    'Type': 'CustomAction',
                    'Parameters': {
                        'Threads': '$.Parameters.Threads', 'ThreadCount': '$.Parameters.ThreadCount',
                        'Iterations': '$.Parameters.Iterations', 'IterationCount': '$.Parameters.IterationCount',
                        'Connections': '$.Parameters.Connections', 'ConnectionCount': '$.Parameters.ConnectionCount'
                    }
                }]
            }";
            var overrides = new Dictionary<string, IConvertible> { ["Threads"] = "17", ["Iterations"] = "23", ["Connections"] = "29" };
            ExecutionProfile initialized = await InitializeProfileAsync("CUSTOM.json", content, overrides);

            Assert.AreEqual("17", initialized.Actions[0].Parameters["Threads"]);
            Assert.AreEqual("23", initialized.Actions[0].Parameters["Iterations"]);
            Assert.AreEqual("29", initialized.Actions[0].Parameters["Connections"]);
            Assert.AreEqual("5", initialized.Actions[0].Parameters["ThreadCount"].ToString());
            Assert.AreEqual("11", initialized.Actions[0].Parameters["IterationCount"].ToString());
            Assert.AreEqual("19", initialized.Actions[0].Parameters["ConnectionCount"].ToString());
        }

        [Test]
        public async Task AliasesAreResolvedIndependentlyBeforeCombiningProfiles()
        {
            const string aliasedProfile = @"{
                'Description': 'Standardized profile',
                'ParameterAliases': { 'Threads': 'ThreadCount' },
                'Parameters': { 'ThreadCount': 3 },
                'Actions': [{ 'Type': 'StandardizedAction', 'Parameters': { 'Count': '$.Parameters.ThreadCount' } }]
            }";
            const string customProfile = @"{
                'Description': 'Custom profile',
                'Parameters': { 'Threads': 5, 'ThreadCount': 7 },
                'Actions': [{ 'Type': 'CustomAction', 'Parameters': { 'Legacy': '$.Parameters.Threads', 'Other': '$.Parameters.ThreadCount' } }]
            }";
            var fixture = new MockFixture();
            fixture.File.Setup(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns<string, CancellationToken>((path, token) => Task.FromResult(path == "ALIASED.json" ? aliasedProfile : customProfile));
            var command = new TestProfileCommand
            {
                Parameters = new Dictionary<string, IConvertible> { ["Threads"] = "17" }
            };

            ExecutionProfile combined = (await command.InitializeManyAsync(new[] { "ALIASED.json", "CUSTOM.json" }, fixture.Dependencies))
                .Select(descriptor => descriptor.Profile).Merge();

            Assert.AreEqual("17", combined.Actions[0].Parameters["Count"]);
            Assert.AreEqual("17", combined.Actions[1].Parameters["Legacy"]);
            Assert.AreEqual("7", combined.Actions[1].Parameters["Other"].ToString());
            Assert.AreEqual(1, command.Parameters.Count);
        }

        [TestCase("Legacy", "17", "matched")]
        [TestCase("Canonical", "23", "default")]
        [TestCase("Both", "23", "default")]
        [TestCase("Default", "5", "default")]
        public async Task AliasesAreResolvedBeforeConditionalParameters(string scenario, string expectedCount, string expectedSelection)
        {
            const string content = @"{
                'Description': 'Conditional profile aliases',
                'ParameterAliases': { 'Threads': 'ThreadCount' },
                'Parameters': { 'ThreadCount': 5, 'Selection': 'default' },
                'ParametersOn': [{ 'Condition': '{calculate({ThreadCount} == 17)}', 'Selection': 'matched' }],
                'Actions': [{
                    'Type': 'AnyAction',
                    'Parameters': { 'Count': '$.Parameters.ThreadCount', 'Selection': '$.Parameters.Selection' }
                }]
            }";
            var overrides = new Dictionary<string, IConvertible>();
            if (scenario == "Legacy" || scenario == "Both")
            {
                overrides["Threads"] = "17";
            }

            if (scenario == "Canonical" || scenario == "Both")
            {
                overrides["ThreadCount"] = "23";
            }

            ExecutionProfile initialized = await InitializeProfileAsync("CONDITIONAL.json", content, overrides);

            Assert.AreEqual(expectedCount, initialized.Actions[0].Parameters["Count"].ToString());
            Assert.AreEqual(expectedSelection, initialized.Actions[0].Parameters["Selection"]);
        }

        private static async Task<ExecutionProfile> InitializeProfileAsync(string name, string content, IDictionary<string, IConvertible> overrides)
        {
            var fixture = new MockFixture();
            fixture.File.Setup(file => file.ReadAllTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(content);
            var command = new TestProfileCommand { Parameters = overrides };
            return await command.InitializeAsync(name, fixture.Dependencies);
        }

        private static IEnumerable<ExecutionProfileElement> GetElements(ExecutionProfile profile)
        {
            return GetElements(profile.Actions.Concat(profile.Dependencies).Concat(profile.Monitors));
        }

        private static IEnumerable<ExecutionProfileElement> GetElements(IEnumerable<ExecutionProfileElement> elements)
        {
            foreach (ExecutionProfileElement element in elements)
            {
                yield return element;
                foreach (ExecutionProfileElement child in GetElements(element.Components ?? Enumerable.Empty<ExecutionProfileElement>()))
                {
                    yield return child;
                }
            }
        }

        private class TestProfileCommand : ExecuteProfileCommand
        {
            public Task<ExecutionProfile> InitializeAsync(string path, IServiceCollection dependencies)
            {
                return this.InitializeProfileAsync(path, dependencies, CancellationToken.None);
            }

            public Task<IEnumerable<ExecutionProfileDescriptor>> InitializeManyAsync(IEnumerable<string> paths, IServiceCollection dependencies)
            {
                return this.InitializeProfilesAsync(paths, dependencies, CancellationToken.None);
            }
        }
    }
}
