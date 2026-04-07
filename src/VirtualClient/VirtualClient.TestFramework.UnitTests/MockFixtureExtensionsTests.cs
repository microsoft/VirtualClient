// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;
using VirtualClient.Contracts;

namespace VirtualClient
{
    [TestFixture]
    [Category("Unit")]
    public class MockFixtureExtensionsTests
    {
        private MockFixture mockFixture;

        public void SetupTest(PlatformID platform = PlatformID.Unix, Architecture architecture = Architecture.X64)
        {
            this.mockFixture = new MockFixture();
            this.mockFixture.Setup(platform, architecture);
        }

        [Test]
        public void SetupDirectoryExtensionSetsUpTheExpectedBehaviorForTheMockFileSystem_Directory_Exists()
        {
            this.SetupTest(PlatformID.Unix);

            string directory = "/home/user/test";
            bool directoryExists = this.mockFixture.Directory.Object.Exists(directory);
            Assert.IsFalse(directoryExists);

            this.mockFixture.SetupDirectory(directory);
            directoryExists = this.mockFixture.Directory.Object.Exists(directory);
            Assert.IsTrue(directoryExists);
        }

        [Test]
        public void SetupDirectoryExtensionSetsUpTheExpectedBehaviorForTheMockFileSystem_Subdirectories_Exist()
        {
            this.SetupTest(PlatformID.Unix);

            string directory = "/home/user/dir1";
            string[] expectedFiles = new string[]
            {
                "/home/user/dir1/file1.txt",
                "/home/user/dir1/file1.exe",
                "/home/user/dir1/dir2/file2.txt",
                "/home/user/dir1/dir2/file2.exe",
                "/home/user/dir1/dir2/dir3/file3.txt",
                "/home/user/dir1/dir2/dir3/file3.exe"
            };

            bool directoryExists = this.mockFixture.Directory.Object.Exists(directory);
            Assert.IsFalse(directoryExists);

            this.mockFixture.SetupDirectory(directory, expectedFiles);
            Assert.IsTrue(this.mockFixture.Directory.Object.Exists("/home/user/dir1"));
            Assert.IsTrue(this.mockFixture.Directory.Object.Exists("/home/user/dir1"));
            Assert.IsTrue(this.mockFixture.Directory.Object.Exists("/home/user/dir1/dir2"));
            Assert.IsTrue(this.mockFixture.Directory.Object.Exists("/home/user/dir1/dir2/dir3"));
        }

        [Test]
        public void SetupDirectoryExtensionSetsUpTheExpectedBehaviorForTheMockFileSystem_Files_Exist()
        {
            this.SetupTest(PlatformID.Unix);

            string directory = "/home/user/dir1";
            string[] expectedFiles = new string[]
            {
                "/home/user/dir1/file1.txt",
                "/home/user/dir1/file1.exe",
                "/home/user/dir1/dir2/file2.txt",
                "/home/user/dir1/dir2/file2.exe",
                "/home/user/dir1/dir2/dir3/file3.txt",
                "/home/user/dir1/dir2/dir3/file3.exe"
            };

            bool directoryExists = this.mockFixture.Directory.Object.Exists(directory);
            Assert.IsFalse(directoryExists);

            this.mockFixture.SetupDirectory(directory, expectedFiles);
            foreach (string file in expectedFiles)
            {
                Assert.IsTrue(this.mockFixture.File.Object.Exists(file));
            }
        }

        [Test]
        public void SetupDirectoryExtensionSetsUpTheExpectedBehaviorForTheMockFileSystem_Directory_EnumerateFiles_Overload_1()
        {
            this.SetupTest(PlatformID.Unix);

            string directory = "/home/user/test";
            string[] expectedFiles = new string[]
            {
                "/home/user/test/file1.txt",
                "/home/user/test/file1.exe"
            };

            IEnumerable<string> actualFiles = this.mockFixture.Directory.Object.EnumerateFiles(directory);
            Assert.IsEmpty(actualFiles);

            this.mockFixture.SetupDirectory(directory, expectedFiles);

            actualFiles = this.mockFixture.Directory.Object.EnumerateFiles(directory);
            Assert.IsNotEmpty(actualFiles);
            CollectionAssert.AreEquivalent(expectedFiles, actualFiles);
        }

