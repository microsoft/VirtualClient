// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class RunProfileCommandTests
    {
        private static readonly string ProfilesDirectory = Path.Combine(
           Path.GetDirectoryName(Assembly.GetAssembly(typeof(RunProfileCommandTests)).Location),
           "profiles");

        private MockFixture mockFixture;
        private TestRunProfileCommand command;

        [SetUp]
        public void SetupDefaults()
        {
            this.mockFixture = new MockFixture();

            // For these tests, we have to access the actual file system. The tests below will be looking for 
            // profiles in the default 'profiles' directory. We copy all of the profiles for these tests to a
            // directory with that name. We have to set the current working directory to ensure that relative paths
            // work as expected.
            IFileSystem fileSystem = new FileSystem();
            this.mockFixture.Dependencies.RemoveAll<IFileSystem>();
            this.mockFixture.Dependencies.AddSingleton<IFileSystem>(fileSystem);
            this.mockFixture.SystemManagement.SetupGet(sm => sm.FileSystem).Returns(fileSystem);
            this.mockFixture.SystemManagement.SetupGet(sm => sm.PlatformSpecifics).Returns(new TestPlatformSpecifics(
                Environment.OSVersion.Platform,
                RuntimeInformation.OSArchitecture,
                Path.GetDirectoryName(Assembly.GetAssembly(typeof(RunProfileCommandTests)).Location)));

            this.command = new TestRunProfileCommand
            {
                AgentId = "AnyAgent",
                Debug = false,
                Timeout = ProfileTiming.OneIteration(),
                ExecutionSystem = "AnySystem",
                ExperimentId = Guid.NewGuid().ToString(),
                Profiles = new List<string>(),
                InstallDependencies = false
            };
        }

        [Test]
        public async Task RunProfileCommandCreatesTheExpectedProfile_DefaultScenario()
        {
            // Scenario:
            // In the default scenario, a workload profile is supplied that only contains
            // workloads (i.e. no specific monitors).
            List<string> profiles = new List<string> { Path.Combine(RunProfileCommandTests.ProfilesDirectory, "TEST-WORKLOAD-PROFILE.json") };

            ExecutionProfile profile = await this.command.InitializeProfileAsync(profiles,this.mockFixture.Dependencies, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(profile.Actions.Any());
            Assert.IsTrue(profile.Actions.Count == 2);
            Assert.IsTrue(profile.Dependencies.Any());
            Assert.IsTrue(profile.Dependencies.Count == 3);
            Assert.IsTrue(profile.Monitors.Any());
            Assert.IsTrue(profile.Monitors.Count == 2);

            CollectionAssert.AreEqual(
               new List<string> { "Workload1", "Workload2" },
               profile.Actions.Select(a => a.Parameters["Scenario"].ToString()));

            CollectionAssert.AreEqual(
                new List<string> { "Dependency1", "InstallEpelPackage", "InstallAtop" },
                profile.Dependencies.Select(a => a.Parameters["Scenario"].ToString()));

            // The monitors from the MONITORS-DEFAULT.json profile should have been added as well.
            CollectionAssert.AreEqual(
                new List<string> { "CaptureCounters", "CaptureDeviceInformation" },
                profile.Monitors.Select(a => a.Parameters["Scenario"].ToString()));
        }

        [Test]
        public async Task RunProfileCommandCreatesTheExpectedProfile_DefaultMonitorProfileExplicitlyDefinedScenario()
        {
            // Scenario:
            // In the default scenario, a workload profile is supplied that only contains
            // workloads (i.e. no specific monitors).
            List<string> profiles = new List<string>
            {
                Path.Combine(RunProfileCommandTests.ProfilesDirectory, "TEST-WORKLOAD-PROFILE.json"),
                Path.Combine(RunProfileCommandTests.ProfilesDirectory, "MONITORS-DEFAULT.json")
            };

            ExecutionProfile profile = await this.command.InitializeProfileAsync(profiles, this.mockFixture.Dependencies, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(profile.Actions.Any());
            Assert.IsTrue(profile.Actions.Count == 2);
            Assert.IsTrue(profile.Dependencies.Any());
            Assert.IsTrue(profile.Dependencies.Count == 3);
            Assert.IsTrue(profile.Monitors.Any());
            Assert.IsTrue(profile.Monitors.Count == 2);

            CollectionAssert.AreEqual(
                new List<string> { "Workload1", "Workload2" },
                profile.Actions.Select(a => a.Parameters["Scenario"].ToString()));

            CollectionAssert.AreEqual(
                new List<string> { "Dependency1", "InstallEpelPackage", "InstallAtop" },
                profile.Dependencies.Select(a => a.Parameters["Scenario"].ToString()));

            // The monitors from the MONITORS-DEFAULT.json profile should have been added as well.
            CollectionAssert.AreEqual(
                new List<string> { "CaptureCounters", "CaptureDeviceInformation" },
                profile.Monitors.Select(a => a.Parameters["Scenario"].ToString()));
        }

        [Test]
        public async Task RunProfileCommandCreatesTheExpectedProfile_MonitorsExcludedScenario()
        {
            // Scenario:
            // In the default scenario, a workload profile is supplied that only contains
            // workloads (i.e. no specific monitors).
            List<string> profiles = new List<string>
            {
                Path.Combine(RunProfileCommandTests.ProfilesDirectory, "TEST-WORKLOAD-PROFILE.json"),
                Path.Combine(RunProfileCommandTests.ProfilesDirectory, "MONITORS-NONE.json")
            };

            ExecutionProfile profile = await this.command.InitializeProfileAsync(profiles, this.mockFixture.Dependencies, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(profile.Actions.Any());
            Assert.IsTrue(profile.Actions.Count == 2);
            Assert.IsTrue(profile.Dependencies.Any());
            Assert.IsTrue(profile.Dependencies.Count == 1);
            Assert.IsFalse(profile.Monitors.Any());

            CollectionAssert.AreEqual(
                new List<string> { "Workload1", "Workload2" },
                profile.Actions.Select(a => a.Parameters["Scenario"].ToString()));

            CollectionAssert.AreEqual(
                new List<string> { "Dependency1" },
                profile.Dependencies.Select(a => a.Parameters["Scenario"].ToString()));
        }

        [Test]
        public async Task RunProfileCommandCreatesTheExpectedProfile_ProfileWithActionsDependenciesAndMonitorsScenario()
        {
            // Scenario:
            // In the default scenario, a workload profile is supplied that only contains
            // workloads (i.e. no specific monitors).
            List<string> profiles = new List<string> { Path.Combine(RunProfileCommandTests.ProfilesDirectory, "TEST-WORKLOAD-PROFILE-2.json") };

            ExecutionProfile profile = await this.command.InitializeProfileAsync(profiles, this.mockFixture.Dependencies, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsTrue(profile.Actions.Any());
            Assert.IsTrue(profile.Actions.Count == 2);
            Assert.IsTrue(profile.Dependencies.Any());
            Assert.IsTrue(profile.Dependencies.Count == 2);
            Assert.IsTrue(profile.Monitors.Any());
            Assert.IsTrue(profile.Monitors.Count == 2);

            CollectionAssert.AreEqual(
                new List<string> { "Workload1", "Workload2" },
                profile.Actions.Select(a => a.Parameters["Scenario"].ToString()));

            CollectionAssert.AreEqual(
                new List<string> { "Dependency1", "Dependency2" },
                profile.Dependencies.Select(a => a.Parameters["Scenario"].ToString()));

            // The monitors from the MONITORS-DEFAULT.json profile should have been added as well.
            CollectionAssert.AreEqual(
                new List<string> { "Monitor1", "Monitor2" },
                profile.Monitors.Select(a => a.Parameters["Scenario"].ToString()));
        }

        [Test]
        public async Task RunProfileCommandCreatesTheExpectedProfile_ProfilesWithMonitorsOnlyScenario()
        {
            // Scenario:
            // In the default scenario, a workload profile is supplied that only contains
            // workloads (i.e. no specific monitors).
            List<string> profiles = new List<string> { Path.Combine(RunProfileCommandTests.ProfilesDirectory, "MONITORS-DEFAULT.json") };

            ExecutionProfile profile = await this.command.InitializeProfileAsync(profiles, this.mockFixture.Dependencies, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.IsFalse(profile.Actions.Any());
            Assert.IsTrue(profile.Dependencies.Any());
            Assert.IsTrue(profile.Dependencies.Count == 2);
            Assert.IsTrue(profile.Monitors.Any());
            Assert.IsTrue(profile.Monitors.Count == 2);

            CollectionAssert.AreEqual(
                new List<string> { "InstallEpelPackage", "InstallAtop" },
                profile.Dependencies.Select(a => a.Parameters["Scenario"].ToString()));

            // The monitors from the MONITORS-DEFAULT.json profile should have been added as well.
            CollectionAssert.AreEqual(
                new List<string> { "CaptureCounters", "CaptureDeviceInformation" },
                profile.Monitors.Select(a => a.Parameters["Scenario"].ToString()));
        }

        private class TestRunProfileCommand : RunProfileCommand
        {
            public new Task<ExecutionProfile> InitializeProfileAsync(IEnumerable<string> profiles, IServiceCollection dependencies, CancellationToken cancellationToken)
            {
                return base.InitializeProfileAsync(profiles, dependencies, cancellationToken);
            }

            public new void SetGlobalTelemetryProperties(IEnumerable<string> profiles, IServiceCollection dependencies)
            {
                base.SetGlobalTelemetryProperties(profiles, dependencies);
            }

            public new void SetHostMetadata(IEnumerable<string> profiles, IServiceCollection dependencies)
            {
                base.SetHostMetadata(profiles, dependencies);
            }
        }

        private static Tuple<string, string> GetAccessTokenPair()
        {
            // Note:
            // You cannot have secrets anywhere in plain text including fake/mock secrets used
            // to test logic that obscures them. We are using a technique here of converting a byte
            // array into the string at runtime to avoid being flagged by scanners.
            var originalBytes = new List<byte>
            {
                115, 114, 113, 102, 119, 114, 101, 52, 53, 102, 49, 101, 106, 112, 107, 109, 51, 100, 103, 113, 114, 56,
                121, 53, 100, 119, 99, 113, 110, 113, 106, 114, 108, 120, 109, 100, 53, 120, 101, 104, 97, 112, 100, 107,
                110, 113, 111, 109, 116, 113, 116, 97
            };

            // 80, 101, 114, 115, 111, 110, 97, 108, 65, 99, 99, 101, 115, 115, 84, 111, 107, 101, 110, 61,

            var obscuredBytes = new List<byte>
            {
                46, 46, 46
            };

            string decodedOriginalBytes = Encoding.UTF8.GetString(originalBytes.ToArray());
            string decodedObscuredBytes = Encoding.UTF8.GetString(obscuredBytes.ToArray());

            return new Tuple<string, string>(decodedOriginalBytes, decodedObscuredBytes);
        }
    }
}
