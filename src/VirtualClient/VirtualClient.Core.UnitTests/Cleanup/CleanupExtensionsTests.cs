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
            DependencyFixture mockFixture = new DependencyFixture(PlatformID.Win32NT);
            string logsDirectory = mockFixture.GetLogsPath();

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
                mockFixture.Combine(logsDirectory, "directory1", "fileA.log"),
                mockFixture.Combine(logsDirectory, "directory1", "subdirectory1_1", "file1.log"),
                mockFixture.Combine(logsDirectory, "directory1", "subdirectory1_2", "file2.log"),
                mockFixture.Combine(logsDirectory, "directory2", "fileB.log"),
                mockFixture.Combine(logsDirectory, "directory2", "subdirectory2_1", "file3.log"),
                mockFixture.Combine(logsDirectory, "directory2", "subdirectory2_2", "file4.log"),
            };

            filePaths.ForEach(file => mockFixture.FileSystem.File.Create(file));

            await mockFixture.SystemManagement.Object.CleanLogsDirectoryAsync(CancellationToken.None);

            Assert.IsFalse(mockFixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(file.Path)));
            Assert.IsFalse(mockFixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(MockFixture.GetDirectoryName(file.Path))));
        }

        [Test]
        public async Task CleanLogsDirectoryExtensionDeletesAllFilesAndDirectoriesInTheLogsFolderLinux()
        {
            DependencyFixture mockFixture = new DependencyFixture(PlatformID.Unix);
            string logsDirectory = mockFixture.GetLogsPath();

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
                mockFixture.Combine(logsDirectory, "directory1", "fileA.log"),
                mockFixture.Combine(logsDirectory, "directory1", "subdirectory1_1", "file1.log"),
                mockFixture.Combine(logsDirectory, "directory1", "subdirectory1_2", "file2.log"),
                mockFixture.Combine(logsDirectory, "directory2", "fileB.log"),
                mockFixture.Combine(logsDirectory, "directory2", "subdirectory2_1", "file3.log"),
                mockFixture.Combine(logsDirectory, "directory2", "subdirectory2_2", "file4.log"),
            };

            filePaths.ForEach(file => mockFixture.FileSystem.File.Create(file));

            await mockFixture.SystemManagement.Object.CleanLogsDirectoryAsync(CancellationToken.None);

            Assert.IsFalse(mockFixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(file.Path)));
            Assert.IsFalse(mockFixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(MockFixture.GetDirectoryName(file.Path))));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task CleanLogsDirectoryExtensionDoesNotRemoveFilesOrDirectoriesOutsideOfTheLogsDirectoryWindows()
        {
            DependencyFixture mockFixture = new DependencyFixture(PlatformID.Win32NT);
            string logsDirectory = mockFixture.GetLogsPath();
            string otherDirectory = "any-directory";

            // Scenario:
            // Only the 'logs' folder files and directories should get removed. No other directories
            // outside of this folder should be touched.

            List<string> filePaths = new List<string>
            {
                // Should be removed.
                mockFixture.Combine(logsDirectory, "directory1", "fileA.log"),
                mockFixture.Combine(logsDirectory, "directory1", "subdirectory1_1", "fileB.log"),

                // Should not be touched
                mockFixture.Combine(otherDirectory, "directory2", "fileC.log"),
                mockFixture.Combine(otherDirectory, "directory2", "subdirectory1_1", "fileD.log"),
            };

            filePaths.ForEach(file => mockFixture.FileSystem.File.Create(file));

            await mockFixture.SystemManagement.Object.CleanLogsDirectoryAsync(CancellationToken.None);

            Assert.IsTrue(mockFixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(2)));
            Assert.IsTrue(mockFixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(3)));
        }

        [Test]
        public async Task CleanLogsDirectoryExtensionDoesNotRemoveFilesOrDirectoriesOutsideOfTheLogsDirectoryLinux()
        {
            DependencyFixture mockFixture = new DependencyFixture(PlatformID.Unix);
            string logsDirectory = mockFixture.GetLogsPath();
            string otherDirectory = "any-directory";

            // Scenario:
            // Only the 'logs' folder files and directories should get removed. No other directories
            // outside of this folder should be touched.

            List<string> filePaths = new List<string>
            {
                // Should be removed.
                mockFixture.Combine(logsDirectory, "directory1", "fileA.log"),
                mockFixture.Combine(logsDirectory, "directory1", "subdirectory1_1", "fileB.log"),

                // Should not be touched
                mockFixture.Combine(otherDirectory, "directory2", "fileC.log"),
                mockFixture.Combine(otherDirectory, "directory2", "subdirectory1_1", "fileD.log"),
            };

            filePaths.ForEach(file => mockFixture.FileSystem.File.Create(file));

            await mockFixture.SystemManagement.Object.CleanLogsDirectoryAsync(CancellationToken.None);

            Assert.IsTrue(mockFixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(2)));
            Assert.IsTrue(mockFixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(3)));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task CleanStateDirectoryExtensionDeletesAllFilesAndDirectoriesInTheStateFolderWindows()
        {
            DependencyFixture mockFixture = new DependencyFixture(PlatformID.Win32NT);
            string stateDirectory = mockFixture.GetStatePath();

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
                mockFixture.Combine(stateDirectory, "state1.json"),
                mockFixture.Combine(stateDirectory, "directory1", "state2.json"),
                mockFixture.Combine(stateDirectory, "directory2", "state3.json"),
                mockFixture.Combine(stateDirectory, "directory2", "subdirectory2_1", "state4.json"),
            };

            filePaths.ForEach(file => mockFixture.FileSystem.File.Create(file));

            await mockFixture.SystemManagement.Object.CleanStateDirectoryAsync(CancellationToken.None);

            Assert.IsFalse(mockFixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(file.Path)));
            Assert.IsFalse(mockFixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(MockFixture.GetDirectoryName(file.Path))));
        }

        [Test]
        public async Task CleanStateDirectoryExtensionDeletesAllFilesAndDirectoriesInTheStateFolderLinux()
        {
            DependencyFixture mockFixture = new DependencyFixture(PlatformID.Unix);
            string stateDirectory = mockFixture.GetStatePath();

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
                mockFixture.Combine(stateDirectory, "state1.json"),
                mockFixture.Combine(stateDirectory, "directory1", "state2.json"),
                mockFixture.Combine(stateDirectory, "directory2", "state3.json"),
                mockFixture.Combine(stateDirectory, "directory2", "subdirectory2_1", "state4.json"),
            };

            filePaths.ForEach(file => mockFixture.FileSystem.File.Create(file));

            await mockFixture.SystemManagement.Object.CleanStateDirectoryAsync(CancellationToken.None);

            Assert.IsFalse(mockFixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(file.Path)));
            Assert.IsFalse(mockFixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(MockFixture.GetDirectoryName(file.Path))));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task CleanStateDirectoryExtensionDoesNotRemoveFilesOrDirectoriesOutsideOfTheStateDirectoryWindows()
        {
            DependencyFixture mockFixture = new DependencyFixture(PlatformID.Win32NT);
            string stateDirectory = mockFixture.GetStatePath();
            string otherDirectory = "any-directory";

            // Scenario:
            // Only the 'state' folder files and directories should get removed. No other directories
            // outside of this folder should be touched.

            List<string> filePaths = new List<string>
            {
                // Should be removed.
                mockFixture.Combine(stateDirectory, "directory1", "fileA.log"),
                mockFixture.Combine(stateDirectory, "directory1", "subdirectory1_1", "fileB.log"),

                // Should not be touched
                mockFixture.Combine(otherDirectory, "directory2", "fileC.log"),
                mockFixture.Combine(otherDirectory, "directory2", "subdirectory1_1", "fileD.log"),
            };

            filePaths.ForEach(file => mockFixture.FileSystem.File.Create(file));

            await mockFixture.SystemManagement.Object.CleanStateDirectoryAsync(CancellationToken.None);

            Assert.IsTrue(mockFixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(2)));
            Assert.IsTrue(mockFixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(3)));
        }

        [Test]
        public async Task CleanStateDirectoryExtensionDoesNotRemoveFilesOrDirectoriesOutsideOfTheStateDirectoryLinux()
        {
            DependencyFixture mockFixture = new DependencyFixture(PlatformID.Unix);
            string stateDirectory = mockFixture.GetStatePath();
            string otherDirectory = "any-directory";

            // Scenario:
            // Only the 'state' folder files and directories should get removed. No other directories
            // outside of this folder should be touched.

            List<string> filePaths = new List<string>
            {
                // Should be removed.
                mockFixture.Combine(stateDirectory, "directory1", "fileA.log"),
                mockFixture.Combine(stateDirectory, "directory1", "subdirectory1_1", "fileB.log"),

                // Should not be touched
                mockFixture.Combine(otherDirectory, "directory2", "fileC.log"),
                mockFixture.Combine(otherDirectory, "directory2", "subdirectory1_1", "fileD.log"),
            };

            filePaths.ForEach(file => mockFixture.FileSystem.File.Create(file));

            await mockFixture.SystemManagement.Object.CleanStateDirectoryAsync(CancellationToken.None);

            Assert.IsTrue(mockFixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(2)));
            Assert.IsTrue(mockFixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(3)));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task CleanPackagesDirectoryExtensionDeletesExpectedPackageFilesAndDirectoriesInThePackagesFolderWindows()
        {
            DependencyFixture mockFixture = new DependencyFixture(PlatformID.Win32NT);
            string packagesDirectory = mockFixture.GetPackagePath();

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
            var package1Registration = new DependencyPath("package1", mockFixture.Combine(packagesDirectory, "package1"));

            var package2Definition = new DependencyMetadata("package2");
            var package2Registration = new DependencyPath("package2", mockFixture.Combine(packagesDirectory, "package2"));

            List<string> filePaths = new List<string>
            {
                mockFixture.Combine(packagesDirectory, "package1.vcpkgreg"),
                mockFixture.Combine(package1Registration.Path, "package1.vcpkg"),
                mockFixture.Combine(package1Registration.Path, "linux-x64", "workload1.sh"),
                mockFixture.Combine(package1Registration.Path, "linux-arm64", "workload1.sh"),
                mockFixture.Combine(package1Registration.Path, "win-x64", "workload1.exe"),
                mockFixture.Combine(package1Registration.Path, "win-arm64", "workload1.exe"),
                mockFixture.Combine(packagesDirectory, "package2.vcpkgreg"),
                mockFixture.Combine(package2Registration.Path, "package2.vcpkg"),
                mockFixture.Combine(package2Registration.Path, "linux-x64", "workload2.sh"),
                mockFixture.Combine(package2Registration.Path, "linux-arm64", "workload2.sh"),
                mockFixture.Combine(package2Registration.Path, "win-x64", "workload2.exe"),
                mockFixture.Combine(package2Registration.Path, "win-arm64", "workload2.exe"),
            };

            filePaths.ForEach(file =>
            {
                mockFixture.FileSystem.File.Create(file);

                InMemoryFile targetFile = mockFixture.FileSystem.FileSystemEntries.First(e => e.Path == file) as InMemoryFile;

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

            await mockFixture.SystemManagement.Object.CleanPackagesDirectoryAsync(CancellationToken.None);

            Assert.IsFalse(mockFixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(file.Path)));
            Assert.IsFalse(mockFixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(MockFixture.GetDirectoryName(file.Path))));
        }

        [Test]
        public async Task CleanPackagesDirectoryExtensionDeletesExpectedPackageFilesAndDirectoriesInThePackagesFolderLinux()
        {
            DependencyFixture mockFixture = new DependencyFixture(PlatformID.Unix);
            string packagesDirectory = mockFixture.GetPackagePath();

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
            var package1Registration = new DependencyPath("package1", mockFixture.Combine(packagesDirectory, "package1"));

            var package2Definition = new DependencyMetadata("package2");
            var package2Registration = new DependencyPath("package2", mockFixture.Combine(packagesDirectory, "package2"));

            List<string> filePaths = new List<string>
            {
                mockFixture.Combine(packagesDirectory, "package1.vcpkgreg"),
                mockFixture.Combine(package1Registration.Path, "package1.vcpkg"),
                mockFixture.Combine(package1Registration.Path, "linux-x64", "workload1.sh"),
                mockFixture.Combine(package1Registration.Path, "linux-arm64", "workload1.sh"),
                mockFixture.Combine(package1Registration.Path, "win-x64", "workload1.exe"),
                mockFixture.Combine(package1Registration.Path, "win-arm64", "workload1.exe"),
                mockFixture.Combine(packagesDirectory, "package2.vcpkgreg"),
                mockFixture.Combine(package2Registration.Path, "package2.vcpkg"),
                mockFixture.Combine(package2Registration.Path, "linux-x64", "workload2.sh"),
                mockFixture.Combine(package2Registration.Path, "linux-arm64", "workload2.sh"),
                mockFixture.Combine(package2Registration.Path, "win-x64", "workload2.exe"),
                mockFixture.Combine(package2Registration.Path, "win-arm64", "workload2.exe"),
            };

            filePaths.ForEach(file =>
            {
                mockFixture.FileSystem.File.Create(file);

                InMemoryFile targetFile = mockFixture.FileSystem.FileSystemEntries.First(e => e.Path == file) as InMemoryFile;

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

            await mockFixture.SystemManagement.Object.CleanPackagesDirectoryAsync(CancellationToken.None);

            Assert.IsFalse(mockFixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(file.Path)));
            Assert.IsFalse(mockFixture.FileSystem.FileSystemEntries.Any(file => filePaths.Contains(MockFixture.GetDirectoryName(file.Path))));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public async Task CleanPackagesDirectoryExtensionDoesNotRemoveFilesOrDirectoriesOutsideOfThePackagesDirectoryWindows()
        {
            DependencyFixture mockFixture = new DependencyFixture(PlatformID.Win32NT);
            string packagesDirectory = mockFixture.GetPackagePath();
            string otherDirectory = "any-directory";

            // Scenario:
            // Only the 'packages' folder files and directories should get removed. No other directories
            // outside of this folder should be touched.

            var package1Definition = new DependencyMetadata("package1");
            var package1Registration = new DependencyPath("package1", mockFixture.Combine(packagesDirectory, "package1"));

            List<string> filePaths = new List<string>
            {
                // Should be removed.
                mockFixture.Combine(packagesDirectory, "package1.vcpkgreg"),
                mockFixture.Combine(package1Registration.Path, "package1.vcpkg"),
                mockFixture.Combine(package1Registration.Path, "linux-x64", "workload1.sh"),
                mockFixture.Combine(package1Registration.Path, "linux-arm64", "workload1.sh"),
                mockFixture.Combine(package1Registration.Path, "win-x64", "workload1.exe"),
                mockFixture.Combine(package1Registration.Path, "win-arm64", "workload1.exe"),

                // Should not be touched
                mockFixture.Combine(otherDirectory, "directory2", "fileC.log"),
                mockFixture.Combine(otherDirectory, "directory2", "subdirectory1_1", "fileD.log"),
            };

            filePaths.ForEach(file =>
            {
                mockFixture.FileSystem.File.Create(file);

                InMemoryFile targetFile = mockFixture.FileSystem.FileSystemEntries.First(e => e.Path == file) as InMemoryFile;

                if (file.EndsWith("package1.vcpkg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package1Definition.ToJson()));
                }
                else if (file.EndsWith("package1.vcpkgreg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package1Registration.ToJson()));
                }
            });

            await mockFixture.SystemManagement.Object.CleanPackagesDirectoryAsync(CancellationToken.None);

            Assert.IsTrue(mockFixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(6)));
            Assert.IsTrue(mockFixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(7)));
        }

        [Test]
        public async Task CleanPackagesDirectoryExtensionDoesNotRemoveFilesOrDirectoriesOutsideOfThePackagesDirectoryLinux()
        {
            DependencyFixture mockFixture = new DependencyFixture(PlatformID.Unix);
            string packagesDirectory = mockFixture.GetPackagePath();
            string otherDirectory = "any-directory";

            // Scenario:
            // Only the 'packages' folder files and directories should get removed. No other directories
            // outside of this folder should be touched.

            var package1Definition = new DependencyMetadata("package1");
            var package1Registration = new DependencyPath("package1", mockFixture.Combine(packagesDirectory, "package1"));

            List<string> filePaths = new List<string>
            {
                // Should be removed.
                mockFixture.Combine(packagesDirectory, "package1.vcpkgreg"),
                mockFixture.Combine(package1Registration.Path, "package1.vcpkg"),
                mockFixture.Combine(package1Registration.Path, "linux-x64", "workload1.sh"),
                mockFixture.Combine(package1Registration.Path, "linux-arm64", "workload1.sh"),
                mockFixture.Combine(package1Registration.Path, "win-x64", "workload1.exe"),
                mockFixture.Combine(package1Registration.Path, "win-arm64", "workload1.exe"),

                // Should not be touched
                mockFixture.Combine(otherDirectory, "directory2", "fileC.log"),
                mockFixture.Combine(otherDirectory, "directory2", "subdirectory1_1", "fileD.log"),
            };

            filePaths.ForEach(file =>
            {
                mockFixture.FileSystem.File.Create(file);

                InMemoryFile targetFile = mockFixture.FileSystem.FileSystemEntries.First(e => e.Path == file) as InMemoryFile;

                if (file.EndsWith("package1.vcpkg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package1Definition.ToJson()));
                }
                else if (file.EndsWith("package1.vcpkgreg"))
                {
                    targetFile.FileBytes.AddRange(Encoding.UTF8.GetBytes(package1Registration.ToJson()));
                }
            });

            await mockFixture.SystemManagement.Object.CleanPackagesDirectoryAsync(CancellationToken.None);

            Assert.IsTrue(mockFixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(6)));
            Assert.IsTrue(mockFixture.FileSystem.FileSystemEntries.Any(file => file.Path == filePaths.ElementAt(7)));
        }
    }
}