        [Test]
        public void SetupDirectoryExtensionSetsUpTheExpectedBehaviorForTheMockFileSystem_Directory_EnumerateFiles_Overload_2()
        {
            this.SetupTest(PlatformID.Unix);

            string directory = "/home/user/test";
            string[] expectedFiles = new string[]
            {
                "/home/user/test/file.txt",
                "/home/user/test/file.exe",
                "/home/user/file/other.log",
            };

            IEnumerable<string> actualFiles = this.mockFixture.Directory.Object.EnumerateFiles(directory, "*.txt");
            Assert.IsEmpty(actualFiles);

            // Scenario:
            // Filtering on file endings
            this.mockFixture.SetupDirectory(directory, expectedFiles);
            actualFiles = this.mockFixture.Directory.Object.EnumerateFiles(directory, "*.txt");
            Assert.IsNotEmpty(actualFiles);
            CollectionAssert.AreEquivalent(expectedFiles.Where(f => f.EndsWith(".txt")), actualFiles);

            // Scenario:
            // Filtering on file names. File names must be exact including name and extension or
            // they will not be matched.
            actualFiles = this.mockFixture.Directory.Object.EnumerateFiles(directory, "file");
            Assert.IsEmpty(actualFiles);

            actualFiles = this.mockFixture.Directory.Object.EnumerateFiles(directory, "file.exe");
            Assert.IsNotEmpty(actualFiles);
            CollectionAssert.AreEquivalent(expectedFiles.Where(f => f.EndsWith(".exe")), actualFiles);
        }

        [Test]
        public void SetupDirectoryExtensionSetsUpTheExpectedBehaviorForTheMockFileSystem_Directory_EnumerateFiles_Overload_3()
        {
            this.SetupTest(PlatformID.Unix);

            string directory = "/home/user/dir1";
            string[] expectedFiles = new string[]
            {
                "/home/user/dir1/file1.txt",
                "/home/user/dir1/file1.exe",
                "/home/user/dir1/dir2/file2.txt",
                "/home/user/dir1/dir2/file2.exe",
                "/home/user/dir1/dir2/dir3/file3.txt",
                "/home/user/dir1/dir2/dir3/file3.exe"
            };

            IEnumerable<string> actualFiles = this.mockFixture.Directory.Object.EnumerateFiles(directory, "*.txt", SearchOption.AllDirectories);
            Assert.IsEmpty(actualFiles);

            // Scenario:
            // Filtering on file endings, all directories
            this.mockFixture.SetupDirectory(directory, expectedFiles);
            actualFiles = this.mockFixture.Directory.Object.EnumerateFiles(directory, "*.txt", SearchOption.AllDirectories);
            Assert.IsNotEmpty(actualFiles);
            CollectionAssert.AreEquivalent(expectedFiles.Where(f => f.EndsWith(".txt")), actualFiles);

            // Scenario:
            // Filtering on file endings, top directory only.
            actualFiles = this.mockFixture.Directory.Object.EnumerateFiles(directory, "*.txt", SearchOption.TopDirectoryOnly);
            Assert.IsNotEmpty(actualFiles);
            CollectionAssert.AreEquivalent(expectedFiles.Where(f => f == "/home/user/dir1/file1.txt"), actualFiles);

            // Scenario:
            // Filtering on file names, all directories. File names must be exact including name and extension or
            // they will not be matched.
            actualFiles = this.mockFixture.Directory.Object.EnumerateFiles(directory, "file3", SearchOption.AllDirectories);
            Assert.IsEmpty(actualFiles);

            actualFiles = this.mockFixture.Directory.Object.EnumerateFiles(directory, "file3.exe", SearchOption.AllDirectories);
            Assert.IsNotEmpty(actualFiles);
            CollectionAssert.AreEquivalent(expectedFiles.Where(f => f == "/home/user/dir1/dir2/dir3/file3.exe"), actualFiles);

            // Scenario:
            // Filtering on file names, top directory only. File names must be exact including name and extension or
            // they will not be matched.
            actualFiles = this.mockFixture.Directory.Object.EnumerateFiles(directory, "file3", SearchOption.TopDirectoryOnly);
            Assert.IsEmpty(actualFiles);

            actualFiles = this.mockFixture.Directory.Object.EnumerateFiles(directory, "file3.exe", SearchOption.TopDirectoryOnly);
            Assert.IsEmpty(actualFiles);
        }

