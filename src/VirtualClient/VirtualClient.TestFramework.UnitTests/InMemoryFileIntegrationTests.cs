// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.IO;
    using System.Linq;
    using NUnit.Framework;

    [TestFixture]
    [Category("Unit")]
    public class InMemoryFileIntegrationTests
    {
        private InMemoryFileSystem fileSystem;

        [Test]
        public void InMemoryFileIntegrationCreatesFilesAsExpectedOnWindowsSystems()
        {
            this.SetupFileSystem(PlatformID.Win32NT);

            Assert.IsEmpty(this.fileSystem);

            string expectedFile = @"C:\any\directory\with\file.exe";
            this.fileSystem.File.Create(expectedFile);

            Assert.IsNotEmpty(this.fileSystem);

            InMemoryFile actualFile = this.fileSystem.FirstOrDefault(file => file.Path == expectedFile) as InMemoryFile;
            Assert.IsNotNull(actualFile);
            Assert.IsEmpty(actualFile.FileBytes);
            Assert.AreEqual(FileAttributes.Normal, actualFile.Attributes);
            Assert.IsTrue(object.ReferenceEquals(actualFile, this.fileSystem.GetFile(expectedFile)));
        }

        [Test]
        public void InMemoryFileIntegrationCreatesFilesAsExpectedOnUnixSystems()
        {
            this.SetupFileSystem(PlatformID.Unix);

            Assert.IsEmpty(this.fileSystem);

            string expectedFile = "/home/any/directory/with/file";
            this.fileSystem.File.Create(expectedFile);

            Assert.IsNotEmpty(this.fileSystem);

            InMemoryFile actualFile = this.fileSystem.FirstOrDefault(file => file.Path == expectedFile) as InMemoryFile;
            Assert.IsNotNull(actualFile);
            Assert.IsEmpty(actualFile.FileBytes);
            Assert.AreEqual(FileAttributes.Normal, actualFile.Attributes);
            Assert.IsTrue(object.ReferenceEquals(actualFile, this.fileSystem.GetFile(expectedFile)));
        }

        [Test]
        public void InMemoryFileIntegrationDeletesFilesAsExpectedOnWindowsSystems()
        {
            this.SetupFileSystem(PlatformID.Win32NT);

            Assert.IsEmpty(this.fileSystem);

            string expectedFile = @"C:\any\directory\with\file.exe";
            this.fileSystem.File.Create(expectedFile);

            Assert.IsNotNull(this.fileSystem.FirstOrDefault(file => file.Path == expectedFile) as InMemoryFile);
            this.fileSystem.File.Delete(expectedFile);

            Assert.IsNull(this.fileSystem.FirstOrDefault(file => file.Path == expectedFile) as InMemoryFile);
        }

        [Test]
        public void InMemoryFileIntegrationDeletesFilesAsExpectedOnUnixSystems()
        {
            this.SetupFileSystem(PlatformID.Unix);

            Assert.IsEmpty(this.fileSystem);

            string expectedFile = "/home/any/directory/with/file";
            this.fileSystem.File.Create(expectedFile);

            Assert.IsNotNull(this.fileSystem.FirstOrDefault(file => file.Path == expectedFile) as InMemoryFile);
            this.fileSystem.File.Delete(expectedFile);

            Assert.IsNull(this.fileSystem.FirstOrDefault(file => file.Path == expectedFile) as InMemoryFile);
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
