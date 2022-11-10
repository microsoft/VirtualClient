// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Represents an in-memory file system directory that can be used for test scenarios.
    /// </summary>
    [DebuggerDisplay("Directory: {Path}")]
    public class InMemoryDirectory : InMemoryFileSystemEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryDirectory"/> class.
        /// </summary>
        public InMemoryDirectory(string path, string parentPath)
            : this(path, parentPath, DateTime.UtcNow)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemoryDirectory"/> class.
        /// </summary>
        public InMemoryDirectory(string path, string parentPath, DateTime createdTime)
            : base(path, System.IO.FileAttributes.Directory, createdTime)
        {
            this.ParentPath = parentPath;
        }

        /// <summary>
        /// Gets the parent directory/path of this directory.
        /// </summary>
        public string ParentPath { get; }
    }
}
