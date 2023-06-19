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
    using System.Text.RegularExpressions;
    using Moq;
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
    public class InMemoryDirectoryIntegration : IDirectory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryDirectoryIntegration"/> class.
        /// </summary>
        public InMemoryDirectoryIntegration(InMemoryFileSystem fileSystem)
        {
            fileSystem.ThrowIfNull(nameof(fileSystem));
            this.FileSystem = fileSystem;
        }

        /// <summary>
        /// The file system implementation itself.
        /// </summary>
        public IFileSystem FileSystem { get; }

        public Mock<IDirectory> Mock { get; }

        /// <inheritdoc />
        public IDirectoryInfo CreateDirectory(string path)
        {
            (this.FileSystem as InMemoryFileSystem).OnCreateDirectory?.Invoke(path);
            InMemoryDirectory directory = (this.FileSystem as InMemoryFileSystem).AddOrGetDirectory(path);
            return new InMemoryDirectoryInfo((this.FileSystem as InMemoryFileSystem), directory.Path);
        }

        /// <inheritdoc />
        public IDirectoryInfo CreateDirectory(string path, DirectorySecurity directorySecurity)
        {
            return this.CreateDirectory(path);
        }

        public IFileSystemInfo CreateSymbolicLink(string path, string pathToTarget)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Delete(string path)
        {
            this.Delete(path, true);
        }

        /// <inheritdoc />
        public void Delete(string path, bool recursive)
        {
            (this.FileSystem as InMemoryFileSystem).OnDeleteDirectory?.Invoke(path);
            (this.FileSystem as InMemoryFileSystem).RemoveDirectory(path);
        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateDirectories(string path)
        {
            return this.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);

        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern)
        {
            return this.GetDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            return this.GetDirectories(path, searchPattern, searchOption);
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateFiles(string path)
        {
            return this.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern)
        {
            return this.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            return this.GetFiles(path, searchPattern, searchOption);
        }

        /// <summary>
        /// Not Implemented
        /// </summary>
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern, EnumerationOptions enumerationOptions)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateFileSystemEntries(string path)
        {
            return this.GetDirectories(path)?.Union(this.GetFiles(path));
        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern)
        {
            return this.GetDirectories(path, searchPattern)?.Union(this.GetFiles(path, searchPattern));
        }

        /// <inheritdoc />
        public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
        {
            return this.GetDirectories(path, searchPattern, searchOption)?.Union(this.GetFiles(path, searchPattern, searchOption));
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Exists(string path)
        {
            (this.FileSystem as InMemoryFileSystem).OnDirectoryExists?.Invoke(path);
            return (this.FileSystem as InMemoryFileSystem).TryGetDirectory(path, out InMemoryDirectory directory);
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public DirectorySecurity GetAccessControl(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public DirectorySecurity GetAccessControl(string path, AccessControlSections includeSections)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public DateTime GetCreationTime(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public DateTime GetCreationTimeUtc(string path)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public string GetCurrentDirectory()
        {
            return (this.FileSystem as InMemoryFileSystem).PlatformSpecifics.CurrentDirectory;
        }

        /// <inheritdoc />
        public string[] GetDirectories(string path)
        {
            return this.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
        }

        /// <inheritdoc />
        public string[] GetDirectories(string path, string searchPattern)
        {
            return this.GetDirectories(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        /// <inheritdoc />
        public string[] GetDirectories(string path, string searchPattern, SearchOption searchOption)
        {
            string normalizedSearchPattern = InMemoryFileSystem.ConvertToRegularExpression(
                !string.IsNullOrEmpty(searchPattern) ? searchPattern : "*");

            List<string> matchingPaths = new List<string>();
            IEnumerable<InMemoryDirectory> directories = (this.FileSystem as InMemoryFileSystem).GetDirectories(path, searchOption);

            RegexOptions options = (this.FileSystem as InMemoryFileSystem).Platform == PlatformID.Win32NT
                ? RegexOptions.IgnoreCase
                : RegexOptions.None;

            Regex expression = new Regex(normalizedSearchPattern, options);
            foreach (InMemoryDirectory directory in directories)
            {
                if (expression.IsMatch(directory.Path))
                {
                    matchingPaths.Add(directory.Path);
                }
            }

            return matchingPaths.OrderBy(path => path).ToArray();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public string[] GetDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public string GetDirectoryRoot(string path)
        {
            return InMemoryFileSystem.GetPathSegments(path)?.FirstOrDefault();
        }

        /// <inheritdoc />
        public string[] GetFiles(string path)
        {
            return this.GetFiles(path, "*", SearchOption.TopDirectoryOnly);
        }

        /// <inheritdoc />
        public string[] GetFiles(string path, string searchPattern)
        {
            return this.GetFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        /// <inheritdoc />
        public string[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            string normalizedSearchPattern = InMemoryFileSystem.ConvertToRegularExpression(
                !string.IsNullOrEmpty(searchPattern) ? searchPattern : "*");

            List<string> matchingPaths = new List<string>();
            IEnumerable<InMemoryFile> files = (this.FileSystem as InMemoryFileSystem).GetFiles(path, searchOption);

            RegexOptions options = (this.FileSystem as InMemoryFileSystem).Platform == PlatformID.Win32NT
                ? RegexOptions.IgnoreCase
                : RegexOptions.None;

            Regex expression = new Regex(normalizedSearchPattern, options);
            foreach (InMemoryFile file in files)
            {
                if (expression.IsMatch(file.Path))
                {
                    matchingPaths.Add(file.Path);
                }
            }

            return matchingPaths.OrderBy(path => path).ToArray();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public string[] GetFiles(string path, string searchPattern, EnumerationOptions enumerationOptions)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public string[] GetFileSystemEntries(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public string[] GetFileSystemEntries(string path, string searchPattern)
        {
            throw new NotImplementedException();
        }

        public string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption)
        {
            throw new NotImplementedException();
        }

        public string[] GetFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public DateTime GetLastAccessTime(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public DateTime GetLastAccessTimeUtc(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public DateTime GetLastWriteTime(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public DateTime GetLastWriteTimeUtc(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public string[] GetLogicalDrives()
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IDirectoryInfo GetParent(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Move(string sourceDirName, string destDirName)
        {
            throw new NotImplementedException();
        }

        public IFileSystemInfo ResolveLinkTarget(string linkPath, bool returnFinalTarget)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void SetAccessControl(string path, DirectorySecurity directorySecurity)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void SetCreationTime(string path, DateTime creationTime)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void SetCurrentDirectory(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void SetLastAccessTime(string path, DateTime lastAccessTime)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void SetLastWriteTime(string path, DateTime lastWriteTime)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        {
            throw new NotImplementedException();
        }
    }
}
