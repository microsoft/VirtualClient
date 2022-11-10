using VirtualClient.Common.Extensions;

// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    /// <summary>
    /// Represents a dependency store on a file system.
    /// </summary>
    public class DependencyFileStore : DependencyStore
    {
        /// <summary>
        /// Initializes an instance of the <see cref="DependencyFileStore"/> class.
        /// </summary>
        /// <param name="storeName">The name of the file store (e.g. Content, Packages).</param>
        /// <param name="directoryPath">A path to the directory for the store.</param>
        public DependencyFileStore(string storeName, string directoryPath)
            : base(storeName, DependencyStore.StoreTypeFileSystem)
        {
            directoryPath.ThrowIfNullOrWhiteSpace(nameof(directoryPath));
            this.DirectoryPath = directoryPath;
        }

        /// <summary>
        /// A path to the directory for the store where blobs/files can be uploaded or
        /// downloaded depending upon the store type.
        /// </summary>
        public string DirectoryPath { get; }
    }
}
