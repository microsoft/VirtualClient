// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.UnitTests
{
    using System;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    internal class CommandBaseTests : CommandBase
    {
        private MockFixture mockFixture;

        // Note:
        // Property and method overrides are defined at the bottom of this class below
        // the unit tests.

        public void SetupTest(PlatformID platform, Architecture architecture)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedDefaultLogDirectoryLocation(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "logs");
            string defaultDirectory = this.mockFixture.PlatformSpecifics.LogsDirectory;

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.LogsDirectory;

            Assert.AreEqual(expectedDirectory, defaultDirectory, "Default logs directory does not match expected.");
            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedLogDirectoryLocationWhenSpecifiedOnTheCommandLine(PlatformID platform, Architecture architecture)
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
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedLogDirectoryLocationWhenSpecifiedInTheSupportedEnvironmentVariable(PlatformID platform, Architecture architecture)
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
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedDefaultPackagesDirectoryLocation(PlatformID platform, Architecture architecture)
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
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedPackagesDirectoryLocationWhenSpecifiedOnTheCommandLine(PlatformID platform, Architecture architecture)
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
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedPackagesDirectoryLocationWhenSpecifiedInTheSupportedEnvironmentVariable(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            // Setup:
            // Environment variable 'VC_PACKAGES_DIR' can be used to define the logs directory.
            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "alternate_packages_2");
            this.mockFixture.SetEnvironmentVariable(EnvironmentVariable.VC_PACKAGES_DIR, expectedDirectory);

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.PackagesDirectory;

            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedDefaultStateDirectoryLocation(PlatformID platform, Architecture architecture)
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
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedStateDirectoryLocationWhenSpecifiedOnTheCommandLine(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            // Setup:
            // Property set when --state-dir=<path> is used on the command line.
            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "alternate_state_1");
            this.StateDirectory = expectedDirectory;

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.StateDirectory;

            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        [TestCase(PlatformID.Unix, Architecture.X64)]
        [TestCase(PlatformID.Unix, Architecture.Arm64)]
        [TestCase(PlatformID.Win32NT, Architecture.X64)]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64)]
        public void ApplicationUsesTheExpectedStateDirectoryLocationWhenSpecifiedInTheSupportedEnvironmentVariable(PlatformID platform, Architecture architecture)
        {
            this.SetupTest(platform, architecture);

            // Setup:
            // Environment variable 'VC_STATE_DIR' can be used to define the logs directory.
            string expectedDirectory = this.mockFixture.Combine(this.mockFixture.PlatformSpecifics.CurrentDirectory, "alternate_state_2");
            this.mockFixture.SetEnvironmentVariable(EnvironmentVariable.VC_STATE_DIR, expectedDirectory);

            this.EvaluateDirectoryPathOverrides(this.mockFixture.PlatformSpecifics);
            string actualDirectory = this.mockFixture.PlatformSpecifics.StateDirectory;

            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        /// <summary>
        /// Not implemented yet.
        /// </summary>
        public override Task<int> ExecuteAsync(string[] args, CancellationTokenSource cancellationTokenSource)
        {
            throw new NotImplementedException();
        }

        public new void EvaluateDirectoryPathOverrides(PlatformSpecifics platformSpecifics)
        {
            base.EvaluateDirectoryPathOverrides(platformSpecifics);
        }
    }
}
