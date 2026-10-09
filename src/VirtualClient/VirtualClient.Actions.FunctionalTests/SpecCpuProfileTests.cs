// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Contracts;
    using NUnit.Framework;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Functional")]
    public class SpecCpuProfileTests
    {
        private DependencyFixture mockFixture;

        [SetUp]
        public void SetupTests()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(MockFixture.TestAssemblyDirectory);
        }

        [Test]
        [TestCase("PERF-SPECCPU-FPRATE.json")]
        [TestCase("PERF-SPECCPU-FPSPEED.json")]
        [TestCase("PERF-SPECCPU-INTRATE.json")]
        [TestCase("PERF-SPECCPU-INTSPEED.json")]
        [TestCase("PERF-SPECCPU2026-FPRATE.json")]
        [TestCase("PERF-SPECCPU2026-FPSPEED.json")]
        [TestCase("PERF-SPECCPU2026-INTRATE.json")]
        [TestCase("PERF-SPECCPU2026-INTSPEED.json")]
        public void SpecCpuWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-SPECCPU-INTRATE.json")]
        [TestCase("PERF-SPECCPU-FPRATE.json")]
        public async Task SpecCpuRateProfilesForwardProcessorAffinityToEveryAction(string profileName)
        {
            this.mockFixture.Setup(PlatformID.Unix);

            ExecutionProfile profile = await ExecutionProfile.ReadProfileAsync(Path.Combine(MockFixture.TestResourcesDirectory, "profiles", profileName));
            profile.Parameters["ProcessorAffinity"] = "2,4,8,10";
            profile.Parameters["Copies"] = 4;
            profile.Inline();

            List<ExecutionProfileElement> actions = profile.Actions.Where(action => action.Type == "SpecCpuExecutor").ToList();
            Assert.IsNotEmpty(actions);
            foreach (ExecutionProfileElement action in actions)
            {
                Assert.AreEqual("2,4,8,10", action.Parameters["ProcessorAffinity"]);
                Assert.AreEqual("4", action.Parameters["Copies"].ToString());
            }
        }

        [Test]
        [TestCase("PERF-SPECCPU2026-FPRATE.json", Architecture.X64)]
        [TestCase("PERF-SPECCPU2026-FPRATE.json", Architecture.Arm64)]
        [TestCase("PERF-SPECCPU2026-FPSPEED.json", Architecture.X64)]
        [TestCase("PERF-SPECCPU2026-FPSPEED.json", Architecture.Arm64)]
        [TestCase("PERF-SPECCPU2026-INTRATE.json", Architecture.X64)]
        [TestCase("PERF-SPECCPU2026-INTRATE.json", Architecture.Arm64)]
        [TestCase("PERF-SPECCPU2026-INTSPEED.json", Architecture.X64)]
        [TestCase("PERF-SPECCPU2026-INTSPEED.json", Architecture.Arm64)]
        public void SpecCpu2026WorkloadProfileParametersAreInlinedCorrectlyOnWindows(string profile, Architecture architecture)
        {
            this.mockFixture.Setup(PlatformID.Win32NT, architecture);
            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase(
            PlatformID.Unix,
            Architecture.X64,
            13,
            "-O2 -march=x86-64-v3 -flto -frecord-gcc-switches",
            "-O3 -march=x86-64-v3 -flto -frecord-gcc-switches")]
        [TestCase(
            PlatformID.Unix,
            Architecture.Arm64,
            13,
            "-O2 -march=armv8.2-a -flto -frecord-gcc-switches",
            "-O3 -march=armv8.2-a -flto -frecord-gcc-switches")]
        [TestCase(
            PlatformID.Win32NT,
            Architecture.X64,
            13,
            "-O2 -march=x86-64-v3 -flto -frecord-gcc-switches",
            "-O3 -march=x86-64-v3 -flto -frecord-gcc-switches")]
        [TestCase(
            PlatformID.Win32NT,
            Architecture.Arm64,
            13,
            "-O2 -march=armv8.2-a -flto -frecord-gcc-switches",
            "-O3 -march=armv8.2-a -flto -frecord-gcc-switches")]
        public async Task SpecCpu2026WorkloadProfilesUseExpectedDefaultRecipes(
            PlatformID platform,
            Architecture architecture,
            object expectedCompilerVersion,
            string expectedBaseFlags,
            string expectedPeakFlags)
        {
            string[] profiles =
            {
                "PERF-SPECCPU2026-FPRATE.json",
                "PERF-SPECCPU2026-FPSPEED.json",
                "PERF-SPECCPU2026-INTRATE.json",
                "PERF-SPECCPU2026-INTSPEED.json"
            };

            this.mockFixture.Setup(platform, architecture);

            foreach (string profile in profiles)
            {
                using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
                {
                    ExecutionProfileElement action = executor.Profile.Actions.Single();
                    ExecutionProfileElement compilerInstallation = executor.Profile.Dependencies.Single(
                        dependency => dependency.Type == "CompilerInstallation");

                    await ProfileExpressionEvaluator.Instance.EvaluateAsync(this.mockFixture.Dependencies, action.Parameters);

                    Assert.AreEqual(expectedCompilerVersion, compilerInstallation.Parameters["CompilerVersion"]);
                    Assert.AreEqual(expectedBaseFlags, action.Parameters["BaseOptimizingFlags"]);
                    Assert.AreEqual(expectedPeakFlags, action.Parameters["PeakOptimizingFlags"]);
                    Assert.AreEqual("12:00:00", executor.Profile.Metadata["RecommendedMinimumExecutionTime"]);
                    Assert.AreEqual("AzureLinux,CentOS,Debian,OpenSuse,RedHat,Ubuntu,Windows", executor.Profile.Metadata["SupportedOperatingSystems"]);
                }
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

            using (ProfileExecutor executor = TestProfileResources.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
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