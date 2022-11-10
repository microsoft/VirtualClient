// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using NUnit.Framework;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class DependencyExtensionsTests
    {
        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "win-x64")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "win-arm64")]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-arm64")]
        public void ToPlatformSpecificToolsPathCreatesTheExpectedPackageDefinition_SpecificPlatformScenario(PlatformID platform, Architecture architecture, string platformSpecificDirectory)
        {
            TestPlatformSpecifics platformSpecifics = platform == PlatformID.Win32NT
                ? new TestPlatformSpecifics(PlatformID.Win32NT, architecture, @"C:\any\directory")
                : new TestPlatformSpecifics(PlatformID.Win32NT, architecture, @"/any/directory");

            DependencyPath dependency = new DependencyPath(
                "any.package",
                platformSpecifics.GetPackagePath("any.package", "1.0.0"));

            string expectedPath = platformSpecifics.GetPackagePath("any.package", "1.0.0", platformSpecificDirectory);
            string actualPath = platformSpecifics.ToPlatformSpecificPath(dependency, platform, architecture).Path;

            Assert.AreEqual(expectedPath, actualPath);
        }
    }
}
