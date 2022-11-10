// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace VirtualClient
{
    using System.IO.Abstractions;
    using VirtualClient.Contracts;

    /// <summary>
    /// State manager for packages that exist on the system.
    /// </summary>
    public class PackageStateManager : StateManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PackageStateManager"/> class.
        /// </summary>
        /// <param name="fileSystem">Provides features for interacting with the file system.</param>
        /// <param name="platformSpecifics">Provides platform-specific path information.</param>
        public PackageStateManager(IFileSystem fileSystem, PlatformSpecifics platformSpecifics)
            : base(fileSystem, platformSpecifics)
        {
        }

        /// <summary>
        /// Returns the full path to the package state location.
        /// </summary>
        /// <param name="packageName">The name of the package.</param>
        protected override string GetStateFilePath(string packageName)
        {
            // Example:
            // C:\any\directory\VirtualClient.1.2.3.4\content\VirtualClient.exe
            // C:\any\directory\VirtualClient.1.2.3.4\content\packages\examplepackage.vcpkgreg
            return this.PlatformSpecifics.GetPackagePath($"{packageName.ToLowerInvariant()}{PackageManager.VCPkgRegExtension}");
        }
    }
}
