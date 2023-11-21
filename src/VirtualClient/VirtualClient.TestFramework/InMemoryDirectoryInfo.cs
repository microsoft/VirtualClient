// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;
    using System.Security.AccessControl;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Implements an in-memory file system interface for testing scenarios. This implementation
    /// is for directory-specific scenarios.
    /// </summary>
    /// <remarks>
    /// Note that we are NOT implementing all properties and methods of the interface purposefully.
    /// We are implementing ONLY the ones that we need and use typically in the Virtual Client
    /// codebase and testing scenarios. This minimizes the number of issues we would have directly
    /// caused by test/mock implementations that are either incorrect or overly assumptive.
    /// </remarks>
    public class InMemoryDirectoryInfo : IDirectoryInfo
    {
        private InMemoryDirectory directory;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryDirectoryInfo"/> class.
        /// </summary>
        public InMemoryDirectoryInfo(InMemoryFileSystem fileSystem, string directoryPath)
        {
            fileSystem.ThrowIfNull(nameof(fileSystem));
            directoryPath.ThrowIfNullOrWhiteSpace(nameof(directoryPath));

            this.FullName = directoryPath;
            this.Name = Path.GetFileName(directoryPath);
            this.FileSystem = fileSystem;
            this.Attributes = FileAttributes.Directory;
            this.CreationTime = DateTime.MinValue.ToUniversalTime();
            this.CreationTimeUtc = DateTime.MinValue.ToUniversalTime();

            if (fileSystem.TryGetDirectory(directoryPath, out InMemoryDirectory existingDirectory))
            {
                this.directory = existingDirectory;
            }

            string[] pathSegments = InMemoryFileSystem.GetPathSegments(directoryPath);
            string parentDirectory = null;
            string root = pathSegments[0];

            if (pathSegments.Length > 1)
            {
                parentDirectory = fileSystem.PlatformSpecifics.Combine(pathSegments.Take(pathSegments.Length - 1).ToArray());
            }

            if (!string.IsNullOrEmpty(parentDirectory))
            {
                this.Parent = new InMemoryDirectoryInfo(fileSystem, parentDirectory);
            }

            if (!string.IsNullOrWhiteSpace(root) && root != directoryPath)
            {
                this.Root = new InMemoryDirectoryInfo(fileSystem, root);
            }
            else
            {
                this.Root = this;
            }
        }

        /// <summary>
        /// The parent directory of this directory.
        /// </summary>
        public IDirectoryInfo Parent { get; }

        /// <summary>
        /// The root directory of this directory.
        /// </summary>
        public IDirectoryInfo Root { get; }

        /// <summary>
        /// The file system provider.
        /// </summary>
        public IFileSystem FileSystem { get; }

        /// <inheritdoc />
        public FileAttributes Attributes
        {
            get
            {
                return this.directory != null
                    ? this.directory.Attributes
                    : FileAttributes.Directory;
            }

            set
            {
                if (this.directory != null)
                {
                    this.directory.Attributes = value;
                }
            }
        }

        /// <inheritdoc />
        public DateTime CreationTime
        {
            get
            {
                return this.directory != null
                    ? this.directory.CreatedTime
                    : DateTime.MinValue;
            }

            set
            {
                if (this.directory != null)
                {
                    this.directory.CreatedTime = value;
                }
            }
        }

        /// <inheritdoc />
        public DateTime CreationTimeUtc
        {
            get
            {
                return this.directory != null
                    ? this.directory.CreatedTime.ToUniversalTime()
                    : DateTime.MinValue.ToUniversalTime();
            }

            set
            {
                if (this.directory != null)
                {
                    this.directory.CreatedTime = value.ToUniversalTime();
                }
            }
        }

        /// <inheritdoc />
        public bool Exists
        {
            get
            {
                return (this.FileSystem as InMemoryFileSystem).TryGetDirectory(this.FullName, out InMemoryDirectory directory);
            }
        }

        /// <inheritdoc />
        public string Extension { get; } = null;

        /// <inheritdoc />
        public string FullName { get; }

        /// <inheritdoc />
        public DateTime LastAccessTime
        {
            get
            {
                return this.directory != null
                    ? this.directory.LastAccessTime
                    : DateTime.MinValue;
            }

            set
            {
                if (this.directory != null)
                {
                    this.directory.LastAccessTime = value;
                }
            }
        }

        /// <inheritdoc />
        public DateTime LastAccessTimeUtc
        {
            get
            {
                return this.directory != null
                    ? this.directory.LastAccessTime.ToUniversalTime()
                    : DateTime.MinValue.ToUniversalTime();
            }

            set
            {
                if (this.directory != null)
                {
                    this.directory.LastAccessTime = value.ToUniversalTime();
                }
            }
        }

        /// <inheritdoc />
        public DateTime LastWriteTime
        {
            get
            {
                return this.directory != null
                    ? this.directory.LastWriteTime
                    : DateTime.MinValue;
            }

            set
            {
                if (this.directory != null)
                {
                    this.directory.LastWriteTime = value;
                }
            }
        }

        /// <inheritdoc />
        public DateTime LastWriteTimeUtc
        {
            get
            {
                return this.directory != null
                    ? this.directory.LastWriteTime.ToUniversalTime()
                    : DateTime.MinValue.ToUniversalTime();
            }

            set
            {
                if (this.directory != null)
                {
                    this.directory.LastWriteTime = value.ToUniversalTime();
                }
            }
        }

        /// <inheritdoc />
        public string Name { get; }

        public string LinkTarget { get; }

        UnixFileMode IFileSystemInfo.UnixFileMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        /// <inheritdoc />
        public void Create()
        {
            (this.FileSystem as InMemoryFileSystem).AddOrGetDirectory(this.FullName);
        }

        /// <inheritdoc />
        public void Create(DirectorySecurity directorySecurity)
        {
            (this.FileSystem as InMemoryFileSystem).AddOrGetDirectory(this.FullName);
        }

        public void CreateAsSymbolicLink(string pathToTarget)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IDirectoryInfo CreateSubdirectory(string path)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Delete(bool recursive)
        {
            this.Delete();
        }

        /// <inheritdoc />
        public void Delete()
        {
            if (this.directory != null)
            {
                (this.FileSystem as InMemoryFileSystem).RemoveDirectory(this.FullName);
            }
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IEnumerable<IDirectoryInfo> EnumerateDirectories()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IEnumerable<IDirectoryInfo> EnumerateDirectories(string searchPattern)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IEnumerable<IDirectoryInfo> EnumerateDirectories(string searchPattern, SearchOption searchOption)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IEnumerable<IDirectoryInfo> EnumerateDirectories(string searchPattern, EnumerationOptions enumerationOptions)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IEnumerable<IFileInfo> EnumerateFiles()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IEnumerable<IFileInfo> EnumerateFiles(string searchPattern)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, SearchOption searchOption)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IEnumerable<IFileInfo> EnumerateFiles(string searchPattern, EnumerationOptions enumerationOptions)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos(string searchPattern)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos(string searchPattern, SearchOption searchOption)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IEnumerable<IFileSystemInfo> EnumerateFileSystemInfos(string searchPattern, EnumerationOptions enumerationOptions)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public DirectorySecurity GetAccessControl()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public DirectorySecurity GetAccessControl(AccessControlSections includeSections)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IDirectoryInfo[] GetDirectories()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IDirectoryInfo[] GetDirectories(string searchPattern)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IDirectoryInfo[] GetDirectories(string searchPattern, SearchOption searchOption)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IDirectoryInfo[] GetDirectories(string searchPattern, EnumerationOptions enumerationOptions)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IFileInfo[] GetFiles()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IFileInfo[] GetFiles(string searchPattern)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IFileInfo[] GetFiles(string searchPattern, SearchOption searchOption)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IFileInfo[] GetFiles(string searchPattern, EnumerationOptions enumerationOptions)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IFileSystemInfo[] GetFileSystemInfos()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IFileSystemInfo[] GetFileSystemInfos(string searchPattern)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IFileSystemInfo[] GetFileSystemInfos(string searchPattern, SearchOption searchOption)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IFileSystemInfo[] GetFileSystemInfos(string searchPattern, EnumerationOptions enumerationOptions)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public void MoveTo(string destDirName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public void Refresh()
        {
            throw new NotImplementedException();
        }

        public IFileSystemInfo ResolveLinkTarget(bool returnFinalTarget)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public void SetAccessControl(DirectorySecurity directorySecurity)
        {
            throw new NotImplementedException();
        }
    }
}
