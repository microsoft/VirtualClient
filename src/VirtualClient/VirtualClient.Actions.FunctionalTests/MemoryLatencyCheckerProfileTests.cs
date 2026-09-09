// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using global::VirtualClient;
    using global::VirtualClient.Common;
    using global::VirtualClient.Contracts;
    using NUnit.Framework;

    [TestFixture]
    [Category("Functional")]
    public class MemoryLatencyCheckerProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-MEM-LATENCY.json", PlatformID.Unix)]
        [TestCase("PERF-MEM-LATENCY.json", PlatformID.Win32NT)]
        public void MemoryLatencyCheckerWorkloadProfileParametersAreInlinedCorrectly(string profile, PlatformID platform)
        {
            this.mockFixture.Setup(platform);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-MEM-LATENCY.json", PlatformID.Unix)]
        [TestCase("PERF-MEM-LATENCY.json", PlatformID.Win32NT)]
        public async Task MemoryLatencyCheckerWorkloadProfileExecutesTheExpectedWorkloads(string profile, PlatformID platform)
        {
            this.mockFixture.Setup(platform);
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(platform);
            this.SetupDefaultMockBehaviors(platform);

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                process.StandardOutput.Append(TestDependencies.GetResourceFileContents("MemoryLatencyChecker", "memory_latency_checker_results_1.txt"));

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None)
                    .ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        private IEnumerable<string> GetProfileExpectedCommands(PlatformID platform)
        {
            List<string> commands = null;
            switch (platform)
            {
                case PlatformID.Win32NT:
                    commands = new List<string>
                    {
                        @$"{this.mockFixture.GetPackagePath()}\mlc\win-x64\mlc.exe --latency_matrix"
                    };
                    break;

                case PlatformID.Unix:
                    commands = new List<string>
                    {
                        $"sudo chmod +x \"{this.mockFixture.GetPackagePath()}/mlc/linux-x64/mlc\"",
                        @$"sudo {this.mockFixture.GetPackagePath()}/mlc/linux-x64/mlc --latency_matrix"
                    };
                    break;
            }

            return commands;
        }

        private void SetupDefaultMockBehaviors(PlatformID platform)
        {
            if (platform == PlatformID.Win32NT)
            {
                this.mockFixture.Setup(PlatformID.Win32NT);
                this.mockFixture.SetupPackage("mlc", expectedFiles: @"win-x64/mlc.exe");
            }
            else
            {
                this.mockFixture.Setup(PlatformID.Unix);
                this.mockFixture.SetupPackage("mlc", expectedFiles: @"linux-x64/mlc");

                string packagePath = this.mockFixture.PlatformSpecifics.GetPackagePath("mlc");
                string executablePath = this.mockFixture.PlatformSpecifics.Combine(packagePath, "linux-x64", "mlc");
                // this.mockFixture.SetupFile(executablePath);
            }
        }
    }
}