        [Test]
        public void SetupDirectoryExtensionSetsUpTheExpectedBehaviorForTheMockFileSystem_Directory_GetFiles_Overload_1()
        {
            this.SetupTest(PlatformID.Unix);

            string directory = "/home/user/test";
            string[] expectedFiles = new string[]
            {
                "/home/user/test/file1.txt",
                "/home/user/test/file1.exe"
            };

            string[] actualFiles = this.mockFixture.Directory.Object.GetFiles(directory);
            Assert.IsEmpty(actualFiles);

            this.mockFixture.SetupDirectory(directory, expectedFiles);

            actualFiles = this.mockFixture.Directory.Object.GetFiles(directory);
            Assert.IsNotEmpty(actualFiles);
            CollectionAssert.AreEquivalent(expectedFiles, actualFiles);
        }

        [Test]
        public void SetupDirectoryExtensionSetsUpTheExpectedBehaviorForTheMockFileSystem_Directory_GetFiles_Overload_2()
        {
            this.SetupTest(PlatformID.Unix);

            string directory = "/home/user/test";
            string[] expectedFiles = new string[]
            {
                "/home/user/test/file.txt",
                "/home/user/test/file.exe",
                "/home/user/file/other.log",
            };

            string[] actualFiles = this.mockFixture.Directory.Object.GetFiles(directory, "*.txt");
            Assert.IsEmpty(actualFiles);

            // Scenario:
            // Filtering on file endings
            this.mockFixture.SetupDirectory(directory, expectedFiles);
            actualFiles = this.mockFixture.Directory.Object.GetFiles(directory, "*.txt");
            Assert.IsNotEmpty(actualFiles);
            CollectionAssert.AreEquivalent(expectedFiles.Where(f => f.EndsWith(".txt")), actualFiles);

            // Scenario:
            // Filtering on file names. File names must be exact including name and extension or
            // they will not be matched.
            actualFiles = this.mockFixture.Directory.Object.GetFiles(directory, "file");
            Assert.IsEmpty(actualFiles);

            actualFiles = this.mockFixture.Directory.Object.GetFiles(directory, "file.exe");
            Assert.IsNotEmpty(actualFiles);
            CollectionAssert.AreEquivalent(expectedFiles.Where(f => f.EndsWith(".exe")), actualFiles);
        }

