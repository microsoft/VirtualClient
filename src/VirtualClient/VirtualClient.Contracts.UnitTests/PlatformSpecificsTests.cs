// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Fare;
    using Microsoft.CodeAnalysis.Scripting;
    using Microsoft.Extensions.FileSystemGlobbing.Internal;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class PlatformSpecificsTests
    {
        private static Assembly dllAssembly = Assembly.GetAssembly(typeof(DependencyPathTests));

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void FundamentalPathsMatchExpectedPaths()
        {
            string currentDirectory = Path.GetDirectoryName(PlatformSpecificsTests.dllAssembly.Location);
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64);

            Assert.AreEqual(currentDirectory, platformSpecifics.CurrentDirectory);
            Assert.AreEqual(Path.Combine(currentDirectory, "logs"), platformSpecifics.LogsDirectory);
            Assert.AreEqual(Path.Combine(currentDirectory, "packages"), platformSpecifics.PackagesDirectory);
            Assert.AreEqual(Path.Combine(currentDirectory, "profiles"), platformSpecifics.ProfilesDirectory);
            Assert.AreEqual(Path.Combine(currentDirectory, "scripts"), platformSpecifics.ScriptsDirectory);
            Assert.AreEqual(Path.Combine(currentDirectory, "state"), platformSpecifics.StateDirectory);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void FundamentalPathsMatchExpectedPathsWhenUseUnixStylePathsOnlyIsUsed()
        {
            // e.g.
            // C:\Any\Path\To\The\Test -> C:/Any/Path/To/The/Test
            string currentDirectory = Path.GetDirectoryName(PlatformSpecificsTests.dllAssembly.Location).Replace("\\", "/");
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64, useUnixStylePathsOnly: true);

            Assert.AreEqual(currentDirectory, platformSpecifics.CurrentDirectory);
            Assert.AreEqual($"{currentDirectory}/logs", platformSpecifics.LogsDirectory);
            Assert.AreEqual($"{currentDirectory}/packages", platformSpecifics.PackagesDirectory);
            Assert.AreEqual($"{currentDirectory}/profiles", platformSpecifics.ProfilesDirectory);
            Assert.AreEqual($"{currentDirectory}/scripts", platformSpecifics.ScriptsDirectory);
            Assert.AreEqual($"{currentDirectory}/state", platformSpecifics.StateDirectory);
        }

        [Test]
        public void GetPackagePathReturnsTheExpectedPathOnUnixSystems()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics2(PlatformID.Unix, Architecture.X64, "/home/anyuser/virtualclient");

            Assert.AreEqual("/home/anyuser/virtualclient/packages", platformSpecifics.GetPackagePath());
            Assert.AreEqual("/home/anyuser/virtualclient/packages/any.package/1.0.0", platformSpecifics.GetPackagePath("/any.package/1.0.0"));
            Assert.AreEqual("/home/anyuser/virtualclient/packages/any.package/1.0.0", platformSpecifics.GetPackagePath("/any.package", "/1.0.0"));
            Assert.AreEqual("/home/anyuser/virtualclient/packages/any.package/1.0.0", platformSpecifics.GetPackagePath("any.package", "1.0.0"));
        }

        [Test]
        public void GetPackagePathReturnsTheExpectedPathOnWindowsSystems()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics2(PlatformID.Win32NT, Architecture.X64, @"C:\users\anyuser\virtualclient");

            Assert.AreEqual(@"C:\users\anyuser\virtualclient\packages", platformSpecifics.GetPackagePath());
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\packages\any.package\1.0.0", platformSpecifics.GetPackagePath(@"\any.package\1.0.0"));
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\packages\any.package\1.0.0", platformSpecifics.GetPackagePath(@"\any.package", @"\1.0.0"));
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\packages\any.package\1.0.0", platformSpecifics.GetPackagePath(@"any.package", "1.0.0"));
        }

        [Test]
        public void GetProfilePathReturnsTheExpectedPathOnUnixSystems()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics2(PlatformID.Unix, Architecture.X64, "/home/anyuser/virtualclient");

            Assert.AreEqual("/home/anyuser/virtualclient/profiles", platformSpecifics.GetProfilePath());
            Assert.AreEqual("/home/anyuser/virtualclient/profiles/ANY-PROFILE.json", platformSpecifics.GetProfilePath("ANY-PROFILE.json"));
            Assert.AreEqual("/home/anyuser/virtualclient/profiles/other/ANY-PROFILE.json", platformSpecifics.GetProfilePath("/other", "ANY-PROFILE.json"));
            Assert.AreEqual("/home/anyuser/virtualclient/profiles/other/ANY-PROFILE.json", platformSpecifics.GetProfilePath("other", "ANY-PROFILE.json"));
        }

        [Test]
        public void GetProfilePathReturnsTheExpectedPathOnWindowsSystems()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics2(PlatformID.Win32NT, Architecture.X64, @"C:\users\anyuser\virtualclient");

            Assert.AreEqual(@"C:\users\anyuser\virtualclient\profiles", platformSpecifics.GetProfilePath());
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\profiles\ANY-PROFILE.json", platformSpecifics.GetProfilePath(@"ANY-PROFILE.json"));
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\profiles\other\ANY-PROFILE.json", platformSpecifics.GetProfilePath(@"\other", "ANY-PROFILE.json"));
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\profiles\other\ANY-PROFILE.json", platformSpecifics.GetProfilePath("other", "ANY-PROFILE.json"));
        }

        [Test]
        public void GetScriptPathReturnsTheExpectedPathOnUnixSystems()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics2(PlatformID.Unix, Architecture.X64, "/home/anyuser/virtualclient");

            Assert.AreEqual("/home/anyuser/virtualclient/scripts", platformSpecifics.GetScriptPath());
            Assert.AreEqual("/home/anyuser/virtualclient/scripts/anyscript.sh", platformSpecifics.GetScriptPath("/anyscript.sh"));
            Assert.AreEqual("/home/anyuser/virtualclient/scripts/other/anyscript.sh", platformSpecifics.GetScriptPath("/other", "/anyscript.sh"));
            Assert.AreEqual("/home/anyuser/virtualclient/scripts/other/anyscript.sh", platformSpecifics.GetScriptPath("other", "anyscript.sh"));
        }

        [Test]
        public void GetScriptPathReturnsTheExpectedPathOnWindowsSystems()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics2(PlatformID.Win32NT, Architecture.X64, @"C:\users\anyuser\virtualclient");

            Assert.AreEqual(@"C:\users\anyuser\virtualclient\scripts", platformSpecifics.GetScriptPath());
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\scripts\anyscript.cmd", platformSpecifics.GetScriptPath(@"anyscript.cmd"));
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\scripts\other\anyscript.cmd", platformSpecifics.GetScriptPath(@"\other", "anyscript.cmd"));
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\scripts\other\anyscript.cmd", platformSpecifics.GetScriptPath(@"\other", "anyscript.cmd"));
        }

        [Test]
        public void GetStatePathReturnsTheExpectedPathOnUnixSystems()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics2(PlatformID.Unix, Architecture.X64, "/home/anyuser/virtualclient");

            Assert.AreEqual("/home/anyuser/virtualclient/state", platformSpecifics.GetStatePath());
            Assert.AreEqual("/home/anyuser/virtualclient/state/anystate.json", platformSpecifics.GetStatePath("anystate.json"));
            Assert.AreEqual("/home/anyuser/virtualclient/state/other/anystate.json", platformSpecifics.GetStatePath("/other", "anystate.json"));
            Assert.AreEqual("/home/anyuser/virtualclient/state/other/anystate.json", platformSpecifics.GetStatePath("other", "anystate.json"));
        }

        [Test]
        public void GetStatePathReturnsTheExpectedPathOnWindowsSystems()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics2(PlatformID.Win32NT, Architecture.X64, @"C:\users\anyuser\virtualclient");

            Assert.AreEqual(@"C:\users\anyuser\virtualclient\state", platformSpecifics.GetStatePath());
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\state\anystate.json", platformSpecifics.GetStatePath(@"anystate.json"));
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\state\other\anystate.json", platformSpecifics.GetStatePath(@"\other", "anystate.json"));
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\state\other\anystate.json", platformSpecifics.GetStatePath(@"\other", "anystate.json"));
        }

        [Test]
        public void GetToolsPathReturnsTheExpectedPathOnUnixSystems()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics2(PlatformID.Unix, Architecture.X64, "/home/anyuser/virtualclient");

            Assert.AreEqual("/home/anyuser/virtualclient/tools", platformSpecifics.GetToolsPath());
            Assert.AreEqual("/home/anyuser/virtualclient/tools/lshw", platformSpecifics.GetToolsPath("lshw"));
            Assert.AreEqual("/home/anyuser/virtualclient/tools/lshw/linux-x64", platformSpecifics.GetToolsPath("lshw", "linux-x64"));
            Assert.AreEqual("/home/anyuser/virtualclient/tools/lshw/linux-x64/lshw", platformSpecifics.GetToolsPath("lshw", "linux-x64", "lshw"));
        }

        [Test]
        public void GetToolsPathReturnsTheExpectedPathOnWindowsSystems()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics2(PlatformID.Win32NT, Architecture.X64, @"C:\users\anyuser\virtualclient");

            Assert.AreEqual(@"C:\users\anyuser\virtualclient\tools", platformSpecifics.GetToolsPath());
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\tools\systemtools", platformSpecifics.GetToolsPath("systemtools"));
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\tools\systemtools\win-x64", platformSpecifics.GetToolsPath("systemtools", "win-x64"));
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\tools\systemtools\win-x64\coreinfo.exe", platformSpecifics.GetToolsPath("systemtools", "win-x64", "coreinfo.exe"));
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "win-x64")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "win-arm64")]
        [TestCase(PlatformID.Unix, Architecture.X64, "linux-x64")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "linux-arm64")]
        public void GetPlatformArchitectureNameReturnsTheExpectedNameForCombinationsSupported(PlatformID platform, Architecture architecture, string expectedValue)
        {
            string actualValue = PlatformSpecifics.GetPlatformArchitectureName(platform, architecture);
            Assert.AreEqual(expectedValue, actualValue);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, true)]
        [TestCase(PlatformID.Unix, true)]
        [TestCase(PlatformID.Other , false)]
        public void TheListOfSupportedPlatformsMatchesExpected(PlatformID platform, bool isSupported)
        {
            if (isSupported)
            {
                Assert.DoesNotThrow(() => PlatformSpecifics.ThrowIfNotSupported(platform));
            }
            else
            {
                Assert.Throws<NotSupportedException>(() => PlatformSpecifics.ThrowIfNotSupported(platform));
            }
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "PERF-CPU-COREMARK.json", "PERF-CPU-COREMARK (win-x64)")]
        [TestCase(PlatformID.Win32NT, Architecture.Arm64, "PERF-CPU-COREMARK.json", "PERF-CPU-COREMARK (win-arm64)")]
        [TestCase(PlatformID.Unix, Architecture.X64, "PERF-CPU-COREMARK.json", "PERF-CPU-COREMARK (linux-x64)")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "PERF-CPU-COREMARK.json", "PERF-CPU-COREMARK (linux-arm64)")]
        public void GetPlatformNameReturnsTheExpectedPlatformSpecificNameForAllSupportedPlatformsAndArchitectures(PlatformID platform, Architecture architecture, string profile, string expectedName)
        {
            string actualName = PlatformSpecifics.GetProfileName(profile, platform, architecture);
            Assert.AreEqual(expectedName, actualName);
        }

        [Test]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "PERF-CPU-COREMARK", "PERF-CPU-COREMARK (win-x64)")]
        [TestCase(PlatformID.Win32NT, Architecture.X64, "PERF-CPU-COREMARK.json", "PERF-CPU-COREMARK (win-x64)")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "PERF-CPU-COREMARK", "PERF-CPU-COREMARK (linux-arm64)")]
        [TestCase(PlatformID.Unix, Architecture.Arm64, "PERF-CPU-COREMARK.json", "PERF-CPU-COREMARK (linux-arm64)")]
        public void GetPlatformNameHandlesPossibleVariationsInTheProfileNames(PlatformID platform, Architecture architecture, string profile, string expectedName)
        {
            string actualName = PlatformSpecifics.GetProfileName(profile, platform, architecture);
            Assert.AreEqual(expectedName, actualName);
        }

        [Test]
        [TestCase(Architecture.X64, true)]
        [TestCase(Architecture.Arm64, true)]
        [TestCase(Architecture.Arm, false)]
        [TestCase(Architecture.X86, false)]
        public void TheListOfSupportedProcessorArchitecturesMatchesExpected(Architecture architecture, bool isSupported)
        {
            if (isSupported)
            {
                Assert.DoesNotThrow(() => PlatformSpecifics.ThrowIfNotSupported(architecture));
            }
            else
            {
                Assert.Throws<NotSupportedException>(() => PlatformSpecifics.ThrowIfNotSupported(architecture));
            }
        }

        [Test]
        [TestCase(@"\packages", @"\packages")]
        [TestCase(@"/packages", @"\packages")]
        [TestCase(@"C:", "C:")]
        [TestCase(@"C:\packages\", @"C:\packages")]
        [TestCase(@"C:\packages//", @"C:\packages")]
        [TestCase(@"C:/packages\", @"C:\packages")]
        [TestCase(@"C:\packages\", @"C:\packages")]
        public void PlatformStandardizesPathsCorrectlyOnWindowsSystems(string path, string expectedPath)
        {
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64);
            string actualPath = platformSpecifics.StandardizePath(path);
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        [TestCase(@"/", "/")]
        [TestCase(@"/packages/", @"/packages")]
        [TestCase(@"/packages//", @"/packages")]
        [TestCase(@"/packages\", @"/packages")]
        [TestCase(@"\packages\", @"/packages")]
        public void PlatformStandardizesPathsCorrectlyOnLinuxSystems(string path, string expectedPath)
        {
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, Architecture.X64);
            string actualPath = platformSpecifics.StandardizePath(path);
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        [TestCase("/home/any/unix/style/path")]
        [TestCase("/home/any/unix/style/path/")]
        [TestCase(@"/home/any/unix\style\path")]
        [TestCase(@"/home\any\unix/style\path")]
        [TestCase(@"/home\\any\unix//style\path")]
        public void PlatformSpecificsStandardizesPathsCorrectlyWhenUseUnixStylePathsOnlyIsUsed_Unix_Platform(string path)
        {
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, Architecture.X64, useUnixStylePathsOnly: true);

            string expectedPath = "/home/any/unix/style/path";
            string actualPath = platformSpecifics.StandardizePath(path);
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        [TestCase(@"C:\Users\Any\Windows\Style\Path")]
        [TestCase(@"C:\Users\Any\Windows\Style\Path\")]
        [TestCase(@"C:/Users/Any\Windows/Style/Path")]
        [TestCase(@"C:\Users\Any/Windows\Style/Path")]
        [TestCase(@"C:\Users\\Any\Windows//Style\Path")]
        public void PlatformSpecificsStandardizesPathsCorrectlyWhenUseUnixStylePathsOnlyIsUsed_Windows_Platform(string path)
        {
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64, useUnixStylePathsOnly: true);

            string expectedPath = "C:/Users/Any/Windows/Style/Path";
            string actualPath = platformSpecifics.StandardizePath(path);
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        [TestCase(@"/a", "/b", "/c", "/")]
        [TestCase(@"/packages/a", @"/packages/b", @"/packages/c", @"/packages/")]
        [TestCase(@"/packages/a/b/c", @"/packages/a/b", @"/packages/a", @"/packages/")]
        [TestCase(@"/packages/a/b/c/d", @"/packages/a/b/c", @"/packages/a/b", @"/packages/a/")]
        [TestCase(@"/packages2/a/b/c/d", @"/packages/a/b/c", @"/packages/a/b", @"/")]
        [TestCase(@"/vc/packages2/a\b/c/d", @"/vc\packages2/a/b/c", @"\vc/packages/a/b", @"/vc/")]
        public void PlatformGetCommmonDirectoryCorrectlyOnLinuxSystems(string path1, string path2, string path3, string expectedPath)
        {
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, Architecture.X64);
            string actualPath = platformSpecifics.GetCommonDirectory(new string[] { path1, path2, path3 });
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        [TestCase(@"A:\", @"B:\", @"C:\", "")]
        [TestCase(@"A:\a.txt", @"A:\b.pdf", @"A:\c.jpg", @"A:\")]
        [TestCase(@"A:\a\a.txt", @"A:\a\b\b.pdf", @"A:\a\b\c\c.jpg", @"A:\a\")]
        [TestCase(@"A:\a\a.txt", @"A:\a\b\b.pdf", @"AA:\a\b\c\c.jpg", @"")]
        [TestCase(@"A:\a\a.txt", @"B:\a\b.pdf", @"A:\a\c.jpg", @"")]
        public void PlatformGetCommmonDirectoryCorrectlyOnWindowsSystems(string path1, string path2, string path3, string expectedPath)
        {
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64);
            string actualPath = platformSpecifics.GetCommonDirectory(new string[] { path1, path2, path3 });
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        public void PlatformSpecificsJoinsPathSegmentsCorrectlyOnWindowsSystems()
        {
            string[] pathSegments = new string[] { "C:", "any", "path", "on", "the", "system" };
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64);

            string expectedPath = @"C:\any\path\on\the\system";
            string actualPath = platformSpecifics.Combine(pathSegments);

            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        public void PlatformSpecificsJoinsPathSegmentsCorrectlyOnUnixSystems()
        {
            string[] pathSegments = new string[] { "home", "any", "path", "on", "the", "system" };
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, Architecture.X64);

            string expectedPath = @"home/any/path/on/the/system";
            string actualPath = platformSpecifics.Combine(pathSegments);

            Assert.AreEqual(expectedPath, actualPath);
        }


        [Test]
        public void PlatformSpecificsHandlesExtraneousPathDelimitersOnWindowsSystems()
        {
            string[] pathSegments = new string[] { @"C:\", "any", @"\path", @"on\\", @"\the", @"\\\\system" };
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64);

            string expectedPath = @"C:\any\path\on\the\system";
            string actualPath = platformSpecifics.Combine(pathSegments);

            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        public void PlatformSpecificsHandlesMixedPathDelimitersOnWindowsSystems()
        {
            string[] pathSegments = new string[] { @"C:\", "any", @"\path", "//on", @"\the///", "////system" };
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64);

            string expectedPath = @"C:\any\path\on\the\system";
            string actualPath = platformSpecifics.Combine(pathSegments);

            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\r\n")]
        public void PlatformSpecificsHandlesPathSegmentsThatAreNothingButWhitespaceOnWindowsSystems(string emptyPathSegment)
        {
            string[] pathSegments = new string[] { @"C:\", "any", "path", "on", "the", "system", emptyPathSegment };
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64);

            string expectedPath = @"C:\any\path\on\the\system";
            string actualPath = platformSpecifics.Combine(pathSegments);

            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        public void PlatformSpecificsHandlesExtraneousPathDelimitersOnUnixSystems()
        {
            string[] pathSegments = new string[] { "/home/", "any//", "///path", "on", "/the/", "//system" };
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, Architecture.X64);

            string expectedPath = @"/home/any/path/on/the/system";
            string actualPath = platformSpecifics.Combine(pathSegments);

            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        public void PlatformSpecificsHandlesMixedPathDelimitersOnUnixSystems()
        {
            string[] pathSegments = new string[] { "/home/", "any//", "//path", "on", @"\the/", @"\\\\system" };
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, Architecture.X64);

            string expectedPath = "/home/any/path/on/the/system";
            string actualPath = platformSpecifics.Combine(pathSegments);

            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        public void PlatformSpecificsHandlesMixedPathDelimitersOnUnixSystems2()
        {
            string[] pathSegments = new string[] { "/etc", "rc.local" };
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, Architecture.X64);

            string actualPath = platformSpecifics.Combine(pathSegments);
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\n")]
        public void PlatformSpecificsHandlesPathSegmentsThatAreNothingButWhitespaceOnUnixSystems(string emptyPathSegment)
        {
            string[] pathSegments = new string[] { "/home", "any", "path", "on", "the", "system", emptyPathSegment };
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, Architecture.X64);

            string expectedPath = "/home/any/path/on/the/system";
            string actualPath = platformSpecifics.Combine(pathSegments);

            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        public void PlatformSpecificsCreatesTheExpectedPathWhenUseUnixStylePathsOnlyIsUsed_Unix_Platform()
        {
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, Architecture.X64, useUnixStylePathsOnly: true);

            string expectedPath = "/home/any/unix/style/path";
            string actualPath = platformSpecifics.Combine("/home/any/unix", "style/path");
            Assert.AreEqual(expectedPath, actualPath);

            actualPath = platformSpecifics.Combine("/home/any/unix", "style", "path");
            Assert.AreEqual(expectedPath, actualPath);

            actualPath = platformSpecifics.Combine("/home/any/unix/", "style", "path");
            Assert.AreEqual(expectedPath, actualPath);

            actualPath = platformSpecifics.Combine("/home/any/unix/", "/style/path/");
            Assert.AreEqual(expectedPath, actualPath);

            actualPath = platformSpecifics.Combine("/home/any/unix/", @"\style\path");
            Assert.AreEqual(expectedPath, actualPath);

            actualPath = platformSpecifics.Combine(@"/home\any\unix/", @"/style\path");
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        public void PlatformSpecificsCreatesTheExpectedPathWhenUseUnixStylePathsOnlyIsUsed_Windows_Platform()
        {
            PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64, useUnixStylePathsOnly: true);

            string expectedPath = "C:/Users/Any/Unix/Style/Path";
            string actualPath = platformSpecifics.Combine(@"C:\Users\Any\Unix", @"Style\Path");
            Assert.AreEqual(expectedPath, actualPath);

            actualPath = platformSpecifics.Combine(@"C:\Users\Any\Unix", @"Style", "Path");
            Assert.AreEqual(expectedPath, actualPath);

            actualPath = platformSpecifics.Combine("C:/Users/Any/Unix", "Style/Path");
            Assert.AreEqual(expectedPath, actualPath);

            actualPath = platformSpecifics.Combine("C:/Users/Any/Unix", "Style", "Path");
            Assert.AreEqual(expectedPath, actualPath);

            actualPath = platformSpecifics.Combine("C:/Users/Any/Unix/", "Style", "Path");
            Assert.AreEqual(expectedPath, actualPath);

            actualPath = platformSpecifics.Combine("C:/Users/Any/Unix/", "/Style/Path/");
            Assert.AreEqual(expectedPath, actualPath);

            actualPath = platformSpecifics.Combine("C:/Users/Any/Unix/", @"\Style\Path");
            Assert.AreEqual(expectedPath, actualPath);

            actualPath = platformSpecifics.Combine(@"C:/Users\Any\Unix/", @"\Style/Path");
            Assert.AreEqual(expectedPath, actualPath);
        }

        [Test]
        public void PlatformSpecificsSetsEnvironmentVariablesToExpectedValues_WindowsSystems()
        {
            string environmentVariableName = "VC_SYSTEM_MANAGEMENT_TEST_1";
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;

            try
            {
                string expectedValue = Guid.NewGuid().ToString();

                // Set initial value
                Environment.SetEnvironmentVariable(environmentVariableName, "InitialValue", target);

                // Change initial value
                PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64);
                platformSpecifics.SetEnvironmentVariable(environmentVariableName, expectedValue, target);

                string actualValue = Environment.GetEnvironmentVariable(environmentVariableName, target);
                Assert.AreEqual(expectedValue, actualValue);
            }
            finally
            {
                // Remove the environment variable.
                Environment.SetEnvironmentVariable(environmentVariableName, null, target);
            }
        }

        [Test]
        public void PlatformSpecificsAppendsValuesToEnvironmentVariablesAsExpected_WindowsSystems_1()
        {
            string environmentVariableName = "VC_SYSTEM_MANAGEMENT_TEST_2";
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;

            try
            {
                string expectedValue = Guid.NewGuid().ToString();

                // Set initial value
                Environment.SetEnvironmentVariable(environmentVariableName, "InitialValue1;InitialValue2", target);

                // Change initial value
                PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64);
                platformSpecifics.SetEnvironmentVariable(environmentVariableName, expectedValue, target, append: true);

                string actualValue = Environment.GetEnvironmentVariable(environmentVariableName, target);
                Assert.AreEqual($"InitialValue1;InitialValue2;{expectedValue}", actualValue);
            }
            finally
            {
                // Remove the environment variable.
                Environment.SetEnvironmentVariable(environmentVariableName, null, target);
            }
        }

        [Test]
        public void PlatformSpecificsAppendsValuesToEnvironmentVariablesAsExpected_WindowsSystems_2()
        {
            string environmentVariableName = "VC_SYSTEM_MANAGEMENT_TEST_3";
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;

            try
            {
                string expectedValue = Guid.NewGuid().ToString();

                // No value set in the environment variable. The append should work like a direct
                // set in this case.
                Environment.SetEnvironmentVariable(environmentVariableName, "", target);

                // Change initial value
                PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64);
                platformSpecifics.SetEnvironmentVariable(environmentVariableName, expectedValue, target, append: true);

                string actualValue = Environment.GetEnvironmentVariable(environmentVariableName, target);
                Assert.AreEqual(expectedValue, actualValue);
            }
            finally
            {
                // Remove the environment variable.
                Environment.SetEnvironmentVariable(environmentVariableName, null, target);
            }
        }

        [Test]
        public void PlatformSpecificsAppendsValuesToEnvironmentVariablesAsExpected_WindowsSystems_3()
        {
            string environmentVariableName = "VC_SYSTEM_MANAGEMENT_TEST_4";
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;

            try
            {
                string expectedValue = Guid.NewGuid().ToString();

                // The value set is just a delimiter.  The append should work like a direct
                // set in this case. 
                Environment.SetEnvironmentVariable(environmentVariableName, ";", target);

                // Change initial value
                PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64);
                platformSpecifics.SetEnvironmentVariable(environmentVariableName, expectedValue, target, append: true);

                string actualValue = Environment.GetEnvironmentVariable(environmentVariableName, target);
                Assert.AreEqual(expectedValue, actualValue);
            }
            finally
            {
                // Remove the environment variable.
                Environment.SetEnvironmentVariable(environmentVariableName, null, target);
            }
        }

        [Test]
        public void PlatformSpecificsDoesNotAppendDuplicateValuesToEnvironmentVariables_WindowsSystems()
        {
            string environmentVariableName = "VC_SYSTEM_MANAGEMENT_TEST_5";
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;

            try
            {
                string expectedValue = Guid.NewGuid().ToString();

                // 1) Initial value already contains the expected value.
                Environment.SetEnvironmentVariable(environmentVariableName, $"InitialValue1;InitialValue2;{expectedValue}", target);

                // Change initial value
                PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Win32NT, Architecture.X64);
                platformSpecifics.SetEnvironmentVariable(environmentVariableName, expectedValue, target, append: true);

                string actualValue = Environment.GetEnvironmentVariable(environmentVariableName, target);
                Assert.AreEqual($"InitialValue1;InitialValue2;{expectedValue}", actualValue);

                // 2) Initial value already contains the expected value but with a trailing delimiter
                Environment.SetEnvironmentVariable(environmentVariableName, $"InitialValue1;InitialValue2;{expectedValue};", target);
                platformSpecifics.SetEnvironmentVariable(environmentVariableName, expectedValue, target, append: true);

                actualValue = Environment.GetEnvironmentVariable(environmentVariableName, target);
                Assert.AreEqual($"InitialValue1;InitialValue2;{expectedValue};", actualValue);
            }
            finally
            {
                // Remove the environment variable.
                Environment.SetEnvironmentVariable(environmentVariableName, null, target);
            }
        }


        [Test]
        public void PlatformSpecificsSetsEnvironmentVariablesToExpectedValues_UnixSystems()
        {
            string environmentVariableName = "VC_SYSTEM_MANAGEMENT_TEST_UNIX_1";
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;

            try
            {
                string expectedValue = Guid.NewGuid().ToString();

                // Set initial value
                Environment.SetEnvironmentVariable(environmentVariableName, "InitialValue", target);

                // Change initial value
                PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, Architecture.X64);
                platformSpecifics.SetEnvironmentVariable(environmentVariableName, expectedValue, target);

                string actualValue = Environment.GetEnvironmentVariable(environmentVariableName, target);
                Assert.AreEqual(expectedValue, actualValue);
            }
            finally
            {
                // Remove the environment variable.
                Environment.SetEnvironmentVariable(environmentVariableName, null, target);
            }
        }

        [Test]
        public void PlatformSpecificsAppendsValuesToEnvironmentVariablesAsExpected_UnixSystems_1()
        {
            string environmentVariableName = "VC_SYSTEM_MANAGEMENT_TEST_UNIX_2";
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;

            try
            {
                string expectedValue = Guid.NewGuid().ToString();

                // Set initial value
                Environment.SetEnvironmentVariable(environmentVariableName, "InitialValue1:InitialValue2", target);

                // Change initial value
                PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, Architecture.X64);
                platformSpecifics.SetEnvironmentVariable(environmentVariableName, expectedValue, target, append: true);

                string actualValue = Environment.GetEnvironmentVariable(environmentVariableName, target);
                Assert.AreEqual($"InitialValue1:InitialValue2:{expectedValue}", actualValue);
            }
            finally
            {
                // Remove the environment variable.
                Environment.SetEnvironmentVariable(environmentVariableName, null, target);
            }
        }

        [Test]
        public void PlatformSpecificsAppendsValuesToEnvironmentVariablesAsExpected_UnixSystems_2()
        {
            string environmentVariableName = "VC_SYSTEM_MANAGEMENT_TEST_UNIX_3";
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;

            try
            {
                string expectedValue = Guid.NewGuid().ToString();

                // No value set in the environment variable. The append should work like a direct
                // set in this case.
                Environment.SetEnvironmentVariable(environmentVariableName, "", target);

                // Change initial value
                PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, Architecture.X64);
                platformSpecifics.SetEnvironmentVariable(environmentVariableName, expectedValue, target, append: true);

                string actualValue = Environment.GetEnvironmentVariable(environmentVariableName, target);
                Assert.AreEqual(expectedValue, actualValue);
            }
            finally
            {
                // Remove the environment variable.
                Environment.SetEnvironmentVariable(environmentVariableName, null, target);
            }
        }

        [Test]
        public void PlatformSpecificsAppendsValuesToEnvironmentVariablesAsExpected_UnixSystems_3()
        {
            string environmentVariableName = "VC_SYSTEM_MANAGEMENT_TEST_UNIX_4";
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;

            try
            {
                string expectedValue = Guid.NewGuid().ToString();

                // The value set is just a delimiter.  The append should work like a direct
                // set in this case. 
                Environment.SetEnvironmentVariable(environmentVariableName, ":", target);

                // Change initial value
                PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, Architecture.X64);
                platformSpecifics.SetEnvironmentVariable(environmentVariableName, expectedValue, target, append: true);

                string actualValue = Environment.GetEnvironmentVariable(environmentVariableName, target);
                Assert.AreEqual(expectedValue, actualValue);
            }
            finally
            {
                // Remove the environment variable.
                Environment.SetEnvironmentVariable(environmentVariableName, null, target);
            }
        }

        [Test]
        public void PlatformSpecificsDoesNotAppendDuplicateValuesToEnvironmentVariables_UnixSystems()
        {
            string environmentVariableName = "VC_SYSTEM_MANAGEMENT_TEST_UNIX_5";
            EnvironmentVariableTarget target = EnvironmentVariableTarget.Process;

            try
            {
                string expectedValue = Guid.NewGuid().ToString();

                // 1) Initial value already contains the expected value.
                Environment.SetEnvironmentVariable(environmentVariableName, $"InitialValue1:InitialValue2:{expectedValue}", target);

                // Change initial value
                PlatformSpecifics platformSpecifics = new PlatformSpecifics(PlatformID.Unix, Architecture.X64);
                platformSpecifics.SetEnvironmentVariable(environmentVariableName, expectedValue, target, append: true);

                string actualValue = Environment.GetEnvironmentVariable(environmentVariableName, target);
                Assert.AreEqual($"InitialValue1:InitialValue2:{expectedValue}", actualValue);

                // 2) Initial value already contains the expected value but with a trailing delimiter
                Environment.SetEnvironmentVariable(environmentVariableName, $"InitialValue1:InitialValue2:{expectedValue}:", target);
                platformSpecifics.SetEnvironmentVariable(environmentVariableName, expectedValue, target, append: true);

                actualValue = Environment.GetEnvironmentVariable(environmentVariableName, target);
                Assert.AreEqual($"InitialValue1:InitialValue2:{expectedValue}:", actualValue);
            }
            finally
            {
                // Remove the environment variable.
                Environment.SetEnvironmentVariable(environmentVariableName, null, target);
            }
        }

        // Used to expose the ability to define the 'current directory' for the purposes of
        // testing paths (i.e. comparisons of Windows vs. Unix formatted paths).
        private class TestPlatformSpecifics2 : PlatformSpecifics
        {
            public TestPlatformSpecifics2(PlatformID platform, Architecture architecture, string currentDirectory)
                : base(platform, architecture, currentDirectory)
            {
            }
        }
    }
}
