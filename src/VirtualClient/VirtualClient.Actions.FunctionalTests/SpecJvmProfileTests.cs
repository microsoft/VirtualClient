// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Functional")]
    public class SpecJvmProfileTests
    {
        private DependencyFixture mockFixture;

        [SetUp]
        public void SetupFixture()
        {
            this.mockFixture = new DependencyFixture();
            ComponentTypeCache.Instance.LoadComponentTypes(TestDependencies.TestDirectory);
        }

        [Test]
        [TestCase("PERF-SPECJVM.json")]
        public void SpecJvmWorkloadProfileParametersAreInlinedCorrectly(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);
            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                WorkloadAssert.ParameterReferencesInlined(executor.Profile);
            }
        }

        [Test]
        [TestCase("PERF-SPECJVM.json")]
        public async Task SpecJvmWorkloadProfileInstallsTheExpectedDependenciesOnLinuxPlatforms(string profile)
        {
            this.mockFixture.Setup(PlatformID.Unix);

            // The location of the Java (Java SDK) executable
            string expectedJavaExecutablePath = this.mockFixture.GetPackagePath("microsoft-jdk-17.0.3/linux-x64/bin/java");
            this.mockFixture.SetupFile(expectedJavaExecutablePath);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package expectations
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "specjvm2008");
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "javadevelopmentkit", pkg =>
                {
                    Assert.IsTrue(pkg.Metadata.TryGetValue(PackageMetadata.ExecutablePath, out IConvertible actualExecutablePath));
                    Assert.AreEqual(actualExecutablePath.ToString(), expectedJavaExecutablePath);
                });
            }
        }

        [Test]
        [TestCase("PERF-SPECJVM.json")]
        public async Task SpecJvmWorkloadProfileInstallsTheExpectedDependenciesOnWindowsPlatforms(string profile)
        {
            this.mockFixture.Setup(PlatformID.Win32NT);

            // The location of the Java (Java SDK) executable
            string expectedJavaExecutablePath = this.mockFixture.GetPackagePath(@"microsoft-jdk-17.0.3\win-x64\bin\java.exe");
            this.mockFixture.SetupFile(expectedJavaExecutablePath);

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies, dependenciesOnly: true))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                // Workload dependency package expectations
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "specjvm2008");
                WorkloadAssert.WorkloadPackageInstalled(this.mockFixture, "javadevelopmentkit", pkg =>
                {
                    Assert.IsTrue(pkg.Metadata.TryGetValue(PackageMetadata.ExecutablePath, out IConvertible actualExecutablePath));
                    Assert.AreEqual(actualExecutablePath.ToString(), expectedJavaExecutablePath);
                });
            }
        }

        [Test]
        [TestCase("PERF-SPECJVM.json")]
        public async Task SpecJvmWorkloadProfileExecutesTheExpectedWorkloadsOnWindowsPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(PlatformID.Win32NT);

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>()
            {
                { PackageMetadata.ExecutablePath, "java.exe" }
            };

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Win32NT);
            this.mockFixture.SetupDisks(withRemoteDisks: false);
            this.mockFixture.SetupWorkloadPackage("specjvm2008", expectedFiles: @"win-x64\SPECjvm2008.jar");
            this.mockFixture.SetupWorkloadPackage("javadevelopmentkit", metadata, expectedFiles: @"win-x64\bin\java.exe");

            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetTotalSystemMemoryKiloBytes()).Returns(1024 * 1024 * 100);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetSystemCoreCount()).Returns(71);

            this.mockFixture.ProcessManager.OnGetProcess = (id) => null;
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("-jar", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_SPECjvm.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
                await executor.ExecuteAsync(ProfileTiming.OneIteration(), CancellationToken.None).ConfigureAwait(false);

                WorkloadAssert.CommandsExecuted(this.mockFixture, expectedCommands.ToArray());
            }
        }

        [Test]
        [TestCase("PERF-SPECJVM.json")]
        public async Task SpecJvmWorkloadProfileExecutesTheExpectedWorkloadsOnUnixPlatform(string profile)
        {
            IEnumerable<string> expectedCommands = this.GetProfileExpectedCommands(PlatformID.Unix);

            Dictionary<string, IConvertible> metadata = new Dictionary<string, IConvertible>()
            {
                { PackageMetadata.ExecutablePath, "java" }
            };

            // Setup the expectations for the workload
            // - Workload package is installed and exists.
            // - Workload binaries/executables exist on the file system.
            // - The workload generates valid results.
            this.mockFixture.Setup(PlatformID.Unix);
            this.mockFixture.SetupDisks(withRemoteDisks: false);
            this.mockFixture.SetupWorkloadPackage("specjvm2008", expectedFiles: @"linux-x64/SPECjvm2008.jar");
            this.mockFixture.SetupWorkloadPackage("javadevelopmentkit", metadata, expectedFiles: @"linux-x64/bin/java");

            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetTotalSystemMemoryKiloBytes()).Returns(1024 * 1024 * 100);
            this.mockFixture.SystemManagement.Setup(mgr => mgr.GetSystemCoreCount()).Returns(71);

            this.mockFixture.ProcessManager.OnGetProcess = (id) => null;
            this.mockFixture.ProcessManager.OnCreateProcess = (command, arguments, workingDir) =>
            {
                IProcessProxy process = this.mockFixture.CreateProcess(command, arguments, workingDir);
                if (arguments.Contains("-jar", StringComparison.OrdinalIgnoreCase))
                {
                    process.StandardOutput.Append(TestDependencies.GetResourceFileContents("Results_SPECjvm.txt"));
                }

                return process;
            };

            using (ProfileExecutor executor = TestDependencies.CreateProfileExecutor(profile, this.mockFixture.Dependencies))
            {
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
                        @"java\.exe -XX:ParallelGCThreads=[0-9]+ -XX:\+UseParallelGC -XX:\+UseAES -XX:\+UseSHA -Xms[0-9]+m -Xmx[0-9]+m -jar SPECjvm2008.jar -ikv -ict compress crypto derby mpegaudio scimark serial sunflow"
                    };
                    break;

                case PlatformID.Unix:
                    commands = new List<string>
                    {
                        @"sudo java -XX:ParallelGCThreads=[0-9]+ -XX:\+UseParallelGC -XX:\+UseAES -XX:\+UseSHA -Xms[0-9]+m -Xmx[0-9]+m -jar SPECjvm2008.jar -ikv -ict compress crypto derby mpegaudio scimark serial sunflow"
                    };
                    break;
            }

            return commands;
        }
    }
}
