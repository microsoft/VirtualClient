// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class OpenFOAMProfileTests
    {
        private DependencyFixture fixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.fixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-OPENFOAM.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-OPENFOAM.json", PlatformID.Unix, Architecture.Arm64)]
        public void OpenFOAMWorkloadProfileParametersAreInlinedCorrectly(string profile, PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-OPENFOAM.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-OPENFOAM.json", PlatformID.Unix, Architecture.Arm64)]
        public async Task OpenFOAMWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile, PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(platform, architecture);

            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.fixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.fixture.CreateProcess(command, arguments, workingDir);
                if (arguments.EndsWith("Allrun\"", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("OpenFoamResults.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.fixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-OPENFOAM.json", PlatformID.Unix, Architecture.X64)]
        [TestCase("PERF-OPENFOAM.json", PlatformID.Unix, Architecture.Arm64)]
        public void OpenFOAMWorkloadProfileActionsWillNotBeExecutedIfTheWorkloadPackageDoesNotExist(string profile, PlatformID platform, Architecture architecture)
        {
            this.SetupDefaultMockBehavior(platform, architecture);
            // We ensure the workload package does not exist.
            this.fixture.PackageManager.Clear();

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.fixture.Dependencies))
            {
                executor.ExecuteDependencies = false;

                DependencyException error = Assert.ThrowsAsync<DependencyException>(() => executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None));
                Assert.IsTrue(error.Reason == ErrorReason.WorkloadDependencyMissing);
            }
        }

        private IEnumerable<string> GetProfileExpectedCommands(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            string platformArch = PlatformSpecifics.GetPlatformArchitectureName(platform, architecture);

            List<string> expectedCommands = new List<string>
            {
                $"{platformArch}/airFoil2D/Allclean",
                $"{platformArch}/airFoil2D/Allrun",
                $"{platformArch}/elbow/Allclean",
                $"{platformArch}/elbow/Allrun",
                $"{platformArch}/lockExchange/Allclean",
                $"{platformArch}/lockExchange/Allrun",
                $"{platformArch}/pitzDaily/Allclean",
                $"{platformArch}/pitzDaily/Allrun"
            };

            // motorBike does not run on ARM64
            if (architecture == Architecture.X64)
            {
                expectedCommands.AddRange(new string[]
                {
                    $"{platformArch}/motorBike/Allclean",
                    $"{platformArch}/motorBike/Allrun",
                });
            }

            return expectedCommands;
        }

        private void SetupDefaultMockBehavior(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.fixture.Setup(platform, architecture);
            string platformArch = PlatformSpecifics.GetPlatformArchitectureName(platform, architecture);

            List<string> expectedFiles = new List<string>
            {
                $"{platformArch}/tools/AllrunWrapper",
                $"{platformArch}/airFoil2D/Allclean",
                $"{platformArch}/airFoil2D/Allrun",
                $"{platformArch}/airFoil2D/system/controlDict",
                $"{platformArch}/airFoil2D/log.simpleFoam",
                $"{platformArch}/elbow/Allclean",
                $"{platformArch}/elbow/Allrun",
                $"{platformArch}/elbow/system/controlDict",
                $"{platformArch}/elbow/log.icoFoam",
                $"{platformArch}/lockExchange/Allclean",
                $"{platformArch}/lockExchange/Allrun",
                $"{platformArch}/lockExchange/system/controlDict",
                $"{platformArch}/lockExchange/log.twoLiquidMixingFoam",
                $"{platformArch}/motorBike/Allclean",
                $"{platformArch}/motorBike/Allrun",
                $"{platformArch}/motorBike/system/controlDict",
                $"{platformArch}/motorBike/log.simpleFoam",
                $"{platformArch}/pitzDaily/Allclean",
                $"{platformArch}/pitzDaily/Allrun",
                $"{platformArch}/pitzDaily/system/controlDict",
                $"{platformArch}/pitzDaily/log.simpleFoam"
            };

            this.fixture.SetupWorkloadPackage("openfoam", expectedFiles: expectedFiles.ToArray());

            string resultsFileContent = TestDependencies.GetResourceFileContents("OpenFoamResults.txt");
            this.fixture.SetupFile("openfoam", $"{platformArch}/airFoil2D/log.simpleFoam", resultsFileContent);
            this.fixture.SetupFile("openfoam", $"{platformArch}/elbow/log.icoFoam", resultsFileContent);
            this.fixture.SetupFile("openfoam", $"{platformArch}/lockExchange/log.twoLiquidMixingFoam", resultsFileContent);
            this.fixture.SetupFile("openfoam", $"{platformArch}/motorBike/log.simpleFoam", resultsFileContent);
            this.fixture.SetupFile("openfoam", $"{platformArch}/pitzDaily/log.simpleFoam", resultsFileContent);
        }
    }
}