// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Contracts
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
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
        public void GetLoggedInUserReturnsTheExpectedUserOnWindowsSystems()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics(PlatformID.Win32NT, Architecture.X64);
            string user = platformSpecifics.GetLoggedInUser();
            Assert.AreEqual(Environment.UserName, user);

            platformSpecifics.SetEnvironmentVariable(EnvironmentVariable.SUDO_USER, "User01");
        }

        [Test]
        public void GetLoggedInUserReturnsTheExpectedUserOnWindowsSystems_2()
        {
            // Environment variables do not matter on Windows and should not affect
            // the return user.
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics(PlatformID.Win32NT, Architecture.X64);

            platformSpecifics.SetEnvironmentVariable(EnvironmentVariable.SUDO_USER, "User01");
            string user = platformSpecifics.GetLoggedInUser();
            Assert.AreEqual(Environment.UserName, user);

            platformSpecifics.SetEnvironmentVariable(EnvironmentVariable.VC_SUDO_USER, "User02");
            user = platformSpecifics.GetLoggedInUser();
            Assert.AreEqual(Environment.UserName, user);
        }

        [Test]
        public void GetLoggedInUserReturnsTheExpectedUserOnUnixSystems()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics(PlatformID.Unix, Architecture.X64);
            string user = platformSpecifics.GetLoggedInUser();
            Assert.AreEqual(Environment.UserName, user);
        }

        [Test]
        public void GetLoggedInUserReturnsTheExpectedUserOnUnixSystemsWhenSudoIsUsed()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics(PlatformID.Unix, Architecture.X64);

            string sudoUser = "User01";
            platformSpecifics.SetEnvironmentVariable(EnvironmentVariable.SUDO_USER, sudoUser);
            string user = platformSpecifics.GetLoggedInUser();
            Assert.AreEqual(sudoUser, user);
        }

        [Test]
        public void GetLoggedInUserReturnsTheExpectedUserOnUnixSystemsWhenCustomSudoAlternativesAreUsed()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics(PlatformID.Unix, Architecture.X64);

            string sudoUser = "User01";
            platformSpecifics.SetEnvironmentVariable(EnvironmentVariable.VC_SUDO_USER, sudoUser);
            string user = platformSpecifics.GetLoggedInUser();
            Assert.AreEqual(sudoUser, user);
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
        public void GetTempPathReturnsTheExpectedPathOnUnixSystems()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics2(PlatformID.Unix, Architecture.X64, "/home/anyuser/virtualclient");

            Assert.AreEqual("/home/anyuser/virtualclient/temp", platformSpecifics.GetTempPath());
            Assert.AreEqual("/home/anyuser/virtualclient/temp/any-jobfile.fio", platformSpecifics.GetTempPath("any-jobfile.fio"));
            Assert.AreEqual("/home/anyuser/virtualclient/temp/jobfiles/any-jobfile.fio", platformSpecifics.GetTempPath("jobfiles", "any-jobfile.fio"));
        }

        [Test]
        public void GetTempPathReturnsTheExpectedPathOnWindowsSystems()
        {
            PlatformSpecifics platformSpecifics = new TestPlatformSpecifics2(PlatformID.Win32NT, Architecture.X64, @"C:\users\anyuser\virtualclient");

            Assert.AreEqual(@"C:\users\anyuser\virtualclient\temp", platformSpecifics.GetTempPath());
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\temp\any-jobfile.fio", platformSpecifics.GetTempPath("any-jobfile.fio"));
            Assert.AreEqual(@"C:\users\anyuser\virtualclient\temp\jobfiles\any-jobfile.fio", platformSpecifics.GetTempPath("jobfiles", "any-jobfile.fio"));
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
        [Platform(Include ="Win")]
        public void CanResolveRelativePathsCorrectly()
        {
            IDictionary<string, string> paths = new Dictionary<string, string>
            {
                { @"script.exe", @"script.exe" },
                { @"S:\Path\Contains\None", @"S:\Path\Contains\None" },
                { @".\packages", Path.GetFullPath(@".\packages") },
                { @"..\packages", Path.GetFullPath(@"..\packages") },
                { @"..\..\directory\..\packages", Path.GetFullPath(@"..\..\directory\..\packages") },
                { @".\scripts\script.exe", Path.GetFullPath(@".\scripts\script.exe") },
                { @".\scripts\script.exe --any=option --flag", $"{Path.GetFullPath(@".\scripts\script.exe")} --any=option --flag" },
                { @""".\scripts\script.exe --any=option --flag""", $"\"{Path.GetFullPath(@".\scripts\script.exe")} --any=option --flag\"" },
                { @""".\scripts\script.exe"" --any=option --flag", $"\"{Path.GetFullPath(@".\scripts\script.exe")}\" --any=option --flag" }
            };

            foreach (var entry in paths)
            {
                string relativePath = entry.Key;
                string expectedPath = entry.Value;
                string actualPath = PlatformSpecifics.ResolveRelativePaths(relativePath);

                Assert.AreEqual(expectedPath, actualPath, $"Relative path did not resolve correctly: '{relativePath}'");
            }
        }

        [Test]
        [Platform(Include = "Win")]
        public void CanResolveRelativePathsCorrectly_More_Advanced_Cases()
        {
            IDictionary<string, string> paths = new Dictionary<string, string>
            {
                { 
                    $@"cmd -c "".\scripts\script.exe --any=option --flag""", 
                    $@"cmd -c ""{Path.GetFullPath(@".\scripts\script.exe")} --any=option --flag""" 
                },
                {
                    $@"cmd -c ""'.\scripts\script.exe' --any=option --flag""",
                    $@"cmd -c ""'{Path.GetFullPath(@".\scripts\script.exe")}' --any=option --flag"""
                },
                {
                    $@"..\..\any\path\anycommand.exe --this=true --that=123 --log-path=.\another\path.log ..\and\one\other.thing .\", 
                    $@"{Path.GetFullPath(@"..\..\any\path\anycommand.exe")} --this=true --that=123 --log-path={Path.GetFullPath(@".\another\path.log")} {Path.GetFullPath(@"..\and\one\other.thing")} {Path.GetFullPath(@".\")}" 
                },
                {
                    $@"..\..\any\path\anycommand.exe --who=..\..\Any\Path\AnyCommand.exe",
                    $@"{Path.GetFullPath(@"..\..\any\path\anycommand.exe")} --who={Path.GetFullPath(@"..\..\Any\Path\AnyCommand.exe")}"
                }
            };

            foreach (var entry in paths)
            {
                string relativePath = entry.Key;
                string expectedPath = entry.Value;
                string actualPath = PlatformSpecifics.ResolveRelativePaths(relativePath);

                Assert.AreEqual(expectedPath, actualPath, $"Relative path did not resolve correctly: '{relativePath}'");
            }
        }

        [Test]
        [TestCase("anycommand", "anycommand", null)]
        [TestCase("anycommand  ", "anycommand", null)]
        [TestCase("./anycommand", "./anycommand", null)]
        [TestCase("./anycommand --argument=value", "./anycommand", "--argument=value")]
        [TestCase("./anycommand --argument=value --argument2 value2", "./anycommand", "--argument=value --argument2 value2")]
        [TestCase("./anycommand --argument=value --argument2 value2 --flag", "./anycommand", "--argument=value --argument2 value2 --flag")]
        [TestCase("./anycommand --argument=value --argument2 value2 --flag   ", "./anycommand", "--argument=value --argument2 value2 --flag")]
        [TestCase("../../anycommand --argument=value --argument2 value2 --flag   ", "../../anycommand", "--argument=value --argument2 value2 --flag")]
        [TestCase("/home/user/anycommand", "/home/user/anycommand", null)]
        [TestCase("/home/user/anycommand --argument=value --argument2 value2", "/home/user/anycommand", "--argument=value --argument2 value2")]
        [TestCase("\"/home/user/anycommand\"", "\"/home/user/anycommand\"", null)]
        [TestCase("\"/home/user/dir with space/anycommand\" --argument=value --argument2 value2", "\"/home/user/dir with space/anycommand\"", "--argument=value --argument2 value2")]
        [TestCase("sudo anycommand", "sudo", "anycommand")]
        [TestCase("sudo ./anycommand", "sudo", "./anycommand")]
        [TestCase("sudo /home/user/anycommand", "sudo", "/home/user/anycommand")]
        [TestCase("sudo /home/user/anycommand --argument=value --argument2 value2", "sudo", "/home/user/anycommand --argument=value --argument2 value2")]
        [TestCase("sudo \"/home/user/dir with space/anycommand\"", "sudo", "\"/home/user/dir with space/anycommand\"")]
        [TestCase("sudo \"/home/user/dir with space/anycommand\" --argument=value --argument2 value2", "sudo", "\"/home/user/dir with space/anycommand\" --argument=value --argument2 value2")]
        public void CorrectlyIdentifiesThePartsOfTheCommandOnUnixSystems(string fullCommand, string expectedCommand, string expectedCommandArguments)
        {
            Assert.IsTrue(PlatformSpecifics.TryGetCommandParts(fullCommand, out string actualCommand, out string actualCommandArguments));
            Assert.AreEqual(expectedCommand, actualCommand);
            Assert.AreEqual(expectedCommandArguments, actualCommandArguments);
        }

        [Test]
        [TestCase("bash -c \"/home/user/anyscript.sh\"", "bash", "-c \"/home/user/anyscript.sh\"")]
        [TestCase("bash -c \"/home/user/anyscript.sh --argument=value --argument2 value2\"", "bash", "-c \"/home/user/anyscript.sh --argument=value --argument2 value2\"")]
        [TestCase("bash -c \"/home/dir with space/anyscript.sh\"", "bash", "-c \"/home/dir with space/anyscript.sh\"")]
        [TestCase("sudo bash -c \"/home/user/anyscript.sh --argument=value --argument2 value2\"", "sudo", "bash -c \"/home/user/anyscript.sh --argument=value --argument2 value2\"")]
        public void CorrectlyIdentifiesThePartsOfBashCommandsOnUnixSystems(string fullCommand, string expectedCommand, string expectedCommandArguments)
        {
            Assert.IsTrue(PlatformSpecifics.TryGetCommandParts(fullCommand, out string actualCommand, out string actualCommandArguments));
            Assert.AreEqual(expectedCommand, actualCommand);
            Assert.AreEqual(expectedCommandArguments, actualCommandArguments);
        }

        [Test]
        [TestCase("anycommand.exe", "anycommand.exe", null)]
        [TestCase("anycommand.exe  ", "anycommand.exe", null)]
        [TestCase(".\\anycommand.exe", ".\\anycommand.exe", null)]
        [TestCase(".\\anycommand.exe --argument=value --argument2 value2", ".\\anycommand.exe", "--argument=value --argument2 value2")]
        [TestCase(".\\anycommand.exe --argument=value --argument2 value2 --flag", ".\\anycommand.exe", "--argument=value --argument2 value2 --flag")]
        [TestCase(".\\anycommand.exe --argument=value --argument2 value2 --flag   ", ".\\anycommand.exe", "--argument=value --argument2 value2 --flag")]
        [TestCase(".\\anycommand.exe --argument=value", ".\\anycommand.exe", "--argument=value")]
        [TestCase("..\\..\\anycommand.exe --argument=value", "..\\..\\anycommand.exe", "--argument=value")]
        [TestCase("C:\\Users\\User\\anycommand.exe", "C:\\Users\\User\\anycommand.exe", null)]
        [TestCase("C:\\Users\\User\\anycommand.exe --argument=value --argument2 value2", "C:\\Users\\User\\anycommand.exe", "--argument=value --argument2 value2")]
        [TestCase("\"C:\\Users\\User\\Dir With Space\\anycommand.exe\"--argument=value --argument2 value2", "\"C:\\Users\\User\\Dir With Space\\anycommand.exe\"", "--argument=value --argument2 value2")]
        public void CorrectlyIdentifiesThePartsOfTheCommandOnWindowsSystems(string fullCommand, string expectedCommand, string expectedCommandArguments)
        {
            Assert.IsTrue(PlatformSpecifics.TryGetCommandParts(fullCommand, out string actualCommand, out string actualCommandArguments));
            Assert.AreEqual(expectedCommand, actualCommand);
            Assert.AreEqual(expectedCommandArguments, actualCommandArguments);
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
