// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Represents an in-memory file system file that can be used for test scenarios.
    /// </summary>
    [DebuggerDisplay("File: {Path}")]
    public class InMemoryFile : InMemoryFileSystemEntry
    {
        /// <summary>
        /// The default encoding to use for file content (in-memory).
        /// </summary>
        public static readonly Encoding DefaultEncoding = Encoding.UTF8;

        /// <summary>
        /// Initializes an instance of the <see cref="InMemoryFile"/> class.
        /// </summary>
        public InMemoryFile(string path, InMemoryDirectory directory)
            : this(path, directory, DateTime.UtcNow)
        {
        }

        /// <summary>
        /// Initializes an instance of the <see cref="InMemoryFile"/> class.
        /// </summary>
        public InMemoryFile(string path, InMemoryDirectory directory, DateTime createdTime)
            : base(path, FileAttributes.Normal, createdTime)
        {
            this.Name = System.IO.Path.GetFileName(path);
            this.Directory = directory;
            this.ContentEncoding = Encoding.UTF8;
            this.FileBytes = new List<byte>();
        }

        /// <summary>
        /// The directory that contains the file.
        /// </summary>
        public InMemoryDirectory Directory { get; }

        /// <summary>
        /// The encoding for the file content/bytes.
        /// </summary>
        public Encoding ContentEncoding { get; private set; }

        /// <summary>
        /// The full name of the file.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Represents the contents of the file (in-memory).
        /// </summary>
        public List<byte> FileBytes { get; private set; }

        /// <summary>
        /// Sets the file content.
        /// </summary>
        public void AppendContent(byte[] fileBytes, Encoding encoding)
        {
            if (fileBytes?.Any() == true && this.ContentEncoding != encoding)
            {
                throw new NotSupportedException(
                    $"Invalid operation. The contents of the file use an encoding '{this.ContentEncoding.EncodingName}' that does not match " +
                    $"with the encoding supplied '{encoding.EncodingName}'.");
            }

            this.FileBytes.AddRange(fileBytes);
            this.ContentEncoding = encoding;
            this.LastAccessTime = DateTime.UtcNow;
            this.LastWriteTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the file content.
        /// </summary>
        public void ClearContent()
        {
            this.FileBytes.Clear();
            this.LastAccessTime = DateTime.UtcNow;
            this.LastWriteTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Sets the file content.
        /// </summary>
        public void SetContent(byte[] fileBytes, Encoding encoding)
        {
            this.ClearContent();
            if (fileBytes?.Any() == true)
            {
                this.FileBytes.AddRange(fileBytes);
                this.ContentEncoding = encoding;
            }
        }
    }
}
