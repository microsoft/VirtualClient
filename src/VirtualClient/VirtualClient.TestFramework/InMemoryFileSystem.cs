// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.IO.Abstractions;
    using System.Linq;

    /// <summary>
    /// Implements an in-memory file system interface for testing scenarios.
    /// </summary>
    /// <remarks>
    /// Note that we are NOT implementing all properties and methods of the interface purposefully.
    /// We are implementing ONLY the ones that we need and use typically in the Virtual Client
    /// codebase and testing scenarios. This minimizes the number of issues we would have directly
    /// caused by test/mock implementations that are either incorrect or overly assumptive.
    /// </remarks>
    public class InMemoryFileSystem : IEnumerable<InMemoryFileSystemEntry>, IFileSystem
    {
        private StringComparison pathCaseSensitivity;
        private HashSet<InMemoryFileSystemEntry> fileSystemEntries;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryFileSystem"/> class.
        /// </summary>
        public InMemoryFileSystem(TestPlatformSpecifics platformSpecifics)
        {
            this.fileSystemEntries = new HashSet<InMemoryFileSystemEntry>(new FileSystemEntryReferenceComparer(platformSpecifics.Platform));
            this.PlatformSpecifics = platformSpecifics;
            this.File = new InMemoryFileIntegration(this);
            this.Directory = new InMemoryDirectoryIntegration(this);
            this.pathCaseSensitivity = this.PlatformSpecifics.Platform == PlatformID.Win32NT
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;
        }

        /// <summary>
        /// The count of all file system entries.
        /// </summary>
        public int Count
        {
            get
            {
                return this.fileSystemEntries.Count;
            }
        }

        /// <summary>
        /// Represents an in-memory test file interface.
        /// </summary>
        public IFile File { get; }

        /// <summary>
        /// Represents an in-memory test directory interface.
        /// </summary>
        public IDirectory Directory { get; }

        /// <summary>
        /// Not implemented.
        /// </summary>
        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "We do not need to implement this entire interface.")]
        public IFileInfoFactory FileInfo => throw new NotImplementedException();

        /// <summary>
        /// Not implemented.
        /// </summary>
        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "We do not need to implement this entire interface.")]
        public IFileStreamFactory FileStream => throw new NotImplementedException();

        /// <summary>
        /// Not implemented.
        /// </summary>
        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "We do not need to implement this entire interface.")]
        public IPath Path => throw new NotImplementedException();

        /// <summary>
        /// The OS platform (e.g. Windows, Unix).
        /// </summary>
        public PlatformID Platform
        {
            get
            {
                return this.PlatformSpecifics.Platform;
            }
        }

        /// <summary>
        /// Provides cross-platform (e.g. Windows, Unix) specific information.
        /// </summary>
        public TestPlatformSpecifics PlatformSpecifics { get; }

        /// <summary>
        /// Not implemented.
        /// </summary>
        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "We do not need to implement this entire interface.")]
        public IDirectoryInfoFactory DirectoryInfo => throw new NotImplementedException();

        /// <summary>
        /// Not implemented.
        /// </summary>
        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "We do not need to implement this entire interface.")]
        public IDriveInfoFactory DriveInfo => throw new NotImplementedException();

        /// <summary>
        /// Not implemented.
        /// </summary>
        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "We do not need to implement this entire interface.")]
        public IFileSystemWatcherFactory FileSystemWatcher => throw new NotImplementedException();

        /// <summary>
        /// All file system entries (file and directory) defined for the in-memory
        /// file system.
        /// </summary>
        public IEnumerable<InMemoryFileSystemEntry> FileSystemEntries
        {
            get
            {
                return this.fileSystemEntries;
            }
        }

        /// <summary>
        /// Event handler is invoked after a directory is created
        /// (Arguments = directoryPath).
        /// </summary>
        public Action<string> OnCreateDirectory { get; set; }

        /// <summary>
        /// Event handler is invoked after a file is created
        /// (Arguments = filePath).
        /// </summary>
        public Action<string> OnCreateFile { get; set; }

        /// <summary>
        /// Event handler is invoked before a directory is deleted
        /// (Arguments = directoryPath).
        /// </summary>
        public Action<string> OnDeleteDirectory { get; set; }

        /// <summary>
        /// Event handler is invoked before a file is deleted
        /// (Arguments = filePath).
        /// </summary>
        public Action<string> OnDeleteFile { get; set; }

        /// <summary>
        /// Event handler is invoked when a directory is being verified as existing.
        /// (Arguments = directoryPath).
        /// </summary>
        public Action<string> OnDirectoryExists { get; set; }

        /// <summary>
        /// Event handler is invoked when a file is being verified as existing.
        /// (Arguments = filePath).
        /// </summary>
        public Action<string> OnFileExists { get; set; }

        /// <summary>
        /// Event handler is invoked after a file is read.
        /// (Arguments = filePath).
        /// </summary>
        public Action<string> OnReadFile { get; set; }

        /// <summary>
        /// Event handler is invoked after a file is written to including appends.
        /// (Arguments = filePath, contents).
        /// </summary>
        public Action<string, byte[]> OnWriteFile { get; set; }

        [SuppressMessage("Design", "CA1065:Do not raise exceptions in unexpected locations", Justification = "We are not implementing support for everything.")]
        public IFileVersionInfoFactory FileVersionInfo => throw new NotImplementedException();

        /// <summary>
        /// Clears/removes all directories and files from the in-memory file system.
        /// </summary>
        public void Clear()
        {
            this.fileSystemEntries.Clear();
        }

        /// <summary>
        /// Gets an enumerator for all file system entries (file and directory) in the
        /// in-memory file system.
        /// </summary>
        public IEnumerator<InMemoryFileSystemEntry> GetEnumerator()
        {
            return this.fileSystemEntries.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        internal static string ConvertToRegularExpression(string searchPattern)
        {
            string normalizedSearchPattern = searchPattern ?? "*.*";

            if (normalizedSearchPattern == "*.*")
            {
                return ".";
            }
            else
            {
                // We are converting a normal search pattern that may contain wildcards '*' and
                // periods to a regular expression form (e.g. *.txt -> .*\.txt)
                return normalizedSearchPattern.Replace("*", ".*").Replace(".", "\\.");
            }
        }

        internal InMemoryDirectory AddOrGetDirectory(string path)
        {
            string normalizedPath = this.PlatformSpecifics.StandardizePath(path);

            InMemoryDirectory directory;
            if (!this.TryGetDirectory(normalizedPath, out directory))
            {
                string pathSeparator = this.Platform == PlatformID.Win32NT ? "\\" : "/";
                string[] pathSegments = InMemoryFileSystem.GetPathSegments(normalizedPath);

                for (int segment = 0; segment < pathSegments.Length; segment++)
                {
                    string directoryPath = string.Join(pathSeparator, pathSegments.Take(segment + 1));
                    string parentDirectory = this.PlatformSpecifics.StandardizePath(System.IO.Path.GetDirectoryName(directoryPath));

                    if (this.Platform == PlatformID.Unix)
                    {
                        if (!directoryPath.StartsWith(pathSeparator))
                        {
                            directoryPath = $"{pathSeparator}{directoryPath}";
                        }

                        if (directoryPath != pathSeparator && string.IsNullOrWhiteSpace(parentDirectory))
                        {
                            parentDirectory = pathSeparator;
                        }
                    }

                    directory = new InMemoryDirectory(directoryPath, parentDirectory);
                    this.fileSystemEntries.Add(directory);
                }
            }

            return directory;
        }

        internal InMemoryFile AddOrGetFile(string path)
        {
            string normalizedDirectoryPath = this.PlatformSpecifics.StandardizePath(System.IO.Path.GetDirectoryName(path));
            InMemoryDirectory directory = this.AddOrGetDirectory(normalizedDirectoryPath);

            InMemoryFile file;
            if (!this.TryGetFile(path, out file))
            {
                file = new InMemoryFile(path, directory);
                this.fileSystemEntries.Add(file);
            }

            return file;
        }

        internal InMemoryDirectory GetDirectory(string path)
        {
            string normalizedPath = this.PlatformSpecifics.StandardizePath(path);
            if (!this.TryGetDirectory(normalizedPath, out InMemoryDirectory directory))
            {
                throw new DirectoryNotFoundException($"Directory not found '{path}'.");
            }

            return directory;
        }

        internal IEnumerable<InMemoryDirectory> GetDirectories(string path, SearchOption searchOption)
        {
            string normalizedPath = this.PlatformSpecifics.StandardizePath(path);
            List<InMemoryDirectory> directories = new List<InMemoryDirectory>();
            if (this.TryGetDirectory(normalizedPath, out InMemoryDirectory directory))
            {
                foreach (var entry in this.fileSystemEntries)
                {
                    InMemoryDirectory existingDirectory = (entry as InMemoryDirectory);

                    if (existingDirectory != null
                        && existingDirectory.Path.StartsWith(normalizedPath, this.pathCaseSensitivity)
                        && !string.Equals(existingDirectory.Path, normalizedPath, this.pathCaseSensitivity))
                    {
                        if (searchOption == SearchOption.AllDirectories)
                        {
                            directories.Add(existingDirectory);
                        }
                        else if (searchOption == SearchOption.TopDirectoryOnly 
                            && string.Equals(existingDirectory.ParentPath, normalizedPath, this.pathCaseSensitivity))
                        {
                            directories.Add(existingDirectory);
                        }
                    }
                }
            }

            return directories;
        }

        internal InMemoryFile GetFile(string path)
        {
            if (!this.TryGetFile(path, out InMemoryFile file))
            {
                throw new FileNotFoundException($"File not found '{path}'.");
            }

            return file;
        }

        internal IEnumerable<InMemoryFile> GetFiles(string directoryPath, SearchOption searchOption)
        {
            string normalizedPath = this.PlatformSpecifics.StandardizePath(directoryPath);

            List<InMemoryFile> files = new List<InMemoryFile>();
            if (this.TryGetDirectory(normalizedPath, out InMemoryDirectory directory))
            {
                foreach (var entry in this.fileSystemEntries)
                {
                    InMemoryFile existingFile = (entry as InMemoryFile);

                    if (existingFile != null && existingFile.Path.StartsWith(normalizedPath, this.pathCaseSensitivity))
                    {
                        if (searchOption == SearchOption.AllDirectories)
                        {
                            files.Add(existingFile);
                        }
                        else if (searchOption == SearchOption.TopDirectoryOnly
                            && string.Equals(existingFile.Directory.Path, normalizedPath, this.pathCaseSensitivity))
                        {
                            files.Add(existingFile);
                        }
                    }
                }
            }

            return files;
        }

        internal static string[] GetPathSegments(string path)
        {
            return path.Split(new string[] { "\\", "/" }, StringSplitOptions.TrimEntries);
        }

        internal void RemoveDirectory(string path)
        {
            string normalizedPath = this.PlatformSpecifics.StandardizePath(path);
            List<InMemoryFileSystemEntry> entriesToRemove = new List<InMemoryFileSystemEntry>();
            foreach (var entry in this.fileSystemEntries)
            {
                if (entry.Path.StartsWith(normalizedPath, this.pathCaseSensitivity))
                {
                    entriesToRemove.Add(entry);
                }
            }

            foreach (var entry in entriesToRemove)
            {
                this.fileSystemEntries.Remove(entry);
            }
        }

        internal void RemoveFile(string path)
        {
            if (this.TryGetFile(path, out InMemoryFile file))
            {
                this.fileSystemEntries.Remove(file);
            }
        }

        internal bool TryGetDirectory(string path, out InMemoryDirectory directory)
        {
            string normalizedPath = this.PlatformSpecifics.StandardizePath(path);

            directory = null;
            foreach (var entry in this.fileSystemEntries)
            {
                if (entry is InMemoryDirectory && string.Equals(entry.Path, normalizedPath, this.pathCaseSensitivity))
                {
                    directory = entry as InMemoryDirectory;
                    break;
                }
            }

            return directory != null;
        }

        internal bool TryGetFile(string path, out InMemoryFile file)
        {
            string normalizedPath = this.PlatformSpecifics.StandardizePath(path);

            file = null;
            foreach (var entry in this.fileSystemEntries)
            {
                if (entry is InMemoryFile && string.Equals(entry.Path, normalizedPath, this.pathCaseSensitivity))
                {
                    file = entry as InMemoryFile;
                    break;
                }
            }

            return file != null;
        }

        private class FileSystemEntryReferenceComparer : IEqualityComparer<InMemoryFileSystemEntry>
        {
            private StringComparison pathCaseSensitivity;

            public FileSystemEntryReferenceComparer(PlatformID platform)
            {
                this.pathCaseSensitivity = platform == PlatformID.Win32NT
                    ? StringComparison.OrdinalIgnoreCase
                    : StringComparison.Ordinal;
            }

            public bool Equals(InMemoryFileSystemEntry x, InMemoryFileSystemEntry y)
            {
                return string.Equals(x.Path, y.Path, this.pathCaseSensitivity);
            }

            public int GetHashCode(InMemoryFileSystemEntry obj)
            {
                return obj.Path.GetHashCode(this.pathCaseSensitivity);
            }
        }
    }
}
