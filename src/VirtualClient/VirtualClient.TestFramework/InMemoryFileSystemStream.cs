// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.IO;
    using System.IO.Abstractions;

    /// <summary>
    /// Concrete implementation of <see cref="FileSystemStream"/> that wraps the <see cref="FileStream"/>.
    /// </summary>
    public class InMemoryFileSystemStream : FileSystemStream
    {
        public InMemoryFileSystemStream()
            : base(new MemoryStream(), "fakepath", false)
        {
        }

        public InMemoryFileSystemStream(Stream stream, string path, bool isAsync) 
            : base(stream, path, isAsync)
        {
        }
    }
}
