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
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Win32.SafeHandles;
    using VirtualClient.Common.Extensions;

    /// <summary>
    /// Implements an in-memory implementation of a file system file object. This implementation
    /// is for file-specific scenarios.
    /// </summary>
    /// <remarks>
    /// Note that we are NOT implementing all properties and methods of the interface purposefully.
    /// We are implementing ONLY the ones that we need and use typically in the Virtual Client
    /// codebase and testing scenarios. This minimizes the number of issues we would have directly
    /// caused by test/mock implementations that are either incorrect or overly assumptive.
    /// </remarks>
    public class InMemoryFileIntegration : IFile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryFileIntegration"/> class.
        /// </summary>
        public InMemoryFileIntegration(InMemoryFileSystem fileSystem)
        {
            fileSystem.ThrowIfNull(nameof(fileSystem));
            this.FileSystem = fileSystem;
        }

        /// <summary>
        /// The file system implementation itself.
        /// </summary>
        public IFileSystem FileSystem { get; }

        public void AppendAllBytes(string path, byte[] bytes)
        {
            throw new NotImplementedException();
        }

        public void AppendAllBytes(string path, ReadOnlySpan<byte> bytes)
        {
            throw new NotImplementedException();
        }

        public Task AppendAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task AppendAllBytesAsync(string path, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void AppendAllLines(string path, IEnumerable<string> contents)
        {
            this.AppendAllLines(path, contents, InMemoryFile.DefaultEncoding);
        }

        /// <inheritdoc />
        public void AppendAllLines(string path, IEnumerable<string> contents, Encoding encoding)
        {
            this.AppendAllText(path, string.Join(Environment.NewLine, contents), encoding);
        }

        /// <inheritdoc />
        public Task AppendAllLinesAsync(string path, IEnumerable<string> contents, CancellationToken cancellationToken = default)
        {
            return this.AppendAllLinesAsync(path, contents, InMemoryFile.DefaultEncoding, cancellationToken);
        }

        /// <inheritdoc />
        public Task AppendAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default)
        {
            this.AppendAllLines(path, contents, encoding);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void AppendAllText(string path, string contents)
        {
            this.AppendAllText(path, contents, InMemoryFile.DefaultEncoding);
        }

        /// <inheritdoc />
        public void AppendAllText(string path, string contents, Encoding encoding)
        {
            InMemoryFile file = (this.FileSystem as InMemoryFileSystem).AddOrGetFile(path);
            byte[] fileContents = encoding.GetBytes(contents);
            file.AppendContent(fileContents, encoding);
            (this.FileSystem as InMemoryFileSystem).OnWriteFile?.Invoke(path, fileContents);
        }

        public void AppendAllText(string path, ReadOnlySpan<char> contents)
        {
            throw new NotImplementedException();
        }

        public void AppendAllText(string path, ReadOnlySpan<char> contents, Encoding encoding)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task AppendAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        {
            return this.AppendAllTextAsync(path, contents, InMemoryFile.DefaultEncoding, cancellationToken);
        }

        /// <inheritdoc />
        public Task AppendAllTextAsync(string path, string contents, Encoding encoding, CancellationToken cancellationToken = default)
        {
            this.AppendAllText(path, contents, encoding);
            return Task.CompletedTask;
        }

        public Task AppendAllTextAsync(string path, ReadOnlyMemory<char> contents, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task AppendAllTextAsync(string path, ReadOnlyMemory<char> contents, Encoding encoding, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public StreamWriter AppendText(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Copy(string sourceFileName, string destFileName)
        {
            this.Copy(sourceFileName, destFileName, false);
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Copy(string sourceFileName, string destFileName, bool overwrite)
        {
            InMemoryFile file = (this.FileSystem as InMemoryFileSystem).GetFile(sourceFileName);

            if ((this.FileSystem as InMemoryFileSystem).TryGetFile(destFileName, out InMemoryFile existingFile))
            {
                if (!overwrite)
                {
                    throw new IOException($"File at path '{destFileName}' already exists.");
                }

                (this.FileSystem as InMemoryFileSystem).RemoveFile(destFileName);
            }

            InMemoryFile fileCopy = (this.FileSystem as InMemoryFileSystem).AddOrGetFile(destFileName);

            if (file.FileBytes?.Any() == true)
            {
                fileCopy.SetContent(file.FileBytes.ToArray(), file.ContentEncoding);
            }
        }

        /// <inheritdoc />
        public FileSystemStream Create(string path)
        {
            FileSystemStream stream = this.Open(path, FileMode.CreateNew);
            (this.FileSystem as InMemoryFileSystem).OnCreateFile?.Invoke(path);
            return stream;
        }

        /// <inheritdoc />
        public FileSystemStream Create(string path, int bufferSize)
        {
            FileSystemStream stream = this.Open(path, FileMode.CreateNew);
            (this.FileSystem as InMemoryFileSystem).OnCreateFile?.Invoke(path);
            return stream;
        }

        /// <inheritdoc />
        public FileSystemStream Create(string path, int bufferSize, FileOptions options)
        {
            FileSystemStream stream = this.Open(path, FileMode.CreateNew);
            (this.FileSystem as InMemoryFileSystem).OnCreateFile?.Invoke(path);
            return stream;
        }

        public IFileSystemInfo CreateSymbolicLink(string path, string pathToTarget)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public StreamWriter CreateText(string path)
        {
            StreamWriter writer = new StreamWriter(this.Open(path, FileMode.CreateNew));
            (this.FileSystem as InMemoryFileSystem).OnCreateFile?.Invoke(path);
            return writer;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Decrypt(string path)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void Delete(string path)
        {
            (this.FileSystem as InMemoryFileSystem).OnDeleteFile?.Invoke(path);
            (this.FileSystem as InMemoryFileSystem).RemoveFile(path);
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Encrypt(string path)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public bool Exists(string path)
        {
            (this.FileSystem as InMemoryFileSystem).OnFileExists?.Invoke(path);
            return (this.FileSystem as InMemoryFileSystem).TryGetFile(path, out InMemoryFile file);
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public FileSecurity GetAccessControl(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public FileSecurity GetAccessControl(string path, AccessControlSections includeSections)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public FileAttributes GetAttributes(string path)
        {
            return (this.FileSystem as InMemoryFileSystem).GetFile(path).Attributes;
        }

        /// <inheritdoc />
        public DateTime GetCreationTime(string path)
        {
            return (this.FileSystem as InMemoryFileSystem).GetFile(path).CreatedTime;
        }

        /// <inheritdoc />
        public DateTime GetCreationTimeUtc(string path)
        {
            return (this.FileSystem as InMemoryFileSystem).GetFile(path).CreatedTime.ToUniversalTime();
        }

        /// <inheritdoc />
        public DateTime GetLastAccessTime(string path)
        {
            return (this.FileSystem as InMemoryFileSystem).GetFile(path).LastAccessTime;
        }

        /// <inheritdoc />
        public DateTime GetLastAccessTimeUtc(string path)
        {
            return (this.FileSystem as InMemoryFileSystem).GetFile(path).LastAccessTime.ToUniversalTime();
        }

        /// <inheritdoc />
        public DateTime GetLastWriteTime(string path)
        {
            return (this.FileSystem as InMemoryFileSystem).GetFile(path).LastWriteTime;
        }

        /// <inheritdoc />
        public DateTime GetLastWriteTimeUtc(string path)
        {
            return (this.FileSystem as InMemoryFileSystem).GetFile(path).LastWriteTime.ToUniversalTime();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Move(string sourceFileName, string destFileName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Move(string sourceFileName, string destFileName, bool overwrite)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public FileSystemStream Open(string path, FileMode mode)
        {
            return this.Open(path, mode, FileAccess.ReadWrite, FileShare.ReadWrite);
        }

        /// <inheritdoc />
        public FileSystemStream Open(string path, FileMode mode, FileAccess access)
        {
            return this.Open(path, mode, access, FileShare.ReadWrite);
        }

        /// <inheritdoc />
        public FileSystemStream Open(string path, FileMode mode, FileAccess access, FileShare share)
        {
            InMemoryFile file = null;

            switch (mode)
            {
                case FileMode.Open:
                    file = (this.FileSystem as InMemoryFileSystem).GetFile(path);
                    file.LastAccessTime = DateTime.UtcNow;
                    break;

                case FileMode.Append:
                    file = (this.FileSystem as InMemoryFileSystem).GetFile(path);
                    file.LastAccessTime = DateTime.UtcNow;
                    break;

                case FileMode.Create:
                case FileMode.OpenOrCreate:
                    file = (this.FileSystem as InMemoryFileSystem).AddOrGetFile(path);
                    file.LastAccessTime = DateTime.UtcNow;
                    break;

                case FileMode.CreateNew:
                    if ((this.FileSystem as InMemoryFileSystem).TryGetFile(path, out file))
                    {
                        throw new IOException($"The file '{path}' already exists.");
                    }

                    file = (this.FileSystem as InMemoryFileSystem).AddOrGetFile(path);
                    break;

                case FileMode.Truncate:
                    file = (this.FileSystem as InMemoryFileSystem).GetFile(path);
                    file.ClearContent();
                    break;
            }

            (this.FileSystem as InMemoryFileSystem).OnReadFile?.Invoke(path);
            
            return new InMemoryFileSystemStream(new MemoryStream(file.FileBytes.ToArray()), path, false);
        }

        public FileSystemStream Open(string path, FileStreamOptions options)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public FileSystemStream OpenRead(string path)
        {
            return this.Open(path, FileMode.Open);
        }

        /// <inheritdoc />
        public StreamReader OpenText(string path)
        {
            return new StreamReader(this.Open(path, FileMode.Open));
        }

        /// <inheritdoc />
        public FileSystemStream OpenWrite(string path)
        {
            return this.Open(path, FileMode.OpenOrCreate);
        }

        /// <inheritdoc />
        public byte[] ReadAllBytes(string path)
        {
            InMemoryFile file = (this.FileSystem as InMemoryFileSystem).GetFile(path);
            file.LastAccessTime = DateTime.UtcNow;
            (this.FileSystem as InMemoryFileSystem).OnReadFile?.Invoke(path);
            return file.FileBytes.ToArray();
        }

        /// <inheritdoc />
        public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.ReadAllBytes(path));
        }

        /// <inheritdoc />
        public string[] ReadAllLines(string path)
        {
            return this.ReadAllLines(path, InMemoryFile.DefaultEncoding);
        }

        /// <inheritdoc />
        public string[] ReadAllLines(string path, Encoding encoding)
        {
            return this.ReadAllText(path, encoding).Split(Environment.NewLine);
        }

        /// <inheritdoc />
        public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default)
        {
            return this.ReadAllLinesAsync(path, InMemoryFile.DefaultEncoding, cancellationToken);
        }

        /// <inheritdoc />
        public Task<string[]> ReadAllLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.ReadAllLines(path, encoding));
        }

        /// <inheritdoc />
        public string ReadAllText(string path)
        {
            return this.ReadAllText(path, InMemoryFile.DefaultEncoding);
        }

        /// <inheritdoc />
        public string ReadAllText(string path, Encoding encoding)
        {
            InMemoryFile file = (this.FileSystem as InMemoryFileSystem).GetFile(path);
            file.LastAccessTime = DateTime.UtcNow;
            (this.FileSystem as InMemoryFileSystem).OnReadFile?.Invoke(path);
            return encoding.GetString(file.FileBytes.ToArray());
        }

        /// <inheritdoc />
        public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.ReadAllText(path, InMemoryFile.DefaultEncoding));
        }

        /// <inheritdoc />
        public Task<string> ReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.ReadAllText(path, encoding));
        }

        /// <inheritdoc />
        public IEnumerable<string> ReadLines(string path)
        {
            return this.ReadAllLines(path, InMemoryFile.DefaultEncoding);
        }

        /// <inheritdoc />
        public IEnumerable<string> ReadLines(string path, Encoding encoding)
        {
            return this.ReadAllLines(path, encoding);
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors)
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
        public void SetAccessControl(string path, FileSecurity fileSecurity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void SetAttributes(string path, FileAttributes fileAttributes)
        {
            (this.FileSystem as InMemoryFileSystem).GetFile(path).Attributes = fileAttributes;
        }

        /// <inheritdoc />
        public void SetCreationTime(string path, DateTime creationTime)
        {
            (this.FileSystem as InMemoryFileSystem).GetFile(path).CreatedTime = creationTime;
        }

        /// <inheritdoc />
        public void SetCreationTimeUtc(string path, DateTime creationTimeUtc)
        {
            (this.FileSystem as InMemoryFileSystem).GetFile(path).CreatedTime = creationTimeUtc.ToUniversalTime();
        }

        /// <inheritdoc />
        public void SetLastAccessTime(string path, DateTime lastAccessTime)
        {
            (this.FileSystem as InMemoryFileSystem).GetFile(path).LastAccessTime = lastAccessTime;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc)
        {
            (this.FileSystem as InMemoryFileSystem).GetFile(path).LastAccessTime = lastAccessTimeUtc.ToUniversalTime();
        }

        /// <inheritdoc />
        public void SetLastWriteTime(string path, DateTime lastWriteTime)
        {
            (this.FileSystem as InMemoryFileSystem).GetFile(path).LastWriteTime = lastWriteTime;
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc)
        {
            (this.FileSystem as InMemoryFileSystem).GetFile(path).LastWriteTime = lastWriteTimeUtc.ToUniversalTime();
        }

        /// <inheritdoc />
        public void WriteAllBytes(string path, byte[] bytes)
        {
            InMemoryFile file = (this.FileSystem as InMemoryFileSystem).AddOrGetFile(path);
            file.SetContent(bytes, file.ContentEncoding);
            (this.FileSystem as InMemoryFileSystem).OnWriteFile?.Invoke(path, bytes);
        }

        public void WriteAllBytes(string path, ReadOnlySpan<byte> bytes)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            this.WriteAllBytes(path, bytes);
            return Task.CompletedTask;
        }

        public Task WriteAllBytesAsync(string path, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void WriteAllLines(string path, IEnumerable<string> contents)
        {
            this.WriteAllLines(path, contents.ToArray(), InMemoryFile.DefaultEncoding);
        }

        /// <inheritdoc />
        public void WriteAllLines(string path, IEnumerable<string> contents, Encoding encoding)
        {
            this.WriteAllLines(path, contents.ToArray(), encoding);
        }

        /// <inheritdoc />
        public void WriteAllLines(string path, string[] contents)
        {
            this.WriteAllLines(path, contents, InMemoryFile.DefaultEncoding);
        }

        /// <inheritdoc />
        public void WriteAllLines(string path, string[] contents, Encoding encoding)
        {
            this.WriteAllBytes(path, encoding.GetBytes(string.Join(Environment.NewLine, contents)));
        }

        /// <inheritdoc />
        public Task WriteAllLinesAsync(string path, IEnumerable<string> contents, CancellationToken cancellationToken = default)
        {
            return this.WriteAllLinesAsync(path, contents, InMemoryFile.DefaultEncoding, cancellationToken);
        }

        /// <inheritdoc />
        public Task WriteAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default)
        {
            this.WriteAllLines(path, contents, encoding);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task WriteAllLinesAsync(string path, string[] contents, CancellationToken cancellationToken = default)
        {
            return this.WriteAllLinesAsync(path, contents, InMemoryFile.DefaultEncoding, cancellationToken);
        }

        /// <inheritdoc />
        public Task WriteAllLinesAsync(string path, string[] contents, Encoding encoding, CancellationToken cancellationToken = default)
        {
            this.WriteAllLines(path, contents, encoding);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void WriteAllText(string path, string contents)
        {
            this.WriteAllText(path, contents, InMemoryFile.DefaultEncoding);
        }

        /// <inheritdoc />
        public void WriteAllText(string path, string contents, Encoding encoding)
        {
            this.WriteAllBytes(path, encoding.GetBytes(contents));
        }

        public void WriteAllText(string path, ReadOnlySpan<char> contents)
        {
            throw new NotImplementedException();
        }

        public void WriteAllText(string path, ReadOnlySpan<char> contents, Encoding encoding)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default)
        {
            return this.WriteAllTextAsync(path, contents, InMemoryFile.DefaultEncoding, cancellationToken);
        }

        /// <inheritdoc />
        public Task WriteAllTextAsync(string path, string contents, Encoding encoding, CancellationToken cancellationToken = default)
        {
            this.WriteAllText(path, contents, encoding);
            return Task.CompletedTask;
        }

        public Task WriteAllTextAsync(string path, ReadOnlyMemory<char> contents, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task WriteAllTextAsync(string path, ReadOnlyMemory<char> contents, Encoding encoding, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        FileAttributes IFile.GetAttributes(SafeFileHandle fileHandle)
        {
            throw new NotImplementedException();
        }

        DateTime IFile.GetCreationTime(SafeFileHandle fileHandle)
        {
            throw new NotImplementedException();
        }

        DateTime IFile.GetCreationTimeUtc(SafeFileHandle fileHandle)
        {
            throw new NotImplementedException();
        }

        DateTime IFile.GetLastAccessTime(SafeFileHandle fileHandle)
        {
            throw new NotImplementedException();
        }

        DateTime IFile.GetLastAccessTimeUtc(SafeFileHandle fileHandle)
        {
            throw new NotImplementedException();
        }

        DateTime IFile.GetLastWriteTime(SafeFileHandle fileHandle)
        {
            throw new NotImplementedException();
        }

        DateTime IFile.GetLastWriteTimeUtc(SafeFileHandle fileHandle)
        {
            throw new NotImplementedException();
        }

        UnixFileMode IFile.GetUnixFileMode(string path)
        {
            throw new NotImplementedException();
        }

        UnixFileMode IFile.GetUnixFileMode(SafeFileHandle fileHandle)
        {
            throw new NotImplementedException();
        }

        IAsyncEnumerable<string> IFile.ReadLinesAsync(string path, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        IAsyncEnumerable<string> IFile.ReadLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        void IFile.SetAttributes(SafeFileHandle fileHandle, FileAttributes fileAttributes)
        {
            throw new NotImplementedException();
        }

        void IFile.SetCreationTime(SafeFileHandle fileHandle, DateTime creationTime)
        {
            throw new NotImplementedException();
        }

        void IFile.SetCreationTimeUtc(SafeFileHandle fileHandle, DateTime creationTimeUtc)
        {
            throw new NotImplementedException();
        }

        void IFile.SetLastAccessTime(SafeFileHandle fileHandle, DateTime lastAccessTime)
        {
            throw new NotImplementedException();
        }

        void IFile.SetLastAccessTimeUtc(SafeFileHandle fileHandle, DateTime lastAccessTimeUtc)
        {
            throw new NotImplementedException();
        }

        void IFile.SetLastWriteTime(SafeFileHandle fileHandle, DateTime lastWriteTime)
        {
            throw new NotImplementedException();
        }

        void IFile.SetLastWriteTimeUtc(SafeFileHandle fileHandle, DateTime lastWriteTimeUtc)
        {
            throw new NotImplementedException();
        }

        void IFile.SetUnixFileMode(string path, UnixFileMode mode)
        {
            throw new NotImplementedException();
        }

        void IFile.SetUnixFileMode(SafeFileHandle fileHandle, UnixFileMode mode)
        {
            throw new NotImplementedException();
        }
    }
}
