// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient.Cleanup
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using VirtualClient.Common.Contracts;
    using VirtualClient.Contracts;

    [TestFixture]
    [Category("Unit")]
    public class CleanupExtensionsTests
    {
        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task CleanLogsDirectoryExtensionDeletesAllFilesAndDirectoriesInTheLogsFolderWindows()
        {
            DependencyFixture fixture = new DependencyFixture(PlatformID.Win32NT);
            string logsDirectory = fixture.GetLogsPath();

            // Scenario:
            // All of the directories and subdirectories should be deleted.
            //
            // e.g. all of these get deleted.
            // ../logs/directory1/fileA.log
            // ../logs/directory1/subdirectory1_1/file1.log
            // ../logs/directory1/subdirectory1_2/file2.log
            // ../logs/directory2/fileB.log
            // ../logs/directory2/subdirectory2_1/file3.log
            // ../logs/directory2/subdirectory2_2/file4.log

            List<string> filePaths = new List<string>
            {
                fixture.Combine(logsDirectory, "directory1", "fileA.log"),
                fixture.Combine(logsDirectory, "directory1", "subdirectory1_1", "file1.log"),
                fixture.Combine(logsDirectory, "directory1", "subdirectory1_2", "file2.log"),
                fixture.Combine(logsDirectory, "directory2", "fileB.log"),
                fixture.Combine(logsDirectory, "directory2", "subdirectory2_1", "file3.log"),
                fixture.Combine(logsDirectory, "directory2", "subdirectory2_2", "file4.log"),
            };

            filePaths.ForEach(file => fixture.FileSystem.File.Create(file));

            await fixture.SystemManagement.Object.CleanLogsDirectoryAsync(CancellationToken.None);

            Assert.IsFalse(fixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(file.Path)));
            Assert.IsFalse(fixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(MockFixture.GetDirectoryName(file.Path))));
        }

        [Test]
        public async Task CleanLogsDirectoryExtensionDeletesAllFilesAndDirectoriesInTheLogsFolderLinux()
        {
            DependencyFixture fixture = new DependencyFixture(PlatformID.Unix);
            string logsDirectory = fixture.GetLogsPath();

            // Scenario:
            // All of the directories and subdirectories should be deleted.
            //
            // e.g. all of these get deleted.
            // ../logs/directory1/fileA.log
            // ../logs/directory1/subdirectory1_1/file1.log
            // ../logs/directory1/subdirectory1_2/file2.log
            // ../logs/directory2/fileB.log
            // ../logs/directory2/subdirectory2_1/file3.log
            // ../logs/directory2/subdirectory2_2/file4.log

            List<string> filePaths = new List<string>
            {
                fixture.Combine(logsDirectory, "directory1", "fileA.log"),
                fixture.Combine(logsDirectory, "directory1", "subdirectory1_1", "file1.log"),
                fixture.Combine(logsDirectory, "directory1", "subdirectory1_2", "file2.log"),
                fixture.Combine(logsDirectory, "directory2", "fileB.log"),
                fixture.Combine(logsDirectory, "directory2", "subdirectory2_1", "file3.log"),
                fixture.Combine(logsDirectory, "directory2", "subdirectory2_2", "file4.log"),
            };

            filePaths.ForEach(file => fixture.FileSystem.File.Create(file));

            await fixture.SystemManagement.Object.CleanLogsDirectoryAsync(CancellationToken.None);

            Assert.IsFalse(fixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(file.Path)));
            Assert.IsFalse(fixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(MockFixture.GetDirectoryName(file.Path))));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task CleanLogsDirectoryExtensionDoesNotRemoveFilesOrDirectoriesOutsideOfTheLogsDirectoryWindows()
        {
            DependencyFixture fixture = new DependencyFixture(PlatformID.Win32NT);
            string logsDirectory = fixture.GetLogsPath();
            string otherDirectory = "any-directory";

            // Scenario:
            // Only the 'logs' folder files and directories should get removed. No other directories
            // outside of this folder should be touched.

            List<string> filePaths = new List<string>
            {
                // Should be removed.
                fixture.Combine(logsDirectory, "directory1", "fileA.log"),
                fixture.Combine(logsDirectory, "directory1", "subdirectory1_1", "fileB.log"),

                // Should not be touched
                fixture.Combine(otherDirectory, "directory2", "fileC.log"),
                fixture.Combine(otherDirectory, "directory2", "subdirectory1_1", "fileD.log"),
            };

            filePaths.ForEach(file => fixture.FileSystem.File.Create(file));

            await fixture.SystemManagement.Object.CleanLogsDirectoryAsync(CancellationToken.None);

            Assert.IsTrue(fixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(2)));
            Assert.IsTrue(fixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(3)));
        }

        [Test]
        public async Task CleanLogsDirectoryExtensionDoesNotRemoveFilesOrDirectoriesOutsideOfTheLogsDirectoryLinux()
        {
            DependencyFixture fixture = new DependencyFixture(PlatformID.Unix);
            string logsDirectory = fixture.GetLogsPath();
            string otherDirectory = "any-directory";

            // Scenario:
            // Only the 'logs' folder files and directories should get removed. No other directories
            // outside of this folder should be touched.

            List<string> filePaths = new List<string>
            {
                // Should be removed.
                fixture.Combine(logsDirectory, "directory1", "fileA.log"),
                fixture.Combine(logsDirectory, "directory1", "subdirectory1_1", "fileB.log"),

                // Should not be touched
                fixture.Combine(otherDirectory, "directory2", "fileC.log"),
                fixture.Combine(otherDirectory, "directory2", "subdirectory1_1", "fileD.log"),
            };

            filePaths.ForEach(file => fixture.FileSystem.File.Create(file));

            await fixture.SystemManagement.Object.CleanLogsDirectoryAsync(CancellationToken.None);

            Assert.IsTrue(fixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(2)));
            Assert.IsTrue(fixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(3)));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task CleanStateDirectoryExtensionDeletesAllFilesAndDirectoriesInTheStateFolderWindows()
        {
            DependencyFixture fixture = new DependencyFixture(PlatformID.Win32NT);
            string stateDirectory = fixture.GetStatePath();

            // Scenario:
            // All of the directories and subdirectories should be deleted.
            //
            // e.g. all of these get deleted.
            // ../state/state1.json
            // ../state/directory1/state2.json
            // ../state/directory2/state3.json
            // ../state/directory2/subdirectory2_1/state4.json

            List<string> filePaths = new List<string>
            {
                fixture.Combine(stateDirectory, "state1.json"),
                fixture.Combine(stateDirectory, "directory1", "state2.json"),
                fixture.Combine(stateDirectory, "directory2", "state3.json"),
                fixture.Combine(stateDirectory, "directory2", "subdirectory2_1", "state4.json"),
            };

            filePaths.ForEach(file => fixture.FileSystem.File.Create(file));

            await fixture.SystemManagement.Object.CleanStateDirectoryAsync(CancellationToken.None);

            Assert.IsFalse(fixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(file.Path)));
            Assert.IsFalse(fixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(MockFixture.GetDirectoryName(file.Path))));
        }

        [Test]
        public async Task CleanStateDirectoryExtensionDeletesAllFilesAndDirectoriesInTheStateFolderLinux()
        {
            DependencyFixture fixture = new DependencyFixture(PlatformID.Unix);
            string stateDirectory = fixture.GetStatePath();

            // Scenario:
            // All of the directories and subdirectories should be deleted.
            //
            // e.g. all of these get deleted.
            // ../state/state1.json
            // ../state/directory1/state2.json
            // ../state/directory2/state3.json
            // ../state/directory2/subdirectory2_1/state4.json

            List<string> filePaths = new List<string>
            {
                fixture.Combine(stateDirectory, "state1.json"),
                fixture.Combine(stateDirectory, "directory1", "state2.json"),
                fixture.Combine(stateDirectory, "directory2", "state3.json"),
                fixture.Combine(stateDirectory, "directory2", "subdirectory2_1", "state4.json"),
            };

            filePaths.ForEach(file => fixture.FileSystem.File.Create(file));

            await fixture.SystemManagement.Object.CleanStateDirectoryAsync(CancellationToken.None);

            Assert.IsFalse(fixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(file.Path)));
            Assert.IsFalse(fixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(MockFixture.GetDirectoryName(file.Path))));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task CleanStateDirectoryExtensionDoesNotRemoveFilesOrDirectoriesOutsideOfTheStateDirectoryWindows()
        {
            DependencyFixture fixture = new DependencyFixture(PlatformID.Win32NT);
            string stateDirectory = fixture.GetStatePath();
            string otherDirectory = "any-directory";

            // Scenario:
            // Only the 'state' folder files and directories should get removed. No other directories
            // outside of this folder should be touched.

            List<string> filePaths = new List<string>
            {
                // Should be removed.
                fixture.Combine(stateDirectory, "directory1", "fileA.log"),
                fixture.Combine(stateDirectory, "directory1", "subdirectory1_1", "fileB.log"),

                // Should not be touched
                fixture.Combine(otherDirectory, "directory2", "fileC.log"),
                fixture.Combine(otherDirectory, "directory2", "subdirectory1_1", "fileD.log"),
            };

            filePaths.ForEach(file => fixture.FileSystem.File.Create(file));

            await fixture.SystemManagement.Object.CleanStateDirectoryAsync(CancellationToken.None);

            Assert.IsTrue(fixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(2)));
            Assert.IsTrue(fixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(3)));
        }

        [Test]
        public async Task CleanStateDirectoryExtensionDoesNotRemoveFilesOrDirectoriesOutsideOfTheStateDirectoryLinux()
        {
            DependencyFixture fixture = new DependencyFixture(PlatformID.Unix);
            string stateDirectory = fixture.GetStatePath();
            string otherDirectory = "any-directory";

            // Scenario:
            // Only the 'state' folder files and directories should get removed. No other directories
            // outside of this folder should be touched.

            List<string> filePaths = new List<string>
            {
                // Should be removed.
                fixture.Combine(stateDirectory, "directory1", "fileA.log"),
                fixture.Combine(stateDirectory, "directory1", "subdirectory1_1", "fileB.log"),

                // Should not be touched
                fixture.Combine(otherDirectory, "directory2", "fileC.log"),
                fixture.Combine(otherDirectory, "directory2", "subdirectory1_1", "fileD.log"),
            };

            filePaths.ForEach(file => fixture.FileSystem.File.Create(file));

            await fixture.SystemManagement.Object.CleanStateDirectoryAsync(CancellationToken.None);

            Assert.IsTrue(fixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(2)));
            Assert.IsTrue(fixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(3)));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task CleanPackagesDirectoryExtensionDeletesExpectedPackageFilesAndDirectoriesInThePackagesFolderWindows()
        {
            DependencyFixture fixture = new DependencyFixture(PlatformID.Win32NT);
            string packagesDirectory = fixture.GetPackagePath();

            // Scenario:
            // All of the package directories and subdirectories should be deleted.
            //
            // e.g. all of these get deleted.
            // ../packages/package1.vcpkgreg
            // ../packages/package1/package1.vcpkg
            // ../packages/package1/linux-x64/workload1.sh
            // ../packages/package1/linux-arm64/workload1.sh
            // ../packages/package1/win-x64/workload1.exe
            // ../packages/package1/win-arm64/workload1.exe

            var package1Definition = new DependencyMetadata("package1");
            var package1Registration = new DependencyPath("package1", fixture.Combine(packagesDirectory, "package1"));

            var package2Definition = new DependencyMetadata("package2");
            var package2Registration = new DependencyPath("package2", fixture.Combine(packagesDirectory, "package2"));

            List<string> filePaths = new List<string>
            {
                fixture.Combine(packagesDirectory, "package1.vcpkgreg"),
                fixture.Combine(package1Registration.Path, "package1.vcpkg"),
                fixture.Combine(package1Registration.Path, "linux-x64", "workload1.sh"),
                fixture.Combine(package1Registration.Path, "linux-arm64", "workload1.sh"),
                fixture.Combine(package1Registration.Path, "win-x64", "workload1.exe"),
                fixture.Combine(package1Registration.Path, "win-arm64", "workload1.exe"),
                fixture.Combine(packagesDirectory, "package2.vcpkgreg"),
                fixture.Combine(package2Registration.Path, "package2.vcpkg"),
                fixture.Combine(package2Registration.Path, "linux-x64", "workload2.sh"),
                fixture.Combine(package2Registration.Path, "linux-arm64", "workload2.sh"),
                fixture.Combine(package2Registration.Path, "win-x64", "workload2.exe"),
                fixture.Combine(package2Registration.Path, "win-arm64", "workload2.exe"),
            };

            filePaths.ForEach(file =>
            {
                fixture.FileSystem.File.Create(file);

                InMemoryFile targetFile = fixture.FileSystem.FileSystemEntries.First(e => e.Path == file) as InMemoryFile;

                if (file.EndsWith("package1.vcpkg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package1Definition.ToJson()));
                }
                else if (file.EndsWith("package1.vcpkgreg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package1Registration.ToJson()));
                }
                else if (file.EndsWith("package2.vcpkg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package2Definition.ToJson()));
                }
                else if (file.EndsWith("package2.vcpkgreg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package2Registration.ToJson()));
                }
            });

            await fixture.SystemManagement.Object.CleanPackagesDirectoryAsync(CancellationToken.None);

            Assert.IsFalse(fixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(file.Path)));
            Assert.IsFalse(fixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(MockFixture.GetDirectoryName(file.Path))));
        }

        [Test]
        public async Task CleanPackagesDirectoryExtensionDeletesExpectedPackageFilesAndDirectoriesInThePackagesFolderLinux()
        {
            DependencyFixture fixture = new DependencyFixture(PlatformID.Unix);
            string packagesDirectory = fixture.GetPackagePath();

            // Scenario:
            // All of the package directories and subdirectories should be deleted.
            //
            // e.g. all of these get deleted.
            // ../packages/package1.vcpkgreg
            // ../packages/package1/package1.vcpkg
            // ../packages/package1/linux-x64/workload1.sh
            // ../packages/package1/linux-arm64/workload1.sh
            // ../packages/package1/win-x64/workload1.exe
            // ../packages/package1/win-arm64/workload1.exe

            var package1Definition = new DependencyMetadata("package1");
            var package1Registration = new DependencyPath("package1", fixture.Combine(packagesDirectory, "package1"));

            var package2Definition = new DependencyMetadata("package2");
            var package2Registration = new DependencyPath("package2", fixture.Combine(packagesDirectory, "package2"));

            List<string> filePaths = new List<string>
            {
                fixture.Combine(packagesDirectory, "package1.vcpkgreg"),
                fixture.Combine(package1Registration.Path, "package1.vcpkg"),
                fixture.Combine(package1Registration.Path, "linux-x64", "workload1.sh"),
                fixture.Combine(package1Registration.Path, "linux-arm64", "workload1.sh"),
                fixture.Combine(package1Registration.Path, "win-x64", "workload1.exe"),
                fixture.Combine(package1Registration.Path, "win-arm64", "workload1.exe"),
                fixture.Combine(packagesDirectory, "package2.vcpkgreg"),
                fixture.Combine(package2Registration.Path, "package2.vcpkg"),
                fixture.Combine(package2Registration.Path, "linux-x64", "workload2.sh"),
                fixture.Combine(package2Registration.Path, "linux-arm64", "workload2.sh"),
                fixture.Combine(package2Registration.Path, "win-x64", "workload2.exe"),
                fixture.Combine(package2Registration.Path, "win-arm64", "workload2.exe"),
            };

            filePaths.ForEach(file =>
            {
                fixture.FileSystem.File.Create(file);

                InMemoryFile targetFile = fixture.FileSystem.FileSystemEntries.First(e => e.Path == file) as InMemoryFile;

                if (file.EndsWith("package1.vcpkg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package1Definition.ToJson()));
                }
                else if (file.EndsWith("package1.vcpkgreg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package1Registration.ToJson()));
                }
                else if (file.EndsWith("package2.vcpkg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package2Definition.ToJson()));
                }
                else if (file.EndsWith("package2.vcpkgreg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package2Registration.ToJson()));
                }
            });

            await fixture.SystemManagement.Object.CleanPackagesDirectoryAsync(CancellationToken.None);

            Assert.IsFalse(fixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(file.Path)));
            Assert.IsFalse(fixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(MockFixture.GetDirectoryName(file.Path))));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task CleanPackagesDirectoryExtensionDoesNotRemoveFilesOrDirectoriesOutsideOfThePackagesDirectoryWindows()
        {
            DependencyFixture fixture = new DependencyFixture(PlatformID.Win32NT);
            string packagesDirectory = fixture.GetPackagePath();
            string otherDirectory = "any-directory";

            // Scenario:
            // Only the 'packages' folder files and directories should get removed. No other directories
            // outside of this folder should be touched.

            var package1Definition = new DependencyMetadata("package1");
            var package1Registration = new DependencyPath("package1", fixture.Combine(packagesDirectory, "package1"));

            List<string> filePaths = new List<string>
            {
                // Should be removed.
                fixture.Combine(packagesDirectory, "package1.vcpkgreg"),
                fixture.Combine(package1Registration.Path, "package1.vcpkg"),
                fixture.Combine(package1Registration.Path, "linux-x64", "workload1.sh"),
                fixture.Combine(package1Registration.Path, "linux-arm64", "workload1.sh"),
                fixture.Combine(package1Registration.Path, "win-x64", "workload1.exe"),
                fixture.Combine(package1Registration.Path, "win-arm64", "workload1.exe"),

                // Should not be touched
                fixture.Combine(otherDirectory, "directory2", "fileC.log"),
                fixture.Combine(otherDirectory, "directory2", "subdirectory1_1", "fileD.log"),
            };

            filePaths.ForEach(file =>
            {
                fixture.FileSystem.File.Create(file);

                InMemoryFile targetFile = fixture.FileSystem.FileSystemEntries.First(e => e.Path == file) as InMemoryFile;

                if (file.EndsWith("package1.vcpkg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package1Definition.ToJson()));
                }
                else if (file.EndsWith("package1.vcpkgreg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package1Registration.ToJson()));
                }
            });

            await fixture.SystemManagement.Object.CleanPackagesDirectoryAsync(CancellationToken.None);

            Assert.IsTrue(fixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(6)));
            Assert.IsTrue(fixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(7)));
        }

        [Test]
        public async Task CleanPackagesDirectoryExtensionDoesNotRemoveFilesOrDirectoriesOutsideOfThePackagesDirectoryLinux()
        {
            DependencyFixture fixture = new DependencyFixture(PlatformID.Unix);
            string packagesDirectory = fixture.GetPackagePath();
            string otherDirectory = "any-directory";

            // Scenario:
            // Only the 'packages' folder files and directories should get removed. No other directories
            // outside of this folder should be touched.

            var package1Definition = new DependencyMetadata("package1");
            var package1Registration = new DependencyPath("package1", fixture.Combine(packagesDirectory, "package1"));

            List<string> filePaths = new List<string>
            {
                // Should be removed.
                fixture.Combine(packagesDirectory, "package1.vcpkgreg"),
                fixture.Combine(package1Registration.Path, "package1.vcpkg"),
                fixture.Combine(package1Registration.Path, "linux-x64", "workload1.sh"),
                fixture.Combine(package1Registration.Path, "linux-arm64", "workload1.sh"),
                fixture.Combine(package1Registration.Path, "win-x64", "workload1.exe"),
                fixture.Combine(package1Registration.Path, "win-arm64", "workload1.exe"),

                // Should not be touched
                fixture.Combine(otherDirectory, "directory2", "fileC.log"),
                fixture.Combine(otherDirectory, "directory2", "subdirectory1_1", "fileD.log"),
            };

            filePaths.ForEach(file =>
            {
                fixture.FileSystem.File.Create(file);

                InMemoryFile targetFile = fixture.FileSystem.FileSystemEntries.First(e => e.Path == file) as InMemoryFile;

                if (file.EndsWith("package1.vcpkg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package1Definition.ToJson()));
                }
                else if (file.EndsWith("package1.vcpkgreg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package1Registration.ToJson()));
                }
            });

            await fixture.SystemManagement.Object.CleanPackagesDirectoryAsync(CancellationToken.None);

            Assert.IsTrue(fixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(6)));
            Assert.IsTrue(fixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(7)));
        }
    }
}
