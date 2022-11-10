// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class InMemoryDirectoryIntegrationTests
    {
        private InMemoryFileSystem fileSystem;

        [Test]
        public void InMemoryDirectoryIntegrationCreatesDirectoriesAsExpectedOnWindowsSystems()
        {
            this.SetupFileSystem(PlatformID.Win32NT);

            Assert.IsEmpty(this.fileSystem);

            string expectedDirectory = @"C:\any\directory\with\files";
            this.fileSystem.Directory.CreateDirectory(expectedDirectory);

            Assert.IsNotEmpty(this.fileSystem);

            InMemoryDirectory actualDirectory = this.fileSystem.FirstOrDefault(entry => entry.Path == expectedDirectory) as InMemoryDirectory;
            Assert.IsNotNull(actualDirectory);
            Assert.AreEqual(FileAttributes.Directory, actualDirectory.Attributes);
            Assert.IsTrue(object.ReferenceEquals(actualDirectory, this.fileSystem.GetDirectory(expectedDirectory)));
        }

        [Test]
        public void InMemoryDirectoryIntegrationCreatesDirectoriesAsExpectedOnUnixSystems()
        {
            this.SetupFileSystem(PlatformID.Unix);

            Assert.IsEmpty(this.fileSystem);

            string expectedDirectory = @"/any/directory/with/files";
            this.fileSystem.Directory.CreateDirectory(expectedDirectory);

            Assert.IsNotEmpty(this.fileSystem);

            InMemoryDirectory actualDirectory = this.fileSystem.FirstOrDefault(entry => entry.Path == expectedDirectory) as InMemoryDirectory;
            Assert.IsNotNull(actualDirectory);
            Assert.AreEqual(FileAttributes.Directory, actualDirectory.Attributes);
            Assert.IsTrue(object.ReferenceEquals(actualDirectory, this.fileSystem.GetDirectory(expectedDirectory)));
        }

        [Test]
        public void InMemoryDirectoryIntegrationDeletesDirectoriesAsExpectedOnWindowsSystems()
        {
            this.SetupFileSystem(PlatformID.Win32NT);

            Assert.IsEmpty(this.fileSystem);

            string expectedDirectory = @"C:\any\directory\with\files";
            this.fileSystem.Directory.CreateDirectory(expectedDirectory);

            Assert.IsNotNull(this.fileSystem.FirstOrDefault(entry => entry.Path == expectedDirectory) as InMemoryDirectory);
            this.fileSystem.Directory.Delete(expectedDirectory);

            Assert.IsNull(this.fileSystem.FirstOrDefault(entry => entry.Path == expectedDirectory) as InMemoryDirectory);
        }

        [Test]
        public void InMemoryDirectoryIntegrationDeletesFilesAsExpectedOnUnixSystems()
        {
            this.SetupFileSystem(PlatformID.Unix);

            Assert.IsEmpty(this.fileSystem);

            string expectedDirectory = @"/any/directory/with/files";
            this.fileSystem.Directory.CreateDirectory(expectedDirectory);

            Assert.IsNotNull(this.fileSystem.FirstOrDefault(entry => entry.Path == expectedDirectory) as InMemoryDirectory);
            this.fileSystem.Directory.Delete(expectedDirectory);

            Assert.IsNull(this.fileSystem.FirstOrDefault(entry => entry.Path == expectedDirectory) as InMemoryDirectory);
        }

        [Test]
        public void InMemoryDirectoryIntegrationGetsTheExpectedMatchingDirectoriesOnWindowsSystems_Default()
        {
            this.SetupFileSystem(PlatformID.Win32NT);

            Assert.IsEmpty(this.fileSystem);

            List<string> expectedDirectories = new List<string>
            {
                @"C:\any\directory\1",
                @"C:\any\directory\2",
                @"C:\any\directory\1\other3",
                @"C:\any\directory\2\other4",
            };

            expectedDirectories.ForEach(dir => this.fileSystem.Directory.CreateDirectory(dir));

            Assert.IsNotEmpty(this.fileSystem);
            IEnumerable<string> actualDirectories = this.fileSystem.Directory.GetDirectories(@"C:\any\directory");

            Assert.IsNotNull(actualDirectories);
            Assert.IsTrue(actualDirectories.Count() == 2);
            CollectionAssert.AreEquivalent(expectedDirectories.Take(2), actualDirectories);
        }

        [Test]
        public void InMemoryDirectoryIntegrationGetsTheExpectedMatchingDirectoriesOnUnixSystems_Default()
        {
            this.SetupFileSystem(PlatformID.Unix);

            Assert.IsEmpty(this.fileSystem);

            List<string> expectedDirectories = new List<string>
            {
                @"/any/directory/1",
                @"/any/directory/2",
                @"/any/directory/1/other3",
                @"/any/directory/2/other4",
            };

            expectedDirectories.ForEach(dir => this.fileSystem.Directory.CreateDirectory(dir));

            Assert.IsNotEmpty(this.fileSystem);
            IEnumerable<string> actualDirectories = this.fileSystem.Directory.GetDirectories(@"/any/directory");

            Assert.IsNotNull(actualDirectories);
            Assert.IsTrue(actualDirectories.Count() == 2);
            CollectionAssert.AreEquivalent(expectedDirectories.Take(2), actualDirectories);
        }

        [Test]
        public void InMemoryDirectoryIntegrationGetsTheExpectedMatchingDirectoriesOnWindowsSystems_RecursiveSearch()
        {
            this.SetupFileSystem(PlatformID.Win32NT);

            Assert.IsEmpty(this.fileSystem);

            List<string> expectedDirectories = new List<string>
            {
                @"C:\any\directory\1",
                @"C:\any\directory\2",
                @"C:\any\directory\1\other3",
                @"C:\any\directory\2\other4",
            };

            expectedDirectories.ForEach(dir => this.fileSystem.Directory.CreateDirectory(dir));

            Assert.IsNotEmpty(this.fileSystem);
            IEnumerable<string> actualDirectories = this.fileSystem.Directory.GetDirectories(@"C:\any\directory", "*", SearchOption.AllDirectories);

            Assert.IsNotNull(actualDirectories);
            Assert.IsTrue(actualDirectories.Count() == 4);
            CollectionAssert.AreEquivalent(expectedDirectories, actualDirectories);
        }

        [Test]
        public void InMemoryDirectoryIntegrationGetsTheExpectedMatchingDirectoriesOnUnixSystems_RecursiveSearch()
        {
            this.SetupFileSystem(PlatformID.Unix);

            Assert.IsEmpty(this.fileSystem);

            List<string> expectedDirectories = new List<string>
            {
                @"/any/directory/1",
                @"/any/directory/2",
                @"/any/directory/1/other3",
                @"/any/directory/2/other4",
            };

            expectedDirectories.ForEach(dir => this.fileSystem.Directory.CreateDirectory(dir));

            Assert.IsNotEmpty(this.fileSystem);
            IEnumerable<string> actualDirectories = this.fileSystem.Directory.GetDirectories(@"/any/directory", "*", SearchOption.AllDirectories);

            Assert.IsNotNull(actualDirectories);
            Assert.IsTrue(actualDirectories.Count() == 4);
            CollectionAssert.AreEquivalent(expectedDirectories, actualDirectories);
        }

        [Test]
        public void InMemoryDirectoryIntegrationGetsTheExpectedMatchingDirectoriesOnWindowsSystems_RecursiveSearch_WithSearchPattern()
        {
            this.SetupFileSystem(PlatformID.Win32NT);

            Assert.IsEmpty(this.fileSystem);

            List<string> expectedDirectories = new List<string>
            {
                @"C:\any\directory\1",
                @"C:\any\directory\2",
                @"C:\any\directory\1\other3",
                @"C:\any\directory\2\other4",
            };

            expectedDirectories.ForEach(dir => this.fileSystem.Directory.CreateDirectory(dir));

            Assert.IsNotEmpty(this.fileSystem);
            IEnumerable<string> actualDirectories = this.fileSystem.Directory.GetDirectories(@"C:\any\directory", "*other", SearchOption.AllDirectories);

            Assert.IsNotNull(actualDirectories);
            Assert.IsTrue(actualDirectories.Count() == 2);
            CollectionAssert.AreEquivalent(expectedDirectories.Skip(2), actualDirectories);
        }

        [Test]
        public void InMemoryDirectoryIntegrationGetsTheExpectedMatchingDirectoriesOnUnixSystems_RecursiveSearch_WithSearchPattern()
        {
            this.SetupFileSystem(PlatformID.Unix);

            Assert.IsEmpty(this.fileSystem);

            List<string> expectedDirectories = new List<string>
            {
                @"/any/directory/1",
                @"/any/directory/2",
                @"/any/directory/1/other3",
                @"/any/directory/2/other4",
            };

            expectedDirectories.ForEach(dir => this.fileSystem.Directory.CreateDirectory(dir));

            Assert.IsNotEmpty(this.fileSystem);
            IEnumerable<string> actualDirectories = this.fileSystem.Directory.GetDirectories(@"/any/directory", "*other", SearchOption.AllDirectories);

            Assert.IsNotNull(actualDirectories);
            Assert.IsTrue(actualDirectories.Count() == 2);
            CollectionAssert.AreEquivalent(expectedDirectories.Skip(2), actualDirectories);
        }

        [Test]
        public void InMemoryDirectoryIntegrationGetsTheExpectedMatchingFilesOnUnixSystems_Default()
        {
            this.SetupFileSystem(PlatformID.Unix);

            Assert.IsEmpty(this.fileSystem);

            List<string> expectedFiles = new List<string>
            {
                @"/any/directory/with/file1",
                @"/any/directory/with/file1.dll",
                @"/any/directory/with/file2.dll",
                @"/any/directory/with/other/file2",
                @"/any/directory/with/other/file3.dll",
                @"/any/directory/with/other/file4.dll"
            };

            expectedFiles.ForEach(file => this.fileSystem.File.Create(file));

            Assert.IsNotEmpty(this.fileSystem);
            IEnumerable<string> actualFiles = this.fileSystem.Directory.GetFiles("/any/directory/with");

            Assert.IsNotNull(actualFiles);
            Assert.IsTrue(actualFiles.Count() == 3);
            CollectionAssert.AreEquivalent(expectedFiles.Take(3), actualFiles);
        }

        [Test]
        public void InMemoryDirectoryIntegrationGetsTheExpectedMatchingFilesOnWindowsSystems_RecursiveSearch()
        {
            this.SetupFileSystem(PlatformID.Win32NT);

            Assert.IsEmpty(this.fileSystem);

            List<string> expectedFiles = new List<string>
            {
                @"C:\any\directory\with\file1.exe",
                @"C:\any\directory\with\file1.dll",
                @"C:\any\directory\with\file2.dll",
                @"C:\any\directory\with\another\file2.exe",
                @"C:\any\directory\with\another\file3.dll",
                @"C:\any\directory\with\another\file4.dll"
            };

            expectedFiles.ForEach(file => this.fileSystem.File.Create(file));

            Assert.IsNotEmpty(this.fileSystem);
            IEnumerable<string> actualFiles = this.fileSystem.Directory.GetFiles(@"C:\any\directory\with", "*", SearchOption.AllDirectories);

            Assert.IsNotNull(actualFiles);
            Assert.IsTrue(actualFiles.Count() == 6);
            CollectionAssert.AreEquivalent(expectedFiles, actualFiles);
        }

        [Test]
        public void InMemoryDirectoryIntegrationGetsTheExpectedMatchingFilesOnUnixSystems_RecursiveSearch()
        {
            this.SetupFileSystem(PlatformID.Unix);

            Assert.IsEmpty(this.fileSystem);

            List<string> expectedFiles = new List<string>
            {
                @"/any/directory/with/file1",
                @"/any/directory/with/file1.dll",
                @"/any/directory/with/file2.dll",
                @"/any/directory/with/other/file2",
                @"/any/directory/with/other/file3.dll",
                @"/any/directory/with/other/file4.dll"
            };

            expectedFiles.ForEach(file => this.fileSystem.File.Create(file));

            Assert.IsNotEmpty(this.fileSystem);
            IEnumerable<string> actualFiles = this.fileSystem.Directory.GetFiles("/any/directory/with", "*", SearchOption.AllDirectories);

            Assert.IsNotNull(actualFiles);
            Assert.IsTrue(actualFiles.Count() == 6);
            CollectionAssert.AreEquivalent(expectedFiles, actualFiles);
        }

        [Test]
        public void InMemoryDirectoryIntegrationGetsTheExpectedMatchingFilesOnWindowsSystems_RecursiveSearch_WithSearchPattern()
        {
            this.SetupFileSystem(PlatformID.Win32NT);

            Assert.IsEmpty(this.fileSystem);

            List<string> expectedFiles = new List<string>
            {
                @"C:\any\directory\with\file1.exe",
                @"C:\any\directory\with\file1.dll",
                @"C:\any\directory\with\file2.dll",
                @"C:\any\directory\with\another\file2.exe",
                @"C:\any\directory\with\another\file3.dll",
                @"C:\any\directory\with\another\file4.dll"
            };

            expectedFiles.ForEach(file => this.fileSystem.File.Create(file));

            Assert.IsNotEmpty(this.fileSystem);
            IEnumerable<string> actualFiles = this.fileSystem.Directory.GetFiles(@"C:\any\directory\with", "*.dll", SearchOption.AllDirectories);

            Assert.IsNotNull(actualFiles);
            Assert.IsTrue(actualFiles.Count() == 4);
            CollectionAssert.AreEquivalent(expectedFiles.Skip(1).Take(2).Union(expectedFiles.Skip(4)), actualFiles);
        }

        [Test]
        public void InMemoryDirectoryIntegrationGetsTheExpectedMatchingFilesOnUnixSystems_RecursiveSearch_WithSearchPattern()
        {
            this.SetupFileSystem(PlatformID.Unix);

            Assert.IsEmpty(this.fileSystem);

            List<string> expectedFiles = new List<string>
            {
                @"/any/directory/with/file1",
                @"/any/directory/with/file1.dll",
                @"/any/directory/with/file2.dll",
                @"/any/directory/with/other/file2",
                @"/any/directory/with/other/file3.dll",
                @"/any/directory/with/other/file4.dll"
            };

            expectedFiles.ForEach(file => this.fileSystem.File.Create(file));

            Assert.IsNotEmpty(this.fileSystem);
            IEnumerable<string> actualFiles = this.fileSystem.Directory.GetFiles("/any/directory/with", "file2*", SearchOption.AllDirectories);

            Assert.IsNotNull(actualFiles);
            Assert.IsTrue(actualFiles.Count() == 2);
            CollectionAssert.AreEquivalent(expectedFiles.Skip(2).Take(2), actualFiles);
        }

        private void SetupFileSystem(PlatformID platform)
        {
            switch (platform)
            {
                case PlatformID.Unix:
                    this.fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(platform, @"/any/directory"));
                    break;

                case PlatformID.Win32NT:
                    this.fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(platform, @"C:\any\directory"));
                    break;
            }
        }
    }
}
