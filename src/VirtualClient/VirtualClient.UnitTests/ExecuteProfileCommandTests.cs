// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Net.Http;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using Microsoft.Extensions.DependencyInjection;
    using Moq;
    using NUnit.Framework;
    using VirtualClient.Contracts;
    using VirtualClient.TestExtensions;

    [TestFixture]
    [Category("Unit")]
    public class ExecuteProfileCommandTests
    {
        private static readonly string ProfilesDirectory = PlatformSpecifics.StandardizePath(
            Environment.OSVersion.Platform,
            Path.Combine(MockFixture.TestAssemblyDirectory, "profiles"),
            true);

        private MockFixture mockFixture;
        private TestRunProfileCommand command;

        [SetUp]
        public void SetupDefaults()
        {
            this.mockFixture = new MockFixture();

            this.command = new TestRunProfileCommand
            {
                ClientId = "AnyAgent",
                Verbose = false,
                Timeout = ProfileTiming.OneIteration(),
                ExecutionSystem = "AnySystem",
                ExperimentId = Guid.NewGuid().ToString(),
                Profiles = new List<DependencyProfileReference>(),
                InstallDependencies = false
            };
        }

        [Test]
        public async Task RunProfileCommandSupportsProfilesThatExistInTheDefaultProfilesLocation()
        {
            this.command.Profiles = new List<DependencyProfileReference>
            {
                new DependencyProfileReference("PROFILE1.json"),
                new DependencyProfileReference("PROFILE2.json")
            };

            // Setup:
            // The profiles exist in the default 'profiles' directory
            this.mockFixture.File.Setup(file => file.Exists(this.mockFixture.GetProfilesPath("PROFILE1.json")))
                .Returns(true);

            this.mockFixture.File.Setup(file => file.Exists(this.mockFixture.GetProfilesPath("PROFILE2.json")))
                .Returns(true);

            IEnumerable<string> profilePaths = await this.command.EvaluateProfilesAsync(this.mockFixture.Dependencies);

            Assert.IsNotNull(profilePaths);
            CollectionAssert.AreEquivalent(
                this.command.Profiles.Select(reference => this.mockFixture.GetProfilesPath(reference.ProfileName)),
                profilePaths);
        }

        [Test]
        public async Task RunProfileCommandSupportsProfilesThatExistInTheDefaultProfileDownloadsLocation()
        {
            this.command.Profiles = new List<DependencyProfileReference>
            {
                new DependencyProfileReference("PROFILE1.json"),
                new DependencyProfileReference("PROFILE2.json")
            };

            // Setup:
            // The profiles exist in the default 'profiles' directory
            this.mockFixture.File.Setup(file => file.Exists(this.mockFixture.GetProfileDownloadsPath("PROFILE1.json")))
                .Returns(true);

            this.mockFixture.File.Setup(file => file.Exists(this.mockFixture.GetProfileDownloadsPath("PROFILE2.json")))
                .Returns(true);

            IEnumerable<string> profilePaths = await this.command.EvaluateProfilesAsync(this.mockFixture.Dependencies);

            Assert.IsNotNull(profilePaths);
            CollectionAssert.AreEquivalent(
                this.command.Profiles.Select(reference => this.mockFixture.GetProfileDownloadsPath(reference.ProfileName)),
                profilePaths);
        }

        [Test]
        public async Task RunProfileCommandSupportsProfilesThatExistInAnExtensionsLocation()
        {
            this.command.Profiles = new List<DependencyProfileReference>
            {
                new DependencyProfileReference("PROFILE1.json"),
                new DependencyProfileReference("PROFILE2.json")
            };

            // Setup:
            // Extensions packages exist that have profiles available.
            string extensionsProfile1Path = this.mockFixture.GetPackagePath("anyextensions.1.0.0", "profiles", "PROFILE1.json");
            Mock<IFileInfo> extensionsProfile1 = new Mock<IFileInfo>().Setup(extensionsProfile1Path);

            string extensionsProfile2Path = this.mockFixture.GetPackagePath("anyextensions.1.0.0", "profiles", "PROFILE2.json");
            Mock<IFileInfo> extensionsProfile2 = new Mock<IFileInfo>().Setup(extensionsProfile2Path);

            this.command.Extensions = new PlatformExtensions(profiles: new List<IFileInfo>
            {
                extensionsProfile1.Object,
                extensionsProfile2.Object
            });

            // Setup:
            // The profiles exist in the default 'profiles' directory
            this.mockFixture.File.Setup(file => file.Exists(extensionsProfile1Path))
                .Returns(true);

            this.mockFixture.File.Setup(file => file.Exists(extensionsProfile2Path))
                .Returns(true);

            IEnumerable<string> profilePaths = await this.command.EvaluateProfilesAsync(this.mockFixture.Dependencies);

            Assert.IsNotNull(profilePaths);
            CollectionAssert.AreEquivalent(
                new List<string>
                {
                    extensionsProfile1Path,
                    extensionsProfile2Path
                },
                profilePaths);
        }

        [Test]
        public async Task RunProfileCommandCreatesTheExpectedProfile_DefaultScenario()
        {
            // Scenario:
            // In the default scenario, a workload profile is supplied that only contains
            // workloads (i.e. no specific monitors).
            string profile1 = "TEST-WORKLOAD-PROFILE.json";
            string defaultMonitorProfile = "MONITORS-DEFAULT.json";
            List<string> profiles = new List<string> { this.mockFixture.GetProfilesPath(profile1) };

            // Setup:
            // Read the actual profile content from the local file system.
            this.mockFixture.File
                .Setup(file => file.ReadAllTextAsync(It.Is<string>(file => file.EndsWith(profile1)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(File.ReadAllText(this.mockFixture.Combine(ExecuteProfileCommandTests.ProfilesDirectory, profile1)));

            this.mockFixture.File
                .Setup(file => file.ReadAllTextAsync(It.Is<string>(file => file.EndsWith(defaultMonitorProfile)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(File.ReadAllText(this.mockFixture.Combine(ExecuteProfileCommandTests.ProfilesDirectory, defaultMonitorProfile)));

            ExecutionProfile profile = await this.command.InitializeProfilesAsync(profiles,this.mockFixture.Dependencies, CancellationToken.None)
                .ConfigureAwait(false);

            Assert.AreEqual(2, profile.Actions.Count);
            Assert.AreEqual(1, profile.Dependencies.Count);
            Assert.AreEqual(0, profile.Monitors.Count);

            CollectionAssert.AreEqual(
               new List<string> { "Workload1", "Workload2" },
               profile.Actions.Select(a => a.Parameters["Scenario"].ToString()));
        }

        [Test]
        public async Task RunProfileCommandAddsTheExpectedMetadataToProfile()
        {
            // Scenario:
            // In the default scenario, a workload profile is supplied that only contains
            // workloads (i.e. no specific monitors).
            string profile1 = "TEST-WORKLOAD-PROFILE.json";
            string defaultMonitorProfile = "MONITORS-DEFAULT.json";
            List<string> profiles = new List<string> { this.mockFixture.GetProfilesPath(profile1) };
            this.command.Metadata = new Dictionary<string, IConvertible>();
            this.command.Metadata.Add("MetadataKey1", "MetadataValue1");
            this.command.Metadata.Add("MetadataKey2", "MetadataValue2");

            // Setup:
            // Read the actual profile content from the local file system.
            this.mockFixture.File
                .Setup(file => file.ReadAllTextAsync(It.Is<string>(file => file.EndsWith(profile1)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(File.ReadAllText(this.mockFixture.Combine(ExecuteProfileCommandTests.ProfilesDirectory, profile1)));

            this.mockFixture.File
                .Setup(file => file.ReadAllTextAsync(It.Is<string>(file => file.EndsWith(defaultMonitorProfile)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(File.ReadAllText(this.mockFixture.Combine(ExecuteProfileCommandTests.ProfilesDirectory, defaultMonitorProfile)));

            ExecutionProfile profile = await this.command.InitializeProfilesAsync(profiles, this.mockFixture.Dependencies, CancellationToken.None)
                .ConfigureAwait(false);

            bool isCommandMetadataSubset = this.command.Metadata.All(kvp =>
            profile.Metadata.TryGetValue(kvp.Key, out var value) && value.Equals(kvp.Value));
            
            Assert.IsTrue(isCommandMetadataSubset);
        }

        [Test]
        public async Task RunProfileCommandAddsTheExpectedParametersToProfile()
        {
            // Scenario:
            // In the default scenario, a workload profile is supplied that only contains
            // workloads (i.e. no specific monitors).
            string profile1 = "TEST-WORKLOAD-PROFILE.json";
            string defaultMonitorProfile = "MONITORS-DEFAULT.json";
            List<string> profiles = new List<string> { this.mockFixture.GetProfilesPath(profile1) };
            this.command.Parameters = new Dictionary<string, IConvertible>();
            this.command.Parameters.Add("ParameterKey1", "ParameterValue1");
            this.command.Parameters.Add("ParameterKey2", "ParameterValue2");

            // Setup:
            // Read the actual profile content from the local file system.
            this.mockFixture.File
                .Setup(file => file.ReadAllTextAsync(It.Is<string>(file => file.EndsWith(profile1)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(File.ReadAllText(this.mockFixture.Combine(ExecuteProfileCommandTests.ProfilesDirectory, profile1)));

            this.mockFixture.File
                .Setup(file => file.ReadAllTextAsync(It.Is<string>(file => file.EndsWith(defaultMonitorProfile)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(File.ReadAllText(this.mockFixture.Combine(ExecuteProfileCommandTests.ProfilesDirectory, defaultMonitorProfile)));

            ExecutionProfile profile = await this.command.InitializeProfilesAsync(profiles, this.mockFixture.Dependencies, CancellationToken.None)
                .ConfigureAwait(false);

            bool isCommandParametersSubset = this.command.Parameters.All(kvp =>
            profile.Parameters.TryGetValue(kvp.Key, out var value) && value.Equals(kvp.Value));

            Assert.IsTrue(isCommandParametersSubset);
        }

        [Test]
        public async Task RunProfileCommandCreatesTheExpectedProfile_DefaultMonitorProfileExplicitlyDefinedScenario()
        {
            // Scenario:
            // In the default scenario, a workload profile is supplied that only contains
            // workloads (i.e. no specific monitors).
            string profile1 = "TEST-WORKLOAD-PROFILE.json";
            string defaultMonitorProfile = "MONITORS-DEFAULT.json";

            List<string> profiles = new List<string>
            {
                Path.Combine(ExecuteProfileCommandTests.ProfilesDirectory, profile1),
                Path.Combine(ExecuteProfileCommandTests.ProfilesDirectory, defaultMonitorProfile)
            };

            // Setup:
            // Read the actual profile content from the local file system.
            this.mockFixture.File
                .Setup(file => file.ReadAllTextAsync(It.Is<string>(file => file.EndsWith(profile1)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(File.ReadAllText(this.mockFixture.Combine(ExecuteProfileCommandTests.ProfilesDirectory, profile1)));

            this.mockFixture.File
                .Setup(file => file.ReadAllTextAsync(It.Is<string>(file => file.EndsWith(defaultMonitorProfile)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(File.ReadAllText(this.mockFixture.Combine(ExecuteProfileCommandTests.ProfilesDirectory, defaultMonitorProfile)));

            ExecutionProfile profile = await this.command.InitializeProfilesAsync(profiles, this.mockFixture.Dependencies, CancellationToken.None)
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
            string profile1 = "TEST-WORKLOAD-PROFILE.json";
            string noMonitorsProfile = "MONITORS-NONE.json";

            List<string> profiles = new List<string>
            {
                Path.Combine(ExecuteProfileCommandTests.ProfilesDirectory, profile1),
                Path.Combine(ExecuteProfileCommandTests.ProfilesDirectory, noMonitorsProfile)
            };

            // Setup:
            // Read the actual profile content from the local file system.
            this.mockFixture.File
                .Setup(file => file.ReadAllTextAsync(It.Is<string>(file => file.EndsWith(profile1)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(File.ReadAllText(this.mockFixture.Combine(ExecuteProfileCommandTests.ProfilesDirectory, profile1)));

            this.mockFixture.File
                .Setup(file => file.ReadAllTextAsync(It.Is<string>(file => file.EndsWith(noMonitorsProfile)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(File.ReadAllText(this.mockFixture.Combine(ExecuteProfileCommandTests.ProfilesDirectory, noMonitorsProfile)));

            ExecutionProfile profile = await this.command.InitializeProfilesAsync(profiles, this.mockFixture.Dependencies, CancellationToken.None)
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
            string profile1 = "TEST-WORKLOAD-PROFILE-2.json";

            List<string> profiles = new List<string>
            {
                Path.Combine(ExecuteProfileCommandTests.ProfilesDirectory, profile1),
            };

            // Setup:
            // Read the actual profile content from the local file system.
            this.mockFixture.File
                .Setup(file => file.ReadAllTextAsync(It.Is<string>(file => file.EndsWith(profile1)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(File.ReadAllText(this.mockFixture.Combine(ExecuteProfileCommandTests.ProfilesDirectory, profile1)));

            ExecutionProfile profile = await this.command.InitializeProfilesAsync(profiles, this.mockFixture.Dependencies, CancellationToken.None)
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
            string defaultMonitorProfile = "MONITORS-DEFAULT.json";

            List<string> profiles = new List<string>
            {
                Path.Combine(ExecuteProfileCommandTests.ProfilesDirectory, defaultMonitorProfile),
            };

            // Setup:
            // Read the actual profile content from the local file system.
            this.mockFixture.File
                .Setup(file => file.ReadAllTextAsync(It.Is<string>(file => file.EndsWith(defaultMonitorProfile)), It.IsAny<CancellationToken>()))
                .ReturnsAsync(File.ReadAllText(this.mockFixture.Combine(ExecuteProfileCommandTests.ProfilesDirectory, defaultMonitorProfile)));

            ExecutionProfile profile = await this.command.InitializeProfilesAsync(profiles, this.mockFixture.Dependencies, CancellationToken.None)
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

        private class TestRunProfileCommand : ExecuteProfileCommand
        {
            public new PlatformExtensions Extensions
            {
                get
                {
                    return base.Extensions;
                }

                set
                {
                    base.Extensions = value;
                }
            }

            public Action<IServiceCollection, DependencyProfileReference, string> OnDownloadProfile { get; set; }

            public new Task<IEnumerable<string>> EvaluateProfilesAsync(IServiceCollection dependencies, bool initialize = false, CancellationToken cancellationToken = default(CancellationToken))
            {
                return base.EvaluateProfilesAsync(dependencies, initialize, cancellationToken);
            }

            public new Task<ExecutionProfile> InitializeProfilesAsync(IEnumerable<string> profiles, IServiceCollection dependencies, CancellationToken cancellationToken)
            {
                return base.InitializeProfilesAsync(profiles, dependencies, cancellationToken);
            }

            public new void SetGlobalTelemetryProperties(IEnumerable<string> profiles, IServiceCollection dependencies)
            {
                base.SetGlobalTelemetryProperties(profiles, dependencies);
            }

            public new void SetHostMetadataTelemetryProperties(IEnumerable<string> profiles, IServiceCollection dependencies)
            {
                base.SetHostMetadataTelemetryProperties(profiles, dependencies);
            }

            protected override Task DownloadProfileAsync(IServiceCollection dependencies, DependencyProfileReference profile, string profilePath, CancellationToken cancellationToken)
            {
                this.OnDownloadProfile?.Invoke(dependencies, profile, profilePath);
                return Task.CompletedTask;
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
