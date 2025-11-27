// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.UnitTests
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    internal partial class CommandBaseTests : CommandBase
    {
        [Test]
        [Order(100)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void SdkAgentApplicationUsesTheExpectedDefaultLogDirectoryLocation(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            this.ExperimentId = Guid.NewGuid().ToString();
            this.ClientId = Environment.MachineName;

            string expectedDirectory = this.mockFixture.Combine(
                this.mockFixture.PlatformSpecifics.CurrentDirectory, 
                "/../logs", 
                this.ClientId.ToLowerInvariant(), 
                this.ExperimentId.ToLowerInvariant());

            this.ApplyAgentDefaults(this.mockFixture.PlatformSpecifics);

            Assert.AreEqual(expectedDirectory, this.LogDirectory, "Default logs directory does not match expected.");

        }

        [Test]
        [Order(101)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void SdkAgentApplicationUsesTheExpectedLogDirectoryLocationWhenSpecifiedOnTheCommandLine(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            // Setup:
            // Property set when --log-dir=<path> is used on the command line.
            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "alternate_logs_1");
            this.LogDirectory = expectedDirectory;

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.LogsDirectory;

            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [Order(102)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void SdkAgentApplicationUsesTheExpectedLogDirectoryLocationWhenSpecifiedInTheSupportedEnvironmentVariable(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            // Setup:
            // Environment variable 'VC_LOGS_DIR' can be used to define the logs directory.
            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "alternate_logs_2");
            this.mockFixture.SetEnvironmentVariable(EnvironmentVariable.VC_LOGS_DIR, expectedDirectory);

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.LogsDirectory;

            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [Order(103)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void SdkAgentApplicationUsesTheExpectedDefaultPackagesDirectoryLocation(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "packages");
            string defaultDirectory = this.mockFixture.PlatformSpecifics.PackagesDirectory;

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.PackagesDirectory;

            Assert.AreEqual(expectedDirectory, defaultDirectory, "Default packages directory does not match expected.");
            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [Order(104)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void SdkAgentApplicationUsesTheExpectedPackagesDirectoryLocationWhenSpecifiedOnTheCommandLine(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            // Setup:
            // Property set when --package-dir=<path> is used on the command line.
            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "alternate_packages_1");
            this.PackageDirectory = expectedDirectory;

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.PackagesDirectory;

            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [Order(105)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void SdkAgentApplicationUsesTheExpectedDefaultStateDirectoryLocation(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            PlatformSpecifics platformSpecifics = new PlatformSpecifics(platform, architecture);
            string expectedDirectory = this.mockFixture.Combine(MockFixture.GetDirectory(typeof(CommandBaseTests), "state"));
            string defaultDirectory = platformSpecifics.StateDirectory;

            this.EvaluateDirectoryPathOverrides(platformSpecifics);
            string actualDirectory = platformSpecifics.StateDirectory;

            Assert.AreEqual(expectedDirectory, defaultDirectory, "Default state directory does not match expected.");
            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [Order(106)]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void SdkAgentApplicationUsesTheExpectedDefaultTempDirectoryLocation(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            PlatformSpecifics platformSpecifics = new PlatformSpecifics(platform, architecture);
            string expectedDirectory = this.mockFixture.Combine(MockFixture.GetDirectory(typeof(CommandBaseTests), "temp"));
            string defaultDirectory = platformSpecifics.TempDirectory;

            this.EvaluateDirectoryPathOverrides(platformSpecifics);
            string actualDirectory = platformSpecifics.TempDirectory;

            Assert.AreEqual(expectedDirectory, defaultDirectory, "Default temp directory does not match expected.");
            Assert.AreEqual(expectedDirectory, actualDirectory);
        }
    }
}
