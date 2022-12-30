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
    public class SpecJbbProfileTests
    {
        private DependencyFixture mockFixture;

        [OneTimeSetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-SPECJBB.json")]
        public void SpecJbbWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [Ignore("We need to rethink how to do dependency testing with extension model.")]
        [TestCase("PERF-SPECJBB.json")]
        public async Task SpecJbbWorkloadProfileInstallsTheExpectedDependenciesOnLinuxPlatforms(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);

            // The location of the Java (Java SDK) executable
            string expectedJavaExecutablePath = this.mockFixture.GetPackagePath("microsoft-jdk-17.0.3/linux-x64/bin/java");
            this.mockFixture.SetupFile(expectedJavaExecutablePath);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package expectations
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "specjbb2015");
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "javadevelopmentkit", pkg =>
                {
                    Assert.IsTrue(pkg.Metadata.TryGetValue(PackageMetadata.ExecutablePath, out IConvertible actualExecutablePath));
                    Assert.AreEqual(actualExecutablePath.ToString(), expectedJavaExecutablePath);
                });
            }
        }

        [Test]
        [Ignore("We need to rethink how to do dependency testing with extension model.")]
        [TestCase("PERF-SPECJBB.json")]
        public async Task SpecJbbWorkloadProfileInstallsTheExpectedDependenciesOnWindowsPlatforms(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT);

            // The location of the Java (Java SDK) executable
            string expectedJavaExecutablePath = this.mockFixture.GetPackagePath(@"microsoft-jdk-17.0.3\win-x64\bin\java.exe");
            this.mockFixture.SetupFile(expectedJavaExecutablePath);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package expectations
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "specjbb2015");
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "javadevelopmentkit", pkg =>
                {
                    Assert.IsTrue(pkg.Metadata.TryGetValue(PackageMetadata.ExecutablePath, out IConvertible actualExecutablePath));
                    Assert.AreEqual(actualExecutablePath.ToString(), expectedJavaExecutablePath);
                });
            }
        }

        [Test]
        [TestCase("PERF-SPECJBB.json")]
        public async Task SpecJbbWorkloadProfileExecutesTheExpectedWorkloadsOnWindowsPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(PlatformID.Win32NT);
            this.SetupDefaultMockBehaviors(PlatformID.Win32NT);
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("-jar", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_SPECJbb.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-SPECJBB.json")]
        public async Task SpecJbbWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(PlatformID.Unix);
            this.SetupDefaultMockBehaviors(PlatformID.Unix);
            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.

            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("-jar", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_SPECJbb.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                executor.ExecuteDependencies = false;
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

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
                        // @"java\.exe -XX:\+AlwaysPreTouch -XX:\+UseLargePages -XX:\+UseTransparentHugePages -XX:\+UseParallelGC -XX:ParallelGCThreads=71 -Xms87040m -Xmx87040m -Xlog:gc\*,gc\+ref=debug,gc\+phases=debug,gc\+age=trace,safepoint:file=gc.log -jar specjbb2015\.jar -m composite -ikv"
                        @"java\.exe -XX:\+AlwaysPreTouch -XX:\+UseLargePages -XX:\+UseParallelGC -XX:ParallelGCThreads=71 -Xms87040m -Xmx87040m -Xlog:gc\*,gc\+ref=debug,gc\+phases=debug,gc\+age=trace,safepoint:file=gc.log -jar specjbb2015\.jar -m composite -ikv"
                    };
                    break;

                case PlatformID.Unix:
                    commands = new List<string>
                    {
                        // @"sudo java -XX:\+AlwaysPreTouch -XX:\+UseLargePages -XX:\+UseTransparentHugePages -XX:\+UseParallelGC -XX:ParallelGCThreads=71 -Xms87040m -Xmx87040m -Xlog:gc\*,gc\+ref=debug,gc\+phases=debug,gc\+age=trace,safepoint:file=gc.log -jar specjbb2015\.jar -m composite -ikv"
                        @"sudo java -XX:\+AlwaysPreTouch -XX:\+UseLargePages -XX:\+UseParallelGC -XX:ParallelGCThreads=71 -Xms87040m -Xmx87040m -Xlog:gc\*,gc\+ref=debug,gc\+phases=debug,gc\+age=trace,safepoint:file=gc.log -jar specjbb2015\.jar -m composite -ikv"
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
                this.mockFixture.SetupWorkloadPackage("specjbb2015", expectedFiles: @"runtimes/win-x64/specjbb2015.jar");

                Dictionary<string, IConvertible> specifics = new Dictionary<string, IConvertible>()
                {
                    { PackageMetadata.ExecutablePath, "java.exe" }
                };

                this.mockFixture.SetupWorkloadPackage("javadevelopmentkit", specifics, expectedFiles: @"runtimes/win-x64/bin/java.exe");
            }
            else
            {
                this.mockFixture.Setup(PlatformID.Unix);
                this.mockFixture.SetupWorkloadPackage("specjbb2015", expectedFiles: @"runtimes/linux-x64/SPECJbb2008.jar");

                Dictionary<string, IConvertible> specifics = new Dictionary<string, IConvertible>()
                {
                    { PackageMetadata.ExecutablePath, "java" }
                };

                this.mockFixture.SetupWorkloadPackage("javadevelopmentkit", specifics, expectedFiles: @"runtimes/linux-x64/bin/java");
            }

            this.mockFixture.SetupDisks(withRemoteDisks: false);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetTotalSystemMemoryKiloBytes()).Returns(1024 * 1024 * 100);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetSystemCoreCount()).Returns(71);
            this.mockFixture.ProcessManager.OnGetProcess = (id) => null;

            // Remove any mock blob managers so that we do not evaluate the code paths that
            // upload log files by default.
            this.mockFixture.Dependencies.RemoveAll<IEnumerable<IBlobManager>>();
        }
    }
}