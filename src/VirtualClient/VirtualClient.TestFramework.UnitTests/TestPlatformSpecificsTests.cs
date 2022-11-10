// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class TestPlatformSpecificsTests
    {
        [Test]
        public void TestPlatformSpecificsWorksWithPathCombineUsagesCorrectlyOnWindowsSystems()
        {
            TestPlatformSpecifics platformSpecifics = new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\users\current");

            string expectedPath = @"C:\users\current\packages\VirtualClient";
            string actualPath = platformSpecifics.StandardizePath(Path.Combine(platformSpecifics.CurrentDirectory, "packages", "VirtualClient"));
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        public void TestPlatformSpecificsWorksWithPathCombineUsagesCorrectlyOnLinuxSystems()
        {
            TestPlatformSpecifics platformSpecifics = new TestPlatformSpecifics(PlatformID.Unix, "/home/users/current");

            string expectedPath = @"/home/users/current/packages/VirtualClient";
            string actualPath = platformSpecifics.StandardizePath(Path.Combine(platformSpecifics.CurrentDirectory, "packages", "VirtualClient"));
            Assert.AreEqual(expectedPath, actualPath);
        }
    }
}
