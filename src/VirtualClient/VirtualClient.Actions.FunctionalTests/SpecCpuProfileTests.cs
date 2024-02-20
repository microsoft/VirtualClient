// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using NUnit.Framework;

    [TestFixture]
    [Category("Functional")]
    public class SpecCpuProfileTests
    {
        private DependencyFixture mockFixture;

        [SetUp]
        public void SetupTests()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-SPECCPU-FPRATE.json")]
        [TestCase("PERF-SPECCPU-FPSPEED.json")]
        [TestCase("PERF-SPECCPU-INTRATE.json")]
        [TestCase("PERF-SPECCPU-INTSPEED.json")]
        public void SpecCpuWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [Ignore("We need to rethink how to do dependency testing with extension model.")]
        [TestCase("PERF-SPECCPU-FPRATE.json")]
        [TestCase("PERF-SPECCPU-FPSPEED.json")]
        [TestCase("PERF-SPECCPU-INTRATE.json")]
        [TestCase("PERF-SPECCPU-INTSPEED.json")]
        public async Task SpecCpuWorkloadProfileInstallsTheExpectedDependenciesOnLinuxPlatforms(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupLinuxPackagesInstalled(new Dictionary<string, string>
            {
                { "gcc", "10" }, // Should match profile defaults.
                { "cc", "10" }
            });

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package expectations
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "speccpu2017", pkg =>
                {
                    pkg.Path.EndsWith($"/speccpu2017.[\x20-\x7E]+.zip");
                });
            }
        }
    }
}