        [Test]
        public void SetupDirectoryExtensionSetsUpTheExpectedBehaviorForTheMockFileSystem_Directory_GetFiles_Overload_3()
        {
            this.SetupTest(PlatformID.Unix);

            string directory = "/home/user/dir1";
            string[] expectedFiles = new string[]
            {
                "/home/user/dir1/file1.txt",
                "/home/user/dir1/file1.exe",
                "/home/user/dir1/dir2/file2.txt",
                "/home/user/dir1/dir2/file2.exe",
                "/home/user/dir1/dir2/dir3/file3.txt",
                "/home/user/dir1/dir2/dir3/file3.exe"
            };

            string[] actualFiles = this.mockFixture.Directory.Object.GetFiles(directory, "*.txt", SearchOption.AllDirectories);
            Assert.IsEmpty(actualFiles);

            // Scenario:
            // Filtering on file endings, all directories
            this.mockFixture.SetupDirectory(directory, expectedFiles);
            actualFiles = this.mockFixture.Directory.Object.GetFiles(directory, "*.txt", SearchOption.AllDirectories);
            Assert.IsNotEmpty(actualFiles);
            CollectionAssert.AreEquivalent(expectedFiles.Where(f => f.EndsWith(".txt")), actualFiles);

            // Scenario:
            // Filtering on file endings, top directory only.
            actualFiles = this.mockFixture.Directory.Object.GetFiles(directory, "*.txt", SearchOption.TopDirectoryOnly);
            Assert.IsNotEmpty(actualFiles);
            CollectionAssert.AreEquivalent(expectedFiles.Where(f => f == "/home/user/dir1/file1.txt"), actualFiles);

            // Scenario:
            // Filtering on file names, all directories. File names must be exact including name and extension or
            // they will not be matched.
            actualFiles = this.mockFixture.Directory.Object.GetFiles(directory, "file3", SearchOption.AllDirectories);
            Assert.IsEmpty(actualFiles);

            actualFiles = this.mockFixture.Directory.Object.GetFiles(directory, "file3.exe", SearchOption.AllDirectories);
            Assert.IsNotEmpty(actualFiles);
            CollectionAssert.AreEquivalent(expectedFiles.Where(f => f == "/home/user/dir1/dir2/dir3/file3.exe"), actualFiles);

            // Scenario:
            // Filtering on file names, top directory only. File names must be exact including name and extension or
            // they will not be matched.
            actualFiles = this.mockFixture.Directory.Object.GetFiles(directory, "file3", SearchOption.TopDirectoryOnly);
            Assert.IsEmpty(actualFiles);

            actualFiles = this.mockFixture.Directory.Object.GetFiles(directory, "file3.exe", SearchOption.TopDirectoryOnly);
            Assert.IsEmpty(actualFiles);
        }

        [Test]
        public void SetupFileExtensionSetsUpTheExpectedBehaviorForTheMockFileSystem_File_Exists()
        {
            this.SetupTest(PlatformID.Unix);

            string file = "/home/user/test/file1.txt";
            bool fileExists = this.mockFixture.File.Object.Exists(file);
            Assert.IsFalse(fileExists);

            this.mockFixture.SetupFile(file);
            fileExists = this.mockFixture.File.Object.Exists(file);
            Assert.IsTrue(fileExists);
        }

        [Test]
        public async Task SetupFileExtensionSetsUpTheExpectedBehaviorForTheMockFileSystem_File_ReadAllText_Overloads()
        {
            this.SetupTest(PlatformID.Unix);

            string file = "/home/user/test/file1.txt";
            StringBuilder content = new StringBuilder();
            content.AppendLine("Line 1");
            content.AppendLine("Line 2");
            content.AppendLine("Line 3");

            string expectedContent = content.ToString();

            // Method Overload 1
            this.mockFixture.SetupFile(file, expectedContent);
            string actualContent = this.mockFixture.File.Object.ReadAllText(file);
            Assert.AreEqual(expectedContent, actualContent);

            // Method Overload 2
            actualContent = this.mockFixture.File.Object.ReadAllText(file, Encoding.UTF8);
            Assert.AreEqual(expectedContent, actualContent);

            // Method Overload 3
            actualContent = await this.mockFixture.File.Object.ReadAllTextAsync(file);
            Assert.AreEqual(expectedContent, actualContent);

            // Method Overload 4
            actualContent = await this.mockFixture.File.Object.ReadAllTextAsync(file, Encoding.UTF8);
            Assert.AreEqual(expectedContent, actualContent);
        }

