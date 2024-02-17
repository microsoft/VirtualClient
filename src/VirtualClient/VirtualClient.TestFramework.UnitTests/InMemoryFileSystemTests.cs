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
    public class InMemoryFileSystemTests
    {
        [Test]
        public void InMemoryFileSystemAddsExpectedDirectoriesOnWindowsSystems()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));

            string directoryPath = @"C:\packages\VirtualClient\1.0.0\content\win-x64";
            InMemoryDirectory directory = fileSystem.AddOrGetDirectory(directoryPath);

            Assert.IsNotNull(directory);
            Assert.AreEqual(directoryPath, directory.Path);
            Assert.AreEqual(FileAttributes.Directory, directory.Attributes);

            // When a directory is added, all parent directories above it are expected
            // to be added.
            Assert.AreEqual(6, fileSystem.Count);
            Assert.IsTrue(fileSystem.All(entry => entry is InMemoryDirectory));

            Assert.AreEqual(@"C:", fileSystem.ElementAt(0).Path);
            Assert.AreEqual(@"C:\packages", fileSystem.ElementAt(1).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient", fileSystem.ElementAt(2).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient\1.0.0", fileSystem.ElementAt(3).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient\1.0.0\content", fileSystem.ElementAt(4).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient\1.0.0\content\win-x64", fileSystem.ElementAt(5).Path);
        }

        [Test]
        public void InMemoryFileSystemAddsExpectedDirectoriesOnUnixSystems()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Unix, @"/any/directory"));

            string directoryPath = @"/home/packages/VirtualClient/1.0.0/content/win-x64";
            InMemoryDirectory directory = fileSystem.AddOrGetDirectory(directoryPath);

            Assert.IsNotNull(directory);
            Assert.AreEqual(directoryPath, directory.Path);
            Assert.AreEqual(FileAttributes.Directory, directory.Attributes);

            // When a directory is added, all parent directories above it are expected
            // to be added.
            Assert.AreEqual(7, fileSystem.Count);
            Assert.IsTrue(fileSystem.All(entry => entry is InMemoryDirectory));

            Assert.AreEqual(@"/", fileSystem.ElementAt(0).Path);
            Assert.AreEqual(@"/home", fileSystem.ElementAt(1).Path);
            Assert.AreEqual(@"/home/packages", fileSystem.ElementAt(2).Path);
            Assert.AreEqual(@"/home/packages/VirtualClient", fileSystem.ElementAt(3).Path);
            Assert.AreEqual(@"/home/packages/VirtualClient/1.0.0", fileSystem.ElementAt(4).Path);
            Assert.AreEqual(@"/home/packages/VirtualClient/1.0.0/content", fileSystem.ElementAt(5).Path);
            Assert.AreEqual(@"/home/packages/VirtualClient/1.0.0/content/win-x64", fileSystem.ElementAt(6).Path);
        }

        [Test]
        public void InMemoryFileSystemHandlesExtraneousPathSeparatorsOnWindowsSystemPaths()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));

            string directoryPath = @"C:\packages\";
            InMemoryDirectory directory = fileSystem.AddOrGetDirectory(directoryPath);

            Assert.IsNotNull(directory);
            Assert.AreEqual(directoryPath.TrimEnd('\\'), directory.Path);
            Assert.AreEqual(FileAttributes.Directory, directory.Attributes);

            // When a directory is added, all parent directories above it are expected
            // to be added.
            Assert.AreEqual(2, fileSystem.Count);
            Assert.IsTrue(fileSystem.All(entry => entry is InMemoryDirectory));

            Assert.AreEqual(@"C:", fileSystem.ElementAt(0).Path);
            Assert.AreEqual(@"C:\packages", fileSystem.ElementAt(1).Path);
        }

        [Test]
        public void InMemoryFileSystemHandlesExtraneousPathSeparatorsOnUnixSystemPaths()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Unix, @"/any/directory"));

            string directoryPath = @"/home/packages/";
            InMemoryDirectory directory = fileSystem.AddOrGetDirectory(directoryPath);

            Assert.IsNotNull(directory);
            Assert.AreEqual(directoryPath.TrimEnd('/'), directory.Path);
            Assert.AreEqual(FileAttributes.Directory, directory.Attributes);

            // When a directory is added, all parent directories above it are expected
            // to be added.
            Assert.AreEqual(3, fileSystem.Count);
            Assert.IsTrue(fileSystem.All(entry => entry is InMemoryDirectory));

            Assert.AreEqual(@"/", fileSystem.ElementAt(0).Path);
            Assert.AreEqual(@"/home", fileSystem.ElementAt(1).Path);
            Assert.AreEqual(@"/home/packages", fileSystem.ElementAt(2).Path);
        }

        [Test]
        public void InMemoryFileSystemGetsTheExpectedDirectoryByPathOnWindowsSystems()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));

            string directoryPath = @"C:\packages\VirtualClient\1.0.0\content\win-x64";
            InMemoryDirectory expectedDirectory = fileSystem.AddOrGetDirectory(directoryPath);
            InMemoryDirectory actualDirectory = fileSystem.GetDirectory(expectedDirectory.Path);

            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        public void InMemoryFileSystemGetsTheExpectedDirectoriesByPathOnWindowsSystems_RecursiveSearch()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));

            string directoryPath1 = @"C:\packages\VirtualClient\1.0.0\content\win-x64";
            string directoryPath2 = @"C:\packages\VirtualClient\1.0.0\tools";
            string directoryPath3 = @"C:\packages\Other\1.0.0\content\win-x64";

            fileSystem.AddOrGetDirectory(directoryPath1);
            fileSystem.AddOrGetDirectory(directoryPath2);
            fileSystem.AddOrGetDirectory(directoryPath3);

            IEnumerable<InMemoryDirectory> directories = fileSystem.GetDirectories(
                @"C:\packages\VirtualClient",
                SearchOption.AllDirectories);

            Assert.AreEqual(4, directories.Count());

            Assert.AreEqual(@"C:\packages\VirtualClient\1.0.0", directories.ElementAt(0).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient\1.0.0\content", directories.ElementAt(1).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient\1.0.0\content\win-x64", directories.ElementAt(2).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient\1.0.0\tools", directories.ElementAt(3).Path);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void InMemoryFileSystemGetsTheExpectedDirectoriesByPathOnWindowsSystems_TopDirectoryOnly()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));

            string directoryPath1 = @"C:\packages\VirtualClient\1.0.0\content\win-x64";
            string directoryPath2 = @"C:\packages\VirtualClient\1.0.0\tools";
            string directoryPath3 = @"C:\packages\Other\1.0.0\content\win-x64";

            fileSystem.AddOrGetDirectory(directoryPath1);
            fileSystem.AddOrGetDirectory(directoryPath2);
            fileSystem.AddOrGetDirectory(directoryPath3);

            IEnumerable<InMemoryDirectory> directories = fileSystem.GetDirectories(
                @"C:\packages\VirtualClient\1.0.0",
                SearchOption.TopDirectoryOnly);

            Assert.AreEqual(2, directories.Count());
            Assert.AreEqual(@"C:\packages\VirtualClient\1.0.0\content", directories.ElementAt(0).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient\1.0.0\tools", directories.ElementAt(1).Path);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void InMemoryFileSystemGetsTheExpectedDirectoriesByPathOnWindowsSystems_TopDirectoryOnly_Scenario2()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));

            string directoryPath1 = @"C:\packages\VirtualClient\1.0.0\content\win-x64";
            string directoryPath2 = @"C:\tools\any";

            fileSystem.AddOrGetDirectory(directoryPath1);
            fileSystem.AddOrGetDirectory(directoryPath2);

            IEnumerable<InMemoryDirectory> directories = fileSystem.GetDirectories(@"C:", SearchOption.TopDirectoryOnly);

            Assert.AreEqual(2, directories.Count());
            Assert.AreEqual(@"C:\packages", directories.ElementAt(0).Path);
            Assert.AreEqual(@"C:\tools", directories.ElementAt(1).Path);

            // Should handle the trailing backslash as well.
            directories = fileSystem.GetDirectories(@"C:\", SearchOption.TopDirectoryOnly);

            Assert.AreEqual(2, directories.Count());
            Assert.AreEqual(@"C:\packages", directories.ElementAt(0).Path);
            Assert.AreEqual(@"C:\tools", directories.ElementAt(1).Path);
        }

        [Test]
        public void InMemoryFileSystemGetsTheExpectedDirectoryByPathOnUnixSystems()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Unix, @"/any/directory"));

            string directoryPath = @"/home/packages/VirtualClient/1.0.0/content/win-x64";
            InMemoryDirectory expectedDirectory = fileSystem.AddOrGetDirectory(directoryPath);
            InMemoryDirectory actualDirectory = fileSystem.GetDirectory(expectedDirectory.Path);

            Assert.AreEqual(expectedDirectory, actualDirectory);
        }

        [Test]
        public void InMemoryFileSystemGetsTheExpectedDirectoriesByPathOnUnixSystems_RecursiveSearch()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Unix, @"/any/directory"));

            string directoryPath1 = @"/home/packages/VirtualClient/1.0.0/content/win-x64";
            string directoryPath2 = @"/home/packages/VirtualClient/1.0.0/tools";
            string directoryPath3 = @"/home/packages/Other/1.0.0/content/win-x64";

            fileSystem.AddOrGetDirectory(directoryPath1);
            fileSystem.AddOrGetDirectory(directoryPath2);
            fileSystem.AddOrGetDirectory(directoryPath3);

            IEnumerable<InMemoryDirectory> directories = fileSystem.GetDirectories(
                @"/home/packages/VirtualClient",
                SearchOption.AllDirectories);

            Assert.AreEqual(4, directories.Count());
            Assert.AreEqual(@"/home/packages/VirtualClient/1.0.0", directories.ElementAt(0).Path);
            Assert.AreEqual(@"/home/packages/VirtualClient/1.0.0/content", directories.ElementAt(1).Path);
            Assert.AreEqual(@"/home/packages/VirtualClient/1.0.0/content/win-x64", directories.ElementAt(2).Path);
            Assert.AreEqual(@"/home/packages/VirtualClient/1.0.0/tools", directories.ElementAt(3).Path);
        }

        [Test]
        public void InMemoryFileSystemGetsTheExpectedDirectoriesByPathOnUnixSystems_TopDirectoryOnly()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Unix, @"/any/directory"));

            string directoryPath1 = @"/home/packages/VirtualClient/1.0.0/content/win-x64";
            string directoryPath2 = @"/home/packages/VirtualClient/1.0.0/tools";
            string directoryPath3 = @"/home/packages/Other/1.0.0/content/win-x64";

            fileSystem.AddOrGetDirectory(directoryPath1);
            fileSystem.AddOrGetDirectory(directoryPath2);
            fileSystem.AddOrGetDirectory(directoryPath3);

            IEnumerable<InMemoryDirectory> directories = fileSystem.GetDirectories(
                @"/home/packages/VirtualClient/1.0.0",
                SearchOption.TopDirectoryOnly);

            Assert.AreEqual(2, directories.Count());
            Assert.AreEqual(@"/home/packages/VirtualClient/1.0.0/content", directories.ElementAt(0).Path);
            Assert.AreEqual(@"/home/packages/VirtualClient/1.0.0/tools", directories.ElementAt(1).Path);
        }

        [Test]
        public void InMemoryFileSystemGetsTheExpectedDirectoriesByPathOnUnixSystems_TopDirectoryOnly_Scenario2()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Unix, @"/any/directory"));

            string directoryPath1 = @"/home/packages/VirtualClient/1.0.0/content/win-x64";
            string directoryPath2 = @"/tools";

            fileSystem.AddOrGetDirectory(directoryPath1);
            fileSystem.AddOrGetDirectory(directoryPath2);

            IEnumerable<InMemoryDirectory> directories = fileSystem.GetDirectories("/", SearchOption.TopDirectoryOnly);

            Assert.AreEqual(2, directories.Count());
            Assert.AreEqual(@"/home", directories.ElementAt(0).Path);
            Assert.AreEqual(@"/tools", directories.ElementAt(1).Path);
        }

        [Test]
        public void InMemoryFileSystemRemovesTheExpectedDirectoriesOnWindowsSystems()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));
            fileSystem.AddOrGetDirectory(@"C:\packages\VirtualClient\1.0.0\content\win-x64");
            fileSystem.AddOrGetDirectory(@"C:\packages\any");

            Assert.AreEqual(7, fileSystem.Count);
            fileSystem.RemoveDirectory(@"C:\packages\VirtualClient\1.0.0\content\win-x64");

            Assert.AreEqual(6, fileSystem.Count());
            Assert.AreEqual(@"C:", fileSystem.ElementAt(0).Path);
            Assert.AreEqual(@"C:\packages", fileSystem.ElementAt(1).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient", fileSystem.ElementAt(2).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient\1.0.0", fileSystem.ElementAt(3).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient\1.0.0\content", fileSystem.ElementAt(4).Path);
            Assert.AreEqual(@"C:\packages\any", fileSystem.ElementAt(5).Path);
        }

        [Test]
        public void InMemoryFileSystemRemovesTheExpectedDirectoriesOnWindowsSystems_2()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));
            fileSystem.AddOrGetDirectory(@"C:\packages\VirtualClient\1.0.0\content\win-x64");
            fileSystem.AddOrGetDirectory(@"C:\packages\any");

            Assert.AreEqual(7, fileSystem.Count);
            fileSystem.RemoveDirectory(@"C:\packages\VirtualClient");

            Assert.AreEqual(3, fileSystem.Count());
            Assert.AreEqual(@"C:", fileSystem.ElementAt(0).Path);
            Assert.AreEqual(@"C:\packages", fileSystem.ElementAt(1).Path);
            Assert.AreEqual(@"C:\packages\any", fileSystem.ElementAt(2).Path);
        }

        [Test]
        public void InMemoryFileSystemRemovesTheExpectedDirectoriesOnWindowsSystems_3()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));
            fileSystem.AddOrGetDirectory(@"C:\packages\VirtualClient\1.0.0\content\win-x64");
            fileSystem.AddOrGetDirectory(@"C:\packages\any");
            fileSystem.AddOrGetDirectory(@"C:\packages\anyOther");

            Assert.AreEqual(8, fileSystem.Count);
            fileSystem.RemoveDirectory(@"C:\packages");

            Assert.AreEqual(1, fileSystem.Count());
            Assert.AreEqual(@"C:", fileSystem.ElementAt(0).Path);
        }

        [Test]
        public void InMemoryFileSystemRemovesTheExpectedDirectoriesOnUnixSystems()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Unix, @"/any/directory"));
            fileSystem.AddOrGetDirectory("/home/packages/VirtualClient/1.0.0/content/win-x64");
            fileSystem.AddOrGetDirectory("/home/packages/any");

            Assert.AreEqual(8, fileSystem.Count);
            fileSystem.RemoveDirectory(@"/home/packages/VirtualClient/1.0.0/content/win-x64");

            Assert.AreEqual(7, fileSystem.Count());
            Assert.AreEqual("/", fileSystem.ElementAt(0).Path);
            Assert.AreEqual("/home", fileSystem.ElementAt(1).Path);
            Assert.AreEqual("/home/packages", fileSystem.ElementAt(2).Path);
            Assert.AreEqual("/home/packages/VirtualClient", fileSystem.ElementAt(3).Path);
            Assert.AreEqual("/home/packages/VirtualClient/1.0.0", fileSystem.ElementAt(4).Path);
            Assert.AreEqual("/home/packages/VirtualClient/1.0.0/content", fileSystem.ElementAt(5).Path);
            Assert.AreEqual("/home/packages/any", fileSystem.ElementAt(6).Path);
        }

        [Test]
        public void InMemoryFileSystemRemovesTheExpectedDirectoriesOnUnixSystems_2()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Unix, @"/any/directory"));
            fileSystem.AddOrGetDirectory("/home/packages/VirtualClient/1.0.0/content/win-x64");
            fileSystem.AddOrGetDirectory("/home/packages/any");

            Assert.AreEqual(8, fileSystem.Count);
            fileSystem.RemoveDirectory(@"/home/packages/VirtualClient");

            Assert.AreEqual(4, fileSystem.Count());
            Assert.AreEqual("/", fileSystem.ElementAt(0).Path);
            Assert.AreEqual("/home", fileSystem.ElementAt(1).Path);
            Assert.AreEqual("/home/packages", fileSystem.ElementAt(2).Path);
            Assert.AreEqual("/home/packages/any", fileSystem.ElementAt(3).Path);
        }

        [Test]
        public void InMemoryFileSystemRemovesTheExpectedDirectoriesOnUnixSystems_3()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Unix, @"/any/directory"));
            fileSystem.AddOrGetDirectory("/home/packages/VirtualClient/1.0.0/content/win-x64");
            fileSystem.AddOrGetDirectory("/home/packages/any");

            Assert.AreEqual(8, fileSystem.Count);
            fileSystem.RemoveDirectory(@"/home/packages");

            Assert.AreEqual(2, fileSystem.Count());
            Assert.AreEqual("/", fileSystem.ElementAt(0).Path);
            Assert.AreEqual("/home", fileSystem.ElementAt(1).Path);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void InMemoryFileSystemAddsExpectedFilesOnWindowsSystems()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));

            string filePath = @"C:\packages\VirtualClient\1.0.0\content\win-x64\VirtualClient.exe";
            InMemoryFile file = fileSystem.AddOrGetFile(filePath);
            InMemoryDirectory directory = fileSystem.GetDirectory(file.Directory.Path);

            Assert.IsNotNull(file);
            Assert.AreEqual("VirtualClient.exe", file.Name);
            Assert.AreEqual(filePath, file.Path);
            Assert.IsTrue(object.ReferenceEquals(directory, file.Directory));
            Assert.AreEqual(FileAttributes.Normal, file.Attributes);

            // When a file is added, all parent directories above it are expected
            // to be added.
            Assert.AreEqual(7, fileSystem.Count);
            Assert.IsTrue(fileSystem.Take(6).All(entry => entry is InMemoryDirectory));

            Assert.AreEqual(@"C:", fileSystem.ElementAt(0).Path);
            Assert.AreEqual(@"C:\packages", fileSystem.ElementAt(1).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient", fileSystem.ElementAt(2).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient\1.0.0", fileSystem.ElementAt(3).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient\1.0.0\content", fileSystem.ElementAt(4).Path);
            Assert.AreEqual(@"C:\packages\VirtualClient\1.0.0\content\win-x64", fileSystem.ElementAt(5).Path);

            Assert.IsInstanceOf<InMemoryFile>(fileSystem.ElementAt(6));
            Assert.IsTrue(object.ReferenceEquals(file, fileSystem.ElementAt(6)));
        }

        [Test]
        public void InMemoryFileSystemAddsExpectedFilesOnUnixSystems()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Unix, @"/any/directory"));

            string filePath = @"/home/packages/VirtualClient/1.0.0/content/win-x64/VirtualClient";
            InMemoryFile file = fileSystem.AddOrGetFile(filePath);
            InMemoryDirectory directory = fileSystem.GetDirectory(file.Directory.Path);

            Assert.IsNotNull(file);
            Assert.AreEqual("VirtualClient", file.Name);
            Assert.AreEqual(filePath, file.Path);
            Assert.IsTrue(object.ReferenceEquals(directory, file.Directory));
            Assert.AreEqual(FileAttributes.Normal, file.Attributes);

            // When a directory is added, all parent directories above it are expected
            // to be added.
            Assert.AreEqual(8, fileSystem.Count);
            Assert.IsTrue(fileSystem.Take(7).All(entry => entry is InMemoryDirectory));

            Assert.AreEqual(@"/", fileSystem.ElementAt(0).Path);
            Assert.AreEqual(@"/home", fileSystem.ElementAt(1).Path);
            Assert.AreEqual(@"/home/packages", fileSystem.ElementAt(2).Path);
            Assert.AreEqual(@"/home/packages/VirtualClient", fileSystem.ElementAt(3).Path);
            Assert.AreEqual(@"/home/packages/VirtualClient/1.0.0", fileSystem.ElementAt(4).Path);
            Assert.AreEqual(@"/home/packages/VirtualClient/1.0.0/content", fileSystem.ElementAt(5).Path);
            Assert.AreEqual(@"/home/packages/VirtualClient/1.0.0/content/win-x64", fileSystem.ElementAt(6).Path);

            Assert.IsInstanceOf<InMemoryFile>(fileSystem.ElementAt(7));
            Assert.IsTrue(object.ReferenceEquals(file, fileSystem.ElementAt(7)));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void InMemoryFileSystemGetsTheExpectedFileByPathOnWindowsSystems()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));

            string filePath = @"C:\packages\VirtualClient\1.0.0\content\win-x64\VirtualClient.exe";
            InMemoryFile expectedFile = fileSystem.AddOrGetFile(filePath);
            InMemoryFile actualFile = fileSystem.GetFile(expectedFile.Path);

            Assert.AreEqual(expectedFile, actualFile);
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void InMemoryFileSystemGetsTheExpectedFilesByPathOnWindowsSystems_RecursiveSearch()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));

            string filePath1 = @"C:\packages\VirtualClient\1.0.0\content\win-x64\VirtualClient.exe";
            string filePath2 = @"C:\packages\VirtualClient\1.0.0\tools\fio.exe";
            string filePath3 = @"C:\packages\Other\1.0.0\content\win-x64\other.exe";

            InMemoryFile file1 = fileSystem.AddOrGetFile(filePath1);
            InMemoryFile file2 = fileSystem.AddOrGetFile(filePath2);
            InMemoryFile file3 = fileSystem.AddOrGetFile(filePath3);

            IEnumerable<InMemoryFile> files = fileSystem.GetFiles(@"C:\packages\VirtualClient", SearchOption.AllDirectories);

            Assert.AreEqual(2, files.Count());
            Assert.IsTrue(object.ReferenceEquals(file1, files.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(file2, files.ElementAt(1)));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void InMemoryFileSystemGetsTheExpectedFilesByPathOnWindowsSystems_RecursiveSearch_2()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));

            string filePath1 = @"C:\packages\VirtualClient\1.0.0\content\win-x64\VirtualClient.exe";
            string filePath2 = @"C:\packages\VirtualClient\1.0.0\tools\fio.exe";
            string filePath3 = @"C:\packages\Other\1.0.0\content\win-x64\other.exe";

            InMemoryFile file1 = fileSystem.AddOrGetFile(filePath1);
            InMemoryFile file2 = fileSystem.AddOrGetFile(filePath2);
            InMemoryFile file3 = fileSystem.AddOrGetFile(filePath3);

            IEnumerable<InMemoryFile> files = fileSystem.GetFiles(@"C:\packages", SearchOption.AllDirectories);

            Assert.AreEqual(3, files.Count());
            Assert.IsTrue(object.ReferenceEquals(file1, files.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(file2, files.ElementAt(1)));
            Assert.IsTrue(object.ReferenceEquals(file3, files.ElementAt(2)));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void InMemoryFileSystemGetsTheExpectedFilesByPathOnWindowsSystems_TopDirectoryOnly()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));

            string filePath1 = @"C:\packages\VirtualClient\VirtualClient.exe";
            string filePath2 = @"C:\packages\VirtualClient\VirtualClient.dll";
            string filePath3 = @"C:\packages\VirtualClient\tools\fio.exe";

            InMemoryFile file1 = fileSystem.AddOrGetFile(filePath1);
            InMemoryFile file2 = fileSystem.AddOrGetFile(filePath2);
            InMemoryFile file3 = fileSystem.AddOrGetFile(filePath3);

            IEnumerable<InMemoryFile> files = fileSystem.GetFiles(@"C:\packages\VirtualClient", SearchOption.TopDirectoryOnly);

            Assert.AreEqual(2, files.Count());
            Assert.IsTrue(object.ReferenceEquals(file1, files.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(file2, files.ElementAt(1)));
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void InMemoryFileSystemGetsTheExpectedFilesByPathOnWindowsSystems_TopDirectoryOnly_2()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Win32NT, @"C:\any\directory"));

            // There aren't any files in the top directory
            string filePath1 = @"C:\packages\VirtualClient\content\win-x64\VirtualClient.exe";
            string filePath2 = @"C:\packages\VirtualClient\content\win-x64\VirtualClient.dll";
            string filePath3 = @"C:\packages\VirtualClient\tools\fio.exe";

            fileSystem.AddOrGetFile(filePath1);
            fileSystem.AddOrGetFile(filePath2);
            fileSystem.AddOrGetFile(filePath3);

            IEnumerable<InMemoryFile> files = fileSystem.GetFiles(@"C:\packages\VirtualClient", SearchOption.TopDirectoryOnly);

            Assert.IsEmpty(files);
        }

        [Test]
        public void InMemoryFileSystemGetsTheExpectedFilesByPathOnUnixSystems_RecursiveSearch()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Unix, @"/any/directory"));

            string filePath1 = @"/home/packages/VirtualClient/1.0.0/content/linux-x64/VirtualClient";
            string filePath2 = @"/home/packages/VirtualClient/1.0.0/tools/fio";
            string filePath3 = @"/home/packages/Other/1.0.0/content/linux-x64/other";

            InMemoryFile file1 = fileSystem.AddOrGetFile(filePath1);
            InMemoryFile file2 = fileSystem.AddOrGetFile(filePath2);
            InMemoryFile file3 = fileSystem.AddOrGetFile(filePath3);

            IEnumerable<InMemoryFile> files = fileSystem.GetFiles(@"/home/packages/VirtualClient", SearchOption.AllDirectories);

            Assert.AreEqual(2, files.Count());
            Assert.IsTrue(object.ReferenceEquals(file1, files.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(file2, files.ElementAt(1)));
        }

        [Test]
        public void InMemoryFileSystemGetsTheExpectedFilesByPathOnUnixSystems_RecursiveSearch_2()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Unix, @"/any/directory"));

            string filePath1 = @"/home/packages/VirtualClient/1.0.0/content/linux-x64/VirtualClient";
            string filePath2 = @"/home/packages/VirtualClient/1.0.0/tools/fio";
            string filePath3 = @"/home/packages/Other/1.0.0/content/linux-x64/other";

            InMemoryFile file1 = fileSystem.AddOrGetFile(filePath1);
            InMemoryFile file2 = fileSystem.AddOrGetFile(filePath2);
            InMemoryFile file3 = fileSystem.AddOrGetFile(filePath3);

            IEnumerable<InMemoryFile> files = fileSystem.GetFiles(@"/home/packages", SearchOption.AllDirectories);

            Assert.AreEqual(3, files.Count());
            Assert.IsTrue(object.ReferenceEquals(file1, files.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(file2, files.ElementAt(1)));
            Assert.IsTrue(object.ReferenceEquals(file3, files.ElementAt(2)));
        }

        [Test]
        public void InMemoryFileSystemGetsTheExpectedFilesByPathOnUnixSystems_TopDirectoryOnly()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Unix, @"/any/directory"));

            string filePath1 = @"/home/packages/VirtualClient/VirtualClient";
            string filePath2 = @"/home/packages/VirtualClient/VirtualClient.exe";
            string filePath3 = @"/home/packages/VirtualClient/other/other.dll";

            InMemoryFile file1 = fileSystem.AddOrGetFile(filePath1);
            InMemoryFile file2 = fileSystem.AddOrGetFile(filePath2);
            InMemoryFile file3 = fileSystem.AddOrGetFile(filePath3);

            IEnumerable<InMemoryFile> files = fileSystem.GetFiles(@"/home/packages/VirtualClient", SearchOption.TopDirectoryOnly);

            Assert.AreEqual(2, files.Count());
            Assert.IsTrue(object.ReferenceEquals(file1, files.ElementAt(0)));
            Assert.IsTrue(object.ReferenceEquals(file2, files.ElementAt(1)));
        }

        [Test]
        public void InMemoryFileSystemGetsTheExpectedFilesByPathOnUnixSystems_TopDirectoryOnly_2()
        {
            InMemoryFileSystem fileSystem = new InMemoryFileSystem(new TestPlatformSpecifics(PlatformID.Unix, @"/any/directory"));

            string filePath1 = @"/home/packages/VirtualClient/content/linux-x64/VirtualClient";
            string filePath2 = @"/home/packages/VirtualClient/content/linux-x64/VirtualClient.dll";
            string filePath3 = @"/home/packages/VirtualClient/other/other.dll";

            fileSystem.AddOrGetFile(filePath1);
            fileSystem.AddOrGetFile(filePath2);
            fileSystem.AddOrGetFile(filePath3);

            IEnumerable<InMemoryFile> files = fileSystem.GetFiles(@"/home/packages/VirtualClient", SearchOption.TopDirectoryOnly);
            Assert.IsEmpty(files);
        }
    }
}