        [Test]
        public async Task SetupFileExtensionSetsUpTheExpectedBehaviorForTheMockFileSystem_File_ReadAllBytes_Overloads()
        {
            this.SetupTest(PlatformID.Unix);

            string file = "/home/user/test/file1.txt";
            StringBuilder content = new StringBuilder();
            content.AppendLine("Line 1");
            content.AppendLine("Line 2");
            content.AppendLine("Line 3");

            string expectedContent = content.ToString();

            // Method Overload 1
            this.mockFixture.SetupFile(file, expectedContent);
            string actualContent = Encoding.UTF8.GetString(this.mockFixture.File.Object.ReadAllBytes(file));
            Assert.AreEqual(expectedContent, actualContent);

            // Method Overload 2
            actualContent = Encoding.UTF8.GetString(await this.mockFixture.File.Object.ReadAllBytesAsync(file));
            Assert.AreEqual(expectedContent, actualContent);
        }

        [Test]
        public async Task SetupFileExtensionSetsUpTheExpectedBehaviorForTheMockFileSystem_File_ReadAllLines_Overloads()
        {
            this.SetupTest(PlatformID.Unix);

            string file = "/home/user/test/file1.txt";
            StringBuilder content = new StringBuilder();
            content.AppendLine("Line 1");
            content.AppendLine("Line 2");
            content.AppendLine("Line 3");

            string expectedContent = content.ToString();

            // Method Overload 1
            this.mockFixture.SetupFile(file, expectedContent);
            string actualContent = string.Join(Environment.NewLine, this.mockFixture.File.Object.ReadAllLines(file));
            Assert.AreEqual(expectedContent, actualContent);

            // Method Overload 2
            actualContent = string.Join(Environment.NewLine, this.mockFixture.File.Object.ReadAllLines(file, Encoding.UTF8));
            Assert.AreEqual(expectedContent, actualContent);

            // Method Overload 3
            actualContent = string.Join(Environment.NewLine, await this.mockFixture.File.Object.ReadAllLinesAsync(file));
            Assert.AreEqual(expectedContent, actualContent);

            // Method Overload 4
            actualContent = string.Join(Environment.NewLine, await this.mockFixture.File.Object.ReadAllLinesAsync(file, Encoding.UTF8));
            Assert.AreEqual(expectedContent, actualContent);
        }

        [Test]
        public async Task SetupPackageExtensionSetsUpTheExpectedBehaviorForTheMockPackageManager_Overload_1()
        {
            this.SetupTest();

            DependencyPath expectedPackage = new DependencyPath("any_package", this.mockFixture.GetPackagePath("any_package"));
            DependencyPath actualPackage = await this.mockFixture.PackageManager.Object.GetPackageAsync(expectedPackage.Name, CancellationToken.None);
            Assert.IsNull(actualPackage);

            this.mockFixture.SetupPackage(expectedPackage);
            actualPackage = await this.mockFixture.PackageManager.Object.GetPackageAsync(expectedPackage.Name, CancellationToken.None);
            Assert.IsTrue(object.ReferenceEquals(expectedPackage, actualPackage));
        }

        [Test]
        public async Task SetupPackageExtensionSetsUpTheExpectedBehaviorForTheMockPackageManager_Overload_2()
        {
            this.SetupTest();

            DependencyPath expectedPackage = new DependencyPath("any_package", this.mockFixture.GetPackagePath("any_package"));
            DependencyPath actualPackage = await this.mockFixture.PackageManager.Object.GetPackageAsync(expectedPackage.Name, CancellationToken.None);
            Assert.IsNull(actualPackage);

            this.mockFixture.SetupPackage(expectedPackage, "linux-arm64", "linux-x64");
            actualPackage = await this.mockFixture.PackageManager.Object.GetPackageAsync(expectedPackage.Name, CancellationToken.None);

            Assert.IsTrue(object.ReferenceEquals(expectedPackage, actualPackage));
            Assert.IsTrue(this.mockFixture.Directory.Object.Exists(this.mockFixture.Combine(expectedPackage.Path, "linux-arm64")));
            Assert.IsTrue(this.mockFixture.Directory.Object.Exists(this.mockFixture.Combine(expectedPackage.Path, "linux-x64")));
            Assert.IsFalse(this.mockFixture.Directory.Object.Exists(this.mockFixture.Combine(expectedPackage.Path, "win-arm64")));
            Assert.IsFalse(this.mockFixture.Directory.Object.Exists(this.mockFixture.Combine(expectedPackage.Path, "win-x64")));
        }

        [Test]
        public async Task SetupPackageExtensionDoesNotPreventLocalTestSetupOverrides__Overload_1_Scenario1()
        {
            this.SetupTest();

            string packageName = "any_package";
            DependencyPath actualPackage = null;
            DependencyPath package1 = new DependencyPath(packageName, this.mockFixture.GetPackagePath("any_package"));
            DependencyPath package2 = new DependencyPath(packageName, this.mockFixture.GetPackagePath("other_package"));

            this.mockFixture.SetupPackage(package1);

            // Expectation:
            // The expected package should be matched/returned.
            actualPackage = await this.mockFixture.PackageManager.Object.GetPackageAsync(packageName, CancellationToken.None);
            Assert.IsNotNull(actualPackage);
            Assert.IsTrue(object.ReferenceEquals(package1, actualPackage));

            // Expectation:
            // The initial SetupPackage behavior should be able to be overridden to return
            // a different package in a later step.
            this.mockFixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(packageName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(package2);

            actualPackage = await this.mockFixture.PackageManager.Object.GetPackageAsync(packageName, CancellationToken.None);
            Assert.IsNotNull(actualPackage);
            Assert.IsTrue(object.ReferenceEquals(package2, actualPackage));

            // Expectation:
            // The initial SetupPackage behavior should be able to be overridden to 
            // accommodate a package not exists scenario.
            this.mockFixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(packageName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(null as DependencyPath);

            actualPackage = await this.mockFixture.PackageManager.Object.GetPackageAsync(packageName, CancellationToken.None);
            Assert.IsNull(actualPackage);
        }

        [Test]
        public async Task SetupPackageExtensionDoesNotPreventLocalTestSetupOverrides__Overload_1_Scenario2()
        {
            this.SetupTest();

            string packageName = "any_package";
            DependencyPath actualPackage = null;
            DependencyPath package1 = new DependencyPath(packageName, this.mockFixture.GetPackagePath("any_package"));
            DependencyPath package2 = new DependencyPath(packageName, this.mockFixture.GetPackagePath("other_package"));

            // Expectation:
            // No setups have happened. The expected package should not exist.
            actualPackage = await this.mockFixture.PackageManager.Object.GetPackageAsync(packageName, CancellationToken.None);
            Assert.IsNull(actualPackage);

            // Expectation:
            // A setup has happened. The expected package should exist.
            this.mockFixture.PackageManager.Setup(mgr => mgr.GetPackageAsync(packageName, It.IsAny<CancellationToken>()))
                .ReturnsAsync(package2);

            actualPackage = await this.mockFixture.PackageManager.Object.GetPackageAsync(packageName, CancellationToken.None);
            Assert.IsNotNull(actualPackage);
            Assert.IsTrue(object.ReferenceEquals(package2, actualPackage));

            // Expectation:
            // The initial SetupPackage behavior should be able to be overridden to 
            // accommodate a package not exists scenario.
            this.mockFixture.SetupPackage(package1);

            actualPackage = await this.mockFixture.PackageManager.Object.GetPackageAsync(packageName, CancellationToken.None);
            Assert.IsNotNull(actualPackage);
            Assert.IsTrue(object.ReferenceEquals(package1, actualPackage));
        }
    }
}
